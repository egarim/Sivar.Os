# 🔄 Hybrid Blazor Service Architecture - ProfileSwitcherService

## Overview

The ProfileSwitcher implementation follows the **two-tier service pattern** used throughout the Sivar.Os hybrid Blazor application:

1. **Client-Side** - Uses HttpClient for API calls
2. **Server-Side** - Uses repositories for direct database access

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     Home.razor (InteractiveServer)              │
│                                                                 │
│  @inject IProfileSwitcherService ProfileSwitcherService       │
│  Runs on: SERVER-SIDE (Server's DI Container)                  │
└─────────────────────────────────────────────────────────────────┘
                               ↓
                ┌──────────────────────────────┐
                │  IProfileSwitcherService     │
                │  (Shared Interface)          │
                │  Sivar.Os.Client/Services   │
                └──────────────────────────────┘
                    ↙                      ↖
        ┌──────────────────────┐  ┌──────────────────────┐
        │  Client-Side         │  │  Server-Side         │
        │  Implementation      │  │  Implementation      │
        ├──────────────────────┤  ├──────────────────────┤
        │ProfileSwitcherService│  │ProfileSwitcherClient │
        │                      │  │                      │
        │Uses: HttpClient      │  │Uses: Repositories    │
        │API: /api/profile/*   │  │Direct DB Access      │
        │                      │  │                      │
        │Location:             │  │Location:             │
        │Sivar.Os.Client/      │  │Sivar.Os/Services/    │
        │Services/             │  │Clients/              │
        │ProfileSwitcher       │  │ProfileSwitcher       │
        │Service.cs            │  │Client.cs             │
        └──────────────────────┘  └──────────────────────┘
                ↓                           ↓
         HTTP Requests            Direct Repository Access
         to API Controllers        - IProfileService
                                   - IProfileTypeService
                                   - IHttpContextAccessor
                                   - Database (via EF Core)
```

---

## File Structure

### Client-Side Implementation
```
Sivar.Os.Client/
├── Services/
│   └── ProfileSwitcherService.cs    ← Uses HttpClient
│       ├── GetUserProfilesAsync()      → GET /api/profile/my-profiles
│       ├── GetActiveProfileAsync()     → GET /api/profile/active
│       ├── SwitchProfileAsync()        → PUT /api/profile/{id}/set-active
│       ├── CreateProfileAsync()        → POST /api/profile
│       └── GetProfileTypesAsync()      → GET /api/profile-type
```

### Server-Side Implementation
```
Sivar.Os/Services/Clients/
└── ProfileSwitcherClient.cs         ← Uses Repositories
    ├── GetUserProfilesAsync()          → IProfileService.GetMyProfilesAsync()
    ├── GetActiveProfileAsync()         → IProfileService.GetMyActiveProfileAsync()
    ├── SwitchProfileAsync()            → IProfileService.SetActiveProfileAsync()
    ├── CreateProfileAsync()            → IProfileService.CreateProfileAsync()
    └── GetProfileTypesAsync()          → IProfileTypeService.GetActiveProfileTypesAsync()
```

---

## Dependency Injection Registration

### Client Project (Program.cs)
```csharp
// Client-side: For WASM components (if any)
builder.Services.AddScoped<IProfileSwitcherService, ProfileSwitcherService>();
```

### Server Project (Program.cs)
```csharp
// Server-side: For InteractiveServer components
builder.Services.AddScoped<IProfileSwitcherService, ProfileSwitcherClient>();
```

---

## How It Works

### When Home.razor Loads (InteractiveServer)
```
1. Home.razor requests IProfileSwitcherService
   ↓
2. Server's DI container provides ProfileSwitcherClient
   ↓
3. ProfileSwitcherClient extracts user's Keycloak ID from HttpContext claims
   ↓
4. Direct repository access via IProfileService and IProfileTypeService
   ↓
5. Database query returns ProfileDto objects
   ↓
6. Home.razor receives data immediately (no HTTP overhead)
```

### Key Differences

| Aspect | Client-Side | Server-Side |
|--------|------------|------------|
| **HttpClient** | ✅ Required | ❌ Not needed |
| **Dependencies** | HttpClient, Logger | IProfileService, IProfileTypeService, IHttpContextAccessor, Logger |
| **User Context** | Via JWT token | Via HttpContext claims |
| **Performance** | HTTP network calls | Direct memory access |
| **Use Case** | WASM components | InteractiveServer components |

---

## Implementation Details

### Extracting User Context (Server-Side)
```csharp
private string GetCurrentUserKeycloakId()
{
    var user = _httpContextAccessor.HttpContext?.User;
    var keycloakId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(keycloakId))
    {
        throw new UnauthorizedAccessException("User is not authenticated");
    }

    return keycloakId;
}
```

The server-side implementation automatically handles:
- ✅ Getting current user from HttpContext
- ✅ Extracting Keycloak ID from claims
- ✅ Passing it to ProfileService methods
- ✅ Error handling for authentication issues

### DTO Conversion (Server-Side)
```csharp
// CreateAnyProfileDto → CreateProfileDto
var createDto = new CreateProfileDto
{
    DisplayName = request.DisplayName,
    Bio = request.Bio ?? string.Empty,
    Avatar = request.Avatar ?? string.Empty,
    Location = request.Location,
    VisibilityLevel = request.VisibilityLevel,
    // ... other properties
};

// ProfileService automatically determines profile type from metadata
var profile = await _profileService.CreateProfileAsync(createDto, keycloakId);
```

---

## Error Handling

Both implementations include comprehensive error handling:

```csharp
try
{
    // Perform operation
}
catch (UnauthorizedAccessException ex)
{
    _logger.LogWarning("Authentication error: {Message}", ex.Message);
    // Return empty/null value gracefully
}
catch (Exception ex)
{
    _logger.LogError("Error: {Message}", ex.Message);
    // Return empty/null value gracefully
}
```

**Logging Pattern:**
```
[ProfileSwitcherClient] Creating new profile
[ProfileSwitcherClient] Successfully created profile: {ProfileId}
[ProfileSwitcherClient] Set profile {ProfileId} as active
```

---

## Integration with Home.razor

### Registration
```csharp
@inject IProfileSwitcherService ProfileSwitcherService
```

### Usage
```csharp
private async Task LoadUserProfilesAsync()
{
    _userProfiles = await ProfileSwitcherService.GetUserProfilesAsync();
    _activeProfile = await ProfileSwitcherService.GetActiveProfileAsync();
}

private async Task HandleProfileChanged(ProfileDto profile)
{
    var success = await ProfileSwitcherService.SwitchProfileAsync(profile.Id);
    if (success)
    {
        _activeProfile = profile;
        // Reload feed with new profile
    }
}
```

---

## Why This Pattern?

### Benefits of Two-Tier Architecture

1. **Single Interface** - `IProfileSwitcherService` works for both client and server
2. **Performance** - Server uses direct DB access, no HTTP overhead
3. **Scalability** - Clients can be WASM or server-rendered interchangeably
4. **Consistency** - Same interface signature for both implementations
5. **Maintainability** - Changes to API automatically work server-side
6. **Security** - Server-side has direct context access without JWT parsing

### Example: Adding New Method

If you add a new method to `IProfileSwitcherService`:

```csharp
Task<ProfileStatisticsDto> GetProfileStatsAsync();
```

Implement it in **both**:

**Client-side:**
```csharp
// Use HttpClient
var response = await _httpClient.GetAsync("/api/profile/statistics");
```

**Server-side:**
```csharp
// Use repositories directly
var stats = await _profileService.GetProfileStatisticsAsync();
```

Both automatically work in their respective contexts!

---

## Configuration Summary

| Component | Registration | Implementation | Dependencies |
|-----------|-------------|----------------|--------------|
| **Client** | `Program.cs` (Client) | `ProfileSwitcherService` | HttpClient |
| **Server** | `Program.cs` (Server) | `ProfileSwitcherClient` | IProfileService, IProfileTypeService |
| **Interface** | Shared (Sivar.Os.Client.Services) | `IProfileSwitcherService` | Both |

---

## Testing

### Client-Side (Unit Test)
```csharp
// Mock HttpClient
var mockHttp = new MockHttpMessageHandler();
mockHttp.Expect(HttpMethod.Get, "/api/profile/my-profiles")
    .Respond(JsonContent.Create(new List<ProfileDto> { ... }));

var service = new ProfileSwitcherService(httpClient, logger);
var profiles = await service.GetUserProfilesAsync();
// Assert...
```

### Server-Side (Integration Test)
```csharp
// Use real repositories
var profileService = new ProfileService(repository, ...);
var client = new ProfileSwitcherClient(profileService, ...);
var profiles = await client.GetUserProfilesAsync();
// Assert...
```

---

**Architecture Pattern:** Hybrid Blazor with Repository Pattern
**Date:** October 28, 2025
**Status:** ✅ Implemented
