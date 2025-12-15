-- =============================================
-- Script: Add Full-Text Search Columns to Posts
-- Version: 6.0
-- Phase: 3 - PostgreSQL Full-Text Search
-- Description: Adds tsvector columns for language-aware and language-agnostic full-text search
-- Dependencies: PostgreSQL 12+, Posts table must exist
-- Idempotent: Yes (uses IF NOT EXISTS checks)
-- =============================================

-- Add language-aware full-text search vector column
-- Uses Spanish configuration since most content is in Spanish
-- Provides stemming and stop words for Spanish language
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'Sivar_Posts' 
        AND column_name = 'SearchVector'
    ) THEN
        ALTER TABLE "Sivar_Posts"
        ADD COLUMN "SearchVector" tsvector 
        GENERATED ALWAYS AS (
            to_tsvector(
                'spanish'::regconfig,
                COALESCE("Title", '') || ' ' || "Content"
            )
        ) STORED;
        
        RAISE NOTICE 'Added SearchVector column to Sivar_Posts';
    ELSE
        RAISE NOTICE 'SearchVector column already exists in Sivar_Posts';
    END IF;
END $$;

-- Add language-agnostic full-text search vector column
-- Uses 'simple' configuration - no stemming, works for all languages
-- Best for cross-language searches and unsupported languages
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'Sivar_Posts' 
        AND column_name = 'SearchVectorSimple'
    ) THEN
        ALTER TABLE "Sivar_Posts"
        ADD COLUMN "SearchVectorSimple" tsvector 
        GENERATED ALWAYS AS (
            to_tsvector(
                'simple'::regconfig,
                COALESCE("Title", '') || ' ' || "Content"
            )
        ) STORED;
        
        RAISE NOTICE 'Added SearchVectorSimple column to Sivar_Posts';
    ELSE
        RAISE NOTICE 'SearchVectorSimple column already exists in Sivar_Posts';
    END IF;
END $$;

-- Create GIN index on language-aware search vector for fast full-text search
-- This is the primary index for most searches
CREATE INDEX IF NOT EXISTS "IX_Posts_SearchVector_Gin"
ON "Sivar_Posts" USING gin("SearchVector");

-- Create GIN index on language-agnostic search vector
-- Used for cross-language searches
CREATE INDEX IF NOT EXISTS "IX_Posts_SearchVectorSimple_Gin"
ON "Sivar_Posts" USING gin("SearchVectorSimple");

-- Log completion
DO $$
BEGIN
    RAISE NOTICE '✅ Full-text search columns and indexes created successfully';
    RAISE NOTICE '   - SearchVector (language-aware with stemming)';
    RAISE NOTICE '   - SearchVectorSimple (language-agnostic, no stemming)';
    RAISE NOTICE '   - GIN indexes created for fast full-text search';
END $$;
