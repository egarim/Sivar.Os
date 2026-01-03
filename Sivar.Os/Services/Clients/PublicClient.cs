using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of public client for anonymous access.
/// Delegates all operations to services to ensure unified business logic.
/// </summary>
public class PublicClient : IPublicClient
{
    private readonly IPostService _postService;
    private readonly IProfileService _profileService;
    private readonly ILogger<PublicClient> _logger;

    public PublicClient(
        IPostService postService,
        IProfileService profileService,
        ILogger<PublicClient> logger)
    {
        _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PostFeedDto> GetPublicFeedAsync(int pageSize = 20, int pageNumber = 1, string? profileType = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[PublicClient.GetPublicFeedAsync] PageSize={PageSize}, PageNumber={PageNumber}, ProfileType={ProfileType}", 
                pageSize, pageNumber, profileType);
            var (posts, totalCount) = await _postService.GetPublicFeedAsync(pageNumber, pageSize, profileType);
            var postList = posts.ToList();
            return new PostFeedDto
            {
                Posts = postList,
                Page = pageNumber - 1, // 0-based page
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicClient.GetPublicFeedAsync] Error fetching public feed");
            return new PostFeedDto { Posts = new List<PostDto>(), Page = 0, PageSize = pageSize, TotalCount = 0 };
        }
    }

    /// <inheritdoc />
    public async Task<PostDto?> GetPublicPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[PublicClient.GetPublicPostAsync] PostId={PostId}", postId);
            return await _postService.GetPublicPostByIdAsync(postId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicClient.GetPublicPostAsync] Error fetching public post {PostId}", postId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDto?> GetPublicProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[PublicClient.GetPublicProfileAsync] ProfileId={ProfileId}", profileId);
            return await _profileService.GetPublicProfileByIdAsync(profileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicClient.GetPublicProfileAsync] Error fetching public profile {ProfileId}", profileId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDto?> GetPublicProfileByHandleAsync(string handle, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[PublicClient.GetPublicProfileByHandleAsync] Handle={Handle}", handle);
            return await _profileService.GetPublicProfileByHandleAsync(handle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicClient.GetPublicProfileByHandleAsync] Error fetching public profile by handle {Handle}", handle);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PostFeedDto> GetPublicPostsByProfileAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[PublicClient.GetPublicProfilePostsAsync] ProfileId={ProfileId}, PageSize={PageSize}, PageNumber={PageNumber}", 
                profileId, pageSize, pageNumber);
            var (posts, totalCount) = await _postService.GetPublicPostsByProfileAsync(profileId, pageNumber, pageSize);
            var postList = posts.ToList();
            return new PostFeedDto
            {
                Posts = postList,
                Page = pageNumber - 1, // 0-based page
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicClient.GetPublicProfilePostsAsync] Error fetching public posts for profile {ProfileId}", profileId);
            return new PostFeedDto { Posts = new List<PostDto>(), Page = 0, PageSize = pageSize, TotalCount = 0 };
        }
    }

    /// <inheritdoc />
    public async Task<List<ProfileSummaryDto>> GetSimilarProfilesAsync(Guid profileId, int limit = 4, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[PublicClient.GetSimilarProfilesAsync] ProfileId={ProfileId}, Limit={Limit}", profileId, limit);
            return await _profileService.GetSimilarProfilesAsync(profileId, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicClient.GetSimilarProfilesAsync] Error fetching similar profiles for {ProfileId}", profileId);
            return new List<ProfileSummaryDto>();
        }
    }

    /// <inheritdoc />
    public async Task<PostFeedDto> GetTrendingPostsAsync(int limit = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[PublicClient.GetTrendingPostsAsync] Limit={Limit}", limit);
            var (posts, totalCount) = await _postService.GetTrendingPublicPostsAsync(limit);
            return new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = 0,
                PageSize = limit,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PublicClient.GetTrendingPostsAsync] Error fetching trending posts");
            return new PostFeedDto { Posts = new List<PostDto>(), TotalCount = 0 };
        }
    }
}
