# Mercato nopCommerce Integration

Target: **nopCommerce 4.90.7** (`release-4.90.7`), running on **.NET 9**.

This directory contains the Mercato integration core and five concrete nopCommerce 4.90.7 plugins.

- `Mercato.Connector.Plugin` owns Mercato connectivity and shared plugin configuration.
- `Mercato.ProductSync.Plugin` reads the Mercato catalog and upserts nopCommerce products using authoritative Mercato names, prices, and SKUs.
- `Mercato.InventorySync.Plugin` reads branch-specific Mercato availability and writes nopCommerce `StockQuantity`; Mercato remains the inventory authority.
- `Mercato.BranchSelector.Plugin` renders a storefront branch selector and persists the selected Mercato branch in nopCommerce generic attributes.
- `Mercato.OrderSync.Plugin` consumes nopCommerce `OrderPaidEvent`, maps the paid order to Mercato, and uses Mercato's retry-safe checkout pipeline.

`Mercato.NopCommerce.Core` contains the HTTP client and integration contracts shared by the plugins.

## Authority boundary

Mercato remains authoritative for products, prices, branches, inventory, accounting, and artist settlement. nopCommerce remains authoritative for storefront presentation, SEO, cart, checkout UI, payment gateways, and shipping providers.

## nopCommerce plugin conventions

The plugin projects intentionally follow the nopCommerce 4.90.7 project layout and build conventions:

- target `net9.0`;
- reference the exact 4.90.7 `Nop.Web.csproj` when `NopCommerceRoot` is supplied;
- output directly to `Nop.Web/Plugins/Mercato.*`;
- set `CopyLocalLockFileAssemblies` to `false`;
- copy `plugin.json` and plugin views as content;
- run nopCommerce `Build/ClearPluginAssemblies.proj` after build;
- use `INopStartup`, `BasePlugin`, `IMiscPlugin` or `IWidgetPlugin` according to plugin type;
- use nopCommerce generic attributes for Mercato entity mappings;
- use `IRouteProvider` for the branch-selection endpoint;
- use `NopViewComponent` with the standard widget invocation signature;
- use nopCommerce `ISettingService` for connector configuration.

The four functional plugins declare `DependsOnSystemNames: [ "Mercato.Connector" ]`, so nopCommerce understands their dependency on the shared connector plugin.

## Configuration

Install and configure **Mercato Connector** first. Its nopCommerce admin configuration page stores these values through `ISettingService`:

- Mercato API base URL — required;
- bearer token — optional when the Mercato API does not require one;
- default Mercato branch ID — optional, used for scheduled inventory sync and as the paid-order branch fallback.

For environment-managed deployments, the Connector also supports these host-configuration fallbacks when the corresponding plugin setting is empty:

- `Mercato:BaseUrl`
- `Mercato:BearerToken`
- `Mercato:DefaultBranchId`

Keep credentials outside source control.

## Product and order identity

Product synchronization is SKU-based. When a Mercato product has no SKU, the adapter uses the stable fallback `MERCATO-{ProductId:N}`. Inventory synchronization uses the same catalog SKU identity.

Mercato product identity is stored using nopCommerce `IGenericAttributeService` under `Mercato.ProductId`. Order synchronization reads this generic attribute first. A legacy `Mercato.ProductId=<guid>` value in `Product.AdminComment` is still accepted as a migration fallback for products synchronized by older plugin builds.

Branch selection uses the generic attribute `Mercato.BranchId`. Optional customer mapping uses `Mercato.CustomerId`. Missing product or branch mappings fail loudly and are written to the nopCommerce log instead of silently creating an incorrect Mercato transaction.

Paid orders are sent to Mercato with idempotency key `nop:{nopOrderId}`.

## Automatic synchronization

`Mercato.ProductSync.Plugin` installs an enabled nopCommerce schedule task with a default period of 900 seconds. `Mercato.InventorySync.Plugin` installs an enabled schedule task with a default period of 300 seconds. Inventory synchronization uses the Connector's configured default branch. Uninstalling either plugin removes its task.

## 4.90.7 binding and build

The projects can build their version-independent core without a nopCommerce checkout. To compile the concrete plugin against the official source tree, pass the root of a nopCommerce 4.90.7 checkout:

```bash
dotnet build integrations/nopCommerce/Mercato.Connector.Plugin/Mercato.Connector.Plugin.csproj \
  -p:NopCommerceRoot=/path/to/nopCommerce-4.90.7
```

The same `NopCommerceRoot` property applies to the other plugins. CI checks out official `release-4.90.7`, builds the concrete plugins against it, runs nopCommerce's plugin-assembly cleanup, and packages the resulting native `Nop.Web/Plugins/Mercato.*` directories as the `mercato-nopcommerce-4.90.7-plugins` artifact.

## Deployment

Use the CI artifact or the cleaned native plugin output directories. Copy each `Mercato.*` folder into the target nopCommerce `Plugins` directory, restart nopCommerce, install `Mercato.Connector`, configure it, then install the dependent Mercato plugins. nopCommerce's dependency metadata prevents the functional plugins from being treated as independent of the Connector.
