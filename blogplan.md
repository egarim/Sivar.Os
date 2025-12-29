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

---

## WordPress Migration Plan (jocheojeda.com)

> **Goal**: Migrate personal blog from WordPress to Sivar.Os with a streamlined, modern approach.

### Design Philosophy

Instead of replicating WordPress 1:1, we take a **simplified approach**:

| WordPress Feature | Sivar.Os Approach | Rationale |
|-------------------|-------------------|-----------|
| 60+ Categories (hierarchical) | **Tags only** | Flat tags with vector search makes categories redundant |
| Monthly Archives | **Semantic search** | AI-powered search replaces date browsing |
| Sidebar widgets | **Clean single-column** | Modern blog design, mobile-first |
| Comments (WordPress) | **Existing comment system** | Already implemented |
| Related Posts (manual) | **AI-powered suggestions** | Use embeddings for true semantic similarity |

### Key Insight: Embeddings Replace Categories

With pgvector embeddings on every post, we can:
- Find related content **semantically** (not just by category)
- Show "More like this" based on actual content similarity
- Eliminate category maintenance overhead


---

### Streamlined Feature List

#### Must Have (Phase 1)
1. **SEO Slug URLs** - `/blog/{slug}` for permalink redirects
2. **Blog Landing Page** - `/blog` showing blog posts only
3. **Related Posts** - AI-powered via embeddings
4. **Code Syntax Highlighting** - For technical blog posts

#### Nice to Have (Phase 2)
5. **RSS Feed** - `/blog/feed`
6. **Search within blogs** - Filter by PostType.Blog
7. **Featured/Pinned posts**

#### Skip (WordPress legacy)
- ❌ Hierarchical categories → Use flat tags
- ❌ Monthly archives → Use search
- ❌ Sidebar → Clean single-column layout
- ❌ Widgets → Not needed


---

### Schema Changes Required

```csharp
// Add to Post entity
public virtual string? Slug { get; set; }        // SEO-friendly URL
public virtual bool IsFeatured { get; set; }     // Pin to top
```

---

### Related Posts: Embedding-Based Similarity

The killer feature: **AI-powered related content** using existing pgvector embeddings.

```sql
-- Find 5 most similar blog posts to current post
SELECT p."Id", p."Content", p."Slug",
       p."ContentEmbedding" <=> current_embedding AS distance
FROM "Sivar_Posts" p
WHERE p."PostType" = 7  -- Blog only
  AND p."Id" != current_post_id
  AND p."IsDeleted" = false
  AND p."Visibility" = 0  -- Public
ORDER BY distance
LIMIT 5;
```

This replaces WordPress "Related Posts" plugins with true semantic similarity.


---

### New Pages/Components

| Component | Route | Purpose |
|-----------|-------|---------|
| `BlogHome.razor` | `/blog` | Blog listing page |
| `BlogPost.razor` | `/blog/{slug}` | Single post view with related |
| `RelatedPosts.razor` | (component) | AI-powered similar content |
| `CodeBlock.razor` | (component) | Syntax highlighting wrapper |

---

### Implementation Phases

#### Phase 1: Core Blog Public Page (Current Focus)
- [ ] Add `Slug` field to Post entity
- [ ] Create `BlogHome.razor` page at `/blog`
- [ ] Create `BlogPost.razor` page at `/blog/{slug}`
- [ ] Add `RelatedPosts.razor` component using embeddings
- [ ] Add code syntax highlighting (Prism.js)

#### Phase 2: Migration & SEO
- [ ] WordPress export script (WP REST API to Sivar.Os)
- [ ] 301 redirect middleware for old URLs
- [ ] RSS feed endpoint
- [ ] Meta tags (Open Graph, Twitter)

#### Phase 3: Polish
- [ ] Featured/pinned posts
- [ ] Blog search filtering
- [ ] Reading progress indicator


---

### WordPress URL Redirect Strategy

Map old WordPress URLs to new Sivar.Os URLs:

| WordPress URL | Sivar.Os URL |
|---------------|--------------|
| `/2025/12/23/post-title/` | `/blog/post-title` |
| `/category/xaf/` | `/blog?tag=xaf` |
| `/author/joche/` | `/profile/joche` |

Create redirect controller or middleware for 301 redirects.


---

### Next Action Items

1. **Add `Slug` to Post entity** - Required for SEO URLs
2. **Create BlogHome.razor** - Public blog listing at `/blog`
3. **Create BlogPost.razor** - Single post view at `/blog/{slug}`
4. **Implement RelatedPosts** - Use existing pgvector embeddings
5. **Add Prism.js** - Code syntax highlighting for technical posts


---

### Image Migration Strategy

WordPress images need to be migrated to Azure Blob Storage. Three options:

#### Option 1: Keep WordPress URLs (Quick but Risky) ❌
- Keep `https://www.jocheojeda.com/wp-content/uploads/...` URLs
- **Pros**: Zero migration effort
- **Cons**: If WordPress goes down, all images break

#### Option 2: Migrate to Azure Blob Storage (Recommended) ✅
- Download all images from WordPress
- Upload to Azure Blob Storage (`blog-images` container)
- Update URLs in BlogContent to point to new blob URLs

**Migration Script Flow:**
```
1. Parse WordPress export for image URLs
2. Download each image
3. Upload to Azure Blob via IFileStorageService
4. Get new blob URL
5. Replace old URL with new URL in content
```

#### Option 3: CDN Proxy (Hybrid)
- Use Cloudflare or Azure CDN as proxy
- CDN caches WordPress images
- Gradually migrate to blob storage

---

### Image URL Replacement in Content

WordPress blog posts have embedded images like:
```html
<img src="https://www.jocheojeda.com/wp-content/uploads/2024/05/image.png" />
```

Migration script must:
1. Find all `<img src="...jocheojeda.com...">` in BlogContent
2. Download each image
3. Upload to Azure Blob Storage
4. Replace URL in content

**Regex Pattern:**
```csharp
var wpImagePattern = @"https?://www\.jocheojeda\.com/wp-content/uploads/[^\s""'>]+";
```

---

### Cover Images

For blog cover images (`CoverImageUrl` field):
- Same process: download and re-upload
- Store blob URL in `CoverImageUrl`
- Store blob FileId in `CoverImageFileId` (existing fields)

---

### Image Storage Structure in Azure Blob

```
blog-images/
├── 2024/
│   ├── 05/
│   │   ├── {fileId}/image.png
│   │   └── {fileId}/diagram.jpg
│   └── 12/
│       └── {fileId}/screenshot.png
└── 2025/
    └── 12/
        └── {fileId}/xpo-odbc.png
```

Uses existing hierarchical namespace pattern from `AzureBlobStorageService`.


---

### Apache Redirect Configuration (Ubuntu/Virtualmin)

Since WordPress is hosted on Ubuntu with Virtualmin, use Apache .htaccess for SEO-friendly 301 redirects.

#### Phase 1: Add .htaccess Rules (Keep WordPress Running)

Add to WordPress root `.htaccess`:

```apache
RewriteEngine On
RewriteBase /

# === SIVAR.OS REDIRECTS ===

# Blog posts: /2025/12/23/post-slug/ -> sivar.os/blog/post-slug
RewriteCond %{REQUEST_URI} ^/\d{4}/\d{2}/\d{2}/([^/]+)/?$
RewriteRule ^.*/([^/]+)/?$ https://sivar.os/blog/$1 [R=301,L]

# Categories -> Tags
RewriteRule ^category/(.*)$ https://sivar.os/blog?tag=$1 [R=301,L]

# Author page
RewriteRule ^author/egarim/?$ https://sivar.os/profile/joche [R=301,L]

# === KEEP THESE DURING MIGRATION ===

# Keep wp-content for images
RewriteCond %{REQUEST_URI} ^/wp-content/
RewriteRule .* - [L]

# Keep wp-admin accessible
RewriteCond %{REQUEST_URI} ^/wp-admin/
RewriteRule .* - [L]
```


#### Phase 2: Full Redirect (After Migration Complete)

Replace WordPress VirtualHost with pure redirect:

```apache
<VirtualHost *:443>
    ServerName www.jocheojeda.com
    ServerAlias jocheojeda.com
    
    # Blog posts
    RedirectMatch 301 "^/\d{4}/\d{2}/\d{2}/([^/]+)/?$" "https://sivar.os/blog/$1"
    
    # Categories to tags
    RedirectMatch 301 "^/category/(.+)/?$" "https://sivar.os/blog?tag=$1"
    
    # Home and everything else
    Redirect 301 "/" "https://sivar.os/blog"
</VirtualHost>
```

#### Test Redirects

```bash
# Test blog post
curl -I "https://www.jocheojeda.com/2025/12/23/post-slug/"
# Expected: HTTP/1.1 301, Location: https://sivar.os/blog/post-slug

# Test category
curl -I "https://www.jocheojeda.com/category/xaf/"
# Expected: Location: https://sivar.os/blog?tag=xaf
```

#### Migration Timeline

| Phase | WordPress | Redirects | Images |
|-------|-----------|-----------|--------|
| **Now** | Running | .htaccess rules | On WordPress |
| **During** | Running | Active | Migrating to Blob |
| **After** | Shutdown | VirtualHost only | On Azure Blob |



---

## Sivar.Os URL Routing Strategy

### Current Routing Architecture

Based on analysis of the existing codebase, Sivar.Os uses Blazor's built-in routing:

| Current Route | Page | Auth | Description |
|---------------|------|------|-------------|
| `/` | Landing.razor | Anonymous | Landing page (redirects to /home if authenticated) |
| `/home` | Home.razor | Required | Authenticated feed |
| `/explore` | Explore.razor | Anonymous | Public content discovery |
| `/public` | Explore.razor | Anonymous | Alias for /explore |
| `/post/{PostId:guid}` | PostDetail.razor | Required | Single post view (GUID-based) |
| `/{Identifier}` | ProfilePage.razor | Required | Profile by GUID or username |
| `/search` | Search.razor | Required | Search page |

### Key Observations

1. **Current post URLs use GUIDs**: `/post/a1b2c3d4-...`
   - Good for uniqueness
   - Bad for SEO and sharing
   - WordPress uses: `/2025/12/23/post-slug/`

2. **Profile pages support slugs**: `/{Identifier}` accepts both GUID and username
   - This pattern should be replicated for blog posts

3. **Anonymous access via `[AllowAnonymous]`**: Explore page demonstrates pattern
   - Blog pages should follow same pattern


---

### Proposed Blog URL Structure

#### Primary Routes (Public)

| Route | Page | Auth | Description |
|-------|------|------|-------------|
| `/blog` | BlogHome.razor | Anonymous | Blog listing (all blog posts) |
| `/blog/{slug}` | BlogPost.razor | Anonymous | Single blog post by slug |
| `/blog/tag/{tag}` | BlogHome.razor | Anonymous | Filter by tag |

#### Secondary Routes (Authenticated)

| Route | Page | Auth | Description |
|-------|------|------|-------------|
| `/blog/drafts` | BlogDrafts.razor | Required | User's draft posts |
| `/blog/manage` | BlogManage.razor | Required | Blog management dashboard |


---

### Slug Field Implementation

Add `Slug` to Post entity:

```csharp
// Post.cs - Add after CanonicalUrl field
/// <summary>
/// SEO-friendly URL slug for blog posts (e.g., "my-first-blog-post")
/// Generated from title, must be unique per profile
/// </summary>
[StringLength(200)]
public virtual string? Slug { get; set; }
```

#### Slug Generation Rules

1. Convert title to lowercase
2. Replace spaces with hyphens
3. Remove special characters (keep alphanumeric and hyphens)
4. Truncate to 200 chars
5. Ensure uniqueness (append number if collision)

```csharp
// SlugHelper.cs
public static string GenerateSlug(string title)
{
    var slug = title.ToLowerInvariant()
        .Replace(" ", "-")
        .Replace("--", "-");
    
    // Remove non-alphanumeric except hyphens
    slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
    
    // Truncate
    if (slug.Length > 200) slug = slug[..200];
    
    return slug.Trim('-');
}
```


---

### BlogHome.razor Implementation

```razor
@page "/blog"
@page "/blog/tag/{Tag}"
@attribute [Microsoft.AspNetCore.Authorization.AllowAnonymous]
@using MudBlazor

<PageTitle>Blog - Sivar.Os</PageTitle>

<div class="blog-home">
    <AuthorizeView>
        <NotAuthorized>
            <JoinCta DisplayVariant="banner" />
        </NotAuthorized>
    </AuthorizeView>
    
    <h1>Blog</h1>
    
    @if (!string.IsNullOrEmpty(Tag))
    {
        <MudChip T="string" Color="Color.Primary">Tag: @Tag</MudChip>
    }
    
    @foreach (var post in _blogPosts)
    {
        <BlogCard Post="@post" OnClick="HandlePostClick" />
    }
</div>

@code {
    [Parameter]
    public string? Tag { get; set; }
    
    private List<PostDto> _blogPosts = new();
    
    private void HandlePostClick(PostDto post)
    {
        // Navigate using slug if available, fallback to GUID
        var url = !string.IsNullOrEmpty(post.Slug) 
            ? $"/blog/{post.Slug}" 
            : $"/post/{post.Id}";
        Navigation.NavigateTo(url);
    }
}
```


---

### BlogPost.razor Implementation

```razor
@page "/blog/{Slug}"
@attribute [Microsoft.AspNetCore.Authorization.AllowAnonymous]

<PageTitle>@(_post?.Content ?? "Blog") - Sivar.Os</PageTitle>

@if (_post != null)
{
    <article class="blog-post">
        <!-- Cover Image -->
        @if (!string.IsNullOrEmpty(_post.CoverImageUrl))
        {
            <img src="@_post.CoverImageUrl" alt="@_post.Content" class="cover-image" />
        }
        
        <!-- Title -->
        <h1>@_post.Content</h1>
        
        <!-- Meta -->
        <div class="blog-meta">
            <span>@_post.Profile?.DisplayName</span>
            <span>@FormatDate(_post.PublishedAt ?? _post.CreatedAt)</span>
            <span>@_post.ReadTimeMinutes min read</span>
        </div>
        
        <!-- Content -->
        <div class="blog-content">
            @((MarkupString)_post.BlogContent)
        </div>
        
        <!-- Related Posts (AI-powered) -->
        <RelatedPosts CurrentPostId="@_post.Id" />
    </article>
}

@code {
    [Parameter]
    public string Slug { get; set; } = string.Empty;
    
    private PostDto? _post;
    
    protected override async Task OnParametersSetAsync()
    {
        _post = await PostsClient.GetBySlugAsync(Slug);
    }
}
```


---

### API Endpoints for Slug-Based Access

Add to PostsController.cs:

```csharp
/// <summary>
/// Gets a blog post by slug (public, no auth required)
/// </summary>
[HttpGet("slug/{slug}")]
[AllowAnonymous]
public async Task<ActionResult<PostDto>> GetPostBySlug(string slug)
{
    var post = await _postService.GetPostBySlugAsync(slug);
    
    if (post == null)
        return NotFound("Blog post not found");
    
    // Only return public blog posts
    if (post.Visibility != VisibilityLevel.Public || post.PostType != PostType.Blog)
        return NotFound("Blog post not found");
    
    return Ok(post);
}

/// <summary>
/// Gets all public blog posts (paginated)
/// </summary>
[HttpGet("blog")]
[AllowAnonymous]
public async Task<ActionResult<PagedResult<PostDto>>> GetBlogPosts(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? tag = null)
{
    var posts = await _postService.GetPublicBlogPostsAsync(page, pageSize, tag);
    return Ok(posts);
}

/// <summary>
/// Gets related blog posts using embeddings (public)
/// </summary>
[HttpGet("{id}/related")]
[AllowAnonymous]
public async Task<ActionResult<List<PostDto>>> GetRelatedPosts(Guid id, [FromQuery] int limit = 5)
{
    var relatedPosts = await _postService.GetRelatedBlogPostsAsync(id, limit);
    return Ok(relatedPosts);
}
```


---

### Backward Compatibility: GUID Fallback

The BlogPost.razor page should handle both slug and GUID:

```razor
@page "/blog/{Slug}"
@page "/post/{PostId:guid}"  // Fallback for old links
```

Logic in OnParametersSetAsync:
```csharp
protected override async Task OnParametersSetAsync()
{
    if (PostId != Guid.Empty)
    {
        // GUID route - load by ID, then redirect to slug if available
        _post = await PostsClient.GetByIdAsync(PostId);
        if (_post != null && !string.IsNullOrEmpty(_post.Slug))
        {
            // 301 redirect to canonical slug URL
            Navigation.NavigateTo($"/blog/{_post.Slug}", replace: true);
            return;
        }
    }
    else if (!string.IsNullOrEmpty(Slug))
    {
        // Slug route - load by slug
        _post = await PostsClient.GetBySlugAsync(Slug);
    }
}
```


---

### Route Conflict Prevention

**Problem**: `/{Identifier}` (ProfilePage) catches everything including `/blog`.

**Solution**: Update ProfilePage to exclude known routes:

```csharp
// ProfilePage.razor.cs - OnParametersSetAsync
protected override async Task OnParametersSetAsync()
{
    // Skip known routes to prevent conflicts
    var reservedRoutes = new[] { "blog", "search", "home", "explore", "public", "login", "signup" };
    if (reservedRoutes.Contains(Identifier?.ToLower()))
    {
        // Let Blazor router handle these
        return;
    }
    
    // Continue with profile lookup...
}
```

Alternatively, use route constraints:
```razor
@page "/{Identifier:regex(^(?!blog|search|home|explore).*$)}"
```


---

### URL Mapping: WordPress → Sivar.Os

| WordPress URL | Sivar.Os URL | Redirect |
|---------------|--------------|----------|
| `/2025/12/23/post-slug/` | `/blog/post-slug` | Apache 301 |
| `/category/xaf/` | `/blog/tag/xaf` | Apache 301 |
| `/author/egarim/` | `/@joche` or `/profile/joche` | Apache 301 |
| `/feed/` | `/blog/feed` | Future RSS |


---

### Implementation Checklist

#### Phase 1: Schema & Backend
- [ ] Add `Slug` field to Post entity
- [ ] Add EF Core migration
- [ ] Create `SlugHelper` utility class
- [ ] Add `GetPostBySlugAsync` to IPostService
- [ ] Add `GetPublicBlogPostsAsync` to IPostService
- [ ] Add `GetRelatedBlogPostsAsync` using embeddings
- [ ] Add slug-based API endpoints

#### Phase 2: Blazor Pages
- [ ] Create `BlogHome.razor` at `/blog`
- [ ] Create `BlogPost.razor` at `/blog/{slug}`
- [ ] Create `RelatedPosts.razor` component
- [ ] Update `BlogCard` navigation to use slug
- [ ] Add route conflict prevention to ProfilePage

#### Phase 3: SEO & Polish
- [ ] Add canonical URLs in `<head>`
- [ ] Add Open Graph meta tags
- [ ] Add structured data (JSON-LD) for blog posts
- [ ] Implement RSS feed at `/blog/feed`
- [ ] Add sitemap generation for blog posts


---

## Blog Post Seeding Strategy (Updater.cs)

### Existing Patterns

The Updater.cs follows a consistent pattern for seeding demo data:

1. **JSON files in `DemoData/` folder** - Structured data with profiles and posts
2. **`SeedXxxAsync()` methods** - One per category (Restaurants, Entertainment, etc.)
3. **Pre-computed embeddings** - Generated via `generate_embeddings.py`
4. **`_pendingEmbeddings` dictionary** - Queued for raw SQL after commit

### New: DemoData/Blog Folder

Create `DemoData/Blog/blog.json`:

```json
{
  "metadata": {
    "category": "Blog",
    "description": "Migrated blog posts from jocheojeda.com",
    "profileTypeId": "11111111-1111-1111-1111-111111111111",
    "postType": 7
  },
  "profiles": [
    {
      "id": "b1000000-0000-0000-0000-000000000001",
      "displayName": "Jose Ojeda",
      "handle": "joche",
      "bio": "Software developer, DevExpress MVP",
      "categoryKeys": ["blog", "developer", "xaf"]
    }
  ],
  "posts": [
    {
      "id": "b2000000-0000-0000-0000-000000000001",
      "profileId": "b1000000-0000-0000-0000-000000000001",
      "postType": 7,
      "title": "Using XPO with ODBC Data Sources",
      "slug": "using-xpo-with-odbc-data-sources",
      "content": "Learn how to connect XPO to ODBC...",
      "blogContent": "<p>Full HTML content here...</p>",
      "coverImageUrl": "https://blob.sivar.os/blog/2025/12/xpo-odbc.png",
      "tags": ["xpo", "devexpress", "odbc"],
      "readTimeMinutes": 8,
      "publishedAt": "2025-12-23T10:00:00Z",
      "contentEmbedding": "[0.123,0.456,...]"
    }
  ]
}
```

### New: SeedBlogPostsAsync Method

Add to Updater.cs:

```csharp
/// <summary>
/// Seeds blog posts from DemoData/Blog/blog.json
/// Used for WordPress migration and demo blog content
/// </summary>
private async Task SeedBlogPostsAsync(string demoDataPath)
{
    var blogJsonPath = Path.Combine(demoDataPath, "Blog", "blog.json");
    if (!File.Exists(blogJsonPath))
    {
        System.Diagnostics.Debug.WriteLine($"[Updater] Blog JSON not found. Skipping.");
        return;
    }
    
    var jsonContent = await File.ReadAllTextAsync(blogJsonPath);
    var demoData = JsonSerializer.Deserialize<DemoDataFile>(jsonContent, _jsonOptions);
    
    foreach (var postData in demoData?.Posts ?? new())
    {
        var postId = Guid.Parse(postData.Id);
        var existingPost = ObjectSpace.FirstOrDefault<Post>(p => p.Id == postId);
        if (existingPost != null) continue;
        
        var post = ObjectSpace.CreateObject<Post>();
        post.Id = postId;
        post.ProfileId = Guid.Parse(postData.ProfileId);
        post.PostType = PostType.Blog;
        post.Content = postData.Title ?? "";  // Title in Content for blogs
        post.BlogContent = postData.BlogContent;
        post.Slug = postData.Slug;  // NEW: SEO slug
        post.CoverImageUrl = postData.CoverImageUrl;
        post.Tags = postData.Tags?.ToArray() ?? Array.Empty<string>();
        post.ReadTimeMinutes = postData.ReadTimeMinutes;
        post.PublishedAt = postData.PublishedAt;
        post.Visibility = VisibilityLevel.Public;
        post.IsDraft = false;
        
        // Queue embedding
        if (!string.IsNullOrEmpty(postData.ContentEmbedding))
            _pendingEmbeddings[post.Id] = postData.ContentEmbedding;
    }
    
    ObjectSpace.CommitChanges();
}
```

### DemoPostData DTO Update

Add to existing `DemoPostData` class:

```csharp
public string? Slug { get; set; }
public string? BlogContent { get; set; }
public string? CoverImageUrl { get; set; }
public int? ReadTimeMinutes { get; set; }
public DateTime? PublishedAt { get; set; }
```

### WordPress Migration Script

Create `Scripts/migrate_wordpress.py`:

```python
# Fetches posts from WordPress REST API
# Generates embeddings
# Outputs blog.json for seeding
```

### Seeding Checklist

- [ ] Create `DemoData/Blog/` folder
- [ ] Add `Slug` to `DemoPostData` DTO
- [ ] Add `BlogContent`, `CoverImageUrl`, `ReadTimeMinutes`, `PublishedAt` to DTO
- [ ] Add `SeedBlogPostsAsync()` method
- [ ] Call from `SeedDemoDataAsync()`
- [ ] Create WordPress migration script
- [ ] Generate embeddings for migrated posts
