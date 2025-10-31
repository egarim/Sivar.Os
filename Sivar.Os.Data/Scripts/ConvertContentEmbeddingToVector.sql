-- =====================================================
-- Script: Convert ContentEmbedding from TEXT to VECTOR(384)
-- Purpose: Convert existing ContentEmbedding column to pgvector type
--          This is REQUIRED because EF Core 9.0 cannot handle pgvector types
-- Date: October 31, 2025
-- =====================================================

-- Step 1: Ensure pgvector extension is installed
CREATE EXTENSION IF NOT EXISTS vector;

-- Step 2: Check current column type (optional - for verification)
SELECT column_name, data_type, udt_name
FROM information_schema.columns
WHERE table_name = 'Sivar_Posts' 
AND column_name = 'ContentEmbedding';

-- Step 3: Check if column exists, if not create it
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'Sivar_Posts' 
        AND column_name = 'ContentEmbedding'
    ) THEN
        ALTER TABLE "Sivar_Posts" 
        ADD COLUMN "ContentEmbedding" vector(384);
        RAISE NOTICE 'Column ContentEmbedding created as vector(384)';
    ELSE
        -- Column exists, check if it's already vector type
        IF EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_name = 'Sivar_Posts' 
            AND column_name = 'ContentEmbedding'
            AND udt_name = 'vector'
        ) THEN
            RAISE NOTICE 'Column ContentEmbedding is already vector type';
        ELSE
            -- Convert from text/other type to vector
            ALTER TABLE "Sivar_Posts" 
            ALTER COLUMN "ContentEmbedding" TYPE vector(384) 
            USING CASE 
                WHEN "ContentEmbedding" IS NULL THEN NULL
                WHEN "ContentEmbedding" = '' THEN NULL
                ELSE "ContentEmbedding"::vector
            END;
            RAISE NOTICE 'Column ContentEmbedding converted to vector(384)';
        END IF;
    END IF;
END $$;

-- Step 4: Create HNSW index for fast similarity search
-- Drop index if it exists first
DROP INDEX IF EXISTS "IX_Posts_ContentEmbedding_Hnsw";

-- Create new HNSW index with cosine similarity
CREATE INDEX "IX_Posts_ContentEmbedding_Hnsw" 
ON "Sivar_Posts" 
USING hnsw ("ContentEmbedding" vector_cosine_ops);

-- Step 5: Verify the change
SELECT column_name, data_type, udt_name
FROM information_schema.columns
WHERE table_name = 'Sivar_Posts' 
AND column_name = 'ContentEmbedding';

-- Step 6: Verify the index
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'Sivar_Posts'
AND indexname = 'IX_Posts_ContentEmbedding_Hnsw';

-- =====================================================
-- IMPORTANT NOTES:
-- - This script is idempotent (safe to run multiple times)
-- - EF Core 9.0 CANNOT handle pgvector types - column MUST be ignored in PostConfiguration.cs
-- - Updates to ContentEmbedding MUST use raw SQL (see PostRepository.UpdateContentEmbeddingAsync)
-- - The column is ignored by EF Core but exists in the database
-- - HNSW index improves performance for similarity search queries using <=> operator
-- =====================================================
