-- Check QuickActions status in database
SELECT 
    qa."Id",
    qa."Label",
    qa."DefaultQuery",
    qa."IsActive",
    qa."IsDeleted",
    qa."SortOrder",
    cbs."Key" AS "SettingsKey",
    cbs."Culture"
FROM "QuickActions" qa
LEFT JOIN "ChatBotSettings" cbs ON qa."ChatBotSettingsId" = cbs."Id"
ORDER BY cbs."Culture", qa."SortOrder";
