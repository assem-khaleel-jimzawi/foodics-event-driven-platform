# System walkthrough

What happens when a customer creates an order, explained as you would to a Foodics interviewer. This matches the code in this repository.

## What happens when a customer creates an order?

The client calls **Orders HTTP**, not Inventory and not Payments. Catalog is not queried during checkout. Line items already carry a product name and unit price snapshot.

```text
POST /api/orders
  → Orders API validates the body
  → OrdersDb transaction:
        insert order (status Created)
        insert outbox: OrderCreated (RabbitMQ + Kafka)
        insert outbox: ReserveInventory (RabbitMQ)
  → 201 Created (workflow is not finished)
```

`X-Correlation-Id` is echoed. OpenTelemetry records the HTTP span. The outbox row stores W3C `TraceParent` because the HTTP span ends when the response is sent.

## Outbox → RabbitMQ → Inventory

`OutboxProcessor` polls unpublished rows, restores the trace, and publishes. MassTransit puts `ReserveInventory` on the `reserve-inventory` queue.

**Inventory** claims an inbox row keyed by `(consumer, EventId)`, locks stock in **InventoryDb only**, and:

- on success: outbox `InventoryReserved` (RabbitMQ + Kafka)
- on insufficient stock: outbox `InventoryReservationFailed` (no payment)
- on poison product `4444…`: throws `PermanentMessagingException` → `reserve-inventory_error` (order stays Created)

Inventory never reads OrdersDb.

## InventoryReserved → Payments

Payments consumes **`InventoryReserved`**, not `ProcessPayment`. `ProcessPayment` exists as a contract only so a second charge path cannot appear by accident.

Payments claims inbox, enforces unique `Payment.OrderId`, calls `FakePaymentGateway`, writes **PaymentsDb**, outbox `PaymentCompleted` or `PaymentFailed`.

## Payment result → Orders → Notifications

Orders consumes `PaymentCompleted` → status **Confirmed**, outbox `OrderConfirmed`.

Orders consumes `PaymentFailed` → status **Cancelled**, outbox `OrderCancelled` + `ReleaseInventory`.

Inventory consumes `ReleaseInventory` and returns reserved quantity. That is **compensation**, not a retry of the decline.

Notifications consume `OrderConfirmed` / `OrderCancelled` / `PaymentFailed` and write simulated messages to **NotificationsDb**. They do not send real email.

## Kafka → Analytics

The same local transaction that wrote business rows may also insert Kafka outbox rows for selected facts. Analytics is consumer group `foodflow-analytics` on `foodflow.orders` and `foodflow.payments`. It does not query operational databases. The snapshot is an in-memory projection; Kafka is the replay source.

If Kafka is down, checkout still completes on RabbitMQ. Kafka outbox rows retry.

If RabbitMQ is down, HTTP still commits. Outbox rows stay unpublished until the broker returns.

## OpenTelemetry → Tempo / Prometheus / Grafana

One checkout is one `TraceId` with many `SpanId`s (HTTP, SQL, outbox publish, MassTransit consume, Kafka consume). `CorrelationId` is the business string on HTTP and contracts. They are not the same thing.

`/metrics` is scraped by Prometheus. Grafana shows **FoodFlow overview**. Tempo stores traces when `OpenTelemetry__OtlpEndpoint` is set (`http://tempo:4317` in Compose).

## Responsibility of every service

| Service | Database | Does | Does not |
| --- | --- | --- | --- |
| Orders | OrdersDb | Accept orders, confirm/cancel from facts | Call Catalog, charge cards, hold stock |
| Catalog | CatalogDb | Products over HTTP | Participate in checkout |
| Inventory | InventoryDb | Reserve/release, oversell protection | Charge, confirm orders |
| Payments | PaymentsDb | One fake charge per order | Read stock tables |
| Notifications | NotificationsDb | Record simulated messages | Drive the saga |
| Analytics | none (Kafka group) | Project counts | Block checkout |

There is no orchestrator process and no shared SQL transaction. The saga is choreography plus compensation.
