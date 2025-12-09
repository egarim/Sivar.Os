-- Search for pizzeria posts - using correct column names
SELECT 
    p."Id", 
    SUBSTRING(p."Content", 1, 300) as content_preview,
    p."PostType",
    p."Location_City",
    p."Location_State", 
    p."Location_Country",
    p."Location_Latitude",
    p."Location_Longitude",
    ST_AsText(p."GeoLocation") as geo,
    pr."DisplayName" as author,
    p."CreatedAt"
FROM "Sivar_Posts" p 
JOIN "Sivar_Profiles" pr ON p."ProfileId" = pr."Id" 
WHERE 
    LOWER(p."Content") LIKE '%pizza%' 
    OR LOWER(p."Content") LIKE '%campestre%'
ORDER BY p."CreatedAt" DESC
LIMIT 20;
