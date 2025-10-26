# Post Functionality UI Mapping Plan

## Executive Summary

This document outlines a comprehensive plan to map all post functionality from the `IPostsClient` interface to the Blazor UI components. The goal is to ensure complete coverage of CRUD operations, feed management, search, analytics, and user interactions with proper data binding and state management.

---

## 1. Current State Analysis

### IPostsClient Interface Methods

```csharp
// CRUD operations
Task<PostDto> CreatePostAsync(CreatePostDto request, CancellationToken cancellationToken = default);
Task<PostDto> GetPostAsync(Guid postId, CancellationToken cancellationToken = default);
Task<PostDto> UpdatePostAsync(Guid postId, UpdatePostDto request, CancellationToken cancellationToken = default);
Task DeletePostAsync(Guid postId, CancellationToken cancellationToken = default);

// Feed and discovery
Task<IEnumerable<PostDto>> GetFeedPostsAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
Task<IEnumerable<PostDto>> GetProfilePostsAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
Task<IEnumerable<PostDto>> SearchPostsAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
Task<IEnumerable<PostDto>> GetTrendingPostsAsync(int pageSize = 20, CancellationToken cancellationToken = default);

// Analytics
Task<PostAnalyticsDto> GetPostAnalyticsAsync(Guid postId, CancellationToken cancellationToken = default);
Task<IEnumerable<PostActivityDto>> GetProfileActivityAsync(Guid profileId, int days = 30, CancellationToken cancellationToken = default);
```

### Existing UI Components

#### Feed Components Created:
- `PostCard.razor` - Display individual posts
- `PostHeader.razor` - Post metadata and author info
- `PostFooter.razor` - Interaction buttons (like, comment, share, save)
- `PostReactions.razor` - Display reaction pills
- `PostComments.razor` - Comment section
- `CommentItem.razor` - Individual comment display
- `PostComposer.razor` - Create new post UI
- `FeedHeader.razor` - Section header
- `PostMetadata.razor` - Additional post information

#### Page Integration:
- `Home.razor` - Main feed page with posts, composer, and sidebar

---

## 2. Data Models & DTOs

### Key DTOs to Support

1. **CreatePostDto** - For creating new posts
   - Content (string)
   - PostType (enum)
   - Visibility (enum)
   - Language (string)
   - Tags (list)
   - Location (object)
   - BusinessMetadata (string)
   - Attachments (list)

2. **PostDto** - Full post object
   - All CreatePostDto fields plus:
   - Id, Profile, ReactionSummary, Comments, CommentCount
   - CreatedAt, UpdatedAt, IsEdited, EditedAt

3. **UpdatePostDto** - For editing posts
   - Content, Visibility, Tags, Location, BusinessMetadata

4. **PostAnalyticsDto** - Post metrics
   - Views, TotalReactions, ReactionsByType, TotalComments
   - EngagementRate, PeakEngagementHour, EngagementByLocation

5. **PostActivityDto** - User activity tracking
   - Id, Post, ActivityType, Description, ActivityTime, RelatedProfile

---

## 3. UI Mapping Implementation Plan

### Phase 1: Post Creation & Editing

#### 3.1.1 Create Post (POST Endpoint)

**Component:** `PostComposer.razor`

**Mapping:**
```
PostComposer Input Fields:
├── _postText ➜ CreatePostDto.Content
├── _selectedPostType ➜ CreatePostDto.PostType
├── _visibilityLevel ➜ CreatePostDto.Visibility
├── _tags ➜ CreatePostDto.Tags
├── _selectedLanguage ➜ CreatePostDto.Language
├── _attachmentsList ➜ CreatePostDto.Attachments
└── _locationData ➜ CreatePostDto.Location

Method: HandlePostSubmitAsync() ➜ IPostsClient.CreatePostAsync()
Response: PostDto ➜ Add to _posts list
```

**Required Changes:**
- [ ] Add visibility level selector (dropdown)
- [ ] Add language selector
- [ ] Add location input component
- [ ] Add tags input component
- [ ] Implement attachment upload handler
- [ ] Add business metadata field for business profiles
- [ ] Call `CreatePostAsync()` on publish button click

**Success Criteria:**
- New post appears in feed immediately
- All post fields are properly captured
- File attachments are uploaded and linked
- Validation prevents empty/invalid posts

---

#### 3.1.2 Update Post (PUT Endpoint)

**Component:** `PostCard.razor` (with edit mode) + New `PostEditModal.razor`

**Mapping:**
```
PostEditModal Fields:
├── currentPost.Content ➜ UpdatePostDto.Content
├── currentPost.Visibility ➜ UpdatePostDto.Visibility
├── currentPost.Tags ➜ UpdatePostDto.Tags
├── currentPost.Location ➜ UpdatePostDto.Location
└── currentPost.BusinessMetadata ➜ UpdatePostDto.BusinessMetadata

Method: HandleEditAsync() ➜ IPostsClient.UpdatePostAsync(postId, updateDto)
Response: PostDto ➜ Update in _posts list
```

**Required Changes:**
- [ ] Add edit button to `PostFooter.razor` for post owner
- [ ] Create `PostEditModal.razor` component
- [ ] Populate edit form with existing post data
- [ ] Call `UpdatePostAsync()` on save
- [ ] Update local state after successful edit
- [ ] Show "Edited" badge on edited posts

**Success Criteria:**
- Post owner can edit their posts
- EditedAt timestamp is displayed
- Changes reflect immediately in UI
- Edit history tracking is optional for Phase 1

---

#### 3.1.3 Delete Post (DELETE Endpoint)

**Component:** `PostCard.razor` (with more options menu) + `PostMoreMenu.razor`

**Mapping:**
```
Post More Options Menu:
├── Edit (if owner)
├── Delete (if owner)
├── Report (if not owner)
└── Share Link

Method: HandleDeleteAsync() ➜ IPostsClient.DeletePostAsync(postId)
Response: Success ➜ Remove from _posts list
```

**Required Changes:**
- [ ] Add more options button to `PostFooter.razor`
- [ ] Create `PostMoreMenu.razor` component
- [ ] Add delete confirmation dialog
- [ ] Call `DeletePostAsync()` on confirm
- [ ] Remove post from feed after successful deletion

**Success Criteria:**
- Only post owner can delete
- Confirmation dialog prevents accidental deletion
- Post is removed from feed immediately
- Error handling for failed deletions

---

### Phase 2: Feed & Discovery

#### 3.2.1 Get Feed Posts (GET Endpoint)

**Component:** `Home.razor`

**Mapping:**
```
Home.razor State:
├── _posts ➜ IEnumerable<PostDto>
├── _currentPage ➜ pageNumber parameter
├── _pageSize ➜ pageSize parameter
└── _totalPages ➜ Calculate from response count

Method: LoadFeedPostsAsync() ➜ IPostsClient.GetFeedPostsAsync(pageSize, pageNumber)
Response: IEnumerable<PostDto> ➜ Set _posts, update pagination
```

**Current Status:** ✅ Already implemented in `Home.razor`

**Improvements Needed:**
- [ ] Handle empty feed state gracefully
- [ ] Add skeleton loading during fetch
- [ ] Implement infinite scroll as alternative to pagination
- [ ] Add refresh button for manual feed update
- [ ] Cache posts to prevent redundant API calls

---

#### 3.2.2 Get Profile Posts (GET Endpoint)

**Component:** New `ProfileFeed.razor` page

**Mapping:**
```
ProfileFeed.razor State:
├── _profileId ➜ Route parameter
├── _posts ➜ IEnumerable<PostDto>
├── _currentPage ➜ pageNumber parameter
└── _profile ➜ Profile information

Method: LoadProfilePostsAsync() ➜ IPostsClient.GetProfilePostsAsync(profileId, pageSize, pageNumber)
Response: IEnumerable<PostDto> ➜ Set _posts for profile
```

**Required Changes:**
- [ ] Create `ProfileFeed.razor` page component
- [ ] Add route parameter binding for profile ID
- [ ] Implement pagination for profile posts
- [ ] Display profile header information
- [ ] Show post count for profile
- [ ] Filter posts by profile owner

**Success Criteria:**
- Profile posts display in dedicated feed
- Pagination works correctly
- Only posts from selected profile shown
- Navigation between profile and main feed smooth

---

#### 3.2.3 Search Posts (GET Endpoint)

**Component:** New `SearchFeed.razor` page + `SearchBar.razor`

**Mapping:**
```
SearchBar Input:
├── _searchQuery ➜ IPostsClient.SearchPostsAsync(query)

SearchFeed Results:
├── _posts ➜ IEnumerable<PostDto>
├── _searchQuery ➜ Display in header
├── _currentPage ➜ pageNumber parameter
└── _searchResults ➜ Result count display

Method: PerformSearchAsync() ➜ IPostsClient.SearchPostsAsync(query, pageSize, pageNumber)
Response: IEnumerable<PostDto> ➜ Set _posts with results
```

**Required Changes:**
- [ ] Add search bar to header component
- [ ] Create `SearchFeed.razor` page
- [ ] Implement search result caching
- [ ] Add search filters (date range, post type, visibility)
- [ ] Show "no results" state
- [ ] Add search history
- [ ] Display result count and page info

**Success Criteria:**
- Search finds posts by content, tags, author
- Results are paginated
- Search is case-insensitive
- Performance acceptable for large result sets
- "No results" state is user-friendly

---

#### 3.2.4 Get Trending Posts (GET Endpoint)

**Component:** New `TrendingFeed.razor` page or widget in `Home.razor`

**Mapping:**
```
TrendingFeed/Widget:
├── _posts ➜ IEnumerable<PostDto> (trending)
├── _refreshInterval ➜ Auto-refresh configuration
└── _sortBy ➜ Trending algorithm (reactions, comments, etc.)

Method: LoadTrendingPostsAsync() ➜ IPostsClient.GetTrendingPostsAsync(pageSize)
Response: IEnumerable<PostDto> ➜ Set _posts with trending
```

**Required Changes:**
- [ ] Create trending posts widget or page
- [ ] Implement auto-refresh mechanism (every 5-10 minutes)
- [ ] Add sorting options (by reactions, comments, views)
- [ ] Display trending badges on posts
- [ ] Show "trending since" timestamp
- [ ] Add trending posts to sidebar or separate tab

**Success Criteria:**
- Trending posts display and update regularly
- Top posts bubble up in feed
- Trending criteria clearly explained to user
- Widget integrates well with existing UI

---

### Phase 3: Interactions & Analytics

#### 3.3.1 Like/React to Post

**Component:** `PostReactions.razor` + `PostFooter.razor`

**Mapping:**
```
Reaction Buttons:
├── Like/Reaction Pills ➜ IReactionsClient (separate interface)
└── ReactionSummary ➜ PostDto.ReactionSummary

Note: This likely uses IReactionsClient, not IPostsClient
See: IReactionsClient for AddReactionAsync(), RemoveReactionAsync()
```

**Current Status:** ⚠️ Partially implemented - needs `IReactionsClient` integration

---

#### 3.3.2 Comment on Post

**Component:** `PostComments.razor` + `CommentItem.razor`

**Mapping:**
```
Comment System:
├── Comments Input ➜ ICommentsClient.CreateCommentAsync()
├── Comments Display ➜ PostDto.Comments collection
└── Comment Count ➜ PostDto.CommentCount

Note: This likely uses ICommentsClient, not IPostsClient
See: ICommentsClient for CRUD operations
```

**Current Status:** ⚠️ Partially implemented - needs `ICommentsClient` integration

---

#### 3.3.3 Get Post Analytics (GET Endpoint)

**Component:** New `PostAnalytics.razor` modal/page

**Mapping:**
```
Analytics Display:
├── Views Count ➜ PostAnalyticsDto.Views
├── Reactions ➜ PostAnalyticsDto.ReactionsByType
├── Comments ➜ PostAnalyticsDto.TotalComments
├── EngagementRate ➜ PostAnalyticsDto.EngagementRate
├── PeakHour ➜ PostAnalyticsDto.PeakEngagementHour
└── GeoDistribution ➜ PostAnalyticsDto.EngagementByLocation

Method: LoadPostAnalyticsAsync(postId) ➜ IPostsClient.GetPostAnalyticsAsync(postId)
Response: PostAnalyticsDto ➜ Display in analytics view
```

**Required Changes:**
- [ ] Create `PostAnalytics.razor` modal component
- [ ] Add analytics button to `PostFooter.razor` for post owner
- [ ] Display metrics in dashboard format
- [ ] Add chart visualization for trends over time
- [ ] Show geographic distribution map
- [ ] Show peak engagement hours
- [ ] Add date range selector for analytics

**Success Criteria:**
- Analytics available only to post owner
- Charts and visualizations display correctly
- Data updates regularly
- Performance acceptable for large datasets

---

#### 3.3.4 Get Profile Activity (GET Endpoint)

**Component:** New `ProfileActivity.razor` widget or page

**Mapping:**
```
Activity Display:
├── Activity Timeline ➜ IEnumerable<PostActivityDto>
├── Activity Type ➜ PostActivityDto.ActivityType (filter)
├── Date Range ➜ days parameter (default 30)
└── Activity Cards ➜ PostActivityDto properties

Method: LoadProfileActivityAsync(profileId) ➜ IPostsClient.GetProfileActivityAsync(profileId, days)
Response: IEnumerable<PostActivityDto> ➜ Display timeline
```

**Required Changes:**
- [ ] Create `ProfileActivity.razor` component
- [ ] Add activity timeline view
- [ ] Filter activities by type (posts, reactions, follows, etc.)
- [ ] Add date range selector
- [ ] Group activities by date
- [ ] Show related profiles
- [ ] Add activity export feature

**Success Criteria:**
- Activity timeline displays chronologically
- Filters work correctly
- Related profiles are clickable
- Large activity histories load efficiently

---

### Phase 4: Post Metadata & Features

#### 3.4.1 Post Type Display

**Component:** `PostCard.razor` + `PostHeader.razor`

**Mapping:**
```
Post Type Badge:
├── PostType Enum ➜ PostDto.PostType
├── Display Label ➜ Type-specific styling
└── Color/Icon ➜ CSS badge classes

Types:
├── General ➜ Blue badge
├── Product ➜ Purple badge
├── Service ➜ Green badge
├── Event ➜ Orange badge
└── Job ➜ Pink badge
```

**Current Status:** ✅ Already implemented with styling

**Improvements:**
- [ ] Add type-specific filtering to feed
- [ ] Add post type selector in composer

---

#### 3.4.2 Visibility Levels

**Component:** `PostCard.razor` + `PostComposer.razor`

**Mapping:**
```
Visibility Control:
├── Visibility Badge ➜ PostDto.Visibility
├── Visibility Selector ➜ Composer dropdown
└── Filtering ➜ Feed by visibility

Levels: Public, Followers Only, Private, Business Only
```

**Required Changes:**
- [ ] Add visibility selector to composer
- [ ] Display visibility badge on posts
- [ ] Filter feed by visibility permissions
- [ ] Show visibility icon/label clearly

---

#### 3.4.3 Tags & Location

**Component:** `PostCard.razor` + `PostComposer.razor`

**Mapping:**
```
Tags Display:
├── Tag Badges ➜ PostDto.Tags array
├── Clickable Tags ➜ Navigate to tag search
└── Tag Input ➜ CreatePostDto.Tags

Location Display:
├── Location Badge ➜ PostDto.Location
├── Location Link ➜ Navigate to location filter
└── Location Input ➜ Map picker or text input
```

**Required Changes:**
- [ ] Add clickable tag links
- [ ] Add tag search functionality
- [ ] Display location information
- [ ] Add location picker to composer
- [ ] Show map for location-based posts

---

#### 3.4.4 Attachments Handling

**Component:** `PostCard.razor` + `PostComposer.razor`

**Mapping:**
```
Attachment Display:
├── Image Gallery ➜ PostAttachmentDto list
├── Media Type Icon ➜ AttachmentType enum
├── Download Link ➜ FilePath
└── Alt Text ➜ PostAttachmentDto.AltText

Attachment Upload:
├── File Input ➜ CreatePostAttachmentDto
├── Progress Bar ➜ Upload progress
└── Preview ➜ File preview before post
```

**Current Status:** ⚠️ Partial - only displays first attachment

**Required Changes:**
- [ ] Add multiple attachment gallery
- [ ] Add attachment upload with progress
- [ ] Add drag-and-drop upload
- [ ] Show file type icons
- [ ] Implement video preview
- [ ] Add accessibility (alt text required)

---

### Phase 5: Edge Cases & Error Handling

#### 3.5.1 Error Handling

**Across all Components:**
- [ ] Network error messages
- [ ] Validation error display
- [ ] 404 post not found
- [ ] Unauthorized access (not post owner)
- [ ] Retry mechanisms

#### 3.5.2 Loading States

**Components:**
- [ ] Skeleton loaders for posts
- [ ] Spinner during API calls
- [ ] Disabled buttons during operations
- [ ] Loading indicators for infinite scroll

#### 3.5.3 Empty States

**Scenarios:**
- [ ] No posts in feed
- [ ] No search results
- [ ] No profile posts
- [ ] No trending posts
- [ ] No comments on post
- [ ] No activity history

---

## 4. Implementation Priority Matrix

### High Priority (MVP)
1. ✅ Get Feed Posts - Already implemented
2. ⏳ Create Post - Needs finalization
3. ⏳ Delete Post - Needs implementation
4. ⏳ Update Post - Needs implementation
5. ⏳ Post Display - Improve existing components

### Medium Priority (Phase 2)
6. Search Posts
7. Get Profile Posts
8. Post Analytics
9. Trending Posts
10. Profile Activity

### Low Priority (Phase 3)
11. Advanced filtering
12. Export functionality
13. Post scheduling
14. Analytics charts
15. Activity export

---

## 5. Implementation Checklist

### Phase 1: Core CRUD
- [ ] Finalize `CreatePostAsync` implementation in `PostComposer`
- [ ] Create `PostEditModal.razor` for updates
- [ ] Add delete confirmation and `DeletePostAsync` call
- [ ] Add edit button to post footer
- [ ] Add delete button to post footer
- [ ] Test all CRUD operations end-to-end

### Phase 2: Feed & Discovery
- [ ] Improve `LoadFeedPostsAsync` implementation
- [ ] Create `ProfileFeed.razor` page
- [ ] Create `SearchFeed.razor` page
- [ ] Create search bar component
- [ ] Implement trending posts widget
- [ ] Add pagination/infinite scroll

### Phase 3: Analytics & Interactions
- [ ] Create `PostAnalytics.razor` modal
- [ ] Create `ProfileActivity.razor` component
- [ ] Add analytics button to posts
- [ ] Integrate with `IReactionsClient`
- [ ] Integrate with `ICommentsClient`
- [ ] Display engagement metrics

### Phase 4: Polish & Features
- [ ] Add visibility selector to composer
- [ ] Add tags input component
- [ ] Add location picker
- [ ] Improve attachment handling
- [ ] Add language selector
- [ ] Add business metadata fields

### Phase 5: Testing & Deployment
- [ ] Unit test each component
- [ ] Integration tests for API calls
- [ ] E2E tests for complete workflows
- [ ] Performance testing with large datasets
- [ ] Accessibility audit
- [ ] Browser compatibility testing

---

## 6. File Structure After Implementation

```
Sivar.Os.Client/
├── Components/
│   └── Feed/
│       ├── PostCard.razor (UPDATED)
│       ├── PostHeader.razor (UPDATED)
│       ├── PostFooter.razor (UPDATED)
│       ├── PostReactions.razor
│       ├── PostComments.razor
│       ├── CommentItem.razor
│       ├── PostComposer.razor (UPDATED)
│       ├── FeedHeader.razor
│       ├── PostMetadata.razor
│       ├── PostEditModal.razor (NEW)
│       ├── PostMoreMenu.razor (NEW)
│       ├── PostAnalytics.razor (NEW)
│       ├── ProfileActivity.razor (NEW)
│       └── SearchBar.razor (NEW)
├── Pages/
│   ├── Home.razor (UPDATED)
│   ├── ProfileFeed.razor (NEW)
│   ├── SearchFeed.razor (NEW)
│   └── TrendingFeed.razor (NEW)
└── Services/
    └── PostService.cs (NEW - optional state management)
```

---

## 7. Testing Strategy

### Unit Tests
- Component parameter binding
- Event callback handling
- Calculation logic (pagination, date formatting)

### Integration Tests
- API call integration
- State management updates
- Navigation between pages

### E2E Tests
- Create → Display → Edit → Delete workflow
- Feed navigation with pagination
- Search and filter functionality
- Analytics display

---

## 8. Notes & Dependencies

- `IReactionsClient` needed for like functionality
- `ICommentsClient` needed for comments
- `IFollowersClient` needed for follow buttons
- `IFilesClient` needed for attachment uploads
- Date/Time formatting utility needed
- Map component needed for location display
- Chart library needed for analytics visualization

---

## 9. Deployment Rollout

### Rollout Strategy
1. Deploy Phase 1 (CRUD) first
2. Monitor for issues and feedback
3. Deploy Phase 2 (Feed Discovery)
4. Deploy Phase 3 (Analytics)
5. Full release with all phases

### Rollback Plan
- Feature flags for each phase
- Endpoint versioning if needed
- Graceful degradation for new features

---

## Summary

This comprehensive plan maps all `IPostsClient` interface methods to specific UI components and user workflows. By following this plan systematically, we'll ensure complete coverage of post functionality across the application, proper error handling, and an excellent user experience.

**Next Steps:**
1. Review and approve this plan
2. Begin Phase 1 implementation
3. Create feature branches for each component
4. Set up testing framework
5. Establish code review process
