using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of public (unauthenticated) API client.
/// Allows anonymous users to browse public content before signing up.
/// </summary>
public class PublicClient : BaseClient, IPublicClient
{
    public PublicClient(HttpClient httpClient, SivarClientOptions options)
        : base(httpClient, options) { }

    /// <summary>
    /// Gets public posts feed for unauthenticated users
    /// </summary>
    public async Task<PostFeedDto> GetPublicFeedAsync(int pageSize = 20, int pageNumber = 1, string? profileType = null, CancellationToken cancellationToken = default)
    {
        var url = $"api/public/feed?page={pageNumber - 1}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(profileType))
            url += $"&profileType={Uri.EscapeDataString(profileType)}";
        
        return await GetAsync<PostFeedDto>(url, cancellationToken);
    }

    /// <summary>
    /// Gets a specific public post by ID
    /// </summary>
    public async Task<PostDto?> GetPublicPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetAsync<PostDto>($"api/public/posts/{postId}", cancellationToken);
        }
        catch (SivarApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a public profile by ID
    /// </summary>
    public async Task<ProfileDto?> GetPublicProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetAsync<ProfileDto>($"api/public/profiles/{profileId}", cancellationToken);
        }
        catch (SivarApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a public profile by handle/username
    /// </summary>
    public async Task<ProfileDto?> GetPublicProfileByHandleAsync(string handle, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetAsync<ProfileDto>($"api/public/profiles/handle/{Uri.EscapeDataString(handle)}", cancellationToken);
        }
        catch (SivarApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets public posts by a specific profile
    /// </summary>
    public async Task<PostFeedDto> GetPublicPostsByProfileAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PostFeedDto>($"api/public/profiles/{profileId}/posts?page={pageNumber - 1}&pageSize={pageSize}", cancellationToken);
    }

    /// <summary>
    /// Gets similar profiles based on tags, type, and location
    /// </summary>
    public async Task<List<ProfileSummaryDto>> GetSimilarProfilesAsync(Guid profileId, int limit = 4, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetAsync<List<ProfileSummaryDto>>($"api/public/profiles/{profileId}/similar?limit={limit}", cancellationToken);
        }
        catch
        {
            return new List<ProfileSummaryDto>();
        }
    }

    /// <summary>
    /// Gets trending public posts
    /// </summary>
    public async Task<PostFeedDto> GetTrendingPostsAsync(int limit = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetAsync<PostFeedDto>($"api/public/posts/trending?limit={limit}", cancellationToken);
        }
        catch
        {
            return new PostFeedDto { Posts = new List<PostDto>(), TotalCount = 0 };
        }
    }
}
