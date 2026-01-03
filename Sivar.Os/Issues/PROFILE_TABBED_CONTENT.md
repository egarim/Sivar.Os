# Profile Page Improvement Plan - Tabbed Content View

## Overview

This plan outlines the implementation of a tabbed/filtered content view for the Profile Page, allowing users to view activity streams and filter content by type.

---

## Current State

- **ProfilePage** displays: Profile Card (avatar, name, bio, stats) + Posts list (all types mixed together)
- No way to filter content by type
- No distinct activity stream view

---

## Goal

Add filter pills/chips to organize profile content:
1. **Activity** - All content chronologically (current behavior, but will include reactions + follows + comments)
2. **Content by Type** - Separate filters for each content type the profile can create

---

## Confirmed Requirements

### 1. Content Types per Profile Type ✅ Confirmed Accurate

| Profile Type | Available Content Types |
|--------------|------------------------|
| **Personal** | Posts (General), Blogs |
| **Business** | Posts (General), Blogs, Products, Services, Branches/Offices (BusinessLocation), Events, Jobs |
| **Organization** | Posts (General), Blogs, Events, Jobs |

### 2. UI Choice: **Option B - MudChips/Filter Pills**

```
◉ Activity  ○ Posts  ○ Blogs  ○ Products  ○ Services  ○ Locations  ○ Events  ○ Jobs
─────────────────────────────────────────────────────────────────────────────────────
Content area
```

**Reasons:**
- More compact
- Works well on mobile (can wrap to multiple lines)
- Familiar filter pattern

### 3. Tab Visibility: **Show All Applicable Tabs**

- Show all tabs for the profile type, even if empty
- Display "No [content type] yet" message for empty tabs
- This makes the profile capabilities clear to viewers

### 4. Filtering Approach: **Server-Side Filtering (Option B)**

- Add `postType` parameter to `GetPostsByProfile` endpoint
- More efficient, especially with pagination
- Better for profiles with many posts

**Required Backend Changes:**
1. Modify `GetPostsByProfile` endpoint to accept optional `postType` filter
2. Update `IPostService.GetPostsByProfileAsync` to support type filtering
3. Update `PostService` implementation

### 5. Activity Stream: **Full Activity Stream (Option B)**

The "Activity" tab should include:
- Posts
- Reactions given
- Follows made
- Comments posted

**Required Backend Changes:**
1. Create new `GetProfileActivityAsync` endpoint
2. Return a unified activity feed with different activity types
3. Create `ActivityItemDto` to represent different activity types

### 6. Business Content: **All Content Types are PostTypes** ✅ Confirmed

Based on codebase review:
- **No separate entities** for Products, Services, Branches/Offices
- All are stored as **Posts with specific PostType** enum values:
  - `PostType.General` (1)
  - `PostType.BusinessLocation` (2) - Branches/Offices
  - `PostType.Product` (3)
  - `PostType.Service` (4)
  - `PostType.Event` (5)
  - `PostType.JobPosting` (6)
  - `PostType.Blog` (7)

- Business-specific metadata stored in:
  - `Post.PricingInfo` (JSON) - for Products/Services
  - `Post.BusinessMetadata` (JSON) - type-specific metadata
  - `Post.AvailabilityStatus` - availability enum

This simplifies implementation - we only need to filter by `PostType`.

---

## Implementation Plan

### Phase 1: Backend - Add PostType Filtering ✅ COMPLETED

**Files Modified:**
1. ✅ `Sivar.Os/Controllers/PostsController.cs`
   - Added `postType` query parameter to `GetPostsByProfile`

2. ✅ `Sivar.Os.Shared/Repositories/IPostRepository.cs`
   - Updated `GetByProfileAsync` signature to include `PostType?` filter

3. ✅ `Sivar.Os.Data/Repositories/PostRepository.cs`
   - Implemented filtering by PostType

4. ✅ `Sivar.Os.Shared/Services/IPostService.cs`
   - Updated service interface

5. ✅ `Sivar.Os/Services/PostService.cs`
   - Pass through to repository

6. ✅ `Sivar.Os.Shared/Clients/IPostsClient.cs`
   - Updated client interface

7. ✅ `Sivar.Os.Client/Clients/PostsClient.cs`
   - Updated client implementation

8. ✅ `Sivar.Os/Services/Clients/PostsClient.cs`
   - Updated server-side client

**Completed:** Phase 1 done, all tests pass

---

### Phase 2: Backend - Create Activity Stream Endpoint

**Status:** Not started - Activity tab currently shows all posts (same as before)

**Files to Create/Modify:**
1. `Sivar.Os.Shared/DTOs/ActivityItemDto.cs` (NEW)
   - Create unified activity DTO

2. `Sivar.Os.Shared/Services/IActivityService.cs` (NEW)
   - Define activity service interface

3. `Sivar.Os/Services/ActivityService.cs` (NEW)
   - Implement activity aggregation

4. `Sivar.Os/Controllers/ActivityController.cs` (NEW)
   - Expose activity endpoint

5. `Sivar.Os.Shared/Clients/IActivityClient.cs` (NEW)
   - Client interface

6. `Sivar.Os.Client/Clients/ActivityClient.cs` (NEW)
   - Client implementation

**Estimated Effort:** 3-4 hours

---

### Phase 3: UI - Profile Content Tabs Component ✅ COMPLETED

**Files Created/Modified:**
1. ✅ `Sivar.Os.Client/Components/Profile/ProfileContentTabs.razor` (NEW)
   - Reusable tab/chip component for content filtering
   - Props: ProfileTypeName, SelectedTab, SelectedTabChanged
   - Dynamically shows tabs based on profile type
   - Includes static `GetPostTypeFromTab()` method for tab→PostType conversion

2. ✅ `Sivar.Os.Client/Resources/ProfileContentTabs.resx` (NEW)
   - English localization

3. ✅ `Sivar.Os.Client/Resources/ProfileContentTabs.es.resx` (NEW)
   - Spanish localization

4. ✅ `Sivar.Os.Client/Pages/ProfilePage.razor`
   - Added `viewedProfileTypeName` state (from ProfileDto.ProfileType.Name)
   - Added `selectedContentTab` state
   - Integrated ProfileContentTabs component
   - Added `OnContentTabChanged` handler
   - Modified `LoadPostsAsync` to use selected tab's PostType filter
   - Modified `LoadMorePosts` to use selected tab's PostType filter
   - Added `using Sivar.Os.Shared.Enums` for PostType

**Completed:** Phase 3 done, all tests pass, builds successfully

---

### Phase 4: UI - Content Type Display

**Files to Create/Modify:**
1. `Sivar.Os.Client/Components/Profile/ProfileActivityFeed.razor` (NEW)
   - Display unified activity stream
   - Different card layouts for different activity types

2. `Sivar.Os.Client/Components/Profile/ProfilePostsByType.razor` (NEW)
   - Display posts filtered by type
   - Reuse existing PostCard component
   - Handle empty states per type

3. Update `ProfilePage.razor`
   - Switch between Activity and Posts views
   - Handle loading/error states per tab

**Estimated Effort:** 3-4 hours

---

### Phase 5: Polish & UX

**Enhancements:**
1. Add content counts per tab: "Posts (12)", "Blogs (3)"
2. URL state for selected tab: `/jose-ojeda?tab=blogs`
3. Mobile responsive adjustments
4. Loading skeletons per tab
5. Localization for all new strings

**Estimated Effort:** 2-3 hours

---

## Tab Configuration by Profile Type

```csharp
// Pseudo-code for tab configuration
var tabs = new Dictionary<string, List<ContentTab>>
{
    ["PersonalProfile"] = new()
    {
        new("activity", "Activity", Icons.Material.Filled.Timeline),
        new("general", "Posts", Icons.Material.Filled.Article),
        new("blog", "Blogs", Icons.Material.Filled.EditNote),
    },
    ["BusinessProfile"] = new()
    {
        new("activity", "Activity", Icons.Material.Filled.Timeline),
        new("general", "Posts", Icons.Material.Filled.Article),
        new("blog", "Blogs", Icons.Material.Filled.EditNote),
        new("product", "Products", Icons.Material.Filled.Storefront),
        new("service", "Services", Icons.Material.Filled.HeadsetMic),
        new("businesslocation", "Locations", Icons.Material.Filled.LocationOn),
        new("event", "Events", Icons.Material.Filled.Event),
        new("jobposting", "Jobs", Icons.Material.Filled.Work),
    },
    ["OrganizationProfile"] = new()
    {
        new("activity", "Activity", Icons.Material.Filled.Timeline),
        new("general", "Posts", Icons.Material.Filled.Article),
        new("blog", "Blogs", Icons.Material.Filled.EditNote),
        new("event", "Events", Icons.Material.Filled.Event),
        new("jobposting", "Jobs", Icons.Material.Filled.Work),
    }
};
```

---

## API Endpoints Summary

### Modified Endpoints

```http
GET /api/posts/profile/{profileId}?page=0&pageSize=20&postType=Blog
```
- Adds optional `postType` filter parameter

### New Endpoints

```http
GET /api/activity/profile/{profileId}?page=0&pageSize=20
```
- Returns unified activity stream (posts, reactions, follows, comments)

---

## Data Models

### ActivityItemDto (New)

```csharp
public class ActivityItemDto
{
    public Guid Id { get; set; }
    public ActivityType Type { get; set; } // Post, Reaction, Follow, Comment
    public DateTime Timestamp { get; set; }
    
    // For Post activities
    public PostDto? Post { get; set; }
    
    // For Reaction activities
    public ReactionActivityDto? Reaction { get; set; }
    
    // For Follow activities
    public FollowActivityDto? Follow { get; set; }
    
    // For Comment activities
    public CommentActivityDto? Comment { get; set; }
}

public enum ActivityType
{
    Post = 1,
    Reaction = 2,
    Follow = 3,
    Comment = 4
}
```

---

## Total Estimated Effort

| Phase | Effort |
|-------|--------|
| Phase 1: PostType Filtering | 1-2 hours |
| Phase 2: Activity Stream API | 3-4 hours |
| Phase 3: UI Tab Component | 2-3 hours |
| Phase 4: Content Display | 3-4 hours |
| Phase 5: Polish | 2-3 hours |
| **Total** | **11-16 hours** |

---

## Implementation Order

1. **Phase 1** - Add PostType filtering (backend) ← Start here
2. **Phase 3** - Create UI tab component (can work in parallel)
3. **Phase 4** - Wire up filtered posts display
4. **Phase 2** - Add activity stream (can be done later)
5. **Phase 5** - Polish and enhance

This order allows us to have a working filtered view quickly, then enhance with the full activity stream.

---

## Open Questions

None at this time. All requirements have been confirmed.

---

## Next Steps

1. Review and approve this plan
2. Create a feature branch: `feature/profile-content-tabs`
3. Begin Phase 1 implementation
