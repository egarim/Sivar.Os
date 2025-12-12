-- =====================================================
-- Test Script: Add sample contacts to demo profiles
-- Purpose: Test Phase 1 Contact Actions in the app
-- Run after: Updater.cs has seeded ContactTypes
-- =====================================================

-- Step 1: Verify contact types exist
SELECT "Key", "DisplayName", "Category" FROM "Sivar_ContactTypes" LIMIT 5;

-- Step 2: Get restaurant profile IDs
SELECT "Id", "DisplayName", "Handle" 
FROM "Sivar_Profiles" 
WHERE "DisplayName" LIKE '%Pupusería%' OR "DisplayName" LIKE '%Restaurante%'
LIMIT 5;

-- Step 3: Add contacts to the first restaurant profile
-- Replace 'YOUR_PROFILE_ID' with an actual profile ID from step 2

-- EXAMPLE: Add WhatsApp, Phone, and Instagram to a profile
-- Uncomment and modify the INSERT below:

/*
INSERT INTO "Sivar_BusinessContactInfos" 
("Id", "ProfileId", "ContactTypeId", "Value", "Label", "CountryCode", "SortOrder", "IsPrimary", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted")
VALUES 
-- WhatsApp (primary contact)
(gen_random_uuid(), 
 'YOUR_PROFILE_ID'::uuid,  -- Replace with actual profile ID
 'c0000002-0000-0000-0000-000000000001', -- WhatsApp
 '78901234', 
 'Reservaciones', 
 '503', 
 1, 
 true, 
 true, 
 NOW(), 
 NOW(), 
 false),

-- Phone
(gen_random_uuid(), 
 'YOUR_PROFILE_ID'::uuid,  -- Replace with actual profile ID
 'c0000001-0000-0000-0000-000000000001', -- Phone
 '22001234', 
 'Llamar', 
 '503', 
 2, 
 false, 
 true, 
 NOW(), 
 NOW(), 
 false),

-- Instagram
(gen_random_uuid(), 
 'YOUR_PROFILE_ID'::uuid,  -- Replace with actual profile ID
 'c0000004-0000-0000-0000-000000000002', -- Instagram
 'pupuseria_lachampas', 
 NULL, 
 NULL, 
 3, 
 false, 
 true, 
 NOW(), 
 NOW(), 
 false);
*/

-- Step 4: Verify contacts were added
SELECT 
    bci."Value",
    bci."Label",
    bci."CountryCode",
    ct."Key" as "ContactType",
    ct."DisplayName",
    ct."Icon",
    p."DisplayName" as "Profile"
FROM "Sivar_BusinessContactInfos" bci
JOIN "Sivar_ContactTypes" ct ON bci."ContactTypeId" = ct."Id"
JOIN "Sivar_Profiles" p ON bci."ProfileId" = p."Id"
WHERE bci."IsDeleted" = false;

-- Quick test: Add contacts to ALL restaurant profiles at once
-- This adds WhatsApp and Phone to every restaurant

/*
-- Add WhatsApp to all restaurants
INSERT INTO "Sivar_BusinessContactInfos" 
("Id", "ProfileId", "ContactTypeId", "Value", "Label", "CountryCode", "SortOrder", "IsPrimary", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted")
SELECT 
    gen_random_uuid(),
    p."Id",
    'c0000002-0000-0000-0000-000000000001', -- WhatsApp
    '7' || LPAD((random() * 9999999)::int::text, 7, '0'), -- Random phone
    'WhatsApp',
    '503',
    1,
    true,
    true,
    NOW(),
    NOW(),
    false
FROM "Sivar_Profiles" p
WHERE p."DisplayName" LIKE '%Pupusería%' 
   OR p."DisplayName" LIKE '%Restaurante%'
   OR p."Handle" LIKE '%pupuseria%'
   OR p."Handle" LIKE '%restaurant%';

-- Add Phone to all restaurants  
INSERT INTO "Sivar_BusinessContactInfos" 
("Id", "ProfileId", "ContactTypeId", "Value", "Label", "CountryCode", "SortOrder", "IsPrimary", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted")
SELECT 
    gen_random_uuid(),
    p."Id",
    'c0000001-0000-0000-0000-000000000001', -- Phone
    '22' || LPAD((random() * 999999)::int::text, 6, '0'), -- Random phone
    'Llamar',
    '503',
    2,
    false,
    true,
    NOW(),
    NOW(),
    false
FROM "Sivar_Profiles" p
WHERE p."DisplayName" LIKE '%Pupusería%' 
   OR p."DisplayName" LIKE '%Restaurante%'
   OR p."Handle" LIKE '%pupuseria%'
   OR p."Handle" LIKE '%restaurant%';
*/
