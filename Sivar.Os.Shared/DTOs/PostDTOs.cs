using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for creating a new post
/// </summary>
public record CreatePostDto
{
    /// <summary>
    /// ID of the profile creating the post (sent from client)
    /// </summary>
    public Guid ProfileId { get; init; }

    /// <summary>
    /// Content of the post
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Type of the post
    /// </summary>
    public PostType PostType { get; init; }

    /// <summary>
    /// Visibility level of the post
    /// </summary>
    public VisibilityLevel Visibility { get; init; }

    /// <summary>
    /// Language code (e.g., "en", "es")
    /// </summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Tags associated with the post
    /// </summary>
    public List<string> Tags { get; init; } = new();

    /// <summary>
    /// Location information for the post
    /// </summary>
    public LocationDto? Location { get; init; }

    /// <summary>
    /// Business-specific metadata for business profiles
    /// </summary>
    public string? BusinessMetadata { get; init; }

    /// <summary>
    /// Procedure-specific metadata (JSON) for procedure/how-to posts
    /// Contains steps, required documents, processing time, costs
    /// </summary>
    public string? ProcedureMetadataJson { get; init; }

    /// <summary>
    /// Attachment information for images, videos, files
    /// </summary>
    public List<CreatePostAttachmentDto> Attachments { get; init; } = new();

    // ==================== BLOG-SPECIFIC FIELDS ====================

    /// <summary>
    /// Full blog content (Markdown/HTML) - only used when PostType = Blog
    /// </summary>
    public string? BlogContent { get; init; }

    /// <summary>
    /// Cover/featured image URL for blog posts
    /// </summary>
    public string? CoverImageUrl { get; init; }

    /// <summary>
    /// Cover image file ID from blob storage
    /// </summary>
    public string? CoverImageFileId { get; init; }

    /// <summary>
    /// Blog subtitle or excerpt
    /// </summary>
    public string? Subtitle { get; init; }

    /// <summary>
    /// Whether this is a draft (not yet published)
    /// </summary>
    public bool IsDraft { get; init; } = false;

    /// <summary>
    /// Canonical URL if republished from another source
    /// </summary>
    public string? CanonicalUrl { get; init; }
}

/// <summary>
/// DTO for updating an existing post
/// </summary>
public record UpdatePostDto
{
    /// <summary>
    /// Updated content of the post
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Updated visibility level
    /// </summary>
    public VisibilityLevel? Visibility { get; init; }

    /// <summary>
    /// Updated tags
    /// </summary>
    public List<string>? Tags { get; init; }

    /// <summary>
    /// Updated location information
    /// </summary>
    public LocationDto? Location { get; init; }

    /// <summary>
    /// Updated business metadata
    /// </summary>
    public string? BusinessMetadata { get; init; }

    /// <summary>
    /// Updated procedure metadata (JSON)
    /// </summary>
    public string? ProcedureMetadataJson { get; init; }

    // ==================== BLOG-SPECIFIC FIELDS ====================

    /// <summary>
    /// Updated blog content (Markdown/HTML)
    /// </summary>
    public string? BlogContent { get; init; }

    /// <summary>
    /// Updated cover image URL
    /// </summary>
    public string? CoverImageUrl { get; init; }

    /// <summary>
    /// Updated cover image file ID
    /// </summary>
    public string? CoverImageFileId { get; init; }

    /// <summary>
    /// Updated subtitle
    /// </summary>
    public string? Subtitle { get; init; }

    /// <summary>
    /// Updated canonical URL
    /// </summary>
    public string? CanonicalUrl { get; init; }
}

/// <summary>
/// DTO for post representation in API responses
/// </summary>
public record PostDto
{
    /// <summary>
    /// Post unique identifier
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Profile that created the post
    /// </summary>
    public ProfileDto Profile { get; init; } = null!;

    /// <summary>
    /// Content of the post
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Type of the post
    /// </summary>
    public PostType PostType { get; init; }

    /// <summary>
    /// Visibility level of the post
    /// </summary>
    public VisibilityLevel Visibility { get; init; }

    /// <summary>
    /// Language code
    /// </summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Vector embedding of the content (for internal use in semantic search)
    /// </summary>
    public float[]? ContentEmbedding { get; init; }

    /// <summary>
    /// Tags associated with the post
    /// </summary>
    public List<string> Tags { get; init; } = new();

    /// <summary>
    /// Location information
    /// </summary>
    public LocationDto? Location { get; init; }

    /// <summary>
    /// Distance in kilometers from search point (for proximity search results)
    /// </summary>
    public double? DistanceKm { get; init; }

    /// <summary>
    /// Business-specific metadata
    /// </summary>
    public string? BusinessMetadata { get; init; }

    /// <summary>
    /// Procedure-specific metadata (JSON)
    /// For Procedure posts: steps, required documents, processing time, costs
    /// </summary>
    public string? ProcedureMetadataJson { get; init; }

    /// <summary>
    /// Post attachments
    /// </summary>
    public List<PostAttachmentDto> Attachments { get; init; } = new();

    /// <summary>
    /// Reaction counts and user's reaction
    /// </summary>
    public PostReactionSummaryDto? ReactionSummary { get; init; }

    /// <summary>
    /// Comments on the post
    /// </summary>
    public List<CommentDto> Comments { get; init; } = new();

    /// <summary>
    /// Number of comments
    /// Note: Using set instead of init to allow updating stale values from PostSnapshotJson
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// When the post was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the post was last updated
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Whether the post has been edited
    /// </summary>
    public bool IsEdited { get; init; }

    /// <summary>
    /// When the post was edited (if applicable)
    /// </summary>
    public DateTime? EditedAt { get; init; }

    // ==================== BLOG-SPECIFIC FIELDS ====================

    /// <summary>
    /// Full blog content (Markdown/HTML) - only for Blog post type
    /// </summary>
    public string? BlogContent { get; init; }

    /// <summary>
    /// Cover/featured image URL for blog posts
    /// </summary>
    public string? CoverImageUrl { get; init; }

    /// <summary>
    /// Cover image file ID from blob storage
    /// </summary>
    public string? CoverImageFileId { get; init; }

    /// <summary>
    /// Blog subtitle or excerpt
    /// </summary>
    public string? Subtitle { get; init; }

    /// <summary>
    /// Estimated read time in minutes
    /// </summary>
    public int? ReadTimeMinutes { get; init; }

    /// <summary>
    /// Whether this is a draft (not yet published)
    /// </summary>
    public bool IsDraft { get; init; }

    /// <summary>
    /// When the blog was published
    /// </summary>
    public DateTime? PublishedAt { get; init; }

    /// <summary>
    /// Canonical URL if republished from another source
    /// </summary>
    public string? CanonicalUrl { get; init; }
}

/// <summary>
/// DTO for post engagement statistics
/// </summary>
public record PostEngagementDto
{
    /// <summary>
    /// Post ID
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// Total number of reactions
    /// </summary>
    public int TotalReactions { get; init; }

    /// <summary>
    /// Reactions by type
    /// </summary>
    public Dictionary<ReactionType, int> ReactionsByType { get; init; } = new();

    /// <summary>
    /// Total number of comments
    /// </summary>
    public int TotalComments { get; init; }

    /// <summary>
    /// Number of shares (if implemented)
    /// </summary>
    public int TotalShares { get; init; }

    /// <summary>
    /// Engagement rate calculation
    /// </summary>
    public double EngagementRate { get; init; }

    /// <summary>
    /// Top reaction type
    /// </summary>
    public ReactionType? TopReactionType { get; init; }
}

/// <summary>
/// DTO for creating post attachments
/// </summary>
public record CreatePostAttachmentDto
{
    /// <summary>
    /// Type of attachment
    /// </summary>
    public AttachmentType AttachmentType { get; init; }

    /// <summary>
    /// File ID from file storage service (for uploaded files)
    /// </summary>
    public string? FileId { get; init; }

    /// <summary>
    /// File path or URL
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Original filename
    /// </summary>
    public string OriginalFilename { get; init; } = string.Empty;

    /// <summary>
    /// MIME type
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// Alt text for accessibility
    /// </summary>
    public string? AltText { get; init; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; init; }
}

/// <summary>
/// DTO for post attachment representation
/// </summary>
public record PostAttachmentDto
{
    /// <summary>
    /// Attachment unique identifier
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Type of attachment
    /// </summary>
    public AttachmentType AttachmentType { get; init; }

    /// <summary>
    /// File ID from file storage service (for uploaded files)
    /// </summary>
    public string? FileId { get; init; }

    /// <summary>
    /// File path or URL
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Original filename
    /// </summary>
    public string OriginalFilename { get; init; } = string.Empty;

    /// <summary>
    /// MIME type
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// Alt text for accessibility
    /// </summary>
    public string? AltText { get; init; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; init; }

    /// <summary>
    /// When the attachment was created
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for location information
/// </summary>
public record LocationDto
{
    /// <summary>
    /// City name
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// State or province
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Country name
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// Latitude coordinate
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    public double? Longitude { get; init; }
}

/// <summary>
/// DTO for paginated post feeds
/// </summary>
public record PostFeedDto
{
    /// <summary>
    /// List of posts in the feed
    /// </summary>
    public List<PostDto> Posts { get; init; } = new();

    /// <summary>
    /// Current page number (0-based)
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of posts per page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of posts
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there are more pages
    /// </summary>
    public bool HasMorePages => Page < TotalPages - 1;
}

/// <summary>
/// DTO for post analytics data
/// </summary>
public record PostAnalyticsDto
{
    /// <summary>
    /// Post ID
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// Total views
    /// </summary>
    public int Views { get; init; }

    /// <summary>
    /// Total reactions
    /// </summary>
    public int TotalReactions { get; init; }

    /// <summary>
    /// Reaction breakdown by type
    /// </summary>
    public Dictionary<ReactionType, int> ReactionsByType { get; init; } = new();

    /// <summary>
    /// Total comments
    /// </summary>
    public int TotalComments { get; init; }

    /// <summary>
    /// Engagement rate
    /// </summary>
    public double EngagementRate { get; init; }

    /// <summary>
    /// Peak engagement hour
    /// </summary>
    public int PeakEngagementHour { get; init; }

    /// <summary>
    /// Geographic breakdown of engagement
    /// </summary>
    public Dictionary<string, int> EngagementByLocation { get; init; } = new();
}

/// <summary>
/// DTO for post activity tracking
/// </summary>
public record PostActivityDto
{
    /// <summary>
    /// Activity ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Post involved in the activity
    /// </summary>
    public PostDto Post { get; init; } = null!;

    /// <summary>
    /// Activity type
    /// </summary>
    public string ActivityType { get; init; } = string.Empty;

    /// <summary>
    /// Activity description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// When the activity occurred
    /// </summary>
    public DateTime ActivityTime { get; init; }

    /// <summary>
    /// Related profile for the activity
    /// </summary>
    public ProfileDto? RelatedProfile { get; init; }
}