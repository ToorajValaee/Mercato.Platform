# Mercato Platform Specification

Version: 1.5
Purpose: Source of truth for product requirements, business flows, architecture decisions, implementation status, known gaps, and remaining development work.

## 1. Vision

Mercato is the ERP and business operating platform. It owns business logic, inventory, products, branches, artists, accounting, POS, catalog generation, and settlement capabilities.

nopCommerce is the external commerce engine.

## 2. Responsibility Boundary

### Mercato owns

- ERP Core
- Inventory Engine
- Product management
- Branch management
- Artist management
- Accounting
- POS
- Catalog generation
- Artist settlement
- Business rules
- Synchronization services

### nopCommerce owns

- Storefront
- SEO
- Cart
- Checkout UI
- Payment gateways
- Shipping providers

## 3. Target Architecture

```text
Mercato.Api
    |
Mercato.Application
    |
Mercato.Domain
    |
Mercato.Infrastructure

Business modules:
- Inventory
- Products
- Branches
- Artists
- Accounting
- POS
- Catalog
- NopCommerce integration
```

The current implementation uses a consolidated Clean Architecture solution rather than separate .NET projects for each business module. Do not split modules into separate projects unless a concrete technical need appears.

## 4. Customer UX Flow

### Online customer

1. Customer visits storefront.
2. Customer browses catalog generated from Mercato products.
3. Customer views product availability.
4. Customer selects branch/location if required.
5. Customer adds products to cart.
6. nopCommerce manages cart and checkout.
7. Order is synchronized back to Mercato.
8. Mercato updates inventory.
9. Accounting receives the transaction.
10. Artist settlement data is generated when the sold product belongs to an artist.

## 5. POS UX Flow

### Staff workflow

1. Staff logs into POS.
2. Select branch.
3. Search or scan products.
4. Check branch inventory.
5. Add products to cart.
6. Select payment method.
7. Generate a unique checkout idempotency key for the sale attempt.
8. Checkout using server-authoritative product prices.
9. Persist Order and OrderItems.
10. Reduce inventory.
11. Persist Invoice linked to the Order.
12. Record artist settlement lines using product purchase cost.
13. Persist Payment.
14. Post AccountingTransaction for the sale.
15. Persist the completed checkout result against the idempotency key.
16. Commit the complete checkout transaction atomically.
17. Return a printable receipt payload containing order, invoice, payment, branch, payment method, timestamp, total, durable receipt reference, and line-level product/quantity/price totals.
18. If the same idempotency key is retried, return the original completed checkout result rather than creating a second sale.

## 6. Core Business Modules

### Inventory Engine

Requirements:

- Stock tracking
- Branch stock
- Availability calculation
- Reservation support
- Stock movement history
- Branch transfers
- Sale deductions through inventory ledger/services

### Products

Requirements:

- Product master data
- SKU
- Purchase price
- Sale price
- Categories
- Optional artist ownership
- Images
- Synchronization to storefront

Business rule: POS and integrations must not trust sale price supplied by clients. Mercato resolves authoritative pricing from product data.

### Branches

Requirements:

- Multiple locations
- Branch inventory ownership
- Branch selection logic

### Artists

Requirements:

- Artist profiles
- Optional product ownership (`Product.ArtistId`)
- Purchase-cost based settlement tracking
- Settlement reporting and payment status

Business rule: artist products are tracked by purchase cost, not revenue-sharing percentage. On sale, settlement data records `PurchasePrice × QuantitySold` for the product's artist.

Settlement summaries aggregate sale settlement lines for one artist and one explicit UTC period. The same artist/period can only produce one persisted summary. Marking a settlement paid records `PaidAtUtc`; actual money-transfer method and approval schedule remain business decisions.

### Accounting

Requirements:

- Sales records
- Invoices
- Accounting transactions/journal postings
- Payment records
- Financial reports

Current sale posting model persists one `AccountingTransaction` per completed POS checkout with OrderId, InvoiceId, BranchId, Amount, Type, Description, and timestamp. This is a transaction/event record, not yet a full double-entry general ledger.

### POS

Requirements:

- Fast sales workflow
- Branch operation
- Server-authoritative pricing
- Inventory integration
- Order and line-item persistence
- Invoice creation
- Artist settlement recording
- Payment handling
- Printable receipt response
- Atomic checkout transaction
- Retry-safe checkout idempotency
- Offline-ready consideration

Idempotency rule: every POS checkout request must provide an `IdempotencyKey` of at most 100 characters. Mercato stores the successful `CheckoutResult` using a unique database key. Sequential retries return the stored result. Concurrent requests with the same key are protected by the unique index; the losing transaction rolls back and then returns the already committed result.

### Catalog

Requirements:

- Catalog generation
- Storefront publishing
- Product presentation

## 7. nopCommerce Plugins

The plugins are integration modules, not generic extensions.

Structure:

```text
Mercato.NopCommerce
 |
 |- Mercato.Connector.Plugin
 |- Mercato.ProductSync.Plugin
 |- Mercato.InventorySync.Plugin
 |- Mercato.BranchSelector.Plugin
 |- Mercato.OrderSync.Plugin
```

### Connector Plugin

Responsibilities:

- API connection
- Authentication
- Configuration
- Health checks

### ProductSync Plugin

Responsibilities:

- Sync Mercato products to nopCommerce
- Maintain Mercato as product authority

### InventorySync Plugin

Responsibilities:

- Sync stock availability
- Prevent inventory ownership duplication

### BranchSelector Plugin

Responsibilities:

- Branch selection
- Location-aware inventory

### OrderSync Plugin

Responsibilities:

- Send completed nopCommerce orders to Mercato
- Trigger Mercato inventory, accounting, and settlement flows

## 8. Development Audit Rules

Before considering the platform complete, verify:

- Every business flow has an implementation.
- Every module has domain, application, infrastructure, and API support where needed.
- No business rule exists only in the storefront.
- Inventory remains owned by Mercato.
- Product pricing remains authoritative in Mercato.
- Accounting receives all sale/payment events.
- Settlement receives valid artist/product/order attribution.
- Financial records are persisted, not only returned in memory.
- End-to-end flows are transactionally safe.
- Retried checkout requests cannot create duplicate sales.

## 9. Current Implementation Status

### POS checkout backend

Implemented:

- [x] Checkout request contract
- [x] Server-side stock validation
- [x] Server-side product price resolution
- [x] Order creation
- [x] OrderItem creation and persistence
- [x] Inventory deduction
- [x] Invoice creation and persistence
- [x] Invoice-to-Order link
- [x] Product-to-Artist ownership field
- [x] Purchase-cost settlement line persistence
- [x] Payment method required by checkout request
- [x] Payment persistence linked to Order
- [x] Durable unique receipt/payment reference
- [x] AccountingTransaction persistence for completed sale
- [x] EF transaction boundary across order, stock, invoice, settlement, payment, and accounting operations
- [x] POS checkout API endpoint: `POST /api/pos/checkout`
- [x] Printable checkout receipt payload with BranchId, PaymentMethod, PaidAtUtc, durable reference, and line-level ProductId/ProductName/Quantity/UnitPrice/LineTotal data
- [x] Required checkout IdempotencyKey with 100-character limit
- [x] Persisted successful CheckoutResult for retry replay
- [x] Unique database constraint prevents duplicate committed checkouts for the same idempotency key
- [x] Transaction rollback clears EF tracking so a concurrent idempotency conflict can safely reload the committed result

Still required:

- [ ] POS-specific authorization roles/policies
- [ ] Customer optionality/guest sale rules verification
- [ ] Cash tender/change handling if required
- [ ] Card/gateway authorization metadata if required

### Accounting

Implemented:

- [x] Payment entity persisted through typed repository
- [x] AccountingTransaction entity persisted through typed repository
- [x] Completed POS sale creates accounting transaction
- [x] Sale transaction links Order, Invoice, Branch, and Amount

Still required:

- [ ] Define chart of accounts
- [ ] Implement double-entry journal model/posting rules if required
- [ ] Tax posting rules
- [ ] Refund/return accounting
- [ ] Settlement payable accounting
- [ ] Accounting reports

### Artist settlement

Implemented:

- [x] Product may reference an Artist
- [x] SettlementLine records OrderId, ArtistId, ProductId, QuantitySold, PurchaseAmount
- [x] Checkout records purchase-cost settlement lines only for artist-owned products
- [x] Settlement lines are persisted through EF repository
- [x] Settlement period aggregation by ArtistId and UTC period
- [x] ArtistSettlement summaries persisted through EF
- [x] Duplicate artist/period summaries prevented by unique database index and service lookup
- [x] Paid/unpaid state with PaidAtUtc timestamp
- [x] Settlement listing with artist and paid-status filters
- [x] Settlement API calculation endpoint
- [x] Settlement API mark-paid endpoint
- [x] Settlement API protected for Admin/Manager roles

Still required:

- [ ] Settlement payment approval workflow if required
- [ ] External/bank/cash settlement payment method tracking if required
- [ ] Settlement payable accounting posting
- [ ] Settlement reports/export

### Known technical issues discovered during development

Resolved:

- [x] Product EF mapping referenced a nonexistent `Price` property; now maps PurchasePrice and SalePrice.
- [x] SettlementLine EF mapping referenced a nonexistent `Amount` property; now maps PurchaseAmount.
- [x] Payment EF mapping was an empty `object` configuration; it now maps the real Payment entity.
- [x] Order and Invoice services previously returned in-memory objects without persistence; repository-backed persistence has been introduced.
- [x] Checkout previously trusted client UnitPrice; authoritative SalePrice is now loaded from Mercato product data.
- [x] Checkout previously generated invalid settlement summaries without a valid ArtistId; checkout now records attributable settlement lines instead.
- [x] UnitOfWork previously returned zero without calling EF; it now saves through MercatoDbContext and can execute an atomic EF transaction.
- [x] SettlementsController previously returned an empty placeholder array; it now exposes real settlement workflows.
- [x] ArtistSettlement previously had no settlement period or payment timestamp; period and payment-state audit fields are now modeled.
- [x] Checkout retries previously had no duplicate-sale protection; persisted idempotency result replay is now implemented.

To verify later:

- [ ] Database migrations match the current domain model.
- [ ] Build succeeds after all development batches are complete.
- [ ] Existing database schema migration strategy for ArtistId, SettlementLine.OrderId, Payment.Reference, AccountingTransaction, ArtistSettlement period/payment fields, and CheckoutIdempotencyRecord.
- [ ] Concurrent checkout/stock locking behavior under load beyond duplicate-key protection.

## 10. Remaining Work List

### Development completion

- [ ] Complete Inventory Engine
- [ ] Complete Product module
- [ ] Complete Branch module
- [ ] Complete Artist management module
- [ ] Complete Accounting module beyond sale transaction capture
- [ ] Complete POS authorization/payment-detail workflow
- [ ] Complete settlement payable accounting and reports
- [ ] Complete Catalog generator
- [ ] Complete nopCommerce connector plugin
- [ ] Complete product sync plugin
- [ ] Complete inventory sync plugin
- [ ] Complete branch selector plugin
- [ ] Complete order sync plugin
- [ ] Verify end-to-end customer flow
- [ ] Verify end-to-end POS flow

### Validation after development

- [ ] Add unit tests
- [ ] Add integration tests
- [ ] Add/fix API tests
- [ ] Fix CI pipeline
- [ ] Docker deployment verification
- [ ] Production readiness review

## 11. Knowledge / Unknowns

Known:

- Mercato is the business authority.
- nopCommerce is the commerce/storefront authority only for storefront, SEO, cart, checkout UI, payment gateways, and shipping integrations.
- Inventory is ledger/service driven in Mercato.
- Artist settlement uses purchase cost, not revenue sharing.
- POS checkout must use server-side prices.
- POS checkout records payment and accounting data atomically with the sale.
- POS checkout returns line-level receipt data suitable for a frontend/printer formatting layer.
- POS checkout requires an idempotency key and replays the original successful result for retried requests.
- Artist settlement summaries are period-based, persisted, and explicitly marked paid after aggregation.

Unknown or requiring explicit later decision:

- Final supported POS payment methods.
- Tax calculation rules and tax jurisdiction behavior.
- Discount/coupon authority for POS.
- Fiscal/legal receipt requirements beyond the current durable receipt reference and printable payload.
- Whether anonymous/guest POS customers are represented by `Guid.Empty`, nullable customer IDs, or a system customer.
- Settlement payment schedule, approval workflow, and external payment method.
- Accounting chart of accounts and double-entry posting rules.
- Exact nopCommerce version targeted by the plugins.

## 12. Update Policy

This document must be updated whenever a feature is completed, business rule changes, architecture changes, or an important unknown is resolved.

It is intended to be usable by future developers and AI agents as the reference source for requirements, implementation status, business rules, and remaining work.
