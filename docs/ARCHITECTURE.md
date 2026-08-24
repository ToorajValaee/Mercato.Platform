# Mercato Architecture

## Core principle

Mercato is the business system of record. nopCommerce is an integrated commerce channel.

## Main domains

- Products
- Inventory
- Branches
- Artists
- Settlements
- Accounting
- POS
- Orders
- Catalog

## Inventory Model

The system uses physical inventory ownership.

Product:
- Catalog definition

Product Item:
- Physical unit
- Artist ownership
- Purchase cost
- Current branch

## Artist Settlement

Artists are paid based on the agreed purchase price of sold items.

Example:

Retail price: 900000
Artist cost: 500000

Artist payable: 500000
Store gross profit: 400000
