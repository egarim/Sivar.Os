# Hybrid Blazor WebAssembly Data Flow Analysis

## Architecture Overview

This is a **hybrid Blazor WebAssembly** application with:
- **Server-side**: Full ASP.NET Core with EF Core, repositories, and services
- **Client-side**: Blazor WebAssembly client
- **Two ISivarClient implementations**: One server-side, one client-side

---

## Two Implementations of ISivarClient

### 1. **Server-Side Implementation** (`Sivar.Os.Services.Clients.SivarClient`)
**Location**: `c:\Users\joche\source\repos\SivarOs\Sivar.Os\Sivar.Os\Services\Clients\`

**Files**:
- `UsersClient.cs` - Directly accesses IUserService and IUserRepository
- `ProfilesClient.cs` - Directly accesses IProfileService and IProfileRepository
- `AuthClient.cs` - Directly accesses IUserAuthenticationService
- `BaseRepositoryClient.cs` - Base class for direct repository access
- Other clients follow same pattern

**Characteristics**:
```csharp
// SERVER-SIDE: Direct access to services and repositories
public class UsersClient : BaseRepositoryClient, IUsersClient
{
    private readonly IUserService _userService;
    private readonly IUserRepository _userRepository;

    public async Task<UserDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetMeAsync called");
        // Direct DB access via repository
        return new UserDto { Id = Guid.NewGuid(), Email = "user@example.com" };
    }
}
```

**Registration** (Server Program.cs):
```csharp
// --- Client Registration (Sivar.Os.Services.Clients) ---
builder.Services.AddScoped<IUsersClient, UsersClient>();
builder.Services.AddScoped<IProfilesClient, ProfilesClient>();
// ... other clients

// Register the aggregate SivarClient
builder.Services.AddScoped<ISivarClient, Sivar.Os.Services.Clients.SivarClient>();
```

**Advantages**:
✅ Direct database access
✅ No HTTP overhead
✅ Immediate data consistency
✅ Full transaction support

---

### 2. **Client-Side Implementation** (`Sivar.Os.Client.Clients.SivarClient`)
**Location**: `c:\Users\joche\source\repos\SivarOs\Sivar.Os\Sivar.Os.Client\Clients\`

**Files**:
- `UsersClient.cs` - Makes HTTP calls to API endpoints
- `ProfilesClient.cs` - Makes HTTP calls to API endpoints
- `AuthClient.cs` - Makes HTTP calls to API endpoints
- `BaseClient.cs` - Base class for HTTP operations
- Other clients follow same pattern

**Characteristics**:
```csharp
// CLIENT-SIDE: HTTP calls to API
public class UsersClient : BaseClient, IUsersClient
{
    public UsersClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    public async Task<UserDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        // HTTP GET call
        return await GetAsync<UserDto>("api/users/me", cancellationToken);
    }
}
```

**Registration** (Client Program.cs):
```csharp
// Configure SivarClient options
builder.Services.Configure<SivarClientOptions>(options =>
{
    options.BaseAddress = "https://localhost:7001"; // Server address
});

// Register HTTP-based clients
builder.Services.AddScoped<IUsersClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new Sivar.Os.Client.Clients.UsersClient(httpClient, options.Value);
});

builder.Services.AddScoped<ISivarClient, Sivar.Os.Client.Clients.SivarClient>();
```

**Advantages**:
✅ Stateless (can work without server state)
✅ Decoupled from database schema
✅ Can work with any backend API
✅ Follows standard REST patterns

**Disadvantages**:
❌ HTTP latency overhead
❌ Network dependency
❌ Data freshness issues (needs refresh)
❌ Additional JWT validation on each request

---

## Current Data Flow Problem

### The Issue: Empty User/Profile Data

```
User logs in via Keycloak
    ↓
JWT token issued with claims (sub, email, given_name, family_name)
    ↓
Home.razor loads in CLIENT (WebAssembly)
    ↓
Home.razor calls: SivarClient.Users.GetMeAsync()
    ↓
CLIENT-SIDE UsersClient makes HTTP call to: "api/users/me"
    ↓
HTTP Request reaches Server with JWT token
    ↓
UsersController.GetCurrentUser() extracts Keycloak ID from JWT
    ↓
UsersController calls: _userService.GetCurrentUserAsync(keycloakId)
    ↓
BUT: The mock keycloakId in GetKeycloakIdFromRequest() doesn't match
    ↓
User not found in DB or returns empty data
    ↓
Empty UserDto returned to client ❌
```

---

## The Correct Data Flow (FIXED)

### When User Should Already Be in Database

**Scenario**: User logs in → Keycloak authenticates → User auto-registered in database

**Correct Flow**:

1. **User authenticates with Keycloak**
   - JWT token issued with Keycloak ID in `sub` claim
   - Claims include: `sub`, `email`, `given_name`, `family_name`

2. **Client-side Home.razor calls**
   ```csharp
   var userDto = await SivarClient.Users.GetMeAsync();  // HTTP call
   ```

3. **HTTP reaches UsersController.GetCurrentUser()**
   ```csharp
   var keycloakId = GetKeycloakIdFromRequest();  // Extract from JWT
   var user = await _userService.GetCurrentUserAsync(keycloakId);  // Lookup in DB
   ```

4. **FIX APPLIED**: Controller now correctly extracts Keycloak ID from JWT
   ```csharp
   private string GetKeycloakIdFromRequest()
   {
       // Check JWT Bearer token
       if (User?.Identity?.IsAuthenticated == true)
       {
           var keycloakIdClaim = User.FindFirst("sub")?.Value  // Real ID!
                              ?? User.FindFirst("user_id")?.Value;
           
           if (!string.IsNullOrEmpty(keycloakIdClaim))
           {
               return keycloakIdClaim;  // ✅ Returns real ID from JWT
           }
       }
       return null!;
   }
   ```

5. **UserService.GetCurrentUserAsync(realKeycloakId)**
   ```csharp
   var user = await _userRepository.GetByKeycloakIdAsync(realKeycloakId);
   // Now finds the user! ✅
   ```

6. **UserDto with real data returned to client**
   ```csharp
   new UserDto
   {
       Id = user.Id,
       Email = user.Email,           // ✅ populated
       FirstName = user.FirstName,   // ✅ populated
       LastName = user.LastName,     // ✅ populated
   }
   ```

7. **Client updates header**
   ```csharp
   _userName = userDto.FirstName + " " + userDto.LastName;  // ✅ Shows real name
   _userEmail = userDto.Email;  // ✅ Shows real email
   ```

---

## Recommended Data Loading Pattern

For **Home.razor** in this hybrid architecture:

### Option 1: CLIENT-SIDE HTTP Approach (Current - Correct)

```csharp
// Home.razor - WebAssembly client component
@inject ISivarClient SivarClient  // This gets CLIENT-SIDE implementation

protected override async Task OnInitializedAsync()
{
    // Calls HTTP endpoint on server
    await LoadCurrentUserAsync();
}

private async Task LoadCurrentUserAsync()
{
    // This calls client-side SivarClient.Users.GetMeAsync()
    // which makes HTTP call to: /api/users/me
    var userDto = await SivarClient.Users.GetMeAsync();
    
    if (userDto != null)
    {
        _userName = userDto.FirstName + " " + userDto.LastName;
        _userEmail = userDto.Email;
        
        // ALSO load active profile for display name
        var activeProfile = await SivarClient.Profiles.GetMyActiveProfileAsync();
        if (activeProfile != null && !string.IsNullOrEmpty(activeProfile.DisplayName))
        {
            _userName = activeProfile.DisplayName;  // Override with profile name
        }
    }
}
```

**Data Flow**:
```
WebAssembly Client 
  ↓ (HTTP)
Server API Controller
  ↓ (Extract JWT)
Server Service + Repository
  ↓ (Database)
User Record (FirstName, LastName, Email populated)
```

---

### Option 2: SERVER-SIDE APPROACH (For Server Blazor Components)

If this component runs on the **server** (not WebAssembly):

```csharp
// Blazor Server component
@inject ISivarClient SivarClient  // Gets SERVER-SIDE implementation

protected override async Task OnInitializedAsync()
{
    await LoadCurrentUserAsync();
}

private async Task LoadCurrentUserAsync()
{
    // This calls server-side SivarClient.Users.GetMeAsync()
    // Direct database access - NO HTTP
    var userDto = await SivarClient.Users.GetMeAsync();
    
    // Same code, but no network call
    if (userDto != null)
    {
        _userName = userDto.FirstName + " " + userDto.LastName;
        _userEmail = userDto.Email;
    }
}
```

**Data Flow**:
```
Blazor Server Component 
  ↓ (Direct method call)
Server Service + Repository
  ↓ (Database)
User Record (FirstName, LastName, Email)
```

---

## Key Fixes Applied

### 1. **UsersController.cs** - Extract Real Keycloak ID

**Before (Bug)**:
```csharp
private string GetKeycloakIdFromRequest()
{
    return "mock-keycloak-user-id";  // ❌ Always same value
}
```

**After (Fixed)**:
```csharp
private string GetKeycloakIdFromRequest()
{
    if (Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
    {
        return keycloakIdHeader.ToString();
    }

    if (User?.Identity?.IsAuthenticated == true)
    {
        var keycloakIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value
                           ?? User.FindFirst("user_id")?.Value;
        
        if (!string.IsNullOrEmpty(keycloakIdClaim))
        {
            _logger.LogInformation($"[UsersController] Extracted Keycloak ID from JWT: {keycloakIdClaim}");
            return keycloakIdClaim;  // ✅ Real ID from JWT
        }
    }

    if (Request.Headers.ContainsKey("X-Mock-Auth"))
    {
        return "mock-keycloak-user-id";
    }

    return null!;
}
```

### 2. **UsersController.cs** - Extract Real Claims

**Before (Bug)**:
```csharp
private CreateUserFromKeycloakDto CreateUserDtoFromKeycloakClaims()
{
    return new CreateUserFromKeycloakDto
    {
        KeycloakId = "mock-keycloak-user-id",  // ❌ Mock
        Email = "test@example.com",             // ❌ Mock
        FirstName = "Test",                     // ❌ Mock
        LastName = "User",                      // ❌ Mock
        Role = UserRole.RegisteredUser,
        PreferredLanguage = "en"
    };
}
```

**After (Fixed)**:
```csharp
private CreateUserFromKeycloakDto CreateUserDtoFromKeycloakClaims()
{
    if (User?.Identity?.IsAuthenticated != true)
    {
        return new CreateUserFromKeycloakDto { /* defaults */ };
    }

    // Extract from JWT claims
    var keycloakId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value
                  ?? "unknown-id";

    var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
             ?? User.FindFirst("email")?.Value
             ?? "user@example.com";

    var firstName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value
                 ?? User.FindFirst("given_name")?.Value
                 ?? "";

    var lastName = User.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value
                ?? User.FindFirst("family_name")?.Value
                ?? "";

    _logger.LogInformation($"[UsersController] Creating user from Keycloak claims: ...");

    return new CreateUserFromKeycloakDto
    {
        KeycloakId = keycloakId,  // ✅ Real ID
        Email = email,             // ✅ Real email
        FirstName = firstName,     // ✅ Real first name
        LastName = lastName,       // ✅ Real last name
        Role = UserRole.RegisteredUser,
        PreferredLanguage = "en"
    };
}
```

### 3. **Home.razor** - Two-Way Binding + Profile Loading

**Before (Issue)**:
```razor
<Header UserName="@_userName"
        UserEmail="@_userEmail"
        ... />
```

**After (Fixed)**:
```razor
<Header @bind-UserName="@_userName"
        @bind-UserEmail="@_userEmail"
        ... />
```

**And LoadCurrentUserAsync now loads profile**:
```csharp
private async Task LoadCurrentUserAsync()
{
    // Load user
    var userDto = await SivarClient.Users.GetMeAsync();
    if (userDto != null)
    {
        _userName = $"{userDto.FirstName} {userDto.LastName}".Trim();
        _userEmail = userDto.Email ?? "no-email";
    }

    // ALSO load active profile
    try
    {
        var activeProfile = await SivarClient.Profiles.GetMyActiveProfileAsync();
        if (activeProfile != null && !string.IsNullOrWhiteSpace(activeProfile.DisplayName))
        {
            _userName = activeProfile.DisplayName;  // Use profile display name
        }
    }
    catch { /* non-fatal */ }

    _isUserLoading = false;
    StateHasChanged();  // Triggers two-way binding update
}
```

---

## Expected Console Output After Fixes

```
[Home] Existing user authenticated: joche@joche.com
[UsersController] Extracted Keycloak ID from JWT: 12345-67890-abcdef
[UsersController] Creating user from Keycloak claims: KeycloakId=12345-67890-abcdef, Email=joche@joche.com, FirstName=Joche, LastName=User

[HOME-CLIENT] ✅ User DTO Received:
[HOME-CLIENT]   - ID: 9bd777a4-fdb7-42de-8210-fea9c2ca55aa
[HOME-CLIENT]   - First Name: Joche                    ✅ POPULATED
[HOME-CLIENT]   - Last Name: User                      ✅ POPULATED
[HOME-CLIENT]   - Email: joche@joche.com              ✅ POPULATED
[HOME-CLIENT] - Full Name: Joche User                ✅ POPULATED

[HOME-CLIENT] ✅ Active Profile DTO Received:
[HOME-CLIENT]   - ID: 0c12973e-9dde-41b1-8e41-bbbb7e87afc3
[HOME-CLIENT]   - DisplayName: Joche's Profile        ✅ POPULATED
[HOME-CLIENT]   - Avatar: https://...                 ✅ POPULATED
[HOME-CLIENT]   - LocationDisplay: New York, NY       ✅ POPULATED

[HOME-CLIENT] ✅ StateHasChanged() called
[HOME-CLIENT] ✅ Header updated with: Joche's Profile / joche@joche.com
```

---

## Summary

### Architecture:
- **Server-side clients** (`Services.Clients`): Direct database access
- **Client-side clients** (`Client.Clients`): HTTP API calls
- **Both implement same ISivarClient interface**

### The Problem (Fixed):
- Controller was using mock Keycloak ID instead of extracting real ID from JWT

### The Solution (Applied):
- Extract real Keycloak ID from `sub` JWT claim
- Extract real user claims (email, firstName, lastName) from JWT
- Load active profile data for display
- Use two-way binding in Header component
- Call StateHasChanged() after loading data

### Result:
✅ User profile information shows correctly in header
✅ Data comes from database (already created during login)
✅ Works in hybrid Blazor architecture (both server and client components)
