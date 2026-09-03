#!/usr/bin/env bash
set -euo pipefail

PG_CONTAINER="${NOP_POSTGRES_CONTAINER:-}"
if [[ -z "$PG_CONTAINER" ]]; then
  PG_CONTAINER="$(docker ps --filter 'ancestor=postgres:16' --format '{{.ID}}' | head -n 1)"
fi
[[ -n "$PG_CONTAINER" ]] || { echo "Unable to locate nopCommerce PostgreSQL container." >&2; exit 1; }

# runtime-order-sync-smoke.sh deliberately uses BillingAddressId=0 for its synthetic
# order. A real nopCommerce Order requires that FK to resolve, so provide the
# minimal valid address fixture before creating the paid order.
docker exec "$PG_CONTAINER" psql -v ON_ERROR_STOP=1 -U postgres -d nopcommerce <<'SQL'
INSERT INTO "Address" (
  "Id", "FirstName", "LastName", "Email", "CreatedOnUtc"
)
SELECT 0, 'Mercato', 'Runtime', 'runtime-order@mercato.invalid', CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM "Address" WHERE "Id" = 0);
SQL

exec bash "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/runtime-order-sync-smoke.sh" "$@"
