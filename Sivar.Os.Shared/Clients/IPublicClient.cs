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
}
