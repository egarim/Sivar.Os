-- ============================================================================
-- Phase 3: PostgreSQL Full-Text Search - Add tsvector Columns
-- ============================================================================
-- Run this SQL after recreating the database from EF Core migrations
-- This adds the computed tsvector columns and indexes for multi-language search

-- Add language-specific search vector (with stemming based on Post.Language)
ALTER TABLE "Sivar_Posts" 
ADD COLUMN "SearchVector" tsvector 
GENERATED ALWAYS AS (
    to_tsvector(
        CASE 
            WHEN "Language" = 'en' THEN 'english'::regconfig
            WHEN "Language" = 'es' THEN 'spanish'::regconfig
            WHEN "Language" = 'fr' THEN 'french'::regconfig
            WHEN "Language" = 'de' THEN 'german'::regconfig
            WHEN "Language" = 'pt' THEN 'portuguese'::regconfig
            WHEN "Language" = 'it' THEN 'italian'::regconfig
            WHEN "Language" = 'nl' THEN 'dutch'::regconfig
            WHEN "Language" = 'ru' THEN 'russian'::regconfig
            WHEN "Language" = 'sv' THEN 'swedish'::regconfig
            WHEN "Language" = 'no' THEN 'norwegian'::regconfig
            WHEN "Language" = 'da' THEN 'danish'::regconfig
            WHEN "Language" = 'fi' THEN 'finnish'::regconfig
            WHEN "Language" = 'tr' THEN 'turkish'::regconfig
            WHEN "Language" = 'ro' THEN 'romanian'::regconfig
            WHEN "Language" = 'ar' THEN 'arabic'::regconfig
            ELSE 'simple'::regconfig
        END,
        coalesce("Title", '') || ' ' || "Content"
    )
) STORED;

-- Add universal/simple search vector (no stemming, works for all languages)
ALTER TABLE "Sivar_Posts" 
ADD COLUMN "SearchVectorSimple" tsvector 
GENERATED ALWAYS AS (
    to_tsvector('simple', coalesce("Title", '') || ' ' || "Content")
) STORED;

-- Create GIN indexes for fast full-text search
CREATE INDEX "IX_Posts_SearchVector_Gin" ON "Sivar_Posts" USING gin("SearchVector");
CREATE INDEX "IX_Posts_SearchVectorSimple_Gin" ON "Sivar_Posts" USING gin("SearchVectorSimple");

-- Verify the columns were created
SELECT 
    column_name, 
    data_type, 
    is_generated,
    generation_expression
FROM information_schema.columns
WHERE table_name = 'Sivar_Posts' 
  AND column_name IN ('SearchVector', 'SearchVectorSimple');

-- Verify the indexes were created
SELECT 
    indexname, 
    indexdef
FROM pg_indexes
WHERE tablename = 'Sivar_Posts'
  AND indexname LIKE '%Search%';
