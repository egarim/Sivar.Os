# Profile Creation Diagnostic Guide 🔍

## Quick Diagnosis Checklist

When you try to create a profile and it fails, check your browser console for these specific log messages in this order:

### 1️⃣ **Modal Opens** 
Look for:
```
[ProfileCreatorModal] InitializeProfileTypes: Loaded X profile types
```
- ✅ **If you see this**: Profile types loaded correctly
- ❌ **If missing**: Profile types API call failed - check `/api/profiletypes` endpoint

### 2️⃣ **Click Create Button**
Look for:
```
═══════════════════════════════════════════════════════════════
[ProfileCreatorModal.SubmitForm] START - Submitting create profile form
```
- ✅ **If you see this**: Button click is working
- ❌ **If missing**: JavaScript/Blazor binding issue - button click not triggering

### 3️⃣ **Form Validation**
Look for:
```
[ProfileCreatorModal.SubmitForm] ✅ Form validation passed, starting submission
```
OR
```
[ProfileCreatorModal.SubmitForm] ❌ Form validation failed
  ProfileName: 'XXX' (Length: X)
  SelectedProfileType: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
```
- ✅ **Validation passed**: Form is valid, proceeding
- ❌ **Validation failed**: Check which field is invalid

### 4️⃣ **Request Created**
Look for:
```
[ProfileCreatorModal.SubmitForm] Creating request object
  Name: YourProfileName
  Type: Business Profile (ID: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX)
  SetAsActive: True
```
- ✅ **If you see this**: Request object created successfully

### 5️⃣ **Callback Invoked**
Look for:
```
[ProfileCreatorModal.SubmitForm] Invoking OnCreate callback
```
THEN
```
---------------------------------------------------------------
[Home.HandleCreateProfile] START - Creating new profile
  DisplayName: YourProfileName
  ProfileTypeId: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
```
- ✅ **If you see both**: Modal → Home communication working
- ❌ **If only first**: Home.HandleCreateProfile not receiving the callback

### 6️⃣ **Client-Side API Call**
Look for:
```
[ProfilesClient.CreateProfileAsync] Starting profile creation API call
```
THEN ONE OF:
```
[ProfilesClient.CreateProfileAsync] ✅ API returned successfully
```
OR
```
[ProfilesClient.CreateProfileAsync] ❌ API call failed!
  Status Code: XXX
  Response: {...}
```
- ✅ **Success**: API call worked
- ❌ **Failed**: Check the status code and response

### 7️⃣ **Server-Side Processing** (check server logs)
Look for:
```
[ProfileService] Creating profile for user...
```
AND
```
[ProfileService] Profile created successfully: ID=XXXXXXXX
```

### 8️⃣ **Back to Home - Profile Created**
Look for:
```
[Home.HandleCreateProfile] STEP 2: ✅ Profile created successfully!
  Profile ID: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
  Profile Name: YourProfileName
```

### 9️⃣ **Set As Active** (if SetAsActive = true)
Look for:
```
[Home.HandleCreateProfile] STEP 3: SetAsActive=True, attempting to auto-select
[Home.HandleCreateProfile] STEP 3: ✅ SetMyActiveProfileAsync succeeded!
```
OR
```
[Home.HandleCreateProfile] STEP 3: ❌ SetMyActiveProfileAsync failed!
```

### 🔟 **Feed Reload**
Look for:
```
[Home.HandleCreateProfile] STEP 6: Loading feed for new profile
[Home.LoadFeedActivitiesAsync] START
```

## Common Issues & Solutions

### Issue 1: Button doesn't work (no logs at all)
**Symptoms:** Click button, nothing happens, no console logs

**Check:**
1. Is `IsSubmitting` stuck as `true`? 
2. JavaScript errors in console?
3. Is modal actually open? (`IsOpen = true`)

**Solution:**
```csharp
// Add this to ProfileCreatorModal.razor @code section
protected override void OnParametersSetAsync()
{
    if (IsOpen)
    {
        IsSubmitting = false; // Reset on open
    }
}
```

---

### Issue 2: Validation fails - Empty ProfileName
**Symptoms:** 
```
[ProfileCreatorModal.SubmitForm] ❌ Form validation failed
  ProfileName: '' (Length: 0)
```

**Cause:** `@bind-Value="ProfileName"` not binding correctly

**Solution:** Check MudTextField binding and make sure you're typing in the field

---

### Issue 3: Validation fails - Empty ProfileType
**Symptoms:**
```
[ProfileCreatorModal.SubmitForm] ❌ Form validation failed
  SelectedProfileType: 00000000-0000-0000-0000-000000000000
```

**Cause:** Profile types didn't load or selection isn't working

**Check:**
1. Did profile types load? Look for `[ProfileCreatorModal] InitializeProfileTypes`
2. Is a profile type highlighted in blue in the UI?

**Solution:** Make sure `/api/profiletypes` endpoint is working

---

### Issue 4: API call fails (400 Bad Request)
**Symptoms:**
```
[ProfilesClient.CreateProfileAsync] ❌ API call failed!
  Status Code: 400
  Response: {"errors": ["..."]}
```

**Cause:** Server-side validation failed

**Check server logs for:**
- `[ProfileService] Validation failed: ...`
- Business rules violations (e.g., "User already has 3 business profiles")

---

### Issue 5: API call fails (401 Unauthorized)
**Symptoms:**
```
[ProfilesClient.CreateProfileAsync] ❌ API call failed!
  Status Code: 401
```

**Cause:** User not authenticated or token expired

**Solution:**
1. Check if logged in to Keycloak
2. Try logging out and back in
3. Check browser's Application → Cookies for auth cookies

---

### Issue 6: API call fails (500 Internal Server Error)
**Symptoms:**
```
[ProfilesClient.CreateProfileAsync] ❌ API call failed!
  Status Code: 500
```

**Cause:** Server-side exception

**Check server logs for:**
- Database connection issues
- Foreign key violations
- Null reference exceptions
- ProfileType not found in database

**Common Server Errors:**
1. **ProfileType doesn't exist in database**
   ```sql
   SELECT * FROM ProfileTypes; -- Check if types exist
   ```

2. **User not found in database**
   - Make sure user was created during authentication
   - Check `Users` table for your Keycloak ID

---

### Issue 7: Profile created but not showing
**Symptoms:**
- Console shows success
- Feed doesn't update
- Profile list doesn't show new profile

**Check:**
```
[Home.HandleCreateProfile] STEP 7: Reloading user profiles
[Home.LoadFeedActivitiesAsync] START
```

**Cause:** UI refresh failed

**Solution:** Force refresh with F5 or check `StateHasChanged()` calls

---

### Issue 8: SetAsActive fails
**Symptoms:**
```
[Home.HandleCreateProfile] STEP 3: ❌ SetMyActiveProfileAsync failed!
  Exception: InvalidOperationException
```

**Cause:** Profile exists but can't be set as active

**Check:**
- Is the profile owned by the current user?
- Profile service validation rules

---

## Step-by-Step Testing Procedure

### Test 1: Check Profile Types API
1. Open browser to: `https://localhost:5001/api/profiletypes`
2. Should return JSON array of profile types
3. Example response:
```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "name": "Personal",
    "displayName": "Personal Profile",
    "description": "For individuals"
  }
]
```

### Test 2: Check Authentication
1. Open browser console
2. Type: `document.cookie`
3. Should see cookies with authentication info

### Test 3: Create Profile with Minimal Data
1. Open modal
2. Enter name: "Test123"
3. Select any profile type
4. Click Create
5. Watch console for all 10 steps listed above

### Test 4: Check Database After Creation
```sql
-- Check if profile was created
SELECT TOP 1 * FROM Profiles ORDER BY CreatedAt DESC;

-- Check if it's marked as active
SELECT * FROM Profiles WHERE UserId = 'YOUR_USER_ID' AND IsActive = 1;
```

---

## Log Collection Script

If you need to share logs, use this:

1. Open browser console
2. Right-click in console
3. "Save as..." → save to file
4. Or copy all logs and paste into a text file

Filter logs for profile creation only:
```javascript
// In browser console
console.log("=== PROFILE CREATION LOGS ===");
// Then try to create profile
// All relevant logs will be together
```

---

## Expected Full Success Log

Here's what a COMPLETE SUCCESSFUL profile creation looks like:

```
[ProfileCreatorModal] InitializeProfileTypes: Loaded 3 profile types
  - Personal Profile (ID: 11111111-1111-1111-1111-111111111111)
  - Business Profile (ID: 22222222-2222-2222-2222-222222222222)
  - Brand Profile (ID: 33333333-3333-3333-3333-333333333333)

═══════════════════════════════════════════════════════════════
[ProfileCreatorModal.SubmitForm] START - Submitting create profile form
[ProfileCreatorModal.SubmitForm] ✅ Form validation passed, starting submission
[ProfileCreatorModal.SubmitForm] Creating request object
  Name: My Business
  Type: Business Profile (ID: 22222222-2222-2222-2222-222222222222)
  Bio: A great business
  Visibility: Public
  SetAsActive: True
[ProfileCreatorModal.SubmitForm] Invoking OnCreate callback

---------------------------------------------------------------
[Home.HandleCreateProfile] START - Creating new profile
  DisplayName: My Business
  ProfileTypeId: 22222222-2222-2222-2222-222222222222
  SetAsActive: True
[Home.HandleCreateProfile] STEP 1: Calling SivarClient.Profiles.CreateProfileAsync()

[ProfilesClient.CreateProfileAsync] Starting profile creation API call
[ProfilesClient.CreateProfileAsync] ✅ API returned successfully

[Home.HandleCreateProfile] STEP 2: ✅ Profile created successfully!
  Profile ID: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
  Profile Name: My Business
[Home.HandleCreateProfile] STEP 3: SetAsActive=True, attempting to auto-select
[Home.HandleCreateProfile] STEP 3: ✅ SetMyActiveProfileAsync succeeded!
[Home.HandleCreateProfile] STEP 4: Updated _currentProfileId to XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
[Home.HandleCreateProfile] STEP 5: Reset pagination to page 1
[Home.HandleCreateProfile] STEP 6: Loading feed for new profile
[Home.LoadFeedActivitiesAsync] START
[Home.HandleCreateProfile] STEP 6: Feed loaded successfully
[Home.HandleCreateProfile] STEP 7: Reloading user profiles
[Home.HandleCreateProfile] STEP 7: User profiles reloaded
[Home.HandleCreateProfile] STEP 8: ✅ STATE CHANGED - UI updated
---------------------------------------------------------------
[Home.HandleCreateProfile] ✅✅✅ PROFILE CREATION COMPLETE ✅✅✅
---------------------------------------------------------------

[ProfileCreatorModal.SubmitForm] ✅ OnCreate callback completed
═══════════════════════════════════════════════════════════════
```

---

## Next Steps

1. **Try to create a profile** and note exactly which step fails
2. **Copy the console logs** from that specific step
3. **Share the logs** with the error message
4. We can pinpoint the exact issue and fix it

The logging is very detailed, so we should be able to identify the exact problem! 🎯
