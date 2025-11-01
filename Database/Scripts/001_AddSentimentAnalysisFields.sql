-- =====================================================
-- Sentiment Analysis Database Migration
-- Date: October 31, 2025
-- Description: Adds sentiment analysis fields to Posts and Comments tables
-- =====================================================

-- ============ PART 1: Add Sentiment Fields to Sivar_Posts ============

ALTER TABLE "Sivar_Posts" 
ADD COLUMN IF NOT EXISTS "PrimaryEmotion" VARCHAR(20),
ADD COLUMN IF NOT EXISTS "EmotionScore" DECIMAL(4,3),
ADD COLUMN IF NOT EXISTS "SentimentPolarity" DECIMAL(4,3),
ADD COLUMN IF NOT EXISTS "JoyScore" DECIMAL(4,3),
ADD COLUMN IF NOT EXISTS "SadnessScore" DECIMAL(4,3),
ADD COLUMN IF NOT EXISTS "AngerScore" DECIMAL(4,3),
ADD COLUMN IF NOT EXISTS "FearScore" DECIMAL(4,3),
ADD COLUMN IF NOT EXISTS "HasAnger" BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS "NeedsReview" BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS "AnalyzedAt" TIMESTAMPTZ;

-- ============ PART 2: Add Sentiment Fields to Sivar_Comments ============

ALTER TABLE "Sivar_Comments" 
ADD COLUMN IF NOT EXISTS "PrimaryEmotion" VARCHAR(20),
ADD COLUMN IF NOT EXISTS "EmotionScore" DECIMAL(4,3),
ADD COLUMN IF NOT EXISTS "SentimentPolarity" DECIMAL(4,3),
ADD COLUMN IF NOT EXISTS "JoyScore" DECIMAL(4,3),
ADD COLUMN IF NOT EXISTS "SadnessScore" DECIMAL(4,3),
ADD COLUMN IF NOT EXISTS "AngerScore" DECIMAL(4,3),
ADD COLUMN IF NOT EXISTS "FearScore" DECIMAL(4,3),
ADD COLUMN IF NOT EXISTS "HasAnger" BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS "NeedsReview" BOOLEAN DEFAULT FALSE,
ADD COLUMN IF NOT EXISTS "AnalyzedAt" TIMESTAMPTZ;

-- ============ PART 3: Create Indexes for Performance ============

-- Index for filtering by emotion
CREATE INDEX IF NOT EXISTS idx_posts_primary_emotion 
ON "Sivar_Posts"("PrimaryEmotion") 
WHERE "PrimaryEmotion" IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_comments_primary_emotion 
ON "Sivar_Comments"("PrimaryEmotion") 
WHERE "PrimaryEmotion" IS NOT NULL;

-- Index for moderation queries
CREATE INDEX IF NOT EXISTS idx_posts_needs_review 
ON "Sivar_Posts"("NeedsReview", "CreatedAt") 
WHERE "NeedsReview" = TRUE;

CREATE INDEX IF NOT EXISTS idx_comments_needs_review 
ON "Sivar_Comments"("NeedsReview", "CreatedAt") 
WHERE "NeedsReview" = TRUE;

-- Index for anger detection
CREATE INDEX IF NOT EXISTS idx_posts_has_anger 
ON "Sivar_Posts"("HasAnger", "AngerScore") 
WHERE "HasAnger" = TRUE;

CREATE INDEX IF NOT EXISTS idx_comments_has_anger 
ON "Sivar_Comments"("HasAnger", "AngerScore") 
WHERE "HasAnger" = TRUE;

-- Index for sentiment analysis timestamp
CREATE INDEX IF NOT EXISTS idx_posts_analyzed_at 
ON "Sivar_Posts"("AnalyzedAt") 
WHERE "AnalyzedAt" IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_comments_analyzed_at 
ON "Sivar_Comments"("AnalyzedAt") 
WHERE "AnalyzedAt" IS NOT NULL;

-- ============ PART 4: Create ProfileEmotionSummaries Table ============

CREATE TABLE IF NOT EXISTS "Sivar_ProfileEmotionSummaries" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ProfileId" UUID NOT NULL,
    "TimeWindow" VARCHAR(20) NOT NULL,
    "StartDate" TIMESTAMPTZ NOT NULL,
    "EndDate" TIMESTAMPTZ NOT NULL,
    "TotalPosts" INTEGER NOT NULL DEFAULT 0,
    "TotalComments" INTEGER NOT NULL DEFAULT 0,
    "AvgJoyScore" DECIMAL(4,3) NOT NULL DEFAULT 0,
    "AvgSadnessScore" DECIMAL(4,3) NOT NULL DEFAULT 0,
    "AvgAngerScore" DECIMAL(4,3) NOT NULL DEFAULT 0,
    "AvgFearScore" DECIMAL(4,3) NOT NULL DEFAULT 0,
    "DominantEmotion" VARCHAR(20),
    "JoyCount" INTEGER NOT NULL DEFAULT 0,
    "SadnessCount" INTEGER NOT NULL DEFAULT 0,
    "AngerCount" INTEGER NOT NULL DEFAULT 0,
    "FearCount" INTEGER NOT NULL DEFAULT 0,
    "NeutralCount" INTEGER NOT NULL DEFAULT 0,
    "FlaggedCount" INTEGER NOT NULL DEFAULT 0,
    "OverallPolarity" DECIMAL(4,3) NOT NULL DEFAULT 0,
    "IsAutomated" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Foreign key constraint
    CONSTRAINT fk_profile_emotion_summaries_profile 
        FOREIGN KEY ("ProfileId") REFERENCES "Sivar_Profiles"("Id") ON DELETE CASCADE,
    
    -- Unique constraint to prevent duplicates
    CONSTRAINT uq_profile_emotion_summary 
        UNIQUE("ProfileId", "TimeWindow", "StartDate")
);

-- ============ PART 5: Create Indexes for ProfileEmotionSummaries ============

CREATE INDEX IF NOT EXISTS idx_profile_emotion_profile_id 
ON "Sivar_ProfileEmotionSummaries"("ProfileId");

CREATE INDEX IF NOT EXISTS idx_profile_emotion_time_window 
ON "Sivar_ProfileEmotionSummaries"("TimeWindow");

CREATE INDEX IF NOT EXISTS idx_profile_emotion_dates 
ON "Sivar_ProfileEmotionSummaries"("StartDate", "EndDate");

CREATE INDEX IF NOT EXISTS idx_profile_emotion_dominant 
ON "Sivar_ProfileEmotionSummaries"("DominantEmotion") 
WHERE "DominantEmotion" IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_profile_emotion_flagged 
ON "Sivar_ProfileEmotionSummaries"("FlaggedCount") 
WHERE "FlaggedCount" > 0;

-- ============ PART 6: Add Comments ============

COMMENT ON COLUMN "Sivar_Posts"."PrimaryEmotion" IS 'Primary detected emotion: Joy, Sadness, Anger, Fear, or Neutral';
COMMENT ON COLUMN "Sivar_Posts"."EmotionScore" IS 'Confidence score for primary emotion (0.0-1.0)';
COMMENT ON COLUMN "Sivar_Posts"."SentimentPolarity" IS 'Sentiment polarity score (-1.0 = negative, +1.0 = positive)';
COMMENT ON COLUMN "Sivar_Posts"."HasAnger" IS 'Flag indicating anger above threshold (for moderation)';
COMMENT ON COLUMN "Sivar_Posts"."NeedsReview" IS 'Flag indicating content needs manual review';

COMMENT ON TABLE "Sivar_ProfileEmotionSummaries" IS 'Aggregated emotion statistics per profile over time periods';
COMMENT ON COLUMN "Sivar_ProfileEmotionSummaries"."TimeWindow" IS 'Aggregation period: hourly, daily, weekly, monthly';
COMMENT ON COLUMN "Sivar_ProfileEmotionSummaries"."DominantEmotion" IS 'Most frequent primary emotion in this period';
COMMENT ON COLUMN "Sivar_ProfileEmotionSummaries"."IsAutomated" IS 'True if calculated by TimescaleDB continuous aggregate';

-- ============ Migration Complete ============
-- Run this script against your PostgreSQL database
-- Verify with: SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'Sivar_Posts' AND column_name LIKE '%Emotion%' OR column_name LIKE '%Sentiment%';
