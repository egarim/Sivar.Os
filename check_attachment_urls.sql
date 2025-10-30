-- Check what URLs are stored in PostAttachment table
SELECT 
    "Id",
    "PostId",
    "FileId",
    "Url",
    "OriginalFileName",
    "MimeType",
    "FileSizeBytes",
    "CreatedAt"
FROM "Sivar_PostAttachments"
WHERE "IsDeleted" = false
ORDER BY "CreatedAt" DESC
LIMIT 5;
