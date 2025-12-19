-- Create the AiModelPricing table manually
-- Run this if EF migrations are not working

-- Drop table if exists (uncomment if needed)
-- DROP TABLE IF EXISTS "Sivar_AiModelPricings";

CREATE TABLE IF NOT EXISTS "Sivar_AiModelPricings" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "ModelId" character varying(100) NOT NULL,
    "DisplayName" character varying(200) NOT NULL,
    "Provider" character varying(100) NOT NULL,
    "ModelType" integer NOT NULL,
    "Tier" integer NOT NULL DEFAULT 0,
    "InputCostPer1M" numeric(18,6) NOT NULL,
    "OutputCostPer1M" numeric(18,6) NOT NULL,
    "BatchInputCostPer1M" numeric(18,6) NULL,
    "BatchOutputCostPer1M" numeric(18,6) NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "IsDefault" boolean NOT NULL DEFAULT false,
    "ContextWindowSize" integer NULL,
    "MaxOutputTokens" integer NULL,
    "EmbeddingDimensions" integer NULL,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "Notes" text NULL,
    "PricingUpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_Sivar_AiModelPricings" PRIMARY KEY ("Id")
);

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_AiModelPricing_ModelId" ON "Sivar_AiModelPricings" ("ModelId");
CREATE INDEX IF NOT EXISTS "IX_AiModelPricing_Provider" ON "Sivar_AiModelPricings" ("Provider");
CREATE INDEX IF NOT EXISTS "IX_AiModelPricing_ModelType" ON "Sivar_AiModelPricings" ("ModelType");
CREATE INDEX IF NOT EXISTS "IX_AiModelPricing_Tier" ON "Sivar_AiModelPricings" ("Tier");
CREATE INDEX IF NOT EXISTS "IX_AiModelPricing_ModelType_IsDefault" ON "Sivar_AiModelPricings" ("ModelType", "IsDefault");
CREATE INDEX IF NOT EXISTS "IX_AiModelPricing_IsActive" ON "Sivar_AiModelPricings" ("IsActive");

-- Verify table was created
SELECT 'Table created successfully' AS status;
SELECT COUNT(*) AS row_count FROM "Sivar_AiModelPricings";
