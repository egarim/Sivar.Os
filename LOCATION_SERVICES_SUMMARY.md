# Location Services - Quick Summary

**Date:** October 31, 2025  
**Full Plan:** See `LOCATION_SERVICES_IMPLEMENTATION_PLAN.md`

---

## Current State ✅

**Already Have Location Support:**
- ✅ `Profile` entity has `Location` value object (City, State, Country, Lat/Long)
- ✅ `Post` entity has `Location` value object
- ✅ Basic radius search using Haversine formula (C# approximation)

**What's Missing:**
- ❌ PostGIS extension not installed
- ❌ No spatial indexes (queries are slow)
- ❌ No geocoding (address → coordinates)
- ❌ Using approximation instead of accurate distance

---

## Recommended Solution: PostGIS Extension

### Why PostGIS?

| Feature | Current (Haversine) | With PostGIS |
|---------|-------------------|--------------|
| **Accuracy** | ±0.5% error | Sub-meter |
| **Performance** | O(n) scan | O(log n) indexed |
| **Database** | C# calculation | PostgreSQL native |
| **Queries** | Box approximation | True distance |

### Same Pattern as pgvector

PostGIS will follow the **exact same `.Ignore()` pattern** we use for pgvector:

```csharp
// Entity: Use string, not geometry type
public string? GeoLocation { get; set; }  // Format: "POINT(lng lat)"

// Configuration: Ignore the column
builder.Ignore(p => p.GeoLocation);

// Update: Use raw SQL
await _context.Database.ExecuteSqlRawAsync(@"
    UPDATE ""Sivar_Profiles"" 
    SET ""GeoLocation"" = ST_SetSRID(ST_MakePoint(@lng, @lat), 4326)::geography
    WHERE ""Id"" = @id");

// Query: Use raw SQL
var nearby = await _context.Database.SqlQuery<Result>($@"
    SELECT * FROM find_nearby_profiles({lat}, {lng}, {radius})
").ToListAsync();
```

---

## Entities Needing Location

### ✅ Already Have (Enhance These)
- **Profile** - User/business location
- **Post** - Where post was created (check-ins)

### ⚠️ Consider Adding
- **User** - Home location (different from profile)
- **Activity** - Location tracking

**Recommendation:** Start with Profile + Post only.

---

## Implementation Phases

### Phase 1: PostGIS Setup (2-3 hours) ⭐ START HERE
1. Create `Database/Scripts/003_AddPostGISLocationSupport.sql`
2. Enable PostGIS extension
3. Add `GeoLocation` GEOGRAPHY columns
4. Create spatial indexes (GIST)
5. Migrate existing lat/long data
6. Update entity configurations to `.Ignore()`

### Phase 2: Location Service (4-6 hours)
1. Create `ILocationService` / `LocationService`
2. Implement Nominatim geocoding (FREE)
3. Add PostGIS helper methods
4. Update repositories to use PostGIS

### Phase 3: Profile Integration (2-3 hours)
1. Auto-geocode addresses
2. Add nearby profiles search

### Phase 4: Post Integration (2-3 hours)
1. Set GeoLocation on post creation
2. Add nearby posts filter

### Phase 5: UI Components (6-8 hours)
1. Location picker (Leaflet.js)
2. Map display
3. "Near me" filters

### Phase 6: Testing (4-6 hours)
1. Unit tests
2. Integration tests
3. Performance benchmarks

**Total Estimated Time:** 20-30 hours

---

## Configuration Needed

### appsettings.json

```json
"LocationServices": {
  "Provider": "Nominatim",
  "EnableGeocoding": true,
  "Nominatim": {
    "BaseUrl": "https://nominatim.openstreetmap.org",
    "UserAgent": "Sivar.Os/1.0 (your-email@example.com)",
    "RateLimitPerSecond": 1
  },
  "PostGIS": {
    "DefaultSRID": 4326,
    "UseGeography": true,
    "DefaultRadiusKm": 10
  }
}
```

### Program.cs

```csharp
// Register LocationService
builder.Services.AddScoped<ILocationService, LocationService>();

// Enable PostGIS in DbContext
options.UseNpgsql(connectionString, npgsqlOptions =>
{
    npgsqlOptions.UseVector();  // Existing
    npgsqlOptions.UseNetTopologySuite();  // NEW
});
```

---

## Database Scripts to Create

### 003_AddPostGISLocationSupport.sql
- Enable PostGIS extension
- Add `GeoLocation` GEOGRAPHY(POINT, 4326) columns
- Create GIST spatial indexes
- Create helper functions (find_nearby_profiles, find_nearby_posts)

### 004_MigrateExistingLocationData.sql
- Migrate existing Latitude/Longitude to GeoLocation
- Verify migration

---

## Key Design Decisions

### 1. Geography vs Geometry?
**Decision:** Use `GEOGRAPHY(POINT, 4326)`
- Accurate earth-surface distances
- WGS 84 (GPS standard)

### 2. Geocoding Provider?
**Decision:** Start with Nominatim (FREE), upgrade to Azure Maps later
- Nominatim: Free, 1 req/sec limit
- Azure Maps: 1,000 free/month, then paid

### 3. Which Entities Get Location?
**Decision:** Profile + Post only (for now)
- Can add User/Activity later if needed

### 4. EF Core Integration?
**Decision:** Use `.Ignore()` pattern (same as pgvector)
- Bypass EF Core's type handling
- Use raw SQL for all PostGIS operations

---

## Performance Improvements Expected

### Before (Haversine)
```csharp
// C# approximation, scans all rows
var nearby = posts
    .Where(p => Math.Abs(p.Lat - lat) < delta)
    .ToList();
```
**Performance:** O(n) - scans all posts

### After (PostGIS)
```sql
-- PostgreSQL spatial index
SELECT * FROM find_nearby_posts(40.7128, -74.0060, 10);
```
**Performance:** O(log n) - uses GIST index

**Expected Speedup:** 10-100x faster for large datasets

---

## Next Steps

1. **Review** `LOCATION_SERVICES_IMPLEMENTATION_PLAN.md` (full details)
2. **Decide:** Geocoding provider (Nominatim vs Azure Maps)
3. **Decide:** Which entities need location (Profile+Post only? Or add User/Activity?)
4. **Execute Phase 1:** Create and run database scripts
5. **Test:** Verify PostGIS queries work

---

## Questions to Answer

1. **Do we need real-time "currently at" tracking?** (Activity entity)
2. **Do we need location history?** (LocationHistory table)
3. **What's the priority use case?**
   - Find nearby businesses → Focus on Post
   - Find nearby people → Focus on Profile
   - Both → Equal priority

---

## Files to Review

1. `LOCATION_SERVICES_IMPLEMENTATION_PLAN.md` - Full detailed plan
2. `DEVELOPMENT_RULES.md` - Existing pgvector pattern (lines 46-345)
3. `Database/Scripts/001_AddSentimentAnalysisFields.sql` - Script pattern example
4. `Sivar.Os.Data/Repositories/PostRepository.cs` - Current Haversine code (line 87)

---

**Ready to proceed?** Start with Phase 1 database setup.
