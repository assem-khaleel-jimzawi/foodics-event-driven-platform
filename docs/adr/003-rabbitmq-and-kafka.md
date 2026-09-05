# ADR 003: RabbitMQ for operations, Kafka for analytics

## Status

Accepted (Phase 2)

## Context

Phase 2 needs asynchronous communication. Using one broker for every message shape is simpler. It is also the wrong story for a Foodics-style platform.

## Decision

- **RabbitMQ + MassTransit** moves operational work: commands and the events the workflow reacts to.
- **Kafka** retains a replayable analytics stream. The Analytics worker is a Kafka consumer group. It is not a second copy of the order pipeline.

Selected business facts are written to Kafka from the same local outbox transaction that writes the RabbitMQ row. That is a bridge, not a dual-write to two brokers from application code.

## Consequences

- Payment and inventory consumers can compete on queues, retry, and dead-letter with MassTransit `_error` queues.
- Analytics can lag, restart, or replay without blocking checkout.
- If Kafka is down, operational outbox rows for Kafka stay unpublished and retry. RabbitMQ rows for the same business transaction can still flow.
- If RabbitMQ is down, outbox rows stay in PostgreSQL. The order is already committed. The workflow resumes when the broker returns.

## Alternatives considered

- Kafka for everything: poor default for command work, competing consumers, and per-message retry/dead-letter.
- RabbitMQ for everything: weak replay and independent analytics consumption.
- Two independent publishes (DB then broker): dual-write. Rejected; see the outbox.
