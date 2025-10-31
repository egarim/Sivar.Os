# Profile Handle Duplicate Key Fix

## 🔴 Problem Identified

**Error:** `duplicate key value violates unique constraint "IX_Profiles_Handle"`

### Root Cause
When creating a profile, the `Handle` field (unique identifier like "jose-ojeda") was **not being set** in `ProfileService.CreateProfileAsync()`. This caused:

1. Database insert failed because `Handle` is required (NOT NULL)
2. If a default empty string was used, subsequent profile creations failed with duplicate key constraint
3. Profile creation always failed with cryptic database errors

### Evidence from Console Logs
```
[07:17:59 ERR] Microsoft.EntityFrameworkCore.Update: An exception occurred in the database while saving changes
Microsoft.EntityFrameworkCore.DbUpdateException: An error occurred while saving the entity changes.
---> Npgsql.PostgresException (0x80004005): 23505: duplicate key value violates unique constraint "IX_Profiles_Handle"
```

---

## ✅ Solution Implemented

### 1. Added Unique Handle Generation in ProfileService
**File:** `Sivar.Os/Services/ProfileService.cs`

Added logic to generate unique handles from display names:

```csharp
// Generate unique handle from display name
var baseHandle = Profile.GenerateHandle(createDto.DisplayName);
var uniqueHandle = await GenerateUniqueHandleAsync(baseHandle);

_logger.LogInformation("[CreateProfileAsync] Generated handle: {Handle} from DisplayName: {DisplayName}", 
    uniqueHandle, createDto.DisplayName);

// Create profile with unique handle
var profile = new Profile
{
    UserId = user.Id,
    ProfileTypeId = profileTypeId,
    DisplayName = createDto.DisplayName,
    Handle = uniqueHandle, // ✅ Set unique handle
    Bio = createDto.Bio,
    Avatar = createDto.Avatar,
    // ... rest of properties
};
```

### 2. Added Helper Method for Handle Uniqueness
**File:** `Sivar.Os/Services/ProfileService.cs`

```csharp
/// <summary>
/// Generates a unique handle by appending a number suffix if the base handle already exists
/// </summary>
private async Task<string> GenerateUniqueHandleAsync(string baseHandle)
{
    if (string.IsNullOrWhiteSpace(baseHandle))
    {
        // Fallback to a random handle if base is empty
        baseHandle = $"profile-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
    }

    var handle = baseHandle;
    var suffix = 1;
    
    // Check if handle exists, if so, append incrementing number until unique
    while (await _profileRepository.HandleExistsAsync(handle))
    {
        handle = $"{baseHandle}-{suffix}";
        suffix++;
        
        // Safety limit to prevent infinite loop
        if (suffix > 1000)
        {
            _logger.LogWarning("[GenerateUniqueHandleAsync] Reached suffix limit for handle: {BaseHandle}", baseHandle);
            handle = $"{baseHandle}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            break;
        }
    }
    
    return handle;
}
```

**Examples:**
- DisplayName: "BBBB" → Handle: "bbbb"
- DisplayName: "BBBB" (second profile) → Handle: "bbbb-1"
- DisplayName: "Jose Ojeda" → Handle: "jose-ojeda"
- DisplayName: "Jose Ojeda" (second) → Handle: "jose-ojeda-1"

### 3. Added Repository Method for Handle Existence Check
**File:** `Sivar.Os.Shared/Repositories/IProfileRepository.cs`

```csharp
/// <summary>
/// Checks if a handle already exists in the database
/// </summary>
Task<bool> HandleExistsAsync(string handle);
```

**File:** `Sivar.Os.Data/Repositories/ProfileRepository.cs`

```csharp
/// <summary>
/// Checks if a handle already exists in the database
/// </summary>
public async Task<bool> HandleExistsAsync(string handle)
{
    if (string.IsNullOrWhiteSpace(handle))
        return false;

    // Check if any profile (public or private) has this handle
    return await _dbSet
        .AnyAsync(p => p.Handle.ToLower() == handle.ToLower());
}
```

---

## 📋 How It Works

1. **User creates profile** with DisplayName: "My Business"
2. **ProfileService** calls `Profile.GenerateHandle("My Business")`
   - Result: "my-business"
3. **ProfileService** calls `GenerateUniqueHandleAsync("my-business")`
   - Checks database: Is "my-business" already taken?
   - If **NO** → Returns "my-business"
   - If **YES** → Returns "my-business-1" (or -2, -3, etc.)
4. **Profile entity** created with unique handle
5. **Database insert** succeeds ✅

---

## 🧪 Testing the Fix

### Test Case 1: First Profile with Same Name
1. Create profile: DisplayName = "BBBB"
2. Expected handle: "bbbb"
3. ✅ Should succeed

### Test Case 2: Second Profile with Same Name
1. Create another profile: DisplayName = "BBBB"
2. Expected handle: "bbbb-1"
3. ✅ Should succeed (no duplicate key error)

### Test Case 3: Third Profile with Same Name
1. Create third profile: DisplayName = "BBBB"
2. Expected handle: "bbbb-2"
3. ✅ Should succeed

### Verification Query
```sql
SELECT "Id", "DisplayName", "Handle" 
FROM "Sivar_Profiles" 
ORDER BY "CreatedAt" DESC;
```

Expected results:
```
DisplayName | Handle
------------|--------
BBBB        | bbbb
BBBB        | bbbb-1
BBBB        | bbbb-2
Jose Ojeda  | jose-ojeda
```

---

## 🔄 Next Steps

1. **Restart the application** (the fix is in the code, database changes not required)
2. **Try creating a profile** - should now succeed
3. **Check server logs** for:
   ```
   [CreateProfileAsync] Generated handle: bbbb from DisplayName: BBBB
   [ProfileSwitcherClient] Successfully created profile
   ```
4. **Verify in database** that profile exists with proper handle

---

## 📝 Files Modified

1. ✅ `Sivar.Os/Services/ProfileService.cs`
   - Added handle generation in `CreateProfileAsync()`
   - Added `GenerateUniqueHandleAsync()` helper method

2. ✅ `Sivar.Os.Shared/Repositories/IProfileRepository.cs`
   - Added `HandleExistsAsync()` interface method

3. ✅ `Sivar.Os.Data/Repositories/ProfileRepository.cs`
   - Implemented `HandleExistsAsync()` method

---

## ⚠️ Important Notes

- **No database migration needed** - Handle column already exists
- **Existing profiles without handles** may need data cleanup:
  ```sql
  UPDATE "Sivar_Profiles" 
  SET "Handle" = lower(replace("DisplayName", ' ', '-'))
  WHERE "Handle" IS NULL OR "Handle" = '';
  ```
- **Profile.GenerateHandle()** is a static utility method already present in the `Profile` entity
- **Case-insensitive comparison** ensures "BBBB" and "bbbb" are treated as duplicates

---

## 🎯 Success Criteria

✅ **Build succeeds** without errors  
✅ **Profile creation completes** without database constraint violations  
✅ **Handles are unique** across all profiles  
✅ **Duplicate display names** get suffixed handles (name-1, name-2, etc.)  
✅ **Logs show** handle generation: `Generated handle: {handle} from DisplayName: {name}`  
