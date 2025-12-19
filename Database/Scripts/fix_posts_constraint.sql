-- Fix for EF Core AlternateKey constraint issue on Posts table
-- Run this once to reset the state

-- Check if the constraint exists
SELECT conname, contype 
FROM pg_constraint 
WHERE conrelid = '"Sivar_Posts"'::regclass 
AND conname LIKE '%AK%';

-- If you need to manually drop and recreate, uncomment these:
-- The issue is EF Core trying to drop an index that has a constraint on it
-- Usually this resolves itself after a clean database migration

-- Option 1: Skip schema update on second run (in XAF WinApplication)
-- Change DatabaseVersionMismatch handler to not call UpdateSchema if already up to date

-- Option 2: Drop the constraint first, then the index
-- ALTER TABLE "Sivar_Posts" DROP CONSTRAINT IF EXISTS "AK_Sivar_Posts_Id";
-- DROP INDEX IF EXISTS "AK_Sivar_Posts_Id";
