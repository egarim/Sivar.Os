# ⚡ Quick Reference - ProfileSwitcher Two-Tier Architecture

## Problem → Solution

| Aspect | Details |
|--------|---------|
| **Error** | HttpClient not available in server's DI container |
| **Cause** | Home.razor runs InteractiveServer (server-side), not WASM |
| **Fix** | Create server-side implementation using repositories |

---

## Files Changed

### ✨ Created
```
Sivar.Os/Services/Clients/ProfileSwitcherClient.cs (New server implementation)
```

### ✏️ Updated
```
Sivar.Os/Program.cs (Line 144: ProfileSwitcherService → ProfileSwitcherClient)
Sivar.Os/Program.cs (Line 13: Added using directive)
```

---

## Implementation Quick View

```csharp
// SERVER-SIDE (New)
public class ProfileSwitcherClient : BaseRepositoryClient, IProfileSwitcherService
{
    // Injected dependencies
    private readonly IProfileService _profileService;
    private readonly IProfileTypeService _profileTypeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    // Key method example
    public async Task<List<ProfileDto>> GetUserProfilesAsync()
    {
        var keycloakId = GetCurrentUserKeycloakId();  // From HttpContext claims
        return await _profileService.GetMyProfilesAsync(keycloakId);
    }
}

// Registered as:
builder.Services.AddScoped<IProfileSwitcherService, ProfileSwitcherClient>();
```

---

## How Methods Map

| Interface Method | Server Implementation |
|------------------|---------------------|
| GetUserProfilesAsync() | profileService.GetMyProfilesAsync(keycloakId) |
| GetActiveProfileAsync() | profileService.GetMyActiveProfileAsync(keycloakId) |
| SwitchProfileAsync(id) | profileService.SetActiveProfileAsync(keycloakId, id) |
| CreateProfileAsync(dto) | profileService.CreateProfileAsync(createDto, keycloakId) |
| GetProfileTypesAsync() | profileTypeService.GetActiveProfileTypesAsync() |

---

## Key Methods

### Get Current User
```csharp
private string GetCurrentUserKeycloakId()
{
    var user = _httpContextAccessor.HttpContext?.User;
    var keycloakId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(keycloakId))
        throw new UnauthorizedAccessException("Not authenticated");
    return keycloakId;
}
```

### Create Profile
```csharp
public async Task<ProfileDto?> CreateProfileAsync(CreateAnyProfileDto request)
{
    var keycloakId = GetCurrentUserKeycloakId();
    
    var createDto = new CreateProfileDto { ... };  // Map DTO
    var profile = await _profileService.CreateProfileAsync(createDto, keycloakId);
    
    if (request.SetAsActive && profile != null)
        await _profileService.SetActiveProfileAsync(keycloakId, profile.Id);
    
    return profile;
}
```

---

## Comparison

| Feature | Client | Server |
|---------|--------|--------|
| **Class** | ProfileSwitcherService | ProfileSwitcherClient |
| **Uses** | HttpClient | IProfileService |
| **Location** | Sivar.Os.Client/Services | Sivar.Os/Services/Clients |
| **User Context** | JWT token | HttpContext claims |
| **Database** | API calls | Direct access |
| **Render Mode** | WASM | InteractiveServer |

---

## Logging

All operations logged with prefix:
```
[ProfileSwitcherClient] Operation description
[ProfileSwitcherClient] Retrieved N profiles
[ProfileSwitcherClient] Successfully switched to profile: {Id}
[ProfileSwitcherClient] Error: {Message}
```

---

## Error Handling

```csharp
catch (UnauthorizedAccessException)
    → User not authenticated, return empty list/null
    
catch (Exception)
    → Other error occurred, return empty list/null
    → Always log the error
```

---

## DI Registration

```csharp
// Server Program.cs - Line 144
builder.Services.AddScoped<IProfileSwitcherService, ProfileSwitcherClient>();

// Dependencies automatically injected:
// - IProfileService (already registered)
// - IProfileTypeService (already registered)  
// - IHttpContextAccessor (built-in)
// - ILogger<ProfileSwitcherClient> (built-in)
```

---

## Testing

### In Component
```csharp
@inject IProfileSwitcherService ProfileSwitcherService

// Works on server because server DI container provides ProfileSwitcherClient
var profiles = await ProfileSwitcherService.GetUserProfilesAsync();
```

### In Tests
```csharp
// Server-side tests
var mockProfileService = new Mock<IProfileService>();
var client = new ProfileSwitcherClient(mockProfileService.Object, ...);
```

---

## Status

| Check | Status |
|-------|--------|
| **Compilation** | ✅ No errors |
| **Dependencies** | ✅ All resolved |
| **Registration** | ✅ Correct |
| **Interface** | ✅ Implemented |
| **Error Handling** | ✅ Complete |
| **Logging** | ✅ Configured |
| **Ready to Test** | ✅ YES |

---

## Related Files

- `HYBRID_BLAZOR_SERVICE_FIX.md` - Detailed technical explanation
- `PROFILE_SWITCHER_HYBRID_ARCHITECTURE.md` - Complete architecture guide
- `PROFILE_SWITCHER_SERVICE_FIX_COMPLETE.md` - Full summary

---

**Quick Start:** The ProfileSwitcher component now works with both client-side (HTTP) and server-side (repository) implementations automatically!

✅ **Ready to run the application**
