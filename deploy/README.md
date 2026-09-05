# Compose

```bash
docker compose up --build -d
docker compose ps
docker compose down
```

Phase 2 starts PostgreSQL (orders, catalog, inventory, payments, notifications), RabbitMQ, Kafka (KRaft), and the application images.

Host ports are listed in the root README. Kafka from the host is `localhost:9092`. Containers use `kafka:9094`.
