-- =====================================================
-- TimescaleDB Continuous Aggregates for Sentiment Analysis
-- Date: October 31, 2025
-- Description: Creates materialized views for real-time sentiment analytics
-- Prerequisites: TimescaleDB extension must be installed and enabled
-- =====================================================

-- ============ PART 1: Community Sentiment Hourly Aggregate ============

DROP MATERIALIZED VIEW IF EXISTS community_sentiment_hourly CASCADE;

CREATE MATERIALIZED VIEW community_sentiment_hourly
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 hour', "CreatedAt") AS hour,
    COUNT(*) as total_posts,
    AVG("JoyScore") as avg_joy,
    AVG("SadnessScore") as avg_sadness,
    AVG("AngerScore") as avg_anger,
    AVG("FearScore") as avg_fear,
    AVG("SentimentPolarity") as avg_polarity,
    MODE() WITHIN GROUP (ORDER BY "PrimaryEmotion") as dominant_emotion,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'Joy') as joy_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'Sadness') as sadness_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'Anger') as anger_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'Fear') as fear_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'Neutral') as neutral_count,
    COUNT(*) FILTER (WHERE "NeedsReview" = TRUE) as flagged_posts
FROM "Sivar_Posts"
WHERE "PrimaryEmotion" IS NOT NULL
GROUP BY hour
WITH NO DATA;

-- Add refresh policy (updates every hour, retains last 3 hours)
SELECT add_continuous_aggregate_policy('community_sentiment_hourly',
    start_offset => INTERVAL '3 hours',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour');

COMMENT ON MATERIALIZED VIEW community_sentiment_hourly IS 'Hourly aggregated community-wide sentiment metrics';

-- ============ PART 2: Moderation Metrics Daily Aggregate ============

DROP MATERIALIZED VIEW IF EXISTS moderation_metrics_daily CASCADE;

CREATE MATERIALIZED VIEW moderation_metrics_daily
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 day', "CreatedAt") AS day,
    COUNT(*) as total_posts,
    COUNT(*) FILTER (WHERE "NeedsReview" = TRUE) as flagged_posts,
    COUNT(*) FILTER (WHERE "HasAnger" = TRUE) as posts_with_anger,
    AVG("AngerScore") as avg_anger_score,
    COUNT(*) FILTER (WHERE "AngerScore" > 0.7) as high_anger_count,
    ROUND((COUNT(*) FILTER (WHERE "NeedsReview" = TRUE)::decimal / NULLIF(COUNT(*), 0)) * 100, 2) as flag_rate_percent
FROM "Sivar_Posts"
WHERE "PrimaryEmotion" IS NOT NULL
GROUP BY day
WITH NO DATA;

-- Add refresh policy (updates daily, retains last 3 days)
SELECT add_continuous_aggregate_policy('moderation_metrics_daily',
    start_offset => INTERVAL '3 days',
    end_offset => INTERVAL '1 day',
    schedule_interval => INTERVAL '1 day');

COMMENT ON MATERIALIZED VIEW moderation_metrics_daily IS 'Daily content moderation metrics for anger and toxicity detection';

-- ============ PART 3: Profile Sentiment Daily Aggregate ============

DROP MATERIALIZED VIEW IF EXISTS profile_sentiment_daily CASCADE;

CREATE MATERIALIZED VIEW profile_sentiment_daily
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 day', "CreatedAt") AS day,
    "ProfileId",
    COUNT(*) as post_count,
    AVG("JoyScore") as avg_joy,
    AVG("SadnessScore") as avg_sadness,
    AVG("AngerScore") as avg_anger,
    AVG("FearScore") as avg_fear,
    AVG("SentimentPolarity") as avg_polarity,
    MODE() WITHIN GROUP (ORDER BY "PrimaryEmotion") as dominant_emotion,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'Joy') as joy_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'Sadness') as sadness_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'Anger') as anger_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'Fear') as fear_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'Neutral') as neutral_count,
    COUNT(*) FILTER (WHERE "NeedsReview" = TRUE) as flagged_count
FROM "Sivar_Posts"
WHERE "PrimaryEmotion" IS NOT NULL
GROUP BY day, "ProfileId"
WITH NO DATA;

-- Add refresh policy (updates daily, retains last 3 days)
SELECT add_continuous_aggregate_policy('profile_sentiment_daily',
    start_offset => INTERVAL '3 days',
    end_offset => INTERVAL '1 day',
    schedule_interval => INTERVAL '1 day');

-- Add index for profile queries
CREATE INDEX IF NOT EXISTS idx_profile_sentiment_daily_profile 
ON profile_sentiment_daily("ProfileId", day DESC);

COMMENT ON MATERIALIZED VIEW profile_sentiment_daily IS 'Per-profile daily emotion tracking and trends';

-- ============ PART 4: Initial Refresh (Optional - Run Manually) ============

-- Uncomment to perform initial data refresh
-- This may take time depending on data volume

-- CALL refresh_continuous_aggregate('community_sentiment_hourly', NULL, NULL);
-- CALL refresh_continuous_aggregate('moderation_metrics_daily', NULL, NULL);
-- CALL refresh_continuous_aggregate('profile_sentiment_daily', NULL, NULL);

-- ============ PART 5: Query Examples ============

-- Example 1: Get community sentiment for last 24 hours
-- SELECT * FROM community_sentiment_hourly 
-- WHERE hour >= NOW() - INTERVAL '24 hours' 
-- ORDER BY hour DESC;

-- Example 2: Get moderation metrics for last 7 days
-- SELECT * FROM moderation_metrics_daily 
-- WHERE day >= NOW() - INTERVAL '7 days' 
-- ORDER BY day DESC;

-- Example 3: Get profile emotion trend
-- SELECT * FROM profile_sentiment_daily 
-- WHERE "ProfileId" = 'YOUR-PROFILE-UUID-HERE' 
--   AND day >= NOW() - INTERVAL '30 days' 
-- ORDER BY day DESC;

-- Example 4: Find profiles with high anger trends
-- SELECT "ProfileId", AVG(avg_anger) as avg_anger_30d, SUM(anger_count) as total_anger_posts
-- FROM profile_sentiment_daily
-- WHERE day >= NOW() - INTERVAL '30 days'
-- GROUP BY "ProfileId"
-- HAVING AVG(avg_anger) > 0.5
-- ORDER BY avg_anger_30d DESC
-- LIMIT 20;

-- ============ Continuous Aggregates Complete ============
-- Verify views with: SELECT view_name FROM timescaledb_information.continuous_aggregates;
-- Monitor refresh status with: SELECT * FROM timescaledb_information.job_stats WHERE proc_name LIKE '%continuous%';
