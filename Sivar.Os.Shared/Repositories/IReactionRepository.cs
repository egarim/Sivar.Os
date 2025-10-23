

using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for Reaction entity operations
/// Provides specialized methods for reaction management and aggregation
/// </summary>
public interface IReactionRepository
{
    #region Basic CRUD Operations
    
    /// <summary>
    /// Gets a reaction by its ID with optional related entities
    /// </summary>
    /// <param name="reactionId">The reaction ID</param>
    /// <param name="includeProfile">Whether to include the reactor's profile</param>
    /// <param name="includePost">Whether to include the related post</param>
    /// <param name="includeComment">Whether to include the related comment (if applicable)</param>
    /// <returns>The reaction if found, null otherwise</returns>
    Task<Reaction?> GetByIdAsync(Guid reactionId, bool includeProfile = false, bool includePost = false, bool includeComment = false);
    
    /// <summary>
    /// Adds a new reaction
    /// </summary>
    /// <param name="reaction">The reaction to add</param>
    /// <returns>The added reaction</returns>
    Task<Reaction> AddAsync(Reaction reaction);
    
    /// <summary>
    /// Updates an existing reaction (typically changing reaction type)
    /// </summary>
    /// <param name="reaction">The reaction to update</param>
    /// <returns>The updated reaction</returns>
    Task<Reaction> UpdateAsync(Reaction reaction);
    
    /// <summary>
    /// Deletes a reaction
    /// </summary>
    /// <param name="reactionId">The reaction ID to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(Guid reactionId);
    
    #endregion
    
    #region Post Reaction Operations
    
    /// <summary>
    /// Gets all reactions for a specific post with pagination
    /// </summary>
    /// <param name="postId">The post ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of reactions per page</param>
    /// <param name="reactionType">Optional filter by reaction type</param>
    /// <param name="includeProfile">Whether to include reactor profiles</param>
    /// <returns>Tuple of reactions and total count</returns>
    Task<(IEnumerable<Reaction> Reactions, int TotalCount)> GetByPostAsync(
        Guid postId, 
        int page = 0, 
        int pageSize = 50, 
        ReactionType? reactionType = null, 
        bool includeProfile = false);
    
    /// <summary>
    /// Gets reaction counts grouped by type for a specific post
    /// </summary>
    /// <param name="postId">The post ID</param>
    /// <returns>Dictionary with reaction type as key and count as value</returns>
    Task<Dictionary<ReactionType, int>> GetReactionCountsByPostAsync(Guid postId);
    
    /// <summary>
    /// Gets the total reaction count for a post
    /// </summary>
    /// <param name="postId">The post ID</param>
    /// <returns>Total number of reactions</returns>
    Task<int> GetTotalReactionCountByPostAsync(Guid postId);
    
    /// <summary>
    /// Checks if a specific profile has reacted to a post
    /// </summary>
    /// <param name="postId">The post ID</param>
    /// <param name="profileId">The profile ID</param>
    /// <returns>The reaction if exists, null otherwise</returns>
    Task<Reaction?> GetUserReactionToPostAsync(Guid postId, Guid profileId);
    
    /// <summary>
    /// Gets posts ordered by reaction count within a date range
    /// </summary>
    /// <param name="startDate">Start date for filtering</param>
    /// <param name="endDate">End date for filtering</param>
    /// <param name="reactionType">Optional filter by reaction type</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of posts per page</param>
    /// <returns>Tuple of post IDs with reaction counts and total count</returns>
    Task<(IEnumerable<PostReactionSummary> Posts, int TotalCount)> GetMostReactedPostsAsync(
        DateTime startDate, 
        DateTime endDate, 
        ReactionType? reactionType = null, 
        int page = 0, 
        int pageSize = 20);
    
    #endregion
    
    #region Comment Reaction Operations
    
    /// <summary>
    /// Gets all reactions for a specific comment with pagination
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of reactions per page</param>
    /// <param name="reactionType">Optional filter by reaction type</param>
    /// <param name="includeProfile">Whether to include reactor profiles</param>
    /// <returns>Tuple of reactions and total count</returns>
    Task<(IEnumerable<Reaction> Reactions, int TotalCount)> GetByCommentAsync(
        Guid commentId, 
        int page = 0, 
        int pageSize = 50, 
        ReactionType? reactionType = null, 
        bool includeProfile = false);
    
    /// <summary>
    /// Gets reaction counts grouped by type for a specific comment
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <returns>Dictionary with reaction type as key and count as value</returns>
    Task<Dictionary<ReactionType, int>> GetReactionCountsByCommentAsync(Guid commentId);
    
    /// <summary>
    /// Gets the total reaction count for a comment
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <returns>Total number of reactions</returns>
    Task<int> GetTotalReactionCountByCommentAsync(Guid commentId);
    
    /// <summary>
    /// Checks if a specific profile has reacted to a comment
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="profileId">The profile ID</param>
    /// <returns>The reaction if exists, null otherwise</returns>
    Task<Reaction?> GetUserReactionToCommentAsync(Guid commentId, Guid profileId);
    
    #endregion
    
    #region Profile Reaction Operations
    
    /// <summary>
    /// Gets reactions made by a specific profile
    /// </summary>
    /// <param name="profileId">The profile ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of reactions per page</param>
    /// <param name="reactionType">Optional filter by reaction type</param>
    /// <param name="includePost">Whether to include related posts</param>
    /// <param name="includeComment">Whether to include related comments</param>
    /// <returns>Tuple of reactions and total count</returns>
    Task<(IEnumerable<Reaction> Reactions, int TotalCount)> GetByProfileAsync(
        Guid profileId, 
        int page = 0, 
        int pageSize = 50, 
        ReactionType? reactionType = null, 
        bool includePost = false, 
        bool includeComment = false);
    
    /// <summary>
    /// Gets recent reactions made by a profile
    /// </summary>
    /// <param name="profileId">The profile ID</param>
    /// <param name="sinceDate">Get reactions since this date</param>
    /// <param name="limit">Maximum number of reactions to return</param>
    /// <returns>Recent reactions by the profile</returns>
    Task<IEnumerable<Reaction>> GetRecentByProfileAsync(
        Guid profileId, 
        DateTime sinceDate, 
        int limit = 100);
    
    /// <summary>
    /// Gets reaction statistics for a profile
    /// </summary>
    /// <param name="profileId">The profile ID</param>
    /// <param name="sinceDate">Calculate statistics since this date</param>
    /// <returns>Profile reaction activity summary</returns>
    Task<ProfileReactionActivity> GetProfileReactionActivityAsync(Guid profileId, DateTime sinceDate);
    
    #endregion
    
    #region Batch Operations
    
    /// <summary>
    /// Adds or updates a reaction (toggles or changes reaction type)
    /// If user already reacted with same type, removes it
    /// If user reacted with different type, updates it
    /// If user hasn't reacted, adds new reaction
    /// </summary>
    /// <param name="profileId">The profile ID making the reaction</param>
    /// <param name="postId">The post ID (required if reacting to post)</param>
    /// <param name="commentId">The comment ID (required if reacting to comment)</param>
    /// <param name="reactionType">The type of reaction</param>
    /// <returns>Result indicating what action was taken</returns>
    Task<ReactionResult> ToggleReactionAsync(
        Guid profileId, 
        ReactionType reactionType, 
        Guid? postId = null, 
        Guid? commentId = null);
    
    /// <summary>
    /// Gets reactions for multiple posts in a single query
    /// </summary>
    /// <param name="postIds">The post IDs to get reactions for</param>
    /// <param name="profileId">Optional profile ID to get user's reactions only</param>
    /// <returns>Dictionary with post ID as key and reactions as value</returns>
    Task<Dictionary<Guid, IEnumerable<Reaction>>> GetReactionsByPostsAsync(
        IEnumerable<Guid> postIds, 
        Guid? profileId = null);
    
    /// <summary>
    /// Gets reaction counts for multiple posts in a single query
    /// </summary>
    /// <param name="postIds">The post IDs to get reaction counts for</param>
    /// <returns>Dictionary with post ID as key and reaction counts as value</returns>
    Task<Dictionary<Guid, Dictionary<ReactionType, int>>> GetReactionCountsByPostsAsync(
        IEnumerable<Guid> postIds);
    
    /// <summary>
    /// Gets reactions for multiple comments in a single query
    /// </summary>
    /// <param name="commentIds">The comment IDs to get reactions for</param>
    /// <param name="profileId">Optional profile ID to get user's reactions only</param>
    /// <returns>Dictionary with comment ID as key and reactions as value</returns>
    Task<Dictionary<Guid, IEnumerable<Reaction>>> GetReactionsByCommentsAsync(
        IEnumerable<Guid> commentIds, 
        Guid? profileId = null);
    
    #endregion
    
    #region Analytics and Statistics
    
    /// <summary>
    /// Gets trending reaction types within a time period
    /// </summary>
    /// <param name="startDate">Start date for analysis</param>
    /// <param name="endDate">End date for analysis</param>
    /// <returns>Reaction types ordered by frequency</returns>
    Task<IEnumerable<ReactionTypeCount>> GetTrendingReactionTypesAsync(
        DateTime startDate, 
        DateTime endDate);
    
    /// <summary>
    /// Gets reaction activity timeline for a specific post
    /// </summary>
    /// <param name="postId">The post ID</param>
    /// <param name="intervalHours">Group reactions by this time interval</param>
    /// <returns>Timeline of reaction activity</returns>
    Task<IEnumerable<ReactionTimelineItem>> GetReactionTimelineAsync(
        Guid postId, 
        int intervalHours = 1);
    
    /// <summary>
    /// Gets profiles that react most frequently to a specific profile's content
    /// </summary>
    /// <param name="profileId">The profile ID whose content is being analyzed</param>
    /// <param name="sinceDate">Analyze reactions since this date</param>
    /// <param name="limit">Maximum number of top reactors to return</param>
    /// <returns>Top reactors with reaction counts</returns>
    Task<IEnumerable<TopReactor>> GetTopReactorsToProfileAsync(
        Guid profileId, 
        DateTime sinceDate, 
        int limit = 10);
    
    /// <summary>
    /// Gets engagement rate statistics for a profile's content
    /// </summary>
    /// <param name="profileId">The profile ID</param>
    /// <param name="sinceDate">Calculate engagement since this date</param>
    /// <returns>Engagement statistics</returns>
    Task<EngagementStatistics> GetEngagementStatisticsAsync(Guid profileId, DateTime sinceDate);
    
    #endregion
    
    #region Utility Operations
    
    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    /// <returns>Number of entities affected</returns>
    Task<int> SaveChangesAsync();
    
    /// <summary>
    /// Checks if a reaction exists
    /// </summary>
    /// <param name="reactionId">The reaction ID</param>
    /// <returns>True if reaction exists</returns>
    Task<bool> ExistsAsync(Guid reactionId);
    
    /// <summary>
    /// Validates if a reaction is allowed (business rules)
    /// </summary>
    /// <param name="profileId">The profile ID making the reaction</param>
    /// <param name="postId">The post ID (if reacting to post)</param>
    /// <param name="commentId">The comment ID (if reacting to comment)</param>
    /// <param name="reactionType">The reaction type</param>
    /// <returns>Validation result with any error messages</returns>
    Task<ReactionValidationResult> ValidateReactionAsync(
        Guid profileId, 
        ReactionType reactionType, 
        Guid? postId = null, 
        Guid? commentId = null);
    
    #endregion
}

/// <summary>
/// Result of a toggle reaction operation
/// </summary>
public class ReactionResult
{
    public ReactionAction Action { get; set; }
    public Reaction? Reaction { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Action taken during reaction toggle
/// </summary>
public enum ReactionAction
{
    Added,
    Updated,
    Removed,
    NoChange
}

/// <summary>
/// Summary of post reaction data
/// </summary>
public class PostReactionSummary
{
    public Guid PostId { get; set; }
    public int TotalReactions { get; set; }
    public Dictionary<ReactionType, int> ReactionCounts { get; set; } = new();
    public DateTime LatestReactionDate { get; set; }
}

/// <summary>
/// Profile reaction activity statistics
/// </summary>
public class ProfileReactionActivity
{
    public int TotalReactionsGiven { get; set; }
    public int TotalReactionsReceived { get; set; }
    public Dictionary<ReactionType, int> ReactionsGivenByType { get; set; } = new();
    public Dictionary<ReactionType, int> ReactionsReceivedByType { get; set; } = new();
    public int PostsReactedTo { get; set; }
    public int CommentsReactedTo { get; set; }
    public double AverageReactionsPerDay { get; set; }
    public DateTime? LastReactionDate { get; set; }
    public DateTime? FirstReactionDate { get; set; }
}

/// <summary>
/// Reaction type with count for trending analysis
/// </summary>
public class ReactionTypeCount
{
    public ReactionType ReactionType { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Timeline item for reaction activity
/// </summary>
public class ReactionTimelineItem
{
    public DateTime TimeSlot { get; set; }
    public int ReactionCount { get; set; }
    public Dictionary<ReactionType, int> ReactionsByType { get; set; } = new();
}

/// <summary>
/// Top reactor information
/// </summary>
public class TopReactor
{
    public Guid ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public int ReactionCount { get; set; }
    public Dictionary<ReactionType, int> ReactionsByType { get; set; } = new();
    public DateTime LastReactionDate { get; set; }
}

/// <summary>
/// Engagement statistics for a profile
/// </summary>
public class EngagementStatistics
{
    public int TotalPosts { get; set; }
    public int TotalComments { get; set; }
    public int TotalReactions { get; set; }
    public double ReactionsPerPost { get; set; }
    public double ReactionsPerComment { get; set; }
    public double EngagementRate { get; set; }
    public ReactionType MostUsedReactionType { get; set; }
    public int UniqueReactors { get; set; }
}

/// <summary>
/// Validation result for reaction operations
/// </summary>
public class ReactionValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}