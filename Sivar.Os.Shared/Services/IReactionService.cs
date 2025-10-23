using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for Reaction management in the activity stream
/// Provides business logic layer for reaction operations with analytics and engagement features
/// </summary>
public interface IReactionService
{
    /// <summary>
    /// Toggles a reaction on a post for the authenticated user's active profile
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <param name="postId">Post unique identifier</param>
    /// <param name="reactionType">Type of reaction to toggle</param>
    /// <returns>Reaction result with action taken, null if user/profile/post not found</returns>
    Task<ReactionResultDto?> TogglePostReactionAsync(string keycloakId, Guid postId, ReactionType reactionType);

    /// <summary>
    /// Toggles a reaction on a comment for the authenticated user's active profile
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="reactionType">Type of reaction to toggle</param>
    /// <returns>Reaction result with action taken, null if user/profile/comment not found</returns>
    Task<ReactionResultDto?> ToggleCommentReactionAsync(string keycloakId, Guid commentId, ReactionType reactionType);

    /// <summary>
    /// Gets reaction counts and user's reaction for a post
    /// </summary>
    /// <param name="postId">Post unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting (to include their reaction)</param>
    /// <returns>Post reaction summary with counts and user's reaction</returns>
    Task<PostReactionSummaryDto> GetPostReactionSummaryAsync(Guid postId, string? requestingKeycloakId = null);

    /// <summary>
    /// Gets reaction counts and user's reaction for a comment
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting (to include their reaction)</param>
    /// <returns>Comment reaction summary with counts and user's reaction</returns>
    Task<CommentReactionSummaryDto> GetCommentReactionSummaryAsync(Guid commentId, string? requestingKeycloakId = null);

    /// <summary>
    /// Gets detailed reaction analytics for a post (author only)
    /// </summary>
    /// <param name="postId">Post unique identifier</param>
    /// <param name="keycloakId">Keycloak user identifier (must be post author)</param>
    /// <returns>Detailed reaction analytics if authorized, null otherwise</returns>
    Task<PostReactionAnalyticsDto?> GetPostReactionAnalyticsAsync(Guid postId, string keycloakId);

    /// <summary>
    /// Gets reactions by a specific profile with pagination
    /// </summary>
    /// <param name="profileId">Profile unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting (for permission checks)</param>
    /// <param name="reactionType">Filter by specific reaction type (optional)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of reactions per page</param>
    /// <returns>Paginated list of reactions by the profile</returns>
    Task<(IEnumerable<ReactionDto> Reactions, int TotalCount)> GetReactionsByProfileAsync(
        Guid profileId, 
        string? requestingKeycloakId = null,
        ReactionType? reactionType = null,
        int page = 1, 
        int pageSize = 20);

    /// <summary>
    /// Gets who reacted to a post with specific reaction type
    /// </summary>
    /// <param name="postId">Post unique identifier</param>
    /// <param name="reactionType">Type of reaction to filter by</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting (for permission checks)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of reactions per page</param>
    /// <returns>Paginated list of reactions with profile information</returns>
    Task<(IEnumerable<ReactionWithProfileDto> Reactions, int TotalCount)> GetPostReactionsByTypeAsync(
        Guid postId,
        ReactionType reactionType, 
        string? requestingKeycloakId = null,
        int page = 1, 
        int pageSize = 20);

    /// <summary>
    /// Gets who reacted to a comment with specific reaction type
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="reactionType">Type of reaction to filter by</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting (for permission checks)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of reactions per page</param>
    /// <returns>Paginated list of reactions with profile information</returns>
    Task<(IEnumerable<ReactionWithProfileDto> Reactions, int TotalCount)> GetCommentReactionsByTypeAsync(
        Guid commentId,
        ReactionType reactionType, 
        string? requestingKeycloakId = null,
        int page = 1, 
        int pageSize = 20);

    /// <summary>
    /// Gets reaction engagement statistics for a profile's content
    /// </summary>
    /// <param name="profileId">Profile unique identifier</param>
    /// <param name="keycloakId">Keycloak user identifier (must own the profile)</param>
    /// <param name="daysBack">Number of days to analyze (default 30)</param>
    /// <returns>Profile reaction engagement statistics if authorized, null otherwise</returns>
    Task<ProfileReactionEngagementDto?> GetProfileReactionEngagementAsync(Guid profileId, string keycloakId, int daysBack = 30);

    /// <summary>
    /// Gets recent reaction activity for a profile (for notifications)
    /// </summary>
    /// <param name="profileId">Profile unique identifier to get activity for</param>
    /// <param name="hoursBack">Hours to look back for activity</param>
    /// <returns>List of recent reaction activities</returns>
    Task<IEnumerable<ReactionActivityDto>> GetRecentReactionActivityAsync(Guid profileId, int hoursBack = 24);

    /// <summary>
    /// Validates if a user can react to a specific post
    /// </summary>
    /// <param name="postId">Post unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting to react</param>
    /// <returns>True if user can react to the post, false otherwise</returns>
    Task<bool> CanUserReactToPostAsync(Guid postId, string requestingKeycloakId);

    /// <summary>
    /// Validates if a user can react to a specific comment
    /// </summary>
    /// <param name="commentId">Comment unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting to react</param>
    /// <returns>True if user can react to the comment, false otherwise</returns>
    Task<bool> CanUserReactToCommentAsync(Guid commentId, string requestingKeycloakId);
}