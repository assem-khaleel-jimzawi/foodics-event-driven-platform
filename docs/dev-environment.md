# Phase 1 development environment

Recorded on 2026-09-05 while bootstrapping FoodFlow.

## Toolchain

| Tool | Version / notes |
| --- | --- |
| OS | Ubuntu 24.04 (linux-x64) |
| Git | 2.43.0 |
| Git user | Assem Khaleel Jimzawi `<assem.cs.90@gmail.com>` |
| GitHub CLI | 2.100.0 installed to `~/.local/bin/gh` |
| GitHub auth | **Not logged in**. `gh auth status` failed. Remote repo was not created from this machine. |
| .NET SDK (user-local) | 10.0.400 at `~/.dotnet` |
| System apt SDK | 8.0.424 at `/usr/bin/dotnet` — do not use this for FoodFlow |
| Docker Engine | **Not installed**. `sudo` requires a password, so it could not be installed from the agent session. |
| PostgreSQL (verification) | 16.15 via micromamba prefix `~/.local/foodflow-pg` |

`global.json` pins SDK `10.0.400` with `latestFeature` roll-forward.

If `dotnet --version` prints 8.x, prepend the user-local SDK:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"
```

## Package baseline

| Package | Version | Why |
| --- | --- | --- |
| Target framework | `net10.0` | Current LTS for this assignment |
| Microsoft.EntityFrameworkCore | 10.0.11 | Latest stable EF Core 10 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | Latest stable Npgsql EF provider for .NET 10 |
| Microsoft.AspNetCore.OpenApi | 10.0.11 | Built-in OpenAPI document |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.11 | Readiness check against the service DbContext |
| xunit | 2.9.3 | Template-aligned test runner |
| FluentAssertions | 7.2.1 | Readable assertions; Apache-2.0, avoiding FluentAssertions 8 commercial license |
| dotnet-ef (local tool) | 10.0.11 | Migrations |

Central package management lives in `Directory.Packages.props`. No preview packages.

## Docker vs local PostgreSQL

Preferred: `docker compose up -d` using `postgres:16.15-alpine` on ports 5433 (orders) and 5434 (catalog).

Fallback used for Phase 1 verification on this machine: `scripts/start-local-postgres.sh`.

Local credentials in appsettings are **development defaults** (`foodflow` / `foodflow`) and must not be reused in production.
