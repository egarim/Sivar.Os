-- =====================================================
-- Script: Add Compression Policies
-- Purpose: Automatically compress old chunks to save storage
-- Date: October 31, 2025
-- =====================================================

-- Enable compression on Sivar_Activities hypertable
DO $$
BEGIN
    -- Check if this is a hypertable before enabling compression
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Activities'
    ) THEN
        ALTER TABLE "Sivar_Activities" SET (
            timescaledb.compress,
            timescaledb.compress_segmentby = 'ActorId',
            timescaledb.compress_orderby = 'CreatedAt DESC'
        );
        
        PERFORM add_compression_policy(
            'public."Sivar_Activities"',
            INTERVAL '30 days',
            if_not_exists => TRUE
        );
        
        RAISE NOTICE '✅ Compression enabled on Sivar_Activities';
    ELSE
        RAISE NOTICE '⚠️ Sivar_Activities is not a hypertable, skipping compression';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not enable compression on Sivar_Activities: %', SQLERRM;
END $$;

-- Enable compression on Sivar_Posts hypertable
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Posts'
    ) THEN
        ALTER TABLE "Sivar_Posts" SET (
            timescaledb.compress,
            timescaledb.compress_segmentby = 'ProfileId',
            timescaledb.compress_orderby = 'CreatedAt DESC'
        );
        
        PERFORM add_compression_policy(
            'public."Sivar_Posts"',
            INTERVAL '90 days',
            if_not_exists => TRUE
        );
        
        RAISE NOTICE '✅ Compression enabled on Sivar_Posts';
    ELSE
        RAISE NOTICE '⚠️ Sivar_Posts is not a hypertable, skipping compression';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not enable compression on Sivar_Posts: %', SQLERRM;
END $$;

-- Enable compression on Sivar_ChatMessages hypertable
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_ChatMessages'
    ) THEN
        ALTER TABLE "Sivar_ChatMessages" SET (
            timescaledb.compress,
            timescaledb.compress_segmentby = 'ChatSessionId',
            timescaledb.compress_orderby = 'CreatedAt DESC'
        );
        
        PERFORM add_compression_policy(
            'public."Sivar_ChatMessages"',
            INTERVAL '30 days',
            if_not_exists => TRUE
        );
        
        RAISE NOTICE '✅ Compression enabled on Sivar_ChatMessages';
    ELSE
        RAISE NOTICE '⚠️ Sivar_ChatMessages is not a hypertable, skipping compression';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not enable compression on Sivar_ChatMessages: %', SQLERRM;
END $$;

-- Enable compression on Sivar_Notifications hypertable
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Notifications'
    ) THEN
        ALTER TABLE "Sivar_Notifications" SET (
            timescaledb.compress,
            timescaledb.compress_segmentby = 'RecipientId',
            timescaledb.compress_orderby = 'CreatedAt DESC'
        );
        
        PERFORM add_compression_policy(
            'public."Sivar_Notifications"',
            INTERVAL '30 days',
            if_not_exists => TRUE
        );
        
        RAISE NOTICE '✅ Compression enabled on Sivar_Notifications';
    ELSE
        RAISE NOTICE '⚠️ Sivar_Notifications is not a hypertable, skipping compression';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not enable compression on Sivar_Notifications: %', SQLERRM;
END $$;

-- Verify compression settings and policies (simplified for compatibility)
DO $$
DECLARE
    hypertable_count INTEGER;
    compression_job_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO hypertable_count
    FROM timescaledb_information.hypertables
    WHERE hypertable_schema = 'public';
    
    SELECT COUNT(*) INTO compression_job_count
    FROM timescaledb_information.jobs
    WHERE proc_name = 'policy_compression';
    
    RAISE NOTICE '📊 Found % hypertable(s) and % compression policy job(s)', hypertable_count, compression_job_count;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not verify compression settings: %', SQLERRM;
END $$;

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
