# ADR 001: Database per service

## Status

Accepted (Phase 1)

## Context

FoodFlow is split into independently deployable services. The first two to persist data are Orders and Catalog. A shared database would let Orders join catalog tables and would look convenient in a demo.

## Decision

Each service that owns data gets its own PostgreSQL database. Phase 1 creates `OrdersDb` and `CatalogDb`. Later services will own `InventoryDb` and `PaymentsDb`. No service may query another service's schema.

Locally, that can be two databases in two containers (Compose) or two databases on two ports (the no-Docker script). Production can place them on separate instances without changing application code.

## Consequences

- Independent schema evolution. Catalog can add columns without an Orders migration.
- Independent failure. Catalog DB unavailability does not block order *reads* of already stored snapshots.
- Cross-service consistency becomes eventual and must be designed with messages, outbox, and compensation.
- Developers cannot write `SELECT FROM catalog.products` inside Orders. Product name/price on an order line is a snapshot.

## Alternatives considered

- One PostgreSQL instance, one database, multiple schemas: weaker isolation, easier accidental joins.
- One database shared by all services: fastest demo, worst boundary.
- SQL Server: rejected in ADR 002.
