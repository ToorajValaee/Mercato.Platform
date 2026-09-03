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

The EF Core 10 migration baseline is committed as `20260903155225_InitialBaseline` together with its designer and `MercatoDbContextModelSnapshot`.

Database initialization now uses the committed migration history. For a fresh PostgreSQL database, EF applies the baseline normally. For an existing Mercato schema that predates migration history, startup detects the established Mercato schema, creates/stamps the EF migration history for the initial baseline, and then continues with normal migrations instead of attempting to recreate existing tables.

Core/POS CI runs `backend/migration-smoke.sh` against PostgreSQL 16 and validates both supported transition paths:

- an empty database receives the initial migration and expected core tables such as `Products` and `Branches`;
- an existing Mercato schema with its migration-history table removed is adopted into the baseline, starts successfully through the normal API initializer, retains an unrelated sentinel row, and records the baseline in `__EFMigrationsHistory`.

Future model changes must be delivered as reviewed forward EF migrations and must continue to pass the same migration regression. Do not use `EnsureCreated`, table recreation, or persistent-volume deletion as a production schema-upgrade strategy.

## Persistence and backups

Back up both persistent data sets:

- PostgreSQL (`mercato_pgdata`): authoritative business, inventory, order, accounting, settlement, user, and configuration data;
- MinIO (`mercato_minio_data`): product/media objects.

A production deployment is not operationally ready until restore procedures for both stores have been tested. Backup frequency and retention are deployment/operator decisions and should match the business recovery requirements.

## Authentication and authorization

Before release/deployment, verify production configuration for:

- JWT issuer, audience, key, and expiry behavior;
- Admin-only APIs remaining server-authorized;
- Manager/Cashier branch restrictions remaining enforced server-side;
- Back Office access remaining independent from branch assignment and not enableable by client-side UI changes;
- username/email authentication-mode transitions not locking out all Admin users;
- malformed password hashes failing authentication without causing request failures.

The repository regression suite covers the relevant application authorization and password-hash behavior; deployment operators remain responsible for supplying the production settings and credentials.

## nopCommerce integration

Production nopCommerce must run the exact supported target: **nopCommerce 4.90.7 / .NET 9** unless a separately tested upgrade is approved.

The authoritative nopCommerce regression checks out the official `release-4.90.7` source and validates:

- all five Mercato plugins compile against the native nopCommerce target and install on a clean instance;
- Connector configuration consumption;
- BranchSelector storefront behavior;
- product and category synchronization;
- inventory synchronization;
- paid-order synchronization;
- failed paid-order retry with stable `nop:{orderId}` idempotency key;
- durable `Mercato.OrderSyncedUtc` marker and no duplicate retry after success;
- final native plugin package staging and artifact upload.

## Repository release gate

The repository can be called code/release-ready only when, on the exact same final revision:

- Core/POS/Back Office CI is green, including fresh/existing database migration validation, POS runtime smoke, frontend build/typecheck, and backend tests;
- nopCommerce 4.90.7 regression is green and produces the native plugin package artifact;
- the committed EF migration baseline and compatibility path remain present;
- the source-of-truth specification and this readiness document reflect the shipped state.

## Deployment/operator gate

After the repository release gate is green, an actual production deployment still requires operator/environment work:

- provide real production secrets outside source control;
- configure HTTPS/reverse proxy and network isolation;
- perform and verify PostgreSQL backup/restore;
- perform and verify MinIO backup/restore;
- run deployment-specific acceptance with the intended administrator/staff accounts and connected nopCommerce instance;
- define any jurisdiction/deployment-specific business policies that are required in that environment (for example tax/fiscal rules) or explicitly keep them out of scope.

Repository CI cannot prove these environment-specific controls on behalf of the deployment operator.
