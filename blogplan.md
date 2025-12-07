# Blog System Implementation Plan

## Executive Summary

This document analyzes the current Sivar.Os activity stream system and proposes implementation strategies for adding blog functionality. After analyzing the existing architecture, there are **three main approaches** to consider, each with distinct trade-offs.

---

## Current System Analysis

### Existing Architecture

#### Post Entity (Current Structure)
- **Content limit**: 5,000 characters (suitable for social posts, not blogs)
- **PostType enum**: `General`, `BusinessLocation`, `Product`, `Service`, `Event`, `JobPosting`
- **Supports**: Title, Tags, Attachments, Visibility, Location, Sentiment Analysis, Vector Embeddings
- **Profile-based**: Every post belongs to a Profile (Personal, Business, Organization)

#### ProfileType System
- **Dynamic entity-based** (not enum) - allows creating new profile types
- **Current types**: `PersonalProfile`, `BusinessProfile`, `OrganizationProfile`
- **FeatureFlags**: JSON-based feature toggles per profile type
- **AllowedFeatures**: Controls what each profile type can do

#### Post Composer UI
- Filters PostType options based on `CurrentProfileTypeName`
- Business profiles get all post types
- Personal profiles only get "General" type
- Already has infrastructure for rich options (schedule, media, advanced settings)

---

## Implementation Approaches

### Approach 1: Blog as a New PostType ⭐ (Recommended)

**Strategy**: Add `Blog = 7` to the `PostType` enum and extend the `Post` entity to handle long-form content.

#### Pros
- ✅ **Minimal schema changes** - reuses existing infrastructure
- ✅ **Unified activity stream** - blogs appear in feeds naturally
- ✅ **Existing features work** - comments, reactions, sharing, search all work
- ✅ **Single content table** - no JOIN complexity
- ✅ **Consistent APIs** - same endpoints for all content types

#### Cons
- ⚠️ Requires increasing content limit for blogs (or adding separate `BlogContent` field)
- ⚠️ May need separate rendering logic for blog cards vs. regular posts

#### Implementation Details

```
1. Schema Changes:
   - Add `Blog = 7` to PostType enum
   - Add `BlogContent` (TEXT/unlimited) to Post entity for full content
   - Add `Summary` field (500 chars) for feed previews
   - Add `ReadTimeMinutes` field (calculated)
   - Add `CoverImageUrl` field
   - Add `PublishedAt` field (separate from CreatedAt for draft support)
   - Add `IsDraft` boolean field

2. FeatureFlags Update:
   - Add "AllowsBlogging" flag to ProfileType.FeatureFlags
   - Enable for Personal/Business/Organization as desired

3. UI Components:
   - New BlogComposer component (rich text editor)
   - BlogCard component for feed (shows summary + cover image)
   - BlogPage component for full reading experience
   - BlogDrafts section in profile

4. API Extensions:
   - GetDrafts endpoint
   - PublishDraft endpoint
   - Blog-specific search/filtering
```

---

### Approach 2: Blog as a Separate Entity

**Strategy**: Create a new `Blog` entity with its own table, separate from `Post`.

#### Pros
- ✅ Clean separation of concerns
- ✅ Can have blog-specific fields without affecting Post table
- ✅ Independent optimization (different indexes, caching)
- ✅ Better for very different data structures

#### Cons
- ⚠️ **More complex** - new entity, repository, service, controller, DTOs
- ⚠️ **Feed complexity** - need to merge Posts and Blogs in activity feed
- ⚠️ **Feature duplication** - reactions, comments need linking to both types
- ⚠️ **More migrations** - new tables, foreign keys, indexes

#### Implementation Details

```
1. New Entities:
   - Blog entity (extends BaseEntity)
   - BlogCategory entity
   - BlogComment entity (or reuse Comment with polymorphism)

2. New DbSet:
   - DbSet<Blog> Blogs

3. New Services:
   - IBlogService / BlogService
   - IBlogRepository / BlogRepository

4. Activity Stream Integration:
   - Modify Activity entity to support ObjectType="Blog"
   - Merge blog activities in feed queries
```

---

### Approach 3: BlogProfile - New Profile Type

**Strategy**: Create a `BlogProfile` or `BloggerProfile` profile type that unlocks blogging features.

#### Pros
- ✅ No entity changes - uses existing Post with higher limits
- ✅ Clean permission model - profile type controls capabilities
- ✅ Easy to implement - just add new ProfileType row

#### Cons
- ⚠️ Conflates profile type with feature (a business can also blog)
- ⚠️ Doesn't solve content length issue for blogs
- ⚠️ May confuse users (why do I need a "Blog Profile"?)

#### Implementation Details

```
1. Seed new ProfileType:
   - Name: "BlogProfile" or "ContentCreator"
   - FeatureFlags: AllowsBlogging, AllowsLongFormContent, etc.
   - MaxBioLength: 5000
   
2. Update content limits:
   - Check ProfileType when validating post length
   - Blog profiles can post up to 50,000 characters

3. UI:
   - Show blog-specific composer when ProfileType has AllowsBlogging
```

---

## Detailed Recommendation: Approach 1 (Blog as PostType)

Based on the existing architecture, **Approach 1 is recommended** because:

1. **The system is already designed around PostType differentiation** - the Post entity handles multiple content types (products, services, events) through the PostType enum and metadata fields
2. **The activity stream integrates naturally** - no special handling needed
3. **Existing features carry over** - sentiment analysis, vector embeddings, search, reactions all work automatically
4. **UI pattern already exists** - PostComposer already filters options by profile type

### Detailed Implementation Plan

#### Phase 1: Schema & Backend (2-3 days)

##### 1.1 Update PostType Enum
```csharp
// Sivar.Os.Shared/Enums/PostEnums.cs
public enum PostType
{
    General = 1,
    BusinessLocation = 2,
    Product = 3,
    Service = 4,
    Event = 5,
    JobPosting = 6,
    Blog = 7  // NEW
}
```

##### 1.2 Extend Post Entity
```csharp
// Sivar.Os.Shared/Entities/Post.cs
public class Post : BaseEntity
{
    // Existing fields...
    
    // === NEW BLOG FIELDS ===
    
    /// <summary>
    /// Full blog content (Markdown/HTML) - only used when PostType = Blog
    /// Content field stores the summary/excerpt for blogs
    /// </summary>
    [StringLength(100000)]
    public virtual string? BlogContent { get; set; }
    
    /// <summary>
    /// Cover/featured image URL for blog posts
    /// </summary>
    [StringLength(500)]
    public virtual string? CoverImageUrl { get; set; }
    
    /// <summary>
    /// Cover image file ID from blob storage
    /// </summary>
    [StringLength(255)]
    public virtual string? CoverImageFileId { get; set; }
    
    /// <summary>
    /// Estimated read time in minutes (auto-calculated)
    /// </summary>
    public virtual int? ReadTimeMinutes { get; set; }
    
    /// <summary>
    /// Indicates if this is a draft (not yet published)
    /// </summary>
    public virtual bool IsDraft { get; set; } = false;
    
    /// <summary>
    /// When the blog was published (separate from CreatedAt for drafts)
    /// </summary>
    public virtual DateTime? PublishedAt { get; set; }
    
    /// <summary>
    /// Subtitle or excerpt for blog posts
    /// </summary>
    [StringLength(500)]
    public virtual string? Subtitle { get; set; }
    
    /// <summary>
    /// Canonical URL if republished from another source
    /// </summary>
    [StringLength(500)]
    public virtual string? CanonicalUrl { get; set; }
}
```

##### 1.3 Update PostConfiguration (EF Core)
```csharp
// Add to PostConfiguration.cs
builder.Property(p => p.BlogContent)
    .HasMaxLength(100000);

builder.Property(p => p.CoverImageUrl)
    .HasMaxLength(500);

builder.Property(p => p.Subtitle)
    .HasMaxLength(500);
```

##### 1.4 Create Migration
```bash
dotnet ef migrations add AddBlogFields -p Sivar.Os.Data -s Sivar.Os
dotnet ef database update -p Sivar.Os.Data -s Sivar.Os
```

##### 1.5 Update DTOs
```csharp
// PostDTOs.cs - Add blog-specific properties
public record CreatePostDto
{
    // Existing fields...
    
    /// <summary>
    /// Full blog content (for Blog post type only)
    /// </summary>
    public string? BlogContent { get; init; }
    
    /// <summary>
    /// Cover image URL
    /// </summary>
    public string? CoverImageUrl { get; init; }
    
    /// <summary>
    /// Blog subtitle
    /// </summary>
    public string? Subtitle { get; init; }
    
    /// <summary>
    /// Whether this is a draft
    /// </summary>
    public bool IsDraft { get; init; } = false;
}

public record PostDto
{
    // Existing fields...
    
    public string? BlogContent { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? Subtitle { get; init; }
    public int? ReadTimeMinutes { get; init; }
    public bool IsDraft { get; init; }
    public DateTime? PublishedAt { get; init; }
}
```

##### 1.6 Update FeatureFlags in ProfileType Seeding
```csharp
// Update SeedProfileTypes in Updater.cs
personalProfileType.FeatureFlags = @"{
    ""AllowsDisplayName"": true,
    ""AllowsBio"": true,
    ""AllowsAvatar"": true,
    ""AllowsLocation"": true,
    ""AllowsBookings"": false,
    ""AllowsProducts"": false,
    ""AllowsContactInfo"": true,
    ""AllowsBlogging"": true,  // NEW
    ""MaxBioLength"": 1000
}";

businessProfileType.FeatureFlags = @"{
    ""AllowsDisplayName"": true,
    ""AllowsBio"": true,
    ""AllowsAvatar"": true,
    ""AllowsLocation"": true,
    ""AllowsBookings"": true,
    ""AllowsProducts"": true,
    ""AllowsContactInfo"": true,
    ""AllowsBlogging"": true,  // NEW
    ""MaxBioLength"": 2000
}";
```

##### 1.7 Update PostService
```csharp
// PostService.cs - Add blog-specific logic
public async Task<PostDto?> CreatePostAsync(string keycloakId, CreatePostDto createPostDto)
{
    // Existing validation...
    
    var post = new Post
    {
        // Existing mappings...
        
        // Blog-specific
        BlogContent = createPostDto.BlogContent,
        CoverImageUrl = createPostDto.CoverImageUrl,
        Subtitle = createPostDto.Subtitle,
        IsDraft = createPostDto.IsDraft,
        ReadTimeMinutes = CalculateReadTime(createPostDto.BlogContent),
        PublishedAt = createPostDto.IsDraft ? null : DateTime.UtcNow
    };
}

private int CalculateReadTime(string? content)
{
    if (string.IsNullOrEmpty(content)) return 0;
    
    // Average reading speed: 200-250 words per minute
    var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    return Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
}
```

---

#### Phase 2: UI Components (3-4 days)

##### 2.1 Blog Composer Component
Create a new `BlogComposer.razor` component with:
- Rich text editor (consider TinyMCE, Quill, or TipTap)
- Cover image upload
- Title and subtitle fields
- Tag selection
- Draft save functionality
- Publish button with scheduling option

##### 2.2 Blog Card Component
Create a new `BlogCard.razor` for feed display:
- Cover image hero
- Title and subtitle
- Author info
- Read time
- Summary/excerpt
- "Read More" call to action

##### 2.3 Blog Page Component
Create new page `BlogPost.razor` at route `/blog/{id}`:
- Full blog content rendering (Markdown to HTML)
- Table of contents (optional)
- Comments section
- Reactions
- Share buttons
- Related posts

##### 2.4 Update PostComposer Options
```csharp
// PostComposer.razor - Add blog option
[Parameter]
public List<PostTypeOption> PostTypeOptions { get; set; } = new()
{
    new("general", "General", Icons.Material.Filled.Article, "Share updates."),
    new("blog", "Blog", Icons.Material.Filled.EditNote, "Write a long-form article."),  // NEW
    new("product", "Product", Icons.Material.Filled.Storefront, "Highlight products."),
    // ... existing options
};

private List<PostTypeOption> GetFilteredPostTypeOptions()
{
    var profile = CurrentProfileTypeName?.ToLower();
    
    // Check if blogging is allowed for this profile type
    if (HasFeature("AllowsBlogging"))
    {
        // Show blog option
    }
    
    // ... existing logic
}
```

##### 2.5 Update Feed to Handle Blog Cards
```razor
// Home.razor - Differentiate rendering
@foreach (var activity in _activities)
{
    @if (activity.Post != null)
    {
        @if (activity.Post.PostType == PostType.Blog)
        {
            <BlogCard Post="@activity.Post" ... />
        }
        else
        {
            <PostCard Post="@activity.Post" ... />
        }
    }
}
```

---

#### Phase 3: API & Search (1-2 days)

##### 3.1 Blog-Specific Endpoints
```csharp
// PostsController.cs

/// <summary>
/// Get user's draft blogs
/// </summary>
[HttpGet("drafts")]
public async Task<ActionResult<IEnumerable<PostDto>>> GetDrafts()

/// <summary>
/// Publish a draft blog
/// </summary>
[HttpPost("{id}/publish")]
public async Task<ActionResult<PostDto>> PublishDraft(Guid id)

/// <summary>
/// Get blogs by profile
/// </summary>
[HttpGet("profile/{profileId}/blogs")]
public async Task<ActionResult<IEnumerable<PostDto>>> GetProfileBlogs(
    Guid profileId, 
    [FromQuery] int page = 1, 
    [FromQuery] int pageSize = 10)
```

##### 3.2 Update Search to Include BlogContent
```csharp
// PostRepository.cs - Update search to include BlogContent field
public async Task<IEnumerable<Post>> SearchPostsAsync(string query, ...)
{
    // Include BlogContent in full-text search for Blog type posts
    query = query.Where(p => 
        p.Content.Contains(searchTerm) ||
        (p.Title != null && p.Title.Contains(searchTerm)) ||
        (p.BlogContent != null && p.BlogContent.Contains(searchTerm)) ||  // NEW
        ...
    );
}
```

##### 3.3 Update Full-Text Search Vectors
```sql
-- Update the SearchVector generation to include BlogContent for blogs
ALTER TABLE "Sivar_Posts" 
DROP COLUMN IF EXISTS "SearchVector";

ALTER TABLE "Sivar_Posts" 
ADD COLUMN "SearchVector" tsvector 
GENERATED ALWAYS AS (
    to_tsvector(
        -- language config based on Language field
        CASE 
            WHEN "Language" = 'es' THEN 'spanish'::regconfig
            ELSE 'english'::regconfig
        END,
        coalesce("Title", '') || ' ' || 
        "Content" || ' ' ||
        CASE WHEN "PostType" = 7 THEN coalesce("BlogContent", '') ELSE '' END
    )
) STORED;
```

---

#### Phase 4: Profile Blog Section (1-2 days)

##### 4.1 Profile Blog Tab
Add a "Blog" tab to the profile page that shows:
- All published blogs by this profile
- Statistics (total posts, views, read time)
- Featured/pinned blog posts

##### 4.2 Blog Management Page
Create `/blogs/manage` for users to:
- See all their drafts
- View published blogs
- Edit/unpublish/delete blogs
- View analytics per blog

---

## Database Migration Script (Phase 1)

```sql
-- Migration: Add Blog fields to Sivar_Posts table

-- 1. Add new columns
ALTER TABLE "Sivar_Posts" ADD COLUMN IF NOT EXISTS "BlogContent" TEXT;
ALTER TABLE "Sivar_Posts" ADD COLUMN IF NOT EXISTS "CoverImageUrl" VARCHAR(500);
ALTER TABLE "Sivar_Posts" ADD COLUMN IF NOT EXISTS "CoverImageFileId" VARCHAR(255);
ALTER TABLE "Sivar_Posts" ADD COLUMN IF NOT EXISTS "ReadTimeMinutes" INTEGER;
ALTER TABLE "Sivar_Posts" ADD COLUMN IF NOT EXISTS "IsDraft" BOOLEAN DEFAULT FALSE;
ALTER TABLE "Sivar_Posts" ADD COLUMN IF NOT EXISTS "PublishedAt" TIMESTAMP;
ALTER TABLE "Sivar_Posts" ADD COLUMN IF NOT EXISTS "Subtitle" VARCHAR(500);
ALTER TABLE "Sivar_Posts" ADD COLUMN IF NOT EXISTS "CanonicalUrl" VARCHAR(500);

-- 2. Add index for draft queries
CREATE INDEX IF NOT EXISTS "IX_Posts_IsDraft_ProfileId" 
ON "Sivar_Posts" ("IsDraft", "ProfileId") 
WHERE "PostType" = 7;

-- 3. Add index for published blog queries
CREATE INDEX IF NOT EXISTS "IX_Posts_Blog_PublishedAt" 
ON "Sivar_Posts" ("PublishedAt" DESC) 
WHERE "PostType" = 7 AND "IsDraft" = FALSE;
```

---

## Rich Text Editor Recommendation

For blog content editing, consider these options:

### Option A: TinyMCE (Recommended)
- Full-featured WYSIWYG
- Blazor wrapper available: `TinyMCE.Blazor`
- Supports images, formatting, tables
- Has free tier

### Option B: Quill.js
- Lighter weight
- Good Blazor support
- More customizable

### Option C: Markdig + Monaco
- Markdown editing with live preview
- Developer-friendly
- Uses Monaco editor (VS Code editor)

```xml
<!-- Package reference for TinyMCE -->
<PackageReference Include="TinyMCE.Blazor" Version="1.*" />
```

---

## Future Considerations

1. **Blog Categories/Series**: Add a `BlogCategory` or `BlogSeries` entity for organizing content
2. **RSS Feeds**: Generate RSS for blog posts per profile
3. **SEO Optimization**: Meta tags, Open Graph, structured data for blogs
4. **Newsletter Integration**: Email subscribers when new blog is published
5. **Cross-posting**: Import/export to Medium, Dev.to, etc.
6. **Monetization**: Paid subscribers, member-only content
7. **Co-authors**: Multiple authors per blog post

---

## Timeline Summary

| Phase | Description | Estimated Time |
|-------|-------------|----------------|
| Phase 1 | Schema & Backend | 2-3 days |
| Phase 2 | UI Components | 3-4 days |
| Phase 3 | API & Search | 1-2 days |
| Phase 4 | Profile Blog Section | 1-2 days |
| **Total** | | **7-11 days** |

---

## Conclusion

Adding blog functionality to Sivar.Os is best achieved by extending the existing Post entity with a new `PostType.Blog` value. This approach leverages the existing activity stream, reactions, comments, and search infrastructure while adding blog-specific fields for long-form content.

The implementation is modular and can be rolled out in phases, with the backend changes done first to enable API testing, followed by UI components.

### Next Steps
1. Review and approve this plan
2. Decide on rich text editor choice
3. Decide which profile types should have blogging enabled
4. Begin Phase 1 implementation
