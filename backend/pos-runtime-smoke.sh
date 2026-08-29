#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${MERCATO_POS_BASE_URL:-http://127.0.0.1:5081}"
DB_CONNECTION="${MERCATO_POS_SMOKE_DB_CONNECTION:-Host=127.0.0.1;Port=5432;Database=mercato_pos_smoke;Username=postgres;Password=postgres}"
ADMIN_EMAIL="${MERCATO_POS_ADMIN_EMAIL:-admin@mercato.local}"
ADMIN_PASSWORD="${MERCATO_POS_ADMIN_PASSWORD:-MercatoLocal123!}"
JWT_KEY="${MERCATO_POS_JWT_KEY:-Mercato-POS-Smoke-JWT-Key-Change-Me-2026}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
API_PROJECT="$ROOT_DIR/backend/src/Mercato.Api/Mercato.Api.csproj"
WORK_DIR="${RUNNER_TEMP:-/tmp}/mercato-pos-runtime"
LOG_FILE="$WORK_DIR/api.log"
LOGIN_JSON="$WORK_DIR/login.json"
BRANCHES_JSON="$WORK_DIR/branches.json"
CATALOG_BEFORE="$WORK_DIR/catalog-before.json"
CATALOG_AFTER_SALE="$WORK_DIR/catalog-after-sale.json"
CATALOG_AFTER_RETURN="$WORK_DIR/catalog-after-return.json"
CHECKOUT_JSON="$WORK_DIR/checkout.json"
RETURN_JSON="$WORK_DIR/return.json"
POS_HTML="$WORK_DIR/pos.html"

mkdir -p "$WORK_DIR"
rm -f "$LOG_FILE" "$LOGIN_JSON" "$BRANCHES_JSON" "$CATALOG_BEFORE" "$CATALOG_AFTER_SALE" \
  "$CATALOG_AFTER_RETURN" "$CHECKOUT_JSON" "$RETURN_JSON" "$POS_HTML"

API_PID=""
cleanup() {
  if [[ -n "${API_PID:-}" ]] && kill -0 "$API_PID" 2>/dev/null; then
    kill "$API_PID" 2>/dev/null || true
    wait "$API_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT

(
  cd "$ROOT_DIR"
  ASPNETCORE_ENVIRONMENT=Development \
  ASPNETCORE_URLS="$BASE_URL" \
  MERCATO_CONNECTION_STRING="$DB_CONNECTION" \
  Jwt__Issuer="Mercato.Api" \
  Jwt__Audience="Mercato.Client" \
  Jwt__Key="$JWT_KEY" \
  BootstrapAdmin__Email="$ADMIN_EMAIL" \
  BootstrapAdmin__Password="$ADMIN_PASSWORD" \
  BootstrapDemoData__Enabled=true \
    dotnet run --project "$API_PROJECT" --configuration Release --no-build
) >"$LOG_FILE" 2>&1 &
API_PID=$!

for _ in {1..90}; do
  if ! kill -0 "$API_PID" 2>/dev/null; then
    echo "Mercato API exited before becoming ready." >&2
    cat "$LOG_FILE" >&2 || true
    exit 1
  fi
  if curl -fsS --max-time 3 "$BASE_URL/health" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done
curl -fsS --max-time 10 "$BASE_URL/health" >/dev/null

curl -fsS "$BASE_URL/pos/" -o "$POS_HTML"
grep -q '<title>Mercato POS</title>' "$POS_HTML"

curl -fsS \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}" \
  "$BASE_URL/api/auth/login" -o "$LOGIN_JSON"

TOKEN="$(python3 - "$LOGIN_JSON" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f:
    data=json.load(f)
assert data['user']['role'] == 'Admin', data
print(data['token'])
PY
)"
AUTH=(-H "Authorization: Bearer $TOKEN")

curl -fsS "${AUTH[@]}" "$BASE_URL/api/branches" -o "$BRANCHES_JSON"
BRANCH_ID="$(python3 - "$BRANCHES_JSON" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: data=json.load(f)
assert data, 'No branches returned'
print(data[0]['id'])
PY
)"

curl -fsS "${AUTH[@]}" "$BASE_URL/api/catalog?branchId=$BRANCH_ID" -o "$CATALOG_BEFORE"
read -r PRODUCT_ID INITIAL_STOCK < <(python3 - "$CATALOG_BEFORE" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: data=json.load(f)
assert data, 'No catalog products returned'
p=data[0]
assert p['availableQuantity'] >= 2, p
print(p['productId'], p['availableQuantity'])
PY
)

IDEMPOTENCY_KEY="pos-smoke-$(date +%s)-$RANDOM"
curl -fsS \
  "${AUTH[@]}" \
  -H 'Content-Type: application/json' \
  -d "{\"branchId\":\"$BRANCH_ID\",\"customerId\":\"00000000-0000-0000-0000-000000000000\",\"paymentMethod\":\"Cash\",\"idempotencyKey\":\"$IDEMPOTENCY_KEY\",\"items\":[{\"productId\":\"$PRODUCT_ID\",\"quantity\":2}]}" \
  "$BASE_URL/api/pos/checkout" -o "$CHECKOUT_JSON"

ORDER_ID="$(python3 - "$CHECKOUT_JSON" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: data=json.load(f)
assert data['status'] == 'Completed', data
assert len(data['items']) == 1, data
assert data['items'][0]['quantity'] == 2, data
print(data['orderId'])
PY
)"

# Replaying the same idempotency key must return the same order and make no second deduction.
REPLAY_JSON="$WORK_DIR/replay.json"
curl -fsS \
  "${AUTH[@]}" \
  -H 'Content-Type: application/json' \
  -d "{\"branchId\":\"$BRANCH_ID\",\"customerId\":\"00000000-0000-0000-0000-000000000000\",\"paymentMethod\":\"Cash\",\"idempotencyKey\":\"$IDEMPOTENCY_KEY\",\"items\":[{\"productId\":\"$PRODUCT_ID\",\"quantity\":2}]}" \
  "$BASE_URL/api/pos/checkout" -o "$REPLAY_JSON"
python3 - "$CHECKOUT_JSON" "$REPLAY_JSON" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: first=json.load(f)
with open(sys.argv[2], encoding='utf-8') as f: replay=json.load(f)
assert replay['orderId'] == first['orderId'], (first, replay)
assert replay['invoiceId'] == first['invoiceId'], (first, replay)
assert replay['paymentId'] == first['paymentId'], (first, replay)
PY

curl -fsS "${AUTH[@]}" "$BASE_URL/api/catalog?branchId=$BRANCH_ID" -o "$CATALOG_AFTER_SALE"
python3 - "$CATALOG_AFTER_SALE" "$PRODUCT_ID" "$INITIAL_STOCK" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: data=json.load(f)
p=next(x for x in data if x['productId'] == sys.argv[2])
assert p['availableQuantity'] == int(sys.argv[3]) - 2, p
PY

curl -fsS "${AUTH[@]}" "$BASE_URL/api/pos/orders/$ORDER_ID" -o "$WORK_DIR/order.json"
python3 - "$WORK_DIR/order.json" "$PRODUCT_ID" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: data=json.load(f)
line=next(x for x in data['items'] if x['productId'] == sys.argv[2])
assert line['soldQuantity'] == 2, line
assert line['returnedQuantity'] == 0, line
assert line['returnableQuantity'] == 2, line
PY

curl -fsS \
  "${AUTH[@]}" \
  -H 'Content-Type: application/json' \
  -d "{\"orderId\":\"$ORDER_ID\",\"refundMethod\":\"Cash\",\"items\":[{\"productId\":\"$PRODUCT_ID\",\"quantity\":1}]}" \
  "$BASE_URL/api/pos/returns" -o "$RETURN_JSON"
python3 - "$RETURN_JSON" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: data=json.load(f)
assert data['total'] > 0, data
assert data['reference'].startswith('RET-'), data
PY

curl -fsS "${AUTH[@]}" "$BASE_URL/api/pos/orders/$ORDER_ID" -o "$WORK_DIR/order-after-return.json"
python3 - "$WORK_DIR/order-after-return.json" "$PRODUCT_ID" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: data=json.load(f)
line=next(x for x in data['items'] if x['productId'] == sys.argv[2])
assert line['soldQuantity'] == 2, line
assert line['returnedQuantity'] == 1, line
assert line['returnableQuantity'] == 1, line
assert line['quantity'] == 1, line
PY

curl -fsS "${AUTH[@]}" "$BASE_URL/api/catalog?branchId=$BRANCH_ID" -o "$CATALOG_AFTER_RETURN"
python3 - "$CATALOG_AFTER_RETURN" "$PRODUCT_ID" "$INITIAL_STOCK" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: data=json.load(f)
p=next(x for x in data if x['productId'] == sys.argv[2])
assert p['availableQuantity'] == int(sys.argv[3]) - 1, p
PY

if grep -Eiq 'Unhandled exception|crit:' "$LOG_FILE"; then
  echo "Mercato POS runtime log contains an unexpected failure." >&2
  cat "$LOG_FILE" >&2
  exit 1
fi

echo "Mercato POS runtime smoke passed: UI, JWT login, branch catalog, sale, idempotent replay, partial return, and stock reconciliation."
