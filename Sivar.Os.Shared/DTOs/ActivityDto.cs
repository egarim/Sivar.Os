using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for Activity in the activity stream
/// Represents "Actor performed Verb on Object"
/// </summary>
public class ActivityDto
{
    /// <summary>
    /// Activity ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The profile who performed the activity
    /// </summary>
    public Guid ActorId { get; set; }
    public ProfileDto? Actor { get; set; }

    /// <summary>
    /// Type of action performed
    /// </summary>
    public ActivityVerb Verb { get; set; }

    /// <summary>
    /// Type of the target object (Post, Comment, Profile, etc.)
    /// </summary>
    public string ObjectType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the target object
    /// </summary>
    public Guid ObjectId { get; set; }

    /// <summary>
    /// Optional secondary target type
    /// </summary>
    public string? TargetType { get; set; }

    /// <summary>
    /// Optional secondary target ID
    /// </summary>
    public Guid? TargetId { get; set; }

    /// <summary>
    /// Human-readable summary
    /// Example: "John created a new post", "Jane liked your comment"
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string Metadata { get; set; } = "{}";

    /// <summary>
    /// Visibility level
    /// </summary>
    public VisibilityLevel Visibility { get; set; }

    /// <summary>
    /// When the activity was published
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// Engagement score for ranking
    /// </summary>
    public int EngagementScore { get; set; }

    /// <summary>
    /// Number of views
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Related object data (e.g., the Post if ObjectType is "Post")
    /// This is populated for display purposes
    /// </summary>
    public object? RelatedObject { get; set; }

    /// <summary>
    /// For Post activities, the actual post data
    /// </summary>
    public PostDto? Post { get; set; }

    /// <summary>
    /// For Comment activities, the actual comment data
    /// </summary>
    public CommentDto? Comment { get; set; }

    /// <summary>
    /// For Profile activities (follow), the target profile
    /// </summary>
    public ProfileDto? TargetProfile { get; set; }

    /// <summary>
    /// Time ago display helper
    /// </summary>
    public string TimeAgo => GetTimeAgo(PublishedAt);

    private static string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalSeconds < 60)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)}w ago";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)}mo ago";
        
        return $"{(int)(timeSpan.TotalDays / 365)}y ago";
    }
}
