
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for Comment management in the activity stream
/// Provides business logic layer for comment operations with validation and threading support
/// </summary>
public interface ICommentService
{
    /// <summary>
    /// Creates a new comment on a post for the authenticated user's active profile
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <param name="createCommentDto">Comment creation data</param>
    /// <returns>Created comment DTO if successful, null if user/profile/post not found</returns>
    Task<CommentDto?> CreateCommentAsync(string keycloakId, CreateCommentDto createCommentDto);

    /// <summary>
    /// Creates a reply to an existing comment
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <param name="parentCommentId">Parent comment unique identifier</param>
    /// <param name="createReplyDto">Reply creation data</param>
    /// <returns>Created reply DTO if successful, null if user/profile/comment not found</returns>
    Task<CommentDto?> CreateReplyAsync(string keycloakId, Guid parentCommentId, CreateReplyDto createReplyDto);

    /// <summary>
    /// Gets a comment by ID with permission validation
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting (for permission checks)</param>
    /// <param name="includeReplies">Include nested replies</param>
    /// <param name="includeReactions">Include reaction counts and user's reaction</param>
    /// <returns>Comment DTO if found and accessible, null otherwise</returns>
    Task<CommentDto?> GetCommentByIdAsync(Guid commentId, string? requestingKeycloakId = null, bool includeReplies = true, bool includeReactions = true);

    /// <summary>
    /// Updates an existing comment (only by the author)
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="keycloakId">Keycloak user identifier (must be comment author)</param>
    /// <param name="updateCommentDto">Comment update data</param>
    /// <returns>Updated comment DTO if successful, null if not found or unauthorized</returns>
    Task<CommentDto?> UpdateCommentAsync(Guid commentId, string keycloakId, UpdateCommentDto updateCommentDto);

    /// <summary>
    /// Deletes a comment (only by the author)
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="keycloakId">Keycloak user identifier (must be comment author)</param>
    /// <returns>True if deleted successfully, false if not found or unauthorized</returns>
    Task<bool> DeleteCommentAsync(Guid commentId, string keycloakId);

    /// <summary>
    /// Gets comments for a specific post with pagination and threading
    /// </summary>
    /// <param name="postId">Post unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting (for permission checks)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of top-level comments per page</param>
    /// <param name="includeReplies">Include nested replies</param>
    /// <returns>Paginated list of comments for the post</returns>
    Task<(IEnumerable<CommentDto> Comments, int TotalCount)> GetCommentsByPostAsync(Guid postId, string? requestingKeycloakId = null, int page = 1, int pageSize = 20, bool includeReplies = true);

    /// <summary>
    /// Gets replies to a specific comment with pagination
    /// </summary>
    /// <param name="parentCommentId">Parent comment unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting (for permission checks)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of replies per page</param>
    /// <returns>Paginated list of replies to the comment</returns>
    Task<(IEnumerable<CommentDto> Replies, int TotalCount)> GetRepliesByCommentAsync(Guid parentCommentId, string? requestingKeycloakId = null, int page = 1, int pageSize = 10);

    /// <summary>
    /// Gets comments by a specific profile with pagination
    /// </summary>
    /// <param name="profileId">Profile unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting (for permission checks)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of comments per page</param>
    /// <returns>Paginated list of comments by the profile</returns>
    Task<(IEnumerable<CommentDto> Comments, int TotalCount)> GetCommentsByProfileAsync(Guid profileId, string? requestingKeycloakId = null, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets comment thread depth and reply count for a comment
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <returns>Thread statistics including depth and total reply count</returns>
    Task<CommentThreadStatsDto?> GetCommentThreadStatsAsync(Guid commentId);

    /// <summary>
    /// Validates if a user can view a specific comment based on post visibility
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting access</param>
    /// <returns>True if user can view the comment, false otherwise</returns>
    Task<bool> CanUserViewCommentAsync(Guid commentId, string? requestingKeycloakId);

    /// <summary>
    /// Gets recent activity for comments (for notifications and activity feeds)
    /// </summary>
    /// <param name="profileId">Profile unique identifier to get activity for</param>
    /// <param name="hoursBack">Hours to look back for activity</param>
    /// <returns>List of recent comment activities</returns>
    Task<IEnumerable<CommentActivityDto>> GetRecentCommentActivityAsync(Guid profileId, int hoursBack = 24);

    /// <summary>
    /// Gets comment counts for multiple posts in a single batch operation
    /// </summary>
    /// <param name="postIds">List of post IDs to get counts for</param>
    /// <returns>Dictionary mapping post ID to comment count</returns>
    Task<Dictionary<Guid, int>> GetCommentCountsByPostIdsAsync(IEnumerable<Guid> postIds);
}