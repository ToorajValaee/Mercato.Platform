-- Mercato Platform PostgreSQL initialization baseline
-- EF Core migrations will manage incremental schema changes.

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Baseline marker table for deployment validation
CREATE TABLE IF NOT EXISTS __mercato_schema_version
(
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    version TEXT NOT NULL,
    applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO __mercato_schema_version(version)
SELECT 'baseline'
WHERE NOT EXISTS (
    SELECT 1 FROM __mercato_schema_version WHERE version = 'baseline'
);
