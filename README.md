# Mercato Platform

Mercato is a multi-branch retail ERP platform with integrated e-commerce capabilities.

## Vision

Mercato manages:

- Product catalog and categories
- Artists and purchase-cost settlement
- Physical inventory and multi-branch transfers
- Customers
- POS sales, receipts, orders and returns
- Invoices and payments
- Accounting events and reporting
- Staff accounts and roles
- nopCommerce integration

## Architecture

```text
Mercato Platform

Backend (.NET 10)
    |
    +-- Domain
    +-- Application
    +-- Infrastructure
    +-- API

Built-in UI
    |
    +-- /          Workspace landing page
    +-- /admin/    Back Office
    +-- /pos/      Point of Sale

Integrations
    |
    +-- nopCommerce Plugins
```

## Local deployment

From the repository root:

```bash
docker compose up --build
```

Open `http://localhost:8080/` and choose **Back Office** or **Point of Sale**.

Development defaults:

```text
Email: admin@mercato.local
Password: MercatoLocal123!
```

These credentials and demo data are for local development only and can be overridden through the Compose environment variables.

## Back Office

`/admin/` provides role-aware management for products, categories, artists, branches, customers, inventory adjustments and transfers, movement history, invoices, order lookup, artist settlements, accounting reporting, and Admin-only staff management.

## POS

`/pos/` is the focused cashier application for branch catalog/stock, cart handling, idempotent checkout, receipt printing, order lookup, and partial returns/refunds.

## Business Rules

- Mercato is authoritative for product identity, price and inventory.
- Artist products are tracked by purchase cost, not revenue sharing.
- Inventory is ledger based.
- Branch transfers create balanced inventory movements.
- Sales and refunds create accounting transactions.
- Checkout and returns are atomic and concurrency guarded.
- Tax, discounts, final payment-provider rules, fiscal receipt rules and double-entry GL policy remain intentionally unresolved until specified.
