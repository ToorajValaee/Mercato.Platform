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
CONFIG_BASE_URL="${MERCATO_RUNTIME_BASE_URL:-http://127.0.0.1:5099}"
CONFIG_BEARER_TOKEN="${MERCATO_RUNTIME_BEARER_TOKEN:-runtime-token}"
DEFAULT_BRANCH_ID="${MERCATO_RUNTIME_DEFAULT_BRANCH_ID:-11111111-1111-1111-1111-111111111111}"
PRODUCT_ID="${MERCATO_RUNTIME_PRODUCT_ID:-33333333-3333-3333-3333-333333333333}"
PRODUCT_SKU="${MERCATO_RUNTIME_PRODUCT_SKU:-MERCATO-RUNTIME-001}"
PRODUCT_NAME="${MERCATO_RUNTIME_PRODUCT_NAME:-Mercato Runtime Product}"
PRODUCT_PRICE="${MERCATO_RUNTIME_PRODUCT_PRICE:-42.50}"
PRODUCT_STOCK="${MERCATO_RUNTIME_PRODUCT_STOCK:-17}"
CATEGORY_ID="${MERCATO_RUNTIME_CATEGORY_ID:-55555555-5555-5555-5555-555555555555}"
CATEGORY_NAME="${MERCATO_RUNTIME_CATEGORY_NAME:-Runtime Category}"
UPDATED_NAME="${MERCATO_RUNTIME_UPDATED_PRODUCT_NAME:-Mercato Runtime Product Updated}"
UPDATED_PRICE="${MERCATO_RUNTIME_UPDATED_PRODUCT_PRICE:-51.75}"
UPDATED_STOCK="${MERCATO_RUNTIME_UPDATED_PRODUCT_STOCK:-9}"
UPDATED_CATEGORY_NAME="${MERCATO_RUNTIME_UPDATED_CATEGORY_NAME:-Runtime Category Updated}"
WORK_DIR="${RUNNER_TEMP:-/tmp}/mercato-nopcommerce-runtime-sync"
LOG_FILE="$WORK_DIR/nopcommerce.log"
STUB_LOG_FILE="$WORK_DIR/mercato-stub.log"
TASK_RESULT="$WORK_DIR/task-result.txt"

if [[ ! -f "$NOP_WEB/Nop.Web.csproj" ]]; then
  echo "Nop.Web.csproj was not found under $NOP_WEB" >&2
  exit 2
fi
if [[ ! -f "$NOP_WEB/App_Data/plugins.json" ]]; then
  echo "nopCommerce plugin state was not found; run runtime-install-smoke.sh first." >&2
  exit 2
fi

mkdir -p "$WORK_DIR"
rm -f "$LOG_FILE" "$STUB_LOG_FILE" "$TASK_RESULT"
NOP_PID=""
STUB_PID=""
PG_CONTAINER="${NOP_POSTGRES_CONTAINER:-}"
STUB_PRODUCT_NAME="$PRODUCT_NAME"
STUB_PRODUCT_PRICE="$PRODUCT_PRICE"
STUB_PRODUCT_STOCK="$PRODUCT_STOCK"
STUB_CATEGORY_NAME="$CATEGORY_NAME"

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
  MERCATO_RUNTIME_CATEGORY_ID="$CATEGORY_ID" \
  MERCATO_RUNTIME_CATEGORY_NAME="$STUB_CATEGORY_NAME" \
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
    status="$(curl -sS -o /dev/null -w '%{http_code}' --max-time 5 "$BASE_URL/" || true)"
    if [[ "$status" =~ ^(200|301|302|303|307|308)$ ]]; then
      return 0
    fi
    sleep 2
  done
  echo "Timed out waiting for nopCommerce." >&2
  show_log
  return 1
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
  local task_type
  task_type="$(pg_scalar "SELECT \"Type\" FROM \"ScheduleTask\" WHERE \"Name\"='${task_name//\'/\'\'}' ORDER BY \"Id\" LIMIT 1;")"
  if [[ -z "$task_type" ]]; then
    echo "Schedule task '$task_name' was not installed." >&2
    show_log
    exit 1
  fi

  # nopCommerce's public scheduler endpoint intentionally enforces one run per period.
  # Make this installed task due before each acceptance invocation, then execute through
  # the same /scheduletask/runtask endpoint used by nopCommerce's own scheduler.
  pg_scalar "UPDATE \"ScheduleTask\" SET \"LastStartUtc\" = NOW() - (GREATEST(\"Seconds\", 1) + 5) * INTERVAL '1 second', \"LastEndUtc\" = NOW() - INTERVAL '1 second' WHERE \"Type\"='${task_type//\'/\'\'}';" >/dev/null

  curl -fsS --max-time 120 \
    -X POST "$BASE_URL/scheduletask/runtask" \
    --data-urlencode "taskType=$task_type" \
    -o "$TASK_RESULT"

  local success
  success="$(pg_scalar "SELECT CASE WHEN \"LastSuccessUtc\" IS NULL THEN '' ELSE \"LastSuccessUtc\"::text END FROM \"ScheduleTask\" WHERE \"Type\"='${task_type//\'/\'\'}' LIMIT 1;")"
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

assert_product_mapping() {
  local product_db_id="$1"
  local mapping
  mapping="$(pg_scalar "SELECT \"Value\" FROM \"GenericAttribute\" WHERE \"EntityId\"=$product_db_id AND \"KeyGroup\"='Product' AND \"Key\"='Mercato.ProductId' ORDER BY \"Id\" DESC LIMIT 1;")"
  if [[ "${mapping,,}" != "${PRODUCT_ID,,}" ]]; then
    echo "Expected Mercato.ProductId mapping $PRODUCT_ID for nop product $product_db_id, got '$mapping'." >&2
    exit 1
  fi
}

assert_category_state() {
  local product_db_id="$1"
  local expected_name="$2"
  local category_db_id
  category_db_id="$(pg_scalar "SELECT c.\"Id\" FROM \"Category\" c JOIN \"GenericAttribute\" ga ON ga.\"EntityId\"=c.\"Id\" AND ga.\"KeyGroup\"='Category' AND ga.\"Key\"='Mercato.CategoryId' WHERE lower(ga.\"Value\")=lower('${CATEGORY_ID//\'/\'\'}') ORDER BY c.\"Id\" LIMIT 1;")"
  if [[ -z "$category_db_id" ]]; then
    echo "Product sync did not create a nop category mapped to Mercato category $CATEGORY_ID." >&2
    exit 1
  fi

  local category_name
  category_name="$(pg_scalar "SELECT \"Name\" FROM \"Category\" WHERE \"Id\"=$category_db_id;")"
  if [[ "$category_name" != "$expected_name" ]]; then
    echo "Expected synchronized category name '$expected_name', got '$category_name'." >&2
    exit 1
  fi

  local mapping_count
  mapping_count="$(pg_scalar "SELECT COUNT(*) FROM \"Product_Category_Mapping\" WHERE \"ProductId\"=$product_db_id AND \"CategoryId\"=$category_db_id;")"
  if [[ "$mapping_count" != "1" ]]; then
    echo "Expected one product/category mapping for product $product_db_id and category $category_db_id, found $mapping_count." >&2
    exit 1
  fi

  local category_count
  category_count="$(pg_scalar "SELECT COUNT(*) FROM \"GenericAttribute\" WHERE \"KeyGroup\"='Category' AND \"Key\"='Mercato.CategoryId' AND lower(\"Value\")=lower('${CATEGORY_ID//\'/\'\'}');")"
  if [[ "$category_count" != "1" ]]; then
    echo "Expected one Mercato category identity mapping for $CATEGORY_ID, found $category_count." >&2
    exit 1
  fi
}

start_nop
wait_for_nop
start_stub
find_postgres_container

run_task "Mercato product synchronization"
PRODUCT_DB_ID="$(assert_product_state "$PRODUCT_NAME" "$PRODUCT_PRICE" "0" | tail -n 1)"
assert_product_mapping "$PRODUCT_DB_ID"
assert_category_state "$PRODUCT_DB_ID" "$CATEGORY_NAME"
grep -q 'GET /api/catalog HTTP' "$STUB_LOG_FILE"

run_task "Mercato inventory synchronization"
assert_product_state "$PRODUCT_NAME" "$PRODUCT_PRICE" "$PRODUCT_STOCK" >/dev/null
grep -q "GET /api/catalog?branchId=$DEFAULT_BRANCH_ID HTTP" "$STUB_LOG_FILE"

stop_stub
STUB_PRODUCT_NAME="$UPDATED_NAME"
STUB_PRODUCT_PRICE="$UPDATED_PRICE"
STUB_PRODUCT_STOCK="$UPDATED_STOCK"
STUB_CATEGORY_NAME="$UPDATED_CATEGORY_NAME"
start_stub
run_task "Mercato product synchronization"
run_task "Mercato inventory synchronization"
UPDATED_DB_ID="$(assert_product_state "$UPDATED_NAME" "$UPDATED_PRICE" "$UPDATED_STOCK" | tail -n 1)"
assert_product_mapping "$UPDATED_DB_ID"
assert_category_state "$UPDATED_DB_ID" "$UPDATED_CATEGORY_NAME"

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

echo "nopCommerce ProductSync category sync and InventorySync runtime smoke test passed."
