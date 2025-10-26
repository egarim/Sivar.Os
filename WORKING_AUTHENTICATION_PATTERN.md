# Working Authentication Pattern - User & Profile Creation After Login

## Overview
This document captures the **proven working pattern** used in `Home.razor` for creating/getting users and profiles after Keycloak login. This pattern has been validated and is used successfully across the application.

---

## Pattern Flow (High Level)

```
User Logs In via Keycloak
        ↓
Home.razor OnInitializedAsync() called
        ↓
EnsureUserAndProfileCreatedAsync()
        ├─ Extract Keycloak claims (sub, email, first_name, last_name)
        ├─ Call SivarClient.Auth.AuthenticateUserAsync(keycloakId, authInfo)
        └─ AuthenticateUserAsync:
           ├─ Check if user exists by Keycloak ID
           ├─ If new: Create user + default profile + set as active
           └─ If existing: Return user + active profile
        ↓
LoadCurrentUserAsync()
        ├─ Call SivarClient.Users.GetMeAsync()
        └─ Call SivarClient.Profiles.GetMyActiveProfileAsync()
        ↓
UI displays user info in header
```

---

## Key Components

### 1. **Home.razor** (Client-Side Razor Component)

**Location:** `Sivar.Os.Client/Pages/Home.razor`

**Critical Methods:**

#### `EnsureUserAndProfileCreatedAsync()`
- Called on page initialization (`OnInitializedAsync`)
- Extracts Keycloak claims from authenticated user
- Calls authentication service to create/get user and profile

```csharp
private async Task EnsureUserAndProfileCreatedAsync()
{
    try
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        if (!user.Identity?.IsAuthenticated ?? true)
            return; // User not authenticated

        // Extract Keycloak claims using helper method
        var (keycloakId, email, firstName, lastName) = ExtractKeycloakClaims(user);

        if (string.IsNullOrEmpty(keycloakId) || string.IsNullOrEmpty(email))
            return;

        Console.WriteLine("[Home] Attempting to create user/profile if needed");

        // Call authentication service via SivarClient
        var authInfo = new UserAuthenticationInfo
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = "RegisteredUser"
        };

        var result = await SivarClient.Auth.AuthenticateUserAsync(keycloakId, authInfo);
        
        if (result.IsSuccess)
        {
            if (result.IsNewUser)
                Console.WriteLine($"[Home] New user and profile created for {email}");
            else
                Console.WriteLine($"[Home] Existing user authenticated: {email}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Home] Error ensuring user/profile created: {ex.Message}");
        // Don't throw - this is a best-effort operation
    }
}
```

#### `ExtractKeycloakClaims(ClaimsPrincipal user)`
- Extracts standard OpenID Connect claims from JWT token
- **Primary claim:** `sub` (OpenID Connect standard subject identifier)
- **Fallbacks:** `user_id`, `id`, `NameIdentifier`

```csharp
private (string? keycloakId, string? email, string firstName, string lastName) ExtractKeycloakClaims(ClaimsPrincipal user)
{
    string? keycloakId = user.FindFirst("sub")?.Value
                      ?? user.FindFirst("user_id")?.Value
                      ?? user.FindFirst("id")?.Value
                      ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    string? email = user.FindFirst("email")?.Value
                 ?? user.FindFirst("preferred_username")?.Value;

    string firstName = user.FindFirst("given_name")?.Value ?? "";
    string lastName = user.FindFirst("family_name")?.Value ?? "";

    return (keycloakId, email, firstName, lastName);
}
```

#### `LoadCurrentUserAsync()`
- Loads authenticated user information from API
- Gets current user via `GetMeAsync()`
- Gets active profile via `GetMyActiveProfileAsync()`
- **Used to populate header with user name, email, avatar**

```csharp
private async Task LoadCurrentUserAsync()
{
    try
    {
        _isUserLoading = true;
        StateHasChanged();
        
        // Load user information
        var userDto = await SivarClient.Users.GetMeAsync();
        
        if (userDto != null)
        {
            _userName = $"{userDto.FirstName} {userDto.LastName}".Trim();
            _userEmail = userDto.Email ?? "no-email";
            _currentUserId = userDto.Id;
            
            // Also load the active profile
            try
            {
                var activeProfile = await SivarClient.Profiles.GetMyActiveProfileAsync();
                
                if (activeProfile != null)
                {
                    if (!string.IsNullOrWhiteSpace(activeProfile.DisplayName))
                        _userName = activeProfile.DisplayName;
                    
                    _currentProfileId = activeProfile.Id;
                }
            }
            catch (Exception profileEx)
            {
                Console.WriteLine($"⚠️ Error loading active profile: {profileEx.Message}");
                // Continue with user data even if profile fails
            }
            
            _isUserLoading = false;
            StateHasChanged();
        }
    }
    catch (Exception ex)
    {
        _userName = "Error Loading";
        _userEmail = "Check console";
        _isUserLoading = false;
        StateHasChanged();
    }
}
```

---

### 2. **UserAuthenticationService** (Server-Side Business Logic)

**Location:** `Sivar.Os/Services/UserAuthenticationService.cs`

**Responsibility:** Core business logic for user/profile creation

```csharp
public interface IUserAuthenticationService
{
    Task<UserAuthenticationResult> AuthenticateUserAsync(string keycloakId, UserAuthenticationInfo authInfo);
}

public class UserAuthenticationService : IUserAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IProfileService _profileService;
    private readonly ILogger<UserAuthenticationService> _logger;

    /// <summary>
    /// Handles user authentication flow - creates user and default profile if needed
    /// </summary>
    public async Task<UserAuthenticationResult> AuthenticateUserAsync(string keycloakId, UserAuthenticationInfo authInfo)
    {
        try
        {
            // Check if user exists
            var existingUser = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            
            if (existingUser != null)
            {
                // User exists - get their active profile
                var activeProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);                
                return new UserAuthenticationResult
                {
                    IsSuccess = true,
                    User = MapToDto(existingUser),
                    ActiveProfile = activeProfile,
                    IsNewUser = false
                };
            }

            // New user - create user and default profile
            var newUser = await CreateNewUserAsync(keycloakId, authInfo);
            var defaultProfile = await CreateDefaultProfileAsync(newUser, authInfo);

            return new UserAuthenticationResult
            {
                IsSuccess = true,
                User = MapToDto(newUser),
                ActiveProfile = defaultProfile,
                IsNewUser = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user authentication for {KeycloakId}", keycloakId);
            
            return new UserAuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "Authentication failed"
            };
        }
    }

    private async Task<User> CreateNewUserAsync(string keycloakId, UserAuthenticationInfo authInfo)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            KeycloakId = keycloakId,
            Email = authInfo.Email,
            FirstName = authInfo.FirstName,
            LastName = authInfo.LastName,
            Role = DetermineUserRole(authInfo.Role),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        
        return user;
    }

    private async Task<ProfileDto?> CreateDefaultProfileAsync(User user, UserAuthenticationInfo authInfo)
    {
        var createDto = new CreateProfileDto
        {
            DisplayName = $"{authInfo.FirstName} {authInfo.LastName}",
            Bio = "Welcome to Sivar! This is your default profile.",
            Metadata = "{}",
            Tags = new List<string> { "new-user" },
            VisibilityLevel = VisibilityLevel.Public
        };

        try
        {
            var profile = await _profileService.CreateProfileAsync(createDto, user.KeycloakId);
            
            // Set as active profile
            if (profile != null)
            {
                await _profileService.SetActiveProfileAsync(user.KeycloakId, profile.Id);
            }
            
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default profile for user {KeycloakId}", user.KeycloakId);
            return null;
        }
    }
}
```

---

### 3. **AuthenticationController** (API Endpoint)

**Location:** `Sivar.Os/Controllers/AuthenticationController.cs`

**Endpoint:** `POST /authentication/authenticate/{keycloakId}`

```csharp
[ApiController]
[Route("authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly ILogger<AuthenticationController> _logger;

    /// <summary>
    /// Authenticate user and auto-create user/profile if needed after Keycloak login
    /// This endpoint is called from the client-side Home page
    /// </summary>
    [HttpPost("authenticate/{keycloakId}")]
    public async Task<IActionResult> AuthenticateUser(string keycloakId, [FromBody] UserAuthenticationInfo authInfo)
    {
        try
        {
            _logger.LogInformation(
                "Authenticating user: KeycloakId={KeycloakId}, Email={Email}, Name={FirstName} {LastName}",
                keycloakId, authInfo.Email, authInfo.FirstName, authInfo.LastName);

            var result = await _userAuthenticationService.AuthenticateUserAsync(keycloakId, authInfo);

            if (result.IsSuccess)
            {
                if (result.IsNewUser)
                {
                    _logger.LogInformation(
                        "New user created: UserId={UserId}, ProfileId={ProfileId}, Email={Email}",
                        result.User?.Id, result.ActiveProfile?.Id, authInfo.Email);
                }
                else
                {
                    _logger.LogInformation(
                        "Existing user authenticated: UserId={UserId}, Email={Email}",
                        result.User?.Id, authInfo.Email);
                }
                
                return Ok(result);
            }
            else
            {
                _logger.LogWarning(
                    "Authentication failed for {Email}: {ErrorMessage}",
                    keycloakId, result.ErrorMessage);
                
                return BadRequest(new
                {
                    result.IsSuccess,
                    result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error authenticating user: KeycloakId={KeycloakId}, Email={Email}",
                keycloakId, authInfo.Email);
            
            return StatusCode(500, new
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while authenticating the user"
            });
        }
    }
}
```

---

### 4. **AuthClient** (Server-Side Service Client)

**Location:** `Sivar.Os/Services/Clients/AuthClient.cs`

**Purpose:** Server-side implementation that calls `UserAuthenticationService`

```csharp
public class AuthClient : BaseRepositoryClient, IAuthClient
{
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly ILogger<AuthClient> _logger;

    public async Task<UserAuthenticationResult> AuthenticateUserAsync(
        string keycloakId, 
        UserAuthenticationInfo authInfo, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Authenticating user via client: KeycloakId={KeycloakId}, Email={Email}",
                keycloakId, authInfo.Email);

            var result = await _userAuthenticationService.AuthenticateUserAsync(keycloakId, authInfo);

            if (result.IsSuccess && result.IsNewUser)
            {
                _logger.LogInformation(
                    "New user created via client: UserId={UserId}, Email={Email}",
                    result.User?.Id, authInfo.Email);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error authenticating user via client: KeycloakId={KeycloakId}, Email={Email}",
                keycloakId, authInfo.Email);
            
            return new UserAuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while authenticating the user"
            };
        }
    }
}
```

---

### 5. **Client-Side AuthClient** (HTTP Implementation)

**Location:** `Sivar.Os.Client/Clients/AuthClient.cs`

**Purpose:** Makes HTTP POST request to server

```csharp
public class AuthClient : BaseClient, IAuthClient
{
    private const string BaseRoute = "api/auth";

    public async Task<UserAuthenticationResult> AuthenticateUserAsync(
        string keycloakId, 
        UserAuthenticationInfo authInfo, 
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<UserAuthenticationResult>(
            $"authentication/authenticate/{keycloakId}", 
            authInfo, 
            cancellationToken);
    }
}
```

---

### 6. **UsersController** & **ProfilesController** (Loading User Data)

**Endpoints Used by Home.razor:**

```csharp
// Get current user info
[HttpGet("me")]
public async Task<ActionResult<UserDto>> GetCurrentUser()
{
    var keycloakId = GetKeycloakIdFromRequest();
    if (string.IsNullOrEmpty(keycloakId))
        return Unauthorized("User not authenticated");

    var user = await _userService.GetCurrentUserAsync(keycloakId);
    
    if (user == null)
    {
        var newUserDto = CreateUserDtoFromKeycloakClaims();
        user = await _userService.GetOrCreateUserFromKeycloakAsync(newUserDto);
    }
    
    return Ok(user);
}

// Get active profile
[HttpGet("my/active")]
[Authorize]
public async Task<ActionResult<ProfileDto>> GetMyActiveProfile()
{
    var keycloakId = GetKeycloakIdFromRequest();
    if (string.IsNullOrEmpty(keycloakId))
        return Unauthorized("User not authenticated");

    var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
    
    if (profile == null)
        return NotFound("No active profile found");

    return Ok(profile);
}
```

---

## Key DTOs & Models

### `UserAuthenticationInfo`
```csharp
public class UserAuthenticationInfo
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Role { get; set; }
}
```

### `UserAuthenticationResult`
```csharp
public class UserAuthenticationResult
{
    public bool IsSuccess { get; set; }
    public bool IsNewUser { get; set; }
    public UserDto User { get; set; }
    public ProfileDto ActiveProfile { get; set; }
    public string ErrorMessage { get; set; }
}
```

### `UserDto`
```csharp
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; }
}
```

### `ProfileDto`
```csharp
public class ProfileDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public string Bio { get; set; }
    public string Avatar { get; set; }
    public VisibilityLevel VisibilityLevel { get; set; }
}
```

---

## Complete Flow Diagram

```
┌─────────────────────────────────────┐
│     User Logs In via Keycloak       │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  Home.razor page loads              │
│  OnInitializedAsync() called         │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  EnsureUserAndProfileCreatedAsync() │
│  ├─ Extract Keycloak claims         │
│  │  (sub, email, given_name, etc)  │
│  ├─ Create UserAuthenticationInfo   │
│  └─ Call SivarClient.Auth           │
│     .AuthenticateUserAsync()         │
└──────────────┬──────────────────────┘
               │
               ├──[HTTP POST]──────────────────────┐
               │                                   │
               ▼                                   │
    ┌──────────────────────────────┐              │
    │ Client AuthClient            │              │
    │ .AuthenticateUserAsync()      │              │
    └──────────────┬───────────────┘              │
                   │                              │
                   └──────────────┐────────────────┘
                                  │
                                  ▼
                    POST /authentication/authenticate/{keycloakId}
                                  │
                                  ▼
                    ┌─────────────────────────────────┐
                    │ AuthenticationController        │
                    │ .AuthenticateUser()             │
                    └──────────────┬──────────────────┘
                                   │
                                   ▼
                    ┌─────────────────────────────────┐
                    │ UserAuthenticationService       │
                    │ .AuthenticateUserAsync()        │
                    │ ├─ Check if user exists         │
                    │ ├─ If NEW:                      │
                    │ │  ├─ Create User               │
                    │ │  ├─ Create Default Profile    │
                    │ │  └─ Set as Active             │
                    │ └─ If EXISTS:                   │
                    │    └─ Get Active Profile        │
                    └──────────────┬──────────────────┘
                                   │
                    [UserAuthenticationResult]
                    {IsSuccess, IsNewUser, User, Profile}
                                   │
                                   ├─────────[Response]─────────┐
                                   │                            │
                                   ▼                            │
                    ┌─────────────────────────────────┐         │
                    │ Client AuthClient               │         │
                    │ receives UserAuthenticationResult           │
                    └──────────────┬──────────────────┘         │
                                   │                            │
                                   ▼                            │
                    ┌──────────────────────────────────────┐   │
                    │ Home.razor receives result           │   │
                    │ ├─ result.IsSuccess = true           │   │
                    │ ├─ result.IsNewUser = true/false     │   │
                    │ └─ User and Profile created/loaded   │   │
                    └──────────────┬─────────────────────────┘
                                   │
                                   ▼
                    ┌─────────────────────────────────┐
                    │ LoadCurrentUserAsync()          │
                    │ ├─ Call GetMeAsync()            │
                    │ └─ Call GetMyActiveProfileAsync()
                    └──────────────┬──────────────────┘
                                   │
                    ┌──────────────┴──────────────┐
                    │                             │
                    ▼                             ▼
        GET /api/users/me        GET /api/profiles/my/active
                    │                             │
                    ▼                             ▼
        ┌─────────────────────┐    ┌──────────────────────┐
        │ UserDto received    │    │ ProfileDto received  │
        │ - FirstName         │    │ - DisplayName        │
        │ - LastName          │    │ - Avatar             │
        │ - Email             │    │ - Bio                │
        └─────────────────────┘    └──────────────────────┘
                    │                             │
                    └──────────────┬──────────────┘
                                   │
                                   ▼
                    ┌─────────────────────────────────┐
                    │ Home.razor Header Updated       │
                    │ - Display user name             │
                    │ - Display email                 │
                    │ - Show avatar/initials          │
                    └─────────────────────────────────┘
```

---

## Implementation Checklist

Use this pattern for Post creation and other authenticated features:

- ✅ Extract Keycloak claims in controller using `GetKeycloakIdFromRequest()`
- ✅ Pass keycloakId to service layer
- ✅ Service layer queries database by keycloakId
- ✅ Returns DTO to controller
- ✅ Controller returns DTO to client
- ✅ Client displays/uses the data

---

## Common Issues & Solutions

### Issue: Keycloak ID not extracted
**Solution:** Use the fallback claim chain:
```csharp
var keycloakId = User.FindFirst("sub")?.Value
              ?? User.FindFirst("user_id")?.Value
              ?? User.FindFirst("id")?.Value
              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

### Issue: Profile not found after user creation
**Solution:** `UserAuthenticationService.CreateDefaultProfileAsync()` creates and sets active profile automatically.

### Issue: User loads but profile doesn't
**Solution:** Home.razor has try-catch to continue with user data even if profile fails to load.

---

## Summary

This pattern is **proven to work** and is currently used successfully in:
- Home.razor (User/Profile loading)
- Authentication flow (User/Profile creation)
- All controllers that need authenticated user context

**Key Success Factors:**
1. Extract Keycloak ID from JWT claims in controller
2. Pass keycloakId to service layer
3. Service layer handles all business logic
4. Return DTOs (not entities) to clients
5. Client stores and displays user information
