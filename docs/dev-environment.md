# Development environment

## Toolchain

| Tool | Notes |
| --- | --- |
| OS | Ubuntu 24.04 |
| .NET SDK | 10.0.400 at `~/.dotnet` (`global.json`) |
| Docker Engine | required for Compose and Testcontainers |
| Docker Compose | v5.x |

If `dotnet --version` prints 8.x:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"
```

Do not install RabbitMQ or Kafka on the host. Use Compose.

If Phase 1's optional local PostgreSQL helper is still bound to 5433/5434, stop it before Compose:

```bash
./scripts/stop-local-postgres.sh
```

## Local defaults

Development credentials are `foodflow` / `foodflow`. They are not production secrets.

Mapped ports: OrdersDb 5433, CatalogDb 5434, InventoryDb 5435, PaymentsDb 5436, NotificationsDb 5437, RabbitMQ 5672/15672, Kafka 9092.
