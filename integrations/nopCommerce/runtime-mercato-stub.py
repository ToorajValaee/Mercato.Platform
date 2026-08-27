#!/usr/bin/env python3
import json
import os
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

HOST = os.environ.get("MERCATO_STUB_HOST", "127.0.0.1")
PORT = int(os.environ.get("MERCATO_STUB_PORT", "5099"))
TOKEN = os.environ.get("MERCATO_RUNTIME_BEARER_TOKEN", "runtime-token")
DEFAULT_BRANCH_ID = os.environ.get("MERCATO_RUNTIME_DEFAULT_BRANCH_ID", "11111111-1111-1111-1111-111111111111")
SECOND_BRANCH_ID = os.environ.get("MERCATO_RUNTIME_SECOND_BRANCH_ID", "22222222-2222-2222-2222-222222222222")
PRODUCT_ID = os.environ.get("MERCATO_RUNTIME_PRODUCT_ID", "33333333-3333-3333-3333-333333333333")
SKU = os.environ.get("MERCATO_RUNTIME_PRODUCT_SKU", "MERCATO-RUNTIME-001")
PRODUCT_NAME = os.environ.get("MERCATO_RUNTIME_PRODUCT_NAME", "Mercato Runtime Product")
PRICE = float(os.environ.get("MERCATO_RUNTIME_PRODUCT_PRICE", "42.50"))
STOCK = int(os.environ.get("MERCATO_RUNTIME_PRODUCT_STOCK", "17"))

BRANCHES = [
    {"id": DEFAULT_BRANCH_ID, "name": "Runtime Main Branch", "address": "Runtime address 1"},
    {"id": SECOND_BRANCH_ID, "name": "Runtime Second Branch", "address": "Runtime address 2"},
]


def catalog(branch_id=None):
    quantity = STOCK if not branch_id or branch_id.lower() == DEFAULT_BRANCH_ID.lower() else 5
    return [{
        "productId": PRODUCT_ID,
        "name": PRODUCT_NAME,
        "sku": SKU,
        "salePrice": PRICE,
        "categoryId": None,
        "artistId": None,
        "branchId": branch_id,
        "availableQuantity": quantity,
    }]


class Handler(BaseHTTPRequestHandler):
    server_version = "MercatoRuntimeStub/1.0"

    def log_message(self, fmt, *args):
        print("mercato-stub:", fmt % args, flush=True)

    def _authorized(self):
        if not TOKEN:
            return True
        return self.headers.get("Authorization", "") == f"Bearer {TOKEN}"

    def _json(self, status, payload):
        body = json.dumps(payload).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        parsed = urlparse(self.path)
        if parsed.path == "/health":
            self._json(200, {"status": "ok"})
            return

        if not self._authorized():
            self._json(401, {"error": "unauthorized"})
            return

        if parsed.path == "/api/branches":
            self._json(200, BRANCHES)
            return

        if parsed.path == "/api/catalog":
            branch_id = parse_qs(parsed.query).get("branchId", [None])[0]
            self._json(200, catalog(branch_id))
            return

        self._json(404, {"error": "not_found"})

    def do_POST(self):
        parsed = urlparse(self.path)
        if not self._authorized():
            self._json(401, {"error": "unauthorized"})
            return

        length = int(self.headers.get("Content-Length", "0") or "0")
        body = self.rfile.read(length) if length else b""
        try:
            payload = json.loads(body.decode("utf-8")) if body else {}
        except json.JSONDecodeError:
            self._json(400, {"error": "invalid_json"})
            return

        if parsed.path == "/api/pos/checkout":
            self._json(200, {
                "transactionId": "44444444-4444-4444-4444-444444444444",
                "idempotencyKey": payload.get("idempotencyKey"),
                "externalOrderId": payload.get("externalOrderId"),
            })
            return

        self._json(404, {"error": "not_found"})


if __name__ == "__main__":
    print(f"Mercato runtime stub listening on http://{HOST}:{PORT}", flush=True)
    ThreadingHTTPServer((HOST, PORT), Handler).serve_forever()
