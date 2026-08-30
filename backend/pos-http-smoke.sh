#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${MERCATO_POS_BASE_URL:-http://127.0.0.1:5081}"
ADMIN_EMAIL="${MERCATO_POS_ADMIN_EMAIL:-admin@mercato.local}"
ADMIN_PASSWORD="${MERCATO_POS_ADMIN_PASSWORD:-MercatoLocal123!}"
WORK_DIR="${RUNNER_TEMP:-/tmp}/mercato-pos-http"
mkdir -p "$WORK_DIR"; rm -f "$WORK_DIR"/*.json "$WORK_DIR"/*.html "$WORK_DIR"/*.js 2>/dev/null || true

for _ in {1..90}; do curl -fsS --max-time 3 "$BASE_URL/health" >/dev/null 2>&1 && break; sleep 1; done
curl -fsS --max-time 10 "$BASE_URL/health" >/dev/null
curl -fsS "$BASE_URL/" -o "$WORK_DIR/home.html"; grep -q '<title>Mercato</title>' "$WORK_DIR/home.html"
curl -fsS "$BASE_URL/pos/" -o "$WORK_DIR/pos.html"; grep -q '<title>Mercato POS</title>' "$WORK_DIR/pos.html"; grep -q 'loginLanguage' "$WORK_DIR/pos.html"; grep -q '/api/branches/accessible' "$WORK_DIR/pos.html"
curl -fsS "$BASE_URL/admin/" -o "$WORK_DIR/admin.html"; grep -q '<title>Mercato Back Office</title>' "$WORK_DIR/admin.html"; grep -q 'salesChart' "$WORK_DIR/admin.html"; grep -q 'staffBranches' "$WORK_DIR/admin.html"; grep -q 'pager' "$WORK_DIR/admin.html"
curl -fsS "$BASE_URL/mercato-ui.js" -o "$WORK_DIR/ui.js"; grep -q 'fa-IR-u-ca-persian' "$WORK_DIR/ui.js"; grep -q 'jalaliToLocalDateTime' "$WORK_DIR/ui.js"

curl -fsS -H 'Content-Type: application/json' -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}" "$BASE_URL/api/auth/login" -o "$WORK_DIR/login.json"
TOKEN="$(python3 - "$WORK_DIR/login.json" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); assert d['user']['role']=='Admin'; assert d['token']; print(d['token'])
PY
)"; AUTH=(-H "Authorization: Bearer $TOKEN")

curl -fsS "${AUTH[@]}" "$BASE_URL/api/branches" -o "$WORK_DIR/branches.json"
BRANCH_ID="$(python3 - "$WORK_DIR/branches.json" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); assert d; print(d[0]['id'])
PY
)"

# Root category creation used to fail because null parent and null current id were treated as equal.
CATEGORY_NAME="Runtime Category $RANDOM"
curl -fsS "${AUTH[@]}" -H 'Content-Type: application/json' -d "{\"name\":\"$CATEGORY_NAME\",\"parentCategoryId\":null}" "$BASE_URL/api/categories" -o "$WORK_DIR/category.json"
CATEGORY_ID="$(python3 - "$WORK_DIR/category.json" "$CATEGORY_NAME" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); assert d['name']==sys.argv[2]; assert d.get('parentCategoryId') is None; print(d['id'])
PY
)"
curl -fsS "${AUTH[@]}" -X DELETE "$BASE_URL/api/categories/$CATEGORY_ID" >/dev/null

# Staff branch assignment must persist and drive the POS-accessible branch list.
STAFF_EMAIL="runtime-cashier-${RANDOM}@mercato.local"; STAFF_PASSWORD='RuntimeCashier123!'
curl -fsS "${AUTH[@]}" -H 'Content-Type: application/json' -d "{\"email\":\"$STAFF_EMAIL\",\"password\":\"$STAFF_PASSWORD\",\"role\":\"Cashier\",\"branchIds\":[\"$BRANCH_ID\"]}" "$BASE_URL/api/staff" -o "$WORK_DIR/staff.json"
STAFF_ID="$(python3 - "$WORK_DIR/staff.json" "$BRANCH_ID" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); assert d['role']=='Cashier'; assert sys.argv[2] in d['branchIds']; print(d['id'])
PY
)"
curl -fsS -H 'Content-Type: application/json' -d "{\"email\":\"$STAFF_EMAIL\",\"password\":\"$STAFF_PASSWORD\"}" "$BASE_URL/api/auth/login" -o "$WORK_DIR/staff-login.json"
STAFF_TOKEN="$(python3 - "$WORK_DIR/staff-login.json" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); assert d['user']['role']=='Cashier'; print(d['token'])
PY
)"
curl -fsS -H "Authorization: Bearer $STAFF_TOKEN" "$BASE_URL/api/branches/accessible" -o "$WORK_DIR/staff-branches.json"
python3 - "$WORK_DIR/staff-branches.json" "$BRANCH_ID" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); assert [x['id'] for x in d]==[sys.argv[2]], d
PY

curl -fsS "${AUTH[@]}" "$BASE_URL/api/catalog?branchId=$BRANCH_ID" -o "$WORK_DIR/catalog-before.json"
read -r PRODUCT_ID INITIAL_STOCK < <(python3 - "$WORK_DIR/catalog-before.json" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); p=next((x for x in d if int(x.get('availableQuantity') or 0)>=2),None); assert p; print(p['productId'],p['availableQuantity'])
PY
)

IDEMPOTENCY_KEY="pos-smoke-$(date +%s)-$RANDOM"; REQUEST_JSON="{\"branchId\":\"$BRANCH_ID\",\"customerId\":\"00000000-0000-0000-0000-000000000000\",\"paymentMethod\":\"Cash\",\"idempotencyKey\":\"$IDEMPOTENCY_KEY\",\"items\":[{\"productId\":\"$PRODUCT_ID\",\"quantity\":2}]}"
curl -fsS "${AUTH[@]}" -H 'Content-Type: application/json' -d "$REQUEST_JSON" "$BASE_URL/api/pos/checkout" -o "$WORK_DIR/checkout.json"
ORDER_ID="$(python3 - "$WORK_DIR/checkout.json" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); assert d['status']=='Completed'; assert d['items'][0]['quantity']==2; print(d['orderId'])
PY
)"
curl -fsS "${AUTH[@]}" -H 'Content-Type: application/json' -d "$REQUEST_JSON" "$BASE_URL/api/pos/checkout" -o "$WORK_DIR/replay.json"
python3 - "$WORK_DIR/checkout.json" "$WORK_DIR/replay.json" <<'PY'
import json,sys
a=json.load(open(sys.argv[1])); b=json.load(open(sys.argv[2])); assert (a['orderId'],a['invoiceId'],a['paymentId'])==(b['orderId'],b['invoiceId'],b['paymentId'])
PY
curl -fsS "${AUTH[@]}" "$BASE_URL/api/catalog?branchId=$BRANCH_ID" -o "$WORK_DIR/catalog-after-sale.json"
python3 - "$WORK_DIR/catalog-after-sale.json" "$PRODUCT_ID" "$INITIAL_STOCK" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); p=next(x for x in d if x['productId']==sys.argv[2]); assert p['availableQuantity']==int(sys.argv[3])-2
PY
curl -fsS "${AUTH[@]}" "$BASE_URL/api/pos/orders/$ORDER_ID" -o "$WORK_DIR/order.json"
python3 - "$WORK_DIR/order.json" "$PRODUCT_ID" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); x=next(x for x in d['items'] if x['productId']==sys.argv[2]); assert (x['soldQuantity'],x['returnedQuantity'],x['returnableQuantity'])==(2,0,2)
PY
curl -fsS "${AUTH[@]}" -H 'Content-Type: application/json' -d "{\"orderId\":\"$ORDER_ID\",\"refundMethod\":\"Cash\",\"items\":[{\"productId\":\"$PRODUCT_ID\",\"quantity\":1}]}" "$BASE_URL/api/pos/returns" -o "$WORK_DIR/return.json"
python3 - "$WORK_DIR/return.json" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); assert d['total']>0 and d['reference'].startswith('RET-')
PY
curl -fsS "${AUTH[@]}" "$BASE_URL/api/pos/orders/$ORDER_ID" -o "$WORK_DIR/order-after-return.json"
python3 - "$WORK_DIR/order-after-return.json" "$PRODUCT_ID" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); x=next(x for x in d['items'] if x['productId']==sys.argv[2]); assert (x['soldQuantity'],x['returnedQuantity'],x['returnableQuantity'])==(2,1,1)
PY
curl -fsS "${AUTH[@]}" "$BASE_URL/api/catalog?branchId=$BRANCH_ID" -o "$WORK_DIR/catalog-after-return.json"
python3 - "$WORK_DIR/catalog-after-return.json" "$PRODUCT_ID" "$INITIAL_STOCK" <<'PY'
import json,sys
d=json.load(open(sys.argv[1])); p=next(x for x in d if x['productId']==sys.argv[2]); assert p['availableQuantity']==int(sys.argv[3])-1
PY
curl -fsS "${AUTH[@]}" -X DELETE "$BASE_URL/api/staff/$STAFF_ID" >/dev/null

echo "Mercato runtime smoke passed: bilingual UI assets, category creation, staff branch assignment, POS sale/replay/return, and stock reconciliation."
