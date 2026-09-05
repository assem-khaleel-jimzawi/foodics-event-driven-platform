# FoodFlow runbook

Exact steps to clone, build, test, run, and inspect the platform. Commands below were executed on Ubuntu 24.04 with .NET SDK **10.0.400** (`global.json`), Docker **29.8.0**, and Compose **v5.5.1**.

On this laptop the Docker socket is reachable as the `docker` group:

```bash
sg docker -c 'docker compose ps'
```

If `docker ps` already works for your user, drop the `sg docker -c '...'` wrapper and run the inner command.

## 1. Prerequisites

- Git
- .NET SDK 10 matching `global.json` (10.0.400 or a compatible feature band)
- Docker Engine + Docker Compose v2/v5
- Ports free: 3000, 3200, 5101–5106, 5433–5437, 5672, 9090, 9092, 15672

If `dotnet --version` is 8.x:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$HOME/.dotnet/tools:$PATH"
```

Do not install RabbitMQ or Kafka on the host. Compose provides them.

If an old local Postgres helper is bound to 5433/5434:

```bash
./scripts/stop-local-postgres.sh
```

## 2. Clone and enter the repository

```bash
git clone https://github.com/assem-khaleel-jimzawi/foodics-event-driven-platform.git
cd foodics-event-driven-platform
```

On the development machine this repo already lives at `~/Desktop/Foodics-event-driven`.

## 3. Verify .NET

```bash
dotnet --version
```

Expected: `10.0.400` (or another 10.0 SDK that roll-forward allows).

## 4. Verify Docker

```bash
docker --version
docker compose version
```

## 5. Restore and build

```bash
dotnet restore
dotnet build
```

Expected: 0 warnings, 0 errors.

## 6. Test

```bash
dotnet test
```

Domain tests are in-process. Integration tests start Testcontainers (PostgreSQL, RabbitMQ, Kafka). They need a working Docker socket.

Expected: 23 passed, 0 failed, 0 skipped (7 Orders domain + 4 Catalog domain + 3 Inventory domain + 9 integration).

## 7. Validate Compose, then start the platform

```bash
docker compose config
docker compose up --build -d
docker compose ps
```

Wait until application containers are `(healthy)`. First start after a volume wipe can take several minutes (Kafka health, image publish).

## 8. Health

```bash
curl -sS http://localhost:5101/health/live
curl -sS http://localhost:5101/health/ready
curl -sS http://localhost:5102/health/live
curl -sS http://localhost:5103/health/ready
curl -sS http://localhost:5104/health/ready
curl -sS http://localhost:5105/health/ready
curl -sS http://localhost:5106/health/live
```

`/health/live` is process-up (`self` only). `/health/ready` includes PostgreSQL and, where MassTransit is used, the bus.

## 9. Open the UIs

| What | URL | Notes |
| --- | --- | --- |
| Orders | http://localhost:5101 | `deliveryPhase: 3` on `GET /` |
| Catalog | http://localhost:5102 | |
| Inventory | http://localhost:5103 | |
| Payments | http://localhost:5104 | |
| Notifications | http://localhost:5105 | |
| Analytics | http://localhost:5106/api/analytics/snapshot | |
| RabbitMQ | http://localhost:15672 | user/pass `foodflow` / `foodflow` (local demo) |
| Prometheus | http://localhost:9090 | Status → Targets |
| Grafana | http://localhost:3000 | same local login; dashboard **FoodFlow overview** |
| Tempo | http://localhost:3200 | queried by Grafana; search API `/api/search` |

Kafka has no extra UI. Inspect with the broker CLI (next section).

## 10. Generate a successful order

```bash
curl -sS -X POST http://localhost:5101/api/orders \
  -H 'Content-Type: application/json' \
  -H 'X-Correlation-Id: demo-001' \
  -d '{
        "restaurantId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        "customerId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        "currency":"SAR",
        "items":[{"productId":"cccccccc-cccc-cccc-cccc-cccccccccccc","productName":"Chicken Kabsa","quantity":1,"unitPrice":32.50}]
      }'
```

Copy `id`, then poll:

```bash
curl -sS http://localhost:5101/api/orders/<order-id>
curl -sS http://localhost:5104/api/payments/by-order/<order-id>
curl -sS http://localhost:5103/api/inventory/stock/cccccccc-cccc-cccc-cccc-cccccccccccc
curl -sS http://localhost:5105/api/notifications/by-order/<order-id>
curl -sS http://localhost:5106/api/analytics/snapshot
```

Expect order `Confirmed`, payment `Completed`, reserved stock ≥ 1, a `order_confirmed` notification, analytics `ordersConfirmed` ≥ 1.

## 11. Observe RabbitMQ

In the management UI: Queues `reserve-inventory`, `inventory-reserved`, `payment-completed`, `payment-failed`, notification queues, and `reserve-inventory_error`.

Consumers should be present while services are healthy.

## 12. Observe Kafka

```bash
docker exec foodflow-kafka kafka-topics.sh --bootstrap-server 127.0.0.1:9092 --list
docker exec foodflow-kafka kafka-consumer-groups.sh --bootstrap-server 127.0.0.1:9092 --group foodflow-analytics --describe
```

Expect topics `foodflow.orders` and `foodflow.payments`. `LAG` is `0` when Analytics is caught up. A `-` LAG on an unused partition is normal.

## 13. Observe traces

Grafana → Explore → Tempo, or:

```bash
curl -sS 'http://localhost:3200/api/search?limit=20'
```

JSON logs include `TraceId`, `SpanId`, `CorrelationId`:

```bash
docker compose logs orders --tail=50
```

## 14. Observe metrics

```bash
curl -sS http://localhost:5101/metrics | head
curl -sS 'http://localhost:9090/api/v1/query?query=orders_created_total'
curl -sS 'http://localhost:9090/api/v1/targets'
```

Grafana folder **FoodFlow**, dashboard **FoodFlow overview**.

## 15. Inspect logs

```bash
docker compose logs --tail=100 orders inventory payments notifications analytics
```

## 16. Failure scenarios

**Inventory failure** (no charge):

```bash
# product 11111111-1111-1111-1111-111111111111
```

Use the same POST body with that `productId`. Order becomes `Cancelled`. `GET /api/payments/by-order/{id}` is 404.

**Payment failure + compensation** (customer `22222222-2222-2222-2222-222222222222`):

Order becomes `Cancelled`, payment `Failed`, reserved quantity returns to the previous value.

**Transient retry** (product `33333333-3333-3333-3333-333333333333`): order still `Confirmed`.

**Poison / error queue** (product `44444444-4444-4444-4444-444444444444`): order stays `Created`; RabbitMQ queue `reserve-inventory_error` receives the message. This path does **not** publish `InventoryReservationFailed`.

**Duplicate / idempotency:** covered by `dotnet test` (`Duplicate_reserve_command_does_not_double_reserve`) against real RabbitMQ/PostgreSQL.

## 17. Stop

```bash
docker compose down
```

Volumes are kept (Postgres/Kafka data). To wipe demo data:

```bash
docker compose down -v
```

That only removes Compose named volumes for this project.

## 18. Restart from a stopped state

```bash
docker compose up --build -d
```

Place another successful order (step 10) to confirm recovery.

## Demo identifiers

| Role | Id |
| --- | --- |
| Restaurant | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` |
| Happy-path customer | `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb` |
| In-stock product | `cccccccc-cccc-cccc-cccc-cccccccccccc` |
| Out of stock | `11111111-1111-1111-1111-111111111111` |
| Payment decline customer | `22222222-2222-2222-2222-222222222222` |
| Transient inventory fault | `33333333-3333-3333-3333-333333333333` |
| Poison product | `44444444-4444-4444-4444-444444444444` |

Local credentials `foodflow` / `foodflow` are **development defaults**, not production secrets. See [security.md](security.md).
