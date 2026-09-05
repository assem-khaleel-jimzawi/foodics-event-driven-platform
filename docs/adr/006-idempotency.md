# ADR 006: Inbox idempotency

## Status

Accepted (Phase 2)

## Context

RabbitMQ and the outbox publisher give **at-least-once** delivery. `OrderCreated` can arrive twice. Reserving twice would double-hold stock. Charging twice would double-charge the fake payment.

Exactly-once across two systems is not available. Exactly-once *effects* are: detect a duplicate and skip the side effect.

## Decision

Each consumer claims an inbox row keyed by `(consumer name, EventId)` before mutating business data. The unique primary key makes a race between two workers fail one `SaveChanges`. A second delivery sees the row and returns.

Payments also has a unique index on `Payment.OrderId` as a second belt.

Permanent faults are thrown **before** the inbox claim so a bad payload can land in `_error` without poisoning the inbox.

Transient faults are thrown **after** the in-memory claim but **before** `SaveChanges`, so the claim is not committed and the retry can run the handler again.

## Consequences

- Duplicate `EventId` → one reservation, one payment.
- A new `EventId` for the same order is treated as a new fact. Publishers must keep `EventId` stable when they retry.
- Inbox tables are per service, next to that service's data.

## Alternatives considered

- Idempotency keyed only on `OrderId`: cannot distinguish a legitimate second command.
- Broker-side exactly-once: Kafka transactions do not extend to PostgreSQL stock rows.
- Natural keys without an inbox table: easy to miss a consumer.
