# Integration events

FoodFlow splits integration contracts from EF entities. Records live in `FoodFlow.Contracts`. A service may persist its own `Order` or `StockItem`; it must not publish that class to another service.

Every message has:

| Field | Role |
| --- | --- |
| `EventId` | Idempotency key. Inbox rows are unique on `(consumer, EventId)`. |
| `CorrelationId` | Joins one customer request across services. Copied from `X-Correlation-Id` when an order is created. |
| `OccurredAtUtc` | Business time, always UTC. |

Commands name an action (`ReserveInventory`, `ReleaseInventory`). Events name a fact (`InventoryReserved`). Orders writes `OrderCreated` (fact, also copied to Kafka) and `ReserveInventory` (command) in the same local transaction. Inventory consumes the command, not the fact, so replaying the analytics stream cannot reserve stock again.

## Operational workflow (RabbitMQ)

```text
POST /api/orders
  → Orders transaction: Order row + outbox OrderCreated
  → outbox publisher → RabbitMQ OrderCreated
  → Inventory: reserve or fail
  → InventoryReserved | InventoryReservationFailed
  → Payments consumes InventoryReserved only
  → PaymentCompleted | PaymentFailed
  → Orders confirms or cancels
  → OrderConfirmed | OrderCancelled (+ ReleaseInventory on payment failure)
  → Notifications writes a simulated channel log
```

## Compensation

Payment failure is not a distributed SQL transaction. Inventory already reserved stock. Orders consumes `PaymentFailed`, cancels the order, and enqueues `ReleaseInventory` in the same local transaction. Inventory releases the reservation. That is choreography, not a coordinator saga.

## Analytics copies (Kafka)

The same business transaction that writes a RabbitMQ outbox row also writes a Kafka outbox row for:

- `OrderCreated`, `OrderConfirmed`, `OrderCancelled`
- `PaymentCompleted`, `PaymentFailed`

Inventory reservation events stay on RabbitMQ. They are operational, not an analytics stream.

## Demo identifiers

Defined in `DemoScenarios` so failure paths do not need a real warehouse or PSP:

| Id | Purpose |
| --- | --- |
| `cccccccc-...` | In-stock product |
| `11111111-...` | Out of stock |
| `33333333-...` | Transient consumer failure, then success |
| `44444444-...` | Permanent consumer failure → `_error` queue |
| `22222222-...` | Fake payment decline |
