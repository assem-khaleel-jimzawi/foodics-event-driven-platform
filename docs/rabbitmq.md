# RabbitMQ

RabbitMQ is the **operational** broker. It moves work that must happen for an order to complete: reserve stock, charge, confirm, notify, compensate.

It is not a system of record. PostgreSQL is. If RabbitMQ is down, the outbox keeps the message in the service's own database until the publisher can deliver it.

## Topology

MassTransit 8.5 declares queues from consumers. Endpoint names are kebab-case of the consumer type without the `Consumer` suffix (`ReserveInventoryConsumer` → `reserve-inventory`).

Each service has competing consumers on its own queues. Prefetch is 16. There is no shared "god" queue.

## Retry

`UseMessageRetry` runs three short intervals (200/400/800 ms) for exceptions **except** `PermanentMessagingException`, which is ignored by the retry filter and faults immediately.

After retries are exhausted, MassTransit moves the message to the `_error` queue for that endpoint (`reserve-inventory_error`). That queue is inspectable in the RabbitMQ management UI on port 15672.

Permanent **business** decisions (insufficient stock, fake card decline) are **not** thrown as consumer faults. They are published as failure events and the workflow compensates. Poison payloads (`PermanentMessagingException`) go to `_error`.

## Why not Kafka for this path

The order workflow needs:

- competing consumers
- per-message retry
- a dead-letter/error queue
- routing of commands and events to specific services

Kafka can be forced into that shape. RabbitMQ already is that shape. Kafka is used for the analytics log instead. See [kafka.md](kafka.md) and [ADR 003](adr/003-rabbitmq.md).
