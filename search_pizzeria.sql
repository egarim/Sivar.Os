-- Search for pizzeria campestre / ubicacion post
SELECT 
    p."Id", 
    SUBSTRING(p."Content", 1, 200) as content_preview,
    p."PostType", 
    ST_AsText(p."GeoLocation") as geo,
    p."City", 
    p."State", 
    p."Country",
    p."Latitude",
    p."Longitude",
    pr."DisplayName" as author,
    p."CreatedAt"
FROM "Sivar_Posts" p 
JOIN "Sivar_Profiles" pr ON p."ProfileId" = pr."Id" 
WHERE 
    LOWER(p."Content") LIKE '%pizzeria%' 
    OR LOWER(p."Content") LIKE '%campestre%' 
    OR LOWER(p."Content") LIKE '%ubicacion%'
    OR LOWER(p."Content") LIKE '%pizza%'
ORDER BY p."CreatedAt" DESC;

-- Also check all posts with location data
SELECT 
    p."Id",
    SUBSTRING(p."Content", 1, 100) as content,
    p."City",
    p."Country",
    ST_AsText(p."GeoLocation") as geo,
    pr."DisplayName" as author
FROM "Sivar_Posts" p
JOIN "Sivar_Profiles" pr ON p."ProfileId" = pr."Id"
WHERE p."GeoLocation" IS NOT NULL
ORDER BY p."CreatedAt" DESC
LIMIT 10;
