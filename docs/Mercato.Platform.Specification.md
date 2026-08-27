# Mercato Platform Specification

Version: 2.0
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

Target release: **nopCommerce 4.90.7** (`release-4.90.7`) on **.NET 9**.

The five concrete Mercato plugin projects now follow nopCommerce 4.90.7's native plugin conventions rather than only compiling against nopCommerce APIs:
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
- CI checks out official nopCommerce `release-4.90.7` and compiles the concrete plugins against it;
- concrete builds run nopCommerce's own plugin-assembly cleanup target;
- CI packages the cleaned native `Nop.Web/Plugins/Mercato.*` directories as artifact `mercato-nopcommerce-4.90.7-plugins` rather than reconstructing plugin folders manually;
- a prior native-package run completed backend build/tests, all five concrete plugin builds, staging, and artifact upload successfully; newer nop-native configuration refinements must remain green before being considered validated.

Remaining nopCommerce work is deployed-environment end-to-end verification and optional richer synchronization/admin operations. The project-file, dependency, configuration, routing, widget, product-mapping, scheduled-sync, and paid-order adapter structures are implemented.

## 10. Database initialization

No EF migrations currently exist. Startup uses `MigrateAsync` when migrations exist and otherwise `EnsureCreatedAsync` for a fresh deployment. Before upgrading an existing production database, create and review a baseline/current EF migration; `EnsureCreated` is not a schema-upgrade strategy.

## 11. Resolved technical debt

Resolved: checkout DTO/type shadowing, obsolete checkout workflow stub, no-op inventory repository, no-op UnitOfWork, placeholder branch/invoice/transfer/settlement APIs, invalid EF mappings, in-memory-only order/invoice persistence, client-trusted POS prices, invalid unattributed settlement creation, missing checkout retry protection, broken central NuGet package management, missing Infrastructure → Application project reference, non-native nopCommerce plugin output/cleanup conventions, duplicated Mercato API-client registration across nop plugins, product identity stored in admin comments, ad-hoc branch endpoint routing, and non-standard widget invocation signature.

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
- [x] nopCommerce-native plugin project output/cleanup conventions
- [x] nopCommerce plugin dependency metadata and shared Connector ownership
- [x] Connector nopCommerce settings and admin configuration page
- [x] concrete product gateway with generic-attribute Mercato identity
- [x] concrete inventory gateway
- [x] storefront branch selector, nop-native widget signature/routing, and customer branch persistence
- [x] paid-order event consumer and product reverse mapping
- [x] automatic scheduled product synchronization
- [x] automatic scheduled inventory synchronization
- [x] CI configured to compile/package against official nopCommerce 4.90.7 source

Business/policy decisions intentionally unresolved:
- [ ] final POS payment methods and method-specific fields
- [ ] tax jurisdiction/calculation/posting rules
- [ ] POS discount/coupon authority and rules
- [ ] chart of accounts / double-entry GL decision
- [ ] settlement approval and external-payment metadata
- [ ] fiscal/legal receipt requirements beyond current durable receipt payload
- [ ] production EF migrations for schema upgrades

Integration/deployment work still required:
- [ ] run end-to-end paid nop order → Mercato transaction in a deployed environment
- [ ] verify scheduled sync against a running nopCommerce instance
- [ ] install/configure the packaged plugins in a running nopCommerce 4.90.7 instance and verify admin configuration view/runtime routing
- [ ] Docker deployment verification
- [ ] production-readiness/security review

## 13. Validation status

Backend CI has successful restore, Release build, and tests. A completed native-plugin packaging run also successfully built all five plugins against official nopCommerce 4.90.7, staged their cleaned nopCommerce plugin directories, and uploaded the plugin artifact. Subsequent nopCommerce-native settings/configuration changes are validated through the same exact-source CI pipeline; a change is not considered fully validated until that run is green.

## 14. Known policy decisions

Known: Mercato is product/pricing/inventory/business authority; nopCommerce is storefront/cart/checkout/payment-gateway/shipping authority; target nopCommerce release is 4.90.7 / .NET 9; artist settlement is purchase-cost based; inventory is ledger based; sales create accounting events; checkout is atomic and retry-safe; returns cannot exceed sold quantity.

Do not invent unresolved tax, discount, GL, fiscal receipt, settlement approval, or payment-provider rules.

## 15. Update policy

Update this document whenever implementation status, architecture, or a business rule changes. Future developers and AI agents must inspect the repository before trusting stale unchecked items.
