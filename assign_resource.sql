-- Assign the barber resource "Carlos - Barbero Senior" to your profile
UPDATE "Sivar_BookableResources" 
SET "AssignedProfileId" = '27829549-5bb7-41ec-83e0-3a86b8fdf173' 
WHERE "Name" = 'Carlos - Barbero Senior';

-- Verify the update
SELECT "Id", "Name", "AssignedProfileId" 
FROM "Sivar_BookableResources" 
WHERE "Name" = 'Carlos - Barbero Senior';
