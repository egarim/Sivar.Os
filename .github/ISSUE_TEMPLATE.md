# Auto-create User and Profile after Keycloak Login

## Problem
After a successful Keycloak authentication, users need to be automatically created in our PostgreSQL database along with a default profile. Currently, the implementation in `OnTokenValidated` event is not executing reliably.

## Current Implementation (Not Working Reliably)
Currently using `OnTokenValidated` event in `Program.cs`:

```csharp
OnTokenValidated = async context =>
{
    // Extract claims from Keycloak token
    var keycloakId = context.Principal?.FindFirst("sub")?.Value;
    var email = context.Principal?.FindFirst("email")?.Value;
    // ... create user and profile via IUserAuthenticationService
}
```

**Location**: `Sivar.Os/Program.cs` lines ~230-285

## Issues Observed
1. Event may not fire consistently during authentication flow
2. Debugger shows MONO runtime crashes when breakpoints are set
3. No log output confirming execution during login
4. Potential service scope/DI issues during middleware execution

## Solution - Add Fallback in Home Page Component

We will implement a **fallback approach** in the Home page that ensures user/profile creation after login:

### Implementation Plan

1. **Keep existing `OnTokenValidated` implementation** - This will continue to try creating users during the auth flow
2. **Add fallback check in `Home.razor`** - On component initialization, verify user/profile exist and create if missing
3. **Leverage existing `AuthenticateUserAsync`** - This method already handles duplicate prevention gracefully

### Code Changes

**File**: `Sivar.Os.Client/Pages/Home.razor`

Add the following to the Home page component:

```csharp
@page "/"
@inject AuthenticationStateProvider AuthStateProvider
@inject HttpClient Http
@using Sivar.Os.Shared.Services

<PageTitle>Home</PageTitle>

@code {
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        // Ensure user and profile exist in database after Keycloak login
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        if (user.Identity?.IsAuthenticated == true)
        {
            var keycloakId = user.FindFirst("sub")?.Value;
            var email = user.FindFirst("email")?.Value 
                     ?? user.FindFirst("preferred_username")?.Value;
            var firstName = user.FindFirst("given_name")?.Value ?? "";
            var lastName = user.FindFirst("family_name")?.Value ?? "";
            
            if (!string.IsNullOrEmpty(keycloakId))
            {
                try
                {
                    // Call backend to ensure user/profile exist
                    // The AuthenticateUserAsync endpoint handles both new and existing users
                    var authInfo = new UserAuthenticationInfo
                    {
                        Email = email ?? "",
                        FirstName = firstName,
                        LastName = lastName,
                        Role = "RegisteredUser"
                    };
                    
                    var response = await Http.PostAsJsonAsync(
                        $"/api/authentication/authenticate/{keycloakId}", 
                        authInfo);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<UserAuthenticationResult>();
                        // User and profile are now guaranteed to exist
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't prevent page load
                    Console.WriteLine($"Error ensuring user exists: {ex.Message}");
                }
            }
        }
    }
}
```

### Backend API Endpoint

**File**: `Sivar.Os/Controllers/AuthenticationController.cs`

Add new endpoint:

```csharp
[HttpPost("authenticate/{keycloakId}")]
public async Task<ActionResult<UserAuthenticationResult>> AuthenticateUser(
    string keycloakId, 
    [FromBody] UserAuthenticationInfo authInfo)
{
    _logger.LogInformation("Authenticate endpoint called for Keycloak ID: {KeycloakId}", keycloakId);
    
    var result = await _userAuthenticationService.AuthenticateUserAsync(keycloakId, authInfo);
    
    if (result.IsSuccess)
    {
        return Ok(result);
    }
    
    return BadRequest(result);
}
```

## Why This Solution Works

1. **Defense in depth** - Two places trying to create user (OnTokenValidated + Home page)
2. **Idempotent** - `AuthenticateUserAsync` handles duplicate calls gracefully
3. **User-visible** - Runs in Blazor component with proper DI scope
4. **Debuggable** - Can set breakpoints and see execution
5. **Reliable** - Guaranteed to run when authenticated user loads home page

## Files to Modify
- [x] `Sivar.Os.Client/Pages/Home.razor` - Add user creation check in OnInitializedAsync
- [x] `Sivar.Os/Controllers/AuthenticationController.cs` - Add authenticate endpoint
- [ ] Test with new user login
- [ ] Test with existing user login
- [ ] Verify no duplicate users created

## Testing Steps
1. Clear database (delete test user and profile records)
2. Start application
3. Login with Keycloak credentials
4. Should redirect to home page
5. Check browser console for any errors
6. Verify in database:
   - User record exists with correct KeycloakId
   - Profile record exists for that user
7. Logout and login again
8. Verify no duplicate records created

## Success Criteria
- ✅ New users automatically get User + Profile records created
- ✅ Existing users don't get duplicated
- ✅ Works reliably regardless of OnTokenValidated execution
- ✅ No errors in console or logs
- ✅ User can proceed to use application normally

---

**Priority**: High - blocks user registration flow
**Effort**: 1-2 hours including testing
**Risk**: Low - leverages existing tested `AuthenticateUserAsync` method
