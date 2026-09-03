#!/usr/bin/env bash
set -euo pipefail

NOP_ROOT="${NOP_ROOT:-${1:-}}"
if [[ -z "$NOP_ROOT" ]]; then
  echo "NOP_ROOT (or first argument) must point to an installed nopCommerce 4.90.7 checkout." >&2
  exit 2
fi

NOP_WEB="$NOP_ROOT/src/Presentation/Nop.Web"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BASE_URL="${NOP_BASE_URL:-http://127.0.0.1:5080}"
ADMIN_EMAIL="${NOP_ADMIN_EMAIL:-admin@mercato.local}"
ADMIN_PASSWORD="${NOP_ADMIN_PASSWORD:-MercatoRuntime123!}"
CONFIG_BASE_URL="${MERCATO_RUNTIME_BASE_URL:-http://127.0.0.1:5099}"
CONFIG_BEARER_TOKEN="${MERCATO_RUNTIME_BEARER_TOKEN:-runtime-token}"
DEFAULT_BRANCH_ID="${MERCATO_RUNTIME_DEFAULT_BRANCH_ID:-11111111-1111-1111-1111-111111111111}"
PRODUCT_ID="${MERCATO_RUNTIME_PRODUCT_ID:-33333333-3333-3333-3333-333333333333}"
PRODUCT_SKU="${MERCATO_RUNTIME_PRODUCT_SKU:-MERCATO-RUNTIME-001}"
PRODUCT_NAME="${MERCATO_RUNTIME_PRODUCT_NAME:-Mercato Runtime Product}"
PRODUCT_PRICE="${MERCATO_RUNTIME_PRODUCT_PRICE:-42.50}"
PRODUCT_STOCK="${MERCATO_RUNTIME_PRODUCT_STOCK:-17}"
UPDATED_NAME="${MERCATO_RUNTIME_UPDATED_PRODUCT_NAME:-Mercato Runtime Product Updated}"
UPDATED_PRICE="${MERCATO_RUNTIME_UPDATED_PRODUCT_PRICE:-51.75}"
UPDATED_STOCK="${MERCATO_RUNTIME_UPDATED_PRODUCT_STOCK:-9}"
WORK_DIR="${RUNNER_TEMP:-/tmp}/mercato-nopcommerce-runtime-sync"
COOKIE_JAR="$WORK_DIR/cookies.txt"
LOGIN_PAGE="$WORK_DIR/login.html"
LOGIN_RESULT="$WORK_DIR/login-result.html"
TASK_RESULT="$WORK_DIR/task-result.html"
LOG_FILE="$WORK_DIR/nopcommerce.log"
STUB_LOG_FILE="$WORK_DIR/mercato-stub.log"

if [[ ! -f "$NOP_WEB/Nop.Web.csproj" ]]; then
  echo "Nop.Web.csproj was not found under $NOP_WEB" >&2
  exit 2
fi
if [[ ! -f "$NOP_WEB/App_Data/plugins.json" ]]; then
  echo "nopCommerce plugin state was not found; run runtime-install-smoke.sh first." >&2
  exit 2
fi

mkdir -p "$WORK_DIR"
rm -f "$COOKIE_JAR" "$LOGIN_PAGE" "$LOGIN_RESULT" "$TASK_RESULT" "$LOG_FILE" "$STUB_LOG_FILE"
NOP_PID=""
STUB_PID=""
PG_CONTAINER="${NOP_POSTGRES_CONTAINER:-}"
STUB_PRODUCT_NAME="$PRODUCT_NAME"
STUB_PRODUCT_PRICE="$PRODUCT_PRICE"
STUB_PRODUCT_STOCK="$PRODUCT_STOCK"

show_log() {
  if [[ -f "$LOG_FILE" ]]; then
    echo "----- nopCommerce runtime log -----" >&2
    tail -n 250 "$LOG_FILE" >&2 || true
    echo "-----------------------------------" >&2
  fi
  if [[ -f "$STUB_LOG_FILE" ]]; then
    echo "----- Mercato runtime stub log ----" >&2
    tail -n 150 "$STUB_LOG_FILE" >&2 || true
    echo "-----------------------------------" >&2
  fi
}

stop_nop() {
  if [[ -n "${NOP_PID:-}" ]] && kill -0 "$NOP_PID" 2>/dev/null; then
    kill "$NOP_PID" 2>/dev/null || true
    wait "$NOP_PID" 2>/dev/null || true
  fi
  NOP_PID=""
}

stop_stub() {
  if [[ -n "${STUB_PID:-}" ]] && kill -0 "$STUB_PID" 2>/dev/null; then
    kill "$STUB_PID" 2>/dev/null || true
    wait "$STUB_PID" 2>/dev/null || true
  fi
  STUB_PID=""
}

cleanup() {
  stop_nop
  stop_stub
}
trap cleanup EXIT

start_nop() {
  : > "$LOG_FILE"
  (
    cd "$NOP_WEB"
    ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS="$BASE_URL" \
      dotnet run --project Nop.Web.csproj --configuration Release --no-build
  ) >"$LOG_FILE" 2>&1 &
  NOP_PID=$!
}

start_stub() {
  : > "$STUB_LOG_FILE"
  MERCATO_RUNTIME_BEARER_TOKEN="$CONFIG_BEARER_TOKEN" \
  MERCATO_RUNTIME_DEFAULT_BRANCH_ID="$DEFAULT_BRANCH_ID" \
  MERCATO_RUNTIME_PRODUCT_ID="$PRODUCT_ID" \
  MERCATO_RUNTIME_PRODUCT_SKU="$PRODUCT_SKU" \
  MERCATO_RUNTIME_PRODUCT_NAME="$STUB_PRODUCT_NAME" \
  MERCATO_RUNTIME_PRODUCT_PRICE="$STUB_PRODUCT_PRICE" \
  MERCATO_RUNTIME_PRODUCT_STOCK="$STUB_PRODUCT_STOCK" \
    python3 "$SCRIPT_DIR/runtime-mercato-stub.py" >"$STUB_LOG_FILE" 2>&1 &
  STUB_PID=$!

  for _ in {1..30}; do
    if ! kill -0 "$STUB_PID" 2>/dev/null; then
      echo "Mercato runtime stub exited before becoming ready." >&2
      show_log
      return 1
    fi
    if curl -fsS --max-time 2 "$CONFIG_BASE_URL/health" >/dev/null 2>&1; then
      return 0
    fi
    sleep 0.5
  done
  echo "Timed out waiting for Mercato runtime stub." >&2
  show_log
  return 1
}

wait_for_nop() {
  for _ in {1..120}; do
    if ! kill -0 "$NOP_PID" 2>/dev/null; then
      echo "nopCommerce exited before becoming ready." >&2
      show_log
      return 1
    fi
    local status
    status="$(curl -sS -o /dev/null -w '%{http_code}' --max-time 5 "$BASE_URL/login" || true)"
    if [[ "$status" =~ ^(200|301|302|303|307|308)$ ]]; then
      return 0
    fi
    sleep 2
  done
  echo "Timed out waiting for nopCommerce." >&2
  show_log
  return 1
}

extract_input_value() {
  python3 - "$1" "$2" <<'PY'
from html.parser import HTMLParser
import sys
page, field = sys.argv[1:3]
class Parser(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.value = None
    def handle_starttag(self, tag, attrs):
        if self.value is not None or tag.lower() != "input":
            return
        values = dict(attrs)
        if values.get("name") == field:
            self.value = values.get("value", "")
p = Parser()
with open(page, encoding="utf-8-sig") as stream:
    p.feed(stream.read())
if p.value is None:
    raise SystemExit(f"Unable to locate input {field!r} in {page}")
print(p.value)
PY
}

find_postgres_container() {
  if [[ -n "$PG_CONTAINER" ]]; then
    return 0
  fi
  PG_CONTAINER="$(docker ps --filter 'ancestor=postgres:16' --format '{{.ID}}' | head -n 1)"
  if [[ -z "$PG_CONTAINER" ]]; then
    echo "Unable to locate the nopCommerce PostgreSQL service container." >&2
    return 1
  fi
}

pg_scalar() {
  docker exec "$PG_CONTAINER" psql -v ON_ERROR_STOP=1 -U postgres -d nopcommerce -Atqc "$1"
}

run_task() {
  local task_name="$1"
  local task_id
  task_id="$(pg_scalar "SELECT \"Id\" FROM \"ScheduleTask\" WHERE \"Name\"='${task_name//\'/\'\'}' ORDER BY \"Id\" LIMIT 1;")"
  if [[ -z "$task_id" ]]; then
    echo "Schedule task '$task_name' was not installed." >&2
    show_log
    exit 1
  fi

  curl -fsS -L --max-time 120 \
    -b "$COOKIE_JAR" -c "$COOKIE_JAR" \
    "$BASE_URL/Admin/ScheduleTask/RunNow/$task_id" \
    -o "$TASK_RESULT"

  local success
  success="$(pg_scalar "SELECT CASE WHEN \"LastSuccessUtc\" IS NULL THEN '' ELSE \"LastSuccessUtc\"::text END FROM \"ScheduleTask\" WHERE \"Id\"=$task_id;")"
  if [[ -z "$success" ]]; then
    echo "Schedule task '$task_name' did not record a successful execution." >&2
    cat "$TASK_RESULT" >&2 || true
    show_log
    exit 1
  fi
}

assert_product_state() {
  local expected_name="$1"
  local expected_price="$2"
  local expected_stock="$3"
  local row
  row="$(pg_scalar "SELECT \"Id\"::text || '|' || \"Name\" || '|' || COALESCE(\"Sku\", '') || '|' || \"Price\"::text || '|' || \"StockQuantity\"::text || '|' || \"Published\"::text FROM \"Product\" WHERE \"Sku\"='${PRODUCT_SKU//\'/\'\'}' ORDER BY \"Id\" LIMIT 1;")"
  if [[ -z "$row" ]]; then
    echo "Product sync did not create SKU $PRODUCT_SKU." >&2
    show_log
    exit 1
  fi

  python3 - "$row" "$expected_name" "$PRODUCT_SKU" "$expected_price" "$expected_stock" <<'PY'
from decimal import Decimal
import sys
row, expected_name, expected_sku, expected_price, expected_stock = sys.argv[1:]
parts = row.split('|')
if len(parts) != 6:
    raise SystemExit(f"Unexpected product row: {row!r}")
product_id, name, sku, price, stock, published = parts
assert product_id.isdigit(), product_id
assert name == expected_name, (name, expected_name)
assert sku == expected_sku, (sku, expected_sku)
assert Decimal(price) == Decimal(expected_price), (price, expected_price)
assert int(stock) == int(expected_stock), (stock, expected_stock)
assert published.lower() in ('t', 'true'), published
print(product_id)
PY
}

assert_mapping() {
  local product_db_id="$1"
  local mapping
  mapping="$(pg_scalar "SELECT \"Value\" FROM \"GenericAttribute\" WHERE \"EntityId\"=$product_db_id AND \"KeyGroup\"='Product' AND \"Key\"='Mercato.ProductId' ORDER BY \"Id\" DESC LIMIT 1;")"
  if [[ "${mapping,,}" != "${PRODUCT_ID,,}" ]]; then
    echo "Expected Mercato.ProductId mapping $PRODUCT_ID for nop product $product_db_id, got '$mapping'." >&2
    exit 1
  fi
}

start_nop
wait_for_nop
start_stub
find_postgres_container

LOGIN_URL="$BASE_URL/login?returnUrl=%2FAdmin%2FScheduleTask%2FList"
curl -fsS -c "$COOKIE_JAR" "$LOGIN_URL" -o "$LOGIN_PAGE"
LOGIN_TOKEN="$(extract_input_value "$LOGIN_PAGE" "__RequestVerificationToken")"
LOGIN_STATUS="$(curl -sS -L --max-time 60 \
  -b "$COOKIE_JAR" -c "$COOKIE_JAR" \
  -o "$LOGIN_RESULT" -w '%{http_code}' \
  "$LOGIN_URL" \
  --data-urlencode "__RequestVerificationToken=$LOGIN_TOKEN" \
  --data-urlencode "Email=$ADMIN_EMAIL" \
  --data-urlencode "Password=$ADMIN_PASSWORD" \
  --data-urlencode "RememberMe=true")"
if [[ "$LOGIN_STATUS" != "200" ]]; then
  echo "nopCommerce administrator login failed for synchronization smoke (HTTP $LOGIN_STATUS)." >&2
  show_log
  exit 1
fi

# Product sync must create a concrete nop product and persist the Mercato identity mapping.
run_task "Mercato product synchronization"
PRODUCT_DB_ID="$(assert_product_state "$PRODUCT_NAME" "$PRODUCT_PRICE" "0" | tail -n 1)"
assert_mapping "$PRODUCT_DB_ID"
grep -q 'GET /api/catalog HTTP' "$STUB_LOG_FILE"

# Inventory sync must apply the authoritative default-branch stock snapshot.
run_task "Mercato inventory synchronization"
assert_product_state "$PRODUCT_NAME" "$PRODUCT_PRICE" "$PRODUCT_STOCK" >/dev/null
grep -q "GET /api/catalog?branchId=$DEFAULT_BRANCH_ID HTTP" "$STUB_LOG_FILE"

# Re-run both tasks against changed Mercato values to prove upsert/update behavior without duplicates.
stop_stub
STUB_PRODUCT_NAME="$UPDATED_NAME"
STUB_PRODUCT_PRICE="$UPDATED_PRICE"
STUB_PRODUCT_STOCK="$UPDATED_STOCK"
start_stub
run_task "Mercato product synchronization"
run_task "Mercato inventory synchronization"
UPDATED_DB_ID="$(assert_product_state "$UPDATED_NAME" "$UPDATED_PRICE" "$UPDATED_STOCK" | tail -n 1)"
assert_mapping "$UPDATED_DB_ID"

PRODUCT_COUNT="$(pg_scalar "SELECT COUNT(*) FROM \"Product\" WHERE \"Sku\"='${PRODUCT_SKU//\'/\'\'}';")"
if [[ "$PRODUCT_COUNT" != "1" ]]; then
  echo "Expected exactly one nop product for synchronized SKU $PRODUCT_SKU, found $PRODUCT_COUNT." >&2
  exit 1
fi
if [[ "$UPDATED_DB_ID" != "$PRODUCT_DB_ID" ]]; then
  echo "Product synchronization created a new nop product instead of updating $PRODUCT_DB_ID (got $UPDATED_DB_ID)." >&2
  exit 1
fi

if grep -Eiq 'Unhandled exception|ReflectionTypeLoadException|Could not load file or assembly' "$LOG_FILE"; then
  echo "nopCommerce runtime log contains a plugin/runtime loading failure." >&2
  show_log
  exit 1
fi

echo "nopCommerce ProductSync and InventorySync runtime smoke test passed."
