# Mercato Core, Back Office and POS Acceptance

This is the current acceptance boundary before further nopCommerce development. The goal is to evaluate Mercato Core, back-office operations, inventory, sale, receipt, and return behavior directly without nopCommerce.

## Fast local start

From the repository root:

```bash
docker compose up --build
```

Open the workspace launcher:

```text
http://localhost:8080/
```

Direct workspaces:

```text
Back Office: http://localhost:8080/admin/
POS:         http://localhost:8080/pos/
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

The local Compose stack enables `BootstrapDemoData__Enabled=true`. On an empty database it creates one `Demo Store` branch, four generic products, and opening stock so the POS and inventory screens can be evaluated immediately.

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

## Back Office acceptance

Sign in at `/admin/`. The role-aware Back Office exposes the implemented Mercato management surface:

1. Dashboard counts and quick navigation.
2. Products: create/edit/archive name, SKU, sale price, purchase cost, category and artist.
3. Categories: create/edit hierarchy and Admin deletion.
4. Artists: create/edit identity and Admin deletion.
5. Branches: create/edit and Admin deletion subject to backend constraints.
6. Customers: create/update contact records.
7. Inventory: branch availability, whole-unit adjustments, branch transfers and movement history.
8. Invoices: branch/customer filtering and invoice detail.
9. Order lookup: inspect a Mercato order and returnable quantities.
10. Artist settlements: calculate purchase-cost settlements, filter state and mark paid.
11. Accounting: filter operational transaction history and summary reporting.
12. Staff: Admin-only list/create/edit-role/reset-password/delete for Admin, Manager and Cashier accounts. Self-delete and self-demotion from Admin are blocked.

Cashier accounts do not receive Manager/Admin mutation and finance navigation. Manager accounts receive operational and finance functions but not staff administration. Admin receives the complete Back Office surface.

## POS acceptance flow

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
- Staff roles are enforced server-side; hiding a browser control is not the authorization mechanism.

## Intentionally unresolved policy

The current application does not invent business rules that have not been specified. The following remain product-policy decisions rather than implementation defects:

- final allowed payment methods and provider-specific fields;
- taxes and jurisdiction-specific calculation/posting;
- discounts/coupons;
- fiscal/legal receipt localization;
- double-entry chart-of-accounts policy;
- settlement approval/payment-provider metadata.

The generic payment/refund method field exists so the Core workflow can be evaluated without prematurely locking those policies.

## Automated acceptance

Core/POS CI builds and runs the real Mercato Docker image against PostgreSQL, then executes `backend/pos-http-smoke.sh`. It proves that:

- `/` serves the workspace launcher;
- `/admin/` serves the Back Office;
- `/pos/` serves the POS;
- bootstrap Admin login returns a role-bearing JWT;
- Admin staff list/create/change-role/delete works through the live API;
- authorized branches and branch catalog load;
- a sale completes;
- replaying the same checkout idempotency key returns the same order/invoice/payment;
- branch stock decreases exactly once;
- the order reports remaining returnable quantity;
- a partial return completes;
- branch stock is restored by exactly the returned quantity;
- the full backend test suite passes after runtime acceptance.

Run `33282781177` completed successfully with build, production-like Docker/PostgreSQL runtime acceptance, Back Office/staff lifecycle, POS sale/return workflow, and backend tests all green.

Further nopCommerce feature development is intentionally paused until this Core/Back Office/POS surface is accepted by the user.
