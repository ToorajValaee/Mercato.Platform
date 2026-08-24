# Mercato Platform

Mercato is a multi-branch retail ERP platform with integrated e-commerce capabilities.

## Vision

Mercato manages:

- Product catalog
- Categories
- Physical inventory
- Multi-branch operations
- Artist/supplier consignment
- POS sales
- Accounting
- Settlement
- nopCommerce integration

## Architecture

```
Mercato Platform

Backend (.NET 10)
    |
    +-- Domain
    +-- Application
    +-- Infrastructure
    +-- Modules

Frontend
    |
    +-- Admin Panel
    +-- POS Touch App

Integrations
    |
    +-- nopCommerce Plugins
```

## Deployment

Target deployment uses Docker Compose and PostgreSQL.

## Business Rules

- Artist products are tracked by purchase cost, not revenue sharing.
- Inventory is ledger based.
- Branch transfers create inventory movements.
- Sales create accounting transactions.
