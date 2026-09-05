#!/usr/bin/env bash
set -euo pipefail

# User-local PostgreSQL used when Docker Engine is not installed.
# Mirrors the Phase 1 compose topology: two databases, two ports.

ROOT="${FOODFLOW_PG_ROOT:-$HOME/.local/foodflow-pg}"
DATA_ROOT="${FOODFLOW_PG_DATA:-$HOME/.local/foodflow-data}"
BIN="$ROOT/bin"

if [[ ! -x "$BIN/postgres" ]]; then
  echo "PostgreSQL binaries not found at $BIN/postgres" >&2
  echo "Install with micromamba or use: docker compose up -d" >&2
  exit 1
fi

start_instance() {
  local name="$1"
  local port="$2"
  local database="$3"
  local data_dir="$DATA_ROOT/$name"
  local log_file="$data_dir/postgres.log"
  local sock_dir="$DATA_ROOT/sockets/$name"

  mkdir -p "$sock_dir"

  if [[ ! -f "$data_dir/PG_VERSION" ]]; then
    mkdir -p "$data_dir"
    echo foodflow > "$DATA_ROOT/${name}.pw"
    "$BIN/initdb" \
      --pgdata="$data_dir" \
      --username=foodflow \
      --auth=trust \
      --pwfile="$DATA_ROOT/${name}.pw" \
      --encoding=UTF8 \
      --locale=C
    rm -f "$DATA_ROOT/${name}.pw"
  fi

  if "$BIN/pg_ctl" -D "$data_dir" status >/dev/null 2>&1; then
    echo "$name already running"
  else
    "$BIN/pg_ctl" \
      -D "$data_dir" \
      -l "$log_file" \
      -o "-p $port -k $sock_dir" \
      start
  fi

  export PGHOST="$sock_dir"
  export PGPORT="$port"
  export PGUSER=foodflow

  for _ in {1..30}; do
    if "$BIN/pg_isready" -q; then
      break
    fi
    sleep 0.2
  done

  if ! "$BIN/psql" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$database'" | grep -q 1; then
    "$BIN/createdb" "$database"
  fi

  echo "$name ready on port $port database $database"
}

start_instance orders 5433 orders
start_instance catalog 5434 catalog
