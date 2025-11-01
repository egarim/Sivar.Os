-- =====================================================
-- PostGIS Location Support Migration
-- Date: October 31, 2025
-- Description: Adds PostGIS extension and GeoLocation columns
-- Pattern: Follows pgvector .Ignore() pattern for EF Core 9.0
-- Issue: https://github.com/egarim/Sivar.Os/issues/7
-- =====================================================

-- ============ PART 1: Enable PostGIS Extension ============

CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS postgis_topology;

-- Verify installation
SELECT PostGIS_Version();

COMMENT ON EXTENSION postgis 
IS 'PostGIS geometry and geography spatial types and functions';

-- ============ PART 2: Add GeoLocation to Sivar_Profiles ============

-- Add GEOGRAPHY column (IGNORED by EF Core, managed via raw SQL)
-- Uses WGS 84 (SRID 4326) - standard GPS coordinate system
ALTER TABLE "Sivar_Profiles"
ADD COLUMN IF NOT EXISTS "GeoLocation" GEOGRAPHY(POINT, 4326);

-- Add metadata columns for tracking location updates
ALTER TABLE "Sivar_Profiles"
ADD COLUMN IF NOT EXISTS "GeoLocationUpdatedAt" TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS "GeoLocationSource" VARCHAR(20) DEFAULT 'Manual';

-- Create spatial index (GIST) for fast proximity queries
CREATE INDEX IF NOT EXISTS idx_profiles_geolocation 
ON "Sivar_Profiles" USING GIST("GeoLocation");

-- Create index on source for filtering
CREATE INDEX IF NOT EXISTS idx_profiles_geolocation_source
ON "Sivar_Profiles"("GeoLocationSource")
WHERE "GeoLocationSource" IS NOT NULL;

-- ============ PART 3: Add GeoLocation to Sivar_Posts ============

-- Add GEOGRAPHY column (IGNORED by EF Core, managed via raw SQL)
ALTER TABLE "Sivar_Posts"
ADD COLUMN IF NOT EXISTS "GeoLocation" GEOGRAPHY(POINT, 4326);

-- Add metadata columns
ALTER TABLE "Sivar_Posts"
ADD COLUMN IF NOT EXISTS "GeoLocationUpdatedAt" TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS "GeoLocationSource" VARCHAR(20) DEFAULT 'Manual';

-- Create spatial index (GIST)
CREATE INDEX IF NOT EXISTS idx_posts_geolocation 
ON "Sivar_Posts" USING GIST("GeoLocation");

-- Create index on source
CREATE INDEX IF NOT EXISTS idx_posts_geolocation_source
ON "Sivar_Posts"("GeoLocationSource")
WHERE "GeoLocationSource" IS NOT NULL;

-- Create composite index for common queries (location + created date)
CREATE INDEX IF NOT EXISTS idx_posts_geolocation_created
ON "Sivar_Posts" USING GIST("GeoLocation", "CreatedAt")
WHERE "GeoLocation" IS NOT NULL;

-- ============ PART 4: Create Helper Functions ============

-- Function to calculate distance between two points (returns meters)
CREATE OR REPLACE FUNCTION calculate_distance(
    lat1 DOUBLE PRECISION, 
    lng1 DOUBLE PRECISION,
    lat2 DOUBLE PRECISION, 
    lng2 DOUBLE PRECISION
) RETURNS DOUBLE PRECISION AS $$
BEGIN
    RETURN ST_Distance(
        ST_SetSRID(ST_MakePoint(lng1, lat1), 4326)::geography,
        ST_SetSRID(ST_MakePoint(lng2, lat2), 4326)::geography
    );
END;
$$ LANGUAGE plpgsql IMMUTABLE PARALLEL SAFE;

COMMENT ON FUNCTION calculate_distance(DOUBLE PRECISION, DOUBLE PRECISION, DOUBLE PRECISION, DOUBLE PRECISION)
IS 'Calculates the distance in meters between two geographic points using PostGIS';

-- Function to find nearby profiles
CREATE OR REPLACE FUNCTION find_nearby_profiles(
    center_lat DOUBLE PRECISION,
    center_lng DOUBLE PRECISION,
    radius_km DOUBLE PRECISION DEFAULT 10,
    max_results INT DEFAULT 50
)
RETURNS TABLE (
    profile_id UUID,
    distance_km DOUBLE PRECISION
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        "Id" AS profile_id,
        ST_Distance(
            "GeoLocation",
            ST_SetSRID(ST_MakePoint(center_lng, center_lat), 4326)::geography
        ) / 1000.0 AS distance_km
    FROM "Sivar_Profiles"
    WHERE "GeoLocation" IS NOT NULL
      AND "IsDeleted" = FALSE
      AND ST_DWithin(
          "GeoLocation",
          ST_SetSRID(ST_MakePoint(center_lng, center_lat), 4326)::geography,
          radius_km * 1000  -- Convert km to meters
      )
    ORDER BY distance_km
    LIMIT max_results;
END;
$$ LANGUAGE plpgsql STABLE PARALLEL SAFE;

COMMENT ON FUNCTION find_nearby_profiles(DOUBLE PRECISION, DOUBLE PRECISION, DOUBLE PRECISION, INT)
IS 'Finds profiles within a radius (km) of a center point, ordered by distance';

-- Function to find nearby posts
CREATE OR REPLACE FUNCTION find_nearby_posts(
    center_lat DOUBLE PRECISION,
    center_lng DOUBLE PRECISION,
    radius_km DOUBLE PRECISION DEFAULT 10,
    max_results INT DEFAULT 100
)
RETURNS TABLE (
    post_id UUID,
    distance_km DOUBLE PRECISION
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        "Id" AS post_id,
        ST_Distance(
            "GeoLocation",
            ST_SetSRID(ST_MakePoint(center_lng, center_lat), 4326)::geography
        ) / 1000.0 AS distance_km
    FROM "Sivar_Posts"
    WHERE "GeoLocation" IS NOT NULL
      AND "IsDeleted" = FALSE
      AND ST_DWithin(
          "GeoLocation",
          ST_SetSRID(ST_MakePoint(center_lng, center_lat), 4326)::geography,
          radius_km * 1000
      )
    ORDER BY distance_km, "CreatedAt" DESC
    LIMIT max_results;
END;
$$ LANGUAGE plpgsql STABLE PARALLEL SAFE;

COMMENT ON FUNCTION find_nearby_posts(DOUBLE PRECISION, DOUBLE PRECISION, DOUBLE PRECISION, INT)
IS 'Finds posts within a radius (km) of a center point, ordered by distance and recency';

-- Function to update GeoLocation from Latitude/Longitude
CREATE OR REPLACE FUNCTION update_geolocation_from_coords(
    table_name TEXT,
    record_id UUID,
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    source TEXT DEFAULT 'Manual'
) RETURNS VOID AS $$
DECLARE
    sql TEXT;
BEGIN
    -- Validate table name to prevent SQL injection
    IF table_name NOT IN ('Sivar_Profiles', 'Sivar_Posts') THEN
        RAISE EXCEPTION 'Invalid table name: %', table_name;
    END IF;
    
    -- Validate coordinates
    IF latitude < -90 OR latitude > 90 OR longitude < -180 OR longitude > 180 THEN
        RAISE EXCEPTION 'Invalid coordinates: lat=%, lng=%', latitude, longitude;
    END IF;
    
    sql := format(
        'UPDATE %I SET 
            "GeoLocation" = ST_SetSRID(ST_MakePoint($1, $2), 4326)::geography,
            "GeoLocationUpdatedAt" = NOW(),
            "GeoLocationSource" = $3
         WHERE "Id" = $4',
        table_name
    );
    
    EXECUTE sql USING longitude, latitude, source, record_id;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION update_geolocation_from_coords(TEXT, UUID, DOUBLE PRECISION, DOUBLE PRECISION, TEXT)
IS 'Updates GeoLocation column from latitude/longitude coordinates. Use for Posts or Profiles.';

-- Function to get coordinates from GeoLocation
CREATE OR REPLACE FUNCTION get_coordinates_from_geolocation(
    geolocation GEOGRAPHY
) RETURNS TABLE (
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        ST_Y(geolocation::geometry) AS latitude,
        ST_X(geolocation::geometry) AS longitude;
END;
$$ LANGUAGE plpgsql IMMUTABLE PARALLEL SAFE;

COMMENT ON FUNCTION get_coordinates_from_geolocation(GEOGRAPHY)
IS 'Extracts latitude and longitude from a PostGIS geography point';

-- ============ PART 5: Migrate Existing Location Data ============

-- Migrate Profile locations
UPDATE "Sivar_Profiles"
SET 
    "GeoLocation" = ST_SetSRID(
        ST_MakePoint("LocationLongitude", "LocationLatitude"), 
        4326
    )::geography,
    "GeoLocationUpdatedAt" = NOW(),
    "GeoLocationSource" = 'Migrated'
WHERE "LocationLatitude" IS NOT NULL 
  AND "LocationLongitude" IS NOT NULL
  AND "GeoLocation" IS NULL;

-- Migrate Post locations
UPDATE "Sivar_Posts"
SET 
    "GeoLocation" = ST_SetSRID(
        ST_MakePoint("LocationLongitude", "LocationLatitude"), 
        4326
    )::geography,
    "GeoLocationUpdatedAt" = NOW(),
    "GeoLocationSource" = 'Migrated'
WHERE "LocationLatitude" IS NOT NULL 
  AND "LocationLongitude" IS NOT NULL
  AND "GeoLocation" IS NULL;

-- ============ PART 6: Add Column Comments ============

COMMENT ON COLUMN "Sivar_Profiles"."GeoLocation" 
IS 'PostGIS geography point (SRID 4326, WGS 84). IGNORED by EF Core, use raw SQL or update_geolocation_from_coords() to update.';

COMMENT ON COLUMN "Sivar_Profiles"."GeoLocationUpdatedAt"
IS 'Timestamp when GeoLocation was last updated';

COMMENT ON COLUMN "Sivar_Profiles"."GeoLocationSource" 
IS 'How location was obtained: Manual, Geocoded, GPS, IP, Migrated';

COMMENT ON COLUMN "Sivar_Posts"."GeoLocation" 
IS 'PostGIS geography point (SRID 4326, WGS 84). IGNORED by EF Core, use raw SQL or update_geolocation_from_coords() to update.';

COMMENT ON COLUMN "Sivar_Posts"."GeoLocationUpdatedAt"
IS 'Timestamp when GeoLocation was last updated';

COMMENT ON COLUMN "Sivar_Posts"."GeoLocationSource" 
IS 'How location was obtained: Manual, Geocoded, GPS, IP, Migrated';

-- ============ PART 7: Create Trigger for Auto-Update (Optional) ============

-- Trigger function to auto-update GeoLocation when Latitude/Longitude changes
CREATE OR REPLACE FUNCTION trigger_update_geolocation()
RETURNS TRIGGER AS $$
BEGIN
    -- Only update if both latitude and longitude are present
    IF NEW."LocationLatitude" IS NOT NULL AND NEW."LocationLongitude" IS NOT NULL THEN
        NEW."GeoLocation" := ST_SetSRID(
            ST_MakePoint(NEW."LocationLongitude", NEW."LocationLatitude"),
            4326
        )::geography;
        NEW."GeoLocationUpdatedAt" := NOW();
        
        -- Set source to Auto if not explicitly set
        IF NEW."GeoLocationSource" IS NULL THEN
            NEW."GeoLocationSource" := 'Auto';
        END IF;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply trigger to Profiles
DROP TRIGGER IF EXISTS trg_profiles_update_geolocation ON "Sivar_Profiles";
CREATE TRIGGER trg_profiles_update_geolocation
    BEFORE INSERT OR UPDATE OF "LocationLatitude", "LocationLongitude"
    ON "Sivar_Profiles"
    FOR EACH ROW
    EXECUTE FUNCTION trigger_update_geolocation();

-- Apply trigger to Posts
DROP TRIGGER IF EXISTS trg_posts_update_geolocation ON "Sivar_Posts";
CREATE TRIGGER trg_posts_update_geolocation
    BEFORE INSERT OR UPDATE OF "LocationLatitude", "LocationLongitude"
    ON "Sivar_Posts"
    FOR EACH ROW
    EXECUTE FUNCTION trigger_update_geolocation();

COMMENT ON FUNCTION trigger_update_geolocation()
IS 'Automatically updates GeoLocation when LocationLatitude or LocationLongitude changes';

-- ============ PART 8: Verification Queries ============

-- Count migrated profiles
DO $$
DECLARE
    profile_count INT;
    post_count INT;
BEGIN
    SELECT COUNT(*) INTO profile_count FROM "Sivar_Profiles" WHERE "GeoLocation" IS NOT NULL;
    SELECT COUNT(*) INTO post_count FROM "Sivar_Posts" WHERE "GeoLocation" IS NOT NULL;
    
    RAISE NOTICE 'Migration complete:';
    RAISE NOTICE '  - Profiles with GeoLocation: %', profile_count;
    RAISE NOTICE '  - Posts with GeoLocation: %', post_count;
END $$;

-- Sample query: Find profiles near a point (example: New York)
-- SELECT * FROM find_nearby_profiles(40.7128, -74.0060, 50, 10);

-- Sample query: Find posts near a point
-- SELECT * FROM find_nearby_posts(40.7128, -74.0060, 25, 20);

-- Test distance calculation
-- SELECT calculate_distance(40.7128, -74.0060, 34.0522, -118.2437) / 1000.0 AS distance_km;
-- Expected: ~3935 km (NYC to LA)

-- ============ Migration Complete ============
-- PostGIS extension installed and configured
-- GeoLocation columns added to Profiles and Posts
-- Spatial indexes created for fast proximity queries
-- Helper functions created for distance calculations
-- Existing data migrated
-- Triggers set up for automatic updates
-- 
-- Next steps:
-- 1. Update Entity classes to add GeoLocation property (string?)
-- 2. Update Entity configurations to .Ignore() GeoLocation columns
-- 3. Implement ILocationService interface
-- 4. Test queries using find_nearby_profiles() and find_nearby_posts()
