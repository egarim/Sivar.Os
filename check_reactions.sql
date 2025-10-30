-- Check if reactions are being saved
SELECT 
    "Id",
    "ProfileId",
    "PostId",
    "CommentId",
    "ReactionType",
    "CreatedAt",
    "UpdatedAt",
    "IsDeleted"
FROM "Sivar_Reactions"
WHERE "IsDeleted" = false
ORDER BY "CreatedAt" DESC
LIMIT 20;

-- Count total reactions
SELECT COUNT(*) as TotalReactions
FROM "Sivar_Reactions"
WHERE "IsDeleted" = false;

-- Check reaction counts by type
SELECT 
    "ReactionType",
    COUNT(*) as Count
FROM "Sivar_Reactions"
WHERE "IsDeleted" = false
GROUP BY "ReactionType";
