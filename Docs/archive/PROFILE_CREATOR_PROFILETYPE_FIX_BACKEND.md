# Profile Creator - Backend ProfileTypeId Fix

## Problem Discovered

The frontend was correctly sending `CreateAnyProfileDto` with the `ProfileTypeId` property to create profiles with different types (Business, Organization, etc.), but the server was **ignoring this ProfileTypeId** and determining the profile type from the `Metadata` field instead.

### Root Cause

1. **Controller Issue**: The `[HttpPost]` endpoint in `ProfilesController` accepted `CreateProfileDto` instead of `CreateAnyProfileDto`
2. **Deserialization Loss**: When `CreateAnyProfileDto` was serialized as JSON and deserialized into `CreateProfileDto`, the `ProfileTypeId` property was **lost** because `CreateProfileDto` doesn't have this property
3. **Incorrect Service Logic**: The `ProfileService.CreateProfileAsync` method then called `DetermineProfileTypeFromMetadataAsync()`, which defaults to Personal type when metadata is empty

### Data Flow Problem

```
Frontend sends:
CreateAnyProfileDto { 
  ProfileTypeId: 22222222-2222-2222-2222-222222222222 (Business),
  DisplayName: "BBBBBBB",
  ... other fields ...
}
    ↓
HTTP POST to /api/profiles
    ↓
Controller deserializes into CreateProfileDto (ProfileTypeId is lost!)
    ↓
Service receives CreateProfileDto without ProfileTypeId
    ↓
Service calls DetermineProfileTypeFromMetadataAsync(createDto.Metadata)
    ↓
Metadata empty → defaults to 11111111-1111-1111-1111-111111111111 (Personal)
    ↓
Validation checks: UserHasProfileOfTypeAsync(userId, Personal_ID)
    ↓
Returns TRUE (user has Personal profile)
    ↓
Server rejects: "User already has a profile of this type"
    ↓
WRONG TYPE WAS CHECKED!
```

## Solution Implemented

### 1. **Updated Controller Endpoint** (`ProfilesController.cs` line 377)

**Before**:
```csharp
[HttpPost]
public async Task<ActionResult<ProfileDto>> CreateProfile([FromBody] CreateProfileDto createDto)
```

**After**:
```csharp
[HttpPost]
public async Task<ActionResult<ProfileDto>> CreateProfile([FromBody] CreateAnyProfileDto createDto)
{
    // Convert CreateAnyProfileDto to CreateProfileDto for service
    var createProfileDto = new CreateProfileDto
    {
        DisplayName = createDto.DisplayName,
        Bio = createDto.Bio,
        Avatar = createDto.Avatar,
        AvatarFileId = createDto.AvatarFileId,
        Location = createDto.Location,
        IsPublic = createDto.VisibilityLevel != Sivar.Os.Shared.Enums.VisibilityLevel.Private,
        VisibilityLevel = createDto.VisibilityLevel,
        Tags = createDto.Tags,
        SocialMediaLinks = new Dictionary<string, string>(),
        Metadata = createDto.Metadata
    };

    // Pass the ProfileTypeId to service validation and creation
    var validation = await _profileService.ValidateProfileCreationAsync(
        createProfileDto, keycloakId, createDto.ProfileTypeId);
    
    var profile = await _profileService.CreateProfileAsync(
        createProfileDto, keycloakId, createDto.ProfileTypeId);
```

### 2. **Updated Service Method** (`ProfileService.cs` line 752)

**Before**:
```csharp
public async Task<ProfileDto?> CreateProfileAsync(CreateProfileDto createDto, string userKeycloakId)
{
    // ... validation ...
    var profileTypeId = await DetermineProfileTypeFromMetadataAsync(createDto.Metadata);
}
```

**After**:
```csharp
public async Task<ProfileDto?> CreateProfileAsync(
    CreateProfileDto createDto, 
    string userKeycloakId, 
    Guid? specifiedProfileTypeId = null)
{
    // ... validation ...
    
    // Use specified ProfileTypeId if provided, otherwise determine from metadata
    Guid profileTypeId;
    if (specifiedProfileTypeId.HasValue && specifiedProfileTypeId.Value != Guid.Empty)
    {
        profileTypeId = specifiedProfileTypeId.Value;
        _logger.LogInformation("[CreateProfileAsync] Using specified ProfileTypeId: {ProfileTypeId}", 
            profileTypeId);
    }
    else
    {
        profileTypeId = await DetermineProfileTypeFromMetadataAsync(createDto.Metadata);
        _logger.LogInformation("[CreateProfileAsync] Determined ProfileTypeId from metadata: {ProfileTypeId}", 
            profileTypeId);
    }
}
```

### 3. **Updated Interface** (`IProfileService.cs`)

Added optional parameter to method signature:

```csharp
Task<ProfileDto?> CreateProfileAsync(
    CreateProfileDto createDto, 
    string userKeycloakId, 
    Guid? specifiedProfileTypeId = null);
```

### 4. **Updated ProfileSwitcherClient** (`ProfileSwitcherClient.cs` line 171)

Now passes the `ProfileTypeId` when calling the service:

```csharp
var profile = await _profileService.CreateProfileAsync(
    createDto, 
    keycloakId, 
    request.ProfileTypeId);  // ← Pass the ProfileTypeId from request!
```

## Data Flow After Fix

```
Frontend sends:
CreateAnyProfileDto { 
  ProfileTypeId: 22222222-2222-2222-2222-222222222222 (Business),
  DisplayName: "BBBBBBB",
  ... other fields ...
}
    ↓
HTTP POST to /api/profiles
    ↓
Controller receives CreateAnyProfileDto
    ↓
Controller passes ProfileTypeId to service: CreateProfileAsync(..., createDto.ProfileTypeId)
    ↓
Service receives specifiedProfileTypeId = 22222222-2222-2222-2222-222222222222
    ↓
Service uses this directly (not from metadata!)
    ↓
Validation checks: UserHasProfileOfTypeAsync(userId, Business_ID)
    ↓
Returns FALSE (user doesn't have Business profile)
    ↓
Server creates Business profile successfully! ✅
```

## Testing Checklist

- [ ] Create Personal profile (should succeed)
- [ ] Create Business profile (should succeed - this was failing before)
- [ ] Create Organization profile (should succeed)
- [ ] Try creating second Business profile (should fail with duplicate message)
- [ ] Check server logs show "Using specified ProfileTypeId" message
- [ ] Verify database has profiles with correct ProfileTypeId values

## Files Modified

1. `Controllers/ProfilesController.cs` - Changed endpoint to accept `CreateAnyProfileDto`
2. `Services/ProfileService.cs` - Added optional `specifiedProfileTypeId` parameter
3. `Services/Clients/ProfileSwitcherClient.cs` - Pass `ProfileTypeId` to service
4. `Shared/Services/IProfileService.cs` - Updated interface signature

## Build Status

✅ **Build succeeded with 0 errors** (warnings are pre-existing and unrelated)

## Key Changes Summary

- **ProfileTypeId is now PASSED** from controller to service instead of being ignored
- **Validation happens with CORRECT profile type** (not defaulted to Personal)
- **Service logs show which ProfileTypeId is being used** (specified vs. determined from metadata)
- **Backward compatible** - existing code using Personal profiles still works
