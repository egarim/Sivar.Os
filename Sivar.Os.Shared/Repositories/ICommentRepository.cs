
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for Comment entity operations
/// Provides specialized methods for hierarchical comment management
/// </summary>
public interface ICommentRepository
{
    #region Basic CRUD Operations
    
    /// <summary>
    /// Gets a comment by its ID with optional related entities
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="includeReplies">Whether to include direct replies</param>
    /// <param name="includeProfile">Whether to include the commenter's profile</param>
    /// <returns>The comment if found, null otherwise</returns>
    Task<Comment?> GetByIdAsync(Guid commentId, bool includeReplies = false, bool includeProfile = false);
    
    /// <summary>
    /// Adds a new comment
    /// </summary>
    /// <param name="comment">The comment to add</param>
    /// <returns>The added comment</returns>
    Task<Comment> AddAsync(Comment comment);
    
    /// <summary>
    /// Updates an existing comment
    /// </summary>
    /// <param name="comment">The comment to update</param>
    /// <returns>The updated comment</returns>
    Task<Comment> UpdateAsync(Comment comment);
    
    /// <summary>
    /// Soft deletes a comment by marking it as deleted
    /// </summary>
    /// <param name="commentId">The comment ID to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(Guid commentId);
    
    /// <summary>
    /// Hard deletes a comment and all its replies
    /// </summary>
    /// <param name="commentId">The comment ID to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> HardDeleteAsync(Guid commentId);
    
    #endregion
    
    #region Post Comment Operations
    
    /// <summary>
    /// Gets all comments for a specific post with pagination
    /// </summary>
    /// <param name="postId">The post ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of comments per page</param>
    /// <param name="includeReplies">Whether to include direct replies</param>
    /// <param name="includeProfile">Whether to include commenter profiles</param>
    /// <returns>Tuple of comments and total count</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetByPostAsync(
        Guid postId, 
        int page = 0, 
        int pageSize = 20, 
        bool includeReplies = false, 
        bool includeProfile = false);
    
    /// <summary>
    /// Gets top-level comments for a post (no parent comment)
    /// </summary>
    /// <param name="postId">The post ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of comments per page</param>
    /// <param name="includeProfile">Whether to include commenter profiles</param>
    /// <returns>Tuple of comments and total count</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetTopLevelByPostAsync(
        Guid postId, 
        int page = 0, 
        int pageSize = 20, 
        bool includeProfile = false);
    
    /// <summary>
    /// Gets the total number of comments for a post (including replies)
    /// </summary>
    /// <param name="postId">The post ID</param>
    /// <param name="includeDeleted">Whether to include soft-deleted comments</param>
    /// <returns>Total comment count</returns>
    Task<int> GetCommentCountByPostAsync(Guid postId, bool includeDeleted = false);

    /// <summary>
    /// Gets comment counts for multiple posts in a single query (batch operation)
    /// </summary>
    /// <param name="postIds">List of post IDs</param>
    /// <param name="includeDeleted">Whether to include soft-deleted comments</param>
    /// <returns>Dictionary mapping post ID to comment count</returns>
    Task<Dictionary<Guid, int>> GetCommentCountsByPostIdsAsync(IEnumerable<Guid> postIds, bool includeDeleted = false);
    
    #endregion
    
    #region Reply Operations
    
    /// <summary>
    /// Gets direct replies to a specific comment
    /// </summary>
    /// <param name="parentCommentId">The parent comment ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of replies per page</param>
    /// <param name="includeProfile">Whether to include commenter profiles</param>
    /// <returns>Tuple of replies and total count</returns>
    Task<(IEnumerable<Comment> Replies, int TotalCount)> GetRepliesAsync(
        Guid parentCommentId, 
        int page = 0, 
        int pageSize = 10, 
        bool includeProfile = false);
    
    /// <summary>
    /// Gets all replies in a comment thread (recursive)
    /// </summary>
    /// <param name="parentCommentId">The parent comment ID</param>
    /// <param name="maxDepth">Maximum depth to traverse (0 = no limit)</param>
    /// <param name="includeProfile">Whether to include commenter profiles</param>
    /// <returns>All replies in the thread</returns>
    Task<IEnumerable<Comment>> GetCommentThreadAsync(
        Guid parentCommentId, 
        int maxDepth = 0, 
        bool includeProfile = false);
    
    /// <summary>
    /// Gets the reply count for a specific comment
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="includeDeleted">Whether to include soft-deleted replies</param>
    /// <returns>Number of direct replies</returns>
    Task<int> GetReplyCountAsync(Guid commentId, bool includeDeleted = false);
    
    /// <summary>
    /// Gets the total descendant count for a comment (all nested replies)
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="includeDeleted">Whether to include soft-deleted comments</param>
    /// <returns>Total number of descendants</returns>
    Task<int> GetDescendantCountAsync(Guid commentId, bool includeDeleted = false);
    
    #endregion
    
    #region Profile Comment Operations
    
    /// <summary>
    /// Gets comments made by a specific profile
    /// </summary>
    /// <param name="profileId">The profile ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of comments per page</param>
    /// <param name="includeReplies">Whether to include the user's replies</param>
    /// <returns>Tuple of comments and total count</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetByProfileAsync(
        Guid profileId, 
        int page = 0, 
        int pageSize = 20, 
        bool includeReplies = true);
    
    /// <summary>
    /// Gets recent comments made by a profile
    /// </summary>
    /// <param name="profileId">The profile ID</param>
    /// <param name="sinceDate">Get comments since this date</param>
    /// <param name="limit">Maximum number of comments to return</param>
    /// <returns>Recent comments by the profile</returns>
    Task<IEnumerable<Comment>> GetRecentByProfileAsync(
        Guid profileId, 
        DateTime sinceDate, 
        int limit = 50);
    
    #endregion
    
    #region Search and Filter Operations
    
    /// <summary>
    /// Searches comments by content with optional filters
    /// </summary>
    /// <param name="searchTerm">The term to search for</param>
    /// <param name="postId">Optional post ID to filter by</param>
    /// <param name="profileId">Optional profile ID to filter by</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of results per page</param>
    /// <returns>Tuple of matching comments and total count</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> SearchCommentsAsync(
        string searchTerm, 
        Guid? postId = null, 
        Guid? profileId = null, 
        int page = 0, 
        int pageSize = 20);
    
    /// <summary>
    /// Gets comments within a date range
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="postId">Optional post ID to filter by</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of results per page</param>
    /// <returns>Tuple of comments and total count</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        Guid? postId = null, 
        int page = 0, 
        int pageSize = 20);
    
    #endregion
    
    #region Moderation Operations
    
    /// <summary>
    /// Gets comments that may need moderation (flagged, reported, etc.)
    /// </summary>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of comments per page</param>
    /// <returns>Tuple of comments needing moderation and total count</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetCommentsForModerationAsync(
        int page = 0, 
        int pageSize = 20);
    
    /// <summary>
    /// Marks a comment as flagged for moderation
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="reason">Reason for flagging</param>
    /// <returns>True if flagged successfully</returns>
    Task<bool> FlagCommentAsync(Guid commentId, string reason);
    
    /// <summary>
    /// Gets soft-deleted comments for potential restoration
    /// </summary>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of comments per page</param>
    /// <returns>Tuple of deleted comments and total count</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetDeletedCommentsAsync(
        int page = 0, 
        int pageSize = 20);
    
    /// <summary>
    /// Restores a soft-deleted comment
    /// </summary>
    /// <param name="commentId">The comment ID to restore</param>
    /// <returns>True if restored successfully</returns>
    Task<bool> RestoreCommentAsync(Guid commentId);
    
    #endregion
    
    #region Statistics Operations
    
    /// <summary>
    /// Gets comment statistics for a specific post
    /// </summary>
    /// <param name="postId">The post ID</param>
    /// <returns>Comment statistics</returns>
    Task<CommentStatistics> GetCommentStatisticsAsync(Guid postId);
    
    /// <summary>
    /// Gets comment activity for a profile (comments, replies, engagement)
    /// </summary>
    /// <param name="profileId">The profile ID</param>
    /// <param name="sinceDate">Calculate statistics since this date</param>
    /// <returns>Profile comment activity</returns>
    Task<ProfileCommentActivity> GetProfileCommentActivityAsync(Guid profileId, DateTime sinceDate);
    
    #endregion
    
    #region Utility Operations
    
    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    /// <returns>Number of entities affected</returns>
    Task<int> SaveChangesAsync();
    
    /// <summary>
    /// Checks if a comment exists
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <returns>True if comment exists</returns>
    Task<bool> ExistsAsync(Guid commentId);
    
    /// <summary>
    /// Gets the depth level of a comment in its thread
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <returns>Depth level (0 = top-level comment)</returns>
    Task<int> GetCommentDepthAsync(Guid commentId);
    
    /// <summary>
    /// Gets the root comment ID for a comment thread
    /// </summary>
    /// <param name="commentId">Any comment ID in the thread</param>
    /// <returns>The root comment ID</returns>
    Task<Guid> GetRootCommentIdAsync(Guid commentId);
    
    #endregion
}

/// <summary>
/// Statistics for comments on a specific post
/// </summary>
public class CommentStatistics
{
    public int TotalComments { get; set; }
    public int TopLevelComments { get; set; }
    public int TotalReplies { get; set; }
    public int UniqueCommenters { get; set; }
    public double AverageRepliesPerComment { get; set; }
    public int MaxThreadDepth { get; set; }
    public DateTime? LatestCommentDate { get; set; }
    public DateTime? OldestCommentDate { get; set; }
}

/// <summary>
/// Comment activity statistics for a profile
/// </summary>
public class ProfileCommentActivity
{
    public int TotalComments { get; set; }
    public int TopLevelComments { get; set; }
    public int Replies { get; set; }
    public int PostsCommentedOn { get; set; }
    public double AverageCommentsPerPost { get; set; }
    public DateTime? LastCommentDate { get; set; }
    public DateTime? FirstCommentDate { get; set; }
}