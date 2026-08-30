# Mercato Core, Back Office and POS Acceptance

This is the current acceptance boundary before further nopCommerce development. The goal is to evaluate Mercato Core, back-office operations, inventory, sale, receipt, localization, branch staff access, and return behavior directly without nopCommerce.

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

1. Dashboard counts, quick navigation, recent sales chart, and stock-by-branch chart.
2. Products: create/edit/archive name, SKU, sale price, purchase cost, category and artist.
3. Categories: create root or child categories, edit hierarchy and Admin deletion. A root category with no parent must create successfully; a category may not select itself as parent.
4. Artists: create/edit identity and Admin deletion.
5. Branches: create/edit and Admin deletion subject to backend constraints.
6. Customers: create/update contact records.
7. Inventory: branch availability, whole-unit adjustments, branch transfers and movement history.
8. Invoices: branch/customer filtering and invoice detail.
9. Order lookup: inspect a Mercato order and returnable quantities.
10. Artist settlements: calculate purchase-cost settlements, filter state and mark paid.
11. Accounting: filter operational transaction history and summary reporting.
12. Staff: Admin-only list/create/edit-role/reset-password/delete for Admin, Manager and Cashier accounts, plus assignment to one or more branches. Self-delete and self-demotion from Admin are blocked.
13. Every Back Office data table uses pagination with selectable page size; this includes branch availability, invoices, movements, settlements, accounting transactions, staff, and master-data grids.
14. Business select/combo controls provide an inline search filter so long product/category/artist/branch/customer lists can be narrowed without scrolling the entire list.
15. Navigation groups expand/collapse when their group title is clicked, and the complete sidebar can be opened/closed from the menu button.

Cashier accounts do not receive Manager/Admin mutation and finance navigation. Manager accounts receive operational and finance functions but not staff administration. Admin receives the complete Back Office surface.

## Language and calendar acceptance

Back Office and POS share the same language preference and currently support English and Farsi.

- Switching to Farsi changes document direction to RTL and translates the primary operator UI.
- Switching back to English restores LTR and the English source strings.
- Displayed dates in Farsi use the Persian calendar locale (`fa-IR` with the Persian calendar).
- Back Office date-time inputs used for settlements/accounting replace the visible Gregorian picker with a Persian/Jalali calendar popup in Farsi mode.
- The Persian picker supports Persian month navigation, day selection, Today, hour and minute, and converts the selected Jalali value back to the Gregorian/local representation required by the existing API contract.
- English mode continues to use the browser's standard Gregorian `datetime-local` input.

## Staff branch acceptance

Admin can assign a Manager or Cashier to one or more branches. Assignments are persisted in `UserBranchAssignments` and are enforced by the API rather than only by the browser.

- `GET /api/branches/accessible` returns all branches for Admin and only assigned branches for Manager/Cashier.
- POS uses the accessible-branch endpoint, so staff only see branches assigned to them.
- Branch-scoped catalog/POS checkout/order lookup/returns enforce branch access server-side.
- Branch-scoped inventory read/adjustment/transfer/movement operations enforce branch access server-side.
- An unassigned branch cannot be used merely by manipulating a browser request.

## POS acceptance flow

1. Sign in as Admin, Manager, or Cashier.
2. Confirm the searchable branch selector loads only branches accessible to the signed-in staff member.
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
- Staff roles and branch assignments are enforced server-side; hiding a browser control is not the authorization mechanism.

## Intentionally unresolved policy

The current application does not invent business rules that have not been specified. The following remain product-policy decisions rather than implementation defects:

- final allowed payment methods and provider-specific fields;
- taxes and jurisdiction-specific calculation/posting;
- discounts/coupons;
- fiscal/legal receipt localization beyond the current operator UI/receipt rendering;
- double-entry chart-of-accounts policy;
- settlement approval/payment-provider metadata.

The generic payment/refund method field exists so the Core workflow can be evaluated without prematurely locking those policies.

## Automated acceptance

Core/POS CI builds and runs the real Mercato Docker image against PostgreSQL, validates all browser JavaScript with `node --check`, then executes `backend/pos-http-smoke.sh`. It proves that:

- `/` serves the workspace launcher;
- `/admin/` serves the Back Office with dashboard charts, pagination and staff branch controls;
- `/pos/` serves the bilingual POS and uses accessible branches;
- the shared UI bundle contains Farsi/Persian-calendar support;
- root-category creation with `parentCategoryId=null` succeeds;
- bootstrap Admin login returns a role-bearing JWT;
- Admin can create a Cashier assigned to a branch;
- the Cashier can log in and receives only that assigned branch from `/api/branches/accessible`;
- authorized branch catalog loads;
- a sale completes;
- replaying the same checkout idempotency key returns the same order/invoice/payment;
- branch stock decreases exactly once;
- the order reports remaining returnable quantity;
- a partial return completes;
- branch stock is restored by exactly the returned quantity;
- the full backend test suite passes after runtime acceptance.

Run `33283950862` completed successfully on August 30, 2026 with Release build, browser-JavaScript validation, production-like Docker/PostgreSQL runtime acceptance, root-category regression proof, staff branch-assignment proof, POS sale/return workflow, and backend tests all green.

Further nopCommerce feature development is intentionally paused until this Core/Back Office/POS surface is accepted by the user.
