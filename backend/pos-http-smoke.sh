#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${MERCATO_POS_BASE_URL:-http://127.0.0.1:5081}"
ADMIN_EMAIL="${MERCATO_POS_ADMIN_EMAIL:-admin@mercato.local}"
ADMIN_PASSWORD="${MERCATO_POS_ADMIN_PASSWORD:-MercatoLocal123!}"
WORK_DIR="${RUNNER_TEMP:-/tmp}/mercato-pos-http"
LOGIN_JSON="$WORK_DIR/login.json"
BRANCHES_JSON="$WORK_DIR/branches.json"
CATALOG_BEFORE="$WORK_DIR/catalog-before.json"
CATALOG_AFTER_SALE="$WORK_DIR/catalog-after-sale.json"
CATALOG_AFTER_RETURN="$WORK_DIR/catalog-after-return.json"
CHECKOUT_JSON="$WORK_DIR/checkout.json"
RETURN_JSON="$WORK_DIR/return.json"
POS_HTML="$WORK_DIR/pos.html"
ADMIN_HTML="$WORK_DIR/admin.html"
HOME_HTML="$WORK_DIR/home.html"

mkdir -p "$WORK_DIR"
rm -f "$LOGIN_JSON" "$BRANCHES_JSON" "$CATALOG_BEFORE" "$CATALOG_AFTER_SALE" \
  "$CATALOG_AFTER_RETURN" "$CHECKOUT_JSON" "$RETURN_JSON" "$POS_HTML" "$ADMIN_HTML" "$HOME_HTML"

for _ in {1..90}; do
  if curl -fsS --max-time 3 "$BASE_URL/health" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done
curl -fsS --max-time 10 "$BASE_URL/health" >/dev/null

curl -fsS "$BASE_URL/" -o "$HOME_HTML"
grep -q '<title>Mercato</title>' "$HOME_HTML"
grep -q '/admin/' "$HOME_HTML"
grep -q '/pos/' "$HOME_HTML"

curl -fsS "$BASE_URL/pos/" -o "$POS_HTML"
grep -q '<title>Mercato POS</title>' "$POS_HTML"

curl -fsS "$BASE_URL/admin/" -o "$ADMIN_HTML"
grep -q '<title>Mercato Back Office</title>' "$ADMIN_HTML"
grep -q 'Artist settlements' "$ADMIN_HTML"
grep -q 'Staff accounts' "$ADMIN_HTML"

curl -fsS \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}" \
  "$BASE_URL/api/auth/login" -o "$LOGIN_JSON"

TOKEN="$(python3 - "$LOGIN_JSON" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f:
    data=json.load(f)
assert data['user']['role'] == 'Admin', data
assert data['token'], data
print(data['token'])
PY
)"
AUTH=(-H "Authorization: Bearer $TOKEN")

# Back-office staff administration must be a real Admin-only workflow, not a static screen.
STAFF_EMAIL="runtime-cashier-${RANDOM}@mercato.local"
STAFF_JSON="$WORK_DIR/staff.json"
curl -fsS "${AUTH[@]}" "$BASE_URL/api/staff" -o "$WORK_DIR/staff-before.json"
curl -fsS "${AUTH[@]}" -H 'Content-Type: application/json' \
  -d "{\"email\":\"$STAFF_EMAIL\",\"password\":\"RuntimeCashier123!\",\"role\":\"Cashier\"}" \
  "$BASE_URL/api/staff" -o "$STAFF_JSON"
STAFF_ID="$(python3 - "$STAFF_JSON" "$STAFF_EMAIL" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: data=json.load(f)
assert data['email'] == sys.argv[2], data
assert data['role'] == 'Cashier', data
print(data['id'])
PY
)"
curl -fsS "${AUTH[@]}" -H 'Content-Type: application/json' -X PUT \
  -d '{"role":"Manager","password":null}' "$BASE_URL/api/staff/$STAFF_ID" -o "$WORK_DIR/staff-updated.json"
python3 - "$WORK_DIR/staff-updated.json" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: data=json.load(f)
assert data['role'] == 'Manager', data
PY
curl -fsS "${AUTH[@]}" -X DELETE "$BASE_URL/api/staff/$STAFF_ID" >/dev/null

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
p=next((x for x in data if int(x.get('availableQuantity') or 0) >= 2), None)
assert p is not None, data
print(p['productId'], p['availableQuantity'])
PY
)

IDEMPOTENCY_KEY="pos-smoke-$(date +%s)-$RANDOM"
REQUEST_JSON="{\"branchId\":\"$BRANCH_ID\",\"customerId\":\"00000000-0000-0000-0000-000000000000\",\"paymentMethod\":\"Cash\",\"idempotencyKey\":\"$IDEMPOTENCY_KEY\",\"items\":[{\"productId\":\"$PRODUCT_ID\",\"quantity\":2}]}"

curl -fsS \
  "${AUTH[@]}" \
  -H 'Content-Type: application/json' \
  -d "$REQUEST_JSON" \
  "$BASE_URL/api/pos/checkout" -o "$CHECKOUT_JSON"

ORDER_ID="$(python3 - "$CHECKOUT_JSON" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: data=json.load(f)
assert data['status'] == 'Completed', data
assert len(data['items']) == 1, data
assert data['items'][0]['quantity'] == 2, data
assert data['total'] > 0, data
print(data['orderId'])
PY
)"

REPLAY_JSON="$WORK_DIR/replay.json"
curl -fsS \
  "${AUTH[@]}" \
  -H 'Content-Type: application/json' \
  -d "$REQUEST_JSON" \
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
PY

curl -fsS "${AUTH[@]}" "$BASE_URL/api/catalog?branchId=$BRANCH_ID" -o "$CATALOG_AFTER_RETURN"
python3 - "$CATALOG_AFTER_RETURN" "$PRODUCT_ID" "$INITIAL_STOCK" <<'PY'
import json, sys
with open(sys.argv[1], encoding='utf-8') as f: data=json.load(f)
p=next(x for x in data if x['productId'] == sys.argv[2])
assert p['availableQuantity'] == int(sys.argv[3]) - 1, p
PY

echo "Mercato runtime smoke passed: landing page, back office, staff lifecycle, POS, JWT login, sale, idempotent replay, partial return, and stock reconciliation."
