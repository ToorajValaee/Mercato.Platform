# Mercato Domain Model

## Core Concepts

### Product
A catalog item visible in sales channels.

### Physical Product Item
A unique physical unit tracked by barcode, ownership and location.

### Artist
Supplier/creator who provides products on consignment.

### Branch
A store location holding inventory.

### Inventory Transaction
Every stock change is recorded as a ledger entry.

Types:
- Receive
- Transfer
- Sale
- Return
- Damage
- Adjustment

### Settlement
Calculates payable amounts to artists based on sold products and agreed purchase costs.
