-- =====================================================================
-- Sentiment Analysis Continuous Aggregates
-- =====================================================================
-- Creates TimescaleDB continuous aggregates for sentiment analysis
-- Provides pre-computed sentiment metrics by location (city and country)
--
-- Features:
-- - City-level sentiment metrics (daily)
-- - Country-level sentiment metrics (daily)
-- - Automatic hourly refresh policies
-- - 6-month data retention
--
-- Execution Order: 8.0 (after continuous aggregates)
-- Dependencies: Requires Sivar_Posts to be a hypertable
-- =====================================================================

-- =====================================================================
-- 1. City-Level Sentiment Metrics (Daily)
-- =====================================================================
DO $$
BEGIN
    -- Check if Posts hypertable exists
    IF NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Posts'
    ) THEN
        RAISE NOTICE '⚠️ Skipping sentiment aggregates - Sivar_Posts is not a hypertable';
        RAISE NOTICE 'Run ConvertToHypertables.sql first to enable this feature';
        RETURN;
    END IF;

    -- Drop existing aggregate if it exists (for idempotency)
    IF EXISTS (
        SELECT 1 
        FROM timescaledb_information.continuous_aggregates 
        WHERE view_name = 'sentiment_metrics_city_daily'
    ) THEN
        DROP MATERIALIZED VIEW sentiment_metrics_city_daily CASCADE;
    END IF;

    -- Create city-level sentiment continuous aggregate
    CREATE MATERIALIZED VIEW sentiment_metrics_city_daily
    WITH (timescaledb.continuous) AS
    SELECT 
        time_bucket('1 day', "CreatedAt") AS day,
        "Location_City" as city,
        "Location_Country" as country,
        "PrimaryEmotion" as emotion,
        
        -- Post counts
        COUNT(*) as post_count,
        
        -- Emotion scores
        AVG("EmotionScore") as avg_emotion_score,
        AVG("SentimentPolarity") as avg_polarity,
        
        -- Individual emotion averages
        AVG("JoyScore") as avg_joy,
        AVG("SadnessScore") as avg_sadness,
        AVG("AngerScore") as avg_anger,
        AVG("FearScore") as avg_fear,
        
        -- Moderation metrics
        COUNT(*) FILTER (WHERE "HasAnger" = true) as anger_flagged_count,
        COUNT(*) FILTER (WHERE "NeedsReview" = true) as needs_review_count,
        
        -- Timestamps
        MIN("AnalyzedAt") as first_analyzed,
        MAX("AnalyzedAt") as last_analyzed
        
    FROM "Sivar_Posts"
    WHERE "PrimaryEmotion" IS NOT NULL 
      AND "Location_City" IS NOT NULL
      AND "Location_Country" IS NOT NULL
      AND NOT "IsDeleted"
    GROUP BY day, "Location_City", "Location_Country", "PrimaryEmotion"
    WITH NO DATA;

    -- Create index for faster queries by city and date
    CREATE INDEX idx_sentiment_city_daily_city_day 
    ON sentiment_metrics_city_daily (city, country, day DESC);

    -- Create index for emotion filtering
    CREATE INDEX idx_sentiment_city_daily_emotion 
    ON sentiment_metrics_city_daily (emotion, day DESC);

    -- Add refresh policy: update every hour, retain 6 months of data
    PERFORM add_continuous_aggregate_policy('sentiment_metrics_city_daily',
        start_offset => INTERVAL '6 months',
        end_offset => INTERVAL '1 hour',
        schedule_interval => INTERVAL '1 hour'
    );

    RAISE NOTICE '✅ City-level sentiment aggregate created successfully';
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Failed to create sentiment_metrics_city_daily: %', SQLERRM;
END $$;

-- =====================================================================
-- 2. Country-Level Sentiment Metrics (Daily)
-- =====================================================================
DO $$
BEGIN
    -- Check if Posts hypertable exists
    IF NOT EXISTS (
        SELECT 1 FROM timescaledb_information.hypertables 
        WHERE hypertable_name = 'Sivar_Posts'
    ) THEN
        RAISE NOTICE '⚠️ Skipping country sentiment aggregate - Sivar_Posts is not a hypertable';
        RETURN;
    END IF;

    -- Drop existing aggregate if it exists (for idempotency)
    IF EXISTS (
        SELECT 1 
        FROM timescaledb_information.continuous_aggregates 
        WHERE view_name = 'sentiment_metrics_country_daily'
    ) THEN
        DROP MATERIALIZED VIEW sentiment_metrics_country_daily CASCADE;
    END IF;

    -- Create country-level sentiment continuous aggregate
    CREATE MATERIALIZED VIEW sentiment_metrics_country_daily
    WITH (timescaledb.continuous) AS
    SELECT 
        time_bucket('1 day', "CreatedAt") AS day,
        "Location_Country" as country,
        "PrimaryEmotion" as emotion,
        
        -- Post counts
        COUNT(*) as post_count,
        COUNT(DISTINCT "Location_City") as unique_cities,
        
        -- Emotion scores
        AVG("EmotionScore") as avg_emotion_score,
        AVG("SentimentPolarity") as avg_polarity,
        
        -- Individual emotion averages
        AVG("JoyScore") as avg_joy,
        AVG("SadnessScore") as avg_sadness,
        AVG("AngerScore") as avg_anger,
        AVG("FearScore") as avg_fear,
        
        -- Moderation metrics
        COUNT(*) FILTER (WHERE "HasAnger" = true) as anger_flagged_count,
        COUNT(*) FILTER (WHERE "NeedsReview" = true) as needs_review_count,
        
        -- Engagement metrics
        SUM("ViewCount") as total_views,
        SUM("ShareCount") as total_shares,
        AVG("ViewCount") as avg_views,
        AVG("ShareCount") as avg_shares,
        
        -- Timestamps
        MIN("AnalyzedAt") as first_analyzed,
        MAX("AnalyzedAt") as last_analyzed
        
    FROM "Sivar_Posts"
    WHERE "PrimaryEmotion" IS NOT NULL 
      AND "Location_Country" IS NOT NULL
      AND NOT "IsDeleted"
    GROUP BY day, "Location_Country", "PrimaryEmotion"
    WITH NO DATA;

    -- Create index for faster queries by country and date
    CREATE INDEX idx_sentiment_country_daily_country_day 
    ON sentiment_metrics_country_daily (country, day DESC);

    -- Create index for emotion filtering
    CREATE INDEX idx_sentiment_country_daily_emotion 
    ON sentiment_metrics_country_daily (emotion, day DESC);

    -- Add refresh policy: update every hour, retain 6 months of data
    PERFORM add_continuous_aggregate_policy('sentiment_metrics_country_daily',
        start_offset => INTERVAL '6 months',
        end_offset => INTERVAL '1 hour',
        schedule_interval => INTERVAL '1 hour'
    );

    RAISE NOTICE '✅ Country-level sentiment aggregate created successfully';
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Failed to create sentiment_metrics_country_daily: %', SQLERRM;
END $$;

-- =====================================================================
-- 3. Initial Refresh (populate with existing data)
-- =====================================================================
DO $$
BEGIN
    -- Check if aggregates exist before refreshing
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.continuous_aggregates 
        WHERE view_name = 'sentiment_metrics_city_daily'
    ) THEN
        CALL refresh_continuous_aggregate('sentiment_metrics_city_daily', NULL, NULL);
        RAISE NOTICE '✅ City-level sentiment data refreshed';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM timescaledb_information.continuous_aggregates 
        WHERE view_name = 'sentiment_metrics_country_daily'
    ) THEN
        CALL refresh_continuous_aggregate('sentiment_metrics_country_daily', NULL, NULL);
        RAISE NOTICE '✅ Country-level sentiment data refreshed';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '⚠️ Failed to refresh sentiment aggregates: %', SQLERRM;
END $$;

-- =====================================================================
-- End of Script
-- =====================================================================
