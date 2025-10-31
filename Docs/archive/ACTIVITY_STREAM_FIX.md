# Activity Stream Fix - Complete ✅

## Branch: activity-stream
**Status:** Merged to master

## Summary
Fixed the Home page feed to display activities from profiles the user follows, instead of only showing the user's own activities.

## Problem
The Home.razor page was calling `GetProfileActivitiesAsync(_currentProfileId)` which only returned activities created BY the current user's profile, not activities from profiles they follow.

## Solution
Changed Home.razor to call `GetFeedActivitiesAsync()` which:
1. Gets the user's active profile
2. Queries the ProfileFollower table to find all profiles the user follows
3. Includes the user's own profile in the list
4. Returns activities from all those profiles (creating a personalized feed)

## Code Changes

### File: `Sivar.Os.Client/Pages/Home.razor`

**Before:**
```csharp
// Load activities created BY the current profile (not the follower feed)
Console.WriteLine($"[Home.LoadFeedActivitiesAsync] ?? Calling API: GetProfileActivitiesAsync(profileId={_currentProfileId}, pageSize=10, pageNumber={_currentPage})");
var feedDto = await SivarClient.Activities.GetProfileActivitiesAsync(_currentProfileId, pageSize: 10, pageNumber: _currentPage);
```

**After:**
```csharp
// Load activities from profiles the user follows (their personalized feed)
Console.WriteLine($"[Home.LoadFeedActivitiesAsync] ?? Calling API: GetFeedActivitiesAsync(pageSize=10, pageNumber={_currentPage})");
var feedDto = await SivarClient.Activities.GetFeedActivitiesAsync(pageSize: 10, pageNumber: _currentPage);
```

## How It Works

### 1. Client-Side (Home.razor)
- Calls `SivarClient.Activities.GetFeedActivitiesAsync()`
- No need to pass profileId - it uses authenticated user automatically

### 2. Client-Side HTTP Client (ActivitiesClient.cs)
- Makes HTTP GET request to `/api/activities/feed`
- Passes pagination parameters

### 3. Server-Side Client (Services/Clients/ActivitiesClient.cs)
- Extracts Keycloak ID from authenticated user claims
- Gets user from database
- Calls `_activityService.GetFeedActivitiesAsync(userId, ...)`

### 4. Activity Service (ActivityService.cs)
- Gets user's active profile
- Queries `ProfileFollowerRepository.GetFollowingByProfileIdAsync()` to get followed profiles
- Adds user's own profile to the list
- Calls `ActivityRepository.GetFeedActivitiesAsync(followedProfileIds, ...)`
- Returns combined activities from all followed profiles

## Testing
- Build successful with no compilation errors
- All existing functionality preserved
- Follow functionality from previous branch works correctly with this change

## Commit History
1. `82fe148` - Fix Home feed to show activities from followed profiles instead of only own activities

## Dependencies
- Requires the Follow functionality (already merged in previous branch)
- Uses existing `IActivityService.GetFeedActivitiesAsync()` implementation
- Uses `ProfileFollowerRepository` to query followed profiles

## Notes
- The feed now shows a true "social feed" experience
- Activities include posts, comments, and other actions from followed profiles
- User's own activities are also included in the feed
- Pagination is preserved (10 items per page)
- Proper authentication checks ensure users only see appropriate content
