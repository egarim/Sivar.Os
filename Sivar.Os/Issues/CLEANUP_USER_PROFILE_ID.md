# User ID vs Profile ID Cleanup Plan

## Problem Summary

The codebase has confusion between `User ID` and `Profile ID`:
- **User ID**: Identifies the authenticated user account (from Keycloak)
- **Profile ID**: Identifies a specific profile owned by a user (users can have multiple profiles)

Post ownership checks compare `Post.Profile.Id` with the current user's **Profile ID**, but the variable naming and parameter naming suggest "User ID", causing bugs and confusion.

## Issues Found

### 1. ProfilePage.razor - Hardcoded Empty ID (HIGH PRIORITY)
**File:** `Sivar.Os.Client/Pages/ProfilePage.razor`  
**Line:** 366  
**Issue:** `CurrentUserId="@Guid.Empty"` is hardcoded, preventing users from editing/deleting their posts on their profile page.  
**Fix:** Change to `CurrentUserId="@(currentUserActiveProfileId ?? Guid.Empty)"`

### 2. Home.razor - Inconsistent Variable Usage (MEDIUM PRIORITY)
**File:** `Sivar.Os.Client/Pages/Home.razor`  
**Issue:** The `_currentUserId` variable sometimes holds User ID and sometimes Profile ID:

| Line | Code | Problem |
|------|------|---------|
| 261 | `_currentUserId = activeProf.Id` | Stores Profile ID in User ID variable |
| 344 | `_currentUserId = result.User?.Id` | Correctly stores User ID |
| 1329 | `_currentUserId = userDto.Id` | Correctly stores User ID |
| 1354 | `_currentUserId = activeProfile.Id` | Stores Profile ID in User ID variable |

**Fix:** Remove the workaround lines (261, 1354) that store Profile ID in the User ID variable. The `_currentProfileId` variable should be used exclusively for ownership checks.

### 3. Misleading Parameter Names (LOW PRIORITY - REFACTOR)
**Files:** `PostCard.razor`, `BlogCard.razor`  
**Issue:** Parameter `CurrentUserId` actually expects a Profile ID (compared with `Post.Profile.Id`)  
**Fix:** Rename `CurrentUserId` → `CurrentProfileId` across all files:
- `Sivar.Os.Client/Components/Feed/PostCard.razor`
- `Sivar.Os.Client/Components/Feed/BlogCard.razor`
- `Sivar.Os.Client/Pages/Home.razor`
- `Sivar.Os.Client/Pages/ProfilePage.razor`
- `Sivar.Os.Client/Pages/PostDetail.razor`

## Implementation Steps

### Step 1: Fix ProfilePage.razor (Immediate)
```diff
- CurrentUserId="@Guid.Empty"
+ CurrentUserId="@(currentUserActiveProfileId ?? Guid.Empty)"
```

### Step 2: Clean Up Home.razor Variables
Remove the workaround assignments that store Profile ID in User ID variable:

**Line 261 - Remove or comment out:**
```diff
  _currentProfileId = activeProf.Id;
- _currentUserId = activeProf.Id; // Set current user ID for ownership checks
```

**Line 1354 - Remove or comment out:**
```diff
  _currentProfileId = activeProfile.Id;
- _currentUserId = activeProfile.Id; // Use profile ID for post ownership checks
```

### Step 3: Rename Parameters (Optional Refactor)
Rename `CurrentUserId` to `CurrentProfileId` in components for clarity:

**PostCard.razor:**
```diff
- public Guid CurrentUserId { get; set; }
- private bool IsCurrentUserOwner => Post?.Profile?.Id == CurrentUserId;
+ public Guid CurrentProfileId { get; set; }
+ private bool IsCurrentUserOwner => Post?.Profile?.Id == CurrentProfileId;
```

**BlogCard.razor:**
```diff
- public Guid CurrentUserId { get; set; }
+ public Guid CurrentProfileId { get; set; }
```

**Update all usages in pages:**
```diff
- CurrentUserId="@_currentProfileId"
+ CurrentProfileId="@_currentProfileId"
```

## Testing Checklist

After implementation, verify:
- [ ] Can edit own posts on Home feed
- [ ] Can delete own posts on Home feed
- [ ] Can edit own posts on Profile page
- [ ] Can delete own posts on Profile page
- [ ] Can edit own posts on Post detail page
- [ ] Cannot edit/delete other users' posts
- [ ] Menu shows Edit/Delete options only for own posts

## Files to Modify

| File | Changes |
|------|---------|
| `Sivar.Os.Client/Pages/ProfilePage.razor` | Fix hardcoded Guid.Empty |
| `Sivar.Os.Client/Pages/Home.razor` | Remove workaround assignments |
| `Sivar.Os.Client/Components/Feed/PostCard.razor` | Rename parameter (optional) |
| `Sivar.Os.Client/Components/Feed/BlogCard.razor` | Rename parameter (optional) |
| `Sivar.Os.Client/Pages/PostDetail.razor` | Update parameter name (optional) |
