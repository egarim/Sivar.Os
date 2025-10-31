# Console Logging Reference - What to Expect

When you run the application with the latest fixes, you should see these console logs:

## 1. Authentication Phase (On App Load)

```javascript
[WasmAuthStateProvider] GetAuthenticationStateAsync called (call #1)
[WasmAuthStateProvider] Call #1: Fetch returned profile data
[WasmAuthStateProvider] Call #1: Parsed isAuth=True
[WasmAuthStateProvider] Found 12 claims in profile
[WasmAuthStateProvider] Call #1: Claim: sub=28b46a88-d191-4c63-8812-1bb8f3332228
[WasmAuthStateProvider] Call #1: Claim: email=joche@joche.com
[WasmAuthStateProvider] Call #1: Claim: name=Jose Ojeda
[WasmAuthStateProvider] Call #1: State CHANGED! Notifying subscribers...
```

✅ **What it means**: User authenticated successfully. The "sub" claim contains the Keycloak ID.

---

## 2. Home Initialization Phase

```javascript
[Home] ==================== OnInitializedAsync START ====================
[Home] Step 1: Ensuring user and profile are created
[Home.EnsureUserAndProfileCreatedAsync] START
[Home] Extracted - Keycloak ID: 28b46a88-d191-4c63-8812-1bb8f3332228, Email: joche@joche.com, First Name: Jose, Last Name: Ojeda
[Home.EnsureUserAndProfileCreatedAsync] ✓ Existing user authenticated
[Home] Step 2: Loading current user info
[HOME-CLIENT] ✅ User DTO Received:
[HOME-CLIENT]   - ID: dde085dd-1750-4586-b9b4-a7f92c43041f
[HOME-CLIENT] ✅ Active Profile DTO Received:
[HOME-CLIENT]   - DisplayName: Jose Ojeda
[Home] Step 2.5: Loading user profiles
[Home] ✓ Loaded 1 profiles. Active: Jose Ojeda
[Home] Step 3: Loading feed posts
[Home] ✓ Successfully loaded 10 posts
[Home] Step 4: Loading user statistics
[Home] ==================== OnInitializedAsync END - Posts loaded: 10 ====================
```

✅ **What it means**: Home page initialized, user loaded, active profile set, posts loaded.

---

## 3. Profile Types Loading (When Modal Opens)

```javascript
info: Sivar.Os.Client.Services.ProfileSwitcherService[0]
      [ProfileSwitcherService] Getting profile types
info: Sivar.Os.Client.Services.ProfileSwitcherService[0]
      [ProfileSwitcherService] Retrieved 3 profile types
```

✅ **What it means**: Profile types successfully fetched from server. 3 types available.

---

## 4. Modal Initialization (First Time)

```javascript
[ProfileCreatorModal] InitializeProfileTypes: Loaded 3 profile types
  - Personal Profile (ID: 11111111-1111-1111-1111-111111111111)
  - Business Profile (ID: 22222222-2222-2222-2222-222222222222)
  - Organization Profile (ID: 33333333-3333-3333-3333-333333333333)
[ProfileCreatorModal] OnInitializedAsync: Set SelectedProfileType to Personal Profile (ID: 11111111-1111-1111-1111-111111111111)
```

✅ **What it means**: 
- Modal loaded 3 real profile types from server
- First one (Personal) was auto-selected
- IDs match database seeding

---

## 5. User Selects Profile Type

When user clicks on "Business Profile" option:

```javascript
[ProfileCreatorModal] SelectProfileType: Selected Business Profile (ID: 22222222-2222-2222-2222-222222222222)
```

✅ **What it means**: User selected Business profile type. This ID will be sent to the API.

---

## 6. User Submits Form

```javascript
[ProfileCreatorModal.SubmitForm] Creating profile: Name=BBBB, Type=Business Profile (ID: 22222222-2222-2222-2222-222222222222)
[Home] Creating new profile
[Home] Profile request: DisplayName=BBBB, ProfileTypeId=22222222-2222-2222-2222-222222222222, SetAsActive=False, Visibility=Public
```

✅ **What it means**: 
- Modal submitted form with all data
- Home received the request with ProfileTypeId
- Request contains all required fields

---

## 7. API Call in Progress

```javascript
:5001/api/profiles:1   Failed to load resource: the server responded with a status of 400 ()
```

This can mean:
- ❌ **Duplicate profile type**: User already has a profile of this type (Server validation working!)
- ❌ **Invalid ProfileTypeId**: Server couldn't find the profile type
- ✅ **Other validation errors**: Business logic preventing creation

---

## 8. Success Response (If Creating Different Type)

```javascript
[Home] ✅ Profile created successfully: <new-profile-id>
[Home] Setting new profile as active
[Home] Reloading profiles...
```

✅ **What it means**: Profile successfully created in database and is now active.

---

## 9. Error Response (Duplicate Type)

```javascript
[BaseClient] API Error: BadRequest Bad Request
[BaseClient] Response Content: {"errors":["User already has a profile of this type"]}
[Home] ❌ Error creating profile: API call failed with status 400 (BadRequest): Bad Request
```

✅ **What it means**: Server correctly rejected duplicate profile type. This is GOOD - it shows validation is working!

---

## 10. Modal Reset (When Reopening)

```javascript
[ProfileCreatorModal] OnParametersSetAsync: Modal opened
[ProfileCreatorModal] OnInitializedAsync: Set SelectedProfileType to Personal Profile (ID: 11111111-1111-1111-1111-111111111111)
```

✅ **What it means**: Modal reset and first profile type re-selected when opened again.

---

## How to View These Logs

1. **Open Browser Developer Tools**: Press `F12`
2. **Navigate to Console tab**: Click "Console"
3. **Look for lines starting with**: `[`, `info:`, `:5001`, etc.
4. **Filter logs**: Type any text in the filter box (e.g., `ProfileCreatorModal`)

---

## Troubleshooting Guide

### Problem: No logs appearing
**Solution**: Check if console filter is hiding them. Clear the filter search box.

### Problem: ProfileTypeId shows Guid.Empty
**Solution**: Modal ProfileTypes didn't load. Check if server is responding to `/api/profiletypes` endpoint.

### Problem: Wrong ProfileTypeId in request
**Solution**: User didn't properly select profile type. Check SelectProfileType logs.

### Problem: Always getting "User already has profile" error
**Solution**: Try a different profile type that you don't already have.

---

## Summary

The logging lets you track:
1. ✅ Authentication (Keycloak claims extraction)
2. ✅ Profile loading (how many profiles user has)
3. ✅ Profile types retrieval (3 types from server)
4. ✅ Modal initialization (ProfileTypes loaded with correct IDs)
5. ✅ User selection (which type selected)
6. ✅ Form submission (all data being sent)
7. ✅ API response (success or error with details)

Each log message tells you exactly what's happening at that step, making debugging and verification easy!

🔍 **Use these logs to verify each fix is working correctly.**
