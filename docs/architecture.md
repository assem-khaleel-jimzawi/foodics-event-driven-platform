# FoodFlow architecture

## Problem

A restaurant platform has to accept orders while menu, stock, payment, and notification concerns change and fail independently. A shared database or a single deployable couples those failure domains.

## Style

Pragmatic Clean Architecture inside a service:

- `Api` — HTTP, ProblemDetails, composition root
- `Application` — use cases and HTTP DTOs
- `Domain` — invariants
- `Infrastructure` — EF Core / PostgreSQL, MassTransit consumers, outbox processor

Shared code is intentionally small:

- `FoodFlow.Contracts` — immutable integration records (commands vs events)
- `FoodFlow.Messaging` — MassTransit registration, outbox, inbox, Kafka producer
- `FoodFlow.ServiceDefaults` — logging, health, OpenAPI, correlation id

Services do **not** share entities, DbContexts, or business rules.

## Service boundaries

```text
Orders         -> OrdersDb
Catalog        -> CatalogDb
Inventory      -> InventoryDb
Payments       -> PaymentsDb
Notifications  -> NotificationsDb
Analytics      -> Kafka consumer group foodflow-analytics
```

Synchronous REST is used for client-facing commands and queries. Service-to-service communication is asynchronous.

Orders does not call Catalog. Line items carry a product snapshot.

## Communication

| Path | Transport |
| --- | --- |
| Client -> Orders/Catalog/Inventory/Payments | HTTP |
| ReserveInventory / ReleaseInventory | RabbitMQ commands |
| InventoryReserved (Payments reacts) | RabbitMQ event used as the next-step trigger |
| Inventory/payment/order facts the workflow reacts to | RabbitMQ events |
| Order/payment/inventory facts for analytics | Kafka topics `foodflow.orders`, `foodflow.payments` |

RabbitMQ and Kafka are not given the same job. See [ADR 003](adr/003-rabbitmq-and-kafka.md) and [Phase 2](phase-2-event-driven.md).

## Reliability

- Transactional outbox for the dual-write problem
- Inbox/processed messages for at-least-once delivery
- Retry only for transient faults
- Dead-letter/error queues after exhaustion or permanent exceptions
- Choreographed compensation when payment fails after reservation

## Health

- `/health/live` — process is up. It does not depend on PostgreSQL.
- `/health/ready` — this instance can take traffic. Database-backed services include an EF Core check.

## Money and identity

Money is `decimal` with an explicit ISO currency. IDs are `Guid`. Timestamps are UTC (`DateTimeOffset`).
