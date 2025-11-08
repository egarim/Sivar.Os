-- Check seeded users
SELECT "FirstName", "LastName", "Email", "KeycloakId" 
FROM "Sivar_Users" 
WHERE "IsDeleted" = false 
ORDER BY "FirstName";

-- Check seeded profiles
SELECT p."DisplayName", p."Handle", pt."DisplayName" as ProfileType, p."Bio"
FROM "Sivar_Profiles" p
JOIN "Sivar_ProfileTypes" pt ON p."ProfileTypeId" = pt."Id"
WHERE p."IsDeleted" = false
ORDER BY p."DisplayName";

-- Check seeded posts
SELECT p."Content", pr."DisplayName" as Author, p."CreatedAt"
FROM "Sivar_Posts" p 
JOIN "Sivar_Profiles" pr ON p."ProfileId" = pr."Id"
WHERE p."IsDeleted" = false
ORDER BY p."CreatedAt" DESC;

-- Check follow relationships
SELECT 
    follower."DisplayName" as Follower,
    followed."DisplayName" as Following,
    pf."FollowedAt"
FROM "Sivar_ProfileFollowers" pf
JOIN "Sivar_Profiles" follower ON pf."FollowerProfileId" = follower."Id"
JOIN "Sivar_Profiles" followed ON pf."FollowedProfileId" = followed."Id"
WHERE pf."IsDeleted" = false;

-- Count summary
SELECT 
    (SELECT COUNT(*) FROM "Sivar_Users" WHERE "IsDeleted" = false) as TotalUsers,
    (SELECT COUNT(*) FROM "Sivar_Profiles" WHERE "IsDeleted" = false) as TotalProfiles,
    (SELECT COUNT(*) FROM "Sivar_Posts" WHERE "IsDeleted" = false) as TotalPosts,
    (SELECT COUNT(*) FROM "Sivar_ProfileFollowers" WHERE "IsDeleted" = false) as TotalFollows,
    (SELECT COUNT(*) FROM "Sivar_Reactions" WHERE "IsDeleted" = false) as TotalReactions;