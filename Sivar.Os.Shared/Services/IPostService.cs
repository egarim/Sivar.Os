using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for Post management in the activity stream
/// Provides business logic layer for post operations with validation and error handling
/// </summary>
public interface IPostService
{
    /// <summary>
    /// Creates a new post for the authenticated user's active profile
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <param name="createPostDto">Post creation data</param>
    /// <returns>Created post DTO if successful, null if user/profile not found</returns>
    Task<PostDto?> CreatePostAsync(string keycloakId, CreatePostDto createPostDto);

    /// <summary>
    /// Gets a post by ID with permission validation
    /// </summary>
    /// <param name="postId">Post unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting the post (for permission checks)</param>
    /// <param name="includeReactions">Include reaction counts and user's reaction</param>
    /// <param name="includeComments">Include comments with the post</param>
    /// <returns>Post DTO if found and accessible, null otherwise</returns>
    Task<PostDto?> GetPostByIdAsync(Guid postId, string? requestingKeycloakId = null, bool includeReactions = true, bool includeComments = true);

    /// <summary>
    /// Updates an existing post (only by the author)
    /// </summary>
    /// <param name="postId">Post unique identifier</param>
    /// <param name="keycloakId">Keycloak user identifier (must be post author)</param>
    /// <param name="updatePostDto">Post update data</param>
    /// <returns>Updated post DTO if successful, null if not found or unauthorized</returns>
    Task<PostDto?> UpdatePostAsync(Guid postId, string keycloakId, UpdatePostDto updatePostDto);

    /// <summary>
    /// Deletes a post (only by the author)
    /// </summary>
    /// <param name="postId">Post unique identifier</param>
    /// <param name="keycloakId">Keycloak user identifier (must be post author)</param>
    /// <returns>True if deleted successfully, false if not found or unauthorized</returns>
    Task<bool> DeletePostAsync(Guid postId, string keycloakId);

    /// <summary>
    /// Gets posts for a user's activity feed with pagination
    /// Includes posts from followed profiles and own posts
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of posts per page</param>
    /// <param name="profileType">Filter by profile type name (e.g., "Business", "Personal")</param>
    /// <returns>Paginated list of posts for the user's feed</returns>
    Task<(IEnumerable<PostDto> Posts, int TotalCount)> GetActivityFeedAsync(string keycloakId, int page = 1, int pageSize = 10, string? profileType = null);

    /// <summary>
    /// Gets posts by a specific profile with pagination
    /// </summary>
    /// <param name="profileId">Profile unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting (for permission checks)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of posts per page</param>
    /// <param name="postType">Optional filter by post type</param>
    /// <returns>Paginated list of posts by the profile</returns>
    Task<(IEnumerable<PostDto> Posts, int TotalCount)> GetPostsByProfileAsync(Guid profileId, string? requestingKeycloakId = null, int page = 1, int pageSize = 10, PostType? postType = null);

    /// <summary>
    /// Gets posts by post type with pagination
    /// </summary>
    /// <param name="postType">Type of posts to retrieve</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting (for permission checks)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of posts per page</param>
    /// <returns>Paginated list of posts of the specified type</returns>
    Task<(IEnumerable<PostDto> Posts, int TotalCount)> GetPostsByTypeAsync(PostType postType, string? requestingKeycloakId = null, int page = 1, int pageSize = 10);

    /// <summary>
    /// Searches posts by content with pagination
    /// </summary>
    /// <param name="searchTerm">Search term to look for in post content</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting (for permission checks)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of posts per page</param>
    /// <returns>Paginated list of matching posts</returns>
    Task<(IEnumerable<PostDto> Posts, int TotalCount)> SearchPostsAsync(string searchTerm, string? requestingKeycloakId = null, int page = 1, int pageSize = 10);

    /// <summary>
    /// Gets post engagement statistics (views, reactions, comments)
    /// </summary>
    /// <param name="postId">Post unique identifier</param>
    /// <param name="keycloakId">Keycloak user identifier (must be post author)</param>
    /// <returns>Post engagement statistics if authorized, null otherwise</returns>
    Task<PostEngagementDto?> GetPostEngagementAsync(Guid postId, string keycloakId);

    /// <summary>
    /// Validates if a user can view a specific post based on visibility settings
    /// </summary>
    /// <param name="postId">Post unique identifier</param>
    /// <param name="requestingKeycloakId">Keycloak ID of user requesting access</param>
    /// <returns>True if user can view the post, false otherwise</returns>
    Task<bool> CanUserViewPostAsync(Guid postId, string? requestingKeycloakId);

    /// <summary>
    /// Gets all posts that have vector embeddings for semantic search
    /// </summary>
    /// <returns>List of posts with their vector embeddings</returns>
    Task<List<PostDto>> GetAllPostsWithEmbeddingsAsync();

    /// <summary>
    /// Gets all post entities that have vector embeddings (for internal use)
    /// </summary>
    /// <returns>List of post entities with their vector embeddings</returns>
    Task<List<Post>> GetAllPostEntitiesWithEmbeddingsAsync();

    /// <summary>
    /// Finds posts near a geographic location using PostGIS
    /// </summary>
    /// <param name="latitude">Latitude of the search center (-90 to 90)</param>
    /// <param name="longitude">Longitude of the search center (-180 to 180)</param>
    /// <param name="radiusKm">Search radius in kilometers (default: 10km)</param>
    /// <param name="pageSize">Number of posts to return (default: 20)</param>
    /// <param name="pageNumber">Page number for pagination (default: 1)</param>
    /// <returns>PostFeedDto with nearby posts and metadata</returns>
    Task<PostFeedDto> FindNearbyPostsAsync(double latitude, double longitude, double radiusKm = 10, int pageSize = 20, int pageNumber = 1);
}