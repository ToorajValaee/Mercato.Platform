# Mercato nopCommerce Integration

Target: **nopCommerce 4.90.7** (`release-4.90.7`), running on **.NET 9**.

This directory contains the business core and the nopCommerce plugin adapter boundary for the five Mercato plugins.

- `Mercato.Connector.Plugin` checks Mercato API connectivity and owns connection configuration.
- `Mercato.ProductSync.Plugin` reads the Mercato catalog and upserts products through an `INopProductGateway` adapter.
- `Mercato.InventorySync.Plugin` reads branch-specific Mercato availability and updates nopCommerce stock through an `INopInventoryGateway` adapter.
- `Mercato.BranchSelector.Plugin` exposes branch-specific availability data for storefront branch selection.
- `Mercato.OrderSync.Plugin` sends completed nopCommerce orders to Mercato using the stable idempotency key `nop:{OrderId}`.

`Mercato.NopCommerce.Core` contains the HTTP client and integration contracts shared by the plugins.

## Authority boundary

Mercato remains authoritative for products, prices, branches, inventory, accounting, and artist settlement. nopCommerce remains authoritative for storefront presentation, SEO, cart, checkout UI, payment gateways, and shipping providers.

## 4.90.7 binding

All integration projects target `net9.0`, matching nopCommerce 4.90.x. Each plugin includes a nopCommerce `BasePlugin` adapter and a `plugin.json` declaring `SupportedVersions: [ "4.90" ]` and a 4.90.7 plugin build version.

The projects can still build their version-independent business core without a nopCommerce checkout. To compile the concrete nopCommerce adapter, pass the root of a nopCommerce 4.90.7 source checkout:

```bash
dotnet build integrations/nopCommerce/Mercato.Connector.Plugin/Mercato.Connector.Plugin.csproj \
  -p:NopCommerceRoot=/path/to/nopCommerce-4.90.7
```

The same `NopCommerceRoot` property applies to the other four plugin projects. The project then references `src/Presentation/Nop.Web/Nop.Web.csproj` from that checkout, so adapter compilation is checked against the actual 4.90.7 API surface rather than guessed packages.

## Deployment

Build each plugin against nopCommerce 4.90.7, then copy its output (including `plugin.json`) into the corresponding nopCommerce `Presentation/Nop.Web/Plugins/<SystemName>` directory. Keep Mercato API credentials/configuration outside source control.
