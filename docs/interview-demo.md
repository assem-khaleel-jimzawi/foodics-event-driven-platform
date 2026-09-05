# Interview demonstration (10–15 minutes)

Use this live. Docker commands assume `docker compose` works; on this Ubuntu laptop wrap with `sg docker -c '...'`.

## 1. Architecture (1 min)

Open [README.md](../README.md) mermaid diagram or [system-walkthrough.md](system-walkthrough.md). Say: six services, database-per-service, RabbitMQ for work, Kafka for analytics, outbox+inbox, no distributed transaction.

## 2. Start the platform (if not already up)

```bash
docker compose up --build -d
docker compose ps
```

Wait for `(healthy)` on the six apps.

## 3. Health (30 s)

```bash
curl -sS http://localhost:5101/health/live
curl -sS http://localhost:5101/health/ready
curl -sS http://localhost:5101/
```

Point out: live = process; ready = Postgres + bus. `deliveryPhase` is 3.

## 4. Create a successful order (2 min)

```bash
curl -sS -X POST http://localhost:5101/api/orders \
  -H 'Content-Type: application/json' \
  -H 'X-Correlation-Id: interview-happy' \
  -d '{
        "restaurantId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        "customerId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        "currency":"SAR",
        "items":[{"productId":"cccccccc-cccc-cccc-cccc-cccccccccccc","productName":"Chicken Kabsa","quantity":1,"unitPrice":32.50}]
      }'
```

Poll until `Confirmed`:

```bash
curl -sS http://localhost:5101/api/orders/<id>
```

## 5. RabbitMQ (1 min)

http://localhost:15672 (`foodflow` / `foodflow`). Show `reserve-inventory`, `inventory-reserved`, `payment-completed`, consumers, message rates.

## 6. Inventory (30 s)

```bash
curl -sS http://localhost:5103/api/inventory/stock/cccccccc-cccc-cccc-cccc-cccccccccccc
```

`reserved` increased. InventoryDb only.

## 7. Payment (30 s)

```bash
curl -sS http://localhost:5104/api/payments/by-order/<id>
```

`Completed`. Unique `OrderId`. Fake gateway, not a real PSP.

## 8. Confirmed order + notification (30 s)

```bash
curl -sS http://localhost:5105/api/notifications/by-order/<id>
```

## 9. Kafka analytics (1 min)

```bash
curl -sS http://localhost:5106/api/analytics/snapshot
docker exec foodflow-kafka kafka-consumer-groups.sh --bootstrap-server 127.0.0.1:9092 --group foodflow-analytics --describe
```

Show `LAG` 0. Say why this is not a second RabbitMQ.

## 10. Prometheus (45 s)

http://localhost:9090 → Status → Targets (six UP). Query `orders_created_total`.

## 11. Grafana (45 s)

http://localhost:3000 → FoodFlow overview. Business counters after the order.

## 12. Distributed trace (1 min)

Grafana Explore → Tempo, or `curl -sS 'http://localhost:3200/api/search?limit=20'`. Logs: `TraceId` vs `X-Correlation-Id`.

## 13–14. Payment failure + compensation (2 min)

POST the same body with customer `22222222-2222-2222-2222-222222222222`. Poll until `Cancelled`. Payment `Failed`. Stock `reserved` back to the previous value. Explain: compensation (`ReleaseInventory`), not retry of a declined card.

Optional: out-of-stock product `11111111-...` → cancelled, payment 404.

## 15. Outbox / idempotency (1 min)

Explain: same Postgres transaction as the order row. Publisher is a hosted service. At-least-once + inbox `(consumer, EventId)` + unique payment `OrderId`. Duplicate test is in `dotnet test`.

Poison product `4444…` → `reserve-inventory_error`, order stays `Created` (document this honestly).

## 16. Production improvements (30 s)

Auth, secret store, HA brokers, backups, real payment/SMS, migrate jobs, no public admin ports. Not Kubernetes/Redis/Elasticsearch for this assignment.

Stop if you started Compose only for the demo:

```bash
docker compose down
```
