# Mercato POS Acceptance

This is the current acceptance boundary before further nopCommerce development. The goal is to evaluate Mercato Core, inventory, sale, receipt, and return behavior directly through the POS.

## Fast local start

From the repository root:

```bash
docker compose up --build
```

Open:

```text
http://localhost:8080/pos/
```

The development Compose profile defaults to:

```text
Email: admin@mercato.local
Password: MercatoLocal123!
```

These are local-development defaults only. Override them before using the stack outside local testing:

```bash
export MERCATO_BOOTSTRAP_ADMIN_EMAIL='your-admin@example.com'
export MERCATO_BOOTSTRAP_ADMIN_PASSWORD='your-local-password'
export MERCATO_JWT_KEY='a-long-random-local-signing-key-at-least-32-characters'
docker compose up --build
```

## Demo data

The local Compose stack enables `BootstrapDemoData__Enabled=true`. On an empty database it creates one `Demo Store` branch, four generic products, and opening stock so the POS can be evaluated immediately.

Demo bootstrap only runs when both branch and product tables are empty. It does not overwrite existing business data.

Disable it with:

```bash
MERCATO_BOOTSTRAP_DEMO_DATA=false docker compose up --build
```

To reset the local demo completely:

```bash
docker compose down -v
docker compose up --build
```

## Acceptance flow

1. Sign in as Admin, Manager, or Cashier.
2. Confirm the branch selector loads the available branch.
3. Search products by name or SKU and confirm branch stock is shown.
4. Add products to the cart and change quantities.
5. Complete a sale. Mercato must use its own product prices and branch stock, not browser-supplied prices.
6. Confirm a receipt appears with order ID, line totals, total, payment method, reference, and paid time.
7. Print the receipt if desired.
8. Confirm sold stock decreases immediately.
9. Use `Return items` or paste an order ID into Return / Refund.
10. Perform a partial return and confirm stock increases by the returned quantity.
11. Reload the order and confirm the backend reports sold, already-returned, and still-returnable quantities.
12. Attempting to exceed the remaining returnable quantity must be rejected by Mercato even if a client is manipulated.

## Business rules being evaluated

- Mercato is authoritative for product identity, price, and branch stock.
- Checkout requires an idempotency key and retries cannot create a second sale.
- Duplicate cart lines are normalized before stock validation.
- Competing sales cannot spend the same stock twice.
- A sale atomically persists order, stock deduction, invoice, artist settlement line when applicable, payment, accounting sale event, and idempotency result.
- Artist settlement uses product purchase cost, never sale revenue sharing.
- Returns use the original order line price.
- Duplicate/current and cumulative returns cannot exceed quantity sold.
- A return atomically persists the return, stock restoration, artist settlement reversal when applicable, refund payment, and accounting refund event.
- A downstream failure rolls the entire sale/return transaction back.
- Inventory is whole-unit and ledger-backed; ledger movement sums reconcile to available stock.
- Branch transfers are balanced source/destination movements.

## Intentionally unresolved policy

The current POS does not invent business rules that have not been specified. The following remain product-policy decisions rather than implementation defects:

- final allowed payment methods and provider-specific fields;
- taxes and jurisdiction-specific calculation/posting;
- discounts/coupons;
- fiscal/legal receipt localization;
- double-entry chart-of-accounts policy;
- settlement approval/payment-provider metadata.

The generic payment/refund method field exists so the Core workflow can be evaluated without prematurely locking those policies.

## Automated acceptance

CI runs `backend/pos-runtime-smoke.sh` before nopCommerce integration steps. It proves against PostgreSQL that:

- `/pos/` is served;
- bootstrap Admin login returns a role-bearing JWT;
- authorized branches and branch catalog load;
- a sale completes;
- replaying the same checkout idempotency key returns the same order/invoice/payment;
- branch stock decreases exactly once;
- the order reports remaining returnable quantity;
- a partial return completes;
- branch stock is restored by exactly the returned quantity.

Further nopCommerce feature development is intentionally paused until the POS is accepted.
