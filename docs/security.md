# Security notes

This is a **local interview demo**. It is not production-secure. The distinction matters in a Foodics discussion.

## DEMO / LOCAL

Development identity everywhere is `foodflow` / `foodflow`:

- PostgreSQL users and database passwords in Compose and `appsettings.json`
- RabbitMQ default user
- Grafana admin user
- `.env.example`

These values are **documented development defaults**. They are not production secrets, API keys, or cloud credentials. There are no JWT signing keys, AWS/Azure keys, GitHub tokens, or TLS private keys in the repository.

Compose and `appsettings*.json` contain those demo connection strings so `docker compose up` and `dotnet run` (against Compose-mapped ports / env overrides) work without a secret manager.

Health endpoints and `/metrics` are unauthenticated on the laptop. That is acceptable only because the ports are bound to localhost for a demo.

JSON logs include service name, correlation id, and W3C trace ids. They must not include passwords, tokens, or card data. The fake payment path stores amount, currency, and a short failure reason — not PAN/CVV (there is no real card).

Kafka in Compose is PLAINTEXT on the demo network. That is not a production Kafka security configuration.

Containers share one user-defined bridge network. Host ports are exposed for the interviewer, not because they should be on the public internet.

## PRODUCTION (what this repo does not claim)

- Secret manager or sealed secrets; no passwords in Git
- TLS everywhere; Kafka SASL/mTLS; RabbitMQ TLS
- Authentication and authorization on every API
- Restrict `/metrics` and admin UIs to an internal network
- Dedicated migration identity, not the app's table owner forever
- Log redaction policies reviewed by security
- Real PSP: never log provider tokens; tokenize cards off-box
- Network policies between services
- Backup/PITR and broker ACLs

## Environment variables

Compose injects `ConnectionStrings__*`, `RabbitMq__*`, `Kafka__BootstrapServers`, `OpenTelemetry__OtlpEndpoint`. That is the supported way to override `appsettings.json` without committing new secrets.

`.env` is gitignored. `.env.example` only repeats the documented demo user.

## Payment data

`FakePaymentGateway` declines a well-known demo customer id. There is no card number field on the API. Do not describe this as PCI-compliant.

## Health endpoints

`/health/live` does not depend on PostgreSQL, so a DB blip does not kill the container. `/health/ready` does. Neither is an authz boundary.

## Audit result (this repository)

A full-tree search found no production secrets. Tracked credentials are the demo pair above, clearly labeled in README, Compose comments via docs, and `.env.example`.
