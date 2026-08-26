# Mercato Platform Specification

Version: 1.8
Purpose: Source of truth for product requirements, architecture, business rules, implementation status, known gaps, and continuation work.

## 1. Product boundary

Mercato is the business brain. It owns products, categories, artists, branches, customers, authoritative pricing, inventory, POS, orders, invoices, accounting transaction capture, catalog data, returns, and artist settlement.

nopCommerce is the commerce engine. It owns storefront presentation, SEO, cart, checkout UI, payment gateways, and shipping providers.

Business rule: artist products are tracked by purchase cost, not revenue sharing.

## 2. Backend architecture

```text
Mercato.Api
    |
Mercato.Application
    |
Mercato.Domain
    |
Mercato.Infrastructure
    |
PostgreSQL / EF Core
```

The backend stays consolidated in the existing Clean Architecture projects.

## 3. POS sale flow

```text
Authenticated Staff (Admin/Manager/Cashier)
→ branch
→ cart
→ required IdempotencyKey
→ validate optional customer reference
→ resolve authoritative Mercato prices
→ validate stock
→ Order + OrderItems
→ inventory ledger deductions
→ Invoice
→ purchase-cost artist SettlementLines
→ Payment
→ AccountingTransaction(Sale)
→ persist idempotency response
→ atomic commit
→ printable receipt payload
```

Client prices are never trusted. Checkout retries cannot create a second committed sale. Non-empty CustomerId must identify a real Mercato customer; Guid.Empty represents an unlinked/guest checkout. Sale persistence is atomic across order, inventory, invoice, settlement, payment, accounting, and idempotency state.

## 4. POS return/refund flow

```text
Original Order
→ validate returned quantities
→ prevent cumulative over-return
→ SalesReturn + SalesReturnLines
→ inventory ledger return movements
→ purchase-cost artist settlement reversal lines
→ refund Payment
→ AccountingTransaction(Refund)
→ atomic commit
```

Return values use original order line prices. Artist settlement reversals remain purchase-cost based. API: `POST /api/pos/returns`.

## 5. Inventory engine

Implemented: EF-backed stock movement ledger, branch availability, audited whole-unit adjustments, sale deductions, return additions, persisted branch transfers, movement history, and Admin/Manager authorization. Mercato remains the only inventory authority.

## 6. Master data and catalog

Implemented application/infrastructure/API support covers Products, Categories, Artists, Branches, Customers, Catalog, and Invoices. Products include SKU, purchase price, sale price, category reference, and optional artist reference. Product category/artist references are validated before persistence. Catalog output includes SKU and optional branch-specific availability.

## 7. Artist settlement

Artist-owned sales create SettlementLine records using `PurchasePrice × QuantitySold`; returns create equivalent negative reversals. Period summaries are unique per artist/period, persisted, filterable, and track paid state. Marking a settlement paid creates an `AccountingTransaction(Type=ArtistSettlementPayment)` with a negative amount.

## 8. Accounting

Implemented durable accounting event ledger:
- Sale → positive transaction;
- Refund → negative transaction;
- Artist settlement payment → negative transaction;
- reporting filters by branch, period, and type;
- summary reports GrossSales, Refunds, NetSales, ArtistSettlementPayments, NetCashMovement, and transaction count.

This is not a policy-defined double-entry general ledger. Chart of accounts, jurisdiction-specific tax accounts, and final posting policy require explicit business/accounting decisions.

## 9. nopCommerce 4.90.7 integration

Target release: **nopCommerce 4.90.7** (`release-4.90.7`) on **.NET 9**. All five plugin projects target `net9.0`, include nopCommerce `BasePlugin` adapters, and declare `SupportedVersions: ["4.90"]`.

Implemented shared/core behavior:
- `Mercato.NopCommerce.Core`: authenticated Mercato HTTP client, health, catalog retrieval, and idempotent commerce-order synchronization;
- `Mercato.Connector.Plugin`: connection/health core and concrete DI registration;
- `Mercato.ProductSync.Plugin`: Mercato catalog → concrete nopCommerce product upsert;
- `Mercato.InventorySync.Plugin`: branch-specific Mercato availability → concrete nopCommerce stock snapshot;
- `Mercato.BranchSelector.Plugin`: branch-specific availability plus persisted customer branch selection;
- `Mercato.OrderSync.Plugin`: nopCommerce `OrderPaidEvent` → Mercato checkout.

Concrete adapter rules:
- product sync uses Mercato SKU, or stable fallback `MERCATO-{ProductId:N}` when SKU is absent;
- synchronized nop products carry `Mercato.ProductId=<guid>` in admin metadata for reverse mapping;
- product name and price are overwritten from Mercato so Mercato remains product/pricing authority;
- inventory sync writes nop `StockQuantity` from Mercato availability while Mercato remains stock authority;
- selected branch is stored in nop customer generic attribute `Mercato.BranchId`;
- paid-order sync resolves branch from order/customer attributes and then `Mercato:DefaultBranchId` fallback;
- optional Mercato customer mapping uses `Mercato.CustomerId`; otherwise checkout is unlinked/guest;
- paid-order lines map nop products back to Mercato GUIDs from synchronized metadata;
- order synchronization uses idempotency key `nop:{nopOrderId}`;
- missing branch or unmapped products fail loudly and are logged in nopCommerce instead of creating incorrect Mercato transactions.

Concrete adapters register through nopCommerce `INopStartup`. Project files accept `NopCommerceRoot`; when supplied they reference the exact 4.90.7 `Nop.Web.csproj`. CI checks out official `release-4.90.7` source and compiles the adapters against it.

Configuration keys:
- `Mercato:BaseUrl` — required;
- `Mercato:BearerToken` — server-to-server bearer credential;
- `Mercato:DefaultBranchId` — optional order-sync fallback branch.

Remaining nopCommerce work is deployment/UX triggering: expose branch selection in the chosen storefront theme, add manual/scheduled product and inventory sync triggers, and verify the complete paid-order flow in a running nopCommerce deployment.

## 10. Database initialization

No EF migrations currently exist. Startup uses `MigrateAsync` when migrations exist and otherwise `EnsureCreatedAsync` for a fresh deployment. Before upgrading an existing production database, create and review a baseline/current EF migration; `EnsureCreated` is not a schema-upgrade strategy.

## 11. Resolved technical debt

Resolved: checkout DTO/type shadowing, obsolete checkout workflow stub, no-op inventory repository, no-op UnitOfWork, placeholder branch/invoice/transfer/settlement APIs, invalid EF mappings, in-memory-only order/invoice persistence, client-trusted POS prices, invalid unattributed settlement creation, missing checkout retry protection, broken central NuGet package management, and the missing Infrastructure → Application project reference.

## 12. Development status

Implemented core platform development:
- [x] Product/category/artist/branch/customer management
- [x] Inventory ledger, adjustments, transfers, movement history
- [x] Catalog data API
- [x] POS checkout, atomicity, idempotency, receipt data, authorization roles
- [x] POS returns/refunds and inventory/settlement/accounting reversals
- [x] Artist settlement aggregation/payment state/accounting event
- [x] Accounting transaction/reporting API
- [x] nopCommerce target locked to 4.90.7
- [x] nopCommerce plugin metadata/BasePlugin adapters
- [x] concrete product gateway
- [x] concrete inventory gateway
- [x] customer branch-selection persistence
- [x] paid-order event consumer and product reverse mapping
- [x] CI configured to compile against official nopCommerce 4.90.7 source

Business/policy decisions intentionally unresolved:
- [ ] final POS payment methods and method-specific fields
- [ ] tax jurisdiction/calculation/posting rules
- [ ] POS discount/coupon authority and rules
- [ ] chart of accounts / double-entry GL decision
- [ ] settlement approval and external-payment metadata
- [ ] fiscal/legal receipt requirements beyond current durable receipt payload
- [ ] production EF migrations for schema upgrades

Integration/deployment work still required:
- [ ] expose branch-selector storefront UI in the deployed nopCommerce theme
- [ ] add scheduled/manual product-sync trigger
- [ ] add scheduled/manual inventory-sync trigger
- [ ] run end-to-end paid nop order → Mercato transaction in a deployed environment
- [ ] Docker deployment verification
- [ ] production-readiness/security review

## 13. Validation status

Backend CI has reached successful restore, build, and tests. CI also checks out official nopCommerce `release-4.90.7` and compiles version-specific adapters. Integration changes must remain green against that exact source before a batch is considered complete.

## 14. Known policy decisions

Known: Mercato is product/pricing/inventory/business authority; nopCommerce is storefront/cart/checkout/payment-gateway/shipping authority; target nopCommerce release is 4.90.7 / .NET 9; artist settlement is purchase-cost based; inventory is ledger based; sales create accounting events; checkout is atomic and retry-safe; returns cannot exceed sold quantity.

Do not invent unresolved tax, discount, GL, fiscal receipt, settlement approval, or payment-provider rules.

## 15. Update policy

Update this document whenever implementation status, architecture, or a business rule changes. Future developers and AI agents must inspect the repository before trusting stale unchecked items.
