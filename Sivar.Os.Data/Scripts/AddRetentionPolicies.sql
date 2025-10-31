-- =====================================================
-- Script: Add Data Retention Policies
-- Purpose: Automatically drop old chunks to manage storage
-- Date: October 31, 2025
-- =====================================================

-- Retention policy for Sivar_Activities: 2 years
SELECT add_retention_policy(
    'public."Sivar_Activities"',
    INTERVAL '2 years',
    if_not_exists => TRUE
);

-- Retention policy for Sivar_Posts: 5 years
SELECT add_retention_policy(
    'public."Sivar_Posts"',
    INTERVAL '5 years',
    if_not_exists => TRUE
);

-- Retention policy for Sivar_ChatMessages: 1 year
SELECT add_retention_policy(
    'public."Sivar_ChatMessages"',
    INTERVAL '1 year',
    if_not_exists => TRUE
);

-- Retention policy for Sivar_Notifications: 6 months
SELECT add_retention_policy(
    'public."Sivar_Notifications"',
    INTERVAL '6 months',
    if_not_exists => TRUE
);

-- Verify retention policies
SELECT hypertable_name, 
       job_id,
       schedule_interval,
       config::json->>'drop_after' as retention_interval,
       next_start
FROM timescaledb_information.jobs j
INNER JOIN timescaledb_information.hypertables h ON j.hypertable_name = h.hypertable_name
WHERE j.proc_name = 'policy_retention'
  AND h.hypertable_schema = 'public';

-- =====================================================
-- IMPORTANT NOTES:
-- - This script is idempotent (if_not_exists => TRUE)
-- - Retention policies run automatically as background jobs
-- - Old chunks are dropped when they exceed retention interval
-- - Data is permanently deleted when chunks are dropped
-- - Adjust intervals based on legal/compliance requirements
-- =====================================================
