-- Add Handle column to Sivar_Profiles table
-- This script adds a URL-friendly handle field with unique constraint

-- Step 1: Add the Handle column (nullable first to allow for data population)
ALTER TABLE "Sivar_Profiles" 
ADD COLUMN IF NOT EXISTS "Handle" character varying(50);

-- Step 2: Generate handle values from DisplayName for existing profiles
-- Convert DisplayName to lowercase, replace spaces with hyphens, remove special characters
UPDATE "Sivar_Profiles"
SET "Handle" = lower(
    regexp_replace(
        regexp_replace("DisplayName", '[^a-zA-Z0-9\s-]', '', 'g'),
        '\s+', '-', 'g'
    )
)
WHERE "Handle" IS NULL;

-- Step 3: Handle duplicates by appending the first 8 characters of the ID
WITH duplicates AS (
    SELECT "Id", "Handle",
           ROW_NUMBER() OVER (PARTITION BY "Handle" ORDER BY "CreatedAt") as rn
    FROM "Sivar_Profiles"
)
UPDATE "Sivar_Profiles" p
SET "Handle" = d."Handle" || '-' || substring(p."Id"::text, 1, 8)
FROM duplicates d
WHERE p."Id" = d."Id" AND d.rn > 1;

-- Step 4: Make the column NOT NULL
ALTER TABLE "Sivar_Profiles" 
ALTER COLUMN "Handle" SET NOT NULL;

-- Step 5: Create unique index
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Profiles_Handle" 
ON "Sivar_Profiles" ("Handle");

-- Verify the changes
SELECT "Id", "DisplayName", "Handle" FROM "Sivar_Profiles" LIMIT 10;
