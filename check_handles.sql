-- Check Handle values in profiles
SELECT 
    SUBSTRING("Id"::TEXT, 1, 8) || '...' AS "ID",
    "DisplayName",
    COALESCE("Handle", 'NULL') AS "Handle",
    "IsActive"
FROM "Sivar_Profiles"
WHERE "IsDeleted" = false
ORDER BY "CreatedAt" DESC
LIMIT 10;
