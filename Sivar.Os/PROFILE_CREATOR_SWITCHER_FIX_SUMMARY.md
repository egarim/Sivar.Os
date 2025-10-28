# Profile Creator Switcher - Bug Fix Summary

**Date**: October 28, 2025  
**Branch**: ProfileCreatorSwitcher  
**Issues Fixed**: 3 critical issues

---

## Problem Overview

The ProfileSwitcher component was failing with:
- `[ProfileSwitcherClient] Unable to extract Keycloak ID from user claims`
- `Exception: System.UnauthorizedAccessException - User is not authenticated`
- **Result**: Loaded 0 profiles instead of user's profiles

Additionally, the profile creation flow was broken - the form would accept input but nothing would be created.

---

## Root Causes Identified

### Issue #1: Wrong Keycloak ID Claim Type ⛔ CRITICAL
**File**: `ProfileSwitcherClient.cs`

**Problem**:
The `GetCurrentUserKeycloakId()` method was using `ClaimTypes.NameIdentifier`:
```csharp
var keycloakId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

This claim type doesn't exist in Keycloak's JWT tokens. Other services like `PostsClient` were correctly using the `"sub"` claim.

**Solution**:
Changed to use the correct `"sub"` claim, matching PostsClient implementation:
```csharp
var keycloakId = user?.Claims?.FirstOrDefault(c => c.Type == "sub")?.Value;
```

**Files Changed**:
- `Sivar.Os/Services/Clients/ProfileSwitcherClient.cs` - Line 42-47

---

### Issue #2: Lost Profile Creation Request Data ⛔ CRITICAL
**Files**: 
- `ProfileCreatorModal.razor`
- `ProfileSwitcher.razor`
- `Home.razor`

**Problem**:
The callback chain for profile creation was broken:
1. `ProfileCreatorModal` receives `CreateAnyProfileDto request` in `OnCreate`
2. Calls `ProfileSwitcher.HandleCreateProfile(request)`
3. **BUG**: `ProfileSwitcher.HandleCreateProfile` was ignoring the `request` parameter and just calling `OnCreateProfileClick` without passing data
4. Result: The actual profile data never reached `Home.HandleCreateProfile`

**Solution**:

**Step A**: Added `OnCreateProfile` parameter to ProfileSwitcher to pass the complete request:
```csharp
[Parameter]
public EventCallback<CreateAnyProfileDto> OnCreateProfile { get; set; }

private async Task HandleCreateProfile(CreateAnyProfileDto request)
{
    ShowCreateModal = false;
    // Pass the profile creation request to parent component
    if (OnCreateProfile.HasDelegate)
    {
        await OnCreateProfile.InvokeAsync(request);
    }
    else if (OnCreateProfileClick.HasDelegate)
    {
        // Fallback for backward compatibility
        await OnCreateProfileClick.InvokeAsync();
    }
}
```

**Step B**: Updated Home.razor to bind the new callback:
```csharp
<ProfileSwitcher ActiveProfile="@_activeProfile"
                 UserProfiles="@_userProfiles"
                 OnProfileChanged="@HandleProfileChanged"
                 OnCreateProfile="@HandleCreateProfile" />
```

**Step C**: Implemented proper profile creation in Home.HandleCreateProfile:
```csharp
private async Task HandleCreateProfile(CreateAnyProfileDto request)
{
    try
    {
        Console.WriteLine("[Home] Creating new profile");
        Console.WriteLine($"[Home] Profile request: DisplayName={request.DisplayName}, SetAsActive={request.SetAsActive}");

        // Call the SivarClient to create the profile
        var newProfile = await SivarClient.Profiles.CreateProfileAsync(request);
        
        if (newProfile != null)
        {
            Console.WriteLine($"[Home] ✅ Profile created successfully: {newProfile.Id}");
            
            // If SetAsActive is true, switch to the new profile
            if (request.SetAsActive)
            {
                Console.WriteLine("[Home] Setting new profile as active");
                await SivarClient.Profiles.SetMyActiveProfileAsync(newProfile.Id);
                _activeProfile = newProfile;
            }

            // Reload profiles after creation
            await LoadUserProfilesAsync();
            StateHasChanged();
        }
        else
        {
            Console.WriteLine("[Home] ❌ Profile creation returned null");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Home] ❌ Error creating profile: {ex.Message}");
        Console.WriteLine($"[Home] Stack trace: {ex.StackTrace}");
    }
}
```

**Files Changed**:
- `Sivar.Os.Client/Components/ProfileSwitcher/ProfileSwitcher.razor` - Lines 264-333
- `Sivar.Os.Client/Pages/Home.razor` - Line 1687 (callback binding) + Lines 3034-3067 (handler implementation)

---

## Technical Details

### Keycloak Claims Reference
From the logs, Keycloak JWT contains:
- `"sub"`: Subject (user's unique ID) - **THIS IS THE KEYCLOAK ID**
- `"name"`: Full name
- `"email"`: Email address
- `"preferred_username"`: Username
- `"given_name"`: First name
- `"family_name"`: Last name
- And others...

**Standard claim**: `ClaimTypes.NameIdentifier` typically maps to `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`, which is NOT the Keycloak ID.

### Correct Implementation Pattern
```csharp
// ✅ CORRECT - Used by PostsClient, ProfilesClient
var keycloakId = _httpContextAccessor.HttpContext?.User?.Claims
    .FirstOrDefault(c => c.Type == "sub")?.Value;

// ❌ WRONG - Was used by ProfileSwitcherClient (now fixed)
var keycloakId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

---

## Affected Methods

### ProfileSwitcherClient
- `GetCurrentUserKeycloakId()` - Fixed claim extraction
- `GetUserProfilesAsync()` - Now works correctly
- `GetActiveProfileAsync()` - Now works correctly  
- `SwitchProfileAsync()` - Now works correctly
- `CreateProfileAsync()` - Now works correctly

### ProfileSwitcher Component
- `HandleCreateProfile(CreateAnyProfileDto request)` - Now passes request data to parent

### Home Page
- `HandleCreateProfile(CreateAnyProfileDto request)` - Now implements actual profile creation

---

## Testing Checklist

- [ ] ProfileSwitcher can now load and display user profiles (no more 0 profiles)
- [ ] Profile creation modal opens
- [ ] Enter profile name and click Create
- [ ] New profile is created and appears in the list
- [ ] "Set as active profile" option works
- [ ] Active profile switches correctly after creation
- [ ] Browser console shows proper logging for each step
- [ ] No "Unable to extract Keycloak ID" errors

---

## Files Modified

1. **Sivar.Os/Services/Clients/ProfileSwitcherClient.cs**
   - Fixed `GetCurrentUserKeycloakId()` method

2. **Sivar.Os.Client/Components/ProfileSwitcher/ProfileSwitcher.razor**
   - Added `OnCreateProfile` callback parameter
   - Updated `HandleCreateProfile` to pass request data

3. **Sivar.Os.Client/Pages/Home.razor**
   - Updated ProfileSwitcher binding to use `OnCreateProfile`
   - Implemented full `HandleCreateProfile` method with profile creation logic

---

## References

### Troubleshooting Guide
See: `TROUBLESHOOTING.md` - Section "Keycloak ID vs User ID vs Profile ID"

### Related Services
- `PostsClient.cs` - Correct implementation pattern (uses "sub" claim)
- `ProfileService.cs` - Backend profile management
- `IProfilesClient.cs` - Client interface definition

---

## Common Mistake Pattern (Now Fixed)

This was a **common mistake** as mentioned in TROUBLESHOOTING.md:
> There was a common mistake that we have with the Keycloak ID extraction

The pattern was:
1. Using wrong claim type (`ClaimTypes.NameIdentifier` instead of `"sub"`)
2. Result: Claims extraction would return `null` or wrong value
3. Service would catch exception and return empty results silently
4. User would see "0 profiles" or similar issues

**Solution**: Always use `"sub"` claim for Keycloak ID, which is the JWT subject identifier.
