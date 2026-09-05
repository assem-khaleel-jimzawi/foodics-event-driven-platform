#!/usr/bin/env bash
set -euo pipefail

ROOT="${FOODFLOW_PG_ROOT:-$HOME/.local/foodflow-pg}"
DATA_ROOT="${FOODFLOW_PG_DATA:-$HOME/.local/foodflow-data}"
BIN="$ROOT/bin"

stop_instance() {
  local name="$1"
  local data_dir="$DATA_ROOT/$name"
  if [[ -d "$data_dir" ]] && "$BIN/pg_ctl" -D "$data_dir" status >/dev/null 2>&1; then
    "$BIN/pg_ctl" -D "$data_dir" stop -m fast
    echo "stopped $name"
  fi
}

stop_instance orders
stop_instance catalog
