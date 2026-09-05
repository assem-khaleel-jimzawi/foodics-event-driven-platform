# ADR 003: RabbitMQ for operational messaging

## Status

Accepted (Phase 2)

## Context

The order workflow is a set of short-lived work items: reserve, pay, confirm, notify, release. Services need competing consumers, retries for blips, and an inspectable place for poison messages.

## Decision

Use RabbitMQ 3.13 with MassTransit 8.5 (Apache 2.0) as the operational bus. Each consumer gets its own queue. Retry transient faults; `PermanentMessagingException` is not retried. Permanent faults go to the MassTransit `_error` queue. MassTransit 9 was rejected because it requires a commercial license that Compose and CI cannot assume.

## Consequences

- Workflow wiring is explicit in consumers (`OrderCreated` → Inventory, `InventoryReserved` → Payments).
- RabbitMQ downtime does not lose the business fact: the transactional outbox holds it.
- Operators debug failed work in the management UI, not by scanning a compacted log.

## Alternatives considered

- Raw `RabbitMQ.Client`: more code, same broker, worse endpoint conventions.
- Kafka for the workflow: possible, weaker fit for commands, retry, and dead-letter.
- HTTP between services: couples availability; a payments timeout would fail the customer's POST.
