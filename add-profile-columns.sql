-- Add missing Profile columns for advertising and token tracking

ALTER TABLE "Sivar_Profiles" 
ADD COLUMN IF NOT EXISTS "AdBudget" numeric(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS "AdQualityScore" double precision DEFAULT 0,
ADD COLUMN IF NOT EXISTS "AdSpentToday" numeric(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS "AdTargetKeywords" text DEFAULT '',
ADD COLUMN IF NOT EXISTS "AdTargetRadiusKm" double precision DEFAULT 0,
ADD COLUMN IF NOT EXISTS "DailyAdLimit" numeric(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS "MaxBidPerClick" numeric(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS "SponsoredClicks" bigint DEFAULT 0,
ADD COLUMN IF NOT EXISTS "SponsoredEnabled" boolean DEFAULT false,
ADD COLUMN IF NOT EXISTS "SponsoredImpressions" bigint DEFAULT 0,
ADD COLUMN IF NOT EXISTS "TokenAllowanceLimit" integer DEFAULT 0,
ADD COLUMN IF NOT EXISTS "TokenAllowancePeriod" integer DEFAULT 0,
ADD COLUMN IF NOT EXISTS "TokenPeriodStartedAt" timestamp with time zone,
ADD COLUMN IF NOT EXISTS "TokensUsedThisPeriod" integer DEFAULT 0,
ADD COLUMN IF NOT EXISTS "TotalAdSpent" numeric(18,2) DEFAULT 0,
ADD COLUMN IF NOT EXISTS "TotalTokensUsed" bigint DEFAULT 0,
ADD COLUMN IF NOT EXISTS "ChatDisplayMode" integer DEFAULT 0,
ADD COLUMN IF NOT EXISTS "CategoryKeys" jsonb DEFAULT '{}';

-- Update existing rows to have valid defaults
UPDATE "Sivar_Profiles" 
SET "AdBudget" = COALESCE("AdBudget", 0),
    "AdQualityScore" = COALESCE("AdQualityScore", 0),
    "AdSpentToday" = COALESCE("AdSpentToday", 0),
    "AdTargetKeywords" = COALESCE("AdTargetKeywords", ''),
    "AdTargetRadiusKm" = COALESCE("AdTargetRadiusKm", 0),
    "DailyAdLimit" = COALESCE("DailyAdLimit", 0),
    "MaxBidPerClick" = COALESCE("MaxBidPerClick", 0),
    "SponsoredClicks" = COALESCE("SponsoredClicks", 0),
    "SponsoredEnabled" = COALESCE("SponsoredEnabled", false),
    "SponsoredImpressions" = COALESCE("SponsoredImpressions", 0),
    "TokenAllowanceLimit" = COALESCE("TokenAllowanceLimit", 0),
    "TokenAllowancePeriod" = COALESCE("TokenAllowancePeriod", 0),
    "TokensUsedThisPeriod" = COALESCE("TokensUsedThisPeriod", 0),
    "TotalAdSpent" = COALESCE("TotalAdSpent", 0),
    "TotalTokensUsed" = COALESCE("TotalTokensUsed", 0),
    "ChatDisplayMode" = COALESCE("ChatDisplayMode", 0);
