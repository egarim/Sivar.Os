# Research: Auto-Select New Profile & Auto-Reload Activities

## Current Implementation Status

### ✅ What's Already Working

1. **SetAsActive Property** (CreateAnyProfileDto.cs)
   - Property exists: `public bool SetAsActive { get; set; } = false;`
   - User can check checkbox in ProfileCreatorModal to set as active
   - Location: Line 425 in ProfileDto.cs

2. **Profile Modal Checkbox** (ProfileCreatorModal.razor)
   - User can check "Set as Active" checkbox (Line 86)
   - Property bound to component: `@bind="SetAsActive"` 
   - Gets passed in CreateAnyProfileDto (Line 531)

3. **Backend Support** (ProfileService.cs)
   - ProfileService already sets first profile as active automatically
   - Code at line 813-822 shows:
     ```csharp
     if (userProfiles.Count() == 1)
     {
         // If this is the user's first profile, automatically set it as active
         var enforceResult = await EnforceOneActiveProfileRuleAsync(profile.Id, user.Id);
     }
     ```

### ❌ What's NOT Working / Missing

1. **New Profile Not Selected After Creation**
   - Home.HandleCreateProfile (Line 3040) does:
     ```csharp
     if (request.SetAsActive)
     {
         // Only calls SetMyActiveProfileAsync if user checked the checkbox
         await SivarClient.Profiles.SetMyActiveProfileAsync(newProfile.Id);
         _activeProfile = newProfile;
     }
     ```
   - ❌ PROBLEM: Only works if user explicitly checked "Set as Active"
   - ❌ Should ALWAYS select new profile (by default) without needing checkbox

2. **Activities/Feed NOT Reloaded After Profile Selection**
   - Home.HandleCreateProfile calls LoadUserProfilesAsync() (Line 3064)
   - But it does NOT update `_currentProfileId` to the new profile
   - So feed stays showing old profile's activities
   
   Current flow:
   ```
   New profile created
      ↓
   Reload user profiles list
      ↓
   MISSING: Update _currentProfileId to new profile
      ↓
   MISSING: Call LoadFeedPostsAsync() to reload feed
      ↓
   Result: UI doesn't show new profile's activities
   ```

---

## Data Structure Analysis

### Feed Loading Dependency

**LoadFeedPostsAsync()** (Line 2721) requires:
```csharp
private async Task LoadFeedPostsAsync()
{
    if (_currentProfileId == Guid.Empty)
    {
        // Can't load feed without profile ID
        _posts = new List<PostDto>();
        return;
    }
    
    // Then loads feed using _currentProfileId
    var feedDto = await SivarClient.Posts.GetFeedPostsAsync(pageSize: 10, pageNumber: _currentPage);
}
```

**Key Variables:**
- `_currentProfileId` (Line 1728) - Current active profile ID
- `_posts` (Line 1745) - List of posts/activities for current profile
- `_totalPages` - Pagination info

### Profile Switching Pattern

When user manually switches profile via **HandleProfileChanged()** (Line 3010):
```csharp
private async Task HandleProfileChanged(ProfileDto selectedProfile)
{
    if (await ProfileSwitcherService.SwitchProfileAsync(selectedProfile.Id))
    {
        _activeProfile = selectedProfile;
        _currentProfileId = selectedProfile.Id;  // ← Updates current profile
        
        // Then reloads feed for new profile
        await LoadFeedPostsAsync();  // ← Reloads activities
        StateHasChanged();
    }
}
```

---

## What Needs to Happen

### Requirement 1: Auto-Select New Profile (Not Optional)

**Current**:
```csharp
// New profile created
if (request.SetAsActive)  // ← Only if user checked box!
{
    await SivarClient.Profiles.SetMyActiveProfileAsync(newProfile.Id);
    _activeProfile = newProfile;
}
```

**Should Be**:
```csharp
// New profile created - ALWAYS select it (or make SetAsActive default true)
// Option A: Always set as active
await SivarClient.Profiles.SetMyActiveProfileAsync(newProfile.Id);
_activeProfile = newProfile;
_currentProfileId = newProfile.Id;  // ← Important!

// Option B: Change default checkbox to checked
// In ProfileCreatorModal: SetAsActive = true by default instead of false
```

### Requirement 2: Reload Activities/Feed After Profile Selection

**Missing Code**:
```csharp
private async Task HandleCreateProfile(CreateAnyProfileDto request)
{
    var newProfile = await SivarClient.Profiles.CreateProfileAsync(request);
    
    if (newProfile != null)
    {
        // NEW: Always set as active (or if checkbox was checked)
        _currentProfileId = newProfile.Id;  // ← Set current profile ID
        _activeProfile = newProfile;
        
        // NEW: Reload feed/activities for new profile
        _currentPage = 1;  // Reset to page 1
        await LoadFeedPostsAsync();  // ← Reload activities!
        
        // Reload profiles list
        await LoadUserProfilesAsync();
        
        StateHasChanged();
    }
}
```

---

## Current Code Flow Diagram

### Profile Creation Flow (Current)

```
ProfileCreatorModal.SubmitForm()
    ↓
Creates CreateAnyProfileDto { ProfileTypeId, DisplayName, SetAsActive }
    ↓
Home.HandleCreateProfile(request)
    ↓
SivarClient.Profiles.CreateProfileAsync(request)
    ↓
Backend: Creates profile in database
    ↓
Home.HandleCreateProfile receives newProfile
    ↓
IF request.SetAsActive == true ← ⚠️ CONDITIONAL
    │  ├─ SetMyActiveProfileAsync(newProfile.Id)
    │  └─ Update _activeProfile
    │
LoadUserProfilesAsync()  ← Reloads profile list
    │
StateHasChanged()  ← UI refresh
    │
❌ Feed NOT reloaded!
❌ _currentProfileId NOT updated!
❌ Shows old profile's activities!
```

### Profile Switching Flow (Manual - for comparison)

```
ProfileSwitcher component
    ↓
User clicks profile
    ↓
ProfileSwitcher.HandleProfileChanged(selectedProfile)
    ↓
ProfileSwitcherService.SwitchProfileAsync(profileId)
    ↓
Home.HandleProfileChanged(selectedProfile)
    ↓
Update _activeProfile = selectedProfile
Update _currentProfileId = selectedProfile.Id  ← ✅ Correct
    ↓
LoadFeedPostsAsync()  ← ✅ Reloads feed using _currentProfileId
    ↓
StateHasChanged()
    ↓
✅ UI shows new profile's activities!
```

---

## Required Changes Summary

| Issue | Current Behavior | Required Behavior | Location |
|-------|------------------|-------------------|----------|
| **Auto-Select** | Only if user checks "Set as Active" | Always select new profile by default | Home.HandleCreateProfile (Line 3040) |
| **Feed Not Reloaded** | Loads profile list only | Must reload feed/activities | Home.HandleCreateProfile (Line 3040) |
| **_currentProfileId Not Updated** | Not set to new profile | Must update to new profile ID | Home.HandleCreateProfile (Line 3040) |
| **Checkbox Default** | SetAsActive = false by default | Should be true by default | ProfileCreatorModal.razor (Line 397) |

---

## Key Code Sections to Modify

### 1. ProfileCreatorModal.razor (Line 397)
Current:
```csharp
private bool SetAsActive { get; set; } = false;
```

Should be:
```csharp
private bool SetAsActive { get; set; } = true;  // Default to true
```

### 2. Home.razor HandleCreateProfile (Line 3040-3070)
Current:
```csharp
if (newProfile != null)
{
    if (request.SetAsActive)  // ← Conditional
    {
        await SivarClient.Profiles.SetMyActiveProfileAsync(newProfile.Id);
        _activeProfile = newProfile;
    }

    await LoadUserProfilesAsync();
    StateHasChanged();
}
```

Should be:
```csharp
if (newProfile != null)
{
    // ALWAYS set as active (new default behavior)
    _activeProfile = newProfile;
    _currentProfileId = newProfile.Id;  // ← Update current profile ID!
    
    // ALWAYS reload feed for new profile
    _currentPage = 1;  // Reset pagination
    await LoadFeedPostsAsync();  // ← Reload activities!
    
    // Reload profiles list
    await LoadUserProfilesAsync();
    
    StateHasChanged();
}
```

---

## Implementation Approach Options

### Option A: Always Auto-Select (No Checkbox)
- Remove SetAsActive checkbox from modal
- Always select new profile automatically
- Always reload feed
- Simpler UX (no decision for user)

### Option B: Change Default + Smart Behavior
- Change checkbox default to "checked" (SetAsActive = true)
- If user unchecks it, new profile won't be auto-selected
- But always reload feed anyway (so activities refresh)
- More flexible but still good UX

### Option C: Always Reload, Optional Select
- Keep checkbox as optional "Set as Active"
- Always reload feed when profile created (regardless of checkbox)
- So feed updates even if profile not selected
- User can check if they want immediate switch

---

## Testing Points

After implementation, verify:

1. **New Profile Auto-Selected**
   - [ ] Create new profile
   - [ ] New profile automatically becomes active (without checking checkbox)
   - [ ] Profile switcher shows new profile as selected

2. **Feed Automatically Reloaded**
   - [ ] Create new profile
   - [ ] Feed updates to show new profile's activities
   - [ ] Old profile's activities no longer shown
   - [ ] Activity count matches new profile

3. **Pagination Reset**
   - [ ] Create profile on page 5
   - [ ] Feed reloaded and shows page 1 of new profile
   - [ ] Pagination works correctly for new profile

4. **Multiple Profile Types**
   - [ ] Create Personal profile → feed reloads ✓
   - [ ] Create Business profile → feed reloads ✓
   - [ ] Create Organization profile → feed reloads ✓
   - [ ] Switch between profiles → feed reloads ✓

---

## Variables Involved

**In Home.razor:**
- `_currentProfileId` (Guid) - Current active profile ID (used for feed loading)
- `_activeProfile` (ProfileDto) - Currently active profile object
- `_posts` (List<PostDto>) - List of activities/feed posts
- `_currentPage` (int) - Current pagination page
- `_totalPages` (int) - Total pages in feed

**In CreateAnyProfileDto:**
- `SetAsActive` (bool) - Flag to set profile as active

**Methods to call:**
- `LoadFeedPostsAsync()` - Reloads activities for current profile
- `LoadUserProfilesAsync()` - Reloads profile list
- `SivarClient.Profiles.SetMyActiveProfileAsync()` - Sets profile as active on backend

---

## Summary

**Currently Missing:**
1. ❌ New profile not automatically selected (depends on checkbox)
2. ❌ Activities/feed not reloaded after profile creation
3. ❌ `_currentProfileId` not updated to new profile
4. ❌ Shows stale activity data for old profile

**What's Working:**
1. ✅ Profile creation succeeds
2. ✅ Profile list reloads
3. ✅ SetAsActive checkbox exists
4. ✅ Backend supports setting active profile
5. ✅ LoadFeedPostsAsync exists and works

**Solution:**
1. Update HandleCreateProfile to always select new profile
2. Update HandleCreateProfile to reload feed/activities
3. Update HandleCreateProfile to set _currentProfileId
4. Consider changing SetAsActive default to true
5. Reset pagination when loading new profile's feed
