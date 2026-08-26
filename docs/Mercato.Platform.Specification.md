# Mercato Platform Specification

Version: 1.6
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

The backend stays consolidated in the existing Clean Architecture projects. Do not split business modules into separate .NET projects without a concrete need.

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

Rules:
- client-supplied prices are not trusted;
- `IdempotencyKey` is required and limited to 100 characters;
- sequential or concurrent retries with the same key cannot create a second committed checkout;
- non-empty `CustomerId` must identify a real Mercato customer;
- `Guid.Empty` remains an unlinked/guest checkout customer value because the current invoice model uses a non-nullable Guid;
- the sale transaction is atomic across order, inventory, invoice, settlement, payment, accounting, and idempotency persistence.

## 4. POS return/refund flow

```text
Original Order
→ validate returned products and quantities against sold quantities
→ prevent cumulative over-return
→ SalesReturn + SalesReturnLines
→ inventory ledger return movements
→ purchase-cost artist settlement reversal lines
→ negative Payment(Type=Refund)
→ AccountingTransaction(Type=Refund)
→ atomic commit
```

Return values are calculated from original order line prices, not current product sale prices. Artist settlement reversals use current product ownership/purchase-cost metadata and negative settlement quantities/amounts.

API: `POST /api/pos/returns`.

## 5. Inventory engine

Implemented:
- EF-backed immutable-style stock movement ledger;
- available quantity calculated from ledger totals by product and branch;
- stock adjustments with whole-unit validation and reason/audit text;
- sale deductions and return additions;
- branch transfers with source-stock validation;
- persisted `BranchTransfer` records;
- stock movement history querying;
- Admin/Manager adjustment and transfer authorization.

Mercato remains the only inventory authority. nopCommerce receives availability snapshots only.

## 6. Master-data modules

Implemented application/infrastructure/API support now covers:
- Products: create/update/list/archive, SKU, purchase price, sale price, category reference, optional artist reference;
- Categories: CRUD plus parent hierarchy validation;
- Artists: CRUD plus protection against deleting referenced artists;
- Branches: CRUD;
- Customers: create/update/list/get;
- Catalog: Mercato product catalog with optional branch-specific availability;
- Invoices: persisted creation and query APIs.

Product category/artist references are validated against Mercato records before persistence.

## 7. Artist settlement

On checkout, artist-owned products create `SettlementLine` entries using `PurchasePrice × QuantitySold`.

On return, equivalent negative settlement lines reverse purchase-cost liability.

Settlement summaries:
- aggregate one artist and explicit UTC period;
- are persisted as `ArtistSettlement`;
- are unique per artist/period;
- track paid/unpaid state and `PaidAtUtc`;
- can be listed and filtered;
- can be marked paid by Admin/Manager.

Marking a settlement paid atomically creates an `AccountingTransaction` with type `ArtistSettlementPayment` and a negative amount. External bank/cash transfer metadata and approval workflow remain policy decisions.

## 8. Accounting

Implemented accounting event ledger:
- completed sale → positive `AccountingTransaction(Type=Sale)`;
- return/refund → negative `AccountingTransaction(Type=Refund)`;
- paid artist settlement → negative `AccountingTransaction(Type=ArtistSettlementPayment)`;
- transactions can reference Order, Invoice, Branch, or ArtistSettlement as appropriate;
- reporting API filters by branch, period, and transaction type;
- summary reports GrossSales, Refunds, NetSales, ArtistSettlementPayments, NetCashMovement, and transaction count.

APIs:
- `GET /api/accounting/transactions`
- `GET /api/accounting/summary`

This is a durable accounting event ledger, not a finalized double-entry general ledger. Chart of accounts, tax accounts, and jurisdiction-specific posting rules require explicit accounting policy before implementation.

## 9. nopCommerce integration

Version-agnostic integration core is implemented under `integrations/nopCommerce`:
- `Mercato.NopCommerce.Core`: authenticated HTTP client, health, catalog, idempotent order synchronization;
- `Mercato.Connector.Plugin`: connection/health core;
- `Mercato.ProductSync.Plugin`: Mercato catalog → `INopProductGateway` upsert workflow;
- `Mercato.InventorySync.Plugin`: branch availability → `INopInventoryGateway` stock workflow;
- `Mercato.BranchSelector.Plugin`: branch-specific availability workflow;
- `Mercato.OrderSync.Plugin`: completed nopCommerce order → Mercato checkout flow using `nop:{OrderId}` idempotency key.

The remaining thin nopCommerce adapter layer is intentionally not bound to concrete nop assemblies until the exact target nopCommerce release is selected. That adapter must implement the two gateway interfaces, plugin registration classes, event consumers/admin configuration pages, and version-specific `plugin.json` metadata.

## 10. Database initialization

No EF migrations currently exist in the repository. Startup now:
- uses `MigrateAsync` when migrations exist;
- otherwise uses `EnsureCreatedAsync` so a fresh deployment can create the current schema.

Before upgrading an existing production database, create and review a baseline/current EF migration; `EnsureCreated` is not a schema-upgrade strategy.

## 11. Removed technical debt

Resolved during development:
- obsolete `Mercato.Application.Services.CheckoutResult` type that shadowed the real DTO and could break compilation;
- obsolete checkout workflow stub;
- no-op inventory repository;
- no-op UnitOfWork;
- placeholder branches/invoices/transfers/settlements APIs;
- invalid Product and SettlementLine EF property mappings;
- in-memory-only order/invoice persistence;
- client-trusted POS UnitPrice;
- invalid unattributed settlement creation;
- missing checkout retry protection.

## 12. Development status

### Implemented core platform development
- [x] Product management
- [x] Category management
- [x] Artist management
- [x] Branch management
- [x] Customer management
- [x] Inventory ledger and adjustments
- [x] Branch transfers
- [x] Catalog data API
- [x] POS checkout
- [x] Checkout atomicity
- [x] Checkout idempotency
- [x] POS printable receipt data
- [x] POS authorization roles
- [x] POS returns/refunds
- [x] Return inventory reversal
- [x] Return settlement reversal
- [x] Sale/refund accounting event capture
- [x] Artist settlement aggregation/payment state
- [x] Settlement payment accounting event
- [x] Accounting transaction/reporting API
- [x] nopCommerce version-agnostic connector core
- [x] Product sync core
- [x] Inventory sync core
- [x] Branch selector core
- [x] Order sync core

### Blocked by explicit deployment/business decisions
- [ ] Bind thin plugin adapters to the selected nopCommerce version.
- [ ] Define final supported POS payment methods and method-specific fields (cash tender/change, card authorization references, etc.).
- [ ] Define tax jurisdiction/calculation/posting rules.
- [ ] Define POS discount/coupon authority and rules.
- [ ] Define chart of accounts and decide whether full double-entry GL is required.
- [ ] Define settlement approval/external-payment metadata requirements.
- [ ] Define fiscal/legal receipt requirements beyond current durable reference and printable payload.
- [ ] Generate/review EF migrations for upgradeable production databases.

### Validation phase after development
- [ ] Build all backend projects.
- [ ] Build version-agnostic nopCommerce integration projects.
- [ ] Add/fix unit tests.
- [ ] Add integration/API tests.
- [ ] Exercise concurrent stock checkout behavior.
- [ ] Verify Docker deployment.
- [ ] Verify CI.
- [ ] Production-readiness/security review.

## 13. Known policy decisions

Known:
- Mercato is product/pricing/inventory/business authority.
- nopCommerce is storefront/cart/checkout/payment-gateway/shipping authority.
- artist settlement is purchase-cost based.
- inventory is ledger based.
- sales create accounting transactions.
- checkout must be retry-safe and atomic.
- returns cannot exceed original sold quantity.

Unknown and must not be invented by future developers/AI:
- exact nopCommerce target release;
- jurisdiction-specific tax rules;
- discount policy;
- final chart of accounts/double-entry rules;
- fiscal receipt rules;
- settlement approval and transfer method rules;
- payment-provider-specific metadata requirements.

## 14. Update policy

Update this document whenever implementation status, architecture, or a business rule changes. Future developers and AI agents should inspect the repository before trusting an unchecked item because code may have advanced since the last document edit.
