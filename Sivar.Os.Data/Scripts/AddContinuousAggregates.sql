-- =====================================================
-- Script: Add TimescaleDB Continuous Aggregates
-- Purpose: Create materialized views for real-time analytics
-- Date: October 31, 2025
-- Phase: 7 - Continuous Aggregates
-- =====================================================

-- =====================================================
-- 1. DAILY POST METRICS
-- =====================================================
-- Drop existing view if it exists
DROP MATERIALIZED VIEW IF EXISTS post_metrics_daily CASCADE;

-- Create continuous aggregate for daily post metrics
CREATE MATERIALIZED VIEW post_metrics_daily
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 day', "CreatedAt") AS day,
    "AuthorKey" as author_key,
    "PostType" as post_type,
    COUNT(*) as post_count,
    SUM(COALESCE("ViewCount", 0)) as total_views,
    SUM(COALESCE("ShareCount", 0)) as total_shares,
    AVG(COALESCE("ViewCount", 0)) as avg_views,
    MAX("CreatedAt") as last_post_at
FROM "Sivar_Posts"
WHERE NOT "IsDeleted"
GROUP BY day, "AuthorKey", "PostType"
WITH NO DATA;

-- Create index for faster queries
CREATE INDEX idx_post_metrics_daily_author_day 
ON post_metrics_daily (author_key, day DESC);

-- Add refresh policy: update every hour, retain 3 months of data
SELECT add_continuous_aggregate_policy('post_metrics_daily',
    start_offset => INTERVAL '3 months',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour'
);

-- =====================================================
-- 2. HOURLY ACTIVITY STREAM STATS
-- =====================================================
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
    COUNT(DISTINCT "UserKey") as unique_users,
    MAX("CreatedAt") as last_activity_at
FROM "Sivar_Activities"
WHERE NOT "IsDeleted"
GROUP BY hour, "Verb", "ObjectType"
WITH NO DATA;

-- Create index for faster queries
CREATE INDEX idx_activity_metrics_hourly_hour 
ON activity_metrics_hourly (hour DESC);

-- Add refresh policy: update every hour, retain 1 month of data
SELECT add_continuous_aggregate_policy('activity_metrics_hourly',
    start_offset => INTERVAL '1 month',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour'
);

-- =====================================================
-- 3. USER ENGAGEMENT METRICS (DAILY)
-- =====================================================
-- Drop existing view if it exists
DROP MATERIALIZED VIEW IF EXISTS user_engagement_daily CASCADE;

-- Create continuous aggregate for daily user engagement
CREATE MATERIALIZED VIEW user_engagement_daily
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 day', "CreatedAt") AS day,
    "UserKey" as user_key,
    COUNT(*) as total_activities,
    COUNT(*) FILTER (WHERE "Verb" = 'Create') as creates_count,
    COUNT(*) FILTER (WHERE "Verb" = 'Like') as likes_count,
    COUNT(*) FILTER (WHERE "Verb" = 'Comment') as comments_count,
    COUNT(*) FILTER (WHERE "Verb" = 'Share') as shares_count,
    COUNT(*) FILTER (WHERE "Verb" = 'Follow') as follows_count,
    MAX("CreatedAt") as last_activity_at
FROM "Sivar_Activities"
WHERE NOT "IsDeleted"
GROUP BY day, "UserKey"
WITH NO DATA;

-- Create index for faster queries
CREATE INDEX idx_user_engagement_daily_user_day 
ON user_engagement_daily (user_key, day DESC);

-- Add refresh policy: update every hour, retain 6 months of data
SELECT add_continuous_aggregate_policy('user_engagement_daily',
    start_offset => INTERVAL '6 months',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour'
);

-- =====================================================
-- 4. POST ENGAGEMENT METRICS (DAILY)
-- =====================================================
-- Drop existing view if it exists
DROP MATERIALIZED VIEW IF EXISTS post_engagement_daily CASCADE;

-- Create continuous aggregate for daily post engagement
CREATE MATERIALIZED VIEW post_engagement_daily
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 day', p."CreatedAt") AS day,
    p."Id" as post_id,
    p."AuthorKey" as author_key,
    p."PostType" as post_type,
    COUNT(DISTINCT a."UserKey") FILTER (WHERE a."Verb" = 'Like') as unique_likes,
    COUNT(DISTINCT a."UserKey") FILTER (WHERE a."Verb" = 'Comment') as unique_comments,
    COUNT(DISTINCT a."UserKey") FILTER (WHERE a."Verb" = 'Share') as unique_shares,
    COUNT(DISTINCT a."UserKey") as total_engaged_users,
    MAX(a."CreatedAt") as last_engagement_at
FROM "Sivar_Posts" p
LEFT JOIN "Sivar_Activities" a 
    ON a."ObjectId" = p."Id"::text 
    AND a."ObjectType" = 'Post'
    AND NOT a."IsDeleted"
WHERE NOT p."IsDeleted"
GROUP BY day, p."Id", p."AuthorKey", p."PostType"
WITH NO DATA;

-- Create indexes for faster queries
CREATE INDEX idx_post_engagement_daily_post_day 
ON post_engagement_daily (post_id, day DESC);

CREATE INDEX idx_post_engagement_daily_author_day 
ON post_engagement_daily (author_key, day DESC);

-- Add refresh policy: update every hour, retain 3 months of data
SELECT add_continuous_aggregate_policy('post_engagement_daily',
    start_offset => INTERVAL '3 months',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour'
);

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================

-- Verify all continuous aggregates were created
SELECT view_name, 
       refresh_interval,
       materialization_hypertable_name
FROM timescaledb_information.continuous_aggregates
WHERE view_schema = 'public'
ORDER BY view_name;

-- Show all continuous aggregate policies
SELECT ca.view_name,
       j.job_id,
       j.schedule_interval,
       j.config,
       j.next_start
FROM timescaledb_information.continuous_aggregates ca
INNER JOIN timescaledb_information.jobs j 
    ON ca.view_name = j.hypertable_name
WHERE ca.view_schema = 'public'
  AND j.proc_name = 'policy_refresh_continuous_aggregate'
ORDER BY ca.view_name;

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
