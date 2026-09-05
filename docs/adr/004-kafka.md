# ADR 004: Kafka for analytics streaming

## Status

Accepted (Phase 2)

## Context

Finance and operations will want to replay "what happened" without re-driving Inventory or Payments. RabbitMQ queues are not a durable, replayable log for independent readers.

## Decision

Publish a **copy** of selected business events to Kafka topics `foodflow.orders` and `foodflow.payments` through the same outbox that publishes to RabbitMQ. A single analytics consumer group (`foodflow-analytics`) projects those facts. The Compose demo keeps the projection in memory; Kafka offsets are the durable cursor.

Kafka is not used to reserve stock, charge cards, or cancel orders.

## Consequences

- A new analytics reader can join with a new group id and read history (until retention expires).
- Partition key `OrderId` preserves per-order order.
- Two destinations mean two outbox rows. That is deliberate: different delivery contracts, different failure modes.

## Alternatives considered

- One broker for everything: simpler diagram, worse fit for one of the two jobs.
- CDC from PostgreSQL WAL into Kafka: stronger for data lakes, more moving parts than this assignment needs.
- MassTransit Kafka rider for the workflow: would hide the distinction this project exists to explain.
