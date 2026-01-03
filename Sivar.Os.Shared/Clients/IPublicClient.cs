using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client interface for public (unauthenticated) API access.
/// Allows anonymous users to browse public content before signing up.
/// </summary>
public interface IPublicClient
{
    /// <summary>
    /// Gets public posts feed for unauthenticated users
    /// </summary>
    /// <param name="pageSize">Number of posts per page</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="profileType">Optional filter by profile type (e.g., "Business", "Personal")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of public posts</returns>
    Task<PostFeedDto> GetPublicFeedAsync(int pageSize = 20, int pageNumber = 1, string? profileType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific public post by ID
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Post details if public, null otherwise</returns>
    Task<PostDto?> GetPublicPostAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a public profile by ID
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Profile details if found</returns>
    Task<ProfileDto?> GetPublicProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a public profile by handle/username
    /// </summary>
    /// <param name="handle">Profile handle (username)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Profile details if found</returns>
    Task<ProfileDto?> GetPublicProfileByHandleAsync(string handle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets public posts by a specific profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="pageSize">Number of posts per page</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of public posts by the profile</returns>
    Task<PostFeedDto> GetPublicPostsByProfileAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets similar profiles based on tags, type, and location
    /// </summary>
    /// <param name="profileId">Profile ID to find similar profiles for</param>
    /// <param name="limit">Maximum number of similar profiles</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of similar profile summaries</returns>
    Task<List<ProfileSummaryDto>> GetSimilarProfilesAsync(Guid profileId, int limit = 4, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trending public posts
    /// </summary>
    /// <param name="limit">Maximum number of trending posts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Trending posts feed</returns>
    Task<PostFeedDto> GetTrendingPostsAsync(int limit = 5, CancellationToken cancellationToken = default);
}
