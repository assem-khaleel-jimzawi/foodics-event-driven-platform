# Interview guide — FoodFlow as a senior .NET discussion

Use this with the code open. Answers describe **this repository**, not a generic textbook platform. If a capability is not implemented, the answer says so.

Suggested shape for any design question: **Problem → Constraints → Options → Trade-offs → Decision → Implementation → Failure scenarios → Monitoring → Result**.

## How to walk the project (10 minutes)

1. Phase 1: service boundaries, database-per-service, Orders/Catalog HTTP, snapshots on order lines.
2. Phase 2: choreography, RabbitMQ vs Kafka, outbox/inbox, compensation, `_error`.
3. Phase 3: OpenTelemetry, Prometheus/Grafana/Tempo, health, CI, what you would still add for production.

Demo: `docker compose up --build -d`, post an order, show Grafana **FoodFlow overview**, Prometheus targets, RabbitMQ queues, Tempo search by service `foodflow-orders`.

---

## Why microservices?

**Problem:** menu, stock, payment, and notification fail and scale independently.  
**Decision:** six processes with explicit contracts, not six folders in one deployable that still share a database.  
**Trade-off:** operational cost. This demo pays it to show the patterns Foodics cares about, not because a two-person restaurant needs Kubernetes.  
**Implementation:** `src/Services/*` plus Analytics worker.  
**What we did not do:** split every class into a service.

## Why database-per-service?

So Inventory can lock stock without taking a SQL lock in OrdersDb. Joins across services are forbidden. Catalog is not read during checkout; the client sends a product snapshot. See ADR 001.

Failure: you cannot run `SELECT * FROM orders JOIN stock`. You query each API or you consume events (Analytics).

## Why RabbitMQ?

Checkout is **work**: competing consumers, per-message ack, short retry, inspectable `_error` queues. MassTransit 8.5.10 on RabbitMQ 3.13. Commands (`ReserveInventory`) and workflow events (`InventoryReserved`, `PaymentCompleted`) travel here.

## Why Kafka?

Analytics needs a **replayable, independently consumable log**. Topics `foodflow.orders` and `foodflow.payments`, keys = order id, group `foodflow-analytics`. Operational consumers do not read Kafka to reserve or charge.

## Why both?

They overlap. Using one broker for both jobs usually means forcing Kafka to be a work queue or RabbitMQ to be a replay log. FoodFlow gives each the job it already does well (ADR 003).

## RabbitMQ vs Kafka?

| | RabbitMQ (here) | Kafka (here) |
| --- | --- | --- |
| Role | Operational commands/events | Analytics facts |
| Consume | Competing queue consumers | Consumer group on a log |
| Retry / DLQ | MassTransit retry + `_error` | Skip unknown payload; no Kafka DLQ |
| Replay | Not the point | Offsets / new group |
| If down | Outbox retries; checkout delayed | Checkout continues; analytics lags |

## What is event-driven architecture?

Services react to facts and commands on a broker instead of calling each other’s databases. FoodFlow is **choreography**: Inventory publishes `InventoryReserved`; Payments consumes it. There is no central saga orchestrator process.

## Command vs event?

- **Command:** `ReserveInventory`, `ReleaseInventory` — an intent directed at Inventory.
- **Event:** `OrderCreated`, `InventoryReserved`, `PaymentFailed` — something that already happened.

`ProcessPayment` exists as a **contract only**. Payments must not consume both `ProcessPayment` and `InventoryReserved` or you get two charge paths.

## What is transactional outbox?

Write the business row and `outbox_messages` in one PostgreSQL transaction. `OutboxProcessor` publishes to RabbitMQ and, for analytics contracts, Kafka. Avoids the dual-write hole (commit vs publish). ADR 005. Phase 3 stores `TraceParent` on the row so the later publish stays in the HTTP trace.

## What is idempotency?

At-least-once delivery is assumed. Effects must happen once. Inbox `(consumer, EventId)`, unique `Payment.OrderId`, reservation keyed by order. ADR 006.

## What happens with duplicate messages?

Second delivery: inbox claim fails → handler returns. Duplicate `ReserveInventory` with a **new** EventId for an already reserved order is ignored by Inventory’s reservation-by-order logic (see `InventoryService`). Integration test publishes the command twice after confirm and asserts reserved count is unchanged.

## What is eventual consistency?

Orders returns `201` in `Created` before Inventory or Payments finish. The client polls `GET /api/orders/{id}` until `Confirmed` or `Cancelled`. Analytics is even later (Kafka). There is no distributed transaction.

## What is a Saga?

A long-running business transaction that **compensates** instead of rolling back other databases. FoodFlow’s saga is the choreography: reserve → pay → confirm, or pay fail → `ReleaseInventory` + `OrderCancelled`.

## Choreography vs orchestration?

**Choreography (this repo):** each service publishes facts; others decide.  
**Orchestration:** a coordinator sends commands (`ProcessPayment` would fit that style). We avoided a second charge path and a conductor service.

## What happens when payment fails?

Fake gateway declines customer `22222222-...`. Payments stores `Failed` and outbox `PaymentFailed`. Orders cancels, outbox `OrderCancelled` + `ReleaseInventory`. Inventory releases reserved qty. Notifications record cancellation. Kafka gets payment-failed / order-cancelled facts. Metrics: `payments.failed`, `orders.cancelled`.

## What happens when RabbitMQ is unavailable?

HTTP + PostgreSQL still succeed. Outbox rows stay unpublished (`PublishedAtUtc` null, `AttemptCount` increases). When RabbitMQ returns, the processor publishes. Orders stay `Created` until the workflow runs.

## What happens when Kafka is unavailable?

Same for Kafka-destination outbox rows. RabbitMQ workflow still confirms the order. Analytics snapshot lags. Producer timeouts are 5–10s so the processor does not hang forever.

## What is a dead-letter queue?

MassTransit `_error` queues after retries or `PermanentMessagingException`. Demo poison product `44444444-...` → `reserve-inventory_error`. The order stays `Created` because Inventory never published `InventoryReservationFailed` for that poison path. Inspect in RabbitMQ UI.

## Retry vs compensation?

**Retry:** same message, transient fault (DB blip, `TransientMessagingException`, default exceptions except permanent).  
**Compensation:** a **new** command/event that undoes a previous success (`ReleaseInventory` after payment failure). Do not retry a declined card.

## What is OpenTelemetry?

A vendor-neutral SDK and wire protocol for traces, metrics, and (optionally) logs. FoodFlow uses the official .NET packages: ASP.NET/Http/Runtime/Npgsql/MassTransit + custom `FoodFlow` source/meter. Export: OTLP to Tempo, Prometheus scrape of `/metrics`. ADR 007.

## What is distributed tracing?

A tree of spans sharing a `TraceId`. You see POST `/api/orders` → outbox publish → MassTransit consume → SQL. Implemented with `Activity` / W3C `traceparent`, not a homemade GUID in a log line only.

## TraceId vs CorrelationId?

`TraceId` is W3C tracing. `CorrelationId` is `X-Correlation-Id` on HTTP and on contracts. Logs include both. Correlation still works if Tempo is off. Tracing gives parent/child timing Tempo can draw.

## What is Prometheus?

A pull-based metrics server. It scrapes `/metrics` on each FoodFlow service. Query at http://localhost:9090. This demo does not use Prometheus Alertmanager.

## What is Grafana?

A dashboard UI. Compose provisions Prometheus + Tempo datasources and **FoodFlow overview**. Login `foodflow` / `foodflow`. It does not store the system of record.

## How do you monitor microservices?

1. **Live/ready** — is the process up, can it take work?  
2. **RED metrics** — rate, errors, duration (HTTP histograms + business counters).  
3. **Broker** — RabbitMQ rates and `_error`; Kafka consumer lag.  
4. **Traces** — one checkout across services.  
5. **Logs** — JSON with correlation + trace ids.

## How do you debug a failed order across services?

1. Client `X-Correlation-Id` (or the one the API assigned).  
2. `GET /api/orders/{id}` status.  
3. Payments `GET /api/payments/by-order/{id}`, Inventory stock, Notifications by order, Analytics snapshot.  
4. Logs: `docker compose logs orders inventory payments | grep <correlation>`.  
5. Tempo: search `foodflow-orders` around the timestamp; follow children.  
6. RabbitMQ: stuck queue vs `_error`.  
7. Outbox: `PublishedAtUtc` null or `LastError` set.

## How do you prevent double payment?

Inbox on `InventoryReserved.EventId`, unique index on `Payment.OrderId`, Payments does not consume `ProcessPayment`. Fake gateway is not the control; the database is.

## How do you prevent overselling?

`StockItem` reserved/on-hand invariants in the Inventory domain, row updates in InventoryDb only, inbox on reserve commands. Catalog availability is not the lock.

## How do you deploy this architecture?

Today: Docker Compose on a laptop. CI builds and tests on GitHub Actions. Production would be: one deployable per service, managed Postgres, managed RabbitMQ/Kafka, secret store, migrate jobs **before** new consumers, OTLP to a real backend (Grafana Cloud, Tempo, etc.), no admin ports on the public internet. **Kubernetes is not in this repo.**

## What would you change for production?

AuthN/Z, real payment/SMS, secret manager, HA brokers, Postgres backup/PITR, outbox alerting on lag, Kafka lag alerts, structured log shipping, sampling for traces, dedicated migration runners, remove default passwords, network policies, and load tests. I would **not** add Redis/Elasticsearch/another broker just to look busy.

## Useful code pointers

| Topic | Where |
| --- | --- |
| Choreography | `OrderService`, `InventoryService`, `PaymentService` |
| Outbox | `OutboxProcessor`, `OutboxEnqueue` |
| Inbox | `Inbox.TryClaimAsync` |
| OTel | `OpenTelemetryExtensions`, `FoodFlowTelemetry` |
| Metrics HTTP | `MapPrometheusScrapingEndpoint("/metrics")` |
| Compose | `docker-compose.yml`, `deploy/observability/` |
