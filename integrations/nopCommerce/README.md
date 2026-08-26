# Mercato nopCommerce Integration

This directory contains the version-agnostic business core for the five Mercato nopCommerce plugins.

- `Mercato.Connector.Plugin` checks Mercato API connectivity and owns connection configuration.
- `Mercato.ProductSync.Plugin` reads the Mercato catalog and upserts products through an `INopProductGateway` adapter.
- `Mercato.InventorySync.Plugin` reads branch-specific Mercato availability and updates nopCommerce stock through an `INopInventoryGateway` adapter.
- `Mercato.BranchSelector.Plugin` exposes branch-specific availability data for storefront selection/UI integration.
- `Mercato.OrderSync.Plugin` sends completed nopCommerce orders to Mercato using the POS/order transaction pipeline with the stable idempotency key `nop:{OrderId}`.

`Mercato.NopCommerce.Core` contains the HTTP client and integration contracts shared by the plugins.

## Authority boundary

Mercato remains authoritative for products, prices, branches, inventory, accounting, and artist settlement. nopCommerce remains authoritative for storefront presentation, SEO, cart, checkout UI, payment gateways, and shipping providers.

## nopCommerce adapter layer

The core projects intentionally do not reference a particular nopCommerce assembly version. Concrete implementations of `INopProductGateway`, `INopInventoryGateway`, plugin registration classes, admin configuration pages, event consumers, and `plugin.json` `SupportedVersions` must target the exact nopCommerce release used by deployment. Binding those APIs before the target version is selected would create an unverified version dependency; the business logic is isolated here so the final adapter is thin.
