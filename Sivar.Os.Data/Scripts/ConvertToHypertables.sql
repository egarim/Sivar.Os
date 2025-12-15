-- =====================================================
-- Script: Convert Tables to TimescaleDB Hypertables
-- Purpose: Convert time-series tables to hypertables for better performance
-- Date: October 31, 2025
-- =====================================================

-- NOTE: TimescaleDB hypertables require the partitioning column (CreatedAt)
-- to be part of any unique constraints. Since these tables use UUID primary keys,
-- we need to drop and recreate the primary key constraint.

-- =====================================================
-- 1. Convert Sivar_Activities to hypertable
-- =====================================================
DO $$
BEGIN
    -- Check if already a hypertable
    IF NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Activities'
    ) THEN
        -- Drop the primary key constraint
        ALTER TABLE "Sivar_Activities" DROP CONSTRAINT IF EXISTS "PK_Sivar_Activities";
        
        -- Create hypertable
        PERFORM create_hypertable(
            'public."Sivar_Activities"',
            'CreatedAt',
            chunk_time_interval => INTERVAL '7 days',
            migrate_data => TRUE
        );
        
        -- Recreate primary key including CreatedAt
        ALTER TABLE "Sivar_Activities" ADD CONSTRAINT "PK_Sivar_Activities" 
            PRIMARY KEY ("Id", "CreatedAt");
        
        RAISE NOTICE '✅ Sivar_Activities converted to hypertable';
    ELSE
        RAISE NOTICE 'Sivar_Activities is already a hypertable';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not convert Sivar_Activities: %', SQLERRM;
END $$;

-- =====================================================
-- 2. Convert Sivar_Posts to hypertable
-- =====================================================
DO $$
BEGIN
    -- Check if already a hypertable
    IF NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Posts'
    ) THEN
        -- Drop the primary key constraint
        ALTER TABLE "Sivar_Posts" DROP CONSTRAINT IF EXISTS "PK_Sivar_Posts";
        
        -- Create hypertable
        PERFORM create_hypertable(
            'public."Sivar_Posts"',
            'CreatedAt',
            chunk_time_interval => INTERVAL '30 days',
            migrate_data => TRUE
        );
        
        -- Recreate primary key including CreatedAt
        ALTER TABLE "Sivar_Posts" ADD CONSTRAINT "PK_Sivar_Posts" 
            PRIMARY KEY ("Id", "CreatedAt");
        
        RAISE NOTICE '✅ Sivar_Posts converted to hypertable';
    ELSE
        RAISE NOTICE 'Sivar_Posts is already a hypertable';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not convert Sivar_Posts: %', SQLERRM;
END $$;

-- =====================================================
-- 3. Convert Sivar_ChatMessages to hypertable
-- =====================================================
DO $$
BEGIN
    -- Check if already a hypertable
    IF NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_ChatMessages'
    ) THEN
        -- Drop the primary key constraint
        ALTER TABLE "Sivar_ChatMessages" DROP CONSTRAINT IF EXISTS "PK_Sivar_ChatMessages";
        
        -- Create hypertable
        PERFORM create_hypertable(
            'public."Sivar_ChatMessages"',
            'CreatedAt',
            chunk_time_interval => INTERVAL '7 days',
            migrate_data => TRUE
        );
        
        -- Recreate primary key including CreatedAt
        ALTER TABLE "Sivar_ChatMessages" ADD CONSTRAINT "PK_Sivar_ChatMessages" 
            PRIMARY KEY ("Id", "CreatedAt");
        
        RAISE NOTICE '✅ Sivar_ChatMessages converted to hypertable';
    ELSE
        RAISE NOTICE 'Sivar_ChatMessages is already a hypertable';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not convert Sivar_ChatMessages: %', SQLERRM;
END $$;

-- =====================================================
-- 4. Convert Sivar_Notifications to hypertable
-- =====================================================
DO $$
BEGIN
    -- Check if already a hypertable
    IF NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Notifications'
    ) THEN
        -- Drop the primary key constraint
        ALTER TABLE "Sivar_Notifications" DROP CONSTRAINT IF EXISTS "PK_Sivar_Notifications";
        
        -- Create hypertable
        PERFORM create_hypertable(
            'public."Sivar_Notifications"',
            'CreatedAt',
            chunk_time_interval => INTERVAL '7 days',
            migrate_data => TRUE
        );
        
        -- Recreate primary key including CreatedAt
        ALTER TABLE "Sivar_Notifications" ADD CONSTRAINT "PK_Sivar_Notifications" 
            PRIMARY KEY ("Id", "CreatedAt");
        
        RAISE NOTICE '✅ Sivar_Notifications converted to hypertable';
    ELSE
        RAISE NOTICE 'Sivar_Notifications is already a hypertable';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not convert Sivar_Notifications: %', SQLERRM;
END $$;

-- Verify hypertables were created (simplified query for compatibility)
DO $$
DECLARE
    ht_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO ht_count
    FROM timescaledb_information.hypertables
    WHERE hypertable_schema = 'public';
    
    RAISE NOTICE '📊 Found % hypertable(s) in public schema', ht_count;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not verify hypertables: %', SQLERRM;
END $$;

-- =====================================================
-- IMPORTANT NOTES:
-- - Primary keys are modified to include CreatedAt
-- - This allows TimescaleDB to partition the data
-- - EF Core queries by Id will still work (composite key lookup)
-- - Chunk intervals optimized for each table's usage pattern
-- =====================================================
