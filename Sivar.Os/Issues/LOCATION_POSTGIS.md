# Location Services Implementation Plan
**Date:** October 31, 2025  
**Status:** Planning Phase  
**PostgreSQL Extensions:** PostGIS + Existing pgvector pattern

---

## Executive Summary

This plan outlines the implementation of comprehensive location services for Sivar.Os using PostgreSQL PostGIS extension. We'll follow the same pattern used for pgvector (`.Ignore()` + raw SQL) to handle PostGIS types that are incompatible with EF Core 9.0.

---

## Table of Contents

1. [Current State Analysis](#current-state-analysis)
2. [Entities Requiring Location Data](#entities-requiring-location-data)
3. [PostGIS Extension Strategy](#postgis-extension-strategy)
4. [Database Schema Design](#database-schema-design)
5. [Service Layer Architecture](#service-layer-architecture)
6. [Implementation Phases](#implementation-phases)
7. [Configuration Changes](#configuration-changes)
8. [Database Scripts](#database-scripts)
9. [Testing Strategy](#testing-strategy)
10. [Performance Considerations](#performance-considerations)

---

## 1. Current State Analysis

### ✅ What We Already Have

**Location Value Object (DTOs/ValueObjects/Location.cs):**
```csharp
public class Location
{
    public string City { get; set; }          // ✅ Already exists
    public string State { get; set; }         // ✅ Already exists
    public string Country { get; set; }       // ✅ Already exists
    public double? Latitude { get; set; }     // ✅ Already exists
    public double? Longitude { get; set; }    // ✅ Already exists
}
```

**Entities Currently Using Location:**
- ✅ **Profile** - Has `Location?` as owned entity (configured in SivarDbContext)
- ✅ **Post** - Has `Location?` property (configured in PostConfiguration)

**Current Distance Calculation:**
- ⚠️ Using Haversine formula approximation in C# (PostRepository.cs line 87)
- ⚠️ Not using database spatial indexes
- ⚠️ Performance issues with large datasets

### ❌ What's Missing

1. **PostGIS Extension** - Not installed/configured
2. **Spatial Indexes** - No GIST indexes for fast proximity queries
3. **Geocoding Services** - No address → lat/long conversion
4. **Reverse Geocoding** - No lat/long → address conversion
5. **Proper Distance Queries** - Using approximation instead of accurate calculations
6. **Location-Based Search** - Limited to basic radius filtering

---

## 2. Entities Requiring Location Data

### Priority 1: Already Have Location Fields

| Entity | Current State | Location Type | Use Cases |
|--------|--------------|---------------|-----------|
| **Profile** | ✅ Has `Location?` owned entity | Address + Coordinates | User's home/business location |
| **Post** | ✅ Has `Location?` property | Address + Coordinates | Where post was created (check-ins, business locations) |

### Priority 2: Should Have Location Fields

| Entity | Recommendation | Location Type | Use Cases |
|--------|---------------|---------------|-----------|
| **User** | ⚠️ Consider adding | Address only | User's primary location (different from profile) |
| **Activity** | ⚠️ Consider adding | Coordinates only | Activity stream location tracking |
| **Comment** | ❌ Not needed | N/A | Comments inherit post location |
| **Reaction** | ❌ Not needed | N/A | Not relevant |

### Recommendation: Start with Profile and Post

Focus on enhancing existing location functionality before adding to new entities.

---

## 3. PostGIS Extension Strategy

### Why PostGIS?

| Feature | Current (Haversine) | With PostGIS |
|---------|-------------------|--------------|
| **Accuracy** | ±0.5% error | Sub-meter accuracy |
| **Performance** | O(n) scan | O(log n) with GIST index |
| **Query Type** | Box approximation | True distance/radius |
| **Spatial Queries** | Limited | Full spatial operations |
| **Database Work** | All in C# | All in PostgreSQL |

### EF Core 9.0 Compatibility Issue (Same as pgvector)

**Problem:** NetTopologySuite (PostGIS provider for EF Core) has similar issues with EF Core 9.0 as Pgvector.EntityFrameworkCore.

**Solution:** Use the same `.Ignore()` pattern we use for pgvector:

```csharp
// ❌ DON'T: Use NetTopologySuite types directly
using NetTopologySuite.Geometries;
public Point? GeoLocation { get; set; }  // ❌ May fail with EF Core 9.0

// ✅ DO: Store as string, use raw SQL
public string? GeoLocation { get; set; }  // ✅ Format: "POINT(lng lat)"
```

### Pattern: Follow Phase 3 pgvector Approach

1. **Entity:** Use `string?` for geometry column
2. **Configuration:** Use `.Ignore()` to bypass EF Core
3. **Database:** Create column manually with raw SQL
4. **Queries:** Use raw SQL with PostGIS functions
5. **Service:** Convert between string and typed objects

---

## 4. Database Schema Design

### 4.1 Profile Location Enhancement

**Current:**
```sql
-- Owned entity columns in Sivar_Profiles table
"LocationCity" VARCHAR(100)
"LocationState" VARCHAR(100)
"LocationCountry" VARCHAR(100)
"LocationLatitude" DOUBLE PRECISION
"LocationLongitude" DOUBLE PRECISION
```

**Add PostGIS Column:**
```sql
-- New column for spatial queries (IGNORED by EF Core)
ALTER TABLE "Sivar_Profiles"
ADD COLUMN "GeoLocation" GEOGRAPHY(POINT, 4326);

-- Update existing data
UPDATE "Sivar_Profiles"
SET "GeoLocation" = ST_SetSRID(
    ST_MakePoint("LocationLongitude", "LocationLatitude"), 
    4326
)
WHERE "LocationLatitude" IS NOT NULL 
  AND "LocationLongitude" IS NOT NULL;

-- Create spatial index
CREATE INDEX idx_profiles_geolocation 
ON "Sivar_Profiles" USING GIST("GeoLocation");
```

### 4.2 Post Location Enhancement

**Current:**
```sql
-- Owned entity columns in Sivar_Posts table
"LocationCity" VARCHAR(100)
"LocationState" VARCHAR(100)
"LocationCountry" VARCHAR(100)
"LocationLatitude" DOUBLE PRECISION
"LocationLongitude" DOUBLE PRECISION
```

**Add PostGIS Column:**
```sql
-- New column for spatial queries (IGNORED by EF Core)
ALTER TABLE "Sivar_Posts"
ADD COLUMN "GeoLocation" GEOGRAPHY(POINT, 4326);

-- Update existing data
UPDATE "Sivar_Posts"
SET "GeoLocation" = ST_SetSRID(
    ST_MakePoint("LocationLongitude", "LocationLatitude"), 
    4326
)
WHERE "LocationLatitude" IS NOT NULL 
  AND "LocationLongitude" IS NOT NULL;

-- Create spatial index
CREATE INDEX idx_posts_geolocation 
ON "Sivar_Posts" USING GIST("GeoLocation");
```

### 4.3 Why GEOGRAPHY vs GEOMETRY?

| Type | Use Case | Distance Unit | Accuracy |
|------|----------|---------------|----------|
| **GEOGRAPHY** | ✅ Earth surface (our case) | Meters | True distances |
| **GEOMETRY** | Flat plane (maps) | Same as coords | Fast, less accurate |

**Decision:** Use `GEOGRAPHY(POINT, 4326)` for accurate earth-surface distances.

- **POINT:** We only store single points, not polygons
- **4326:** WGS 84 (GPS standard, lat/long)

---

## 5. Service Layer Architecture

### 5.1 New Location Service

Create `ILocationService` and `LocationService`:

```csharp
// Sivar.Os.Shared/Services/ILocationService.cs
public interface ILocationService
{
    // Geocoding (Address → Coordinates)
    Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string city, string? state = null, string? country = null);
    
    // Reverse Geocoding (Coordinates → Address)
    Task<Location?> ReverseGeocodeAsync(double latitude, double longitude);
    
    // Distance Calculation
    Task<double> CalculateDistanceAsync(
        double lat1, double lng1, double lat2, double lng2);
    
    // Nearby Profiles
    Task<List<ProfileDto>> FindNearbyProfilesAsync(
        double latitude, double longitude, double radiusKm, int limit = 50);
    
    // Nearby Posts
    Task<List<PostDto>> FindNearbyPostsAsync(
        double latitude, double longitude, double radiusKm, int page = 1, int pageSize = 20);
    
    // Helper: Convert Location to PostGIS format
    string ToPostGISPoint(double latitude, double longitude);
    
    // Helper: Parse PostGIS point to coordinates
    (double Latitude, double Longitude)? ParsePostGISPoint(string? geoLocation);
}
```

### 5.2 Geocoding Provider Options

#### Option 1: Nominatim (OpenStreetMap) - FREE

**Pros:**
- ✅ Free, no API key required
- ✅ Open source
- ✅ Good coverage worldwide

**Cons:**
- ⚠️ Rate limited (1 request/second)
- ⚠️ Requires User-Agent header
- ⚠️ Less accurate than commercial services

**appsettings.json:**
```json
"LocationServices": {
  "Provider": "Nominatim",
  "Nominatim": {
    "BaseUrl": "https://nominatim.openstreetmap.org",
    "UserAgent": "Sivar.Os/1.0 (contact@sivar.os)",
    "RateLimitPerSecond": 1
  }
}
```

#### Option 2: Azure Maps - PAID (Recommended for Production)

**Pros:**
- ✅ High accuracy
- ✅ Enterprise SLA
- ✅ Generous free tier (1,000 requests/month)
- ✅ Integrates with Azure ecosystem

**Cons:**
- ⚠️ Requires Azure subscription
- ⚠️ Costs money after free tier

**appsettings.json:**
```json
"LocationServices": {
  "Provider": "AzureMaps",
  "AzureMaps": {
    "SubscriptionKey": "YOUR_AZURE_MAPS_KEY",
    "RateLimitPerSecond": 50
  }
}
```

#### Option 3: Google Maps Geocoding API - PAID

**Pros:**
- ✅ Highest accuracy
- ✅ Best coverage

**Cons:**
- ⚠️ Expensive ($5 per 1,000 requests)
- ⚠️ Requires credit card

**Recommendation:** Start with Nominatim, upgrade to Azure Maps for production.

---

## 6. Implementation Phases

### Phase 1: PostGIS Setup & Database Schema ⭐ START HERE

**Goal:** Install PostGIS and add GeoLocation columns following pgvector pattern.

**Tasks:**
1. ✅ Create database migration script: `003_AddPostGISLocationSupport.sql`
2. ✅ Enable PostGIS extension
3. ✅ Add `GeoLocation` columns to `Sivar_Profiles` and `Sivar_Posts`
4. ✅ Create spatial indexes
5. ✅ Migrate existing lat/long data to GeoLocation
6. ✅ Update entity configurations to `.Ignore()` GeoLocation columns

**Deliverables:**
- `Database/Scripts/003_AddPostGISLocationSupport.sql`
- Updated `ProfileConfiguration.cs` and `PostConfiguration.cs`
- Updated `Profile.cs` and `Post.cs` entities

**Estimated Time:** 2-3 hours

---

### Phase 2: Location Service Implementation

**Goal:** Create LocationService with PostGIS queries and geocoding.

**Tasks:**
1. ✅ Create `ILocationService` interface
2. ✅ Implement `LocationService` with Nominatim provider
3. ✅ Add PostGIS helper methods (ToPostGISPoint, ParsePostGISPoint)
4. ✅ Update `PostRepository` to use PostGIS instead of Haversine
5. ✅ Add `ProfileRepository` location queries
6. ✅ Add configuration in appsettings.json
7. ✅ Register service in Program.cs

**Deliverables:**
- `Sivar.Os.Shared/Services/ILocationService.cs`
- `Sivar.Os/Services/LocationService.cs`
- Updated `PostRepository.cs`
- Updated `ProfileRepository.cs`
- Updated `appsettings.json`

**Estimated Time:** 4-6 hours

---

### Phase 3: Profile Service Integration

**Goal:** Add location features to ProfileService.

**Tasks:**
1. ✅ Update `ProfileService.CreateProfileAsync()` to geocode addresses
2. ✅ Add `UpdateProfileLocationAsync()` method
3. ✅ Add `FindNearbyProfilesAsync()` method
4. ✅ Update PostGIS GeoLocation when lat/long changes

**Deliverables:**
- Updated `ProfileService.cs`
- Updated `IProfileService.cs`

**Estimated Time:** 2-3 hours

---

### Phase 4: Post Service Integration

**Goal:** Add location features to PostService.

**Tasks:**
1. ✅ Update `PostService.CreatePostAsync()` to set GeoLocation
2. ✅ Add location-based filtering to `GetPostsAsync()`
3. ✅ Update PostGIS GeoLocation when location changes
4. ✅ Add nearby posts endpoint

**Deliverables:**
- Updated `PostService.cs`
- Updated `IPostService.cs`

**Estimated Time:** 2-3 hours

---

### Phase 5: UI Components

**Goal:** Add location picker and map display components.

**Tasks:**
1. ✅ Create `LocationPicker.razor` component (MudBlazor + Leaflet.js)
2. ✅ Create `LocationMap.razor` component (display posts on map)
3. ✅ Add location autocomplete in CreateProfile/CreatePost
4. ✅ Add "Nearby" filter in feed

**Deliverables:**
- `Sivar.Os.Client/Components/LocationPicker.razor`
- `Sivar.Os.Client/Components/LocationMap.razor`
- Updated `CreateProfile.razor`
- Updated `CreatePost.razor`

**Estimated Time:** 6-8 hours

---

### Phase 6: Testing & Optimization

**Goal:** Test location features and optimize queries.

**Tasks:**
1. ✅ Write unit tests for LocationService
2. ✅ Write integration tests for location queries
3. ✅ Test geocoding with various addresses
4. ✅ Benchmark PostGIS vs Haversine performance
5. ✅ Optimize spatial indexes

**Deliverables:**
- `Sivar.Os.Tests/Services/LocationServiceTests.cs`
- Performance benchmark report

**Estimated Time:** 4-6 hours

---

## 7. Configuration Changes

### 7.1 appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=XafSivarOs;Username=postgres;Password=1234567890"
  },
  
  "LocationServices": {
    "Provider": "Nominatim",
    "EnableGeocoding": true,
    "EnableReverseGeocoding": true,
    "CacheDurationMinutes": 1440,
    
    "Nominatim": {
      "BaseUrl": "https://nominatim.openstreetmap.org",
      "UserAgent": "Sivar.Os/1.0 (your-email@example.com)",
      "RateLimitPerSecond": 1,
      "TimeoutSeconds": 10
    },
    
    "AzureMaps": {
      "SubscriptionKey": "",
      "RateLimitPerSecond": 50,
      "TimeoutSeconds": 10
    },
    
    "PostGIS": {
      "DefaultSRID": 4326,
      "UseGeography": true,
      "MaxDistanceKm": 100,
      "DefaultRadiusKm": 10
    }
  }
}
```

### 7.2 Program.cs Registration

```csharp
// Register LocationService
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddHttpClient<LocationService>();

// Configure PostGIS for Npgsql (following pgvector pattern)
builder.Services.AddDbContext<SivarDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.UseVector();  // Existing pgvector
            npgsqlOptions.UseNetTopologySuite();  // NEW: PostGIS support
        });
});
```

---

## 8. Database Scripts

### 8.1 Script Structure

Following the existing pattern in `Database/Scripts/`:

```
Database/
  Scripts/
    001_AddSentimentAnalysisFields.sql          (Existing)
    002_AddSentimentContinuousAggregates.sql    (Existing)
    003_AddPostGISLocationSupport.sql           (NEW)
    004_MigrateExistingLocationData.sql         (NEW)
```

### 8.2 003_AddPostGISLocationSupport.sql

```sql
-- =====================================================
-- PostGIS Location Support Migration
-- Date: October 31, 2025
-- Description: Adds PostGIS extension and GeoLocation columns
-- Pattern: Follows pgvector .Ignore() pattern for EF Core 9.0
-- =====================================================

-- ============ PART 1: Enable PostGIS Extension ============

CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS postgis_topology;

-- Verify installation
SELECT PostGIS_Version();

-- ============ PART 2: Add GeoLocation to Sivar_Profiles ============

-- Add GEOGRAPHY column (IGNORED by EF Core, managed via raw SQL)
ALTER TABLE "Sivar_Profiles"
ADD COLUMN IF NOT EXISTS "GeoLocation" GEOGRAPHY(POINT, 4326);

-- Add metadata columns
ALTER TABLE "Sivar_Profiles"
ADD COLUMN IF NOT EXISTS "GeoLocationUpdatedAt" TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS "GeoLocationSource" VARCHAR(20) DEFAULT 'Manual';

-- Create spatial index (GIST)
CREATE INDEX IF NOT EXISTS idx_profiles_geolocation 
ON "Sivar_Profiles" USING GIST("GeoLocation");

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

-- ============ PART 4: Create Helper Functions ============

-- Function to calculate distance between two points (meters)
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
$$ LANGUAGE plpgsql IMMUTABLE;

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
      AND ST_DWithin(
          "GeoLocation",
          ST_SetSRID(ST_MakePoint(center_lng, center_lat), 4326)::geography,
          radius_km * 1000
      )
    ORDER BY distance_km
    LIMIT max_results;
END;
$$ LANGUAGE plpgsql;

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
      AND ST_DWithin(
          "GeoLocation",
          ST_SetSRID(ST_MakePoint(center_lng, center_lat), 4326)::geography,
          radius_km * 1000
      )
    ORDER BY distance_km
    LIMIT max_results;
END;
$$ LANGUAGE plpgsql;

-- ============ PART 5: Add Comments ============

COMMENT ON COLUMN "Sivar_Profiles"."GeoLocation" 
IS 'PostGIS geography point (SRID 4326, WGS 84). IGNORED by EF Core, use raw SQL to update.';

COMMENT ON COLUMN "Sivar_Posts"."GeoLocation" 
IS 'PostGIS geography point (SRID 4326, WGS 84). IGNORED by EF Core, use raw SQL to update.';

COMMENT ON COLUMN "Sivar_Profiles"."GeoLocationSource" 
IS 'How location was obtained: Manual, Geocoded, GPS, IP';

COMMENT ON COLUMN "Sivar_Posts"."GeoLocationSource" 
IS 'How location was obtained: Manual, Geocoded, GPS, IP';

-- ============ Migration Complete ============
-- Verify with: SELECT * FROM find_nearby_profiles(40.7128, -74.0060, 50);
```

### 8.3 004_MigrateExistingLocationData.sql

```sql
-- =====================================================
-- Migrate Existing Location Data to PostGIS
-- Date: October 31, 2025
-- Description: Populates GeoLocation from existing Latitude/Longitude
-- =====================================================

-- ============ PART 1: Migrate Profile Locations ============

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

-- ============ PART 2: Migrate Post Locations ============

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

-- ============ PART 3: Verify Migration ============

-- Count migrated profiles
SELECT COUNT(*) AS migrated_profiles
FROM "Sivar_Profiles"
WHERE "GeoLocation" IS NOT NULL;

-- Count migrated posts
SELECT COUNT(*) AS migrated_posts
FROM "Sivar_Posts"
WHERE "GeoLocation" IS NOT NULL;

-- Sample query: Find posts near New York
SELECT 
    "Id",
    "Content",
    "LocationCity",
    ST_Distance(
        "GeoLocation",
        ST_SetSRID(ST_MakePoint(-74.0060, 40.7128), 4326)::geography
    ) / 1000.0 AS distance_km
FROM "Sivar_Posts"
WHERE "GeoLocation" IS NOT NULL
  AND ST_DWithin(
      "GeoLocation",
      ST_SetSRID(ST_MakePoint(-74.0060, 40.7128), 4326)::geography,
      50000  -- 50 km
  )
ORDER BY distance_km
LIMIT 10;

-- ============ Migration Complete ============
```

---

## 9. Testing Strategy

### 9.1 Unit Tests

```csharp
// Sivar.Os.Tests/Services/LocationServiceTests.cs
public class LocationServiceTests
{
    [Fact]
    public async Task GeocodeAsync_ValidAddress_ReturnsCoordinates()
    {
        // Arrange
        var service = CreateLocationService();
        
        // Act
        var result = await service.GeocodeAsync("New York", "NY", "USA");
        
        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.Value.Latitude, 40.0, 41.0);
        Assert.InRange(result.Value.Longitude, -75.0, -73.0);
    }
    
    [Fact]
    public async Task ReverseGeocodeAsync_ValidCoordinates_ReturnsAddress()
    {
        // Arrange
        var service = CreateLocationService();
        
        // Act
        var result = await service.ReverseGeocodeAsync(40.7128, -74.0060);
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains("New York", result.City);
    }
    
    [Fact]
    public void ToPostGISPoint_ValidCoordinates_ReturnsWKT()
    {
        // Arrange
        var service = CreateLocationService();
        
        // Act
        var result = service.ToPostGISPoint(40.7128, -74.0060);
        
        // Assert
        Assert.Equal("POINT(-74.0060 40.7128)", result);
    }
}
```

### 9.2 Integration Tests

```csharp
// Sivar.Os.Tests/Integration/LocationQueriesTests.cs
public class LocationQueriesTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task FindNearbyPosts_WithinRadius_ReturnsCorrectPosts()
    {
        // Arrange
        var context = CreateContext();
        await SeedPostsWithLocations(context);
        
        // Act
        var nearbyPosts = await context.Database
            .SqlQuery<PostLocationResult>($@"
                SELECT * FROM find_nearby_posts(40.7128, -74.0060, 10, 100)
            ")
            .ToListAsync();
        
        // Assert
        Assert.NotEmpty(nearbyPosts);
        Assert.All(nearbyPosts, p => Assert.True(p.DistanceKm <= 10));
    }
}
```

---

## 10. Performance Considerations

### 10.1 Index Strategy

| Table | Index Type | Column | Purpose |
|-------|-----------|--------|---------|
| Sivar_Profiles | GIST | GeoLocation | Fast proximity queries |
| Sivar_Posts | GIST | GeoLocation | Fast proximity queries |
| Sivar_Posts | BTREE | LocationCity | Text filtering |
| Sivar_Profiles | BTREE | LocationCountry | Text filtering |

### 10.2 Query Optimization

**✅ DO: Use ST_DWithin for proximity queries**
```sql
-- FAST: Uses spatial index
WHERE ST_DWithin(
    "GeoLocation",
    ST_SetSRID(ST_MakePoint(-74.0060, 40.7128), 4326)::geography,
    10000  -- 10 km in meters
)
```

**❌ DON'T: Use ST_Distance in WHERE clause**
```sql
-- SLOW: Doesn't use index efficiently
WHERE ST_Distance("GeoLocation", point) < 10000
```

### 10.3 Caching Strategy

```csharp
// Cache geocoding results to reduce API calls
public class LocationService : ILocationService
{
    private readonly IMemoryCache _cache;
    
    public async Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string city, string? state, string? country)
    {
        var cacheKey = $"geocode:{city}:{state}:{country}";
        
        if (_cache.TryGetValue(cacheKey, out (double, double)? cached))
            return cached;
        
        var result = await GeocodeFromProviderAsync(city, state, country);
        
        if (result.HasValue)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromDays(30));
        }
        
        return result;
    }
}
```

---

## 11. DEVELOPMENT_RULES.md Updates

Add new section after PostgreSQL pgvector section:

```markdown
## ⚠️ CRITICAL: PostgreSQL PostGIS & EF Core 9.0 Compatibility

### 🚨 Similar to pgvector - Use .Ignore() Pattern

**Problem:** NetTopologySuite's PostGIS types may be incompatible with EF Core 9.0.

### ✅ CORRECT Solution: Use String with .Ignore()

**Entity Configuration:**
```csharp
// Profile.cs / Post.cs
public string? GeoLocation { get; set; }  // ✅ Use string, not Point

// ProfileConfiguration.cs / PostConfiguration.cs
builder.Ignore(p => p.GeoLocation);  // ✅ Bypass EF Core
```

**Update via Raw SQL:**
```csharp
await _context.Database.ExecuteSqlRawAsync(
    @"UPDATE ""Sivar_Profiles"" 
      SET ""GeoLocation"" = ST_SetSRID(ST_MakePoint(@lng, @lat), 4326)::geography,
          ""GeoLocationUpdatedAt"" = NOW()
      WHERE ""Id"" = @id",
    new NpgsqlParameter("@lng", longitude),
    new NpgsqlParameter("@lat", latitude),
    new NpgsqlParameter("@id", profileId));
```

**Query via Raw SQL:**
```csharp
var nearby = await _context.Database
    .SqlQuery<ProfileLocationResult>($@"
        SELECT * FROM find_nearby_profiles({latitude}, {longitude}, {radiusKm})
    ")
    .ToListAsync();
```
```

---

## 12. Next Steps & Decision Points

### Immediate Actions Required

1. **Decision:** Choose geocoding provider (Nominatim vs Azure Maps)
   - **Recommendation:** Start with Nominatim for development
   
2. **Decision:** Which entities need location? (Just Profile + Post, or add User/Activity?)
   - **Recommendation:** Start with Profile + Post only

3. **Execute Phase 1:** Create and run database scripts
   - Create `003_AddPostGISLocationSupport.sql`
   - Run against database
   - Verify with test queries

### Questions to Resolve

1. **Do we need real-time location tracking** (like "currently at")?
   - If yes: Add to Activity entity
   - If no: Profile + Post sufficient

2. **Do we need location history** (track where user has been)?
   - If yes: Create LocationHistory table
   - If no: Current setup sufficient

3. **What's the primary use case?**
   - Find nearby businesses? → Focus on Post
   - Find nearby people? → Focus on Profile
   - Both? → Equal priority

---

## 13. Success Metrics

### Phase 1 Success Criteria
- ✅ PostGIS extension installed
- ✅ GeoLocation columns created
- ✅ Spatial indexes created
- ✅ Existing data migrated
- ✅ Test queries return results

### Phase 2 Success Criteria
- ✅ LocationService implemented
- ✅ Geocoding working
- ✅ Distance queries 10x faster than Haversine

### Final Success Criteria
- ✅ Users can search "posts near me"
- ✅ Profiles show distance to viewer
- ✅ Map displays posts/profiles
- ✅ Address autocomplete working
- ✅ < 100ms query time for nearby search

---

## 14. References

### Documentation
- [PostGIS Documentation](https://postgis.net/documentation/)
- [PostGIS ST_DWithin](https://postgis.net/docs/ST_DWithin.html)
- [Nominatim API](https://nominatim.org/release-docs/develop/api/Overview/)
- [Azure Maps Geocoding](https://docs.microsoft.com/en-us/azure/azure-maps/how-to-search-for-address)
- [Leaflet.js (Maps)](https://leafletjs.com/)

### Related Files
- `DEVELOPMENT_RULES.md` - pgvector pattern (lines 46-345)
- `Database/Scripts/001_AddSentimentAnalysisFields.sql` - Script pattern
- `Sivar.Os.Data/Repositories/PostRepository.cs` - Current Haversine implementation

---

**End of Plan**

**Next Action:** Review this plan with the team and get approval to proceed with Phase 1.
