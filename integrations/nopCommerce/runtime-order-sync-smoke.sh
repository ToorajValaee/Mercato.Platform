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
WORK_DIR="${RUNNER_TEMP:-/tmp}/mercato-nopcommerce-runtime-order-sync"
LOG_FILE="$WORK_DIR/nopcommerce.log"
STUB_LOG_FILE="$WORK_DIR/mercato-stub.log"
TASK_RESULT="$WORK_DIR/task-result.txt"
ORDER_MARKER="MERCATO-RUNTIME-ORDER-$(date +%s)-$$"

mkdir -p "$WORK_DIR"
rm -f "$LOG_FILE" "$STUB_LOG_FILE" "$TASK_RESULT"
NOP_PID=""
STUB_PID=""
PG_CONTAINER="${NOP_POSTGRES_CONTAINER:-}"

show_log() {
  [[ ! -f "$LOG_FILE" ]] || { echo "----- nopCommerce runtime log -----" >&2; tail -n 250 "$LOG_FILE" >&2 || true; }
  [[ ! -f "$STUB_LOG_FILE" ]] || { echo "----- Mercato runtime stub log ----" >&2; tail -n 200 "$STUB_LOG_FILE" >&2 || true; }
}

cleanup() {
  if [[ -n "${NOP_PID:-}" ]] && kill -0 "$NOP_PID" 2>/dev/null; then kill "$NOP_PID" 2>/dev/null || true; wait "$NOP_PID" 2>/dev/null || true; fi
  if [[ -n "${STUB_PID:-}" ]] && kill -0 "$STUB_PID" 2>/dev/null; then kill "$STUB_PID" 2>/dev/null || true; wait "$STUB_PID" 2>/dev/null || true; fi
}
trap cleanup EXIT

start_nop() {
  : > "$LOG_FILE"
  (cd "$NOP_WEB" && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$BASE_URL" dotnet run --project Nop.Web.csproj --configuration Release --no-build) >"$LOG_FILE" 2>&1 &
  NOP_PID=$!
  for _ in {1..120}; do
    kill -0 "$NOP_PID" 2>/dev/null || { show_log; return 1; }
    local status
    status="$(curl -sS -o /dev/null -w '%{http_code}' --max-time 5 "$BASE_URL/" || true)"
    [[ "$status" =~ ^(200|301|302|303|307|308)$ ]] && return 0
    sleep 2
  done
  show_log
  return 1
}

start_stub() {
  : > "$STUB_LOG_FILE"
  MERCATO_RUNTIME_BEARER_TOKEN="$CONFIG_BEARER_TOKEN" \
  MERCATO_RUNTIME_DEFAULT_BRANCH_ID="$DEFAULT_BRANCH_ID" \
  MERCATO_RUNTIME_PRODUCT_ID="$PRODUCT_ID" \
  MERCATO_RUNTIME_PRODUCT_SKU="$PRODUCT_SKU" \
  MERCATO_RUNTIME_CHECKOUT_FAILURES=1 \
    python3 "$SCRIPT_DIR/runtime-mercato-stub.py" >"$STUB_LOG_FILE" 2>&1 &
  STUB_PID=$!
  for _ in {1..30}; do
    kill -0 "$STUB_PID" 2>/dev/null || { show_log; return 1; }
    curl -fsS --max-time 2 "$CONFIG_BASE_URL/health" >/dev/null 2>&1 && return 0
    sleep 0.5
  done
  show_log
  return 1
}

find_postgres_container() {
  [[ -n "$PG_CONTAINER" ]] && return 0
  PG_CONTAINER="$(docker ps --filter 'ancestor=postgres:16' --format '{{.ID}}' | head -n 1)"
  [[ -n "$PG_CONTAINER" ]] || { echo "Unable to locate nopCommerce PostgreSQL container." >&2; exit 1; }
}

pg_scalar() {
  docker exec "$PG_CONTAINER" psql -v ON_ERROR_STOP=1 -U postgres -d nopcommerce -Atqc "$1"
}

sql_literal() {
  printf "'%s'" "${1//\'/\'\'}"
}

build_insert() {
  local table="$1"
  shift
  declare -A overrides=()
  while (($#)); do overrides["$1"]="$2"; shift 2; done

  local cols=()
  local vals=()
  while IFS='|' read -r name data_type nullable column_default udt_name; do
    if [[ -n "${overrides[$name]+x}" ]]; then
      cols+=("\"$name\"")
      vals+=("${overrides[$name]}")
      continue
    fi
    if [[ -n "$column_default" ]]; then
      continue
    fi
    if [[ "$nullable" == "YES" ]]; then
      cols+=("\"$name\"")
      vals+=("NULL")
      continue
    fi

    cols+=("\"$name\"")
    case "$data_type" in
      boolean) vals+=("false") ;;
      smallint|integer|bigint|numeric|real|"double precision") vals+=("0") ;;
      "timestamp without time zone"|"timestamp with time zone"|date) vals+=("CURRENT_TIMESTAMP") ;;
      uuid) vals+=("'00000000-0000-0000-0000-000000000001'") ;;
      *) vals+=("''") ;;
    esac
  done < <(docker exec "$PG_CONTAINER" psql -U postgres -d nopcommerce -AtF'|' -c "SELECT column_name,data_type,is_nullable,COALESCE(column_default,''),udt_name FROM information_schema.columns WHERE table_schema='public' AND table_name='$table' ORDER BY ordinal_position")

  local IFS=,
  printf 'INSERT INTO "%s" (%s) VALUES (%s);' "$table" "${cols[*]}" "${vals[*]}"
}

make_task_due() {
  local task_name="$1"
  pg_scalar "UPDATE \"ScheduleTask\" SET \"LastStartUtc\"=CURRENT_TIMESTAMP - (\"Seconds\" + 5) * INTERVAL '1 second', \"LastEndUtc\"=CURRENT_TIMESTAMP - (\"Seconds\" + 4) * INTERVAL '1 second' WHERE \"Name\"=$(sql_literal "$task_name"); SELECT COUNT(*) FROM \"ScheduleTask\" WHERE \"Name\"=$(sql_literal "$task_name");" >/dev/null
}

run_retry_task() {
  local task_name="Mercato paid order synchronization retry"
  local task_type
  task_type="$(pg_scalar "SELECT \"Type\" FROM \"ScheduleTask\" WHERE \"Name\"=$(sql_literal "$task_name") ORDER BY \"Id\" LIMIT 1;")"
  [[ -n "$task_type" ]] || { echo "OrderSync retry task is not installed." >&2; exit 1; }
  make_task_due "$task_name"
  curl -fsS --max-time 120 -X POST "$BASE_URL/scheduletask/runtask" --data-urlencode "taskType=$task_type" -o "$TASK_RESULT"
}

[[ -f "$NOP_WEB/App_Data/plugins.json" ]] || { echo "Run runtime-install-smoke.sh first." >&2; exit 2; }
find_postgres_container

PRODUCT_DB_ID="$(pg_scalar "SELECT \"Id\" FROM \"Product\" WHERE \"Sku\"=$(sql_literal "$PRODUCT_SKU") ORDER BY \"Id\" LIMIT 1;")"
[[ -n "$PRODUCT_DB_ID" ]] || { echo "Product fixture $PRODUCT_SKU is missing; run runtime-sync-smoke.sh first." >&2; exit 1; }

CUSTOMER_ID="$(pg_scalar "SELECT \"Id\" FROM \"Customer\" WHERE \"Deleted\"=false AND \"Active\"=true ORDER BY \"Id\" LIMIT 1;")"
[[ -n "$CUSTOMER_ID" ]] || { echo "No active nop customer is available for OrderSync fixture." >&2; exit 1; }
STORE_ID="$(pg_scalar "SELECT \"Id\" FROM \"Store\" ORDER BY \"Id\" LIMIT 1;")"
[[ -n "$STORE_ID" ]] || STORE_ID=1

ORDER_SQL="$(build_insert Order \
  OrderGuid "'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'" \
  StoreId "$STORE_ID" \
  CustomerId "$CUSTOMER_ID" \
  BillingAddressId "0" \
  OrderStatusId "30" \
  ShippingStatusId "10" \
  PaymentStatusId "30" \
  PaymentMethodSystemName "'Mercato.Runtime.Payment'" \
  CustomerCurrencyCode "'USD'" \
  CurrencyRate "1" \
  CustomerTaxDisplayTypeId "10" \
  PaidDateUtc "CURRENT_TIMESTAMP" \
  CreatedOnUtc "CURRENT_TIMESTAMP" \
  CustomOrderNumber "$(sql_literal "$ORDER_MARKER")")"
pg_scalar "$ORDER_SQL" >/dev/null
ORDER_ID="$(pg_scalar "SELECT \"Id\" FROM \"Order\" WHERE \"CustomOrderNumber\"=$(sql_literal "$ORDER_MARKER") ORDER BY \"Id\" DESC LIMIT 1;")"
[[ -n "$ORDER_ID" ]] || { echo "Failed to create nop order fixture." >&2; exit 1; }

ITEM_SQL="$(build_insert OrderItem OrderId "$ORDER_ID" ProductId "$PRODUCT_DB_ID" Quantity "2")"
pg_scalar "$ITEM_SQL" >/dev/null

# Make the branch source explicit on the order so the runtime path proves order-level mapping precedence.
pg_scalar "INSERT INTO \"GenericAttribute\" (\"EntityId\",\"KeyGroup\",\"Key\",\"Value\",\"StoreId\") VALUES ($ORDER_ID,'Order','Mercato.BranchId',$(sql_literal "$DEFAULT_BRANCH_ID"),$STORE_ID);" >/dev/null

start_stub
start_nop

# First retry reaches Mercato but receives the simulated 503. The order must remain unmarked.
run_retry_task
sleep 1
SYNCED="$(pg_scalar "SELECT COALESCE((SELECT \"Value\" FROM \"GenericAttribute\" WHERE \"EntityId\"=$ORDER_ID AND \"KeyGroup\"='Order' AND \"Key\"='Mercato.OrderSyncedUtc' AND \"StoreId\"=$STORE_ID ORDER BY \"Id\" DESC LIMIT 1),'');")"
[[ -z "$SYNCED" ]] || { echo "Order was marked synced despite the simulated Mercato failure." >&2; exit 1; }

# Second retry uses the same idempotency key and succeeds, then writes the durable sync marker.
run_retry_task
sleep 1
SYNCED="$(pg_scalar "SELECT COALESCE((SELECT \"Value\" FROM \"GenericAttribute\" WHERE \"EntityId\"=$ORDER_ID AND \"KeyGroup\"='Order' AND \"Key\"='Mercato.OrderSyncedUtc' AND \"StoreId\"=$STORE_ID ORDER BY \"Id\" DESC LIMIT 1),'');")"
[[ -n "$SYNCED" ]] || { echo "Successful OrderSync retry did not persist Mercato.OrderSyncedUtc." >&2; show_log; exit 1; }

# A later retry must skip an already synchronized order and must not call Mercato again.
run_retry_task
sleep 1

python3 - "$STUB_LOG_FILE" "$ORDER_ID" "$DEFAULT_BRANCH_ID" "$PRODUCT_ID" <<'PY'
import json
import sys
log_path, order_id, branch_id, product_id = sys.argv[1:]
payloads = []
with open(log_path, encoding="utf-8") as stream:
    for line in stream:
        if line.startswith("checkout-payload: "):
            payloads.append(json.loads(line.split(": ", 1)[1]))
assert len(payloads) == 2, f"expected exactly two checkout attempts (failure + retry), got {len(payloads)}"
for payload in payloads:
    assert payload.get("idempotencyKey") == f"nop:{order_id}", payload
    assert payload.get("branchId", "").lower() == branch_id.lower(), payload
    assert payload.get("customerId") in ("00000000-0000-0000-0000-000000000000", None), payload
    assert payload.get("paymentMethod") == "Mercato.Runtime.Payment", payload
    items = payload.get("items") or []
    assert len(items) == 1, payload
    assert items[0].get("productId", "").lower() == product_id.lower(), payload
    assert int(items[0].get("quantity", 0)) == 2, payload
print("OrderSync retry payload and idempotency assertions passed.")
PY

if grep -Eiq 'Unhandled exception|ReflectionTypeLoadException|Could not load file or assembly' "$LOG_FILE"; then
  echo "nopCommerce runtime log contains a plugin/runtime loading failure." >&2
  show_log
  exit 1
fi

echo "nopCommerce paid OrderSync failure/retry/idempotency runtime smoke test passed."
