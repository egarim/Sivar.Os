-- Fix Schema Mismatches for Booking System
-- Run this to align database with EF Core entity definitions

BEGIN;

-- 1. Fix ResourceAvailability table
-- Remove TimeZone column (not in entity)
-- Add Label column (exists in entity)
ALTER TABLE "Sivar_ResourceAvailability" 
    DROP COLUMN IF EXISTS "TimeZone";

ALTER TABLE "Sivar_ResourceAvailability" 
    ADD COLUMN IF NOT EXISTS "Label" character varying(100) NULL;

-- 2. Verify all booking tables have BaseEntity columns
-- (CreatedAt, UpdatedAt, IsDeleted, DeletedAt are inherited from BaseEntity)

-- These should all have these columns already, but let's verify
DO $$
DECLARE
    tables text[] := ARRAY[
        'Sivar_BookableResources',
        'Sivar_ResourceServices',
        'Sivar_ResourceAvailability',
        'Sivar_ResourceExceptions',
        'Sivar_ResourceBookings'
    ];
    tbl text;
BEGIN
    FOREACH tbl IN ARRAY tables
    LOOP
        -- Check if CreatedAt exists
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = tbl AND column_name = 'CreatedAt'
        ) THEN
            RAISE NOTICE 'Table % missing CreatedAt', tbl;
        END IF;
        
        -- Check if UpdatedAt exists
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = tbl AND column_name = 'UpdatedAt'
        ) THEN
            RAISE NOTICE 'Table % missing UpdatedAt', tbl;
        END IF;
        
        -- Check if IsDeleted exists
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = tbl AND column_name = 'IsDeleted'
        ) THEN
            RAISE NOTICE 'Table % missing IsDeleted', tbl;
        END IF;
        
        -- Check if DeletedAt exists
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns 
            WHERE table_name = tbl AND column_name = 'DeletedAt'
        ) THEN
            RAISE NOTICE 'Table % missing DeletedAt', tbl;
        END IF;
    END LOOP;
END $$;

COMMIT;

-- Verify the fix
SELECT 'Schema fixes applied!' AS result;

-- Show ResourceAvailability columns
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'Sivar_ResourceAvailability'
ORDER BY ordinal_position;
