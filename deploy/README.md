# Compose

```bash
docker compose up --build -d
docker compose ps
docker compose down
```

Phase 3 starts PostgreSQL (orders, catalog, inventory, payments, notifications), RabbitMQ, Kafka (KRaft), application images, Prometheus (`9090`), Tempo (`3200`), and Grafana (`3000`).

Host ports are listed in the root README. Kafka from the host is `localhost:9092`. Containers use `kafka:9094`.

Grafana login: `foodflow` / `foodflow`. Dashboard folder **FoodFlow**. These credentials are local development defaults, not production secrets.
