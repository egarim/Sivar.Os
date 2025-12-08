
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of posts client.
/// Delegates all operations to IPostService to ensure unified business logic.
/// This avoids duplicating mapping/URL resolution logic that exists in the service layer.
/// </summary>
public class PostsClient : BaseRepositoryClient, IPostsClient
{
    private readonly IPostService _postService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PostsClient> _logger;

    public PostsClient(
        IPostService postService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostsClient> logger)
    {
        _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new post
    /// </summary>
    public async Task<PostDto> CreatePostAsync(CreatePostDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            _logger.LogWarning("[PostsClient.CreatePostAsync] Called with null request");
            return null!;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[PostsClient.CreatePostAsync] No authenticated user");
                return null!;
            }

            _logger.LogInformation("[PostsClient.CreatePostAsync] KeycloakId={KeycloakId}, ContentLength={Length}", 
                keycloakId, request.Content?.Length ?? 0);
            
            var post = await _postService.CreatePostAsync(keycloakId, request);
            return post ?? null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsClient.CreatePostAsync] Error creating post");
            throw;
        }
    }

    /// <summary>
    /// Gets a post by ID - delegates to PostService.GetPostByIdAsync
    /// </summary>
    public async Task<PostDto> GetPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        if (postId == Guid.Empty)
        {
            _logger.LogWarning("[PostsClient.GetPostAsync] Called with empty post ID");
            return null!;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            _logger.LogInformation("[PostsClient.GetPostAsync] PostId={PostId}, KeycloakId={KeycloakId}", 
                postId, keycloakId ?? "anonymous");
            
            // Delegate to service - it handles URL resolution, visibility checks, etc.
            var post = await _postService.GetPostByIdAsync(postId, keycloakId);
            return post ?? null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsClient.GetPostAsync] Error retrieving post {PostId}", postId);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing post
    /// </summary>
    public async Task<PostDto> UpdatePostAsync(Guid postId, UpdatePostDto request, CancellationToken cancellationToken = default)
    {
        if (postId == Guid.Empty || request == null)
        {
            _logger.LogWarning("[PostsClient.UpdatePostAsync] Called with invalid parameters");
            return null!;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[PostsClient.UpdatePostAsync] No authenticated user");
                return null!;
            }

            _logger.LogInformation("[PostsClient.UpdatePostAsync] KeycloakId={KeycloakId}, PostId={PostId}", 
                keycloakId, postId);
            
            var post = await _postService.UpdatePostAsync(postId, keycloakId, request);
            return post ?? null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsClient.UpdatePostAsync] Error updating post {PostId}", postId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a post
    /// </summary>
    public async Task DeletePostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        if (postId == Guid.Empty)
        {
            _logger.LogWarning("[PostsClient.DeletePostAsync] Called with empty post ID");
            return;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[PostsClient.DeletePostAsync] No authenticated user");
                return;
            }

            _logger.LogInformation("[PostsClient.DeletePostAsync] KeycloakId={KeycloakId}, PostId={PostId}", 
                keycloakId, postId);
            
            var deleted = await _postService.DeletePostAsync(postId, keycloakId);
            
            if (deleted)
            {
                _logger.LogInformation("[PostsClient.DeletePostAsync] Post deleted: {PostId}", postId);
            }
            else
            {
                _logger.LogWarning("[PostsClient.DeletePostAsync] Post not found or unauthorized: {PostId}", postId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsClient.DeletePostAsync] Error deleting post {PostId}", postId);
            throw;
        }
    }

    /// <summary>
    /// Gets feed posts - delegates to PostService.GetActivityFeedAsync
    /// </summary>
    public async Task<PostFeedDto> GetFeedPostsAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[PostsClient.GetFeedPostsAsync] No authenticated user - returning empty feed");
                return new PostFeedDto
                {
                    Posts = new List<PostDto>(),
                    Page = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
            
            _logger.LogInformation("[PostsClient.GetFeedPostsAsync] KeycloakId={KeycloakId}, Page={Page}, PageSize={PageSize}", 
                keycloakId, pageNumber, pageSize);
            
            // Delegate to service - unified business logic
            var (posts, totalCount) = await _postService.GetActivityFeedAsync(keycloakId, pageNumber, pageSize);
            
            _logger.LogInformation("[PostsClient.GetFeedPostsAsync] Retrieved {Count} posts (Total: {TotalCount})", 
                posts.Count(), totalCount);
            
            return new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsClient.GetFeedPostsAsync] Error retrieving feed posts");
            throw;
        }
    }

    /// <summary>
    /// Gets posts for a specific profile - delegates to PostService.GetPostsByProfileAsync
    /// </summary>
    public async Task<PostFeedDto> GetProfilePostsAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, PostType? postType = null, CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            _logger.LogWarning("[PostsClient.GetProfilePostsAsync] Called with empty profile ID");
            return new PostFeedDto
            {
                Posts = new List<PostDto>(),
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            
            _logger.LogInformation("[PostsClient.GetProfilePostsAsync] ProfileId={ProfileId}, Page={Page}, PageSize={PageSize}, PostType={PostType}", 
                profileId, pageNumber, pageSize, postType?.ToString() ?? "ALL");
            
            // Delegate to service - unified business logic with URL resolution
            var (posts, totalCount) = await _postService.GetPostsByProfileAsync(
                profileId, 
                keycloakId, 
                pageNumber, 
                pageSize, 
                postType);
            
            _logger.LogInformation("[PostsClient.GetProfilePostsAsync] Retrieved {Count} posts (Total: {TotalCount})", 
                posts.Count(), totalCount);
            
            return new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsClient.GetProfilePostsAsync] Error retrieving profile posts for {ProfileId}", profileId);
            throw;
        }
    }

    /// <summary>
    /// Searches for posts - delegates to PostService.SearchPostsAsync
    /// </summary>
    public async Task<PostFeedDto> SearchPostsAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("[PostsClient.SearchPostsAsync] Called with empty query");
            return new PostFeedDto
            {
                Posts = new List<PostDto>(),
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            
            _logger.LogInformation("[PostsClient.SearchPostsAsync] Query='{Query}', Page={Page}, PageSize={PageSize}", 
                query, pageNumber, pageSize);
            
            // Delegate to service - unified business logic
            var (posts, totalCount) = await _postService.SearchPostsAsync(query, keycloakId, pageNumber, pageSize);
            
            _logger.LogInformation("[PostsClient.SearchPostsAsync] Found {Count} posts for query '{Query}'", 
                posts.Count(), query);
            
            return new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsClient.SearchPostsAsync] Error searching posts with query '{Query}'", query);
            throw;
        }
    }

    /// <summary>
    /// Gets trending posts
    /// </summary>
    public async Task<PostFeedDto> GetTrendingPostsAsync(int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            
            _logger.LogInformation("[PostsClient.GetTrendingPostsAsync] PageSize={PageSize}", pageSize);
            
            // TODO: Add GetTrendingPostsAsync to IPostService and delegate
            // For now, use GetPostsByTypeAsync with General type as a placeholder
            var (posts, totalCount) = await _postService.GetPostsByTypeAsync(
                PostType.General, 
                keycloakId, 
                page: 1, 
                pageSize: pageSize);
            
            _logger.LogInformation("[PostsClient.GetTrendingPostsAsync] Retrieved {Count} posts", posts.Count());
            
            return new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = 1,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsClient.GetTrendingPostsAsync] Error retrieving trending posts");
            throw;
        }
    }

    /// <summary>
    /// Gets analytics for a post
    /// </summary>
    public async Task<PostAnalyticsDto> GetPostAnalyticsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        if (postId == Guid.Empty)
        {
            _logger.LogWarning("[PostsClient.GetPostAnalyticsAsync] Called with empty post ID");
            return null!;
        }

        try
        {
            // TODO: Add GetPostAnalyticsAsync to IPostService and delegate
            var analytics = new PostAnalyticsDto
            {
                PostId = postId,
                Views = 0,
                TotalReactions = 0,
                TotalComments = 0,
                ReactionsByType = new Dictionary<ReactionType, int>(),
                EngagementRate = 0.0
            };

            _logger.LogInformation("[PostsClient.GetPostAnalyticsAsync] Retrieved analytics for post {PostId}", postId);
            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsClient.GetPostAnalyticsAsync] Error retrieving analytics for {PostId}", postId);
            throw;
        }
    }

    /// <summary>
    /// Gets profile activity
    /// </summary>
    public async Task<IEnumerable<PostActivityDto>> GetProfileActivityAsync(Guid profileId, int days = 30, CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            _logger.LogWarning("[PostsClient.GetProfileActivityAsync] Called with empty profile ID");
            return new List<PostActivityDto>();
        }

        try
        {
            // TODO: Add GetProfileActivityAsync to IPostService and delegate
            _logger.LogInformation("[PostsClient.GetProfileActivityAsync] ProfileId={ProfileId}, Days={Days}", profileId, days);
            return new List<PostActivityDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsClient.GetProfileActivityAsync] Error retrieving activity for {ProfileId}", profileId);
            throw;
        }
    }

    /// <summary>
    /// Finds nearby posts - delegates to PostService.FindNearbyPostsAsync
    /// </summary>
    public async Task<PostFeedDto> FindNearbyPostsAsync(double latitude, double longitude, double radiusKm = 10, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[PostsClient.FindNearbyPostsAsync] Lat={Lat}, Lon={Lon}, Radius={Radius}km", 
                latitude, longitude, radiusKm);
            
            // Delegate to service
            var feed = await _postService.FindNearbyPostsAsync(latitude, longitude, radiusKm, pageSize, pageNumber);
            return feed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsClient.FindNearbyPostsAsync] Error finding nearby posts");
            throw;
        }
    }

    #region Helper Methods

    /// <summary>
    /// Extracts the Keycloak ID from the current HTTP context
    /// </summary>
    private string? GetKeycloakIdFromContext()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext?.User == null)
        {
            return null;
        }

        // Check for mock authentication header (for integration tests)
        if (httpContext.Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            return keycloakIdHeader.ToString();
        }

        // Check if user is authenticated via claims
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var subClaim = httpContext.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(subClaim))
            {
                return subClaim;
            }

            // Fallback: try to find other common claims
            var userIdClaim = httpContext.User.FindFirst("user_id")?.Value 
                           ?? httpContext.User.FindFirst("id")?.Value 
                           ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim;
        }

        return null;
    }

    #endregion
}
