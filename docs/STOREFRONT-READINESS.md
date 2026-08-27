# Storefront Readiness Milestone

This milestone defines the current development order. nopCommerce is the online-store acceptance boundary; Mercato Core and POS must be proven first so the storefront consumes stable business behavior instead of compensating for unfinished upstream workflows.

## Development order

1. Mercato Core correctness and persistence.
2. POS sale and return workflows.
3. Shared catalog/pricing/inventory/order contracts used by both POS and nopCommerce.
4. nopCommerce storefront synchronization and paid-order handoff.
5. Only after the above are green: richer nop admin UX, deployment hardening, and optional integration features.

## Ownership boundary

Mercato owns product identity, SKU, category/artist relationships, authoritative sale price, purchase cost, branch inventory, customer mapping, orders, invoices, payments captured by Mercato workflows, accounting events, returns, and artist settlement.

nopCommerce owns online storefront presentation, SEO, cart UX, checkout UX, payment gateways, shipping providers, and online customer interaction. nopCommerce must not become a second source of truth for Mercato product pricing or branch inventory.

## Gate A — Core catalog and branch readiness

- [x] Product/category/artist/branch/customer CRUD exists.
- [x] Catalog exposes Mercato product identity, SKU, sale price, and branch availability.
- [x] Product references validate category and artist relationships.
- [ ] Add behavior tests for product/category/artist reference validation.
- [ ] Add behavior tests for catalog branch availability and archived/inactive behavior where applicable.
- [ ] Prove branch inventory cannot be mutated for invalid product/branch inputs.

## Gate B — POS sale readiness

Required flow:

`staff -> branch -> cart -> authoritative price -> aggregate quantities -> validate stock -> order -> stock deduction -> invoice -> artist settlement -> payment -> accounting sale -> idempotency record -> receipt`

- [x] Admin/Manager/Cashier authorization boundary exists.
- [x] Idempotency key is mandatory.
- [x] Optional non-empty customer references must exist.
- [x] Client prices are ignored; Mercato product price is authoritative.
- [x] Duplicate cart lines are aggregated before stock validation (`74d83c2`).
- [x] Sale operations execute inside the unit-of-work transaction boundary.
- [ ] Behavior test: successful sale creates exactly one order/invoice/payment/accounting event.
- [ ] Behavior test: sale deducts exactly the normalized quantity from branch stock.
- [ ] Behavior test: duplicate lines within available stock produce one normalized order/receipt line.
- [x] Regression test: duplicate lines cannot oversell combined branch stock (`9b4be3f`).
- [x] Behavior test: missing idempotency key is rejected before transaction (`9b4be3f`).
- [x] Behavior test: unknown customer is rejected before transaction (`9b4be3f`).
- [ ] Behavior test: repeated idempotency key returns the same completed result without new writes.
- [ ] Behavior test: idempotency insert race resolves to the already-completed result.
- [ ] Behavior test: missing product and insufficient stock leave no committed sale artifacts.
- [ ] Behavior test: artist-owned product uses purchase cost for settlement, never sale revenue.

## Gate C — POS return/refund readiness

Required flow:

`original order -> validate quantities -> cumulative-return guard -> return -> stock addition -> settlement reversal -> refund payment -> accounting refund -> atomic commit`

- [x] Return endpoint and service exist.
- [x] Return pricing is based on original order lines.
- [x] Cumulative over-return prevention exists in implementation.
- [ ] Behavior test: partial return restores exact stock quantity.
- [ ] Behavior test: cumulative returns cannot exceed sold quantity.
- [ ] Behavior test: refund payment and accounting transaction are negative/equivalent as designed.
- [ ] Behavior test: artist settlement reversal uses original purchase-cost basis.
- [ ] Behavior test: failed return leaves inventory/payment/accounting/settlement unchanged.

## Gate D — Inventory readiness

- [x] Ledger-backed adjustments exist.
- [x] Branch transfers exist.
- [x] Movement history exists.
- [ ] Behavior test: sale/return/adjustment/transfer movements reconcile to availability.
- [ ] Behavior test: transfers are balanced between source and destination.
- [ ] Behavior test: invalid or fractional quantities follow the intended whole-unit policy.
- [ ] Verify concurrency behavior for competing sales against the same branch/product stock.

## Gate E — nopCommerce online-store readiness

This gate begins only after A-D are green enough to trust the Core/POS contracts.

- [x] nopCommerce 4.90.7 / .NET 9 target locked.
- [x] Native plugin output and dependency conventions implemented.
- [x] Connector settings, product sync, inventory sync, branch selector, and paid-order adapter exist.
- [x] Product identity uses Mercato generic attributes.
- [x] Branch selection is customer-scoped.
- [x] nop paid-order idempotency key uses `nop:{nopOrderId}`.
- [ ] ProductSync against real Mercato Core must create/update storefront products deterministically.
- [ ] InventorySync must reflect selected/default branch availability without making nop authoritative.
- [ ] Storefront branch selector must render live branches and persist selection.
- [ ] Paid nop order must create exactly one Mercato sale using Mercato authoritative price/stock rules.
- [ ] Replaying the nop paid event must not create a second Mercato order.
- [ ] Missing mapping/branch/stock must fail loudly and preserve Mercato consistency.
- [ ] End-to-end proof: create Mercato product + stock -> sync to nop -> select branch -> online checkout/payment -> Mercato order/invoice/payment/accounting/inventory/settlement state verified.

## Deferred until storefront-ready

Do not spend milestone time on richer nop admin UX, optional plugin operations, final tax policy, discount/coupon policy, double-entry GL design, fiscal receipt localization, or payment-provider-specific POS fields unless they block the storefront-ready acceptance flow.

## Acceptance definition

The platform reaches this milestone when a deterministic CI/runtime scenario proves the same Mercato business rules for both POS and nopCommerce: one authoritative catalog and price, branch-aware stock, atomic/idempotent sale persistence, correct returns, and a nop online checkout that hands a paid order into the same trusted Mercato sale workflow exactly once.
