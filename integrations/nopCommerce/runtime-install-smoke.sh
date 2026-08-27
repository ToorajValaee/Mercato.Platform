#!/usr/bin/env bash
set -euo pipefail

NOP_ROOT="${NOP_ROOT:-${1:-}}"
if [[ -z "$NOP_ROOT" ]]; then
  echo "NOP_ROOT (or first argument) must point to a nopCommerce 4.90.7 checkout." >&2
  exit 2
fi

NOP_WEB="$NOP_ROOT/src/Presentation/Nop.Web"
BASE_URL="${NOP_BASE_URL:-http://127.0.0.1:5080}"
DB_CONNECTION="${NOP_DB_CONNECTION_STRING:-Host=127.0.0.1;Port=55432;Database=nopcommerce;Username=postgres;Password=postgres}"
ADMIN_EMAIL="${NOP_ADMIN_EMAIL:-admin@mercato.local}"
ADMIN_PASSWORD="${NOP_ADMIN_PASSWORD:-MercatoRuntime123!}"
WORK_DIR="${RUNNER_TEMP:-/tmp}/mercato-nopcommerce-runtime-install"
COOKIE_JAR="$WORK_DIR/cookies.txt"
INSTALL_PAGE="$WORK_DIR/install.html"
INSTALL_RESULT="$WORK_DIR/install-result.html"
HOME_PAGE="$WORK_DIR/home.html"
LOG_FILE="$WORK_DIR/nopcommerce.log"
PLUGINS_INFO="$NOP_WEB/App_Data/plugins.json"

EXPECTED_PLUGINS=(
  "Mercato.Connector"
  "Mercato.ProductSync"
  "Mercato.InventorySync"
  "Mercato.BranchSelector"
  "Mercato.OrderSync"
)

if [[ ! -f "$NOP_WEB/Nop.Web.csproj" ]]; then
  echo "Nop.Web.csproj was not found under $NOP_WEB" >&2
  exit 2
fi

mkdir -p "$WORK_DIR"
rm -f "$COOKIE_JAR" "$INSTALL_PAGE" "$INSTALL_RESULT" "$HOME_PAGE" "$LOG_FILE"
NOP_PID=""

show_log() {
  [[ -f "$LOG_FILE" ]] || return 0
  echo "----- nopCommerce runtime log -----" >&2
  tail -n 250 "$LOG_FILE" >&2 || true
  echo "-----------------------------------" >&2
}

stop_nop() {
  if [[ -n "${NOP_PID:-}" ]] && kill -0 "$NOP_PID" 2>/dev/null; then
    kill "$NOP_PID" 2>/dev/null || true
    for _ in {1..20}; do
      kill -0 "$NOP_PID" 2>/dev/null || break
      sleep 0.5
    done
    kill -9 "$NOP_PID" 2>/dev/null || true
  fi
  NOP_PID=""
}
trap stop_nop EXIT

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

wait_for_url() {
  local url="$1"
  local attempts="${2:-120}"
  local status
  for ((i = 1; i <= attempts; i++)); do
    if ! kill -0 "$NOP_PID" 2>/dev/null; then
      echo "nopCommerce exited before $url became ready." >&2
      show_log
      return 1
    fi
    status="$(curl -sS -o /dev/null -w '%{http_code}' --max-time 5 "$url" || true)"
    if [[ "$status" =~ ^(200|301|302|303|307|308)$ ]]; then
      return 0
    fi
    sleep 2
  done
  echo "Timed out waiting for $url." >&2
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

verify_plugin_state() {
  python3 - "$PLUGINS_INFO" "${EXPECTED_PLUGINS[@]}" <<'PY'
import json
import sys
path = sys.argv[1]
expected = set(sys.argv[2:])
with open(path, encoding="utf-8-sig") as stream:
    data = json.load(stream)
installed = {
    item.get("SystemName")
    for item in data.get("InstalledPlugins", [])
    if isinstance(item, dict)
}
missing = sorted(expected - installed)
if missing:
    print("Expected Mercato plugins were not installed: " + ", ".join(missing), file=sys.stderr)
    print(json.dumps(data, indent=2), file=sys.stderr)
    raise SystemExit(1)
pending = json.dumps(data.get("PluginNamesToInstall", []))
if any(name in pending for name in expected):
    print("Mercato plugin installation is still pending after restart.", file=sys.stderr)
    raise SystemExit(1)
print("Installed Mercato plugins: " + ", ".join(sorted(expected)))
PY
}

# Install nopCommerce through its official web installer with the packaged Mercato assemblies present.
start_nop
wait_for_url "$BASE_URL/install"
curl -fsS -c "$COOKIE_JAR" "$BASE_URL/install" -o "$INSTALL_PAGE"
TOKEN="$(extract_input_value "$INSTALL_PAGE" "__RequestVerificationToken")"
INSTALL_STATUS="$(
  curl -sS --max-time 600 \
    -b "$COOKIE_JAR" -c "$COOKIE_JAR" \
    -o "$INSTALL_RESULT" -w '%{http_code}' \
    -X POST "$BASE_URL/install" \
    --data-urlencode "__RequestVerificationToken=$TOKEN" \
    --data-urlencode "AdminEmail=$ADMIN_EMAIL" \
    --data-urlencode "AdminPassword=$ADMIN_PASSWORD" \
    --data-urlencode "ConfirmPassword=$ADMIN_PASSWORD" \
    --data-urlencode "DataProvider=3" \
    --data-urlencode "ConnectionStringRaw=true" \
    --data-urlencode "ConnectionString=$DB_CONNECTION" \
    --data-urlencode "InstallSampleData=false" \
    --data-urlencode "SubscribeNewsletters=false"
)"
if [[ "$INSTALL_STATUS" != "200" ]]; then
  echo "nopCommerce installer returned HTTP $INSTALL_STATUS." >&2
  cat "$INSTALL_RESULT" >&2 || true
  show_log
  exit 1
fi
[[ -f "$PLUGINS_INFO" ]] || { echo "nopCommerce installation did not create App_Data/plugins.json." >&2; exit 1; }

# Restart once to apply the plugin installation queue and core update migrations.
stop_nop
sleep 1
start_nop
wait_for_url "$BASE_URL/"
verify_plugin_state

HOME_STATUS="$(curl -sS -L -o "$HOME_PAGE" -w '%{http_code}' --max-time 30 "$BASE_URL/")"
HOME_EFFECTIVE_URL="$(curl -sS -L -o /dev/null -w '%{url_effective}' --max-time 30 "$BASE_URL/")"
if [[ "$HOME_STATUS" != "200" || "$HOME_EFFECTIVE_URL" == */install* ]]; then
  echo "nopCommerce did not reach an installed storefront. status=$HOME_STATUS effective_url=$HOME_EFFECTIVE_URL" >&2
  show_log
  exit 1
fi

CONFIG_STATUS="$(curl -sS -o /dev/null -w '%{http_code}' --max-time 30 "$BASE_URL/Admin/MercatoConnector/Configure")"
if [[ ! "$CONFIG_STATUS" =~ ^(301|302|303|307|308|401|403)$ ]]; then
  echo "Connector admin endpoint was not protected as expected (HTTP $CONFIG_STATUS)." >&2
  show_log
  exit 1
fi

if grep -Eiq 'Unhandled exception|ReflectionTypeLoadException|Could not load file or assembly' "$LOG_FILE"; then
  echo "nopCommerce runtime log contains a plugin/runtime loading failure." >&2
  show_log
  exit 1
fi

echo "nopCommerce 4.90.7 installation and Mercato plugin activation smoke test passed."
