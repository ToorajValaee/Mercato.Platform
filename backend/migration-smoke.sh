#!/usr/bin/env bash
set -euo pipefail

PG_CONTAINER="${PG_CONTAINER:-$(docker ps --filter 'ancestor=postgres:16' --format '{{.ID}}' | head -n 1)}"
PG_USER="${PG_USER:-postgres}"
PG_HOST="${PG_HOST:-127.0.0.1}"
PG_PORT="${PG_PORT:-55432}"
PG_PASSWORD="${PG_PASSWORD:-postgres}"
FRESH_DB="${MERCATO_MIGRATION_FRESH_DB:-mercato_migration_fresh}"
EXISTING_DB="${MERCATO_MIGRATION_EXISTING_DB:-mercato_migration_existing}"
API_PORT="${MERCATO_MIGRATION_API_PORT:-5082}"
PROJECT="backend/src/Mercato.Infrastructure/Mercato.Infrastructure.csproj"
STARTUP_PROJECT="backend/src/Mercato.Api/Mercato.Api.csproj"

if [[ -z "${PG_CONTAINER}" ]]; then
  echo 'PostgreSQL container not found.' >&2
  exit 1
fi

psql_exec() {
  local db="$1"
  local sql="$2"
  docker exec "${PG_CONTAINER}" psql -U "${PG_USER}" -d "${db}" -v ON_ERROR_STOP=1 -Atqc "${sql}"
}

reset_database() {
  local db="$1"
  docker exec "${PG_CONTAINER}" psql -U "${PG_USER}" -d postgres -v ON_ERROR_STOP=1 -c "DROP DATABASE IF EXISTS \"${db}\";"
  docker exec "${PG_CONTAINER}" psql -U "${PG_USER}" -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE \"${db}\";"
}

connection_string() {
  local db="$1"
  printf 'Host=%s;Port=%s;Database=%s;Username=%s;Password=%s' "${PG_HOST}" "${PG_PORT}" "${db}" "${PG_USER}" "${PG_PASSWORD}"
}

apply_ef_migrations() {
  local db="$1"
  MERCATO_CONNECTION_STRING="$(connection_string "${db}")" \
    dotnet ef database update \
      --project "${PROJECT}" \
      --startup-project "${STARTUP_PROJECT}" \
      --no-build
}

assert_baseline_applied() {
  local db="$1"
  local count
  count="$(psql_exec "${db}" "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '%_InitialBaseline';")"
  if [[ "${count}" -lt 1 ]]; then
    echo "InitialBaseline was not recorded in ${db}." >&2
    exit 1
  fi
}

echo '== Fresh database migration =='
reset_database "${FRESH_DB}"
apply_ef_migrations "${FRESH_DB}"
assert_baseline_applied "${FRESH_DB}"
if [[ "$(psql_exec "${FRESH_DB}" "SELECT to_regclass('\"Products\"') IS NOT NULL;")" != 't' ]]; then
  echo 'Fresh migration did not create Products.' >&2
  exit 1
fi

echo '== Existing database compatibility bridge =='
reset_database "${EXISTING_DB}"
apply_ef_migrations "${EXISTING_DB}"
psql_exec "${EXISTING_DB}" "INSERT INTO \"ApplicationSettings\" (\"Key\", \"Value\") VALUES ('MigrationSmoke.Sentinel', 'preserve-me') ON CONFLICT (\"Key\") DO UPDATE SET \"Value\" = EXCLUDED.\"Value\";" >/dev/null
psql_exec "${EXISTING_DB}" 'DROP TABLE "__EFMigrationsHistory";' >/dev/null

API_LOG="$(mktemp)"
MERCATO_CONNECTION_STRING="$(connection_string "${EXISTING_DB}")" \
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS="http://127.0.0.1:${API_PORT}" \
Jwt__Issuer=Mercato.Api \
Jwt__Audience=Mercato.Client \
Jwt__Key='Mercato-Migration-Smoke-JWT-Key-Change-Me-2026' \
BootstrapDemoData__Enabled=false \
  dotnet run --project "${STARTUP_PROJECT}" --configuration Release --no-build >"${API_LOG}" 2>&1 &
API_PID=$!
cleanup() {
  local status=$?
  kill "${API_PID}" >/dev/null 2>&1 || true
  wait "${API_PID}" >/dev/null 2>&1 || true
  if [[ ${status} -ne 0 ]]; then
    cat "${API_LOG}" >&2 || true
  fi
  rm -f "${API_LOG}"
  exit ${status}
}
trap cleanup EXIT

for _ in $(seq 1 60); do
  if curl --fail --silent "http://127.0.0.1:${API_PORT}/health" >/dev/null; then
    break
  fi
  if ! kill -0 "${API_PID}" >/dev/null 2>&1; then
    echo 'API exited before the migration bridge completed.' >&2
    exit 1
  fi
  sleep 1
done
curl --fail --silent "http://127.0.0.1:${API_PORT}/health" >/dev/null

assert_baseline_applied "${EXISTING_DB}"
if [[ "$(psql_exec "${EXISTING_DB}" "SELECT \"Value\" FROM \"ApplicationSettings\" WHERE \"Key\" = 'MigrationSmoke.Sentinel';")" != 'preserve-me' ]]; then
  echo 'Existing database sentinel data was not preserved.' >&2
  exit 1
fi

if grep -Eiq 'Unhandled exception|crit:' "${API_LOG}"; then
  cat "${API_LOG}" >&2
  exit 1
fi

echo 'Fresh and existing database migration smoke tests passed.'
