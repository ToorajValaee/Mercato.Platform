# Mercato nopCommerce Integration

Target: **nopCommerce 4.90.7** (`release-4.90.7`), running on **.NET 9**.

This directory contains the Mercato integration core and concrete nopCommerce 4.90.7 adapters.

- `Mercato.Connector.Plugin` registers the Mercato API client and provides connection/health functionality.
- `Mercato.ProductSync.Plugin` reads the Mercato catalog and upserts nopCommerce products using authoritative Mercato names/prices/SKUs. It also stamps `Mercato.ProductId=<guid>` into the nop product admin metadata so downstream order sync can map nop products back to Mercato reliably.
- `Mercato.InventorySync.Plugin` reads branch-specific Mercato availability and writes nopCommerce `StockQuantity`; Mercato remains the inventory authority.
- `Mercato.BranchSelector.Plugin` exposes branch availability and persists the selected Mercato branch in nopCommerce customer generic attributes under `Mercato.BranchId`.
- `Mercato.OrderSync.Plugin` consumes nopCommerce `OrderPaidEvent`, resolves the selected/default Mercato branch, maps order lines back to Mercato product IDs, and sends the order through Mercato's idempotent checkout pipeline using `nop:{nopOrderId}`.

`Mercato.NopCommerce.Core` contains the HTTP client and integration contracts shared by the plugins.

## Authority boundary

Mercato remains authoritative for products, prices, branches, inventory, accounting, and artist settlement. nopCommerce remains authoritative for storefront presentation, SEO, cart, checkout UI, payment gateways, and shipping providers.

## Configuration

The concrete adapters read these nopCommerce configuration keys:

- `Mercato:BaseUrl` — required Mercato API base URL.
- `Mercato:BearerToken` — bearer token used by the nopCommerce server when calling Mercato.
- `Mercato:DefaultBranchId` — optional fallback branch for paid orders when no branch selection is stored on the nop customer/order.

Keep credentials outside source control.

## 4.90.7 binding

All integration projects target `net9.0`, matching nopCommerce 4.90.x. Each plugin includes a nopCommerce `BasePlugin` adapter and a `plugin.json` declaring `SupportedVersions: [ "4.90" ]` and a 4.90.7 plugin build version.

The projects can still build their version-independent business core without a nopCommerce checkout. To compile the concrete nopCommerce adapter, pass the root of a nopCommerce 4.90.7 source checkout:

```bash
dotnet build integrations/nopCommerce/Mercato.Connector.Plugin/Mercato.Connector.Plugin.csproj \
  -p:NopCommerceRoot=/path/to/nopCommerce-4.90.7
```

The same `NopCommerceRoot` property applies to the other four plugin projects. CI checks out the official `release-4.90.7` source and compiles all adapters against `src/Presentation/Nop.Web/Nop.Web.csproj`.

## Current integration behavior

Product synchronization is SKU-based. When a Mercato product has no SKU, the adapter uses the stable fallback `MERCATO-{ProductId:N}`. Inventory synchronization uses the same catalog SKU identity, so stock updates resolve the same nop product created by product sync.

For paid orders, `Mercato.OrderSync.Plugin` reads `Mercato.BranchId` and optional `Mercato.CustomerId` generic attributes. Branch mapping falls back to `Mercato:DefaultBranchId`. Unmapped products or a missing branch cause order synchronization to fail loudly and write an error to the nopCommerce log instead of silently creating incorrect Mercato transactions.

## Deployment

Build each plugin against nopCommerce 4.90.7, then copy its output (including `plugin.json`) into the corresponding nopCommerce `Presentation/Nop.Web/Plugins/<SystemName>` directory. Configure Mercato API connectivity in the nopCommerce host configuration.
