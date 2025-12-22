-- Check all bookings
SELECT 
    b."Id", 
    b."StartTime", 
    b."EndTime", 
    b."Status",
    b."ConfirmationCode",
    r."Name" as "ResourceName",
    r."AssignedProfileId",
    p."DisplayName" as "CustomerName"
FROM "Sivar_ResourceBookings" b
LEFT JOIN "Sivar_BookableResources" r ON b."ResourceId" = r."Id"
LEFT JOIN "Sivar_Profiles" p ON b."CustomerProfileId" = p."Id"
ORDER BY b."StartTime";

-- Check resources and their AssignedProfileId
SELECT 
    "Id", 
    "Name",
    "ProfileId",
    "AssignedProfileId"
FROM "Sivar_BookableResources";

-- Check your profile
SELECT "Id", "DisplayName" FROM "Sivar_Profiles" WHERE "DisplayName" LIKE '%Joche%';
