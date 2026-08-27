#!/usr/bin/env bash
set -euo pipefail

ADMIN_EMAIL="${NOP_ADMIN_EMAIL:-admin@mercato.local}"
WORK_DIR="${RUNNER_TEMP:-/tmp}/mercato-nopcommerce-runtime"
COOKIE_JAR="$WORK_DIR/cookies.txt"

if [[ -f "$COOKIE_JAR" ]]; then
  echo "----- nopCommerce smoke cookie jar -----"
  # Cookie values are opaque auth/session material; print only metadata, never values.
  awk 'BEGIN { OFS="\t" }
       /^#/ && $0 !~ /^#HttpOnly_/ { next }
       NF >= 7 {
         domain=$1; sub(/^#HttpOnly_/, "", domain);
         print "domain=" domain, "path=" $3, "secure=" $4, "expires=" $5, "name=" $6
       }' "$COOKIE_JAR" || true
  echo "----------------------------------------"
else
  echo "No smoke-test cookie jar found at $COOKIE_JAR"
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is unavailable; skipping nopCommerce database diagnostics."
  exit 0
fi

PG_CONTAINER="$(docker ps --filter "ancestor=postgres:16" --format '{{.ID}}' | head -n 1)"
if [[ -z "$PG_CONTAINER" ]]; then
  echo "PostgreSQL service container not found; skipping database diagnostics."
  exit 0
fi

# psql variable substitution is reliable when the SQL is read from stdin (rather than -c).
echo "----- nopCommerce installer admin roles -----"
docker exec -i "$PG_CONTAINER" psql -U postgres -d nopcommerce -v ON_ERROR_STOP=1 -v admin_email="$ADMIN_EMAIL" -P pager=off <<'SQL'
SELECT c."Id" AS customer_id,
       c."Email" AS email,
       c."Active" AS active,
       c."Deleted" AS deleted,
       cr."Id" AS role_id,
       cr."Name" AS role_name,
       cr."SystemName" AS role_system_name,
       cr."Active" AS role_active
FROM "Customer" c
LEFT JOIN "Customer_CustomerRole_Mapping" m ON m."Customer_Id" = c."Id"
LEFT JOIN "CustomerRole" cr ON cr."Id" = m."CustomerRole_Id"
WHERE lower(c."Email") = lower(:'admin_email')
ORDER BY cr."Id";
SQL

echo "----- relevant nopCommerce permission mappings -----"
docker exec -i "$PG_CONTAINER" psql -U postgres -d nopcommerce -v ON_ERROR_STOP=1 -P pager=off <<'SQL'
SELECT pr."Id" AS permission_id,
       pr."SystemName" AS permission_system_name,
       cr."Id" AS role_id,
       cr."SystemName" AS role_system_name
FROM "PermissionRecord" pr
LEFT JOIN "PermissionRecord_Role_Mapping" m ON m."PermissionRecord_Id" = pr."Id"
LEFT JOIN "CustomerRole" cr ON cr."Id" = m."CustomerRole_Id"
WHERE pr."SystemName" IN (
  'AccessAdminPanel',
  'Security.AccessAdminPanel',
  'ManagePlugins',
  'Configuration.ManagePlugins',
  'ManageScheduleTasks',
  'System.ManageScheduleTasks',
  'Catalog.ProductsView',
  'Catalog.ProductsCreateEditDelete'
)
ORDER BY pr."SystemName", cr."Id";
SQL

echo "----- recent nopCommerce migration versions -----"
docker exec -i "$PG_CONTAINER" psql -U postgres -d nopcommerce -v ON_ERROR_STOP=1 -P pager=off <<'SQL'
SELECT "Version", "Description"
FROM "MigrationVersionInfo"
ORDER BY "Version" DESC
LIMIT 12;
SQL
