# Mercato Platform

Mercato is a multi-branch retail ERP platform with integrated e-commerce capabilities.

## Vision

Mercato manages product catalog/categories, artists and purchase-cost settlement, physical inventory and branch transfers, customers, POS sales/returns, invoices/payments, accounting events, staff access, and nopCommerce integration.

## Architecture

```text
Mercato.Platform/
├── backend/
│   └── src/
│       ├── Mercato.Api
│       ├── Mercato.Application
│       ├── Mercato.Domain
│       └── Mercato.Infrastructure
├── frontend/
│   ├── admin/                 Back Office HTML entry
│   ├── pos/                   POS HTML entry
│   ├── public/                locales, favicon, login art, licensed-font hook
│   └── src/
│       ├── admin.tsx          Back Office React application
│       ├── pos.tsx            POS React application
│       ├── shared.tsx         shared API/auth/UI components
│       ├── persian.tsx        Jalali datetime control
│       └── styles.css         responsive design system
├── tests/
└── docker-compose.yml
```

The frontend is **React + TypeScript + Vite** and is a separate source project. Production remains simple: Docker builds the frontend, copies `frontend/dist` into the ASP.NET runtime `wwwroot`, and serves everything from the same origin.

Public routes remain:

```text
/           workspace launcher
/admin/     Back Office
/pos/       Point of Sale
```

Back Office routes are real SPA paths such as `/admin/products`, `/admin/invoices`, and `/admin/settings`; refreshing those routes is supported by the API fallback configuration.

## Responsive UI

Back Office and POS are designed for desktop, tablet, and mobile. Important responsive behavior includes:

- drawer navigation below tablet width;
- forms collapsing from two columns to one;
- desktop tables becoming readable record cards on mobile;
- POS catalog/cart stacking on narrow screens;
- touch-sized actions and controls;
- searchable selects rendered outside clipping panels;
- Farsi RTL/Dana typography support;
- Jalali date/time controls that stay within their responsive grid cells.

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

These credentials and demo data are for local development only and can be overridden through Compose environment variables.

### Frontend development

```bash
cd frontend
npm install
npm run dev
```

Production assets are created with:

```bash
npm run build
```

Licensed Dana files belong in `frontend/public/fonts/` as documented by its README. Font binaries are intentionally not committed.

## Back Office

The responsive Back Office covers products, categories, artists, artist goods receipts, branches, customers, inventory adjustments/transfers/history, invoices/reprinting, order lookup, artist settlements, accounting reporting, staff/branch access and application settings.

## POS

The responsive POS covers accessible-branch selection, catalog/stock, optional product images, cart handling, managed payment methods/discounts, customer lookup/fast registration by mobile, idempotent checkout, receipt printing and partial returns/refunds.

## Business Rules

- Mercato is authoritative for product identity, price and inventory.
- Artist settlement uses purchase cost, never revenue sharing.
- Inventory is ledger based.
- Branch transfers create balanced inventory movements.
- Sales and refunds create accounting transactions.
- Checkout and returns are atomic and concurrency guarded.
- Unresolved tax/fiscal/GL/payment-provider policy is not invented by the UI.
