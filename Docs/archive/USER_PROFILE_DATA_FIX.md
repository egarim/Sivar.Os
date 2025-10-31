# User Profile Data Fix - Console Analysis

## Problem Identified

The console output revealed that user and profile data was being returned empty:

```
[HOME-CLIENT] ✅ User DTO Received:
[HOME-CLIENT]   - ID: 9bd777a4-fdb7-42de-8210-fea9c2ca55aa
[HOME-CLIENT]   - First Name:                          ← EMPTY!
[HOME-CLIENT]   - Last Name:                           ← EMPTY!
[HOME-CLIENT]   - Email: user@example.com             ← Has generic fallback value
[HOME-CLIENT] - Full Name:                            ← EMPTY!

[HOME-CLIENT] ✅ Active Profile DTO Received:
[HOME-CLIENT]   - ID: 0c12973e-9dde-41b1-8e41-bbbb7e87afc3
[HOME-CLIENT]   - DisplayName:                         ← EMPTY!
[HOME-CLIENT]   - Avatar:                             ← EMPTY!
[HOME-CLIENT]   - LocationDisplay:                    ← EMPTY!
```

However, the backend log shows:
```
[Home] Existing user authenticated: joche@joche.com
```

This indicated the user **IS** being created/found in the database, but the DTOs returned are missing the string fields.

## Root Cause

The **UsersController** had a hardcoded mock Keycloak ID:

```csharp
private string GetKeycloakIdFromRequest()
{
    // Returns mock value - NOT the real Keycloak ID!
    return "mock-keycloak-user-id";
}
```

When this mock ID was passed to the UserService:
1. `GetCurrentUserAsync("mock-keycloak-user-id")` looked for a user with that mock ID
2. It couldn't find a user with that ID in the database
3. It returned `null` OR returned a user with no FirstName/LastName data

Meanwhile, the actual user in the database had:
- Real Keycloak ID (from JWT token)
- Email: "joche@joche.com"
- FirstName & LastName populated

## Solution Applied

Updated `UsersController.cs` to properly extract the Keycloak ID from the JWT Bearer token:

### 1. Updated GetKeycloakIdFromRequest()

```csharp
private string GetKeycloakIdFromRequest()
{
    // First check for mock header (testing)
    if (Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
    {
        return keycloakIdHeader.ToString();
    }

    // Extract from JWT token - try multiple claim types
    if (User?.Identity?.IsAuthenticated == true)
    {
        var keycloakIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value           // Standard OIDC subject claim
                           ?? User.FindFirst("user_id")?.Value;       // Alternative claim
        
        if (!string.IsNullOrEmpty(keycloakIdClaim))
        {
            _logger.LogInformation($"[UsersController] Extracted Keycloak ID from JWT: {keycloakIdClaim}");
            return keycloakIdClaim;
        }
    }

    // Fallback only if X-Mock-Auth header is present (test scenario)
    if (Request.Headers.ContainsKey("X-Mock-Auth"))
    {
        return "mock-keycloak-user-id";
    }

    return null!;
}
```

### 2. Updated CreateUserDtoFromKeycloakClaims()

```csharp
private CreateUserFromKeycloakDto CreateUserDtoFromKeycloakClaims()
{
    if (User?.Identity?.IsAuthenticated != true)
    {
        // Fallback for unauthenticated - return safe defaults
        return new CreateUserFromKeycloakDto { ... };
    }

    // Extract all claims from JWT token
    var keycloakId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value
                  ?? User.FindFirst("user_id")?.Value
                  ?? "unknown-id";

    var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
             ?? User.FindFirst("email")?.Value
             ?? User.FindFirst("preferred_username")?.Value
             ?? "user@example.com";

    var firstName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value
                 ?? User.FindFirst("given_name")?.Value
                 ?? User.FindFirst("firstName")?.Value
                 ?? "";

    var lastName = User.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value
                ?? User.FindFirst("family_name")?.Value
                ?? User.FindFirst("lastName")?.Value
                ?? "";

    _logger.LogInformation($"[UsersController] Creating user from Keycloak claims: ...");

    return new CreateUserFromKeycloakDto
    {
        KeycloakId = keycloakId,
        Email = email,
        FirstName = firstName,
        LastName = lastName,
        Role = UserRole.RegisteredUser,
        PreferredLanguage = "en"
    };
}
```

## Expected Behavior After Fix

1. **Client logs in via Keycloak** → JWT token with user claims is issued
2. **Client sends requests** → JWT token includes claims like:
   - `sub` (Keycloak ID)
   - `email`
   - `given_name` (FirstName)
   - `family_name` (LastName)
3. **UsersController receives request** → Extracts real Keycloak ID from JWT
4. **UserService looks up user** → Finds user with correct ID and all data
5. **ProfilesController receives request** → Also extracts real Keycloak ID from JWT
6. **ProfileService looks up profile** → Finds profile with correct DisplayName, Avatar, etc.
7. **Client receives DTOs** → All string fields populated correctly
8. **Header displays user info** → Shows "joche" or profile display name + email

## Testing

To verify the fix works:
1. Check browser DevTools Network tab → JWT token in Authorization header
2. Open browser console → Look for log messages:
   - `[UsersController] Extracted Keycloak ID from JWT: ...`
   - `[UsersController] Creating user from Keycloak claims: ...`
3. Verify console shows populated user data:
   - FirstName should match JWT claim
   - LastName should match JWT claim
   - Email should match JWT claim
4. Header should display actual user name and email (not placeholder "Jordan Doe")

## Files Modified

1. **c:\Users\joche\source\repos\SivarOs\Sivar.Os\Sivar.Os\Controllers\UsersController.cs**
   - Updated `GetKeycloakIdFromRequest()` method
   - Updated `CreateUserDtoFromKeycloakClaims()` method
