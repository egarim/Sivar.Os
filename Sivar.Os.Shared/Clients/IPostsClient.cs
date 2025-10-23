using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client for post operations
/// </summary>
public interface IPostsClient
{
    // CRUD operations
    Task<PostDto> CreatePostAsync(CreatePostDto request, CancellationToken cancellationToken = default);
    Task<PostDto> GetPostAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<PostDto> UpdatePostAsync(Guid postId, UpdatePostDto request, CancellationToken cancellationToken = default);
    Task DeletePostAsync(Guid postId, CancellationToken cancellationToken = default);

    // Feed and discovery
    Task<IEnumerable<PostDto>> GetFeedPostsAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<PostDto>> GetProfilePostsAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<PostDto>> SearchPostsAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<PostDto>> GetTrendingPostsAsync(int pageSize = 20, CancellationToken cancellationToken = default);

    // Analytics
    Task<PostAnalyticsDto> GetPostAnalyticsAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PostActivityDto>> GetProfileActivityAsync(Guid profileId, int days = 30, CancellationToken cancellationToken = default);
}
