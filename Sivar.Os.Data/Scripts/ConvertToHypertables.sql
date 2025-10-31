-- =====================================================
-- Script: Convert Tables to TimescaleDB Hypertables
-- Purpose: Convert time-series tables to hypertables for better performance
-- Date: October 31, 2025
-- =====================================================

-- Convert Sivar_Activities to hypertable
-- Chunk interval: 7 days (high activity volume)
SELECT create_hypertable(
    'public."Sivar_Activities"',
    'CreatedAt',
    chunk_time_interval => INTERVAL '7 days',
    if_not_exists => TRUE
);

-- Convert Sivar_Posts to hypertable
-- Chunk interval: 30 days (moderate volume, longer queries)
SELECT create_hypertable(
    'public."Sivar_Posts"',
    'CreatedAt',
    chunk_time_interval => INTERVAL '30 days',
    if_not_exists => TRUE
);

-- Convert Sivar_ChatMessages to hypertable
-- Chunk interval: 7 days (high volume, recent queries)
SELECT create_hypertable(
    'public."Sivar_ChatMessages"',
    'CreatedAt',
    chunk_time_interval => INTERVAL '7 days',
    if_not_exists => TRUE
);

-- Convert Sivar_Notifications to hypertable
-- Chunk interval: 7 days (high volume, time-sensitive)
SELECT create_hypertable(
    'public."Sivar_Notifications"',
    'CreatedAt',
    chunk_time_interval => INTERVAL '7 days',
    if_not_exists => TRUE
);

-- Verify hypertables were created
SELECT hypertable_name, chunk_sizing_func, chunk_target_size
FROM timescaledb_information.hypertables
WHERE hypertable_schema = 'public';

-- Show chunks for verification
SELECT hypertable_name, chunk_name, range_start, range_end
FROM timescaledb_information.chunks
WHERE hypertable_schema = 'public'
ORDER BY hypertable_name, range_start DESC
LIMIT 20;

-- =====================================================
-- IMPORTANT NOTES:
-- - This script is idempotent (if_not_exists => TRUE)
-- - CreatedAt column must exist and be NOT NULL
-- - Chunk intervals optimized for each table's usage pattern
-- - Existing data will be automatically partitioned into chunks
-- - Indexes are preserved during conversion
-- =====================================================
