# FoodFlow architecture

## Problem

A restaurant platform has to accept orders while menu, stock, payment, and notification concerns change and fail independently. A shared database or a single deployable couples those failure domains.

## Style

Pragmatic Clean Architecture inside a service:

- `Api` — HTTP, ProblemDetails, composition root
- `Application` — use cases and HTTP DTOs
- `Domain` — invariants
- `Infrastructure` — EF Core / PostgreSQL

Shared code is intentionally small:

- `FoodFlow.Contracts` — immutable integration records (commands vs events)
- `FoodFlow.ServiceDefaults` — logging, health, OpenAPI, correlation id

Services do **not** share entities, DbContexts, or business rules.

## Service boundaries

```text
Orders     -> OrdersDb
Catalog    -> CatalogDb
Inventory  -> InventoryDb   (Phase 2)
Payments   -> PaymentsDb    (Phase 2)
Notifications worker        (Phase 2 consumers)
Analytics worker            (Phase 2 Kafka)
```

Synchronous REST is used for client-facing commands and queries. Service-to-service communication in later phases is asynchronous, except where a genuine request/response is required.

Orders does not call Catalog in Phase 1. Line items carry a product snapshot. That avoids a distributed read on the checkout path and matches how restaurant tickets usually freeze the sold name and price.

## Communication plan

| Path | Transport | Phase |
| --- | --- | --- |
| Client -> Orders/Catalog | HTTP | 1 |
| OrderCreated -> Inventory | RabbitMQ command/event | 2 |
| InventoryReserved -> Payments | RabbitMQ | 2 |
| PaymentCompleted/Failed -> Orders | RabbitMQ | 2 |
| OrderConfirmed/Cancelled -> Notifications | RabbitMQ | 2 |
| Selected facts -> Analytics | Kafka | 2 |

RabbitMQ and Kafka are not given the same job. RabbitMQ moves operational work. Kafka retains an analytics stream that can be replayed.

## Reliability plan

These are designed in, not coded in Phase 1:

- Transactional outbox for the dual-write problem
- Inbox/processed messages for at-least-once delivery
- Retry only for transient faults
- Dead-letter/error queues after exhaustion
- Choreographed compensation when payment fails after reservation

## Health

- `/health/live` — process is up. It does not depend on PostgreSQL. Kubernetes-style liveness that requires the database causes restart loops during a brief DB blip.
- `/health/ready` — this instance can take traffic. Orders/Catalog include an EF Core database check.

## Money and identity

Money is `decimal` with an explicit ISO currency. IDs are `Guid`. Timestamps are UTC (`DateTimeOffset`).

## What Phase 1 deliberately skips

No MassTransit, no Kafka client, no outbox table, no inbox table, no service Dockerfiles beyond Compose for PostgreSQL. Implementing them now would mix delivery phases and make the interview story harder to tell.
