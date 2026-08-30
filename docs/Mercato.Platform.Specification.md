# Mercato Platform Specification

Version: 2.0
Purpose: Source of truth for product requirements, architecture, business rules, implementation status, known gaps, and continuation work.

## 1. Product boundary

Mercato is the business brain. It owns products, categories, artists, branches, customers, authoritative pricing, inventory, POS, orders, invoices, accounting transaction capture, catalog data, returns, staff roles, and artist settlement.

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

### 2.1 Built-in Mercato operator UI

The Mercato API also serves dependency-free operator interfaces from `wwwroot`; these are clients of the same authenticated APIs and do not duplicate business rules:

- `/` — workspace launcher;
- `/pos/` — focused cashier application;
- `/admin/` — role-aware Back Office.

The Back Office covers dashboard, products, categories, artists, branches, customers, branch inventory, whole-unit adjustments, branch transfers, movement history, invoice history/detail, order lookup, artist settlements, accounting reporting, and Admin-only staff management.

Staff administration is server-authorized through `api/staff`: Admin can list staff, create Admin/Manager/Cashier accounts, change role, optionally reset password, and delete accounts. The API prevents an Admin from deleting their own account or removing their own Admin role. UI visibility is convenience only; API authorization remains authoritative.

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

Target release: **nopCommerce 4.90.7** (`release-4.90.7`) on **.NET 9**.

Further nopCommerce feature development is intentionally paused until the user accepts the Core/Back Office/POS milestone. Existing plugin code is retained and its regression workflow is manual-only during this acceptance phase.

The five concrete Mercato plugin projects follow nopCommerce 4.90.7's native plugin conventions:
- target `net9.0`;
- reference the exact 4.90.7 `Nop.Web.csproj` when `NopCommerceRoot` is supplied;
- output directly to `Nop.Web/Plugins/Mercato.*`;
- use `CopyLocalLockFileAssemblies=false`;
- copy `plugin.json` and required views as plugin content;
- invoke nopCommerce `Build/ClearPluginAssemblies.proj` after build;
- register through `INopStartup`;
- use `BasePlugin` plus `IMiscPlugin` for generic integration plugins and `IWidgetPlugin` for BranchSelector;
- use nopCommerce dependency metadata, generic attributes, settings, routing, and widget conventions.

Plugin responsibilities:
- `Mercato.NopCommerce.Core`: shared Mercato HTTP client, contracts, mapping/configuration keys, and idempotent commerce-order synchronization;
- `Mercato.Connector.Plugin`: shared Mercato API client registration, connection/health core, nopCommerce settings, and admin configuration page;
- `Mercato.ProductSync.Plugin`: Mercato catalog → concrete nopCommerce product upsert;
- `Mercato.InventorySync.Plugin`: branch-specific Mercato availability → concrete nopCommerce stock snapshot;
- `Mercato.BranchSelector.Plugin`: storefront branch selector plus persisted customer branch selection;
- `Mercato.OrderSync.Plugin`: nopCommerce `OrderPaidEvent` → Mercato checkout.

Dependency rules:
- ProductSync, InventorySync, BranchSelector, and OrderSync declare `DependsOnSystemNames: ["Mercato.Connector"]`;
- Connector owns the shared `MercatoApiClient` and connector configuration;
- dependent plugins consume that shared registration instead of creating competing API clients/configuration instances.

Connector configuration:
- primary configuration is stored through nopCommerce `ISettingService` on the Connector admin configuration page;
- settings are Mercato API base URL, bearer token, and optional default branch ID;
- host configuration keys `Mercato:BaseUrl`, `Mercato:BearerToken`, and `Mercato:DefaultBranchId` remain fallback sources when corresponding plugin settings are empty;
- the default branch is used by scheduled inventory synchronization and as the paid-order branch fallback.

Concrete adapter rules:
- product sync uses Mercato SKU, or stable fallback `MERCATO-{ProductId:N}` when SKU is absent;
- synchronized nop products store Mercato identity in nopCommerce generic attribute `Mercato.ProductId` rather than overwriting `Product.AdminComment`;
- OrderSync still reads legacy `Mercato.ProductId=<guid>` admin-comment mappings as a migration fallback;
- product name and price are overwritten from Mercato so Mercato remains product/pricing authority;
- inventory sync writes nop `StockQuantity` from Mercato availability while Mercato remains stock authority;
- selected branch is stored in nop customer generic attribute `Mercato.BranchId`;
- optional Mercato customer mapping uses `Mercato.CustomerId`;
- BranchSelector implements nopCommerce `IWidgetPlugin`, renders in `PublicWidgetZones.HeaderSelectors`, and its component inherits `NopViewComponent` with the standard `(widgetZone, additionalData)` invocation signature;
- the branch-selection endpoint is registered using nopCommerce `IRouteProvider` and retains antiforgery validation;
- paid-order synchronization consumes `OrderPaidEvent`, resolves branch from order/customer attributes then Connector default branch, and maps order lines through generic product attributes;
- order synchronization uses idempotency key `nop:{nopOrderId}`;
- missing branch or unmapped products fail loudly and are logged in nopCommerce instead of creating incorrect Mercato transactions.

Synchronization triggers:
- ProductSync installs an enabled nopCommerce `IScheduleTask` named `Mercato product synchronization`, default period 900 seconds;
- InventorySync installs an enabled nopCommerce `IScheduleTask` named `Mercato inventory synchronization`, default period 300 seconds;
- scheduled inventory synchronization uses Connector-managed default branch configuration;
- plugin uninstall removes the corresponding schedule task.

Packaging and validation:
- the manual nopCommerce regression workflow checks out official nopCommerce `release-4.90.7` and compiles the concrete plugins against it;
- concrete builds run nopCommerce's own plugin-assembly cleanup target;
- the workflow packages the cleaned native `Nop.Web/Plugins/Mercato.*` directories as artifact `mercato-nopcommerce-4.90.7-plugins` rather than reconstructing plugin folders manually;
- a prior native-package run completed backend build/tests, all five concrete plugin builds, staging, and artifact upload successfully.

Remaining nopCommerce work is deployed-environment end-to-end verification and richer synchronization/runtime acceptance after Mercato UI acceptance.

## 10. Database initialization

No EF migrations currently exist. Startup uses migrations when migrations exist and otherwise `EnsureCreated` for a fresh deployment. Local Docker initialization must not pre-create application tables or marker tables before `EnsureCreated`, because any pre-existing user table causes EF to treat the database as non-empty and skip schema creation. Before upgrading an existing production database, create and review a baseline/current EF migration; `EnsureCreated` is not a schema-upgrade strategy.

The runtime image includes the GSSAPI/Kerberos dependency required by Npgsql (`libgssapi-krb5-2`).

## 11. Resolved technical debt

Resolved: checkout DTO/type shadowing, obsolete checkout workflow stub, no-op inventory repository, no-op UnitOfWork, placeholder branch/invoice/transfer/settlement APIs, in-memory-only order/invoice persistence, client-trusted POS prices, invalid unattributed settlement creation, missing checkout retry protection, broken central NuGet package management, missing Infrastructure → Application project reference, partial Docker schema bootstrap conflicting with EF `EnsureCreated`, missing Npgsql GSSAPI runtime dependency, Docker build context missing test projects referenced by the solution, missing staff administration path, and lack of usable Mercato operator/back-office UI.

A non-blocking EF model warning remains: `System.Object` is detected and then ignored during model creation. It does not prevent schema creation/runtime acceptance, but its originating model convention should be removed in later cleanup rather than merely ignored.

## 12. Development status

Implemented core platform development:
- [x] Product/category/artist/branch/customer management
- [x] Inventory ledger, adjustments, transfers, movement history
- [x] Catalog data API
- [x] Staff JWT authentication and Admin/Manager/Cashier authorization
- [x] Bootstrap Admin configuration for clean/local deployments
- [x] Admin-only staff list/create/role-change/password-reset/delete API with self-protection
- [x] Workspace launcher at `/`
- [x] Back Office at `/admin/`
- [x] Back Office product/category/artist/branch/customer management
- [x] Back Office inventory availability/adjustment/transfer/movement history
- [x] Back Office invoice history/detail and order lookup
- [x] Back Office artist settlement and accounting reporting
- [x] Back Office Admin-only staff management
- [x] POS at `/pos/`
- [x] POS checkout, atomicity, idempotency, receipt data, authorization roles
- [x] POS order lookup and remaining-returnable quantity
- [x] POS returns/refunds and inventory/settlement/accounting reversals
- [x] Artist settlement aggregation/payment state/accounting event
- [x] Accounting transaction/reporting API
- [x] Docker Compose local acceptance stack with opt-in development demo data
- [x] Production-like Docker/PostgreSQL HTTP acceptance covering Back Office and POS
- [x] nopCommerce target locked to 4.90.7
- [x] existing nopCommerce plugin project/output/dependency/connector/product/inventory/branch/order-sync structures retained
- [x] nopCommerce regression workflow separated to manual-only while Mercato UI is being accepted

Business/policy decisions intentionally unresolved:
- [ ] final POS payment methods and method-specific fields
- [ ] tax jurisdiction/calculation/posting rules
- [ ] POS discount/coupon authority and rules
- [ ] chart of accounts / double-entry GL decision
- [ ] settlement approval and external-payment metadata
- [ ] fiscal/legal receipt requirements beyond current durable receipt payload
- [ ] production EF migrations for schema upgrades

Integration/deployment work still required:
- [ ] user hands-on acceptance of Core/Back Office/POS
- [ ] production-readiness/security review
- [ ] production EF migration baseline before real schema upgrades
- [ ] resume nopCommerce runtime/end-to-end work after Mercato acceptance

## 13. Validation status

Core/Back Office/POS run `33282781177` completed successfully on August 30, 2026. It passed Release build, production-like Docker image build, PostgreSQL fresh-schema creation, bootstrap Admin/demo data, root workspace landing, Back Office rendering, live Admin staff list/create/role-change/delete workflow, POS JWT login, branch catalog, completed sale, idempotent replay, exact stock deduction, order lookup, partial return, exact stock restoration, and the full backend test suite.

The normal CI pipeline now validates Core/Back Office/POS only. nopCommerce regression is intentionally available as a separate manual workflow until the user accepts Mercato and asks to resume nopCommerce development.

## 14. Known policy decisions

Known: Mercato is product/pricing/inventory/business authority; nopCommerce is storefront/cart/checkout/payment-gateway/shipping authority; target nopCommerce release is 4.90.7 / .NET 9; artist settlement is purchase-cost based; inventory is ledger based; sales create accounting events; checkout is atomic and retry-safe; returns cannot exceed sold quantity.

Do not invent unresolved tax, discount, GL, fiscal receipt, settlement approval, or payment-provider rules.

## 15. Update policy

Update this document whenever implementation status, architecture, operator UI, or a business rule changes. Future developers and AI agents must inspect the repository before trusting stale unchecked items.
