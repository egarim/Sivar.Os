

using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for reaction result after toggle operation
/// </summary>
public record ReactionResultDto
{
    /// <summary>
    /// Action that was performed
    /// </summary>
    public ReactionAction Action { get; init; }

    /// <summary>
    /// Type of reaction
    /// </summary>
    public ReactionType ReactionType { get; init; }

    /// <summary>
    /// Reaction details if added or updated
    /// </summary>
    public ReactionDto? Reaction { get; init; }

    /// <summary>
    /// Updated reaction counts for the target
    /// </summary>
    public Dictionary<ReactionType, int> UpdatedCounts { get; init; } = new();
}

/// <summary>
/// DTO for reaction representation
/// </summary>
public record ReactionDto
{
    /// <summary>
    /// Reaction unique identifier
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Profile that made the reaction
    /// </summary>
    public ProfileDto Profile { get; init; } = null!;

    /// <summary>
    /// Post ID if reaction is on a post
    /// </summary>
    public Guid? PostId { get; init; }

    /// <summary>
    /// Comment ID if reaction is on a comment
    /// </summary>
    public Guid? CommentId { get; init; }

    /// <summary>
    /// Type of reaction
    /// </summary>
    public ReactionType ReactionType { get; init; }

    /// <summary>
    /// When the reaction was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the reaction was last updated
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// DTO for reaction with profile information (for listing who reacted)
/// </summary>
public record ReactionWithProfileDto
{
    /// <summary>
    /// Reaction unique identifier
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Profile that made the reaction
    /// </summary>
    public ProfileDto Profile { get; init; } = null!;

    /// <summary>
    /// Type of reaction
    /// </summary>
    public ReactionType ReactionType { get; init; }

    /// <summary>
    /// When the reaction was created
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for post reaction summary
/// </summary>
public record PostReactionSummaryDto
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
    /// Reaction counts by type
    /// </summary>
    public Dictionary<ReactionType, int> ReactionCounts { get; init; } = new();

    /// <summary>
    /// Current user's reaction (if any)
    /// </summary>
    public ReactionType? UserReaction { get; init; }

    /// <summary>
    /// Most popular reaction type
    /// </summary>
    public ReactionType? TopReactionType { get; init; }

    /// <summary>
    /// Whether the requesting user has reacted
    /// </summary>
    public bool HasUserReacted { get; init; }
}

/// <summary>
/// DTO for comment reaction summary
/// </summary>
public record CommentReactionSummaryDto
{
    /// <summary>
    /// Comment ID
    /// </summary>
    public Guid CommentId { get; init; }

    /// <summary>
    /// Total number of reactions
    /// </summary>
    public int TotalReactions { get; init; }

    /// <summary>
    /// Reaction counts by type
    /// </summary>
    public Dictionary<ReactionType, int> ReactionCounts { get; init; } = new();

    /// <summary>
    /// Current user's reaction (if any)
    /// </summary>
    public ReactionType? UserReaction { get; init; }

    /// <summary>
    /// Most popular reaction type
    /// </summary>
    public ReactionType? TopReactionType { get; init; }

    /// <summary>
    /// Whether the requesting user has reacted
    /// </summary>
    public bool HasUserReacted { get; init; }
}

/// <summary>
/// DTO for detailed post reaction analytics
/// </summary>
public record PostReactionAnalyticsDto
{
    /// <summary>
    /// Post ID
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// Total reactions over time
    /// </summary>
    public int TotalReactions { get; init; }

    /// <summary>
    /// Detailed reaction counts by type
    /// </summary>
    public Dictionary<ReactionType, int> ReactionsByType { get; init; } = new();

    /// <summary>
    /// Reaction trends over time (hourly buckets for last 24h)
    /// </summary>
    public List<ReactionTrendDto> ReactionTrends { get; init; } = new();

    /// <summary>
    /// Peak reaction hour
    /// </summary>
    public int PeakReactionHour { get; init; }

    /// <summary>
    /// Average reactions per hour
    /// </summary>
    public double AvgReactionsPerHour { get; init; }

    /// <summary>
    /// Most active reaction profiles
    /// </summary>
    public List<ProfileDto> TopReactors { get; init; } = new();

    /// <summary>
    /// First reaction timestamp
    /// </summary>
    public DateTime? FirstReactionAt { get; init; }

    /// <summary>
    /// Most recent reaction timestamp
    /// </summary>
    public DateTime? LastReactionAt { get; init; }
}

/// <summary>
/// DTO for reaction trend over time
/// </summary>
public record ReactionTrendDto
{
    /// <summary>
    /// Time bucket (hour)
    /// </summary>
    public DateTime Hour { get; init; }

    /// <summary>
    /// Reaction counts by type for this hour
    /// </summary>
    public Dictionary<ReactionType, int> ReactionCounts { get; init; } = new();

    /// <summary>
    /// Total reactions in this hour
    /// </summary>
    public int TotalCount { get; init; }
}

/// <summary>
/// DTO for profile reaction engagement statistics
/// </summary>
public record ProfileReactionEngagementDto
{
    /// <summary>
    /// Profile ID
    /// </summary>
    public Guid ProfileId { get; init; }

    /// <summary>
    /// Analysis period in days
    /// </summary>
    public int AnalysisDays { get; init; }

    /// <summary>
    /// Total reactions received on profile's content
    /// </summary>
    public int TotalReactionsReceived { get; init; }

    /// <summary>
    /// Total reactions given by this profile
    /// </summary>
    public int TotalReactionsGiven { get; init; }

    /// <summary>
    /// Reactions received by type
    /// </summary>
    public Dictionary<ReactionType, int> ReactionsReceivedByType { get; init; } = new();

    /// <summary>
    /// Reactions given by type
    /// </summary>
    public Dictionary<ReactionType, int> ReactionsGivenByType { get; init; } = new();

    /// <summary>
    /// Average reactions per post
    /// </summary>
    public double AvgReactionsPerPost { get; init; }

    /// <summary>
    /// Most engaging post IDs
    /// </summary>
    public List<Guid> TopEngagingPosts { get; init; } = new();

    /// <summary>
    /// Engagement rate (reactions/posts ratio)
    /// </summary>
    public double EngagementRate { get; init; }

    /// <summary>
    /// Most popular reaction type received
    /// </summary>
    public ReactionType? MostReceivedReactionType { get; init; }

    /// <summary>
    /// Most given reaction type
    /// </summary>
    public ReactionType? MostGivenReactionType { get; init; }
}

/// <summary>
/// DTO for reaction activity notifications
/// </summary>
public record ReactionActivityDto
{
    /// <summary>
    /// Reaction ID
    /// </summary>
    public Guid ReactionId { get; init; }

    /// <summary>
    /// Post ID if reaction is on a post
    /// </summary>
    public Guid? PostId { get; init; }

    /// <summary>
    /// Comment ID if reaction is on a comment
    /// </summary>
    public Guid? CommentId { get; init; }

    /// <summary>
    /// Profile that made the reaction
    /// </summary>
    public ProfileDto Reactor { get; init; } = null!;

    /// <summary>
    /// Type of reaction
    /// </summary>
    public ReactionType ReactionType { get; init; }

    /// <summary>
    /// Type of activity (new, updated, removed)
    /// </summary>
    public ReactionActivityType ActivityType { get; init; }

    /// <summary>
    /// When the activity occurred
    /// </summary>
    public DateTime ActivityAt { get; init; }

    /// <summary>
    /// Target content preview (post/comment content snippet)
    /// </summary>
    public string TargetContentPreview { get; init; } = string.Empty;
}

/// <summary>
/// Types of reaction activities for notifications
/// </summary>
public enum ReactionActivityType
{
    /// <summary>
    /// New reaction added
    /// </summary>
    Added,

    /// <summary>
    /// Existing reaction updated (different type)
    /// </summary>
    Updated,

    /// <summary>
    /// Reaction removed
    /// </summary>
    Removed
}

/// <summary>
/// DTO for creating a reaction on a post
/// </summary>
public record CreatePostReactionDto
{
    /// <summary>
    /// The post to react to
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// Type of reaction
    /// </summary>
    public ReactionType ReactionType { get; init; }
}

/// <summary>
/// DTO for creating a reaction on a comment
/// </summary>
public record CreateCommentReactionDto
{
    /// <summary>
    /// The comment to react to
    /// </summary>
    public Guid CommentId { get; init; }

    /// <summary>
    /// Type of reaction
    /// </summary>
    public ReactionType ReactionType { get; init; }
}

/// <summary>
/// DTO for paginated reaction lists
/// </summary>
public record ReactionListDto
{
    /// <summary>
    /// List of reactions
    /// </summary>
    public List<ReactionWithProfileDto> Reactions { get; init; } = new();

    /// <summary>
    /// Current page number (0-based)
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of reactions per page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of reactions
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
/// Alias for ProfileReactionEngagementDto to match the controller usage
/// </summary>
public record ProfileEngagementDto
{
    /// <summary>
    /// Profile ID
    /// </summary>
    public Guid ProfileId { get; init; }

    /// <summary>
    /// Number of days analyzed
    /// </summary>
    public int AnalysisDays { get; init; }

    /// <summary>
    /// Total reactions received on profile's content
    /// </summary>
    public int TotalReactionsReceived { get; init; }

    /// <summary>
    /// Total reactions given by profile
    /// </summary>
    public int TotalReactionsGiven { get; init; }

    /// <summary>
    /// Breakdown by reaction type for reactions received
    /// </summary>
    public Dictionary<ReactionType, int> ReactionsReceivedByType { get; init; } = new();

    /// <summary>
    /// Breakdown by reaction type for reactions given
    /// </summary>
    public Dictionary<ReactionType, int> ReactionsGivenByType { get; init; } = new();

    /// <summary>
    /// Average reactions per post
    /// </summary>
    public double AvgReactionsPerPost { get; init; }

    /// <summary>
    /// Posts with highest engagement
    /// </summary>
    public List<Guid> TopEngagingPosts { get; init; } = new();

    /// <summary>
    /// Overall engagement rate
    /// </summary>
    public double EngagementRate { get; init; }

    /// <summary>
    /// Most received reaction type
    /// </summary>
    public ReactionType MostReceivedReactionType { get; init; }

    /// <summary>
    /// Most given reaction type
    /// </summary>
    public ReactionType MostGivenReactionType { get; init; }
}