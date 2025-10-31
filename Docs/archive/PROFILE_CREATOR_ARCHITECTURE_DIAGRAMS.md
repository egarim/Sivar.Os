# Profile Creator - Architecture & Data Flow Diagrams

## System Architecture After Fix

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER (Blazor WASM)                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ ProfileCreatorModal Component                               │  │
│  │ ┌────────────────────────────────────────────────────────┐  │  │
│  │ │ Selected Profile Type: Business                        │  │  │
│  │ │ ProfileTypeId: 22222222-2222-2222-2222-222222222222   │  │  │
│  │ │ DisplayName: "BBBBBBB"                                 │  │  │
│  │ │                                                        │  │  │
│  │ │ [Create Profile Button] → SubmitForm()               │  │  │
│  │ └────────────────────────────────────────────────────────┘  │  │
│  │ ├─ Creates CreateAnyProfileDto with:                       │  │
│  │ │  ✓ ProfileTypeId (preserved!)                            │  │
│  │ │  ✓ DisplayName                                           │  │
│  │ │  ✓ Bio, Avatar, Tags, etc.                             │  │
│  │ └─ Calls: OnCreate.InvokeAsync(request)                   │  │
│  └────────────────────────┬─────────────────────────────────────┘  │
│                           │                                         │
│                           ▼                                         │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ ProfileSwitcher Component                                   │  │
│  ├──────────────────────────────────────────────────────────────┤  │
│  │ Receives: CreateAnyProfileDto with ProfileTypeId ✓         │  │
│  │ Calls: OnCreateProfile.InvokeAsync(request)                │  │
│  └────────────────────────┬─────────────────────────────────────┘  │
│                           │                                         │
│                           ▼                                         │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ Home Component (HandleCreateProfile)                        │  │
│  ├──────────────────────────────────────────────────────────────┤  │
│  │ Receives: CreateAnyProfileDto with ProfileTypeId ✓         │  │
│  │ Console Log: [Home] Profile request: ProfileTypeId=222...  │  │
│  │ Calls: SivarClient.Profiles.CreateProfileAsync(request)    │  │
│  └────────────────────────┬─────────────────────────────────────┘  │
│                           │                                         │
└───────────────────────────┼─────────────────────────────────────────┘
                            │
                HTTP POST /api/profiles (JSON)
                Request body includes ProfileTypeId ✓
                            │
┌───────────────────────────┼─────────────────────────────────────────┐
│                           ▼                                         │
│                  SERVER LAYER (ASP.NET Core)                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ ProfilesController.CreateProfile()                          │  │
│  ├──────────────────────────────────────────────────────────────┤  │
│  │ BEFORE FIX: [HttpPost] CreateProfile(CreateProfileDto)    │  │
│  │ AFTER FIX:  [HttpPost] CreateProfile(CreateAnyProfileDto) │  │
│  │                                                             │  │
│  │ NEW LOGIC:                                                 │  │
│  │ 1. Receives: CreateAnyProfileDto with ProfileTypeId ✓     │  │
│  │ 2. Extracts ProfileTypeId: 22222222-2222...              │  │
│  │ 3. Converts to CreateProfileDto (for compatibility)       │  │
│  │ 4. Calls: ValidateProfileCreationAsync(...,              │  │
│  │           specifiedProfileTypeId: 22222222...) ← PASS IT │  │
│  │ 5. Calls: CreateProfileAsync(...,                         │  │
│  │           specifiedProfileTypeId: 22222222...) ← PASS IT │  │
│  └────────────────────────┬─────────────────────────────────────┘  │
│                           │                                         │
│                           ▼                                         │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ ProfileService.ValidateProfileCreationAsync()              │  │
│  ├──────────────────────────────────────────────────────────────┤  │
│  │ BEFORE FIX: profileTypeId = null ?? Determine...()        │  │
│  │            → Defaults to Personal (WRONG!) ❌             │  │
│  │                                                             │  │
│  │ AFTER FIX:  targetProfileTypeId =                         │  │
│  │            specifiedProfileTypeId ??                       │  │
│  │            Determine...()                                  │  │
│  │            → Uses Business (CORRECT!) ✓                   │  │
│  │                                                             │  │
│  │ targetProfileTypeId = 22222222-2222... (Business)         │  │
│  │ Calls: UserHasProfileOfTypeAsync(userId, BUSINESS_ID)    │  │
│  │ Returns: false (user doesn't have Business profile)        │  │
│  │ Result: Validation PASSES ✓                                │  │
│  └────────────────────────┬─────────────────────────────────────┘  │
│                           │                                         │
│                           ▼                                         │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ ProfileService.CreateProfileAsync()                        │  │
│  ├──────────────────────────────────────────────────────────────┤  │
│  │ Receives: specifiedProfileTypeId = 22222222... ✓           │  │
│  │                                                             │  │
│  │ Logic:                                                      │  │
│  │ if (specifiedProfileTypeId.HasValue &&                     │  │
│  │     specifiedProfileTypeId.Value != Guid.Empty)           │  │
│  │ {                                                           │  │
│  │     profileTypeId = specifiedProfileTypeId.Value;         │  │
│  │     Log: "Using specified ProfileTypeId: 22222222..."    │  │
│  │ }                                                           │  │
│  │ else                                                        │  │
│  │ {                                                           │  │
│  │     profileTypeId = Determine...() // fallback            │  │
│  │ }                                                           │  │
│  │                                                             │  │
│  │ Creates Profile entity:                                    │  │
│  │   UserId = user.Id                                         │  │
│  │   ProfileTypeId = 22222222-2222... (Business) ✓            │  │
│  │   DisplayName = "BBBBBBB"                                  │  │
│  │   IsActive = false (initially)                             │  │
│  │                                                             │  │
│  │ Saves to database ✓                                         │  │
│  │ Sets as active (first profile) ✓                            │  │
│  │ Returns: ProfileDto                                         │  │
│  └────────────────────────┬─────────────────────────────────────┘  │
│                           │                                         │
└───────────────────────────┼─────────────────────────────────────────┘
                            │
                HTTP 201 Created
                ProfileDto (with correct ProfileTypeId) ✓
                            │
┌───────────────────────────┼─────────────────────────────────────────┐
│                           ▼                                         │
│                      DATABASE LAYER                                 │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ Profiles Table                                              │  │
│  ├──────────────────────────────────────────────────────────────┤  │
│  │ Id           │ UserId │ ProfileTypeId      │ DisplayName    │  │
│  │──────────────┼────────┼────────────────────┼────────────────│  │
│  │ 11111... ✓   │ user1  │ 11111111-... (Pers)│ Jose Ojeda    │  │
│  │ 99999... ✓   │ user1  │ 22222222-... (Bus) │ BBBBBBB ← NEW! │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ✅ BUSINESS PROFILE CREATED WITH CORRECT ProfileTypeId!            │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Comparison: Before vs After

### BEFORE (Bug) ❌

```
Frontend JSON:
{
  "profileTypeId": "22222222-2222-2222-2222-222222222222",
  "displayName": "BBBBBBB",
  ...
}
    ↓
Controller Parameter Type: CreateProfileDto
    ↓
JSON Deserializer:
  ✓ displayName → mapped
  ✓ bio → mapped
  ✓ avatar → mapped
  ✗ profileTypeId → IGNORED (CreateProfileDto has no this property!)
    ↓
Service receives: CreateProfileDto { displayName, bio, ... }
    ↓
Service code:
  profileTypeId = DetermineFromMetadata(createDto.Metadata)
    ↓
Metadata is empty → defaults to Personal (11111111...)
    ↓
Validation:
  UserHasProfileOfTypeAsync(userId, 11111111...) → TRUE
    ↓
RESULT: "User already has a profile of this type" ❌ (WRONG TYPE CHECKED!)
```

### AFTER (Fixed) ✅

```
Frontend JSON:
{
  "profileTypeId": "22222222-2222-2222-2222-222222222222",
  "displayName": "BBBBBBB",
  ...
}
    ↓
Controller Parameter Type: CreateAnyProfileDto
    ↓
JSON Deserializer:
  ✓ profileTypeId → mapped!
  ✓ displayName → mapped
  ✓ bio → mapped
  ✓ avatar → mapped
    ↓
Service receives: specifiedProfileTypeId = 22222222...
    ↓
Service code:
  if (specifiedProfileTypeId.HasValue && specifiedProfileTypeId.Value != Guid.Empty)
  {
    profileTypeId = specifiedProfileTypeId.Value  // ← USE SPECIFIED VALUE!
  }
    ↓
profileTypeId = 22222222... (Business)
    ↓
Validation:
  UserHasProfileOfTypeAsync(userId, 22222222...) → FALSE
    ↓
RESULT: "Profile created successfully" ✓ (CORRECT TYPE CHECKED!)
```

---

## Type Mismatch Issue

### The Core Problem

```
┌──────────────────────────────────────────────────────┐
│ CreateAnyProfileDto (what frontend sends)            │
├──────────────────────────────────────────────────────┤
│ Properties:                                          │
│ - ProfileTypeId ← IMPORTANT!                         │
│ - DisplayName                                        │
│ - Bio                                                │
│ - Avatar                                             │
│ - Location                                           │
│ - VisibilityLevel                                    │
│ - Tags                                               │
│ - Metadata                                           │
└──────────────────────────────────────────────────────┘

                        ↓ (JSON deserialization)

┌──────────────────────────────────────────────────────┐
│ CreateProfileDto (what controller received - WRONG!) │
├──────────────────────────────────────────────────────┤
│ Properties:                                          │
│ - DisplayName ✓                                      │
│ - Bio ✓                                              │
│ - Avatar ✓                                           │
│ - Location ✓                                         │
│ - IsPublic ✓                                         │
│ - VisibilityLevel ✓                                  │
│ - Tags ✓                                             │
│ - Metadata ✓                                         │
│ - ProfileTypeId ✗ (NOT IN THIS CLASS!)              │
└──────────────────────────────────────────────────────┘

RESULT: ProfileTypeId is DROPPED during deserialization!
```

### The Solution

```
BEFORE:
public CreateProfile([FromBody] CreateProfileDto createDto)  ← LOSES ProfileTypeId
    ↓
profileTypeId determined from Metadata

AFTER:
public CreateProfile([FromBody] CreateAnyProfileDto createDto)  ← PRESERVES ProfileTypeId
    ↓
ProfileTypeId extracted and passed to service
    ↓
profileTypeId = specifiedProfileTypeId (if provided)
```

---

## Method Call Chain

### Data Flow Through Services

```
HTTP Request: POST /api/profiles
{
  profileTypeId: "22222222-2222-2222-2222-222222222222",
  displayName: "BBBBBBB"
}
    ↓
┌─────────────────────────────────────────────────┐
│ ProfilesController.CreateProfile(createDto)     │
│                                                 │
│ Receives: CreateAnyProfileDto                   │
│ ├─ profileTypeId: 22222222... ✓                 │
│ └─ other fields                                 │
│                                                 │
│ Calls:                                          │
│ _profileService.ValidateProfileCreationAsync(   │
│   createProfileDto,                             │
│   keycloakId,                                   │
│   createDto.ProfileTypeId) ← PASS IT           │
└───────┬───────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────┐
│ ProfileService.ValidateProfileCreationAsync()   │
│                                                 │
│ Receives: specifiedProfileTypeId = 222222... ✓ │
│                                                 │
│ Process:                                        │
│ targetProfileTypeId =                           │
│   specifiedProfileTypeId ??                     │
│   Determine...()                                │
│                                                 │
│ Result: targetProfileTypeId = 22222222... ✓    │
│                                                 │
│ Calls:                                          │
│ _profileRepository                              │
│ .UserHasProfileOfTypeAsync(                     │
│   userId,                                       │
│   targetProfileTypeId) ← CORRECT TYPE!          │
│                                                 │
│ Returns: false (user doesn't have Business)     │
└───────┬───────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────┐
│ ProfilesController (continued)                  │
│                                                 │
│ Calls:                                          │
│ _profileService.CreateProfileAsync(             │
│   createProfileDto,                             │
│   keycloakId,                                   │
│   createDto.ProfileTypeId) ← PASS IT           │
└───────┬───────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────┐
│ ProfileService.CreateProfileAsync()             │
│                                                 │
│ Receives: specifiedProfileTypeId = 222222... ✓ │
│                                                 │
│ Logic:                                          │
│ if (specifiedProfileTypeId.HasValue &&          │
│     specifiedProfileTypeId != Guid.Empty)       │
│ {                                               │
│   profileTypeId = specifiedProfileTypeId.Value  │
│ }                                               │
│ else                                            │
│ {                                               │
│   profileTypeId = Determine...()                │
│ }                                               │
│                                                 │
│ Creates: Profile with                           │
│   ProfileTypeId = 22222222... (Business) ✓     │
│                                                 │
│ Saves to database ✓                             │
└─────────────────────────────────────────────────┘
```

---

## Validation Logic Comparison

### BEFORE (Bug)

```
User has: 1 Personal profile (11111111...)
Request: Create Business profile (22222222...)

Flow:
1. profileTypeId = DetermineFromMetadata("") 
2. Metadata empty → profileTypeId = 11111111... (Personal)
3. Check: UserHasProfileOfTypeAsync(userId, 11111111...)
4. Result: true (user HAS Personal profile)
5. Reject: "already has profile of this type"
6. WRONG TYPE WAS CHECKED! ❌
```

### AFTER (Fixed)

```
User has: 1 Personal profile (11111111...)
Request: Create Business profile (22222222...)

Flow:
1. profileTypeId = specifiedProfileTypeId (if provided)
2. specifiedProfileTypeId = 22222222... (Business)
3. profileTypeId = 22222222... (Business)
4. Check: UserHasProfileOfTypeAsync(userId, 22222222...)
5. Result: false (user DOESN'T have Business profile)
6. Accept: Profile created successfully
7. CORRECT TYPE WAS CHECKED! ✅
```

---

## Summary

The fix is **surgical and focused**:

1. ✅ Accept the correct DTO type (CreateAnyProfileDto)
2. ✅ Extract ProfileTypeId from the request
3. ✅ Pass ProfileTypeId through service calls
4. ✅ Use ProfileTypeId instead of metadata default
5. ✅ Validate with correct profile type
6. ✅ Create profile with correct ProfileTypeId

Result: **Business and Organization profile creation now works! 🎉**
