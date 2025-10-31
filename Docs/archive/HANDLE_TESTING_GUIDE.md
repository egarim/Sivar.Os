# Handle Field Testing Guide

## Quick Test Checklist

### 1. Test Handle-Based Routing
Navigate directly to a profile using its handle:

```
http://localhost:5000/jose-ojeda
```

**Expected Result:**
- ✅ Profile page loads successfully
- ✅ URL stays as `/jose-ojeda`
- ✅ Profile data displays correctly
- ✅ No redirect occurs

---

### 2. Test GUID to Handle Redirect
Navigate to a profile using its GUID:

```
http://localhost:5000/f9de039e-bb64-46ac-ade2-0667b9186f45
```

**Expected Result:**
- ✅ Profile loads briefly
- ✅ Browser redirects to `/jose-ojeda`
- ✅ URL changes in address bar
- ✅ No "back" loop (replace: true works)

**How to Verify:**
1. Open browser developer console (F12)
2. Look for console log: `[ProfilePage] Redirecting from GUID to handle: /jose-ojeda`
3. Check Network tab for navigation entry
4. Press browser "Back" button - should go to previous page, not GUID URL

---

### 3. Test Canonical URL Meta Tag
View page source while on a profile accessed via GUID:

```
1. Navigate to: http://localhost:5000/f9de039e-bb64-46ac-ade2-0667b9186f45
2. After redirect to /jose-ojeda
3. Right-click → View Page Source (or Ctrl+U)
4. Search for "canonical"
```

**Expected Result:**
```html
<link rel="canonical" href="http://localhost:5000/jose-ojeda" />
```

---

### 4. Test Handle Validation

#### Valid Handles:
- ✅ `jose-ojeda` (lowercase, hyphenated)
- ✅ `john123` (alphanumeric)
- ✅ `abc` (minimum length: 3)
- ✅ `test-user-123` (multiple hyphens)

#### Invalid Handles (should fail validation):
- ❌ `Jo` (too short, < 3 chars)
- ❌ `Jose_Ojeda` (underscore not allowed)
- ❌ `jose.ojeda` (dot not allowed)
- ❌ `José-Ojeda` (accented characters)
- ❌ `-jose-ojeda` (starts with hyphen)
- ❌ `jose-ojeda-` (ends with hyphen)
- ❌ `Jose-Ojeda` (uppercase not allowed)
- ❌ `jose--ojeda` (consecutive hyphens)

**Test Method (Entity Level):**
```csharp
Profile.IsValidHandle("jose-ojeda")  // Should return true
Profile.IsValidHandle("José-Ojeda")   // Should return false
```

---

### 5. Test Database Index

#### Check Unique Constraint:
Try creating two profiles with the same handle (via SQL or service):

```sql
-- This should work (first profile)
INSERT INTO "Sivar_Profiles" ("Id", "UserId", "ProfileTypeId", "DisplayName", "Handle", "Bio", ...)
VALUES (uuid_generate_v4(), ..., 'john-doe', ...);

-- This should FAIL with unique constraint violation
INSERT INTO "Sivar_Profiles" ("Id", "UserId", "ProfileTypeId", "DisplayName", "Handle", "Bio", ...)
VALUES (uuid_generate_v4(), ..., 'john-doe', ...);
```

**Expected Error:**
```
duplicate key value violates unique constraint "IX_Profiles_Handle"
```

---

### 6. Test Performance (Optional)

Compare query performance before and after Handle implementation:

```sql
-- Old way (DisplayName slug search - slower)
EXPLAIN ANALYZE
SELECT * FROM "Sivar_Profiles"
WHERE LOWER("DisplayName") = LOWER('Jose Ojeda');

-- New way (Handle indexed search - faster)
EXPLAIN ANALYZE
SELECT * FROM "Sivar_Profiles"
WHERE LOWER("Handle") = LOWER('jose-ojeda');
```

**Expected Result:**
- Handle search should use index scan
- DisplayName search may use sequential scan
- Handle search execution time should be <2ms
- DisplayName search execution time may be 5-10ms+

---

### 7. Browser Testing Matrix

Test in multiple browsers to ensure redirect works:

| Browser | Handle URL | GUID URL | Redirect | Canonical Tag |
|---------|-----------|----------|----------|---------------|
| Chrome  | ✅ | ✅ | ✅ | ✅ |
| Firefox | ✅ | ✅ | ✅ | ✅ |
| Edge    | ✅ | ✅ | ✅ | ✅ |
| Safari  | ✅ | ✅ | ✅ | ✅ |

---

### 8. SEO Testing

Use SEO tools to verify canonical URL implementation:

#### Google Search Console:
1. Submit profile URL
2. Request indexing
3. Check URL inspection tool
4. Verify canonical URL is recognized

#### Manual Check:
```bash
curl -I http://localhost:5000/f9de039e-bb64-46ac-ade2-0667b9186f45
```

Look for redirect (if server-side redirect added later):
```
HTTP/1.1 301 Moved Permanently
Location: /jose-ojeda
```

Currently it's client-side redirect (JavaScript), so you'll see 200 OK.

---

## Testing Existing Data

### Verify Migrated Handles:
```sql
SELECT "Id", "DisplayName", "Handle" 
FROM "Sivar_Profiles" 
ORDER BY "CreatedAt" DESC 
LIMIT 10;
```

**Expected Format:**
- DisplayName: `Jose Ojeda` → Handle: `jose-ojeda`
- DisplayName: `Test User 123` → Handle: `test-user-123`
- DisplayName: `ABC Company` → Handle: `abc-company`

### Check for Duplicates (should be empty):
```sql
SELECT "Handle", COUNT(*) 
FROM "Sivar_Profiles" 
GROUP BY "Handle" 
HAVING COUNT(*) > 1;
```

**Expected Result:** No rows (unique constraint working)

---

## Integration Testing

### Test Profile Creation:
Create a new profile via API and verify Handle is auto-generated:

```bash
curl -X POST http://localhost:5000/api/profiles \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "displayName": "New Test User",
    "bio": "Test bio",
    "profileTypeId": "PROFILE_TYPE_GUID"
  }'
```

**Expected Response:**
```json
{
  "id": "NEW_GUID",
  "displayName": "New Test User",
  "handle": "new-test-user",  ← Auto-generated
  "bio": "Test bio",
  ...
}
```

---

## Error Scenarios

### 1. Handle Not Found:
Navigate to non-existent handle:
```
http://localhost:5000/this-does-not-exist
```

**Expected:** Error message or 404 page

### 2. Invalid GUID:
Navigate to malformed GUID:
```
http://localhost:5000/not-a-guid-or-handle
```

**Expected:** Treated as handle, returns not found

### 3. Case Sensitivity:
Test case variations:
```
http://localhost:5000/JOSE-OJEDA  (uppercase)
http://localhost:5000/Jose-Ojeda  (mixed case)
http://localhost:5000/jose-ojeda  (lowercase)
```

**Expected:** All resolve to same profile (case-insensitive lookup)

---

## Automated Test Template

```csharp
[Fact]
public async Task GetProfileByHandle_ReturnsProfile()
{
    // Arrange
    var handle = "jose-ojeda";
    
    // Act
    var profile = await _profileService.GetProfileByIdentifierAsync(handle);
    
    // Assert
    Assert.NotNull(profile);
    Assert.Equal("jose-ojeda", profile.Handle);
    Assert.Equal("Jose Ojeda", profile.DisplayName);
}

[Fact]
public async Task GetProfileByGuid_RedirectsToHandle()
{
    // Arrange
    var guid = "f9de039e-bb64-46ac-ade2-0667b9186f45";
    
    // Act
    var profile = await _profileService.GetProfileByIdentifierAsync(guid);
    
    // Assert
    Assert.NotNull(profile);
    Assert.Equal("jose-ojeda", profile.Handle);
    // UI should redirect to /{profile.Handle}
}

[Theory]
[InlineData("jose-ojeda", true)]
[InlineData("test123", true)]
[InlineData("abc", true)]
[InlineData("-invalid", false)]
[InlineData("invalid-", false)]
[InlineData("José", false)]
[InlineData("ab", false)]
public void IsValidHandle_ValidatesCorrectly(string handle, bool expected)
{
    // Act
    var result = Profile.IsValidHandle(handle);
    
    // Assert
    Assert.Equal(expected, result);
}
```

---

## Smoke Test Script

Quick bash script to verify basic functionality:

```bash
#!/bin/bash

BASE_URL="http://localhost:5000"

echo "Testing handle-based routing..."
curl -s "$BASE_URL/jose-ojeda" | grep -q "Jose Ojeda" && echo "✅ Handle routing works" || echo "❌ Handle routing failed"

echo "Testing GUID-based routing (should redirect)..."
curl -s "$BASE_URL/f9de039e-bb64-46ac-ade2-0667b9186f45" | grep -q "jose-ojeda" && echo "✅ GUID redirect works" || echo "❌ GUID redirect failed"

echo "Testing canonical URL..."
curl -s "$BASE_URL/jose-ojeda" | grep -q 'rel="canonical"' && echo "✅ Canonical tag present" || echo "❌ Canonical tag missing"

echo "Testing non-existent handle..."
curl -s "$BASE_URL/does-not-exist-123" | grep -q "not found" && echo "✅ 404 handling works" || echo "⚠️  Check 404 handling"

echo "All smoke tests complete!"
```

---

## Testing Checklist Summary

- [ ] Handle routing works: `/jose-ojeda`
- [ ] GUID redirect works: `/GUID` → `/handle`
- [ ] Canonical URL appears in page source
- [ ] Handle validation prevents invalid characters
- [ ] Database unique constraint enforced
- [ ] Case-insensitive handle lookup
- [ ] Profile data displays correctly
- [ ] Browser back button works (no loop)
- [ ] Multiple browsers tested
- [ ] Existing profiles migrated successfully
- [ ] New profile creation auto-generates handle
- [ ] 404 for non-existent handles
- [ ] Performance acceptable (<2ms handle lookups)

---

**Status:** Ready for testing  
**Priority:** High (affects all profile URLs)  
**Estimated Testing Time:** 30-45 minutes  
**Required:** Before production deployment
