# Kafka

Kafka is the **analytics stream**. It is not a second RabbitMQ.

Operational consumers never read Kafka to reserve stock or take payment. If they did, we would be duplicating the workflow on a log that is harder to dead-letter and harder to route as a command.

## Topics

| Topic | Producers (via outbox) | Consumer |
| --- | --- | --- |
| `foodflow.orders` | Orders (`OrderCreated`, `OrderConfirmed`, `OrderCancelled`) | Analytics worker, group `foodflow-analytics` |
| `foodflow.payments` | Payments (`PaymentCompleted`, `PaymentFailed`) | Same group |

Keys are `OrderId`. Records for one order therefore land on the same partition and stay ordered for that order. There is no global order across orders.

## Concepts this demo actually uses

| Concept | Where |
| --- | --- |
| Topic | `foodflow.orders`, `foodflow.payments` |
| Partition | Assigned by Kafka from the order-id key |
| Consumer | `KafkaAnalyticsConsumer` |
| Consumer group | `foodflow-analytics` |
| Offset | Committed by the worker after the in-memory snapshot is updated (`EnableAutoCommit = false`) |
| Retention | Broker config `KAFKA_CFG_LOG_RETENTION_HOURS=168` in Compose |
| Replay | Stop the worker, change nothing, start it: the group continues from the committed offset. To replay, use a new group id or reset offsets. |
| Ordering | Per `OrderId` / partition, not cluster-wide |
| Consumer lag | `latest offset - committed offset` on a partition. `kafka-consumer-groups.sh --describe` inside the Kafka container. |

## Incident checks

```bash
sg docker -c 'docker exec foodflow-kafka kafka-topics.sh --bootstrap-server 127.0.0.1:9092 --list'
sg docker -c 'docker exec foodflow-kafka kafka-consumer-groups.sh --bootstrap-server 127.0.0.1:9092 --group foodflow-analytics --describe'
```

Kafka records include `message-type` and `traceparent` headers so Analytics can continue the checkout trace.

## Produce path

Services do not call `IProducer` from the HTTP request. They write a Kafka-destination outbox row in the same PostgreSQL transaction as the business row. `OutboxProcessor` produces with `Acks=All` and idempotent producer settings.

## Consume path

The analytics worker applies each record to an in-memory snapshot (order counts, revenue, failures) and then commits the offset. Duplicate `EventId`s are ignored. Restart without resetting the group continues from the committed offset; the snapshot itself is a demo projection and rebuilds only from new records. Replay from earliest with a new group id to rebuild.

Topics are created at process start (`KafkaTopicProvisioner`) so the consumer does not wait on Kafka's default metadata refresh for a topic that did not exist at subscribe time.

See [ADR 004](adr/004-kafka.md).
