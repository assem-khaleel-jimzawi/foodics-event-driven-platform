# ADR 002: PostgreSQL

## Status

Accepted (Phase 1)

## Context

The platform needs a relational store for orders, products, later reservations, payments, outbox rows, and inbox rows. Those are transactional records with constraints, not a document dump.

## Decision

Use PostgreSQL via Npgsql and EF Core.

Reasons:

- First-class .NET support (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- Strong transactional semantics for order totals, unique inbox message ids, and outbox publishing
- Runs the same on Linux developer machines and in Docker
- Familiar operational story for Foodics-style backends
- No Windows/SQL Server dependency

Phase 1 pins PostgreSQL 16 and EF Core / Npgsql 10.x on .NET 10.

## Consequences

- Money uses `numeric(18,2)` mapped from `decimal`.
- JSON/JSONB is available later if needed; Phase 1 does not require it.
- Developers need Docker or a local PostgreSQL. This repo documents both.

## Alternatives considered

- SQL Server: excellent EF support, weaker fit for this Linux/Docker assignment and unnecessary.
- MongoDB: poor default for reservations, payments, and exactly-once-ish inbox uniqueness.
- SQLite: fine for unit tests, not the target operational database.
