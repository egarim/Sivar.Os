-- Migration: Add PreferredLanguage column to Profiles table
-- Purpose: Store user's language preference (en-US, es-ES, etc.)
-- Date: 2025-11-01

-- Add PreferredLanguage column
ALTER TABLE "Profiles" 
ADD COLUMN "PreferredLanguage" VARCHAR(10) NULL;

-- Add comment to column
COMMENT ON COLUMN "Profiles"."PreferredLanguage" IS 'User preferred language in BCP 47 format (e.g., en-US, es-ES)';

-- Create index for faster lookups
CREATE INDEX "IX_Profiles_PreferredLanguage" ON "Profiles" ("PreferredLanguage");

-- Optional: Set default value for existing users (can be NULL for browser default)
-- UPDATE "Profiles" SET "PreferredLanguage" = 'en-US' WHERE "PreferredLanguage" IS NULL;
