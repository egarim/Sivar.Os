# Profile Routing Implementation Summary

## Overview
Implemented flexible profile routing that supports both GUID-based and human-readable slug-based URLs.

## Routes Implemented

### 1. Home Route
- **URL**: `/`
- **Component**: `Home.razor`
- **Purpose**: Main feed/dashboard

### 2. Profile Route (NEW)
- **URL**: `/{identifier}`
- **Component**: `ProfilePage.razor`
- **Supports**:
  - GUID format: `/f9de039e-bb64-46ac-ade2-0667b9186f45`
  - Slug format: `/jose-ojeda` (converts to "Jose Ojeda")
  - Profile Display Name lookup from database

## Implementation Details

### 1. Repository Layer
**File**: `Sivar.Os.Data/Repositories/ProfileRepository.cs`

Added method: `GetByDisplayNameSlugAsync(string slug)`
- Converts slug to DisplayName (e.g., "jose-ojeda" → "Jose Ojeda")
- Searches for profiles with matching DisplayName (case-insensitive)
- Only returns public profiles for security

**Interface**: `Sivar.Os.Shared/Repositories/IProfileRepository.cs`
```csharp
Task<Profile?> GetByDisplayNameSlugAsync(string slug);
```

### 2. Service Layer
**File**: `Sivar.Os/Services/ProfileService.cs`

Added method: `GetProfileByIdentifierAsync(string identifier)`
- First tries parsing as GUID
- If GUID: calls `GetPublicProfileAsync(profileId)`
- If not GUID: calls `GetByDisplayNameSlugAsync(identifier)`
- Increments view count for successful profile views
- Returns null if profile not found or not public

**Interface**: `Sivar.Os.Shared/Services/IProfileService.cs`
```csharp
Task<ProfileDto?> GetProfileByIdentifierAsync(string identifier);
```

### 3. Client Layer
**Files**: 
- `Sivar.Os.Client/Clients/ProfilesClient.cs` (HTTP client)
- `Sivar.Os/Services/Clients/ProfilesClient.cs` (Server-side client)

Added method: `GetProfileByIdentifierAsync(string identifier)`
- HTTP client calls: `api/profiles/by-identifier/{identifier}`
- Server-side client calls ProfileService directly

**Interface**: `Sivar.Os.Shared/Clients/IProfilesClient.cs`
```csharp
Task<ProfileDto> GetProfileByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
```

### 4. Controller Layer
**File**: `Sivar.Os/Controllers/ProfilesController.cs`

Added endpoint: `GET /api/profiles/by-identifier/{identifier}`
- Route: `[HttpGet("by-identifier/{identifier}")]`
- Attribute: `[AllowAnonymous]`
- Returns: `ActionResult<ProfileDto>`
- Logs: identifier, profile ID, duration, errors

### 5. UI Layer
**File**: `Sivar.Os.Client/Pages/ProfilePage.razor`

**Changes**:
1. **Route**: Changed from `/profile/{ProfileSlug}` to `/{Identifier}`
2. **Parameter**: Changed `ProfileSlug` to `Identifier`
3. **Data Loading**: Now fetches real profile data from `ProfilesClient`
4. **Fallback**: Shows mock data if profile not found
5. **Error Handling**: Displays user-friendly error messages

**Features**:
- Loading state during data fetch
- Real profile data display (DisplayName, Bio, etc.)
- Graceful fallback to mock data for demo
- Support for both GUID and slug identifiers

## Route Priority
Blazor routing gives priority to exact matches:
1. `/` → `Home.razor` (exact match)
2. `/{Identifier}` → `ProfilePage.razor` (parameterized match)

## Database Query Examples

### Find Profile by Slug
```sql
-- Example: /jose-ojeda → "Jose Ojeda"
SELECT * FROM "Sivar_Profiles" 
WHERE LOWER("DisplayName") = LOWER('Jose Ojeda')
AND "VisibilityLevel" = 1  -- Public
AND "IsDeleted" = false;
```

### Find Profile by ID
```sql
-- Example: /f9de039e-bb64-46ac-ade2-0667b9186f45
SELECT * FROM "Sivar_Profiles" 
WHERE "Id" = 'f9de039e-bb64-46ac-ade2-0667b9186f45'
AND "VisibilityLevel" = 1  -- Public
AND "IsDeleted" = false;
```

## Testing Scenarios

### Scenario 1: Profile by DisplayName Slug
**URL**: `/jose-ojeda`
1. ProfilePage receives `Identifier = "jose-ojeda"`
2. Calls `ProfilesClient.GetProfileByIdentifierAsync("jose-ojeda")`
3. Service tries `Guid.TryParse("jose-ojeda")` → fails
4. Service calls repository `GetByDisplayNameSlugAsync("jose-ojeda")`
5. Repository converts to "Jose Ojeda" and searches
6. Returns profile if found and public

### Scenario 2: Profile by GUID
**URL**: `/f9de039e-bb64-46ac-ade2-0667b9186f45`
1. ProfilePage receives `Identifier = "f9de039e-bb64-46ac-ade2-0667b9186f45"`
2. Calls `ProfilesClient.GetProfileByIdentifierAsync(...)`
3. Service tries `Guid.TryParse(...)` → succeeds
4. Service calls `GetPublicProfileAsync(profileId)`
5. Returns profile if found and public

### Scenario 3: Home Route
**URL**: `/`
1. Exact match to `Home.razor`
2. Shows main feed/dashboard

### Scenario 4: Profile Not Found
**URL**: `/non-existent-user`
1. Service searches for "Non Existent User"
2. No match found in database
3. Returns null
4. ProfilePage shows fallback mock data with error message

## Security Considerations

1. **Public Profiles Only**: Only profiles with `VisibilityLevel = Public` are accessible via slug/GUID routes
2. **No Authentication Required**: Profile viewing is anonymous (uses `[AllowAnonymous]`)
3. **View Count Tracking**: Each profile view increments the view counter
4. **Private Profile Protection**: Private profiles cannot be accessed via public routes

## Future Enhancements

### 1. Add Handle Field (Recommended)
Currently using DisplayName for slug generation, which can cause:
- Collisions (multiple users with same name)
- Case sensitivity issues
- Special character problems

**Recommendation**: Add a `Handle` or `Username` field to Profile entity:
```csharp
[Required]
[StringLength(50)]
[RegularExpression(@"^[a-z0-9-]+$")]
public string Handle { get; set; } = string.Empty;
```

Benefits:
- Unique constraint ensures no collisions
- URL-friendly by design
- Easier to search and index
- Consistent format

### 2. Custom Route Constraint
Add a route constraint to prevent ProfilePage from catching reserved routes:
```csharp
@page "/{Identifier:not(api|authentication|signup|login|welcome|counter|weather)}"
```

### 3. 301 Redirects
Redirect GUID URLs to slug URLs for SEO:
```csharp
// If accessed via GUID, redirect to slug
if (Guid.TryParse(Identifier, out _))
{
    var slugUrl = GenerateSlug(profile.DisplayName);
    Navigation.NavigateTo($"/{slugUrl}", replace: true);
}
```

### 4. Canonical URLs
Add canonical URL meta tags:
```html
<link rel="canonical" href="https://yourdomain.com/jose-ojeda" />
```

### 5. Profile Stats Integration
Currently showing placeholder stats (0, 0, 0). Integrate:
- Post count from PostService
- Follower count from FollowerService
- Following count from FollowerService

## Files Modified

### New Methods Added
1. `IProfileRepository.GetByDisplayNameSlugAsync()` ✅
2. `ProfileRepository.GetByDisplayNameSlugAsync()` ✅
3. `IProfileService.GetProfileByIdentifierAsync()` ✅
4. `ProfileService.GetProfileByIdentifierAsync()` ✅
5. `IProfilesClient.GetProfileByIdentifierAsync()` ✅
6. `ProfilesClient.GetProfileByIdentifierAsync()` (HTTP) ✅
7. `ProfilesClient.GetProfileByIdentifierAsync()` (Server) ✅
8. `ProfilesController.GetProfileByIdentifier()` ✅

### Components Updated
1. `ProfilePage.razor` - Complete rewrite with real data fetching ✅

## Verification Steps

1. **Build the solution**:
   ```bash
   dotnet build
   ```

2. **Run the application**:
   ```bash
   dotnet run --project Sivar.Os/Sivar.Os.csproj
   ```

3. **Test routes**:
   - Navigate to `/` → Should show Home feed
   - Navigate to `/jose-ojeda` → Should show profile or "not found"
   - Navigate to `/f9de039e-bb64-46ac-ade2-0667b9186f45` → Should show profile by ID
   - Check browser console for logging

4. **Database Check**:
   ```sql
   -- List existing profiles with their DisplayNames
   SELECT "Id", "DisplayName", "VisibilityLevel" 
   FROM "Sivar_Profiles" 
   WHERE "IsDeleted" = false 
   ORDER BY "CreatedAt" DESC;
   ```

5. **Create a test profile** (if none exist):
   ```sql
   -- Via the application's profile creation UI
   -- Or use existing profiles from the database
   ```

## Logging
The implementation includes comprehensive logging at each layer:
- Repository: Search operations
- Service: GUID vs slug detection, profile lookups
- Controller: API requests, responses, errors
- UI: Profile loading, errors, fallbacks

Check logs for:
- `[ProfileRepository.GetByDisplayNameSlugAsync]`
- `[ProfileService.GetProfileByIdentifierAsync]`
- `[ProfilesController.GetProfileByIdentifier]`
- `[ProfilePage]`

## Conclusion
The routing system now supports both user-friendly slugs (`/jose-ojeda`) and technical IDs (`/f9de039e-bb64-46ac-ade2-0667b9186f45`), providing flexibility for sharing profiles and backward compatibility with GUID-based links.
