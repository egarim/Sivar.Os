-- ============================================================================
-- 005_AddCategoryKeysForMultilingualSearch.sql
-- Phase 6: Multilingual Search Architecture - CategoryKeys and CategoryDefinitions
-- ============================================================================
-- Purpose: 
--   Enable multilingual search using English-First Query Pattern.
--   User searches "pizzerías" → normalizes to ["pizza"] → matches CategoryKeys
-- ============================================================================

-- 1. Create CategoryDefinitions table if not exists (EF will also create this)
CREATE TABLE IF NOT EXISTS "Sivar_CategoryDefinitions" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "Key" varchar(100) NOT NULL,
    "DisplayNameEn" varchar(200) NOT NULL,
    "DisplayNameEs" varchar(200) NOT NULL,
    "ParentKey" varchar(100),
    "Synonyms" text[] NOT NULL DEFAULT '{}',
    "Description" varchar(500),
    "IsActive" boolean NOT NULL DEFAULT true,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    CONSTRAINT "UQ_CategoryDefinitions_Key" UNIQUE ("Key")
);

-- 2. Create GIN index on Synonyms for fast ANY() queries
CREATE INDEX IF NOT EXISTS "IX_CategoryDefinitions_Synonyms_GIN" 
    ON "Sivar_CategoryDefinitions" USING gin ("Synonyms");

-- 3. Create index for active categories
CREATE INDEX IF NOT EXISTS "IX_CategoryDefinitions_Active_SortOrder" 
    ON "Sivar_CategoryDefinitions" ("IsActive", "SortOrder");

-- 4. Add CategoryKeys column to Posts if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Sivar_Posts' AND column_name = 'CategoryKeys'
    ) THEN
        ALTER TABLE "Sivar_Posts" ADD COLUMN "CategoryKeys" text[] NOT NULL DEFAULT '{}';
    END IF;
END $$;

-- 5. Create GIN index on Posts.CategoryKeys for fast containment queries
CREATE INDEX IF NOT EXISTS "IX_Posts_CategoryKeys_Gin" 
    ON "Sivar_Posts" USING gin ("CategoryKeys");

-- 6. Add CategoryKeys column to Profiles if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Sivar_Profiles' AND column_name = 'CategoryKeys'
    ) THEN
        ALTER TABLE "Sivar_Profiles" ADD COLUMN "CategoryKeys" text[] NOT NULL DEFAULT '{}';
    END IF;
END $$;

-- 7. Create GIN index on Profiles.CategoryKeys
CREATE INDEX IF NOT EXISTS "IX_Profiles_CategoryKeys_Gin" 
    ON "Sivar_Profiles" USING gin ("CategoryKeys");

-- ============================================================================
-- Seed initial category definitions
-- ============================================================================
INSERT INTO "Sivar_CategoryDefinitions" ("Id", "Key", "DisplayNameEn", "DisplayNameEs", "ParentKey", "Synonyms", "Description", "SortOrder", "CreatedAt", "UpdatedAt")
VALUES 
    (gen_random_uuid(), 'restaurant', 'Restaurant', 'Restaurante', NULL, 
     ARRAY['restaurant', 'restaurants', 'restaurante', 'restaurantes', 'dining', 'eatery', 'food place', 'comedor'],
     'General dining establishment', 1, now(), now()),
    (gen_random_uuid(), 'pizza', 'Pizza Restaurant', 'Pizzería', 'restaurant',
     ARRAY['pizza', 'pizzeria', 'pizzerias', 'pizzería', 'pizzerías', 'pizza place', 'pizza restaurant', 'lugar de pizza'],
     'Pizza-focused restaurant', 2, now(), now()),
    (gen_random_uuid(), 'cafe', 'Café', 'Cafetería', 'restaurant',
     ARRAY['cafe', 'café', 'cafeteria', 'cafetería', 'coffee shop', 'coffee'],
     'Coffee and light food establishment', 3, now(), now()),
    (gen_random_uuid(), 'fast_food', 'Fast Food', 'Comida Rápida', 'restaurant',
     ARRAY['fast food', 'comida rápida', 'comida rapida', 'hamburger', 'hamburguesa', 'burger'],
     'Quick service restaurant', 4, now(), now()),
    (gen_random_uuid(), 'bank', 'Bank', 'Banco', NULL,
     ARRAY['bank', 'banks', 'banco', 'bancos', 'banking', 'financial institution'],
     'Financial institution', 10, now(), now()),
    (gen_random_uuid(), 'hotel', 'Hotel', 'Hotel', NULL,
     ARRAY['hotel', 'hotels', 'hoteles', 'lodging', 'alojamiento', 'hospedaje'],
     'Lodging establishment', 20, now(), now()),
    (gen_random_uuid(), 'pharmacy', 'Pharmacy', 'Farmacia', NULL,
     ARRAY['pharmacy', 'pharmacies', 'farmacia', 'farmacias', 'drugstore', 'medicine'],
     'Medicine and health products store', 30, now(), now()),
    (gen_random_uuid(), 'hospital', 'Hospital', 'Hospital', NULL,
     ARRAY['hospital', 'hospitals', 'hospitales', 'clinic', 'clínica', 'medical center', 'centro médico'],
     'Medical facility', 31, now(), now()),
    (gen_random_uuid(), 'government_office', 'Government Office', 'Oficina de Gobierno', NULL,
     ARRAY['government', 'gobierno', 'government office', 'oficina de gobierno', 'municipal', 'alcaldía'],
     'Government administrative office', 40, now(), now()),
    (gen_random_uuid(), 'dui_office', 'DUI Office', 'Oficina de DUI', 'government_office',
     ARRAY['dui', 'dui office', 'oficina de dui', 'identity card', 'tarjeta de identidad'],
     'National ID card office', 41, now(), now()),
    (gen_random_uuid(), 'passport_office', 'Passport Office', 'Oficina de Pasaportes', 'government_office',
     ARRAY['passport', 'pasaporte', 'passport office', 'oficina de pasaportes'],
     'Passport issuance office', 42, now(), now()),
    (gen_random_uuid(), 'tourist_attraction', 'Tourist Attraction', 'Atracción Turística', NULL,
     ARRAY['tourist', 'tourism', 'turismo', 'attraction', 'atracción', 'sightseeing'],
     'Place of tourist interest', 50, now(), now()),
    (gen_random_uuid(), 'beach', 'Beach', 'Playa', 'tourist_attraction',
     ARRAY['beach', 'beaches', 'playa', 'playas', 'coast', 'costa'],
     'Coastal beach area', 51, now(), now()),
    (gen_random_uuid(), 'museum', 'Museum', 'Museo', 'tourist_attraction',
     ARRAY['museum', 'museums', 'museo', 'museos', 'gallery', 'galería'],
     'Cultural museum or gallery', 52, now(), now())
ON CONFLICT ("Key") DO NOTHING;

-- ============================================================================
-- Migration of existing data
-- ============================================================================
-- Update existing restaurant posts with CategoryKeys based on content/title
UPDATE "Sivar_Posts" p
SET "CategoryKeys" = ARRAY['restaurant', 'salvadoran']
WHERE p."PostType" = 2  -- BusinessLocation
AND p."IsDeleted" = false
AND (p."Content" ILIKE '%pupusería%' OR p."Content" ILIKE '%pupuseria%' OR p."Content" ILIKE '%típico%')
AND array_length(p."CategoryKeys", 1) IS NULL OR array_length(p."CategoryKeys", 1) = 0;

UPDATE "Sivar_Posts" p
SET "CategoryKeys" = ARRAY['pizza', 'restaurant']
WHERE p."PostType" = 2
AND p."IsDeleted" = false
AND (p."Content" ILIKE '%pizza%' OR p."Title" ILIKE '%pizza%')
AND array_length(p."CategoryKeys", 1) IS NULL OR array_length(p."CategoryKeys", 1) = 0;

UPDATE "Sivar_Posts" p
SET "CategoryKeys" = ARRAY['cafe', 'restaurant']
WHERE p."PostType" = 2
AND p."IsDeleted" = false
AND (p."Content" ILIKE '%café%' OR p."Content" ILIKE '%coffee%' OR p."Title" ILIKE '%café%')
AND array_length(p."CategoryKeys", 1) IS NULL OR array_length(p."CategoryKeys", 1) = 0;

UPDATE "Sivar_Posts" p
SET "CategoryKeys" = ARRAY['government_office']
WHERE p."PostType" = 5  -- Procedure
AND p."IsDeleted" = false
AND array_length(p."CategoryKeys", 1) IS NULL OR array_length(p."CategoryKeys", 1) = 0;

-- Verification query
SELECT 'CategoryKeys Migration Summary' as report;
SELECT 
    CASE WHEN array_length("CategoryKeys", 1) > 0 THEN 'Has CategoryKeys' ELSE 'No CategoryKeys' END as status,
    COUNT(*) as count
FROM "Sivar_Posts"
WHERE "IsDeleted" = false
GROUP BY CASE WHEN array_length("CategoryKeys", 1) > 0 THEN 'Has CategoryKeys' ELSE 'No CategoryKeys' END;
