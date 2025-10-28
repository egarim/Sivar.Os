# Research Findings: Profile Auto-Selection & Activity Reloading

## Problem Statement

After creating a new profile, two things are missing:
1. **New profile is NOT automatically selected** (only if user manually checks "Set as Active" checkbox)
2. **Activities/Feed are NOT automatically reloaded** (showing old profile's data)

---

## Current vs Required Behavior

### CURRENT FLOW ❌

```
User creates new profile
        ↓
┌─────────────────────────────────────┐
│ HandleCreateProfile() called         │
├─────────────────────────────────────┤
│ newProfile = CreateProfileAsync()   │
│                                     │
│ IF request.SetAsActive == true      │ ← Only if checkbox marked!
│ {                                   │
│     SetMyActiveProfileAsync()       │
│     _activeProfile = newProfile     │
│ }                                   │
│                                     │
│ LoadUserProfilesAsync()             │
│ StateHasChanged()                   │
└──────────────┬──────────────────────┘
               │
               ▼
        ❌ PROBLEM 1:
        - Profile NOT selected if checkbox unchecked
        - _currentProfileId NOT updated
        
        ❌ PROBLEM 2:
        - Feed NOT reloaded
        - Still showing old profile's activities
        - Activities not attached to new profile
```

### REQUIRED FLOW ✅

```
User creates new profile
        ↓
┌─────────────────────────────────────┐
│ HandleCreateProfile() called         │
├─────────────────────────────────────┤
│ newProfile = CreateProfileAsync()   │
│                                     │
│ ✅ ALWAYS select new profile:       │
│ {                                   │
│     _activeProfile = newProfile     │
│     _currentProfileId = newProfileId│
│ }                                   │
│                                     │
│ ✅ ALWAYS reload activities:        │
│ {                                   │
│     _currentPage = 1                │
│     LoadFeedPostsAsync()            │
│ }                                   │
│                                     │
│ LoadUserProfilesAsync()             │
│ StateHasChanged()                   │
└──────────────┬──────────────────────┘
               │
               ▼
        ✅ New profile selected
        ✅ Activities reloaded
        ✅ UI updated correctly
```

---

## Code Dependency Chain

```
┌────────────────────────────────────────────────────────┐
│ HandleCreateProfile()                                  │
├────────────────────────────────────────────────────────┤
│                                                        │
│  1. _currentProfileId = newProfile.Id                 │
│     ↓                                                  │
│     └─→ Used by LoadFeedPostsAsync() to fetch posts   │
│                                                        │
│  2. LoadFeedPostsAsync()                              │
│     ├─→ Checks: if (_currentProfileId == Guid.Empty) │
│     ├─→ Calls: GetFeedPostsAsync(profileId)          │
│     └─→ Sets: _posts = feedDto.Posts                 │
│                                                        │
│  3. StateHasChanged()                                 │
│     └─→ Triggers UI refresh with new _posts          │
│                                                        │
└────────────────────────────────────────────────────────┘
```

---

## Activities Attachment Architecture

### Current Architecture

```
┌──────────────────────────────────────────────────────────┐
│ Database                                                 │
├──────────────────────────────────────────────────────────┤
│                                                          │
│ Users Table         Profiles Table      Posts/Activities │
│ ├─ Id               ├─ Id               ├─ Id           │
│ ├─ KeycloakId       ├─ UserId  ◄────┐   ├─ ProfileId◄──┐│
│ └─ ...              ├─ ProfileTypeId   │   └─ ...       ││
│                     ├─ IsActive        │                │
│                     └─ ...             │                │
│                                         └────────────────┘
│                                                          │
└──────────────────────────────────────────────────────────┘

KEY INSIGHT:
- Activities are tied to Profile.Id
- When ProfileId changes → Activity feed changes
- Must reload activities when profile changes
```

### Data Flow for Activities

```
Frontend (Home.razor)
    │
    ├─ _currentProfileId: Guid = "ProfileId_123"
    │
    └─ LoadFeedPostsAsync()
        │
        └─ SivarClient.Posts.GetFeedPostsAsync(
               profileId: _currentProfileId
           )
           │
           └─ Backend API
               │
               └─ SELECT * FROM Posts
                  WHERE ProfileId = _currentProfileId
                  │
                  └─ Returns activities for that profile
                      │
                      └─ Frontend receives _posts
                          │
                          └─ UI displays _posts

IMPLICATION:
If _currentProfileId doesn't change → old activities shown
Must update _currentProfileId when profile created/changed
```

---

## SetAsActive Property Analysis

### Current SetAsActive in CreateAnyProfileDto

**File**: `Sivar.Os.Shared/DTOs/ProfileDto.cs` (Line 425)

```csharp
public class CreateAnyProfileDto
{
    public Guid ProfileTypeId { get; set; }
    public string DisplayName { get; set; }
    public bool SetAsActive { get; set; } = false;  // ← Default is FALSE
}
```

### SetAsActive in ProfileCreatorModal

**File**: `ProfileCreatorModal.razor` (Line 86-97)

```razor
<div class="modal-body">
    <!-- ... other fields ... -->
    
    <div class="form-group">
        <label>
            <input type="checkbox" @bind="SetAsActive" />
            Set as Active Profile
        </label>
    </div>
</div>

@code {
    private bool SetAsActive { get; set; } = false;  // ← Also FALSE by default
    
    private void SubmitForm()
    {
        var request = new CreateAnyProfileDto
        {
            ProfileTypeId = SelectedProfileType?.Id ?? Guid.Empty,
            DisplayName = ProfileName,
            SetAsActive = SetAsActive  // ← User must check box
        };
        
        OnCreate.InvokeAsync(request);
    }
}
```

### SetAsActive Usage in Home.HandleCreateProfile

**File**: `Home.razor` (Line 3056-3060)

```csharp
private async Task HandleCreateProfile(CreateAnyProfileDto request)
{
    var newProfile = await SivarClient.Profiles.CreateProfileAsync(request);
    
    if (newProfile != null)
    {
        // ❌ Only applies if user checked "Set as Active" checkbox
        if (request.SetAsActive)
        {
            Console.WriteLine("[Home] Setting new profile as active");
            await SivarClient.Profiles.SetMyActiveProfileAsync(newProfile.Id);
            _activeProfile = newProfile;
        }
        
        // ❌ Missing: Don't update _currentProfileId!
        // ❌ Missing: Don't reload feed!
        
        await LoadUserProfilesAsync();
        StateHasChanged();
    }
}
```

---

## Variable State Tracking

### Variables That Need Updates

```csharp
// In Home.razor

// 1. Current Profile ID (used for feed loading)
private Guid _currentProfileId = Guid.Empty;

// 2. Active Profile Object
private ProfileDto _activeProfile = null;

// 3. Feed/Activities List
private List<PostDto> _posts = new();

// 4. Pagination
private int _currentPage = 1;  // Must reset to 1 when loading new profile

// 5. Total Pages
private int _totalPages = 0;
```

### Variable Update Sequence

```
BEFORE profile creation:
  _currentProfileId = "old-profile-id"
  _posts = [activities from old profile]

AFTER profile creation (CURRENT - WRONG):
  _currentProfileId = "old-profile-id"  ← ❌ NOT UPDATED!
  _posts = [activities from old profile]  ← ❌ NOT RELOADED!

AFTER profile creation (REQUIRED - CORRECT):
  _currentProfileId = "new-profile-id"  ← ✅ UPDATED!
  _posts = [activities from new profile]  ← ✅ RELOADED!
  _currentPage = 1  ← ✅ RESET!
```

---

## Method Dependency Graph

```
┌─────────────────────────────────────────────────────────┐
│ HandleCreateProfile(CreateAnyProfileDto request)       │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ├─→ SivarClient.Profiles.CreateProfileAsync(request)
                   │   └─→ Returns: newProfile (ProfileDto)
                   │
                   ├─→ ✅ NEW: Update _currentProfileId
                   │
                   ├─→ ✅ NEW: LoadFeedPostsAsync()
                   │   │
                   │   ├─→ Checks: _currentProfileId != Guid.Empty
                   │   ├─→ Calls: SivarClient.Posts.GetFeedPostsAsync(
                   │   │         pageSize: 10, pageNumber: _currentPage)
                   │   └─→ Updates: _posts, _totalPages
                   │
                   ├─→ LoadUserProfilesAsync()
                   │   └─→ Updates: _userProfiles, _activeProfile
                   │
                   └─→ StateHasChanged()
                       └─→ Triggers UI refresh
```

---

## What GetFeedPostsAsync Expects

**API Call**: `SivarClient.Posts.GetFeedPostsAsync(pageSize, pageNumber)`

**What happens internally** (Server-side):
1. Gets current user from authentication
2. Gets user's active profile ID
3. Queries posts WHERE ProfileId = activeProfileId
4. Returns posts for that profile

**Problem**: If we don't update _currentProfileId on frontend, and user doesn't explicitly switch profile, the server will return old profile's activities!

---

## Implementation Checklist

### Changes Needed:

```
1. Home.razor HandleCreateProfile() method
   [ ] Add: _currentProfileId = newProfile.Id
   [ ] Add: _currentPage = 1 (reset pagination)
   [ ] Add: await LoadFeedPostsAsync() (reload activities)
   [ ] Modify: Make profile selection ALWAYS happen (not conditional)

2. ProfileCreatorModal.razor (Optional - for better UX)
   [ ] Change: SetAsActive default from false to true
   [ ] Or: Remove SetAsActive checkbox entirely (always auto-select)

3. Testing
   [ ] Create profile → verify it's selected
   [ ] Create profile → verify feed shows new profile's activities
   [ ] Create profile → verify old activities not shown
   [ ] Try different profile types → all work correctly
```

---

## Side Effects to Consider

### If We Always Reload Feed:

✅ **Positive:**
- Feed always shows correct profile's activities
- User sees new profile's content immediately
- No stale data shown

⚠️ **Potential Issues to Watch:**
- Network call for activities (but already happens on profile switch)
- Pagination resets to page 1 (expected behavior)
- May take moment to load (add loading indicator if needed)

### If We Always Select New Profile:

✅ **Positive:**
- Simpler UX (no checkbox decision needed)
- New profile automatically becomes active
- Consistent behavior with switching

⚠️ **Potential Issues to Watch:**
- Users may not want auto-selection (but seems minor)
- Should still allow "backup" checkbox option if needed

---

## Success Criteria

After implementation, these must all be true:

```
✅ Create new profile
   ├─ Profile is automatically selected (without checkbox)
   ├─ Profile switcher shows new profile as active
   ├─ _activeProfile = newProfile
   ├─ _currentProfileId = newProfile.Id
   └─ UI shows new profile's name in header

✅ Activities automatically reload
   ├─ Feed refreshes to show new profile's posts
   ├─ Old profile's posts not shown
   ├─ Activity count matches new profile
   └─ Pagination resets to page 1

✅ Multiple profile scenarios
   ├─ Create Personal → feed updates ✓
   ├─ Create Business → feed updates ✓
   ├─ Create Organization → feed updates ✓
   └─ Activity associations work correctly ✓
```

---

## Summary of Findings

| Aspect | Current State | Required State | Location |
|--------|---------------|----------------|----------|
| Auto-Select Profile | Only if checkbox | Always | HandleCreateProfile() |
| Reload Activities | No | Yes | HandleCreateProfile() |
| Update _currentProfileId | No | Yes | HandleCreateProfile() |
| SetAsActive Default | false | true (optional) | ProfileCreatorModal.razor |
| LoadFeedPostsAsync() Called | No | Yes | HandleCreateProfile() |
| _currentPage Reset | No | Yes | HandleCreateProfile() |

---

## Next Steps (When Ready to Implement)

1. Modify `Home.HandleCreateProfile()` to:
   - Always select new profile
   - Update `_currentProfileId`
   - Reload activities via `LoadFeedPostsAsync()`
   - Reset `_currentPage = 1`

2. Optionally update `ProfileCreatorModal.razor`:
   - Change `SetAsActive = false` to `SetAsActive = true`
   - Or remove checkbox entirely

3. Test all scenarios:
   - Different profile types
   - Activity display
   - Profile switching
   - Pagination
