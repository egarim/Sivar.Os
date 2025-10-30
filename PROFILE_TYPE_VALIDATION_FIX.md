# Profile Type Limit Validation Fix

## 🎯 Summary

Fixed overly restrictive profile creation validation that only allowed **1 profile per type**. Now properly validates based on `MaxProfilesPerUser` from the `ProfileType` entity.

---

## 🔍 What Was Wrong

### Before (Line 645-649):
```csharp
// Check if user already has a profile of this type (for now, limit to one per type)
var hasExistingProfile = await _profileRepository.UserHasProfileOfTypeAsync(user.Id, targetProfileTypeId);
if (hasExistingProfile)
{
    errors.Add("User already has a profile of this type");
}
```

**Problem:** Hard-coded to allow only **1 profile per type**, ignoring the intended design:
- ❌ Personal: Should allow 3, but only allowed 1
- ❌ Business: Should allow 5, but only allowed 1  
- ❌ Brand: Should allow 5, but only allowed 1
- ❌ Creator: Should allow 3, but only allowed 1

---

## ✅ Solution Implemented

### 1. Added Missing Properties to ProfileType Entity
**File:** `Sivar.Os.Shared/Entities/ProfileType.cs`

Added three new properties:
```csharp
/// <summary>
/// Icon or emoji representing this profile type (e.g., "👤", "💼", "🏢")
/// </summary>
public virtual string? Icon { get; set; }

/// <summary>
/// Maximum number of profiles of this type a user can create
/// </summary>
public virtual int MaxProfilesPerUser { get; set; } = 1;

/// <summary>
/// Allowed features for this profile type (stored as JSON array)
/// </summary>
public virtual string AllowedFeatures { get; set; } = "[]";
```

### 2. Updated Validation Logic
**File:** `Sivar.Os/Services/ProfileService.cs` (Lines 620-638)

Now checks actual count against the limit:
```csharp
// Check if user has reached the maximum number of profiles for this type
var targetProfileType = await _profileTypeRepository.GetByIdAsync(targetProfileTypeId);
if (targetProfileType != null)
{
    var existingProfilesOfType = await _profileRepository.GetProfilesByUserIdAsync(user.Id, includeInactive: false);
    var currentCount = existingProfilesOfType.Count(p => p.ProfileTypeId == targetProfileTypeId);
    
    if (currentCount >= targetProfileType.MaxProfilesPerUser)
    {
        errors.Add($"Maximum number of {targetProfileType.DisplayName} profiles ({targetProfileType.MaxProfilesPerUser}) reached");
    }
}
```

**How It Works:**
1. Gets the ProfileType from database
2. Counts how many profiles the user already has of that type
3. Compares count to `MaxProfilesPerUser`
4. Only blocks creation if limit reached

---

## 🗄️ Database Migration Required

The new properties need to be added to the database. You'll need to:

### Option 1: Create Migration (Recommended)
```bash
dotnet ef migrations add AddProfileTypeMaxAndIcon --project Sivar.Os --context SivarDbContext
dotnet ef database update --project Sivar.Os
```

### Option 2: Manual SQL (Quick Fix)
```sql
-- Add new columns to ProfileTypes table
ALTER TABLE "ProfileTypes" 
ADD COLUMN "Icon" VARCHAR(10),
ADD COLUMN "MaxProfilesPerUser" INTEGER NOT NULL DEFAULT 1,
ADD COLUMN "AllowedFeatures" TEXT NOT NULL DEFAULT '[]';

-- Update existing profile types with proper limits
UPDATE "ProfileTypes" SET "MaxProfilesPerUser" = 3, "Icon" = '👤', "AllowedFeatures" = '["posts", "comments", "followers", "messaging"]' WHERE "Name" = 'Personal';
UPDATE "ProfileTypes" SET "MaxProfilesPerUser" = 5, "Icon" = '💼', "AllowedFeatures" = '["posts", "comments", "followers", "messaging", "analytics", "advertisements"]' WHERE "Name" = 'Business';
UPDATE "ProfileTypes" SET "MaxProfilesPerUser" = 5, "Icon" = '🏢', "AllowedFeatures" = '["posts", "comments", "followers", "messaging", "analytics", "verified_badge"]' WHERE "Name" = 'Brand';
UPDATE "ProfileTypes" SET "MaxProfilesPerUser" = 3, "Icon" = '🎬', "AllowedFeatures" = '["posts", "comments", "followers", "messaging", "monetization", "analytics"]' WHERE "Name" = 'Creator';
```

---

## 📊 Expected Behavior

### ✅ Personal Profile (Max: 3)
- 1st profile creation: ✅ Success
- 2nd profile creation: ✅ Success
- 3rd profile creation: ✅ Success
- 4th profile creation: ❌ "Maximum number of Personal Profile profiles (3) reached"

### ✅ Business Profile (Max: 5)
- 1st-5th profile creation: ✅ Success
- 6th profile creation: ❌ "Maximum number of Business Profile profiles (5) reached"

### ✅ Brand Profile (Max: 5)
- Same as Business (1-5 allowed, 6+ blocked)

### ✅ Creator Profile (Max: 3)
- Same as Personal (1-3 allowed, 4+ blocked)

---

## 🧪 Testing

### Test Scenario 1: Create Multiple Personal Profiles
1. Create profile: "John Personal 1" → Type: Personal
2. Create profile: "John Personal 2" → Type: Personal  
3. Create profile: "John Personal 3" → Type: Personal
4. Try to create: "John Personal 4" → Should fail with: *"Maximum number of Personal Profile profiles (3) reached"*

### Test Scenario 2: Mix Different Types
1. Create 3 Personal profiles → All succeed
2. Create 5 Business profiles → All succeed  
3. Each type has independent limit

### Verification Query
```sql
SELECT 
    u."Email",
    pt."Name" as ProfileType,
    pt."MaxProfilesPerUser",
    COUNT(p."Id") as CurrentCount,
    (pt."MaxProfilesPerUser" - COUNT(p."Id")) as RemainingSlots
FROM "Users" u
LEFT JOIN "Sivar_Profiles" p ON p."UserId" = u."Id"
LEFT JOIN "ProfileTypes" pt ON p."ProfileTypeId" = pt."Id"
WHERE u."KeycloakId" = 'YOUR_KEYCLOAK_ID'
GROUP BY u."Email", pt."Name", pt."MaxProfilesPerUser";
```

---

## 📝 Files Modified

1. ✅ `Sivar.Os.Shared/Entities/ProfileType.cs`
   - Added: `Icon` property
   - Added: `MaxProfilesPerUser` property (default = 1)
   - Added: `AllowedFeatures` property (default = "[]")

2. ✅ `Sivar.Os/Services/ProfileService.cs`
   - Updated `ValidateProfileCreationAsync()` method
   - Changed from boolean check to count-based validation
   - User-friendly error messages showing current limit

---

## ⚠️ Important Notes

- **Migration Required:** The database schema needs updating before this works properly
- **Default Value:** If migration doesn't run, `MaxProfilesPerUser` defaults to 1 (safe fallback)
- **Existing Data:** Current profile types in database need `MaxProfilesPerUser` values set
- **Seed Script:** The `Database_Seed_ProfileTypes.sql` already includes these columns

---

## 🔄 Next Steps

1. **Stop the running application** (it's currently running, preventing migration)
2. **Create and run migration:**
   ```bash
   dotnet ef migrations add AddProfileTypeMaxAndIcon --project Sivar.Os
   dotnet ef database update --project Sivar.Os
   ```
3. **Or manually add columns** using SQL above
4. **Restart application**
5. **Test profile creation** - should now allow multiple profiles per type up to the limit

---

## ✨ Benefits

✅ **Flexible Limits:** Each profile type has its own configurable limit  
✅ **Clear Error Messages:** Users see exactly which limit they hit  
✅ **Scalable:** Easy to change limits without code changes (just update database)  
✅ **Type Safety:** Counts only active profiles of the specific type  
✅ **Future-Proof:** New profile types can have different limits
