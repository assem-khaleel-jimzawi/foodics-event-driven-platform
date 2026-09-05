# Phase 1 Compose lives at the repository root so `docker compose up -d` works.

# Preferred:
#   docker compose up -d
#
# This starts OrdersDb (localhost:5433) and CatalogDb (localhost:5434).
# Application containers, RabbitMQ, Kafka, and observability arrive in later phases.
