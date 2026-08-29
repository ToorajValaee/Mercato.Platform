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
CONFIG_DEFAULT_BRANCH_ID="${MERCATO_RUNTIME_DEFAULT_BRANCH_ID:-11111111-1111-1111-1111-111111111111}"
SECOND_BRANCH_ID="${MERCATO_RUNTIME_SECOND_BRANCH_ID:-22222222-2222-2222-2222-222222222222}"
WORK_DIR="${RUNNER_TEMP:-/tmp}/mercato-nopcommerce-runtime-config"
COOKIE_JAR="$WORK_DIR/cookies.txt"
LOGIN_PAGE="$WORK_DIR/login.html"
LOGIN_RESULT="$WORK_DIR/login-result.html"
CONFIG_PAGE="$WORK_DIR/configure.html"
CONFIG_SAVE_RESULT="$WORK_DIR/configure-save.html"
HOME_PAGE="$WORK_DIR/home.html"
BRANCH_RESULT="$WORK_DIR/branch-result.html"
LOG_FILE="$WORK_DIR/nopcommerce.log"
STUB_LOG_FILE="$WORK_DIR/mercato-stub.log"

if [[ ! -f "$NOP_WEB/Nop.Web.csproj" ]]; then
  echo "Nop.Web.csproj was not found under $NOP_WEB" >&2
  exit 2
fi
if [[ ! -f "$NOP_WEB/App_Data/dataSettings.json" ]]; then
  echo "nopCommerce is not installed; run runtime-install-smoke.sh first." >&2
  exit 2
fi

mkdir -p "$WORK_DIR"
rm -f "$COOKIE_JAR" "$LOGIN_PAGE" "$LOGIN_RESULT" "$CONFIG_PAGE" "$CONFIG_SAVE_RESULT" \
  "$HOME_PAGE" "$BRANCH_RESULT" "$LOG_FILE" "$STUB_LOG_FILE"
NOP_PID=""
STUB_PID=""

show_log() {
  if [[ -f "$LOG_FILE" ]]; then
    echo "----- nopCommerce runtime log -----" >&2
    tail -n 250 "$LOG_FILE" >&2 || true
    echo "-----------------------------------" >&2
  fi
  if [[ -f "$STUB_LOG_FILE" ]]; then
    echo "----- Mercato runtime stub log ----" >&2
    tail -n 100 "$STUB_LOG_FILE" >&2 || true
    echo "-----------------------------------" >&2
  fi
}

cleanup() {
  if [[ -n "${NOP_PID:-}" ]] && kill -0 "$NOP_PID" 2>/dev/null; then
    kill "$NOP_PID" 2>/dev/null || true
  fi
  if [[ -n "${STUB_PID:-}" ]] && kill -0 "$STUB_PID" 2>/dev/null; then
    kill "$STUB_PID" 2>/dev/null || true
  fi
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
  MERCATO_RUNTIME_DEFAULT_BRANCH_ID="$CONFIG_DEFAULT_BRANCH_ID" \
  MERCATO_RUNTIME_SECOND_BRANCH_ID="$SECOND_BRANCH_ID" \
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

extract_form_input_value() {
  python3 - "$1" "$2" "$3" <<'PY'
from html.parser import HTMLParser
import sys
page, action_fragment, field = sys.argv[1:4]
class Parser(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.in_target = False
        self.value = None
    def handle_starttag(self, tag, attrs):
        values = dict(attrs)
        if tag.lower() == "form":
            self.in_target = action_fragment in values.get("action", "")
        elif self.in_target and tag.lower() == "input" and values.get("name") == field and self.value is None:
            self.value = values.get("value", "")
    def handle_endtag(self, tag):
        if tag.lower() == "form":
            self.in_target = False
p = Parser()
with open(page, encoding="utf-8-sig") as stream:
    p.feed(stream.read())
if p.value is None:
    raise SystemExit(f"Unable to locate {field!r} in form containing {action_fragment!r}")
print(p.value)
PY
}

assert_selected_branch() {
  python3 - "$1" "$2" <<'PY'
from html.parser import HTMLParser
import sys
page, expected = sys.argv[1:3]
class Parser(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.in_selector = False
        self.selected = None
    def handle_starttag(self, tag, attrs):
        values = dict(attrs)
        tag = tag.lower()
        if tag == "select" and values.get("id") == "mercato-branch":
            self.in_selector = True
        elif self.in_selector and tag == "option" and "selected" in values:
            self.selected = values.get("value")
    def handle_endtag(self, tag):
        if tag.lower() == "select":
            self.in_selector = False
p = Parser()
with open(page, encoding="utf-8-sig") as stream:
    p.feed(stream.read())
if (p.selected or "").lower() != expected.lower():
    raise SystemExit(f"Expected selected Mercato branch {expected}, got {p.selected}")
PY
}

assert_input_value() {
  local actual
  actual="$(extract_input_value "$1" "$2")"
  if [[ "$actual" != "$3" ]]; then
    echo "Expected $2=$3, got $actual." >&2
    exit 1
  fi
}

start_nop
wait_for_nop
start_stub

CONFIG_URL="$BASE_URL/Admin/MercatoConnector/Configure"
LOGIN_URL="$BASE_URL/login?returnUrl=%2FAdmin%2FMercatoConnector%2FConfigure"

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
LOGIN_EFFECTIVE_URL="$(curl -sS -L --max-time 60 \
  -b "$COOKIE_JAR" -c "$COOKIE_JAR" \
  -o /dev/null -w '%{url_effective}' \
  "$CONFIG_URL")"

if [[ "$LOGIN_STATUS" != "200" ]] || ! grep -q 'Mercato connection' "$LOGIN_RESULT"; then
  echo "Administrator login did not land on Connector configuration. status=$LOGIN_STATUS" >&2
  cat "$LOGIN_RESULT" >&2 || true
  show_log
  exit 1
fi
if [[ "$LOGIN_EFFECTIVE_URL" == *"/login"* ]]; then
  echo "Persistent administrator session did not survive the follow-up request." >&2
  show_log
  exit 1
fi

cp "$LOGIN_RESULT" "$CONFIG_PAGE"
CONFIG_TOKEN="$(extract_input_value "$CONFIG_PAGE" "__RequestVerificationToken")"
CONFIG_SAVE_STATUS="$(curl -sS -L --max-time 60 \
  -b "$COOKIE_JAR" -c "$COOKIE_JAR" \
  -o "$CONFIG_SAVE_RESULT" -w '%{http_code}' \
  "$CONFIG_URL" \
  --data-urlencode "__RequestVerificationToken=$CONFIG_TOKEN" \
  --data-urlencode "BaseUrl=$CONFIG_BASE_URL" \
  --data-urlencode "BearerToken=$CONFIG_BEARER_TOKEN" \
  --data-urlencode "DefaultBranchId=$CONFIG_DEFAULT_BRANCH_ID")"
if [[ "$CONFIG_SAVE_STATUS" != "200" ]]; then
  echo "Connector settings save did not resolve to HTTP 200 (HTTP $CONFIG_SAVE_STATUS)." >&2
  cat "$CONFIG_SAVE_RESULT" >&2 || true
  show_log
  exit 1
fi

grep -q 'Mercato connector settings were saved.' "$CONFIG_SAVE_RESULT"
assert_input_value "$CONFIG_SAVE_RESULT" "BaseUrl" "$CONFIG_BASE_URL"
assert_input_value "$CONFIG_SAVE_RESULT" "BearerToken" "$CONFIG_BEARER_TOKEN"
assert_input_value "$CONFIG_SAVE_RESULT" "DefaultBranchId" "$CONFIG_DEFAULT_BRANCH_ID"

HOME_STATUS="$(curl -sS -L -b "$COOKIE_JAR" -c "$COOKIE_JAR" -o "$HOME_PAGE" -w '%{http_code}' --max-time 30 "$BASE_URL/")"
if [[ "$HOME_STATUS" != "200" ]]; then
  echo "Storefront did not render after Connector configuration (HTTP $HOME_STATUS)." >&2
  show_log
  exit 1
fi

grep -q 'class="mercato-branch-selector"' "$HOME_PAGE"
grep -q 'Runtime Main Branch' "$HOME_PAGE"
grep -q 'Runtime Second Branch' "$HOME_PAGE"

BRANCH_TOKEN="$(extract_form_input_value "$HOME_PAGE" "/mercato/branch/select" "__RequestVerificationToken")"
BRANCH_STATUS="$(curl -sS -L --max-time 30 \
  -b "$COOKIE_JAR" -c "$COOKIE_JAR" \
  -o "$BRANCH_RESULT" -w '%{http_code}' \
  "$BASE_URL/mercato/branch/select" \
  --data-urlencode "__RequestVerificationToken=$BRANCH_TOKEN" \
  --data-urlencode "branchId=$SECOND_BRANCH_ID" \
  --data-urlencode "returnUrl=/")"
if [[ "$BRANCH_STATUS" != "200" ]]; then
  echo "Branch selection did not return to the storefront (HTTP $BRANCH_STATUS)." >&2
  show_log
  exit 1
fi

curl -fsS -b "$COOKIE_JAR" -c "$COOKIE_JAR" "$BASE_URL/" -o "$HOME_PAGE"
assert_selected_branch "$HOME_PAGE" "$SECOND_BRANCH_ID"

grep -q 'GET /api/branches' "$STUB_LOG_FILE"
if grep -Eiq 'Unhandled exception|ReflectionTypeLoadException|Could not load file or assembly' "$LOG_FILE"; then
  echo "nopCommerce runtime log contains a plugin/runtime loading failure." >&2
  show_log
  exit 1
fi

echo "nopCommerce Connector configuration and live branch selection smoke test passed."
