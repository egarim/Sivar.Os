
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of posts client using repositories and services
/// Provides the same interface as the HTTP client but operates directly on the service layer
/// </summary>
public class PostsClient : BaseRepositoryClient, IPostsClient
{
    private readonly IPostService _postService;
    private readonly IPostRepository _postRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PostsClient> _logger;

    public PostsClient(
        IPostService postService,
        IPostRepository postRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostsClient> logger)
    {
        _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
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
            _logger.LogWarning("CreatePostAsync called with null request");
            return null!;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("CreatePostAsync: No authenticated user");
                return null!;
            }

            _logger.LogInformation("CreatePostAsync: {KeycloakId}, Content length={Length}", keycloakId, request.Content?.Length ?? 0);
            var post = await _postService.CreatePostAsync(keycloakId, request);
            return post ?? null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            throw;
        }
    }

    /// <summary>
    /// Gets a post by ID
    /// </summary>
    public async Task<PostDto> GetPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        if (postId == Guid.Empty)
        {
            _logger.LogWarning("GetPostAsync called with empty post ID");
            return null!;
        }

        try
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Post not found: {PostId}", postId);
                return null!;
            }

            return MapPostToDto(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving post {PostId}", postId);
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
            _logger.LogWarning("UpdatePostAsync called with invalid parameters");
            return null!;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("UpdatePostAsync: No authenticated user");
                return null!;
            }

            _logger.LogInformation("UpdatePostAsync: {KeycloakId}, PostId={PostId}", keycloakId, postId);
            var post = await _postService.UpdatePostAsync(postId, keycloakId, request);
            return post ?? null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post {PostId}", postId);
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
            _logger.LogWarning("DeletePostAsync called with empty post ID");
            return;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("DeletePostAsync: No authenticated user");
                return;
            }

            _logger.LogInformation("DeletePostAsync: {KeycloakId}, PostId={PostId}", keycloakId, postId);
            var deleted = await _postService.DeletePostAsync(postId, keycloakId);
            
            if (deleted)
            {
                _logger.LogInformation("Post deleted: {PostId}", postId);
            }
            else
            {
                _logger.LogWarning("Post not found or unauthorized for deletion: {PostId}", postId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", postId);
            throw;
        }
    }

    /// <summary>
    /// Gets feed posts
    /// </summary>
    public async Task<PostFeedDto> GetFeedPostsAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[PostsClient.GetFeedPostsAsync] Server-side called for page {PageNumber}, pageSize {PageSize}", pageNumber, pageSize);
            
            // Get Keycloak ID from HttpContext claims
            var keycloakId = _httpContextAccessor.HttpContext?.User?.Claims
                .FirstOrDefault(c => c.Type == "sub")?.Value;
            
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[PostsClient.GetFeedPostsAsync] No Keycloak ID found in claims - user not authenticated. Returning empty feed.");
                return new PostFeedDto
                {
                    Posts = new List<PostDto>(),
                    Page = pageNumber,  // Keep as 1-based
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
            
            _logger.LogInformation("[PostsClient.GetFeedPostsAsync] Found Keycloak ID: {KeycloakId}, calling PostService", keycloakId);
            
            // Call the actual PostService
            var (posts, totalCount) = await _postService.GetActivityFeedAsync(
                keycloakId, 
                pageNumber, 
                pageSize);
            
            _logger.LogInformation("[PostsClient.GetFeedPostsAsync] PostService returned {Count} posts (total: {TotalCount})", posts.Count(), totalCount);
            
            var feed = new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = pageNumber,  // Keep as 1-based to match UI expectations (no -1)
                PageSize = pageSize,
                TotalCount = totalCount
            };
            
            _logger.LogInformation("[PostsClient.GetFeedPostsAsync] Returning feed with {Count} items", feed.Posts.Count);
            return feed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsClient.GetFeedPostsAsync] Error retrieving feed posts");
            throw;
        }
    }

    /// <summary>
    /// Gets posts for a specific profile
    /// </summary>
    public async Task<PostFeedDto> GetProfilePostsAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            _logger.LogWarning("GetProfilePostsAsync called with empty profile ID");
            return new PostFeedDto
            {
                Posts = new List<PostDto>(),
                Page = pageNumber - 1,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        try
        {
            var (posts, totalCount) = await _postRepository.GetByProfileAsync(profileId, pageSize, pageNumber);
            var dtos = posts.Select(MapPostToDto).ToList();

            _logger.LogInformation("Profile posts retrieved for profile {ProfileId}: {Count} items", profileId, dtos.Count);
            return new PostFeedDto
            {
                Posts = dtos,
                Page = pageNumber,  // Keep as 1-based
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile posts for {ProfileId}", profileId);
            throw;
        }
    }

    /// <summary>
    /// Searches for posts
    /// </summary>
    public async Task<PostFeedDto> SearchPostsAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("SearchPostsAsync called with empty query");
            return new PostFeedDto
            {
                Posts = new List<PostDto>(),
                Page = pageNumber,  // Keep as 1-based
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        try
        {
            var (posts, totalCount) = await _postRepository.SearchPostsAsync(query, pageSize: pageSize, page: pageNumber);
            var dtos = posts.Select(MapPostToDto).ToList();

            _logger.LogInformation("Search posts found: {Count} items for query '{Query}'", dtos.Count, query);
            return new PostFeedDto
            {
                Posts = dtos,
                Page = pageNumber,  // Keep as 1-based
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching posts with query '{Query}'", query);
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
            var (posts, totalCount) = await _postRepository.GetFeaturedPostsAsync(pageSize: pageSize, page: 1);
            var dtos = posts.Select(MapPostToDto).ToList();

            _logger.LogInformation("Trending posts retrieved: {Count} items", dtos.Count);
            return new PostFeedDto
            {
                Posts = dtos,
                Page = 1,  // 1-based page number
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trending posts");
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
            _logger.LogWarning("GetPostAnalyticsAsync called with empty post ID");
            return null!;
        }

        try
        {
            var analytics = new PostAnalyticsDto
            {
                PostId = postId,
                Views = 0,
                TotalReactions = 0,
                TotalComments = 0,
                ReactionsByType = new Dictionary<ReactionType, int>(),
                EngagementRate = 0.0
            };

            _logger.LogInformation("Post analytics retrieved for post {PostId}", postId);
            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving post analytics for {PostId}", postId);
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
            _logger.LogWarning("GetProfileActivityAsync called with empty profile ID");
            return new List<PostActivityDto>();
        }

        try
        {
            var activities = new List<PostActivityDto>();
            _logger.LogInformation("Profile activity retrieved for profile {ProfileId}: {Count} items", profileId, activities.Count);
            return activities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile activity for {ProfileId}", profileId);
            throw;
        }
    }

    /// <summary>
    /// Extracts the Keycloak ID from the current HTTP context
    /// </summary>
    /// <returns>The user's Keycloak ID, or null if not authenticated</returns>
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

            // Fallback: try to find "user_id" or "id" claims if "sub" is not available
            var userIdClaim = httpContext.User.FindFirst("user_id")?.Value 
                           ?? httpContext.User.FindFirst("id")?.Value 
                           ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim;
        }

        return null;
    }

    /// <summary>
    /// Maps a Post entity to PostDto
    /// </summary>
    private PostDto MapPostToDto(Post post)
    {
        var postDto = new PostDto
        {
            Id = post.Id,
            Content = post.Content,
            PostType = post.PostType,
            Visibility = post.Visibility,
            Language = post.Language,
            Tags = string.IsNullOrEmpty(post.Tags) ? new List<string>() : post.GetTags().ToList(),
            Location = post.Location != null ? new LocationDto
            {
                City = post.Location.City,
                State = post.Location.State,
                Country = post.Location.Country,
                Latitude = post.Location.Latitude,
                Longitude = post.Location.Longitude
            } : null,
            BusinessMetadata = post.BusinessMetadata,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            IsEdited = post.IsEdited,
            EditedAt = post.EditedAt,
            CommentCount = post.Comments?.Count(c => !c.IsDeleted) ?? 0,
            Profile = post.Profile != null ? new ProfileDto
            {
                Id = post.Profile.Id,
                DisplayName = post.Profile.DisplayName ?? string.Empty,
                Bio = post.Profile.Bio ?? string.Empty,
                Avatar = post.Profile.Avatar ?? string.Empty,
                UserId = post.Profile.UserId,
                ProfileTypeId = post.Profile.ProfileTypeId,
                VisibilityLevel = post.Profile.VisibilityLevel,
                IsActive = post.Profile.IsActive,
                CreatedAt = post.Profile.CreatedAt
            } : null!,
            Attachments = post.Attachments?.Where(a => !a.IsDeleted).Select(a => new PostAttachmentDto
            {
                Id = a.Id,
                AttachmentType = a.AttachmentType,
                FileId = a.FileId,
                FilePath = a.Url ?? string.Empty,
                OriginalFilename = a.OriginalFileName ?? string.Empty,
                MimeType = a.MimeType ?? string.Empty,
                FileSize = a.FileSizeBytes ?? 0,
                AltText = a.Description,
                DisplayOrder = a.DisplayOrder,
                CreatedAt = a.CreatedAt
            }).ToList() ?? new(),
            Comments = post.Comments?.Where(c => !c.IsDeleted).Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content ?? string.Empty,
                PostId = c.PostId,
                ParentCommentId = c.ParentCommentId,
                Language = c.Language ?? "en",
                Profile = c.Profile != null ? new ProfileDto
                {
                    Id = c.Profile.Id,
                    DisplayName = c.Profile.DisplayName ?? string.Empty,
                    Avatar = c.Profile.Avatar ?? string.Empty,
                    UserId = c.Profile.UserId,
                    ProfileTypeId = c.Profile.ProfileTypeId,
                    VisibilityLevel = c.Profile.VisibilityLevel,
                    IsActive = c.Profile.IsActive,
                    CreatedAt = c.Profile.CreatedAt
                } : null!,
                Replies = new List<CommentDto>(), // TODO: Map nested replies if needed
                ReplyCount = c.Replies?.Count(r => !r.IsDeleted) ?? 0,
                ReactionSummary = null, // TODO: Map reaction summary if needed
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                IsEdited = c.IsEdited,
                EditedAt = c.EditedAt,
                ThreadDepth = 0 // TODO: Calculate thread depth if needed
            }).ToList() ?? new(),
            ReactionSummary = null // TODO: Map reaction summary if needed
        };
        
        return postDto;
    }
}
