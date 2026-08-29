-- Mercato Platform PostgreSQL initialization prerequisites.
--
-- IMPORTANT: the application currently has no EF Core migrations and therefore
-- uses Database.EnsureCreated() for a fresh database. EnsureCreated() will skip
-- the entire Mercato schema if any user table already exists. For that reason
-- this bootstrap must not create marker/application tables.
--
-- Once reviewed EF migrations are introduced, schema versioning belongs in the
-- EF migrations history rather than a pre-created custom table.

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
