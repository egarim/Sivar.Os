
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of posts client
/// </summary>
public class PostsClient : BaseClient, IPostsClient
{
    public PostsClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    public async Task<PostDto> CreatePostAsync(CreatePostDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<PostDto>("api/posts", request, cancellationToken);
    }

    public async Task<PostDto> GetPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PostDto>($"api/posts/{postId}", cancellationToken);
    }

    public async Task<PostDto> UpdatePostAsync(Guid postId, UpdatePostDto request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<PostDto>($"api/posts/{postId}", request, cancellationToken);
    }

    public async Task DeletePostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/posts/{postId}", cancellationToken);
    }

    public async Task<PostFeedDto> GetFeedPostsAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PostFeedDto>($"api/posts/feed?pageSize={pageSize}&pageNumber={pageNumber}", cancellationToken);
    }

    public async Task<PostFeedDto> GetProfilePostsAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PostFeedDto>($"api/posts/profile/{profileId}?pageSize={pageSize}&pageNumber={pageNumber}", cancellationToken);
    }

    public async Task<PostFeedDto> SearchPostsAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PostFeedDto>($"api/posts/search?query={Uri.EscapeDataString(query)}&pageSize={pageSize}&pageNumber={pageNumber}", cancellationToken);
    }

    public async Task<PostFeedDto> GetTrendingPostsAsync(int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PostFeedDto>($"api/posts/trending?limit={pageSize}", cancellationToken);
    }

    public async Task<PostAnalyticsDto> GetPostAnalyticsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PostAnalyticsDto>($"api/posts/{postId}/analytics", cancellationToken);
    }

    public async Task<IEnumerable<PostActivityDto>> GetProfileActivityAsync(Guid profileId, int days = 30, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<PostActivityDto>>($"api/posts/activity/{profileId}?days={days}", cancellationToken);
    }
}
