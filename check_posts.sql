-- Check total counts
SELECT COUNT(*) as total_profiles FROM "Sivar_Profiles";
SELECT COUNT(*) as total_posts FROM "Sivar_Posts";

-- Check for government profiles (g0000001 prefix)
SELECT "Id", "DisplayName", "Handle" FROM "Sivar_Profiles" WHERE "Id"::text LIKE 'g0000001%';

-- Check for government posts (h0000001 prefix)
SELECT "Id", "Title", LEFT("Content", 80) as content FROM "Sivar_Posts" WHERE "Id"::text LIKE 'h0000001%';

-- Check all profile ID prefixes
SELECT LEFT("Id"::text, 8) as prefix, COUNT(*) FROM "Sivar_Profiles" GROUP BY LEFT("Id"::text, 8) ORDER BY COUNT(*) DESC;

-- Check all post ID prefixes  
SELECT LEFT("Id"::text, 8) as prefix, COUNT(*) FROM "Sivar_Posts" GROUP BY LEFT("Id"::text, 8) ORDER BY COUNT(*) DESC;
