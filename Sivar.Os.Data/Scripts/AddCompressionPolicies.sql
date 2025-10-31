-- =====================================================
-- Script: Add Compression Policies
-- Purpose: Automatically compress old chunks to save storage
-- Date: October 31, 2025
-- =====================================================

-- Enable compression on Sivar_Activities hypertable
ALTER TABLE "Sivar_Activities" SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'UserKey',
    timescaledb.compress_orderby = 'CreatedAt DESC'
);

-- Add compression policy: compress chunks older than 30 days
SELECT add_compression_policy(
    'public."Sivar_Activities"',
    INTERVAL '30 days',
    if_not_exists => TRUE
);

-- Enable compression on Sivar_Posts hypertable
ALTER TABLE "Sivar_Posts" SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'AuthorKey',
    timescaledb.compress_orderby = 'CreatedAt DESC'
);

-- Add compression policy: compress chunks older than 90 days
SELECT add_compression_policy(
    'public."Sivar_Posts"',
    INTERVAL '90 days',
    if_not_exists => TRUE
);

-- Enable compression on Sivar_ChatMessages hypertable
ALTER TABLE "Sivar_ChatMessages" SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'ChatKey',
    timescaledb.compress_orderby = 'CreatedAt DESC'
);

-- Add compression policy: compress chunks older than 30 days
SELECT add_compression_policy(
    'public."Sivar_ChatMessages"',
    INTERVAL '30 days',
    if_not_exists => TRUE
);

-- Enable compression on Sivar_Notifications hypertable
ALTER TABLE "Sivar_Notifications" SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'UserKey',
    timescaledb.compress_orderby = 'CreatedAt DESC'
);

-- Add compression policy: compress chunks older than 30 days
SELECT add_compression_policy(
    'public."Sivar_Notifications"',
    INTERVAL '30 days',
    if_not_exists => TRUE
);

-- Verify compression settings and policies
SELECT hypertable_name,
       compression_enabled,
       compress_segmentby,
       compress_orderby
FROM timescaledb_information.hypertables
WHERE hypertable_schema = 'public';

-- Show compression policy jobs
SELECT h.hypertable_name,
       j.job_id,
       j.schedule_interval,
       j.config::json->>'compress_after' as compress_after,
       j.next_start
FROM timescaledb_information.jobs j
INNER JOIN timescaledb_information.hypertables h ON j.hypertable_name = h.hypertable_name
WHERE j.proc_name = 'policy_compression'
  AND h.hypertable_schema = 'public';

-- Show compression statistics (after compression has run)
SELECT hypertable_name,
       total_chunks,
       number_compressed_chunks,
       before_compression_total_bytes,
       after_compression_total_bytes,
       pg_size_pretty(before_compression_total_bytes) as size_before,
       pg_size_pretty(after_compression_total_bytes) as size_after,
       ROUND(100.0 * (before_compression_total_bytes - after_compression_total_bytes) / 
             NULLIF(before_compression_total_bytes, 0), 2) as compression_ratio
FROM timescaledb_information.hypertables
WHERE hypertable_schema = 'public';

-- =====================================================
-- IMPORTANT NOTES:
-- - This script is idempotent for policies (if_not_exists => TRUE)
-- - ALTER TABLE commands may error if compression already enabled
-- - compress_segmentby: Groups data for better compression
-- - compress_orderby: Sorts data within segments
-- - Compression is one-way (cannot easily decompress chunks)
-- - Compression policies run automatically as background jobs
-- - Typical compression ratio: 60-90% storage reduction
-- - Compressed chunks are read-only (inserts go to new chunks)
-- =====================================================
