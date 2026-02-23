-- Photo Studio Booking System Demo Data
-- For "Studio Fotográfico El Salvador"

-- Use an existing profile or create variables
DO $$
DECLARE
    studio_profile_id UUID := 'f25044ff-7fa5-48d0-bed3-f29bd614190b'; -- Using finaltest profile
    resource_id UUID := gen_random_uuid();
    service_wedding_id UUID := gen_random_uuid();
    service_quince_id UUID := gen_random_uuid();
    service_portrait_id UUID := gen_random_uuid();
BEGIN
    -- 1. Create the Photo Studio as a Bookable Resource
    INSERT INTO "Sivar_BookableResources" (
        "Id", "ProfileId", "Name", "Description", "ResourceType", "Category",
        "SlotDurationMinutes", "BufferMinutes", "MaxConcurrentBookings",
        "DefaultPrice", "Currency", "ConfirmationMode",
        "MinAdvanceBookingHours", "MaxAdvanceBookingDays", "CancellationWindowHours",
        "IsActive", "IsVisible", "DisplayOrder", "Tags",
        "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES (
        resource_id,
        studio_profile_id,
        'Studio Fotográfico El Salvador',
        'Estudio profesional de fotografía especializado en bodas, quinceañeras y retratos. Más de 10 años de experiencia capturando momentos especiales.',
        0, -- ResourceType: Person (photographer)
        3, -- Category: Photography
        60, -- Default slot: 1 hour
        30, -- Buffer: 30 minutes between sessions
        1, -- Max concurrent: 1 booking at a time
        150.00,
        'USD',
        1, -- Manual confirmation required
        24, -- Must book 24 hours in advance
        90, -- Can book up to 90 days ahead
        48, -- Cancel 48 hours before
        true,
        true,
        0,
        ARRAY['fotografía', 'bodas', 'quinceañeras', 'retratos', 'eventos']::text[],
        NOW(),
        NOW(),
        false
    );

    RAISE NOTICE 'Created BookableResource: %', resource_id;

    -- 2. Create Services offered by the studio

    -- Wedding Photography Package
    INSERT INTO "Sivar_ResourceServices" (
        "Id", "ResourceId", "Name", "Description",
        "DurationMinutes", "Price", "Currency",
        "IsActive", "DisplayOrder",
        "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES (
        service_wedding_id,
        resource_id,
        'Fotografía de Bodas - Paquete Completo',
        'Cobertura completa de tu boda: ceremonia, recepción y sesión de fotos. Incluye: 8 horas de cobertura, 2 fotógrafos, 500+ fotos editadas, álbum premium 30x30cm, y entrega digital.',
        480, -- 8 hours
        800.00,
        'USD',
        true,
        1,
        NOW(),
        NOW(),
        false
    );

    -- Quinceañera Photography Package
    INSERT INTO "Sivar_ResourceServices" (
        "Id", "ResourceId", "Name", "Description",
        "DurationMinutes", "Price", "Currency",
        "IsActive", "DisplayOrder",
        "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES (
        service_quince_id,
        resource_id,
        'Fotografía de Quinceañera',
        'Sesión completa para tu quinceañera: preparativos, ceremonia y fiesta. Incluye: 4 horas de cobertura, 1 fotógrafo, 250+ fotos editadas, álbum 20x20cm, y entrega digital.',
        240, -- 4 hours
        450.00,
        'USD',
        true,
        2,
        NOW(),
        NOW(),
        false
    );

    -- Professional Portraits
    INSERT INTO "Sivar_ResourceServices" (
        "Id", "ResourceId", "Name", "Description",
        "DurationMinutes", "Price", "Currency",
        "IsActive", "DisplayOrder",
        "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES (
        service_portrait_id,
        resource_id,
        'Retratos Profesionales',
        'Sesión de retratos en estudio o locación. Incluye: 1 hora de sesión, 1 fotógrafo, 30+ fotos editadas, y entrega digital. Ideal para familias, parejas o individual.',
        60, -- 1 hour
        150.00,
        'USD',
        true,
        3,
        NOW(),
        NOW(),
        false
    );

    RAISE NOTICE 'Created 3 ResourceServices';

    -- 3. Set up weekly availability (Tuesday to Sunday)
    
    -- Tuesday: 2PM - 8PM
    INSERT INTO "Sivar_ResourceAvailability" ("Id", "ResourceId", "DayOfWeek", "StartTime", "EndTime", "IsAvailable", "TimeZone", "CreatedAt", "UpdatedAt", "IsDeleted")
    VALUES (gen_random_uuid(), resource_id, 2, '14:00:00', '20:00:00', true, 'America/El_Salvador', NOW(), NOW(), false);
    
    -- Wednesday: 2PM - 8PM
    INSERT INTO "Sivar_ResourceAvailability" ("Id", "ResourceId", "DayOfWeek", "StartTime", "EndTime", "IsAvailable", "TimeZone", "CreatedAt", "UpdatedAt", "IsDeleted")
    VALUES (gen_random_uuid(), resource_id, 3, '14:00:00', '20:00:00', true, 'America/El_Salvador', NOW(), NOW(), false);
    
    -- Thursday: 2PM - 8PM
    INSERT INTO "Sivar_ResourceAvailability" ("Id", "ResourceId", "DayOfWeek", "StartTime", "EndTime", "IsAvailable", "TimeZone", "CreatedAt", "UpdatedAt", "IsDeleted")
    VALUES (gen_random_uuid(), resource_id, 4, '14:00:00', '20:00:00', true, 'America/El_Salvador', NOW(), NOW(), false);
    
    -- Friday: 2PM - 9PM
    INSERT INTO "Sivar_ResourceAvailability" ("Id", "ResourceId", "DayOfWeek", "StartTime", "EndTime", "IsAvailable", "TimeZone", "CreatedAt", "UpdatedAt", "IsDeleted")
    VALUES (gen_random_uuid(), resource_id, 5, '14:00:00', '21:00:00', true, 'America/El_Salvador', NOW(), NOW(), false);
    
    -- Saturday: 9AM - 9PM (premium day)
    INSERT INTO "Sivar_ResourceAvailability" ("Id", "ResourceId", "DayOfWeek", "StartTime", "EndTime", "IsAvailable", "TimeZone", "CreatedAt", "UpdatedAt", "IsDeleted")
    VALUES (gen_random_uuid(), resource_id, 6, '09:00:00', '21:00:00', true, 'America/El_Salvador', NOW(), NOW(), false);
    
    -- Sunday: 9AM - 6PM (premium day)
    INSERT INTO "Sivar_ResourceAvailability" ("Id", "ResourceId", "DayOfWeek", "StartTime", "EndTime", "IsAvailable", "TimeZone", "CreatedAt", "UpdatedAt", "IsDeleted")
    VALUES (gen_random_uuid(), resource_id, 0, '09:00:00', '18:00:00', true, 'America/El_Salvador', NOW(), NOW(), false);

    RAISE NOTICE 'Created 6 availability slots (Tue-Sun)';

    -- 4. Block some dates (example: studio closed for vacation)
    INSERT INTO "Sivar_ResourceExceptions" (
        "Id", "ResourceId", "ExceptionType", 
        "StartDate", "EndDate", 
        "Reason", "IsRecurring",
        "CreatedAt", "UpdatedAt", "IsDeleted"
    ) VALUES (
        gen_random_uuid(),
        resource_id,
        0, -- Blocked/Unavailable
        '2026-03-15 00:00:00+00', -- Start of vacation
        '2026-03-22 23:59:59+00', -- End of vacation
        'Vacaciones del estudio',
        false,
        NOW(),
        NOW(),
        false
    );

    RAISE NOTICE 'Created vacation exception';

    -- Output summary
    RAISE NOTICE '=== Photo Studio Demo Data Created ===';
    RAISE NOTICE 'Studio Profile ID: %', studio_profile_id;
    RAISE NOTICE 'Resource ID: %', resource_id;
    RAISE NOTICE 'Wedding Service ID: %', service_wedding_id;
    RAISE NOTICE 'Quinceañera Service ID: %', service_quince_id;
    RAISE NOTICE 'Portrait Service ID: %', service_portrait_id;
    RAISE NOTICE '=====================================';
END $$;

-- Verify what was created
SELECT 
    'BookableResources' as table_name,
    COUNT(*) as count
FROM "Sivar_BookableResources"
UNION ALL
SELECT 
    'ResourceServices',
    COUNT(*)
FROM "Sivar_ResourceServices"
UNION ALL
SELECT 
    'ResourceAvailability',
    COUNT(*)
FROM "Sivar_ResourceAvailability"
UNION ALL
SELECT 
    'ResourceExceptions',
    COUNT(*)
FROM "Sivar_ResourceExceptions"
UNION ALL
SELECT 
    'ResourceBookings',
    COUNT(*)
FROM "Sivar_ResourceBookings";
