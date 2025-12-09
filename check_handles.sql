-- Check profile handles
SELECT 
    p."Id",
    p."DisplayName",
    p."Handle",
    pt."Name" as profile_type
FROM "Sivar_Profiles" p
LEFT JOIN "Sivar_ProfileTypes" pt ON p."ProfileTypeId" = pt."Id"
ORDER BY p."CreatedAt" DESC
LIMIT 10;
