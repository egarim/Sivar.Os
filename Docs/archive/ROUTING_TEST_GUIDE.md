# Profile Routing - Quick Test Guide

## Prerequisites
1. Application running on `https://localhost:5001`
2. At least one profile in database with `VisibilityLevel = Public`

## Quick Database Check

```powershell
# Connect to database
$env:PGPASSWORD='1234567890'; psql -h localhost -U postgres -d XafSivarOs

# Check existing profiles
SELECT "Id", "DisplayName", "VisibilityLevel" 
FROM "Sivar_Profiles" 
WHERE "IsDeleted" = false 
AND "VisibilityLevel" = 1
ORDER BY "CreatedAt" DESC 
LIMIT 10;
```

## Test Cases

### Test 1: Home Route
**URL**: `https://localhost:5001/`

**Expected**:
- ✅ Home.razor page loads
- ✅ Activity feed displays
- ✅ Profile switcher visible in sidebar

**Verify**:
```
1. Open browser to https://localhost:5001/
2. Should see main feed
3. Should NOT see profile page
```

---

### Test 2: Profile by Slug (DisplayName)
**URL**: `https://localhost:5001/jose-ojeda`

**Expected**:
- ✅ ProfilePage.razor loads
- ✅ Displays profile for "Jose Ojeda" (if exists in DB)
- ✅ Shows DisplayName, Bio, Stats
- ✅ Or shows "Profile not found" error

**Steps**:
1. Get a real DisplayName from database:
   ```sql
   SELECT "DisplayName" FROM "Sivar_Profiles" 
   WHERE "VisibilityLevel" = 1 AND "IsDeleted" = false 
   LIMIT 1;
   ```
2. Convert to slug (e.g., "Jose Ojeda" → "jose-ojeda")
3. Navigate to `https://localhost:5001/jose-ojeda`
4. Check browser console for logs

**Expected Logs**:
```
[ProfilePage] Loading profile for identifier: jose-ojeda
[ProfilesClient.GetProfileByIdentifierAsync] Fetching profile by identifier: jose-ojeda
[ProfileService.GetProfileByIdentifierAsync] Identifier is slug: jose-ojeda
[ProfilePage] Profile loaded: Jose Ojeda (ID: ...)
```

---

### Test 3: Profile by GUID
**URL**: `https://localhost:5001/{profile-id}`

**Steps**:
1. Get a real profile ID from database:
   ```sql
   SELECT "Id" FROM "Sivar_Profiles" 
   WHERE "VisibilityLevel" = 1 AND "IsDeleted" = false 
   LIMIT 1;
   ```
2. Navigate to `https://localhost:5001/{that-guid}`
3. Check browser console

**Expected**:
- ✅ ProfilePage.razor loads
- ✅ Displays profile data
- ✅ URL stays as GUID (no redirect yet)

**Expected Logs**:
```
[ProfilePage] Loading profile for identifier: f9de039e-bb64-46ac-ade2-0667b9186f45
[ProfileService.GetProfileByIdentifierAsync] Identifier is GUID: f9de039e-bb64-46ac-ade2-0667b9186f45
[ProfilePage] Profile loaded: Jose Ojeda (ID: f9de039e-bb64-46ac-ade2-0667b9186f45)
```

---

### Test 4: Non-Existent Profile
**URL**: `https://localhost:5001/fake-user-name`

**Expected**:
- ✅ ProfilePage.razor loads
- ✅ Shows "Profile not found" message
- ✅ Displays fallback data

**Expected Logs**:
```
[ProfilePage] Loading profile for identifier: fake-user-name
[ProfileService.GetProfileByIdentifierAsync] Identifier is slug: fake-user-name
[ProfileService.GetProfileByIdentifierAsync] Profile not found or not public for slug: fake-user-name
[ProfilePage] Profile not found for identifier: fake-user-name
```

---

### Test 5: Private Profile (Security Test)
**Setup**:
1. Create or find a profile with `VisibilityLevel = Private` (value 0)
2. Get its ID or DisplayName slug

**URL**: `https://localhost:5001/{private-profile-slug}`

**Expected**:
- ✅ Profile NOT accessible
- ✅ Shows "Profile not found" (does not reveal it's private)
- ✅ Protects private user data

---

### Test 6: Case Insensitivity
**URL**: 
- `https://localhost:5001/jose-ojeda`
- `https://localhost:5001/Jose-Ojeda`
- `https://localhost:5001/JOSE-OJEDA`

**Expected**:
- ✅ All three URLs should find the same profile
- ✅ DisplayName search is case-insensitive

---

## API Testing

### Direct API Call
```powershell
# Test the API endpoint directly
curl -X GET "https://localhost:5001/api/profiles/by-identifier/jose-ojeda" `
  -H "Accept: application/json" `
  -k

# With profile ID (GUID)
curl -X GET "https://localhost:5001/api/profiles/by-identifier/f9de039e-bb64-46ac-ade2-0667b9186f45" `
  -H "Accept: application/json" `
  -k
```

**Expected Response** (success):
```json
{
  "id": "f9de039e-bb64-46ac-ade2-0667b9186f45",
  "displayName": "Jose Ojeda",
  "bio": "...",
  "profileType": {
    "displayName": "PersonalProfile"
  },
  ...
}
```

**Expected Response** (not found):
```json
"Profile not found for identifier: fake-user"
```

---

## Performance Check

### View Count Increment
1. Navigate to a profile: `/jose-ojeda`
2. Check view count in database:
   ```sql
   SELECT "DisplayName", "ViewCount" 
   FROM "Sivar_Profiles" 
   WHERE "DisplayName" ILIKE '%jose%';
   ```
3. Refresh the page
4. Check view count again (should increment by 1)

---

## Browser Console Checks

### Open Developer Tools
Press `F12` in browser

### Check for Errors
Look in Console tab for:
- ❌ Red errors → Something is broken
- ⚠️ Yellow warnings → May be okay
- ℹ️ Blue info → Expected logs

### Network Tab
1. Navigate to a profile
2. Check Network tab
3. Look for: `by-identifier/{identifier}`
4. Should return 200 OK for existing profiles
5. Should return 404 Not Found for non-existent profiles

---

## Troubleshooting

### Problem: All routes go to Home
**Solution**: Check route priority, ProfilePage might not be registered

### Problem: ProfilePage shows 404
**Solution**: 
1. Check if profile exists in database
2. Check if profile is Public
3. Check API endpoint is working

### Problem: Slug not working but GUID works
**Solution**: 
1. Check `GetByDisplayNameSlugAsync` implementation
2. Verify DisplayName matches (case-insensitive)
3. Check database has matching DisplayName

### Problem: No data displayed
**Solution**:
1. Check browser console for errors
2. Verify API endpoint returns data
3. Check ProfilesClient is injected correctly

---

## Sample Test Data Creation

If you need to create test profiles:

```sql
-- Check your user ID first
SELECT "Id", "Email" FROM "Sivar_Users" WHERE "IsDeleted" = false LIMIT 1;

-- Check PersonalProfile type ID
SELECT "Id" FROM "Sivar_ProfileTypes" WHERE "Name" = 'PersonalProfile';

-- Create a test profile (adjust IDs as needed)
INSERT INTO "Sivar_Profiles" 
("Id", "UserId", "ProfileTypeId", "DisplayName", "Bio", "VisibilityLevel", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted")
VALUES 
(gen_random_uuid(), 
 'YOUR-USER-ID-HERE', 
 'YOUR-PROFILE-TYPE-ID-HERE',
 'Jose Ojeda', 
 'Software Developer and AI Enthusiast', 
 1,  -- Public
 true, 
 NOW(), 
 NOW(), 
 false);
```

---

## Success Criteria

✅ Home route (`/`) shows Home.razor  
✅ Profile slug route (`/jose-ojeda`) shows ProfilePage.razor  
✅ Profile GUID route (`/{guid}`) shows ProfilePage.razor  
✅ Non-existent profile shows error message  
✅ Private profiles are protected  
✅ View count increments on profile view  
✅ Case-insensitive slug matching works  
✅ API endpoint returns correct data  
✅ Browser console shows appropriate logs  
✅ No compilation errors  

---

## Next Steps After Testing

1. **If all tests pass**: ✅ Ready for production
2. **If slug collisions occur**: Consider adding `Handle` field to Profile entity
3. **If performance issues**: Add database index on DisplayName
4. **For SEO**: Implement 301 redirects from GUID to slug URLs
5. **For analytics**: Track view counts and popular profiles

---

## Support

If issues persist:
1. Check `ROUTING_IMPLEMENTATION_SUMMARY.md` for architecture details
2. Check `TROUBLESHOOTING.md` for common issues
3. Review server logs for detailed error messages
4. Check database connection and data integrity
