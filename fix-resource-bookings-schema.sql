-- Complete Schema Fix for ResourceBookings table
-- Align with actual entity definition

BEGIN;

-- 1. Add missing columns to ResourceBookings
ALTER TABLE "Sivar_ResourceBookings"
    ADD COLUMN IF NOT EXISTS "CancelledBy" integer NULL,
    ADD COLUMN IF NOT EXISTS "CheckedInAt" timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS "GuestCount" integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "OriginalBookingId" uuid NULL,
    ADD COLUMN IF NOT EXISTS "RescheduledToBookingId" uuid NULL;

-- 2. Rename ReviewText to Review (if column exists)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Sivar_ResourceBookings' AND column_name = 'ReviewText'
    ) THEN
        ALTER TABLE "Sivar_ResourceBookings" RENAME COLUMN "ReviewText" TO "Review";
    END IF;
END $$;

-- 3. Add Review column if it doesn't exist (increase length to 2000)
ALTER TABLE "Sivar_ResourceBookings"
    ADD COLUMN IF NOT EXISTS "Review" character varying(2000) NULL;

-- 4. Remove CancelledByProfileId column if it exists (replaced by ConfirmedByProfileId)
-- Actually keep it, entity still has ConfirmedByProfileId

-- 5. Add foreign keys for booking relationships
ALTER TABLE "Sivar_ResourceBookings"
    DROP CONSTRAINT IF EXISTS "FK_Sivar_ResourceBookings_OriginalBooking",
    DROP CONSTRAINT IF EXISTS "FK_Sivar_ResourceBookings_RescheduledToBooking";

ALTER TABLE "Sivar_ResourceBookings"
    ADD CONSTRAINT "FK_Sivar_ResourceBookings_OriginalBooking" 
        FOREIGN KEY ("OriginalBookingId") REFERENCES "Sivar_ResourceBookings"("Id") ON DELETE SET NULL,
    ADD CONSTRAINT "FK_Sivar_ResourceBookings_RescheduledToBooking" 
        FOREIGN KEY ("RescheduledToBookingId") REFERENCES "Sivar_ResourceBookings"("Id") ON DELETE SET NULL;

-- 6. Remove columns that don't exist in entity
-- NoShowAt, FollowUpSent, FollowUpSentAt exist in entity, keep them

COMMIT;

-- Verify the fix
SELECT 'ResourceBookings schema fixed!' AS result;

-- Show all columns
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'Sivar_ResourceBookings'
ORDER BY ordinal_position;
