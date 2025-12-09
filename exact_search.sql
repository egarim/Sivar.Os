-- Exact search test
SELECT 
    p."Id", 
    p."Content",
    p."Location_City",
    pr."DisplayName" as author
FROM "Sivar_Posts" p 
JOIN "Sivar_Profiles" pr ON p."ProfileId" = pr."Id" 
WHERE 
    LOWER(p."Content") LIKE '%pizzeria%';
