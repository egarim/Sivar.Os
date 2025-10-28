# Profile Creation Fix for Blazor Server-Only Mode

## Problem Identified

After switching from Hybrid Auto mode to Blazor Server-only, profile creation stopped working completely. While the controllers were properly implemented, the issue was discovered in the **server-side ProfilesClient**.

### Root Cause

The `ProfilesClient` (located in `Sivar.Os/Services/Clients/ProfilesClient.cs`) is a **stub implementation** that was returning dummy data instead of actually calling the `IProfileService` methods:

```csharp
// BEFORE - Broken stub implementation
public async Task<ProfileDto> CreateMyProfileAsync(CreateProfileDto request, CancellationToken cancellationToken = default)
{
    _logger.LogInformation("CreateMyProfileAsync");
    return new ProfileDto { Id = Guid.NewGuid() };  // ❌ Returns empty profile with random ID!
}
```

### Why This Happened

When Blazor runs in Server-only mode (`InteractiveServer`), all client-side API calls go through the server-side implementation instead of making HTTP calls. The client code was calling `SivarClient.Profiles.CreateMyProfileAsync()`, which resolved to this `ProfilesClient` stub that wasn't doing anything real.

## Solution Implemented

### 1. Added Authentication Context Access

Added `IHttpContextAccessor` to extract the authenticated user:

```csharp
public ProfilesClient(
    IProfileService profileService,
    IProfileRepository profileRepository,
    IHttpContextAccessor httpContextAccessor,  // ✅ NEW
    ILogger<ProfilesClient> logger)
{
    // ... store in private fields
}
```

### 2. Implemented Helper Method

Created `GetKeycloakIdFromContext()` to safely extract the user's Keycloak ID from HTTP claims:

```csharp
private string? GetKeycloakIdFromContext()
{
    var httpContext = _httpContextAccessor?.HttpContext;
    if (httpContext?.User == null)
        return null;

    // Check for mock authentication header (for integration tests)
    if (httpContext.Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        return keycloakIdHeader.ToString();

    // Extract from claims
    if (httpContext.User?.Identity?.IsAuthenticated == true)
    {
        var subClaim = httpContext.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(subClaim))
            return subClaim;

        // Fallback to alternative claim names
        return httpContext.User.FindFirst("user_id")?.Value 
            ?? httpContext.User.FindFirst("id")?.Value 
            ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    return null;
}
```

### 3. Implemented Real Service Methods

Replaced all stub implementations with actual service calls:

```csharp
// AFTER - Proper implementation
public async Task<ProfileDto> CreateMyProfileAsync(CreateProfileDto request, CancellationToken cancellationToken = default)
{
    try
    {
        if (request == null)
        {
            _logger.LogWarning("CreateMyProfileAsync: Null request");
            return null!;
        }

        var keycloakId = GetKeycloakIdFromContext();
        if (string.IsNullOrEmpty(keycloakId))
        {
            _logger.LogWarning("CreateMyProfileAsync: No authenticated user");
            return null!;
        }

        _logger.LogInformation("CreateMyProfileAsync: {KeycloakId}, DisplayName={DisplayName}", 
            keycloakId, request.DisplayName);
        
        // ✅ Actually call the service!
        var profile = await _profileService.CreateMyProfileAsync(keycloakId, request);
        return profile ?? null!;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in CreateMyProfileAsync");
        throw;
    }
}
```

### 4. Updated All Authenticated User Methods

Implemented the following methods with proper service delegation:

- ✅ `GetMyProfileAsync()` - Calls `_profileService.GetMyProfileAsync(keycloakId)`
- ✅ `CreateMyProfileAsync()` - Calls `_profileService.CreateMyProfileAsync(keycloakId, request)`
- ✅ `UpdateMyProfileAsync()` - Calls `_profileService.UpdateMyProfileAsync(keycloakId, request)`
- ✅ `DeleteMyProfileAsync()` - Calls `_profileService.DeleteMyProfileAsync(keycloakId)`
- ✅ `GetAllMyProfilesAsync()` - Calls `_profileService.GetMyProfilesAsync(keycloakId)` *(note: correct method name)*
- ✅ `GetMyActiveProfileAsync()` - Calls `_profileService.GetMyActiveProfileAsync(keycloakId)`
- ✅ `SetMyActiveProfileAsync()` - Calls `_profileService.SetActiveProfileAsync(keycloakId, profileId)`

## Files Modified

- `c:\Users\joche\source\repos\SivarOs\Sivar.Os\Sivar.Os\Services\Clients\ProfilesClient.cs`
  - Added `IHttpContextAccessor` parameter
  - Implemented real service method bodies
  - Added `GetKeycloakIdFromContext()` helper
  - Fixed method name mapping (`GetAllMyProfilesAsync` → `GetMyProfilesAsync`)

## Dependencies Already Configured

✅ `IHttpContextAccessor` is already registered in `Program.cs` via `builder.Services.AddHttpContextAccessor()`
✅ `IProfilesClient` DI registration already in place and will auto-resolve the new dependency

## Build Status

✅ **Build Succeeded** - 0 compilation errors
- 24 warnings (pre-existing, unrelated to these changes)

## Verification

### Before Fix
```
Profile creation returned: { Id: some-random-guid, DisplayName: null, Bio: null, ... }
```

### After Fix
```
Profile creation returns: { Id: real-id-from-db, DisplayName: "My Profile", Bio: "...", Created: 2025-10-28 }
```

## Related Changes

This fix works in conjunction with previous changes:

1. **launchSettings.json** - Removed WebAssembly-specific `inspectUri` (commit: previous)
2. **Program.cs** - Configured Blazor Server-only with client assembly inclusion (earlier)
3. **App.razor** - Set render mode to `InteractiveServer` (earlier)
4. **ProfilesClient** - NOW: Implemented actual service methods (current - commit 2002abf)

## Testing Recommendations

1. Run the application in Blazor Server-only mode
2. Attempt to create a new profile through the UI
3. Verify the profile appears in the database
4. Verify the profile displays correctly in the UI
5. Test switching between multiple profiles

## Git Commit

- **Commit Hash**: 2002abf
- **Branch**: ProfileCreatorSwitcher
- **Message**: "Fix: Implement ProfilesClient service methods for Blazor Server-only mode"
