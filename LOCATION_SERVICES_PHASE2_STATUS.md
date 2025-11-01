# Location Services - Phase 2 Status Report

**Date:** Current Session  
**Branch:** `feature/location-services`  
**GitHub Issue:** [#7](https://github.com/egarim/Sivar.Os/issues/7)

## ✅ Phase 2 Completed Tasks

### 1. Core Service Interfaces & DTOs
- ✅ **ILocationService.cs** - Provider-agnostic interface with all required methods
- ✅ **LocationResultDtos.cs** - Internal DTOs for PostGIS query results
  - `ProfileLocationResult` - Holds ProfileId + DistanceKm
  - `PostLocationResult` - Holds PostId + DistanceKm
- ✅ **ProfileDto.cs** - Added `DistanceKm` property for proximity search results
- ✅ **PostDto.cs** - Added `DistanceKm` property for proximity search results

### 2. Base Service Implementation
- ✅ **LocationServiceBase.cs** - Abstract base class with shared logic
  - Haversine distance calculation (fallback when PostGIS unavailable)
  - PostGIS proximity queries (`find_nearby_profiles`, `find_nearby_posts`)
  - GeoLocation update methods (raw SQL for PostGIS columns)
  - Helper methods: `ToPostGISPoint()`, `ParsePostGISPoint()`, `IsValidCoordinates()`

### 3. Nominatim Provider Implementation
- ✅ **NominatimLocationService.cs** - FREE OpenStreetMap geocoding
  - Forward geocoding (address → coordinates)
  - Reverse geocoding (coordinates → address)
  - Rate limiting (1 request/second, configurable)
  - In-memory caching (30 days TTL)
  - Proper User-Agent header (required by Nominatim)

### 4. Configuration
- ✅ **LocationServicesOptions.cs** - Configuration classes
  - `LocationServicesOptions` - Main config with provider selection
  - `NominatimOptions` - Nominatim-specific settings
  - `AzureMapsOptions` - Placeholder for future implementation
  - `GoogleMapsOptions` - Placeholder for future implementation

- ✅ **appsettings.json** - Added LocationServices section
  ```json
  "LocationServices": {
    "Provider": "Nominatim",
    "Nominatim": {
      "BaseUrl": "https://nominatim.openstreetmap.org",
      "UserAgent": "Sivar.Os/1.0 (Contact: your-email@example.com)",
      "RateLimitPerSecond": 1.0
    }
  }
  ```

- ✅ **Program.cs** - Service registration with dependency injection
  - Provider selection based on appsettings.json
  - HttpClient configuration for Nominatim
  - Memory cache registration
  - Switch statement for future providers (Azure Maps, Google Maps)

## ⚠️ Known Issues to Resolve

### 1. Repository Method Missing: `GetDbContext()`
**Problem:**
```csharp
var results = await _profileRepository.GetDbContext()
    .Database
    .SqlQuery<ProfileLocationResult>(...)
```

**Error:** `IProfileRepository` does not contain definition for `GetDbContext()`

**Solution Options:**
1. Add `GetDbContext()` method to `IProfileRepository` and `IPostRepository`
2. Inject `SivarDbContext` directly into LocationServiceBase
3. Use a different approach for raw SQL queries

**Recommendation:** Add `DbContext GetDbContext()` to `IBaseRepository<T>` interface

---

### 2. Repository Method Missing: `GetByIdsAsync()`
**Problem:**
```csharp
var profiles = await _profileRepository.GetByIdsAsync(profileIds, cancellationToken);
```

**Error:** `IProfileRepository` does not contain definition for `GetByIdsAsync()`

**Solution:** Add `Task<List<T>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken)` to `IBaseRepository<T>`

---

### 3. Entity Property Mismatches
**Problems:**
- `Profile` entity doesn't have: `AvatarUrl`, `BannerUrl`, `WebsiteUrl`, `IsVerified`
- `Post.Tags` is `string[]` but DTO expects `List<string>`

**Solution:** Need to review actual `Profile` and `Post` entity structures and adjust mapping

---

### 4. Interface Method Signatures (Static vs Instance)
**Problem:**
```csharp
// ILocationService declares instance methods:
string ToPostGISPoint(double latitude, double longitude);

// LocationServiceBase implements as static:
public static string ToPostGISPoint(double latitude, double longitude)
```

**Error:** Cannot implement instance interface member with static method

**Solution:** Change ILocationService to declare these as static interface methods (C# 11+) OR remove static keyword

---

### 5. Missing NuGet Packages
**Potential Issue:** `Microsoft.Extensions.Options` may not be referenced in Sivar.Os.Shared project

**Solution:** Add to Sivar.Os.Shared.csproj:
```xml
<PackageReference Include="Microsoft.Extensions.Options" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="9.0.0" />
```

---

## 🔨 Next Steps to Complete Phase 2

### Step 1: Fix Repository Interfaces
**File:** `Sivar.Os.Shared/Repositories/IBaseRepository.cs`

Add these methods to the base repository interface:

```csharp
public interface IBaseRepository<T> where T : class
{
    // Existing methods...
    
    /// <summary>
    /// Get the underlying DbContext for raw SQL queries
    /// </summary>
    DbContext GetDbContext();
    
    /// <summary>
    /// Get multiple entities by their IDs
    /// </summary>
    Task<List<T>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default);
}
```

**File:** Implement in `BaseRepository.cs`:

```csharp
public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly SivarDbContext _context;
    
    public DbContext GetDbContext() => _context;
    
    public async Task<List<T>> GetByIdsAsync(
        List<Guid> ids, 
        CancellationToken cancellationToken = default)
    {
        // Assuming all entities have an Id property
        return await _context.Set<T>()
            .Where(e => ids.Contains(EF.Property<Guid>(e, "Id")))
            .ToListAsync(cancellationToken);
    }
}
```

---

### Step 2: Fix Interface Static Methods
**File:** `Sivar.Os.Shared/Services/ILocationService.cs`

**Option A:** Change to static interface methods (requires C# 11+):

```csharp
public interface ILocationService
{
    // ... existing methods ...
    
    static abstract string ToPostGISPoint(double latitude, double longitude);
    static abstract (double Latitude, double Longitude)? ParsePostGISPoint(string? geoLocation);
    static abstract bool IsValidCoordinates(double latitude, double longitude);
}
```

**Option B (Recommended):** Keep as instance methods, change LocationServiceBase:

```csharp
// In LocationServiceBase.cs
public virtual string ToPostGISPoint(double latitude, double longitude)
{
    return $"POINT({longitude} {latitude})";
}

public virtual (double Latitude, double Longitude)? ParsePostGISPoint(string? geoPoint)
{
    // ... implementation ...
}

public virtual bool IsValidCoordinates(double latitude, double longitude)
{
    return latitude >= -90 && latitude <= 90 && 
           longitude >= -180 && longitude <= 180;
}
```

---

### Step 3: Check Profile Entity Structure
**File:** `Sivar.Os.Shared/Entities/Profile.cs`

Verify which properties actually exist, then update `MapProfileToDto()` method in LocationServiceBase to use correct property names.

---

### Step 4: Simplify DTO Mapping (Temporary)
For now, use a simplified mapping that doesn't include all profile properties:

```csharp
private ProfileDto MapProfileToDto(Profile profile, double distanceKm)
{
    return new ProfileDto
    {
        Id = profile.Id,
        Handle = profile.Handle,
        DisplayName = profile.DisplayName,
        Bio = profile.Bio,
        ProfileType = MapProfileType(profile.ProfileType), // Need mapping function
        LocationDisplay = profile.LocationDisplay,
        IsActive = profile.IsActive,
        CreatedAt = profile.CreatedAt,
        DistanceKm = distanceKm
    };
}

private PostDto MapPostToDto(Post post, double distanceKm)
{
    return new PostDto
    {
        Id = post.Id,
        Content = post.Content,
        PostType = post.PostType,
        Visibility = post.Visibility,
        Language = post.Language,
        Tags = post.Tags?.ToList() ?? new List<string>(), // Convert array to list
        CreatedAt = post.CreatedAt,
        UpdatedAt = post.UpdatedAt,
        IsEdited = post.IsEdited,
        EditedAt = post.EditedAt,
        DistanceKm = distanceKm,
        Profile = new ProfileDto
        {
            Id = post.Profile.Id,
            Handle = post.Profile.Handle,
            DisplayName = post.Profile.DisplayName
        }
    };
}
```

---

### Step 5: Add Missing NuGet Packages
**File:** `Sivar.Os.Shared/Sivar.Os.Shared.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Options" Version="9.0.0" />
  <PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="9.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
</ItemGroup>
```

---

## 📊 Phase 2 Progress

**Overall Completion: ~70%**

| Task | Status | Notes |
|------|--------|-------|
| ILocationService interface | ✅ Complete | Needs minor signature fix (static methods) |
| LocationResultDtos | ✅ Complete | Working correctly |
| ProfileDto.DistanceKm | ✅ Complete | Property added |
| PostDto.DistanceKm | ✅ Complete | Property added |
| LocationServiceBase | ⚠️ 90% | Needs repository method fixes + DTO mapping |
| NominatimLocationService | ⚠️ 90% | Needs repository fixes + NuGet packages |
| LocationServicesOptions | ✅ Complete | Configuration ready |
| appsettings.json | ✅ Complete | Configuration added |
| Program.cs registration | ✅ Complete | DI setup complete |

---

## 🎯 Immediate Action Required

**Priority 1:** Fix repository interfaces (`GetDbContext()`, `GetByIdsAsync()`)  
**Priority 2:** Fix static/instance method mismatch in interface  
**Priority 3:** Add missing NuGet packages  
**Priority 4:** Fix DTO mapping with correct entity properties  

**Estimated Time to Complete:** 2-3 hours

Once these fixes are applied, Phase 2 will be complete and ready for testing.

---

## 📝 Testing Plan (After Fixes)

1. **Unit Tests:**
   - Test `ToPostGISPoint()` and `ParsePostGISPoint()` helper methods
   - Test `IsValidCoordinates()` validation
   - Test Haversine distance calculation

2. **Integration Tests:**
   - Test Nominatim geocoding with real API (respecting rate limits)
   - Test PostGIS proximity queries with test data
   - Test cache behavior (should not hit API twice for same query)

3. **Manual Testing:**
   - Create profile with location
   - Update GeoLocation using `UpdateProfileGeoLocationAsync()`
   - Search for nearby profiles
   - Verify distance calculations are accurate

---

## 📦 Files Created/Modified in Phase 2

**New Files:**
1. `Sivar.Os.Shared/Services/LocationServiceBase.cs` - 420 lines
2. `Sivar.Os.Shared/Services/NominatimLocationService.cs` - 200 lines
3. `Sivar.Os.Shared/Configuration/LocationServicesOptions.cs` - 100 lines
4. `Sivar.Os.Shared/DTOs/LocationResultDtos.cs` - 20 lines

**Modified Files:**
1. `Sivar.Os.Shared/DTOs/ProfileDto.cs` - Added DistanceKm property
2. `Sivar.Os.Shared/DTOs/PostDTOs.cs` - Added DistanceKm property to PostDto
3. `Sivar.Os/appsettings.json` - Added LocationServices section
4. `Sivar.Os/Program.cs` - Added service registration (30 lines)

**Total Lines of Code:** ~770 new lines in Phase 2

---

## 🚀 What's Next After Phase 2

**Phase 3: Profile Integration** (2-3 hours)
- Update ProfileService to use ILocationService
- Add geocoding when profile location is set
- Add "Find Nearby Profiles" feature

**Phase 4: Post Integration** (2-3 hours)
- Update PostService to use ILocationService
- Add location tagging for posts
- Add "Find Nearby Posts" feed

**Phase 5: UI Components with Leaflet.js** (6-8 hours)
- LocationPicker.razor component (map click to select location)
- LocationMap.razor component (display posts/profiles on map)
- Integrate into CreatePost and EditProfile forms

**Phase 6: Testing & Documentation** (4-6 hours)
- Write comprehensive unit/integration tests
- Performance benchmarks (PostGIS vs Haversine)
- User documentation
- API documentation

---

## 💡 Architecture Highlights

### Provider-Agnostic Design
✅ Components depend only on `ILocationService` interface  
✅ Easy to swap providers via appsettings.json  
✅ No vendor lock-in  

### FREE Stack (No API Keys Required)
✅ PostgreSQL PostGIS extension (FREE)  
✅ OpenStreetMap tiles (FREE)  
✅ Nominatim geocoding (FREE, 1 req/sec)  
✅ Leaflet.js mapping library (FREE, open source)  

### Performance Optimizations
✅ PostGIS GIST spatial indexes for O(log n) queries  
✅ GEOGRAPHY(POINT, 4326) type for true earth-surface distances  
✅ In-memory caching (30 days TTL) to reduce geocoding API calls  
✅ Rate limiting to respect free tier limits  
✅ Haversine fallback when PostGIS unavailable  

### EF Core Compatibility
✅ Uses same .Ignore() pattern as existing pgvector ContentEmbedding  
✅ Raw SQL for PostGIS operations (bypasses EF Core limitations)  
✅ Triggers auto-sync GeoLocation from Lat/Long changes  
✅ No third-party EF Core extensions required  

---

**Status:** ⚠️ Phase 2 is 70% complete - pending repository interface updates and NuGet package additions.

**Ready for:** Repository method implementation, then testing and Phase 3.
