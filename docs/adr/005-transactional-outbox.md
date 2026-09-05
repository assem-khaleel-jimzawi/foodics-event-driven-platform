# ADR 005: Transactional outbox

## Status

Accepted (Phase 2)

## Context

The dual-write problem: `SaveChanges` on PostgreSQL and `Publish` to a broker are two systems. If the process dies after the commit and before the publish, Inventory never sees `OrderCreated`. If it publishes first and then the commit fails, Inventory reserves stock for an order that does not exist.

MassTransit's EF outbox would work. This codebase uses an explicit `outbox_messages` table and `OutboxProcessor<TContext>` so the interview story is one screen of SQL plus a hosted service, not a library internals dive.

## Decision

In the same local transaction:

```text
BEGIN
  save business row
  insert outbox row(s)   -- RabbitMQ, and Kafka when the contract is an analytics event
COMMIT
```

A background publisher polls unpublished rows, publishes, then sets `PublishedAtUtc`. If the broker is down, the row stays unpublished and is retried. If the process crashes after a successful publish and before the flag is saved, the message is delivered twice. Consumers are idempotent (ADR 006).

## Consequences

- Database success + broker outage does not lose the event.
- Each service owns its outbox table. There is no shared outbox database.
- Publisher lag is a first-class operational concern (Phase 3 metrics).

## Alternatives considered

- Publish in the HTTP request after `SaveChanges`: classic dual-write.
- Listen to the PostgreSQL WAL (Debezium): excellent at scale, heavy for this demo.
- MassTransit EF outbox: same pattern, less visible in our code.
