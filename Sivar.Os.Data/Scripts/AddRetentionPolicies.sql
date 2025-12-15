-- =====================================================
-- Script: Add Data Retention Policies
-- Purpose: Automatically drop old chunks to manage storage
-- Date: October 31, 2025
-- =====================================================

-- Retention policy for Sivar_Activities: 2 years
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Activities'
    ) THEN
        PERFORM add_retention_policy(
            'public."Sivar_Activities"',
            INTERVAL '2 years',
            if_not_exists => TRUE
        );
        RAISE NOTICE '✅ Retention policy added for Sivar_Activities (2 years)';
    ELSE
        RAISE NOTICE '⚠️ Sivar_Activities is not a hypertable, skipping retention policy';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not add retention policy for Sivar_Activities: %', SQLERRM;
END $$;

-- Retention policy for Sivar_Posts: 5 years
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Posts'
    ) THEN
        PERFORM add_retention_policy(
            'public."Sivar_Posts"',
            INTERVAL '5 years',
            if_not_exists => TRUE
        );
        RAISE NOTICE '✅ Retention policy added for Sivar_Posts (5 years)';
    ELSE
        RAISE NOTICE '⚠️ Sivar_Posts is not a hypertable, skipping retention policy';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not add retention policy for Sivar_Posts: %', SQLERRM;
END $$;

-- Retention policy for Sivar_ChatMessages: 1 year
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_ChatMessages'
    ) THEN
        PERFORM add_retention_policy(
            'public."Sivar_ChatMessages"',
            INTERVAL '1 year',
            if_not_exists => TRUE
        );
        RAISE NOTICE '✅ Retention policy added for Sivar_ChatMessages (1 year)';
    ELSE
        RAISE NOTICE '⚠️ Sivar_ChatMessages is not a hypertable, skipping retention policy';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not add retention policy for Sivar_ChatMessages: %', SQLERRM;
END $$;

-- Retention policy for Sivar_Notifications: 6 months
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Notifications'
    ) THEN
        PERFORM add_retention_policy(
            'public."Sivar_Notifications"',
            INTERVAL '6 months',
            if_not_exists => TRUE
        );
        RAISE NOTICE '✅ Retention policy added for Sivar_Notifications (6 months)';
    ELSE
        RAISE NOTICE '⚠️ Sivar_Notifications is not a hypertable, skipping retention policy';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not add retention policy for Sivar_Notifications: %', SQLERRM;
END $$;

-- Verify retention policies (simplified for compatibility)
DO $$
DECLARE
    policy_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO policy_count
    FROM timescaledb_information.jobs
    WHERE proc_name = 'policy_retention';
    
    RAISE NOTICE '📊 Found % retention policy job(s)', policy_count;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not verify retention policies: %', SQLERRM;
END $$;

-- =====================================================
-- IMPORTANT NOTES:
-- - This script is idempotent (if_not_exists => TRUE)
-- - Retention policies run automatically as background jobs
-- - Old chunks are dropped when they exceed retention interval
-- - Data is permanently deleted when chunks are dropped
-- - Adjust intervals based on legal/compliance requirements
-- =====================================================
