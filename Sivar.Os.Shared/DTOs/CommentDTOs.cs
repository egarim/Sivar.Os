
namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for creating a new comment
/// </summary>
public record CreateCommentDto
{
    /// <summary>
    /// ID of the post being commented on
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// Content of the comment
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Language code (e.g., "en", "es")
    /// </summary>
    public string Language { get; init; } = "en";
}

/// <summary>
/// DTO for creating a reply to a comment
/// </summary>
public record CreateReplyDto
{
    /// <summary>
    /// Content of the reply
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Language code (e.g., "en", "es")
    /// </summary>
    public string Language { get; init; } = "en";
}

/// <summary>
/// DTO for updating an existing comment
/// </summary>
public record UpdateCommentDto
{
    /// <summary>
    /// Updated content of the comment
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Updated language code
    /// </summary>
    public string? Language { get; init; }
}

/// <summary>
/// DTO for comment representation in API responses
/// </summary>
public record CommentDto
{
    /// <summary>
    /// Comment unique identifier
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Profile that created the comment
    /// </summary>
    public ProfileDto Profile { get; init; } = null!;

    /// <summary>
    /// Post that the comment belongs to
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// Parent comment ID if this is a reply
    /// </summary>
    public Guid? ParentCommentId { get; init; }

    /// <summary>
    /// Content of the comment
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Language code
    /// </summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Nested replies to this comment
    /// </summary>
    public List<CommentDto> Replies { get; init; } = new();

    /// <summary>
    /// Number of replies
    /// </summary>
    public int ReplyCount { get; init; }

    /// <summary>
    /// Reaction counts and user's reaction
    /// </summary>
    public CommentReactionSummaryDto? ReactionSummary { get; init; }

    /// <summary>
    /// When the comment was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the comment was last updated
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Whether the comment has been edited
    /// </summary>
    public bool IsEdited { get; init; }

    /// <summary>
    /// When the comment was edited (if applicable)
    /// </summary>
    public DateTime? EditedAt { get; init; }

    /// <summary>
    /// Depth in the comment thread (0 for top-level comments)
    /// </summary>
    public int ThreadDepth { get; init; }
}

/// <summary>
/// DTO for comment thread statistics
/// </summary>
public record CommentThreadStatsDto
{
    /// <summary>
    /// Comment ID
    /// </summary>
    public Guid CommentId { get; init; }

    /// <summary>
    /// Maximum depth of replies in this thread
    /// </summary>
    public int MaxDepth { get; init; }

    /// <summary>
    /// Total number of replies in this thread
    /// </summary>
    public int TotalReplies { get; init; }

    /// <summary>
    /// Number of direct replies
    /// </summary>
    public int DirectReplies { get; init; }

    /// <summary>
    /// Most recent activity in the thread
    /// </summary>
    public DateTime LastActivityAt { get; init; }
}

/// <summary>
/// DTO for comment activity notifications
/// </summary>
public record CommentActivityDto
{
    /// <summary>
    /// Comment ID
    /// </summary>
    public Guid CommentId { get; init; }

    /// <summary>
    /// Post ID that the comment belongs to
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// Profile that made the comment
    /// </summary>
    public ProfileDto Commenter { get; init; } = null!;

    /// <summary>
    /// Type of activity (new comment, reply, edit)
    /// </summary>
    public CommentActivityType ActivityType { get; init; }

    /// <summary>
    /// Comment content (truncated for notifications)
    /// </summary>
    public string ContentPreview { get; init; } = string.Empty;

    /// <summary>
    /// When the activity occurred
    /// </summary>
    public DateTime ActivityAt { get; init; }

    /// <summary>
    /// Whether this is a reply to another comment
    /// </summary>
    public bool IsReply { get; init; }

    /// <summary>
    /// Parent comment ID if this is a reply
    /// </summary>
    public Guid? ParentCommentId { get; init; }
}

/// <summary>
/// Types of comment activities for notifications
/// </summary>
public enum CommentActivityType
{
    /// <summary>
    /// New comment created
    /// </summary>
    NewComment,

    /// <summary>
    /// Reply to a comment
    /// </summary>
    Reply,

    /// <summary>
    /// Comment edited
    /// </summary>
    Edit,

    /// <summary>
    /// Comment deleted
    /// </summary>
    Delete
}

/// <summary>
/// DTO for paginated comment threads
/// </summary>
public record CommentThreadDto
{
    /// <summary>
    /// List of comments in the thread
    /// </summary>
    public List<CommentDto> Comments { get; init; } = new();

    /// <summary>
    /// Current page number (0-based)
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of comments per page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of comments
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

    /// <summary>
    /// Root comment ID if this is a thread view
    /// </summary>
    public Guid? RootCommentId { get; init; }

    /// <summary>
    /// Maximum depth of nested comments shown
    /// </summary>
    public int MaxDepth { get; init; }
}