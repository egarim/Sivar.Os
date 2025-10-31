# Comment Reply System Improvement Plan

## 🎯 Executive Summary

This plan outlines the implementation of **Instagram-style threaded comment replies** for the Sivar.Os activity stream. The backend infrastructure is **100% complete** - all database models, repositories, services, and API endpoints for replies already exist. This is **purely a frontend/UI implementation** project.

### Visual Target: Instagram Comment Pattern

Based on the provided Instagram screenshot, the system will feature:

- ✅ **Inline reply buttons** on every comment
- ✅ **"View replies (N)"** collapsible toggle with connecting line
- ✅ **Lazy-loaded nested replies** (load on demand, not upfront)
- ✅ **2-level visual nesting** (Instagram pattern)
- ✅ **Minimalist design** (username · time · actions on one line)
- ✅ **Optimistic updates** (instant UI feedback)
- ✅ **Pagination** ("Load more" instead of numbered pages)

### Key Instagram UX Patterns to Replicate:

1. **Horizontal Action Layout**: `username · 2h · 1 like · Reply` (all inline)
2. **Reply Toggle**: `—— View replies (1)` (with horizontal line prefix)
3. **Compact Indentation**: 40px left margin for nested replies
4. **Lazy Loading**: Comments load 5 at a time, replies load on click
5. **Avatar + Text Layout**: Small avatar (32px) on left, content on right
6. **Inline Reply Input**: Appears directly below comment when "Reply" clicked

---

## Current State Analysis

### ✅ What's Already Implemented (Backend)
1. **Database Layer**
   - ✅ `Comment` entity has `ParentCommentId` and `Replies` collection
   - ✅ Self-referencing relationship properly configured in `CommentConfiguration`
   - ✅ Proper indexes on `ParentCommentId`

2. **Repository Layer**
   - ✅ `ICommentRepository` has comprehensive reply methods:
     - `GetRepliesAsync()` - Get direct replies with pagination
     - `GetCommentThreadAsync()` - Get full thread recursively
     - `GetReplyCountAsync()` - Count direct replies
     - `GetDescendantCountAsync()` - Count all nested replies
     - `GetCommentDepthAsync()` - Get nesting level
     - `GetRootCommentIdAsync()` - Find root comment

3. **DTOs**
   - ✅ `CreateReplyDto` - For creating replies
   - ✅ `CommentDto` has `Replies`, `ReplyCount`, `ThreadDepth`, `ParentCommentId`
   - ✅ `CommentThreadStatsDto` - For thread statistics

4. **Service Layer**
   - ✅ `ICommentService.CreateReplyAsync()` - Create a reply
   - ✅ `ICommentService.GetRepliesByCommentAsync()` - Get paginated replies

5. **Controller/API Layer**
   - ✅ `POST /api/comments/{parentCommentId}/reply` - Create reply endpoint
   - ✅ `GET /api/comments/{commentId}/replies` - Get replies endpoint
   - ✅ `GET /api/comments/{commentId}/thread` - Get thread stats endpoint

### ❌ What's Missing (Frontend)

1. **UI Components**
   - ❌ No "Reply" button implementation in `CommentItem`
   - ❌ No reply input form/textarea
   - ❌ No visual indication of nested replies (indentation, threading)
   - ❌ No "Show Replies" / "Hide Replies" toggle
   - ❌ No loading state for replies
   - ❌ Replies are not rendered recursively

2. **Client Communication**
   - ❌ `ICommentsClient` missing `CreateReplyAsync()` method
   - ❌ `ICommentsClient` missing `GetRepliesAsync()` method
   - ❌ Client implementations not calling reply endpoints

3. **State Management**
   - ❌ No tracking of expanded/collapsed reply threads
   - ❌ No optimistic updates for replies
   - ❌ Reply count not updated when adding/deleting replies

---

## Visual Design & UX Patterns (Instagram-Style)

### Reference Implementation
Based on the Instagram screenshot and existing `CommentSection.razor` component, the comment system should follow these design patterns:

### 1. Comment Display Layout

#### Top-Level Comments (Instagram Pattern)
```
┌─────────────────────────────────────────────────────┐
│ 💬 2,550 Comments                          [▼]      │ ← Collapsible header
├─────────────────────────────────────────────────────┤
│                                                     │
│  [Avatar] username · 2h · 1 like · Reply          │ ← Comment item
│          Comment text goes here...                 │
│                                                     │
│          ┌─ View replies (1) ───────────┐         │ ← Reply toggle (if replies exist)
│          │                               │         │
│  [Avatar] username · 20h · 2 likes · Reply        │
│          Comment text goes here...                 │
│          [emoji] [emoji]                           │
│                                                     │
│          ┌─ View replies (1) ───────────┐         │
│          │  [Avatar] reply text · 5h    │         │ ← Nested reply (indented)
│          │          Reply                │         │
│          └───────────────────────────────┘         │
│                                                     │
│  [Avatar] username · 22h · Reply                   │
│          Post a comment...                         │
│                                                     │
│  [Load more comments...]                           │ ← Pagination
└─────────────────────────────────────────────────────┘
```

#### Key Visual Elements:
1. **Avatar Circle**: Small (32-36px), positioned left
2. **Username**: Bold, clickable, next to avatar
3. **Timestamp**: Small, gray text with "·" separator (e.g., "2h", "20h")
4. **Actions**: Inline text buttons (Reply, Like count)
5. **Reply Button**: Always visible, subtle, positioned after like count
6. **View Replies Link**: Only shown if `ReplyCount > 0`, collapsible
7. **Nested Indentation**: Replies indented ~40-48px from left
8. **Max Nesting**: Instagram limits to 1-2 levels visually

### 2. Component Structure (Instagram Pattern)

```
PostCard
└── CommentSection
    ├── Header: "💬 2,550 Comments [▼]"
    ├── CommentInput: "Add a comment... [Post]"
    └── CommentList
        ├── CommentItem (depth=0) ← Top-level comment
        │   ├── Avatar (32px circle)
        │   ├── Header: "username · 2h"
        │   ├── Content: "Comment text..."
        │   ├── Actions: "1 like · Reply"
        │   ├── ReplyButton: "— View replies (3)"
        │   └── RepliesContainer (if expanded)
        │       ├── CommentItem (depth=1) ← Nested reply
        │       │   ├── Avatar
        │       │   ├── Header: "username · 20h"
        │       │   ├── Content: "Reply text..."
        │       │   ├── Actions: "2 likes · Reply"
        │       │   └── ReplyInput (if replying)
        │       ├── CommentItem (depth=1)
        │       └── LoadMoreReplies (if needed)
        ├── CommentItem (depth=0)
        └── LoadMoreComments
```

**Component Relationships:**
```
┌─────────────────────────────────────────────────────────┐
│ CommentSection.razor                                    │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ foreach (topLevelComment)                           │ │
│ │   CommentItem                                       │ │
│ │   ├── depth = 0                                     │ │
│ │   ├── maxDepth = 2                                  │ │
│ │   └── if (hasReplies && expanded)                   │ │
│ │       foreach (reply)                               │ │
│ │         CommentItem (RECURSIVE)                     │ │
│ │         ├── depth = 1                               │ │
│ │         └── if (hasReplies && expanded)             │ │
│ │             foreach (nestedReply)                   │ │
│ │               CommentItem (RECURSIVE)               │ │
│ │               └── depth = 2 (MAX)                   │ │
│ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

**Data Flow:**
```
User clicks "Reply" on CommentItem
    ↓
CommentItem shows ReplyInput component
    ↓
User types reply and clicks "Post"
    ↓
ReplyInput calls SivarClient.Comments.CreateReplyAsync()
    ↓
API: POST /api/comments/{parentId}/reply
    ↓
OnReplyCreated callback fires
    ↓
CommentItem adds new reply to local _replies list (optimistic)
    ↓
CommentItem increments Comment.ReplyCount
    ↓
CommentItem shows "View replies (N+1)"
    ↓
Parent CommentSection increments _totalComments
    ↓
UI re-renders with new reply visible
```

### 3. Interaction Patterns

#### Lazy Loading (Instagram Pattern)
- **Initial Load**: Show only 3-5 top-level comments
- **"Load More"**: Load next batch of top-level comments (10-20)
- **Replies**: NOT loaded by default
- **"View Replies (N)"**: Click to expand and load replies
- **Reply Pagination**: If >10 replies, show "View more replies"

#### Reply Flow
1. User clicks **"Reply"** button on a comment
2. Reply input appears **directly below** that comment (inline)
3. Input is auto-focused with placeholder: "@username Reply..."
4. Submit adds reply, shows optimistically, collapses input
5. Reply count increments: "View replies (2)" appears

#### Visual States
- **Default**: Comment with Reply button visible
- **Has Replies**: Shows "View replies (N)" link (collapsed by default)
- **Expanded**: Replies visible with nesting, "Hide replies" to collapse
- **Replying**: Reply input visible inline, others hidden
- **Loading**: Skeleton/spinner for replies being fetched

### 4. Styling Specifications

#### Colors (Wireframe Theme)
```css
--comment-bg: rgba(0, 0, 0, 0.02);          /* Light gray background */
--comment-border: var(--wire-border);       /* Subtle border */
--comment-text-primary: #262626;            /* Dark text */
--comment-text-secondary: #8e8e8e;          /* Gray metadata */
--comment-action: #0095f6;                  /* Instagram blue for actions */
--comment-reply-indent: 40px;               /* Left padding for replies */
```

#### Typography
```css
.comment-username {
    font-size: 14px;
    font-weight: 600;
    color: var(--comment-text-primary);
}

.comment-text {
    font-size: 14px;
    line-height: 1.4;
    color: var(--comment-text-primary);
}

.comment-time {
    font-size: 12px;
    color: var(--comment-text-secondary);
}

.comment-action {
    font-size: 12px;
    font-weight: 600;
    color: var(--comment-text-secondary);
    cursor: pointer;
}
```

#### Layout
```css
.comment-item {
    display: flex;
    gap: 12px;
    padding: 12px 0;
}

.comment-avatar {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    flex-shrink: 0;
}

.comment-content {
    flex: 1;
    min-width: 0; /* Allows text to wrap */
}

/* Nested reply indentation */
.comment-item.reply {
    margin-left: 40px; /* Indent replies */
}

/* Max depth visual treatment */
.comment-item.max-depth {
    margin-left: 80px; /* 2 levels deep */
    border-left: 2px solid var(--comment-border);
    padding-left: 12px;
}
```

### 5. Loading Strategy (Based on Performance)

#### Initial Page Load
```
- Load top 5 top-level comments
- Show "View more comments" button
- Replies NOT loaded (lazy)
```

#### User Clicks "View Replies (N)"
```
- Fetch first 10 replies for that comment
- Show inline, indented
- If >10 replies exist, show "Load more replies"
```

#### User Clicks "Reply"
```
- Show inline reply input
- Hide other reply inputs (only one active at a time)
- Focus input, pre-fill with "@username "
```

### 6. Component Props & State

#### CommentItem Component
```typescript
interface CommentItemProps {
    comment: CommentDto;              // The comment data
    depth: number;                    // Nesting level (0, 1, 2...)
    maxDepth: number;                 // Max nesting to display (default 2)
    currentUserId: Guid;              // For ownership checks
    onReply: (commentId) => void;     // Reply button clicked
    onDelete: (commentId) => void;    // Delete comment
    onLike: (commentId) => void;      // Like/unlike comment
}

interface CommentItemState {
    showReplies: boolean;             // Replies expanded?
    showReplyInput: boolean;          // Reply input visible?
    loadingReplies: boolean;          // Fetching replies?
    replies: CommentDto[];            // Loaded child comments
    replyText: string;                // Input text
}
```

### 7. Mobile Responsiveness

#### Breakpoints
- **Desktop (>768px)**: Full layout, all features visible
- **Tablet (768px-1024px)**: Slightly reduced padding
- **Mobile (<768px)**: 
  - Avatar size: 28px (smaller)
  - Font sizes reduced by 1-2px
  - Reply indent: 32px (less indentation)
  - Collapse "Like count" to just icon on very small screens

```css
@media (max-width: 768px) {
    .comment-avatar {
        width: 28px;
        height: 28px;
    }
    
    .comment-item.reply {
        margin-left: 32px; /* Less indent on mobile */
    }
    
    .comment-username {
        font-size: 13px;
    }
    
    .comment-text {
        font-size: 13px;
    }
}
```

### 8. Accessibility Requirements

- **ARIA Labels**: 
  - `aria-label="Reply to comment"` on Reply button
  - `aria-label="View 5 replies"` on expand link
  - `aria-expanded="true/false"` on reply containers
  
- **Keyboard Navigation**:
  - Tab through comments
  - Enter to expand/collapse replies
  - Escape to close reply input
  
- **Screen Readers**:
  - Announce reply count: "5 replies available"
  - Announce when reply added: "Reply posted successfully"

---

## Implementation Plan

### Phase 1: Client Layer (API Communication)

#### 1.1 Update `ICommentsClient` Interface
**File:** `Sivar.Os.Shared/Clients/ICommentsClient.cs`

Add missing methods to support reply functionality:

```csharp
/// <summary>
/// Client interface for comment operations
/// </summary>
public interface ICommentsClient
{
    // Existing methods...
    Task<CommentDto> CreateCommentAsync(CreateCommentDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<CommentDto>> GetPostCommentsAsync(Guid postId, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default);
    
    // NEW: Reply methods
    /// <summary>
    /// Create a reply to an existing comment
    /// </summary>
    Task<CommentDto> CreateReplyAsync(
        Guid parentCommentId, 
        CreateReplyDto request, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get paginated replies for a specific comment
    /// </summary>
    Task<(IEnumerable<CommentDto> Replies, int TotalCount)> GetRepliesAsync(
        Guid parentCommentId, 
        int page = 0, 
        int pageSize = 10, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get top-level comments for a post (no parent, for lazy loading)
    /// </summary>
    Task<(IEnumerable<CommentDto> Comments, int TotalCount)> GetTopLevelByPostAsync(
        Guid postId, 
        int page = 0, 
        int pageSize = 5, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get thread statistics for a comment
    /// </summary>
    Task<CommentThreadStatsDto> GetThreadStatsAsync(
        Guid commentId, 
        CancellationToken cancellationToken = default);
}
```

#### 1.2 Implement in Server-Side Client
**File:** `Sivar.Os/Services/Clients/CommentsClient.cs`

```csharp
public class CommentsClient : BaseRepositoryClient, ICommentsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CommentsClient> _logger;
    
    public CommentsClient(HttpClient httpClient, ILogger<CommentsClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    // Existing implementations...
    
    /// <summary>
    /// Create a reply to a comment
    /// </summary>
    public async Task<CommentDto> CreateReplyAsync(
        Guid parentCommentId, 
        CreateReplyDto request, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[CommentsClient.CreateReplyAsync] Creating reply to comment {ParentId}", parentCommentId);
        
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/comments/{parentCommentId}/reply", 
                request, 
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            var reply = await response.Content.ReadFromJsonAsync<CommentDto>(cancellationToken: cancellationToken);
            
            _logger.LogInformation("[CommentsClient.CreateReplyAsync] Reply created: {ReplyId}", reply?.Id);
            
            return reply ?? throw new InvalidOperationException("Failed to deserialize reply");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[CommentsClient.CreateReplyAsync] HTTP error creating reply");
            throw new SivarApiException("Failed to create reply", ex);
        }
    }
    
    /// <summary>
    /// Get replies for a comment with pagination
    /// </summary>
    public async Task<(IEnumerable<CommentDto> Replies, int TotalCount)> GetRepliesAsync(
        Guid parentCommentId, 
        int page = 0, 
        int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[CommentsClient.GetRepliesAsync] Fetching replies for comment {CommentId}, Page={Page}, PageSize={PageSize}", 
            parentCommentId, page, pageSize);
        
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/comments/{parentCommentId}/replies?page={page}&pageSize={pageSize}", 
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<CommentThreadDto>(cancellationToken: cancellationToken);
            
            if (result == null)
            {
                _logger.LogWarning("[CommentsClient.GetRepliesAsync] Null response from API");
                return (Enumerable.Empty<CommentDto>(), 0);
            }
            
            _logger.LogInformation(
                "[CommentsClient.GetRepliesAsync] Loaded {Count} replies, Total={Total}", 
                result.Comments.Count, result.TotalCount);
            
            return (result.Comments, result.TotalCount);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[CommentsClient.GetRepliesAsync] HTTP error fetching replies");
            throw new SivarApiException("Failed to fetch replies", ex);
        }
    }
    
    /// <summary>
    /// Get top-level comments only (for lazy loading)
    /// </summary>
    public async Task<(IEnumerable<CommentDto> Comments, int TotalCount)> GetTopLevelByPostAsync(
        Guid postId, 
        int page = 0, 
        int pageSize = 5, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[CommentsClient.GetTopLevelByPostAsync] Fetching top-level comments for post {PostId}, Page={Page}, PageSize={PageSize}", 
            postId, page, pageSize);
        
        try
        {
            // The existing endpoint should be modified to accept a 'topLevelOnly' parameter
            // or create a new endpoint: GET /api/comments/post/{postId}/top-level
            var response = await _httpClient.GetAsync(
                $"/api/comments/post/{postId}?page={page}&pageSize={pageSize}&topLevelOnly=true", 
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<CommentThreadDto>(cancellationToken: cancellationToken);
            
            if (result == null)
            {
                _logger.LogWarning("[CommentsClient.GetTopLevelByPostAsync] Null response from API");
                return (Enumerable.Empty<CommentDto>(), 0);
            }
            
            _logger.LogInformation(
                "[CommentsClient.GetTopLevelByPostAsync] Loaded {Count} comments, Total={Total}", 
                result.Comments.Count, result.TotalCount);
            
            return (result.Comments, result.TotalCount);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[CommentsClient.GetTopLevelByPostAsync] HTTP error fetching comments");
            throw new SivarApiException("Failed to fetch comments", ex);
        }
    }
    
    /// <summary>
    /// Get thread statistics
    /// </summary>
    public async Task<CommentThreadStatsDto> GetThreadStatsAsync(
        Guid commentId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[CommentsClient.GetThreadStatsAsync] Fetching thread stats for comment {CommentId}", commentId);
        
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/comments/{commentId}/thread", 
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            var stats = await response.Content.ReadFromJsonAsync<CommentThreadStatsDto>(cancellationToken: cancellationToken);
            
            _logger.LogInformation("[CommentsClient.GetThreadStatsAsync] Stats loaded: {Stats}", stats);
            
            return stats ?? throw new InvalidOperationException("Failed to deserialize thread stats");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[CommentsClient.GetThreadStatsAsync] HTTP error fetching stats");
            throw new SivarApiException("Failed to fetch thread statistics", ex);
        }
    }
}
```

#### 1.3 Implement in WASM Client
**File:** `Sivar.Os.Client/Clients/CommentsClient.cs`

**Important:** Mirror the exact same implementation as the server-side client, but inject the WASM HttpClient.

```csharp
public class CommentsClient : BaseClient, ICommentsClient
{
    public CommentsClient(HttpClient httpClient, ILogger<CommentsClient> logger) 
        : base(httpClient, logger)
    {
    }
    
    // Copy all methods from server-side implementation
    // The HTTP calls are identical, just different execution context
    
    public async Task<CommentDto> CreateReplyAsync(
        Guid parentCommentId, 
        CreateReplyDto request, 
        CancellationToken cancellationToken = default)
    {
        // Same implementation as server-side
        return await PostAsync<CreateReplyDto, CommentDto>(
            $"/api/comments/{parentCommentId}/reply", 
            request, 
            cancellationToken);
    }
    
    // ... other methods
}
```

#### 1.4 Minor Backend Update (Optional but Recommended)
**File:** `Sivar.Os/Controllers/CommentsController.cs`

Update the `GetCommentsByPost` endpoint to support a `topLevelOnly` filter:

```csharp
/// <summary>
/// Gets comments for a specific post
/// </summary>
[HttpGet("post/{postId}")]
public async Task<ActionResult<CommentThreadDto>> GetCommentsByPost(
    Guid postId,
    [FromQuery] int page = 0, 
    [FromQuery] int pageSize = 20,
    [FromQuery] bool topLevelOnly = false)  // NEW parameter
{
    try
    {
        var keycloakId = GetKeycloakIdFromRequest();
        
        if (pageSize > 100)
            pageSize = 100;

        // Use different service method based on topLevelOnly flag
        var (comments, totalCount) = topLevelOnly
            ? await _commentService.GetTopLevelByPostAsync(postId, keycloakId, page, pageSize)
            : await _commentService.GetCommentsByPostAsync(postId, keycloakId, page, pageSize);
        
        var result = new CommentThreadDto
        {
            Comments = comments.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
        
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting comments for post {PostId}", postId);
        return StatusCode(500, "Internal server error");
    }
}
```

**Note:** The `GetTopLevelByPostAsync` method already exists in `ICommentRepository` and should be implemented in `CommentRepository`.

---

### Phase 2: UI Components (Reply Interface)

#### 2.1 Create `ReplyInput.razor` Component
**File:** `Sivar.Os.Client/Components/Feed/ReplyInput.razor`

**Purpose:** Inline reply input (Instagram-style)

**Features:**
- Single-line text input (expands to 2-3 lines if needed)
- Placeholder: "Add a reply..." or "@username Reply..."
- Character counter (max 2000, shown when >1900 chars)
- "Post" button (text button, blue when input has text)
- "Cancel" link (small, gray)
- Loading state during submission
- Auto-focus when shown
- Auto-collapse after successful submit

**Visual Layout:**
```html
<div class="reply-input-container">
    <textarea class="reply-input" 
              placeholder="Add a reply..." 
              rows="1" 
              maxlength="2000"></textarea>
    <div class="reply-input-actions">
        <button class="reply-post-btn" disabled="@IsEmpty">Post</button>
        <button class="reply-cancel-btn">Cancel</button>
    </div>
</div>
```

**Styling (Instagram Pattern):**
```css
.reply-input-container {
    margin-left: 44px; /* Align with comment text */
    margin-top: 8px;
    margin-bottom: 12px;
}

.reply-input {
    width: 100%;
    border: 1px solid var(--comment-border);
    border-radius: 8px;
    padding: 8px 12px;
    font-size: 14px;
    resize: none;
    overflow: hidden; /* Auto-grow */
}

.reply-input:focus {
    outline: none;
    border-color: var(--comment-action);
}

.reply-input-actions {
    display: flex;
    justify-content: flex-end;
    gap: 12px;
    margin-top: 8px;
}

.reply-post-btn {
    color: var(--comment-action);
    font-weight: 600;
    font-size: 14px;
    background: none;
    border: none;
    cursor: pointer;
}

.reply-post-btn:disabled {
    opacity: 0.4;
    cursor: not-allowed;
}

.reply-cancel-btn {
    color: var(--comment-text-secondary);
    font-size: 13px;
    background: none;
    border: none;
    cursor: pointer;
}
```

**Parameters:**
```csharp
[Parameter] public Guid ParentCommentId { get; set; }
[Parameter] public string? MentionUsername { get; set; } // For @username prefix
[Parameter] public EventCallback<CommentDto> OnReplyCreated { get; set; }
[Parameter] public EventCallback OnCancel { get; set; }
```

**Auto-Grow Logic:**
```csharp
private async Task OnInput()
{
    // Auto-grow textarea up to max 5 lines
    await JS.InvokeVoidAsync("autoGrowTextarea", _textAreaRef);
}
```

#### 2.2 Update `CommentItem.razor` Component
**File:** `Sivar.Os.Client/Components/Feed/CommentItem.razor`

**Complete Redesign (Instagram Pattern):**

**Visual Structure:**
```html
<div class="comment-item @GetDepthClass()">
    <!-- Avatar + Content (Horizontal Layout) -->
    <div class="comment-avatar">
        <MudAvatar Size="Size.Small">@GetInitials()</MudAvatar>
    </div>
    
    <div class="comment-content">
        <!-- Username · Time · Actions (Single Line) -->
        <div class="comment-header-inline">
            <span class="comment-username" @onclick="ViewProfile">@Comment.Profile.DisplayName</span>
            <span class="comment-separator">·</span>
            <span class="comment-time">@GetTimeAgo()</span>
            
            @if (IsCurrentUserComment())
            {
                <MudMenu Icon="@Icons.Material.Filled.MoreVert" Size="Size.Small" Class="comment-menu">
                    <MudMenuItem OnClick="DeleteComment">Delete</MudMenuItem>
                    <MudMenuItem OnClick="EditComment">Edit</MudMenuItem>
                </MudMenu>
            }
        </div>
        
        <!-- Comment Text -->
        <div class="comment-text">@Comment.Content</div>
        
        <!-- Actions: Like · Reply · View Replies -->
        <div class="comment-actions">
            @if (Comment.ReactionSummary?.TotalReactions > 0)
            {
                <button class="comment-action-btn" @onclick="ToggleLike">
                    @(Comment.ReactionSummary.UserReaction.HasValue ? "Unlike" : "Like") · @Comment.ReactionSummary.TotalReactions
                </button>
            }
            
            <button class="comment-action-btn" @onclick="ToggleReplyInput">
                Reply
            </button>
            
            @if (Comment.ReplyCount > 0 && !_showReplies)
            {
                <button class="view-replies-btn" @onclick="ToggleReplies">
                    <span class="view-replies-line">—</span> View replies (@Comment.ReplyCount)
                </button>
            }
            
            @if (_showReplies && Comment.ReplyCount > 0)
            {
                <button class="view-replies-btn" @onclick="ToggleReplies">
                    <MudIcon Icon="@Icons.Material.Filled.ExpandLess" Size="Size.Small" />
                    Hide replies
                </button>
            }
        </div>
        
        <!-- Inline Reply Input (Conditional) -->
        @if (_showReplyInput)
        {
            <ReplyInput ParentCommentId="@Comment.Id"
                        MentionUsername="@Comment.Profile.DisplayName"
                        OnReplyCreated="HandleReplyCreated"
                        OnCancel="@(() => _showReplyInput = false)" />
        }
        
        <!-- Nested Replies (Conditional, Lazy Loaded) -->
        @if (_showReplies)
        {
            <div class="replies-container">
                @if (_loadingReplies)
                {
                    <div class="loading-replies">
                        <MudProgressCircular Size="Size.Small" Indeterminate="true" />
                        <span>Loading replies...</span>
                    </div>
                }
                else if (_replies.Any())
                {
                    @foreach (var reply in _replies)
                    {
                        <CommentItem Comment="@reply"
                                     Depth="@(Depth + 1)"
                                     MaxDepth="@MaxDepth"
                                     CurrentUserProfileId="@CurrentUserProfileId"
                                     OnReplyCreated="HandleNestedReplyCreated"
                                     OnDelete="OnDelete" />
                    }
                    
                    @* Load More Replies if needed *@
                    @if (_hasMoreReplies)
                    {
                        <button class="load-more-replies-btn" @onclick="LoadMoreReplies">
                            Load more replies...
                        </button>
                    }
                }
            </div>
        }
    </div>
</div>
```

**New Styling (Instagram-Style):**
```css
.comment-item {
    display: flex;
    gap: 12px;
    padding: 8px 0;
    position: relative;
}

/* Depth-based indentation */
.comment-item.depth-0 {
    margin-left: 0;
}

.comment-item.depth-1 {
    margin-left: 40px;
}

.comment-item.depth-2 {
    margin-left: 80px;
}

/* Max depth gets special treatment */
.comment-item.max-depth {
    border-left: 2px solid var(--comment-border);
    padding-left: 12px;
}

.comment-avatar {
    flex-shrink: 0;
}

.comment-content {
    flex: 1;
    min-width: 0;
}

.comment-header-inline {
    display: flex;
    align-items: center;
    gap: 6px;
    margin-bottom: 4px;
    flex-wrap: wrap;
}

.comment-username {
    font-weight: 600;
    font-size: 14px;
    color: var(--comment-text-primary);
    cursor: pointer;
}

.comment-username:hover {
    text-decoration: underline;
}

.comment-separator {
    color: var(--comment-text-secondary);
    font-size: 12px;
}

.comment-time {
    font-size: 12px;
    color: var(--comment-text-secondary);
}

.comment-text {
    font-size: 14px;
    line-height: 1.4;
    margin-bottom: 6px;
    color: var(--comment-text-primary);
    word-wrap: break-word;
}

.comment-actions {
    display: flex;
    gap: 16px;
    align-items: center;
    flex-wrap: wrap;
}

.comment-action-btn {
    background: none;
    border: none;
    font-size: 12px;
    font-weight: 600;
    color: var(--comment-text-secondary);
    cursor: pointer;
    padding: 0;
}

.comment-action-btn:hover {
    color: var(--comment-text-primary);
}

.view-replies-btn {
    background: none;
    border: none;
    font-size: 12px;
    font-weight: 600;
    color: var(--comment-text-secondary);
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 4px;
    padding: 0;
}

.view-replies-line {
    display: inline-block;
    width: 20px;
    height: 1px;
    background: var(--comment-text-secondary);
    margin-right: 4px;
}

.replies-container {
    margin-top: 12px;
}

.loading-replies {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 12px 0;
    color: var(--comment-text-secondary);
    font-size: 13px;
}

.load-more-replies-btn {
    background: none;
    border: none;
    color: var(--comment-action);
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    padding: 8px 0;
    margin-left: 0;
}
```

**New State Variables:**
```csharp
private bool _showReplyInput = false;
private bool _showReplies = false;
private bool _loadingReplies = false;
private List<CommentDto> _replies = new();
private bool _hasMoreReplies = false;
private int _repliesPage = 1;
private const int RepliesPageSize = 10;
```

**New Parameters:**
```csharp
[Parameter] public int Depth { get; set; } = 0;  // Current nesting level
[Parameter] public int MaxDepth { get; set; } = 2;  // Max depth to show nested
[Parameter] public EventCallback<CommentDto> OnReplyCreated { get; set; }
```

**New Methods:**
```csharp
private string GetDepthClass()
{
    if (Depth >= MaxDepth) return "max-depth";
    return $"depth-{Depth}";
}

private async Task ToggleReplyInput()
{
    _showReplyInput = !_showReplyInput;
    
    // Close other reply inputs (only one active at a time)
    if (_showReplyInput)
    {
        await OnReplyInputOpened.InvokeAsync(Comment.Id);
    }
}

private async Task ToggleReplies()
{
    _showReplies = !_showReplies;
    
    // Load replies on first expand
    if (_showReplies && !_replies.Any() && !_loadingReplies)
    {
        await LoadReplies();
    }
}

private async Task LoadReplies()
{
    _loadingReplies = true;
    StateHasChanged();
    
    try
    {
        var (replies, totalCount) = await SivarClient.Comments.GetRepliesAsync(
            Comment.Id, 
            page: _repliesPage, 
            pageSize: RepliesPageSize
        );
        
        _replies.AddRange(replies);
        _hasMoreReplies = _replies.Count < totalCount;
        _repliesPage++;
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error loading replies for comment {CommentId}", Comment.Id);
    }
    finally
    {
        _loadingReplies = false;
        StateHasChanged();
    }
}

private async Task LoadMoreReplies()
{
    await LoadReplies();
}

private async Task HandleReplyCreated(CommentDto newReply)
{
    // Add to local list (optimistic update)
    _replies.Insert(0, newReply);
    
    // Update parent's reply count
    Comment = Comment with { ReplyCount = Comment.ReplyCount + 1 };
    
    // Close reply input
    _showReplyInput = false;
    
    // Ensure replies are visible
    _showReplies = true;
    
    // Notify parent
    await OnReplyCreated.InvokeAsync(newReply);
    
    StateHasChanged();
}

private async Task HandleNestedReplyCreated(CommentDto newReply)
{
    // Propagate up to parent
    await OnReplyCreated.InvokeAsync(newReply);
}

private string GetTimeAgo()
{
    var diff = DateTime.UtcNow - Comment.CreatedAt;
    
    if (diff.TotalSeconds < 60) return "just now";
    if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m";
    if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h";
    if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d";
    if (diff.TotalDays < 30) return $"{(int)(diff.TotalDays / 7)}w";
    
    return Comment.CreatedAt.ToString("MMM d");
}
```

#### 2.3 Update `CommentSection.razor` Component
**File:** `Sivar.Os/Components/Feed/CommentSection.razor`

**Changes (Instagram Lazy-Loading Pattern):**

**Updated Structure:**
```html
<div class="comment-section">
    <!-- Header: Collapsible with count -->
    <div class="comment-header" @onclick="ToggleExpanded">
        <span class="comment-count">💬 @_totalComments Comments</span>
        <MudIconButton Icon="@(IsExpanded ? Icons.Material.Filled.ExpandLess : Icons.Material.Filled.ExpandMore)" 
                       Size="Size.Small" />
    </div>

    @if (IsExpanded)
    {
        <!-- Add Comment Input (Top-Level) -->
        <div class="comment-input-area">
            <MudAvatar Size="Size.Small" Class="current-user-avatar">
                @GetCurrentUserInitials()
            </MudAvatar>
            <MudTextField @bind-Value="_newCommentText"
                          Label="Add a comment..."
                          Variant="Variant.Outlined"
                          Lines="1"
                          MaxLength="2000"
                          Class="comment-input-field" />
            <MudButton Color="Color.Primary" 
                       Variant="Variant.Text" 
                       OnClick="SubmitComment"
                       Disabled="@(string.IsNullOrWhiteSpace(_newCommentText))"
                       Class="comment-post-btn">
                Post
            </MudButton>
        </div>

        @if (_isLoading && !_comments.Any())
        {
            <!-- Initial loading state -->
            <div class="loading-comments">
                @for (int i = 0; i < 3; i++)
                {
                    <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="60px" Class="mb-2" />
                }
            </div>
        }
        else if (_comments.Count == 0)
        {
            <MudText Typo="Typo.body2" Color="Color.Secondary" Class="pa-4 text-center">
                No comments yet. Be the first to comment!
            </MudText>
        }
        else
        {
            <!-- Comment List (Top-Level Only) -->
            <div class="comment-list">
                @foreach (var comment in _comments)
                {
                    <CommentItem Comment="@comment"
                                 Depth="0"
                                 MaxDepth="2"
                                 CurrentUserProfileId="@_currentUserProfileId"
                                 OnReplyCreated="HandleReplyCreated"
                                 OnDelete="HandleDeleteComment" />
                }
            </div>

            <!-- Load More Comments Button -->
            @if (_hasMoreComments)
            {
                <MudButton Variant="Variant.Text" 
                           OnClick="LoadMoreComments"
                           FullWidth="true"
                           Class="load-more-btn"
                           Disabled="@_isLoadingMore">
                    @if (_isLoadingMore)
                    {
                        <MudProgressCircular Size="Size.Small" Indeterminate="true" />
                        <span>Loading...</span>
                    }
                    else
                    {
                        <span>Load more comments</span>
                    }
                </MudButton>
            }
        }
    }
</div>
```

**Updated Styling:**
```css
.comment-section {
    margin-top: 16px;
    border-top: 1px solid var(--wire-border);
    padding-top: 16px;
}

.comment-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    cursor: pointer;
    padding: 8px;
    border-radius: 8px;
    transition: background-color 0.2s;
}

.comment-header:hover {
    background-color: rgba(0, 0, 0, 0.03);
}

.comment-count {
    font-weight: 600;
    color: var(--wire-text-primary);
    font-size: 14px;
}

/* Instagram-style input area */
.comment-input-area {
    display: flex;
    gap: 12px;
    margin: 16px 0;
    align-items: center;
}

.current-user-avatar {
    flex-shrink: 0;
}

.comment-input-field {
    flex: 1;
}

.comment-post-btn {
    flex-shrink: 0;
    font-weight: 600;
}

.comment-list {
    display: flex;
    flex-direction: column;
    margin-top: 16px;
}

.loading-comments {
    padding: 16px 0;
}

.load-more-btn {
    margin-top: 16px;
    color: var(--comment-text-secondary);
    font-weight: 600;
}
```

**Updated Code:**
```csharp
@code {
    [Parameter] public Guid PostId { get; set; }
    [Parameter] public int InitialCommentCount { get; set; }
    [Parameter] public bool IsExpanded { get; set; } = false;
    [Parameter] public EventCallback OnCommentAdded { get; set; }

    private List<CommentDto> _comments = new();
    private string _newCommentText = string.Empty;
    private bool _isLoading = false;
    private bool _isLoadingMore = false;
    private int _currentPage = 0;  // 0-based for API
    private int _totalComments = 0;
    private bool _hasMoreComments = false;
    private Guid? _currentUserProfileId;
    private const int PageSize = 5;  // Instagram loads 5 at a time initially

    protected override async Task OnInitializedAsync()
    {
        _totalComments = InitialCommentCount;
        
        try
        {
            // Get current user's active profile ID
            var currentProfile = await SivarClient.Profiles.GetMyActiveProfileAsync();
            _currentUserProfileId = currentProfile?.Id;
            
            Logger.LogInformation("[CommentSection] Current user profile ID: {ProfileId}", _currentUserProfileId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[CommentSection] Error getting current user profile");
        }
        
        if (IsExpanded)
        {
            await LoadCommentsAsync();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsExpanded && _comments.Count == 0 && !_isLoading)
        {
            await LoadCommentsAsync();
        }
    }

    private async Task ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        
        if (IsExpanded && _comments.Count == 0)
        {
            await LoadCommentsAsync();
        }
        
        StateHasChanged();
    }

    /// <summary>
    /// Load top-level comments only (Instagram pattern)
    /// </summary>
    private async Task LoadCommentsAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            Logger.LogInformation("[CommentSection] Loading top-level comments for PostId={PostId}, Page={Page}", 
                PostId, _currentPage);

            // Call API to get TOP-LEVEL comments only (ParentCommentId == null)
            var response = await SivarClient.Comments.GetTopLevelByPostAsync(
                PostId, 
                page: _currentPage, 
                pageSize: PageSize
            );

            _comments.AddRange(response.Comments);
            _totalComments = response.TotalCount;
            _hasMoreComments = (_currentPage + 1) * PageSize < _totalComments;
            _currentPage++;

            Logger.LogInformation("[CommentSection] Loaded {Count} comments, Total={Total}, HasMore={HasMore}", 
                response.Comments.Count(), _totalComments, _hasMoreComments);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[CommentSection] Error loading comments for PostId={PostId}", PostId);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadMoreComments()
    {
        _isLoadingMore = true;
        StateHasChanged();
        
        await LoadCommentsAsync();
        
        _isLoadingMore = false;
        StateHasChanged();
    }

    private async Task SubmitComment()
    {
        if (string.IsNullOrWhiteSpace(_newCommentText))
            return;

        try
        {
            Logger.LogInformation("[CommentSection] Submitting top-level comment for PostId={PostId}", PostId);

            var createDto = new CreateCommentDto
            {
                PostId = PostId,
                Content = _newCommentText.Trim(),
                Language = "en"
            };

            var newComment = await SivarClient.Comments.CreateCommentAsync(createDto);

            if (newComment != null)
            {
                // Add to top of list (Instagram pattern: newest first)
                _comments.Insert(0, newComment);
                _newCommentText = string.Empty;
                _totalComments++;

                await OnCommentAdded.InvokeAsync();
                
                Logger.LogInformation("[CommentSection] Comment created successfully: {CommentId}", 
                    newComment.Id);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[CommentSection] Error creating comment");
        }

        StateHasChanged();
    }

    private async Task HandleDeleteComment(Guid commentId)
    {
        try
        {
            Logger.LogInformation("[CommentSection] Deleting comment: {CommentId}", commentId);
            
            await SivarClient.Comments.DeleteCommentAsync(commentId);
            
            // Remove from list (could be top-level or nested)
            RemoveCommentRecursive(_comments, commentId);
            _totalComments--;
            
            Logger.LogInformation("[CommentSection] Comment deleted successfully");
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[CommentSection] Error deleting comment");
        }
    }

    private void RemoveCommentRecursive(List<CommentDto> comments, Guid commentId)
    {
        var comment = comments.FirstOrDefault(c => c.Id == commentId);
        if (comment != null)
        {
            comments.Remove(comment);
            return;
        }

        // Check nested replies
        foreach (var c in comments)
        {
            if (c.Replies.Any())
            {
                RemoveCommentRecursive(c.Replies.ToList(), commentId);
            }
        }
    }

    private async Task HandleReplyCreated(CommentDto newReply)
    {
        // Reply was created, increment total count
        _totalComments++;
        
        // Notify parent component
        await OnCommentAdded.InvokeAsync();
        
        StateHasChanged();
    }

    private string GetCurrentUserInitials()
    {
        // Get from auth state or profile service
        return "ME"; // TODO: Get actual user initials
    }
}
```

**Key Changes:**
1. ✅ Load only **top-level** comments (not nested replies)
2. ✅ Pagination: Start with 5 comments, load 5 more at a time
3. ✅ Instagram-style input with avatar + single-line field
4. ✅ Skeleton loader for initial load
5. ✅ "Load more comments" instead of pagination numbers
6. ✅ Comments sorted newest first (Instagram pattern)
7. ✅ Pass `Depth=0` to all top-level CommentItems

---

### Phase 3: Styling & UX Enhancements

#### 3.1 Visual Threading (Instagram Pattern)

**Instagram "View Replies" Visual Design:**

The screenshot shows Instagram's distinctive reply threading with:
1. **Thin connecting line** from avatar to replies section
2. **"View replies (N)"** link with horizontal line prefix
3. **Nested replies** appear in a subtle container when expanded

**CSS Implementation:**
```css
/* Instagram-style reply threading */
.comment-item {
    display: flex;
    gap: 12px;
    padding: 8px 0;
    position: relative;
}

/* Connecting line for comments with replies */
.comment-item.has-replies::before {
    content: '';
    position: absolute;
    left: 16px; /* Center of avatar */
    top: 48px; /* Below avatar */
    bottom: 0;
    width: 2px;
    background: linear-gradient(
        to bottom,
        var(--comment-border) 0%,
        var(--comment-border) 100%
    );
    opacity: 0.5;
}

/* Depth-based indentation (Instagram limits to 2 levels) */
.comment-item.depth-0 {
    margin-left: 0;
}

.comment-item.depth-1 {
    margin-left: 40px;
    padding-left: 12px;
}

.comment-item.depth-2 {
    margin-left: 80px;
    padding-left: 12px;
    border-left: 1px solid var(--comment-border);
}

/* "View replies" button with horizontal line */
.view-replies-btn {
    background: none;
    border: none;
    font-size: 13px;
    font-weight: 600;
    color: var(--comment-text-secondary);
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 4px 0;
    margin-top: 4px;
}

.view-replies-btn:hover {
    color: var(--comment-text-primary);
}

.view-replies-line {
    width: 24px;
    height: 1px;
    background: var(--comment-text-secondary);
}

/* Replies container - subtle background */
.replies-container {
    margin-top: 16px;
    padding-top: 8px;
}

.comment-item.depth-1 .replies-container {
    border-left: 2px solid var(--comment-border);
    padding-left: 12px;
    margin-left: -12px;
}

/* Smooth expand/collapse animation */
.replies-container {
    animation: slideDown 0.3s ease-out;
}

@keyframes slideDown {
    from {
        opacity: 0;
        transform: translateY(-10px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

/* Hide replies animation */
.replies-container.collapsing {
    animation: slideUp 0.2s ease-in;
}

@keyframes slideUp {
    from {
        opacity: 1;
        transform: translateY(0);
    }
    to {
        opacity: 0;
        transform: translateY(-10px);
    }
}
```

#### 3.2 Loading States (Instagram Pattern)

**Skeleton Loaders for Initial Comments:**
```html
<!-- While loading first comments -->
<div class="comment-skeleton">
    <MudSkeleton SkeletonType="SkeletonType.Circle" Width="32px" Height="32px" />
    <div class="comment-skeleton-content">
        <MudSkeleton SkeletonType="SkeletonType.Text" Width="30%" />
        <MudSkeleton SkeletonType="SkeletonType.Text" Width="90%" />
        <MudSkeleton SkeletonType="SkeletonType.Text" Width="20%" />
    </div>
</div>
```

**Loading More Comments (Bottom):**
```html
<div class="loading-more-comments">
    <MudProgressCircular Size="Size.Small" Indeterminate="true" />
    <span class="loading-text">Loading more comments...</span>
</div>
```

**Loading Replies (Inline):**
```html
<div class="loading-replies">
    <div class="loading-spinner-small"></div>
    <span class="loading-text-small">Loading replies...</span>
</div>
```

**CSS:**
```css
.comment-skeleton {
    display: flex;
    gap: 12px;
    padding: 12px 0;
}

.comment-skeleton-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.loading-more-comments {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 12px;
    padding: 16px;
    color: var(--comment-text-secondary);
    font-size: 14px;
}

.loading-replies {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 12px 0 12px 44px; /* Align with comment text */
    color: var(--comment-text-secondary);
    font-size: 13px;
}

.loading-spinner-small {
    width: 16px;
    height: 16px;
    border: 2px solid var(--comment-border);
    border-top-color: var(--comment-action);
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
}

@keyframes spin {
    to { transform: rotate(360deg); }
}
```

#### 3.3 Optimistic Updates (Instagram UX)

When user posts a comment or reply:
1. **Instantly add** to UI (don't wait for API response)
2. Show with temporary ID and "posting..." indicator
3. Replace with real data when API responds
4. If API fails, remove and show error toast

**Implementation:**
```csharp
private async Task SubmitComment()
{
    var optimisticComment = new CommentDto
    {
        Id = Guid.NewGuid(), // Temporary ID
        Content = _newCommentText.Trim(),
        Profile = _currentUserProfile,
        CreatedAt = DateTime.UtcNow,
        IsOptimistic = true, // Custom flag
        // ... other properties
    };
    
    // Add immediately to UI
    _comments.Insert(0, optimisticComment);
    _newCommentText = string.Empty;
    StateHasChanged();
    
    try
    {
        // Send to API
        var realComment = await SivarClient.Comments.CreateCommentAsync(createDto);
        
        // Replace optimistic with real
        var index = _comments.FindIndex(c => c.Id == optimisticComment.Id);
        if (index >= 0)
        {
            _comments[index] = realComment;
        }
    }
    catch (Exception ex)
    {
        // Remove optimistic comment on failure
        _comments.Remove(optimisticComment);
        
        // Show error toast
        Snackbar.Add("Failed to post comment. Please try again.", Severity.Error);
    }
    finally
    {
        StateHasChanged();
    }
}
```

**Visual Indicator for Optimistic Comment:**
```html
@if (Comment.IsOptimistic)
{
    <span class="posting-indicator">
        <MudProgressCircular Size="Size.Small" Indeterminate="true" />
        Posting...
    </span>
}
```

#### 3.4 Accessibility (WCAG 2.1 AA Compliance)

**ARIA Labels:**
```html
<!-- Comment item -->
<div class="comment-item" 
     role="article" 
     aria-label="Comment by @Comment.Profile.DisplayName">
    
    <!-- Reply button -->
    <button aria-label="Reply to @Comment.Profile.DisplayName's comment"
            @onclick="ToggleReplyInput">
        Reply
    </button>
    
    <!-- View replies -->
    <button aria-label="View @Comment.ReplyCount replies"
            aria-expanded="@_showReplies"
            @onclick="ToggleReplies">
        View replies (@Comment.ReplyCount)
    </button>
    
    <!-- Replies container -->
    <div class="replies-container" 
         role="region" 
         aria-label="Replies to @Comment.Profile.DisplayName's comment">
        <!-- Nested comments -->
    </div>
</div>
```

**Keyboard Navigation:**
```javascript
// Add to CommentItem.razor.cs
private async Task HandleKeyDown(KeyboardEventArgs e)
{
    switch (e.Key)
    {
        case "Enter":
        case " ": // Spacebar
            if (e.Target == "reply-button")
            {
                await ToggleReplyInput();
            }
            else if (e.Target == "view-replies-button")
            {
                await ToggleReplies();
            }
            break;
            
        case "Escape":
            if (_showReplyInput)
            {
                _showReplyInput = false;
                StateHasChanged();
            }
            break;
    }
}
```

**Focus Management:**
```csharp
// Auto-focus reply input when opened
private ElementReference _replyInputRef;

private async Task ToggleReplyInput()
{
    _showReplyInput = !_showReplyInput;
    
    if (_showReplyInput)
    {
        await Task.Delay(100); // Wait for render
        await _replyInputRef.FocusAsync();
    }
}
```

**Screen Reader Announcements:**
```html
<!-- Live region for dynamic updates -->
<div role="status" 
     aria-live="polite" 
     aria-atomic="true" 
     class="sr-only">
    @if (_lastAnnouncement != null)
    {
        @_lastAnnouncement
    }
</div>
```

```csharp
private string? _lastAnnouncement;

private void AnnounceToScreenReader(string message)
{
    _lastAnnouncement = message;
    StateHasChanged();
    
    // Clear after 3 seconds
    Task.Delay(3000).ContinueWith(_ => {
        _lastAnnouncement = null;
        InvokeAsync(StateHasChanged);
    });
}

// Usage:
await HandleReplyCreated(newReply);
AnnounceToScreenReader($"Reply posted to {Comment.Profile.DisplayName}'s comment");
```

**CSS for Screen Readers:**
```css
.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border-width: 0;
}
```

---

### Phase 4: Advanced Features (Optional/Future)

#### 4.1 Collapsible Threads
- Click commenter's avatar to collapse entire thread
- Persist collapsed state in local storage

#### 4.2 Reply Notifications
- Notify original commenter when someone replies
- Use existing `NotificationService`

#### 4.3 "Continue Thread" View
- For deeply nested threads (>5 levels)
- Show full thread in modal or separate view
- Flatten display with "in reply to @username" context

#### 4.4 Inline Mentions
- `@username` autocomplete in reply input
- Highlight mentioned users
- Send notifications to mentioned users

#### 4.5 Sorting Options
- Sort replies by: Newest, Oldest, Most Liked
- Add dropdown in `CommentSection`

#### 4.6 Real-time Updates
- Use SignalR to push new replies to connected clients
- Update reply counts in real-time
- Show "New replies available" banner

---

## Implementation Order (Recommended)

### Sprint 1: Core Reply Functionality
1. ✅ Update `ICommentsClient` interface
2. ✅ Implement client methods (server & WASM)
3. ✅ Create `ReplyInput.razor` component
4. ✅ Update `CommentItem.razor` with reply button and input

### Sprint 2: Thread Display
5. ✅ Implement lazy loading of replies in `CommentItem`
6. ✅ Add recursive rendering of nested replies
7. ✅ Update `CommentSection.razor` to load top-level only
8. ✅ Add basic threading styles (indentation)

### Sprint 3: Polish & UX
9. ✅ Add visual threading (borders, depth limits)
10. ✅ Implement show/hide replies toggle
11. ✅ Add loading states and optimistic updates
12. ✅ Test and fix edge cases

### Sprint 4: Advanced Features (Optional)
13. 🔮 Reply notifications
14. 🔮 Collapsible threads
15. 🔮 Inline mentions
16. 🔮 Real-time updates

---

## Technical Considerations

### Max Thread Depth
**Recommended:** Limit to 5 levels
- Beyond 5 levels, threads become hard to read
- Provide "Continue thread" link to view in dedicated page
- Consider flattening display after depth limit

### Performance Optimization
1. **Lazy Loading:** Only load replies when user expands them
2. **Pagination:** For comments with many replies (>10), paginate
3. **Caching:** Cache loaded replies in component state
4. **Virtual Scrolling:** For very long threads (100+ comments)

### Reply Count Updates
When a reply is added/deleted:
1. Update parent comment's `ReplyCount` locally (optimistic)
2. Backend should recalculate counts on operations
3. Consider debouncing for bulk operations

### Data Loading Strategy
**Option A: Eager Loading (Current)**
- Load all comments with nested replies in one call
- Pro: Fewer API calls
- Con: Large payload, slow initial load

**Option B: Lazy Loading (Recommended)**
- Load top-level comments first
- Load replies on demand when user clicks "Show replies"
- Pro: Fast initial load, better UX
- Con: More API calls, but they're smaller

**Recommendation:** Use Option B (Lazy Loading)

---

## API Endpoints Summary

Already implemented and working:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `POST` | `/api/comments` | Create top-level comment |
| `POST` | `/api/comments/{id}/reply` | Create reply to comment |
| `GET` | `/api/comments/{id}` | Get single comment |
| `GET` | `/api/comments/post/{postId}` | Get comments by post |
| `GET` | `/api/comments/{id}/replies` | Get replies to comment |
| `GET` | `/api/comments/{id}/thread` | Get thread statistics |
| `PUT` | `/api/comments/{id}` | Update comment |
| `DELETE` | `/api/comments/{id}` | Delete comment |

---

## Testing Checklist

### Unit Tests
- [ ] Create reply with valid parent ID
- [ ] Create reply with invalid parent ID
- [ ] Load replies for comment with replies
- [ ] Load replies for comment without replies
- [ ] Calculate thread depth correctly
- [ ] Respect max depth limit

### Integration Tests
- [ ] Reply creation updates parent's reply count
- [ ] Deleted replies decrement parent's count
- [ ] Nested reply hierarchy maintained
- [ ] Pagination works for large reply threads
- [ ] Reply notifications sent correctly

### UI/UX Tests
- [ ] Reply button shows/hides input
- [ ] Reply input cancels properly
- [ ] Replies display with correct indentation
- [ ] Show/hide replies toggle works
- [ ] Reply count updates after operations
- [ ] Loading states display correctly
- [ ] Deep threads (5+ levels) handled gracefully
- [ ] Mobile responsive design

### Edge Cases
- [ ] Reply to deleted comment (should fail gracefully)
- [ ] Very long reply content (2000 chars)
- [ ] 100+ replies on single comment
- [ ] Circular reference prevention
- [ ] Concurrent reply submissions
- [ ] Network failures during reply creation

---

## Database Considerations

### Current Schema ✅
The schema is already correctly set up:

```sql
-- Comment table already has:
ParentCommentId GUID NULL  -- References Comments.Id
-- Self-referencing foreign key configured
-- Index on ParentCommentId exists
```

### Optional Optimization: Materialized Path
For very deep threads, consider adding a materialized path column:

```sql
ALTER TABLE Sivar_Comments 
ADD COLUMN ThreadPath VARCHAR(1000) NULL;

-- Example: '1234-5678-9012' represents Comment > Reply > Reply
-- Allows efficient querying of entire thread
```

**Benefits:**
- Fast retrieval of all descendants
- Easy thread navigation
- Efficient sorting within threads

**Trade-offs:**
- More complex updates when moving comments
- Additional storage

**Recommendation:** Not needed for MVP, consider for v2

---

## Migration Guide

No database migration needed! The schema already supports replies.

Only frontend and client-layer changes required.

---

## Success Metrics

After implementation, measure:
1. **Engagement:** % increase in reply activity
2. **UX:** Time to create a reply (should be <3 seconds)
3. **Performance:** Load time for comments section
4. **Errors:** Reply creation failure rate (<1%)
5. **Depth:** Average thread depth (expect 1-2 levels)

---

## Known Issues & Gotchas

### Issue 1: Reply Count Sync
**Problem:** Reply count might get out of sync if replies are deleted
**Solution:** Add background job to recalculate counts periodically, or implement triggers

### Issue 2: Deep Thread Performance
**Problem:** Loading a thread with 100+ nested comments is slow
**Solution:** Implement pagination at every level, not just top-level

### Issue 3: Deleted Comment in Thread
**Problem:** If parent comment is deleted, what happens to replies?
**Current Behavior:** Soft delete (IsDeleted = true)
**UX Decision Needed:** 
- Show "[deleted]" placeholder with replies intact?
- Or hide entire thread?

**Recommendation:** Show placeholder, keep replies visible

### Issue 4: Real-time Updates
**Problem:** User A replies while User B is viewing comments
**Current:** User B won't see new reply until refresh
**Solution Phase 4:** Implement SignalR notifications

---

## Questions for Product Owner

Before starting implementation, clarify:

1. **Max Thread Depth:** Should we limit nesting to 5 levels, or allow unlimited?
2. **Reply Sorting:** Default sort order for replies (chronological, or most liked)?
3. **Deleted Comments:** Show placeholder or hide entire thread?
4. **Notifications:** Should we notify on every reply, or only direct replies?
5. **Mobile Experience:** Collapse threads by default on mobile?
6. **Reply Editing:** Can users edit their replies, same as top-level comments?
7. **Load More:** "Load more replies" pagination after how many replies (10, 20)?

---

## Next Steps

1. **Review this plan** with team
2. **Get answers** to product owner questions
3. **Create tickets** for Sprint 1 tasks
4. **Start with Phase 1:** Client layer implementation
5. **Iterate** based on feedback

---

## Estimated Effort

| Phase | Tasks | Story Points | Duration |
|-------|-------|--------------|----------|
| Phase 1: Client Layer | 3 | 5 | 2-3 days |
| Phase 2: UI Components | 3 | 8 | 3-4 days |
| Phase 3: Styling & UX | 3 | 5 | 2-3 days |
| Phase 4: Advanced (Optional) | 4 | 13 | 5-7 days |
| **Total (Core)** | **9** | **18** | **7-10 days** |
| **Total (with Advanced)** | **13** | **31** | **12-17 days** |

---

## Conclusion

The **backend infrastructure is 100% ready** for replies. The work needed is entirely **frontend/UI focused**:

1. Wire up existing API endpoints in client layer
2. Build reply UI components
3. Implement lazy loading and threading display
4. Polish UX with styling and loading states

This is a well-scoped feature that can deliver significant value to the activity stream platform. The phased approach allows for iterative delivery and user feedback.

---

## Quick Reference: Key Files to Modify

### Frontend Files (New/Modified):
```
Sivar.Os.Client/
├── Components/Feed/
│   ├── CommentSection.razor ← UPDATE (lazy loading, top-level only)
│   ├── CommentItem.razor ← MAJOR UPDATE (threading, replies, depth)
│   └── ReplyInput.razor ← CREATE NEW (inline reply input)
├── Clients/
│   └── CommentsClient.cs ← UPDATE (add reply methods)
└── wwwroot/css/
    └── comments.css ← CREATE NEW (Instagram-style CSS)

Sivar.Os.Shared/
└── Clients/
    └── ICommentsClient.cs ← UPDATE (interface for reply methods)

Sivar.Os/
└── Services/Clients/
    └── CommentsClient.cs ← UPDATE (server-side client)
```

### Backend Files (Minor Updates):
```
Sivar.Os/
└── Controllers/
    └── CommentsController.cs ← OPTIONAL UPDATE (topLevelOnly param)
```

---

## Development Checklist

### Sprint 1: Client Layer (2-3 days)
- [ ] Update `ICommentsClient` interface with reply methods
- [ ] Implement `CreateReplyAsync` in server-side client
- [ ] Implement `GetRepliesAsync` in server-side client
- [ ] Implement `GetTopLevelByPostAsync` in server-side client
- [ ] Mirror implementations in WASM client
- [ ] Test API calls with Postman/Swagger
- [ ] Update `CommentsController` with `topLevelOnly` param (optional)

### Sprint 2: UI Components (3-4 days)
- [ ] Create `ReplyInput.razor` component
  - [ ] Auto-focus on mount
  - [ ] Character counter
  - [ ] Post/Cancel buttons
  - [ ] Auto-grow textarea
- [ ] Update `CommentItem.razor`
  - [ ] Add "Reply" button
  - [ ] Add "View replies (N)" toggle
  - [ ] Implement lazy loading of replies
  - [ ] Add recursive rendering of nested replies
  - [ ] Add depth tracking (0, 1, 2...)
  - [ ] Add optimistic update for new replies
- [ ] Update `CommentSection.razor`
  - [ ] Change to load top-level only
  - [ ] Reduce initial page size to 5
  - [ ] Update pagination to "Load more" button
  - [ ] Add skeleton loaders

### Sprint 3: Styling & Polish (2-3 days)
- [ ] Create `comments.css` with Instagram patterns
  - [ ] Horizontal inline layout (username · time · actions)
  - [ ] "View replies" with connecting line
  - [ ] Depth-based indentation (40px, 80px)
  - [ ] Hover states and transitions
- [ ] Implement loading states
  - [ ] Skeleton loaders for initial comments
  - [ ] Spinner for "Load more"
  - [ ] Inline spinner for loading replies
- [ ] Add optimistic updates
  - [ ] Instant UI add for new comments/replies
  - [ ] Rollback on API failure
- [ ] Accessibility
  - [ ] ARIA labels for all interactive elements
  - [ ] Keyboard navigation (Tab, Enter, Escape)
  - [ ] Screen reader announcements
  - [ ] Focus management

### Sprint 4: Testing & Refinement (1-2 days)
- [ ] Manual testing
  - [ ] Create top-level comments
  - [ ] Create replies (1 level deep)
  - [ ] Create nested replies (2 levels deep)
  - [ ] Test pagination (Load more comments)
  - [ ] Test reply pagination (Load more replies)
  - [ ] Test delete comment/reply
  - [ ] Test mobile responsive design
- [ ] Performance testing
  - [ ] Load time for 100+ comments
  - [ ] Memory usage with deep threads
  - [ ] Network tab (API call efficiency)
- [ ] Accessibility audit
  - [ ] Lighthouse accessibility score >90
  - [ ] Screen reader testing (NVDA/JAWS)
  - [ ] Keyboard-only navigation

---

## Instagram Pattern: Code Quick Copy

### Inline Comment Header (Instagram Style)
```html
<div class="comment-header-inline">
    <span class="comment-username">@Comment.Profile.DisplayName</span>
    <span class="comment-separator">·</span>
    <span class="comment-time">2h</span>
</div>
```

### View Replies Button (with line)
```html
<button class="view-replies-btn" @onclick="ToggleReplies">
    <span class="view-replies-line">—</span> 
    View replies (@Comment.ReplyCount)
</button>
```

### CSS: Depth Indentation
```css
.comment-item.depth-0 { margin-left: 0; }
.comment-item.depth-1 { margin-left: 40px; }
.comment-item.depth-2 { margin-left: 80px; border-left: 1px solid var(--border); }
```

### Lazy Load Replies
```csharp
private async Task ToggleReplies()
{
    _showReplies = !_showReplies;
    
    if (_showReplies && !_replies.Any())
    {
        await LoadReplies(); // First time only
    }
}
```

### Optimistic Update
```csharp
// Add to UI immediately
_comments.Insert(0, optimisticComment);
StateHasChanged();

// Then call API
var realComment = await SivarClient.Comments.CreateCommentAsync(dto);

// Replace optimistic with real
var index = _comments.FindIndex(c => c.Id == optimisticComment.Id);
_comments[index] = realComment;
```

---

## Success Metrics (Post-Implementation)

Track these KPIs after launch:

1. **Engagement**
   - % increase in total comments/replies (target: +30%)
   - % of comments that receive replies (target: >20%)
   - Average reply depth (expect: 1.2-1.5)

2. **Performance**
   - Initial comment load time (target: <500ms)
   - Time to create reply (target: <1s)
   - Largest Contentful Paint (target: <2.5s)

3. **UX**
   - Reply creation failure rate (target: <1%)
   - % of users who expand replies (target: >40%)
   - Mobile vs desktop usage patterns

4. **Accessibility**
   - Lighthouse accessibility score (target: >90)
   - Keyboard navigation success rate (target: 100%)

---

## FAQ

### Q: Why Instagram pattern instead of Reddit/Twitter style?
**A:** Instagram's pattern is simpler, more visual, and performs better:
- Lazy loading by default (faster initial load)
- Limited nesting (prevents performance issues)
- Clean, minimal design (works on mobile)
- Users already familiar with the pattern

### Q: Why limit nesting to 2 levels?
**A:** Deep nesting (5+ levels) creates:
- UI complexity (hard to read on mobile)
- Performance issues (recursive rendering)
- Database query complexity
- Confusing conversation threads

Instagram limits to 1-2 levels and it works well. For deeper discussions, users should create new top-level comments.

### Q: What happens to existing comments?
**A:** They're already compatible! Existing comments have `ParentCommentId = null`, so they'll display as top-level. No migration needed.

### Q: Can we change the max depth later?
**A:** Yes, it's a parameter (`MaxDepth`) in `CommentItem`. Easy to adjust, but test performance first.

### Q: What about real-time updates?
**A:** Phase 4 (optional). Would use SignalR to push new replies to connected clients. Not critical for MVP.

---

## Timeline & Effort Summary

| Sprint | Focus Area | Days | Story Points |
|--------|-----------|------|--------------|
| 1 | Client Layer (API) | 2-3 | 5 |
| 2 | UI Components | 3-4 | 8 |
| 3 | Styling & Polish | 2-3 | 5 |
| 4 | Testing & Refinement | 1-2 | 3 |
| **Total (Core)** | **Minimum Viable** | **8-12** | **21** |

**Optional Enhancements** (Phase 4):
- Real-time updates via SignalR: +3 days
- @mentions with autocomplete: +2 days
- Emoji reactions on replies: +2 days
- Collapsible threads: +1 day

---

## Next Steps

1. ✅ **Review this plan** with team and product owner
2. ✅ **Get answers** to product questions (max depth, sorting, notifications)
3. ✅ **Create tickets** for Sprint 1 tasks in your project management tool
4. ✅ **Set up branch**: `feature/comment-replies`
5. ✅ **Start with Phase 1**: Client layer implementation (lowest risk)
6. ✅ **Daily demos**: Show progress to stakeholders
7. ✅ **Iterate**: Gather feedback after each sprint

**Ready to start coding!** 🚀
