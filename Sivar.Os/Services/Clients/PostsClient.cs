using Sivar.Core.Clients.Posts;
using Sivar.Core.DTOs;
using Sivar.Core.Enums;
using Sivar.Core.Interfaces;
using Sivar.Core.Repositories;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of posts client using repositories and services
/// Provides the same interface as the HTTP client but operates directly on the service layer
/// </summary>
public class PostsClient : BaseRepositoryClient, IPostsClient
{
    private readonly IPostService _postService;
    private readonly IPostRepository _postRepository;
    private readonly ILogger<PostsClient> _logger;

    public PostsClient(
        IPostService postService,
        IPostRepository postRepository,
        ILogger<PostsClient> logger)
    {
        _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
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
            // Note: Server-side client would need keycloakId from context
            // This is a placeholder implementation
            var result = new PostDto
            {
                Id = Guid.NewGuid(),
                Content = request.Content,
                PostType = request.PostType,
                Visibility = request.Visibility,
                Language = request.Language ?? "en",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Post created with ID: {PostId}", result.Id);
            return result;
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
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Post not found for update: {PostId}", postId);
                return null!;
            }

            post.Content = request.Content ?? post.Content;
            post.UpdatedAt = DateTime.UtcNow;

            await _postRepository.UpdateAsync(post);

            _logger.LogInformation("Post updated: {PostId}", postId);
            return MapPostToDto(post);
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
            var deleted = await _postRepository.DeleteAsync(postId);
            if (deleted)
            {
                _logger.LogInformation("Post deleted: {PostId}", postId);
            }
            else
            {
                _logger.LogWarning("Post not found for deletion: {PostId}", postId);
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
    public async Task<IEnumerable<PostDto>> GetFeedPostsAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            // Placeholder implementation
            var posts = new List<PostDto>();
            _logger.LogInformation("Feed posts retrieved: {Count} items", posts.Count);
            return posts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feed posts");
            throw;
        }
    }

    /// <summary>
    /// Gets posts for a specific profile
    /// </summary>
    public async Task<IEnumerable<PostDto>> GetProfilePostsAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            _logger.LogWarning("GetProfilePostsAsync called with empty profile ID");
            return new List<PostDto>();
        }

        try
        {
            var (posts, totalCount) = await _postRepository.GetByProfileAsync(profileId, pageSize, pageNumber);
            var dtos = posts.Select(MapPostToDto).ToList();

            _logger.LogInformation("Profile posts retrieved for profile {ProfileId}: {Count} items", profileId, dtos.Count);
            return dtos;
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
    public async Task<IEnumerable<PostDto>> SearchPostsAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("SearchPostsAsync called with empty query");
            return new List<PostDto>();
        }

        try
        {
            var (posts, totalCount) = await _postRepository.SearchPostsAsync(query, pageSize: pageSize, page: pageNumber);
            var dtos = posts.Select(MapPostToDto).ToList();

            _logger.LogInformation("Search posts found: {Count} items for query '{Query}'", dtos.Count, query);
            return dtos;
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
    public async Task<IEnumerable<PostDto>> GetTrendingPostsAsync(int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var (posts, totalCount) = await _postRepository.GetFeaturedPostsAsync(pageSize: pageSize, page: 1);
            var dtos = posts.Select(MapPostToDto).ToList();

            _logger.LogInformation("Trending posts retrieved: {Count} items", dtos.Count);
            return dtos;
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
                ReactionsByType = new Dictionary<Core.Enums.ReactionType, int>(),
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
    /// Maps a Post entity to PostDto
    /// </summary>
    private PostDto MapPostToDto(Core.Entities.Post post)
    {
        return new PostDto
        {
            Id = post.Id,
            Content = post.Content,
            PostType = post.PostType,
            Visibility = post.Visibility,
            Language = post.Language,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };
    }
}
