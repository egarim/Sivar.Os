# Profile Switcher Endpoint Fix

## Issue Identified
The `ProfileSwitcherService` was calling **incorrect API endpoints** that didn't exist in the backend controller.

### Browser Console Error
```
Failed to load resource: the server responded with a status of 404 ()
[ProfileSwitcherService] Failed to get profiles: NotFound
```

## Root Cause
**Endpoint Mismatch:**
- Service was calling: `/api/profile/...` (singular)
- Controller route: `/api/profiles/...` (plural with different sub-routes)

## Fixed Endpoints

### Before → After

| Method | Before | After |
|--------|--------|-------|
| Get User Profiles | ❌ `/api/profile/my-profiles` | ✅ `/api/profiles/my/all` |
| Get Active Profile | ✅ `/api/profile/active` | ✅ `/api/profiles/my/active` |
| Switch Profile | ❌ `/api/profile/{id}/set-active` | ✅ `/api/profiles/{id}/set-active` |
| Create Profile | ❌ `/api/profile` | ✅ `/api/profiles` |
| Get Profile Types | ❌ `/api/profile-type` | ✅ `/api/profiletypes` |

## Files Modified

### 1. `Sivar.Os.Client/Services/ProfileSwitcherService.cs`
Updated all 5 endpoint calls to use correct API routes:
- `GetUserProfilesAsync()` - Now calls `/api/profiles/my/all` ✅
- `GetActiveProfileAsync()` - Now calls `/api/profiles/my/active` ✅
- `SwitchProfileAsync()` - Now calls `/api/profiles/{id}/set-active` ✅
- `CreateProfileAsync()` - Now calls `/api/profiles` ✅
- `GetProfileTypesAsync()` - Now calls `/api/profiletypes` ✅

### 2. Server-side: `ProfileSwitcherClient.cs`
No changes needed - uses services directly, not HTTP endpoints ✅

## Backend Controller Reference

**File:** `Sivar.Os/Controllers/ProfilesController.cs`  
**Route:** `[Route("api/[controller]")]` = `/api/profiles`

Endpoints:
- `GET /api/profiles/my/all` - GetMyProfiles()
- `GET /api/profiles/my/active` - GetMyActiveProfile()
- `PUT /api/profiles/{id}/set-active` - SetProfileAsActive()
- `POST /api/profiles` - CreateProfile()

**File:** `Sivar.Os/Controllers/ProfileTypesController.cs`  
**Route:** `[Route("api/[controller]")]` = `/api/profiletypes`

Endpoints:
- `GET /api/profiletypes` - GetActiveProfileTypes()

## Compilation Status
✅ **Zero errors** - All ProfileSwitcher code compiles successfully  
(Only pre-existing unused method warning in Program.cs, unrelated)

## Testing Instructions

1. **Clear browser cache** (Ctrl+Shift+Delete)
2. **Refresh page** (F5 or Ctrl+R)
3. **Open DevTools** (F12)
4. **Check Console tab** - No 404 errors should appear
5. **Click Profile Switcher dropdown** - Should load profiles successfully
6. **Try creating a new profile** - Should work without 404 errors

## Expected Behavior After Fix

✅ ProfileSwitcher dropdown loads user's profiles  
✅ Profile switching works  
✅ New profile creation submits successfully  
✅ Profile types load in creation form  
✅ No 404 errors in browser console  

## Success Indicators in Console
Look for these messages (client-side WASM logging):
```
[ProfileSwitcherService] Getting user profiles
[ProfileSwitcherService] Retrieved X profiles
[ProfileSwitcherService] Creating new profile
[ProfileSwitcherService] Successfully created profile: {profileId}
```

## Date Fixed
October 28, 2025

## Related Issues
- Fixed by correcting endpoint routes from `/api/profile/*` to `/api/profiles/*/`
- Ensures ProfileSwitcher component can communicate with backend
- Resolves 404 Not Found errors blocking profile management functionality
