# Mercato Platform Specification

Version: 1.0
Purpose: Source of truth for product requirements, business flows, architecture decisions, and remaining development work.

## 1. Vision

Mercato is an ERP and business operating platform. It owns the business logic, inventory, products, branches, artists, accounting, POS, and settlement capabilities.

nopCommerce is used as the external commerce engine.

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
- Checkout
- Payment gateways
- Shipping providers

## 3. Target Architecture

```
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
9. Accounting receives transaction.
10. Artist settlement is calculated.

## 5. POS UX Flow

### Staff workflow

1. Staff logs into POS.
2. Select branch.
3. Search products.
4. Check inventory.
5. Create customer sale.
6. Process payment.
7. Reduce inventory.
8. Generate accounting records.
9. Update artist settlement data.

## 6. Core Business Modules

## Inventory Engine

Requirements:

- Stock tracking
- Branch stock
- Availability calculation
- Reservation support
- Stock movement history

## Products

Requirements:

- Product master data
- Pricing
- Categories
- Images
- Synchronization to storefront

## Branches

Requirements:

- Multiple locations
- Branch inventory ownership
- Branch selection logic

## Artists

Requirements:

- Artist profiles
- Product ownership
- Revenue attribution
- Settlement calculation

## Accounting

Requirements:

- Sales records
- Invoices
- Transactions
- Financial reports

## POS

Requirements:

- Fast sales workflow
- Branch operation
- Offline-ready consideration
- Inventory integration

## Catalog

Requirements:

- Catalog generation
- Storefront publishing
- Product presentation

## 7. nopCommerce Plugins

The plugins are integration modules, not generic extensions.

Structure:

```
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

- Send orders from nopCommerce to Mercato
- Trigger accounting and settlement flows

## 8. Development Audit Rules

Before considering the platform complete, verify:

- Every business flow has an implementation.
- Every module has domain, application, infrastructure, and API support where needed.
- No business rule exists only in the storefront.
- Inventory remains owned by Mercato.
- Accounting receives all required events.
- Settlement receives artist attribution data.

## 9. Current Known Repository Gap Analysis

Known existing foundation:

- API layer
- Domain layer
- Application layer
- Infrastructure layer

Expected modules requiring verification/completion:

- Inventory module
- Products module
- Branches module
- Artists module
- Accounting module
- POS module
- Catalog module
- NopCommerce plugins

## 10. Remaining Work List

### Development completion

- [ ] Complete Inventory Engine
- [ ] Complete Product module
- [ ] Complete Branch module
- [ ] Complete Artist module
- [ ] Complete Accounting module
- [ ] Complete POS module
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
- [ ] Fix CI pipeline
- [ ] Docker deployment verification
- [ ] Production readiness review

## 11. Update Policy

This document must be updated whenever a feature is completed or architecture changes.

It is intended to be usable by future developers and AI agents as the reference source of requirements.
