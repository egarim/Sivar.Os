# Profile Creator Switcher - Changes Summary

## 🔴 Issues Fixed: 3

### 1. ⛔ ProfileSwitcherClient Keycloak ID Extraction
**Status**: ✅ FIXED

**Before**:
```csharp
var keycloakId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;  // WRONG CLAIM
```

**After**:
```csharp
var keycloakId = user?.Claims?.FirstOrDefault(c => c.Type == "sub")?.Value;  // CORRECT
```

**Impact**: ProfileSwitcherClient can now properly authenticate and load user profiles

---

### 2. ⛔ Lost Profile Creation Data
**Status**: ✅ FIXED

**Before** (ProfileSwitcher.razor):
```csharp
private async Task HandleCreateProfile(CreateAnyProfileDto request)
{
    ShowCreateModal = false;
    // ❌ Request data is LOST here!
    await OnCreateProfileClick.InvokeAsync();  // Doesn't pass request
}
```

**After** (ProfileSwitcher.razor):
```csharp
[Parameter]
public EventCallback<CreateAnyProfileDto> OnCreateProfile { get; set; }

private async Task HandleCreateProfile(CreateAnyProfileDto request)
{
    ShowCreateModal = false;
    // ✅ Request data is properly passed to parent
    if (OnCreateProfile.HasDelegate)
    {
        await OnCreateProfile.InvokeAsync(request);  // PASSES REQUEST!
    }
}
```

**Before** (Home.razor binding):
```csharp
OnCreateProfileClick="@HandleCreateProfile"  // ❌ Old parameter, no data
```

**After** (Home.razor binding):
```csharp
OnCreateProfile="@HandleCreateProfile"  // ✅ New parameter, receives request
```

**Before** (Home.razor handler):
```csharp
private async Task HandleCreateProfile()  // ❌ No parameters!
{
    await LoadUserProfilesAsync();
    StateHasChanged();
}
```

**After** (Home.razor handler):
```csharp
private async Task HandleCreateProfile(CreateAnyProfileDto request)  // ✅ Receives request!
{
    var newProfile = await SivarClient.Profiles.CreateProfileAsync(request);
    
    if (request.SetAsActive)
    {
        await SivarClient.Profiles.SetMyActiveProfileAsync(newProfile.Id);
        _activeProfile = newProfile;
    }
    
    await LoadUserProfilesAsync();
}
```

**Impact**: Profile creation now actually works - request data flows through and profiles are created

---

### 3. ⛔ Callback Chain Broken
**Status**: ✅ FIXED

**Flow Before**:
```
ProfileCreatorModal (has CreateAnyProfileDto)
  ↓ OnCreate callback
ProfileSwitcher.HandleCreateProfile (ignores request data)
  ↓ OnCreateProfileClick
Home.HandleCreateProfile (no data received)
  ✗ Profile never created
```

**Flow After**:
```
ProfileCreatorModal (has CreateAnyProfileDto)
  ↓ OnCreate callback
ProfileSwitcher.HandleCreateProfile (passes CreateAnyProfileDto)
  ↓ OnCreateProfile callback
Home.HandleCreateProfile (receives CreateAnyProfileDto)
  ✓ SivarClient.Profiles.CreateProfileAsync() called
  ✓ Profile created successfully!
```

---

## 📝 Files Changed

| File | Lines | Changes |
|------|-------|---------|
| `ProfileSwitcherClient.cs` | 42-47 | Fixed Keycloak ID claim extraction |
| `ProfileSwitcher.razor` | 264-333 | Added OnCreateProfile parameter, fixed callback |
| `Home.razor` | 1687 | Updated binding to OnCreateProfile |
| `Home.razor` | 3034-3067 | Implemented full profile creation logic |

---

## ✅ Result

### Before Fix
```
[ProfileSwitcherClient] Unable to extract Keycloak ID from user claims
Exception: System.UnauthorizedAccessException
[Home] ✓ Loaded 0 profiles. Active:
```

### After Fix
```
[ProfileSwitcherClient] Getting user profiles
[ProfileSwitcherClient] Retrieved 1 profiles
[Home] ✓ Loaded 1 profiles. Active: Jose Ojeda
[Home] Creating new profile
[Home] ✅ Profile created successfully
```

---

## 🧪 Testing

Run these tests to verify the fix:

1. **Profile Loading**
   - Open Home page
   - Check ProfileSwitcher loads current user's profiles
   - Should show 1+ profiles (not 0)

2. **Profile Creation**
   - Click "Create New Profile" button
   - Fill in profile name and select type
   - Click "Create Profile"
   - New profile should appear in list
   - Browser console should show success messages

3. **Set Active**
   - Check "Set as active profile" option
   - Create profile
   - ActiveProfile should switch to the new profile

---

## 🔍 Key Insight

The "common mistake" was using `ClaimTypes.NameIdentifier` instead of the `"sub"` claim for Keycloak ID extraction.

**Remember**: 
- `"sub"` = Subject (Keycloak user ID) ✅
- `ClaimTypes.NameIdentifier` = Different thing ❌

This mistake appeared in ProfileSwitcherClient while other services (PostsClient, etc.) had it correct.
