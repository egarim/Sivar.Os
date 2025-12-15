-- =====================================================
-- Script: Add TimescaleDB Continuous Aggregates
-- Purpose: Create materialized views for real-time analytics
-- Date: October 31, 2025
-- Phase: 7 - Continuous Aggregates
-- =====================================================

-- Check if required hypertables exist before creating aggregates
DO $$
DECLARE
    posts_is_hypertable BOOLEAN;
    activities_is_hypertable BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Posts'
    ) INTO posts_is_hypertable;
    
    SELECT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Activities'
    ) INTO activities_is_hypertable;
    
    IF NOT posts_is_hypertable THEN
        RAISE NOTICE '⚠️ Sivar_Posts is not a hypertable - skipping continuous aggregates';
        RAISE NOTICE 'Run ConvertToHypertables.sql first to enable this feature';
        RETURN;
    END IF;
    
    IF NOT activities_is_hypertable THEN
        RAISE NOTICE '⚠️ Sivar_Activities is not a hypertable - some aggregates may fail';
    END IF;
    
    RAISE NOTICE '✅ Hypertables found, proceeding with continuous aggregates';
END $$;

-- =====================================================
-- 1. DAILY POST METRICS
-- =====================================================
DO $$
BEGIN
    -- Check if Posts hypertable exists
    IF NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Posts'
    ) THEN
        RAISE NOTICE '⚠️ Skipping post_metrics_daily - Sivar_Posts not a hypertable';
        RETURN;
    END IF;
    
    -- Drop existing view if it exists
    DROP MATERIALIZED VIEW IF EXISTS post_metrics_daily CASCADE;
    
    -- Create continuous aggregate for daily post metrics
    CREATE MATERIALIZED VIEW post_metrics_daily
    WITH (timescaledb.continuous) AS
    SELECT 
        time_bucket('1 day', "CreatedAt") AS day,
        "ProfileId" as author_key,
        "PostType" as post_type,
        COUNT(*) as post_count,
        SUM(COALESCE("ViewCount", 0)) as total_views,
        SUM(COALESCE("ShareCount", 0)) as total_shares,
        AVG(COALESCE("ViewCount", 0)) as avg_views,
        MAX("CreatedAt") as last_post_at
    FROM "Sivar_Posts"
    WHERE NOT "IsDeleted"
    GROUP BY day, "ProfileId", "PostType"
    WITH NO DATA;
    
    -- Create index for faster queries
    CREATE INDEX idx_post_metrics_daily_author_day 
    ON post_metrics_daily (author_key, day DESC);
    
    -- Add refresh policy
    PERFORM add_continuous_aggregate_policy('post_metrics_daily',
        start_offset => INTERVAL '3 months',
        end_offset => INTERVAL '1 hour',
        schedule_interval => INTERVAL '1 hour'
    );
    
    RAISE NOTICE '✅ Created post_metrics_daily aggregate';
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Failed to create post_metrics_daily: %', SQLERRM;
END $$;

-- =====================================================
-- 2. HOURLY ACTIVITY STREAM STATS
-- =====================================================
DO $$
BEGIN
    -- Check if Activities hypertable exists
    IF NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Activities'
    ) THEN
        RAISE NOTICE '⚠️ Skipping activity_metrics_hourly - Sivar_Activities not a hypertable';
        RETURN;
    END IF;
    
    -- Drop existing view if it exists
    DROP MATERIALIZED VIEW IF EXISTS activity_metrics_hourly CASCADE;
    
    -- Create continuous aggregate for hourly activity metrics
    CREATE MATERIALIZED VIEW activity_metrics_hourly
    WITH (timescaledb.continuous) AS
    SELECT 
        time_bucket('1 hour', "CreatedAt") AS hour,
        "Verb" as verb,
        "ObjectType" as object_type,
        COUNT(*) as activity_count,
        COUNT(DISTINCT "ActorId") as unique_users,
        MAX("CreatedAt") as last_activity_at
    FROM "Sivar_Activities"
    WHERE NOT "IsDeleted"
    GROUP BY hour, "Verb", "ObjectType"
    WITH NO DATA;
    
    -- Create index for faster queries
    CREATE INDEX idx_activity_metrics_hourly_hour 
    ON activity_metrics_hourly (hour DESC);
    
    -- Add refresh policy
    PERFORM add_continuous_aggregate_policy('activity_metrics_hourly',
        start_offset => INTERVAL '1 month',
        end_offset => INTERVAL '1 hour',
        schedule_interval => INTERVAL '1 hour'
    );
    
    RAISE NOTICE '✅ Created activity_metrics_hourly aggregate';
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Failed to create activity_metrics_hourly: %', SQLERRM;
END $$;

-- =====================================================
-- 3. USER ENGAGEMENT METRICS (DAILY)
-- =====================================================
DO $$
BEGIN
    -- Check if Activities hypertable exists
    IF NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Activities'
    ) THEN
        RAISE NOTICE '⚠️ Skipping user_engagement_daily - Sivar_Activities not a hypertable';
        RETURN;
    END IF;
    
    -- Drop existing view if it exists
    DROP MATERIALIZED VIEW IF EXISTS user_engagement_daily CASCADE;
    
    -- Create continuous aggregate for daily user engagement
    CREATE MATERIALIZED VIEW user_engagement_daily
    WITH (timescaledb.continuous) AS
    SELECT 
        time_bucket('1 day', "CreatedAt") AS day,
        "ActorId" as user_key,
        COUNT(*) as total_activities,
        COUNT(*) FILTER (WHERE "Verb" = 'Create') as creates_count,
        COUNT(*) FILTER (WHERE "Verb" = 'Like') as likes_count,
        COUNT(*) FILTER (WHERE "Verb" = 'Comment') as comments_count,
        COUNT(*) FILTER (WHERE "Verb" = 'Share') as shares_count,
        COUNT(*) FILTER (WHERE "Verb" = 'Follow') as follows_count,
        MAX("CreatedAt") as last_activity_at
    FROM "Sivar_Activities"
    WHERE NOT "IsDeleted"
    GROUP BY day, "ActorId"
    WITH NO DATA;
    
    -- Create index for faster queries
    CREATE INDEX idx_user_engagement_daily_user_day 
    ON user_engagement_daily (user_key, day DESC);
    
    -- Add refresh policy
    PERFORM add_continuous_aggregate_policy('user_engagement_daily',
        start_offset => INTERVAL '6 months',
        end_offset => INTERVAL '1 hour',
        schedule_interval => INTERVAL '1 hour'
    );
    
    RAISE NOTICE '✅ Created user_engagement_daily aggregate';
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Failed to create user_engagement_daily: %', SQLERRM;
END $$;

-- =====================================================
-- 4. POST ENGAGEMENT METRICS (DAILY)
-- =====================================================
-- Note: This aggregate joins Posts with Activities, both need to be hypertables
-- Skip for now as it's complex - can be enabled when both tables are hypertables
DO $$
BEGIN
    RAISE NOTICE '⏭️ Skipping post_engagement_daily - requires complex join setup';
END $$;

-- =====================================================
-- VERIFICATION (simplified for compatibility)
-- =====================================================
DO $$
DECLARE
    aggregate_count INTEGER;
    refresh_job_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO aggregate_count
    FROM timescaledb_information.continuous_aggregates
    WHERE view_schema = 'public';
    
    SELECT COUNT(*) INTO refresh_job_count
    FROM timescaledb_information.jobs
    WHERE proc_name = 'policy_refresh_continuous_aggregate';
    
    RAISE NOTICE '📊 Found % continuous aggregate(s) and % refresh policy job(s)', aggregate_count, refresh_job_count;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Could not verify continuous aggregates: %', SQLERRM;
END $$;

-- =====================================================
-- IMPORTANT NOTES:
-- - This script is idempotent (DROP IF EXISTS before CREATE)
-- - Continuous aggregates are automatically maintained
-- - Refresh policies run as background jobs
-- - Queries against these views are much faster than raw queries
-- - Data is pre-aggregated and incrementally updated
-- - Adjust start_offset/end_offset based on data retention needs
-- - WITH NO DATA means initial materialization happens via policy
-- =====================================================
