-- Manual creation of booking system tables
-- Based on original Sivar.Os booking entity definitions

-- 1. BookableResources table
CREATE TABLE IF NOT EXISTS "Sivar_BookableResources" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "ProfileId" uuid NOT NULL,
    "AssignedProfileId" uuid NULL,
    "PostId" uuid NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000) NULL,
    "ResourceType" integer NOT NULL DEFAULT 0,
    "Category" integer NOT NULL DEFAULT 0,
    "ImageUrl" character varying(500) NULL,
    "SlotDurationMinutes" integer NOT NULL DEFAULT 30,
    "BufferMinutes" integer NOT NULL DEFAULT 0,
    "MaxConcurrentBookings" integer NOT NULL DEFAULT 1,
    "DefaultPrice" decimal(18,2) NULL,
    "Currency" character varying(3) NOT NULL DEFAULT 'USD',
    "ConfirmationMode" integer NOT NULL DEFAULT 0,
    "MinAdvanceBookingHours" integer NOT NULL DEFAULT 1,
    "MaxAdvanceBookingDays" integer NOT NULL DEFAULT 30,
    "CancellationWindowHours" integer NOT NULL DEFAULT 24,
    "IsActive" boolean NOT NULL DEFAULT true,
    "IsVisible" boolean NOT NULL DEFAULT true,
    "DisplayOrder" integer NOT NULL DEFAULT 0,
    "MetadataJson" text NULL,
    "Tags" text[] NOT NULL DEFAULT '{}',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    
    CONSTRAINT "FK_Sivar_BookableResources_Profiles_ProfileId" FOREIGN KEY ("ProfileId") 
        REFERENCES "Sivar_Profiles"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Sivar_BookableResources_Profiles_AssignedProfileId" FOREIGN KEY ("AssignedProfileId") 
        REFERENCES "Sivar_Profiles"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Sivar_BookableResources_Posts_PostId" FOREIGN KEY ("PostId") 
        REFERENCES "Sivar_Posts"("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_Sivar_BookableResources_ProfileId" ON "Sivar_BookableResources"("ProfileId");
CREATE INDEX IF NOT EXISTS "IX_Sivar_BookableResources_AssignedProfileId" ON "Sivar_BookableResources"("AssignedProfileId");
CREATE INDEX IF NOT EXISTS "IX_Sivar_BookableResources_ResourceType" ON "Sivar_BookableResources"("ResourceType");
CREATE INDEX IF NOT EXISTS "IX_Sivar_BookableResources_IsActive" ON "Sivar_BookableResources"("IsActive");

-- 2. ResourceServices table
CREATE TABLE IF NOT EXISTS "Sivar_ResourceServices" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "ResourceId" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000) NULL,
    "DurationMinutes" integer NOT NULL,
    "Price" decimal(18,2) NOT NULL,
    "Currency" character varying(3) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "DisplayOrder" integer NOT NULL DEFAULT 0,
    "ImageUrl" character varying(500) NULL,
    "MetadataJson" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    
    CONSTRAINT "FK_Sivar_ResourceServices_BookableResources_ResourceId" FOREIGN KEY ("ResourceId") 
        REFERENCES "Sivar_BookableResources"("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Sivar_ResourceServices_ResourceId" ON "Sivar_ResourceServices"("ResourceId");
CREATE INDEX IF NOT EXISTS "IX_Sivar_ResourceServices_IsActive" ON "Sivar_ResourceServices"("IsActive");

-- 3. ResourceAvailability table
CREATE TABLE IF NOT EXISTS "Sivar_ResourceAvailability" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "ResourceId" uuid NOT NULL,
    "DayOfWeek" integer NOT NULL,
    "StartTime" time without time zone NOT NULL,
    "EndTime" time without time zone NOT NULL,
    "IsAvailable" boolean NOT NULL DEFAULT true,
    "TimeZone" character varying(100) NOT NULL DEFAULT 'UTC',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    
    CONSTRAINT "FK_Sivar_ResourceAvailability_BookableResources_ResourceId" FOREIGN KEY ("ResourceId") 
        REFERENCES "Sivar_BookableResources"("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Sivar_ResourceAvailability_ResourceId" ON "Sivar_ResourceAvailability"("ResourceId");
CREATE INDEX IF NOT EXISTS "IX_Sivar_ResourceAvailability_DayOfWeek" ON "Sivar_ResourceAvailability"("DayOfWeek");

-- 4. ResourceExceptions table (holidays, blocked dates, special hours)
CREATE TABLE IF NOT EXISTS "Sivar_ResourceExceptions" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "ResourceId" uuid NOT NULL,
    "ExceptionType" integer NOT NULL DEFAULT 0,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "StartTime" time without time zone NULL,
    "EndTime" time without time zone NULL,
    "Reason" character varying(500) NULL,
    "IsRecurring" boolean NOT NULL DEFAULT false,
    "RecurrenceRule" character varying(500) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    
    CONSTRAINT "FK_Sivar_ResourceExceptions_BookableResources_ResourceId" FOREIGN KEY ("ResourceId") 
        REFERENCES "Sivar_BookableResources"("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Sivar_ResourceExceptions_ResourceId" ON "Sivar_ResourceExceptions"("ResourceId");
CREATE INDEX IF NOT EXISTS "IX_Sivar_ResourceExceptions_StartDate" ON "Sivar_ResourceExceptions"("StartDate");

-- 5. ResourceBookings table
CREATE TABLE IF NOT EXISTS "Sivar_ResourceBookings" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "ResourceId" uuid NOT NULL,
    "ServiceId" uuid NULL,
    "CustomerProfileId" uuid NOT NULL,
    "StartTime" timestamp with time zone NOT NULL,
    "EndTime" timestamp with time zone NOT NULL,
    "TimeZone" character varying(100) NOT NULL DEFAULT 'UTC',
    "Status" integer NOT NULL DEFAULT 0,
    "ConfirmationCode" character varying(20) NOT NULL,
    "CustomerNotes" character varying(1000) NULL,
    "InternalNotes" character varying(1000) NULL,
    "Price" decimal(18,2) NULL,
    "Currency" character varying(3) NULL,
    "IsPaid" boolean NOT NULL DEFAULT false,
    "PaymentTransactionId" character varying(100) NULL,
    "ConfirmedAt" timestamp with time zone NULL,
    "ConfirmedByProfileId" uuid NULL,
    "CancelledAt" timestamp with time zone NULL,
    "CancelledByProfileId" uuid NULL,
    "CancellationReason" character varying(500) NULL,
    "CompletedAt" timestamp with time zone NULL,
    "NoShowAt" timestamp with time zone NULL,
    "ReminderSent" boolean NOT NULL DEFAULT false,
    "ReminderSentAt" timestamp with time zone NULL,
    "FollowUpSent" boolean NOT NULL DEFAULT false,
    "FollowUpSentAt" timestamp with time zone NULL,
    "Rating" integer NULL,
    "ReviewText" character varying(1000) NULL,
    "ReviewedAt" timestamp with time zone NULL,
    "MetadataJson" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedAt" timestamp with time zone NULL,
    
    CONSTRAINT "FK_Sivar_ResourceBookings_BookableResources_ResourceId" FOREIGN KEY ("ResourceId") 
        REFERENCES "Sivar_BookableResources"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Sivar_ResourceBookings_ResourceServices_ServiceId" FOREIGN KEY ("ServiceId") 
        REFERENCES "Sivar_ResourceServices"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Sivar_ResourceBookings_Profiles_CustomerProfileId" FOREIGN KEY ("CustomerProfileId") 
        REFERENCES "Sivar_Profiles"("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Sivar_ResourceBookings_ResourceId" ON "Sivar_ResourceBookings"("ResourceId");
CREATE INDEX IF NOT EXISTS "IX_Sivar_ResourceBookings_ServiceId" ON "Sivar_ResourceBookings"("ServiceId");
CREATE INDEX IF NOT EXISTS "IX_Sivar_ResourceBookings_CustomerProfileId" ON "Sivar_ResourceBookings"("CustomerProfileId");
CREATE INDEX IF NOT EXISTS "IX_Sivar_ResourceBookings_StartTime" ON "Sivar_ResourceBookings"("StartTime");
CREATE INDEX IF NOT EXISTS "IX_Sivar_ResourceBookings_Status" ON "Sivar_ResourceBookings"("Status");
CREATE INDEX IF NOT EXISTS "IX_Sivar_ResourceBookings_ConfirmationCode" ON "Sivar_ResourceBookings"("ConfirmationCode");

-- Create unique constraint on confirmation code
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Sivar_ResourceBookings_ConfirmationCode" ON "Sivar_ResourceBookings"("ConfirmationCode") WHERE "IsDeleted" = false;

-- Success message
SELECT 'Booking system tables created successfully!' AS result;
