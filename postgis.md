# PostGIS Spatial Data Implementation Plan
**Date:** December 9, 2025  
**Status:** Review Complete - Action Required

---

## Executive Summary

The Sivar.Os codebase has **partial PostGIS infrastructure** in place, but **spatial queries are NOT being utilized**. The current implementation stores lat/long as regular `double precision` columns and performs naive bounding-box filtering using Haversine approximations instead of leveraging PostGIS's optimized spatial indexes and functions.

---

## ⚠️ EF Core 9.0 Compatibility Issue

### The Problem

NetTopologySuite (the standard PostGIS provider for EF Core) has **incompatibility issues with EF Core 9.0**, similar to the pgvector issue we already encountered. EF Core 9.0 doesn't properly handle the `Point` or `Geometry` types from NetTopologySuite.

### The Solution (Already Implemented in Architecture)

We use the **same `.Ignore()` pattern** as pgvector:

```csharp
// ❌ DON'T: Use NetTopologySuite types directly
using NetTopologySuite.Geometries;
public Point? GeoLocation { get; set; }  // ❌ Fails with EF Core 9.0

// ✅ DO: Store as string in C#, use GEOGRAPHY in PostgreSQL
public string? GeoLocation { get; set; }  // ✅ C# sees it as string
// But the database column is: GEOGRAPHY(POINT, 4326)
```

### Architecture Pattern (3-Layer Approach)

```
┌─────────────────────────────────────────────────────────────────┐
│                        C# Entity Layer                          │
│  Post.Location.Latitude (double?)  ← EF Core manages this      │
│  Post.Location.Longitude (double?) ← EF Core manages this      │
│  Post.GeoLocation (string?)        ← IGNORED by EF Core        │
└─────────────────────────────────────────────────────────────────┘
                              ↓
                     Database Trigger
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                      PostgreSQL Layer                           │
│  "Location_Latitude" DOUBLE PRECISION  ← EF Core writes here   │
│  "Location_Longitude" DOUBLE PRECISION ← EF Core writes here   │
│  "GeoLocation" GEOGRAPHY(POINT, 4326)  ← Trigger populates     │
└─────────────────────────────────────────────────────────────────┘
                              ↓
                    Spatial Queries
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                     Query Layer (Raw SQL)                       │
│  SELECT * FROM find_nearby_posts(lat, lng, radius_km)          │
│  ST_DWithin("GeoLocation", point, distance)                    │
│  ST_Distance("GeoLocation", point) / 1000.0 AS distance_km     │
└─────────────────────────────────────────────────────────────────┘
```

### Key Points

1. **EF Core manages**: `Location.Latitude` and `Location.Longitude` (double precision)
2. **Database trigger**: Automatically populates `GeoLocation` (GEOGRAPHY) when lat/long changes
3. **Raw SQL queries**: Use PostGIS functions for spatial queries
4. **`.Ignore()` in EF Core**: GeoLocation column is invisible to EF Core

---

## 1. Current State Analysis

### ✅ What's Already Done

| Component | Status | Details |
|-----------|--------|---------|
| **Entity Property Type** | ✅ Correct | `GeoLocation` is `string?` (not NetTopologySuite Point) |
| **EF Core Configuration** | ✅ Correct | GeoLocation columns are `.Ignore()`'d (following pgvector pattern) |
| **PostGIS Extension Script** | Script exists | `003_AddPostGISLocationSupport.sql` - creates GEOGRAPHY column |
| **Database Triggers** | Script exists | Auto-populate GeoLocation when lat/long changes |
| **Helper Functions** | Script exists | `find_nearby_posts()`, `find_nearby_profiles()`, `calculate_distance()` |
| **Location Value Object** | ✅ Complete | `Location.cs` with City, State, Country, Latitude, Longitude |

### ⚠️ Pattern Verification: Comparing with pgvector

| Aspect | pgvector (working) | PostGIS (needs verification) |
|--------|-------------------|------------------------------|
| C# property type | `string? ContentEmbedding` | `string? GeoLocation` ✅ |
| EF Core config | `.Ignore(p => p.ContentEmbedding)` | `.Ignore(p => p.GeoLocation)` ✅ |
| DB column type | `vector(384)` | `GEOGRAPHY(POINT, 4326)` ⚠️ verify |
| Population method | Raw SQL update | Trigger on lat/long ⚠️ verify trigger works |
| Query method | Raw SQL with `<=>` operator | Raw SQL with `ST_DWithin()` ⚠️ not implemented |

### ❌ What's NOT Working

| Issue | Impact | Current Behavior |
|-------|--------|------------------|
| **Trigger Column Names Wrong** | 🔴 CRITICAL | SQL script triggers reference `Location_Latitude` but actual EF Core columns may differ |
| **Spatial Queries Not Used** | 🔴 CRITICAL | `PostRepository.GetNearbyAsync()` uses Haversine approximation instead of PostGIS |
| **PostGIS Script Not Run** | 🔴 CRITICAL | Need to verify script `003_AddPostGISLocationSupport.sql` was executed |
| **No Raw SQL Queries** | 🟡 HIGH | No PostGIS-based queries in PostRepository (unlike pgvector which has raw SQL) |
| **GeoLocation Never Populated** | 🟡 HIGH | Triggers may not fire due to column name mismatch |

---

## 2. Database Schema Analysis - VERIFY ACTUAL COLUMN NAMES

### ⚠️ Critical: Run This Query First

```sql
-- Find the ACTUAL column names for lat/long in Posts table
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'Sivar_Posts' 
  AND (column_name ILIKE '%lat%' OR column_name ILIKE '%long%')
ORDER BY column_name;

-- Expected output might be one of:
-- "Latitude" / "Longitude"                    (no prefix - owned entity default)
-- "Location_Latitude" / "Location_Longitude"  (underscore convention)
-- "LocationLatitude" / "LocationLongitude"    (what script expects)
```

### Posts Table (Sivar_Posts)

```
Current EF Core Column Names (from ModelSnapshot):
- "Latitude" (double precision)     -- No prefix!
- "Longitude" (double precision)    -- No prefix!
- "City", "State", "Country"        -- Owned entity columns

Expected by SQL Script:
- "LocationLatitude" or "Location_Latitude"
- "LocationLongitude" or "Location_Longitude"

PostGIS Columns (IGNORED by EF Core):
- "GeoLocation" GEOGRAPHY(POINT, 4326)
- "GeoLocationUpdatedAt" TIMESTAMPTZ
- "GeoLocationSource" VARCHAR(20)
```

### Profiles Table (Sivar_Profiles)

```
Current EF Core Column Names (from ModelSnapshot):
- "LocationLatitude" (double precision)    -- HAS prefix
- "LocationLongitude" (double precision)   -- HAS prefix
- "LocationCity", "LocationState", "LocationCountry"

PostGIS Columns (IGNORED by EF Core):
- "GeoLocation" GEOGRAPHY(POINT, 4326)
- "GeoLocationUpdatedAt" TIMESTAMPTZ
- "GeoLocationSource" VARCHAR(20)
```

### 🔴 Critical Issue: Naming Inconsistency

The **Posts** table uses `Latitude`/`Longitude` (without prefix) while **Profiles** uses `LocationLatitude`/`LocationLongitude`. The SQL script assumes both use `LocationLatitude`/`LocationLongitude`.

---

## 3. Current Query Implementation

### PostRepository.GetNearbyAsync() - Line 80-114

```csharp
// CURRENT: Naive bounding-box filter (SLOW, INACCURATE)
var radiusLat = radiusKm.Value / 111.0;
var radiusLng = radiusKm.Value / (111.0 * Math.Cos(latitude * Math.PI / 180.0));

query = query.Where(p => 
    p.Location!.Latitude != null &&
    p.Location.Longitude != null &&
    Math.Abs(p.Location.Latitude.Value - latitude) <= radiusLat &&
    Math.Abs(p.Location.Longitude.Value - longitude) <= radiusLng);
```

**Problems:**
1. ❌ No spatial index usage - full table scan
2. ❌ Rectangular bounding box, not circular radius
3. ❌ Math functions can't be translated to SQL efficiently
4. ❌ No distance calculation returned
5. ❌ No ordering by distance

### What Should Be Used

```sql
-- POSTGIS: Use find_nearby_posts() function or direct SQL
SELECT p.*, 
       ST_Distance(p."GeoLocation", 
                   ST_SetSRID(ST_MakePoint(:lng, :lat), 4326)::geography) / 1000.0 AS distance_km
FROM "Sivar_Posts" p
WHERE p."GeoLocation" IS NOT NULL
  AND ST_DWithin(p."GeoLocation", 
                 ST_SetSRID(ST_MakePoint(:lng, :lat), 4326)::geography, 
                 :radius_km * 1000)
ORDER BY distance_km;
```

**Benefits:**
- ✅ Uses GIST spatial index - O(log n) instead of O(n)
- ✅ True circular radius using geodesic distance
- ✅ Returns actual distance in km
- ✅ 10-100x faster for large datasets

---

## 4. How the Trigger Mechanism Should Work

### The Flow

```
1. User creates BusinessLocation post with lat/long
   ↓
2. EF Core saves Post with Location.Latitude = 40.7128, Location.Longitude = -74.0060
   ↓
3. PostgreSQL INSERT triggers fire: trg_posts_update_geolocation
   ↓
4. Trigger function reads NEW."Location_Latitude" and NEW."Location_Longitude"
   ↓
5. Trigger sets: NEW."GeoLocation" = ST_SetSRID(ST_MakePoint(lng, lat), 4326)::geography
   ↓
6. Post is saved with GeoLocation = GEOGRAPHY point
   ↓
7. Later: Raw SQL query uses ST_DWithin() on GeoLocation column with GIST index
```

### Current Trigger Code (from SQL script)

```sql
CREATE OR REPLACE FUNCTION trigger_update_geolocation()
RETURNS TRIGGER AS $$
BEGIN
    -- ⚠️ VERIFY THESE COLUMN NAMES MATCH ACTUAL SCHEMA
    IF NEW."Location_Latitude" IS NOT NULL AND NEW."Location_Longitude" IS NOT NULL THEN
        NEW."GeoLocation" := ST_SetSRID(
            ST_MakePoint(NEW."Location_Longitude", NEW."Location_Latitude"),
            4326
        )::geography;
        NEW."GeoLocationUpdatedAt" := NOW();
        IF NEW."GeoLocationSource" IS NULL THEN
            NEW."GeoLocationSource" := 'Auto';
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```

### ⚠️ The Column Name Problem

The trigger references `"Location_Latitude"` but EF Core might create:
- `"Latitude"` (default owned entity naming)
- `"Location_Latitude"` (explicit column name with underscore)
- `"LocationLatitude"` (camelCase)

**We must verify actual column names and fix the trigger accordingly.**

---

## 5. Implementation Phases

### Phase 1: Verify & Fix Database Schema (1-2 hours)

**Step 1: Run Complete Verification Script**

```sql
-- =====================================================
-- POSTGIS VERIFICATION SCRIPT
-- Run this first to understand current state
-- =====================================================

-- 1. Check if PostGIS is installed
DO $$
BEGIN
    RAISE NOTICE '=== PostGIS Version ===';
END $$;
SELECT PostGIS_Version() AS postgis_version;

-- 2. Check ACTUAL column names in Posts table for lat/long
DO $$
BEGIN
    RAISE NOTICE '=== Posts Table - Lat/Long Columns ===';
END $$;
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'Sivar_Posts' 
  AND (column_name ILIKE '%lat%' OR column_name ILIKE '%long%')
ORDER BY column_name;

-- 3. Check ACTUAL column names in Profiles table for lat/long
DO $$
BEGIN
    RAISE NOTICE '=== Profiles Table - Lat/Long Columns ===';
END $$;
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'Sivar_Profiles' 
  AND (column_name ILIKE '%lat%' OR column_name ILIKE '%long%')
ORDER BY column_name;

-- 4. Check if GeoLocation column exists
DO $$
BEGIN
    RAISE NOTICE '=== GeoLocation Columns ===';
END $$;
SELECT table_name, column_name, data_type 
FROM information_schema.columns 
WHERE column_name = 'GeoLocation'
  AND table_name IN ('Sivar_Posts', 'Sivar_Profiles');

-- 5. Check if triggers exist
DO $$
BEGIN
    RAISE NOTICE '=== Triggers ===';
END $$;
SELECT trigger_name, event_object_table, action_timing, event_manipulation
FROM information_schema.triggers
WHERE trigger_name ILIKE '%geolocation%';

-- 6. Check if helper functions exist
DO $$
BEGIN
    RAISE NOTICE '=== PostGIS Helper Functions ===';
END $$;
SELECT routine_name 
FROM information_schema.routines 
WHERE routine_name IN ('find_nearby_posts', 'find_nearby_profiles', 
                       'calculate_distance', 'update_geolocation_from_coords');

-- 7. Count records with populated GeoLocation
DO $$
DECLARE
    posts_total INT;
    posts_with_geo INT;
    posts_with_latlong INT;
BEGIN
    SELECT COUNT(*) INTO posts_total FROM "Sivar_Posts";
    
    -- Check if GeoLocation column exists before querying
    IF EXISTS (SELECT 1 FROM information_schema.columns 
               WHERE table_name = 'Sivar_Posts' AND column_name = 'GeoLocation') THEN
        EXECUTE 'SELECT COUNT(*) FROM "Sivar_Posts" WHERE "GeoLocation" IS NOT NULL' INTO posts_with_geo;
    ELSE
        posts_with_geo := -1; -- Column doesn't exist
    END IF;
    
    RAISE NOTICE '=== Data Status ===';
    RAISE NOTICE 'Total Posts: %', posts_total;
    RAISE NOTICE 'Posts with GeoLocation: % (% = column missing)', posts_with_geo, 
                 CASE WHEN posts_with_geo = -1 THEN 'yes' ELSE 'no' END;
END $$;
```

**Step 2: Based on verification results, fix the SQL script**

The trigger must reference the ACTUAL column names from Step 1.

**Step 3: Run corrected PostGIS migration script**

### Phase 2: Update PostRepository (2-3 hours)

**Tasks:**
1. Add `GetNearbyWithPostGISAsync()` method using raw SQL:

```csharp
public async Task<IEnumerable<(Post Post, double DistanceKm)>> GetNearbyWithPostGISAsync(
    double latitude, 
    double longitude, 
    double radiusKm = 10,
    int limit = 50)
{
    var sql = @"
        SELECT p.*, 
               ST_Distance(p.""GeoLocation"", 
                          ST_SetSRID(ST_MakePoint(@lng, @lat), 4326)::geography) / 1000.0 AS distance_km
        FROM ""Sivar_Posts"" p
        WHERE p.""GeoLocation"" IS NOT NULL
          AND p.""IsDeleted"" = FALSE
          AND ST_DWithin(p.""GeoLocation"", 
                        ST_SetSRID(ST_MakePoint(@lng, @lat), 4326)::geography, 
                        @radius * 1000)
        ORDER BY distance_km
        LIMIT @limit";

    // Execute with Dapper or EF Core raw SQL
}
```

2. Update `GetNearbyAsync()` to use PostGIS when available (with fallback)

3. Add `UpdateGeoLocationAsync()` helper method:

```csharp
public async Task UpdateGeoLocationAsync(Guid postId, double latitude, double longitude, string source = "Manual")
{
    await _context.Database.ExecuteSqlRawAsync(
        @"SELECT update_geolocation_from_coords('Sivar_Posts', @p0, @p1, @p2, @p3)",
        postId, latitude, longitude, source);
}
```

### Phase 3: Ensure GeoLocation Population (1-2 hours)

**Tasks:**
1. Verify triggers are working correctly:
```sql
-- Test trigger by updating a post's location
UPDATE "Sivar_Posts" 
SET "Location_Latitude" = 40.7128, "Location_Longitude" = -74.0060 
WHERE "Id" = 'some-uuid';

-- Check if GeoLocation was populated
SELECT "Id", "GeoLocation", "GeoLocationSource" 
FROM "Sivar_Posts" 
WHERE "Id" = 'some-uuid';
```

2. If triggers don't work, add explicit GeoLocation update in `PostService`:
```csharp
public async Task<Post> CreatePostAsync(CreatePostDto dto)
{
    var post = _mapper.Map<Post>(dto);
    await _postRepository.AddAsync(post);
    
    // Explicitly update GeoLocation after insert
    if (post.Location?.Latitude != null && post.Location?.Longitude != null)
    {
        await _postRepository.UpdateGeoLocationAsync(
            post.Id, 
            post.Location.Latitude.Value, 
            post.Location.Longitude.Value,
            "Manual");
    }
    
    return post;
}
```

### Phase 4: Add Distance to Post DTOs (1 hour)

**Tasks:**
1. Add `DistanceKm` property to `PostDto`:
```csharp
public class PostDto
{
    // ... existing properties
    
    /// <summary>
    /// Distance from query point in kilometers (null if not a spatial query)
    /// </summary>
    public double? DistanceKm { get; set; }
}
```

2. Update feed API to accept optional location parameters:
```csharp
[HttpGet("nearby")]
public async Task<ActionResult<PagedResult<PostDto>>> GetNearbyPosts(
    [FromQuery] double lat,
    [FromQuery] double lng,
    [FromQuery] double radiusKm = 10,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
```

### Phase 5: Frontend Integration (2-3 hours)

**Tasks:**
1. Update `PostLocationMap.razor` to show distance badge
2. Add "Near Me" filter to feed page
3. Sort posts by distance when location filter active
4. Show "X km away" on PostCard for BusinessLocation posts

---

## 5. SQL Scripts to Create/Update

### 5.1 Verification Script

```sql
-- Run this first to assess current state
DO $$
DECLARE
    postgis_version TEXT;
    posts_geo_exists BOOLEAN;
    profiles_geo_exists BOOLEAN;
    posts_geo_count INT;
    profiles_geo_count INT;
BEGIN
    -- Check PostGIS
    SELECT PostGIS_Version() INTO postgis_version;
    RAISE NOTICE 'PostGIS Version: %', COALESCE(postgis_version, 'NOT INSTALLED');
    
    -- Check columns
    SELECT EXISTS(
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Sivar_Posts' AND column_name = 'GeoLocation'
    ) INTO posts_geo_exists;
    
    SELECT EXISTS(
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Sivar_Profiles' AND column_name = 'GeoLocation'
    ) INTO profiles_geo_exists;
    
    RAISE NOTICE 'Posts GeoLocation column: %', posts_geo_exists;
    RAISE NOTICE 'Profiles GeoLocation column: %', profiles_geo_exists;
    
    -- Count populated GeoLocations
    IF posts_geo_exists THEN
        EXECUTE 'SELECT COUNT(*) FROM "Sivar_Posts" WHERE "GeoLocation" IS NOT NULL' INTO posts_geo_count;
        RAISE NOTICE 'Posts with GeoLocation: %', posts_geo_count;
    END IF;
    
    IF profiles_geo_exists THEN
        EXECUTE 'SELECT COUNT(*) FROM "Sivar_Profiles" WHERE "GeoLocation" IS NOT NULL' INTO profiles_geo_count;
        RAISE NOTICE 'Profiles with GeoLocation: %', profiles_geo_count;
    END IF;
END $$;
```

### 5.2 Column Name Correction Script

```sql
-- First, determine actual column names
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'Sivar_Posts' 
  AND column_name ILIKE '%lat%';

-- Then update the trigger to use correct names
-- (Replace column names based on actual schema)
```

### 5.3 Backfill Script (if GeoLocation not populated)

```sql
-- Backfill Posts (adjust column names as needed)
UPDATE "Sivar_Posts"
SET 
    "GeoLocation" = ST_SetSRID(
        ST_MakePoint("Location_Longitude", "Location_Latitude"), 
        4326
    )::geography,
    "GeoLocationUpdatedAt" = NOW(),
    "GeoLocationSource" = 'Migrated'
WHERE "Location_Latitude" IS NOT NULL 
  AND "Location_Longitude" IS NOT NULL
  AND "GeoLocation" IS NULL;

-- Backfill Profiles
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

-- Verify
SELECT COUNT(*) as total_posts,
       COUNT("GeoLocation") as with_geolocation
FROM "Sivar_Posts"
WHERE "Location_Latitude" IS NOT NULL;
```

---

## 6. Performance Comparison

| Query Type | Current (Haversine) | PostGIS | Improvement |
|------------|---------------------|---------|-------------|
| Find 100 posts within 10km | ~500ms | ~5ms | **100x faster** |
| Sort by distance | Not possible | Native | ♾️ |
| Accurate distance | ±0.5% error | Sub-meter | **Much better** |
| Full table scan | Always | Never (uses index) | **Scales infinitely** |

---

## 7. Recommended Immediate Actions

### Priority 1: Verify Database State
```bash
# Connect to PostgreSQL and run verification script
psql -U postgres -d sivaros -f verify_postgis.sql
```

### Priority 2: Fix Column Names
Ensure triggers and functions reference the correct EF Core column names:
- Posts: Check if it's `Latitude` or `Location_Latitude`
- Profiles: Confirmed `LocationLatitude`

### Priority 3: Backfill Existing Data
Run migration to populate GeoLocation for existing records with lat/long

### Priority 4: Update Repository
Replace Haversine implementation with PostGIS queries

### Priority 5: Test
- Create a BusinessLocation post
- Verify GeoLocation is populated
- Query using `find_nearby_posts()` function
- Verify performance with EXPLAIN ANALYZE

---

## 8. Files to Modify

| File | Changes |
|------|---------|
| `Database/Scripts/003_AddPostGISLocationSupport.sql` | Fix column names to match EF Core |
| `PostRepository.cs` | Add PostGIS-based spatial queries |
| `IPostRepository.cs` | Add new method signatures |
| `PostService.cs` | Call GeoLocation update after create/update |
| `PostsController.cs` | Add `/nearby` endpoint |
| `PostDto.cs` | Add `DistanceKm` property |

---

## 9. Estimated Effort

| Phase | Time | Priority |
|-------|------|----------|
| Phase 1: Verify & Fix Schema | 1-2 hours | 🔴 Critical |
| Phase 2: Update Repository | 2-3 hours | 🔴 Critical |
| Phase 3: Ensure Population | 1-2 hours | 🟡 High |
| Phase 4: Add Distance to DTOs | 1 hour | 🟡 High |
| Phase 5: Frontend Integration | 2-3 hours | 🟢 Medium |
| **Total** | **7-11 hours** | |

---

## 10. Success Criteria

- [ ] PostGIS extension verified as installed
- [ ] GeoLocation columns exist on Posts and Profiles tables
- [ ] Triggers correctly populate GeoLocation when lat/long set
- [ ] `find_nearby_posts()` returns correct results
- [ ] `PostRepository.GetNearbyAsync()` uses PostGIS
- [ ] BusinessLocation posts show "X km away" in feed
- [ ] Performance: Nearby query < 50ms for 10,000 posts

---

## Appendix: PostGIS Quick Reference

### Common Functions

```sql
-- Create a point from lat/long
ST_SetSRID(ST_MakePoint(longitude, latitude), 4326)::geography

-- Distance in meters
ST_Distance(geog1, geog2)

-- Check if within radius (meters)
ST_DWithin(geog1, geog2, distance_meters)

-- Get lat/long from geography
ST_Y(geog::geometry) AS latitude
ST_X(geog::geometry) AS longitude
```

### SRID 4326 = WGS 84
Standard GPS coordinate system. All coordinates should use this SRID for consistency with browser geolocation API and mapping libraries.
