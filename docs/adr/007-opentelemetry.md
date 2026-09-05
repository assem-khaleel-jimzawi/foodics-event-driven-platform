# ADR 007: OpenTelemetry for traces and metrics

## Status

Accepted (Phase 3)

## Context

Phase 2 made the order workflow correct under failure. Phase 3 must make a failed checkout **debuggable across processes** without attaching a debugger to six containers. Logs with a correlation id are necessary but not sufficient: they do not show parent/child timing or SQL vs messaging cost.

## Decision

Use the OpenTelemetry .NET SDK from `FoodFlow.ServiceDefaults`:

- One `ActivitySource` / `Meter` named `FoodFlow` for business and outbox/Kafka spans.
- Library instrumentation: ASP.NET Core, HttpClient, Npgsql, MassTransit.
- Metrics scraped by Prometheus from `/metrics`.
- Traces exported with OTLP gRPC when an endpoint is configured (Tempo in Compose).
- Persist `traceparent` on outbox rows so the background publisher continues the HTTP trace.

Prometheus + Grafana are the metrics UI. Tempo is the trace store. Elasticsearch, Jaeger all-in-one, and a second APM vendor are out of scope.

## Consequences

- A checkout can be found by `TraceId` in Grafana/Tempo and by `X-Correlation-Id` in logs.
- Scraping `/metrics` does not require a sidecar.
- If Tempo is down, applications still serve traffic; spans are dropped by the exporter.
- If Prometheus is down, `/metrics` still exists; history is lost.

## Alternatives considered

- Application Insights only: ties the demo to Azure.
- Serilog sinks as the only “tracing”: not a span tree.
- MassTransit Prometheus addon plus a second metrics stack: duplicate the OTel meter.
