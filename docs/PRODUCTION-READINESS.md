# Mercato Production Readiness

This document tracks operational requirements for deploying Mercato outside local development. It does not replace `docs/Mercato.Platform.Specification.md`; business rules remain defined there.

## Deployment profile

Use `docker-compose.production.yml` for a production-like deployment. The normal `docker-compose.yml` intentionally keeps convenient local-development defaults and demo data.

The production profile:

- runs ASP.NET Core with `ASPNETCORE_ENVIRONMENT=Production`;
- disables demo data;
- requires explicit PostgreSQL, JWT, bootstrap-admin, and MinIO secrets;
- does not publish PostgreSQL or MinIO ports to the host;
- binds the Mercato HTTP endpoint to `127.0.0.1:8080` by default so a host reverse proxy can terminate TLS;
- keeps the React/Vite build as static files served by the Mercato API container, so there is no separate frontend production service.

Required environment values:

```text
MERCATO_POSTGRES_PASSWORD
MERCATO_JWT_KEY
MERCATO_BOOTSTRAP_ADMIN_EMAIL
MERCATO_BOOTSTRAP_ADMIN_PASSWORD
MERCATO_MINIO_ACCESS_KEY
MERCATO_MINIO_SECRET_KEY
```

Optional production values include `MERCATO_HTTP_BIND`, `MERCATO_JWT_ISSUER`, `MERCATO_JWT_AUDIENCE`, and `MERCATO_MINIO_BUCKET`.

## Network and TLS

Do not expose PostgreSQL or MinIO directly to the public Internet. Terminate HTTPS at a trusted reverse proxy/load balancer in front of Mercato. If `MERCATO_HTTP_BIND` is changed from the default loopback address, ensure equivalent firewall/network isolation is in place.

The public application origin should expose only the Mercato HTTP application routes that are intended for staff clients and API consumers. MinIO remains an internal dependency unless a future media-delivery design explicitly requires otherwise.

## Secrets and bootstrap access

Never use the local-development JWT key, database password, MinIO credentials, or bootstrap-admin password in production.

The bootstrap Admin exists to make a clean installation operable. After the first successful production bootstrap:

1. verify the Admin can authenticate;
2. create/verify the intended long-term administrator accounts;
3. rotate/remove bootstrap credentials from the deployment environment according to the deployment operator's secret-management process.

JWT signing keys must be high-entropy secrets and must not be committed to the repository.

## Database migrations

**Release blocker:** a production EF Core migration baseline is still required.

Current startup supports fresh development creation and additive compatibility updates. This is not the final production schema-upgrade strategy. Before the first production release:

- generate the initial migration from `MercatoDbContext` using EF Core 10 tooling;
- commit the migration and model snapshot;
- validate migration creation against an empty PostgreSQL 16 database;
- validate the transition for existing local/development databases that were created by `EnsureCreated`;
- make startup use the reviewed migration history without attempting to recreate existing tables;
- test at least one forward migration from the baseline to a subsequent schema change.

Do not delete persistent volumes as a migration strategy.

## Persistence and backups

Back up both persistent data sets:

- PostgreSQL (`mercato_pgdata`): authoritative business, inventory, order, accounting, settlement, user, and configuration data;
- MinIO (`mercato_minio_data`): product/media objects.

A production release is not operationally ready until restore procedures for both stores have been tested. Backup frequency and retention are deployment/operator decisions and should match the business recovery requirements.

## Authentication and authorization

Before release, verify:

- JWT issuer, audience, key, and expiry behavior use production settings;
- Admin-only APIs remain server-authorized;
- Manager/Cashier branch restrictions are enforced server-side;
- Back Office access is independent from branch assignment and cannot be enabled by client-side UI changes;
- username/email authentication-mode transitions cannot lock out all Admin users;
- malformed password hashes fail authentication without causing request failures.

## nopCommerce integration

Production nopCommerce must run the exact supported target: **nopCommerce 4.90.7 / .NET 9** unless a separately tested upgrade is approved.

Before release, the authoritative nop regression must pass all of these against a clean official 4.90.7 installation:

- plugin compilation and installation;
- Connector configuration consumption;
- BranchSelector storefront behavior;
- Product and category synchronization;
- Inventory synchronization;
- paid-order synchronization;
- failed paid-order retry with stable `nop:{orderId}` idempotency key;
- durable `Mercato.OrderSyncedUtc` marker and no duplicate retry after success;
- final native plugin package staging and artifact upload.

## Final release gate

The project can be called release-ready only when:

- Core/POS/Back Office CI is green on the release revision;
- nopCommerce 4.90.7 regression is green on the corresponding integration revision;
- EF migration baseline and upgrade path are validated;
- production secrets are provided outside source control;
- TLS/reverse proxy and network isolation are configured;
- PostgreSQL and MinIO backup/restore procedures are validated;
- unresolved business-policy choices required by the intended deployment (for example tax/fiscal rules) have either been explicitly defined or confirmed out of scope.
