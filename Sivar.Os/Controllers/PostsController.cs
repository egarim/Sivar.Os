using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for post management in the activity stream
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<PostsController> _logger;

    public PostsController(IPostService postService, IRateLimitingService rateLimitingService, ILogger<PostsController> logger)
    {
        _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        _rateLimitingService = rateLimitingService ?? throw new ArgumentNullException(nameof(rateLimitingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new post in the activity stream
    /// </summary>
    /// <remarks>
    /// Creates a new post with the provided content. Rate limited to 5 posts per minute per user.
    /// 
    /// Sample request:
    ///
    ///     POST /api/posts
    ///     {
    ///         "content": "This is my first post!",
    ///         "visibility": "Public",
    ///         "allowComments": true,
    ///         "allowReactions": true
    ///     }
    ///
    /// </remarks>
    /// <param name="createPostDto">Post creation data including content and visibility settings</param>
    /// <returns>The newly created post with generated ID and metadata</returns>
    /// <response code="201">Post created successfully</response>
    /// <response code="400">Invalid post data provided</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="429">Rate limit exceeded (max 5 posts per minute)</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a new post",
        Description = "Creates a new post in the activity stream. Rate limited to 5 posts per minute per user.",
        Tags = new[] { "Posts" }
    )]
    [SwaggerResponse(201, "Post created successfully", typeof(PostDto))]
    [SwaggerResponse(400, "Invalid post data")]
    [SwaggerResponse(401, "User not authenticated")]
    [SwaggerResponse(429, "Rate limit exceeded")]
    public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostDto createPostDto)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            // Check rate limit before processing
            if (!await _rateLimitingService.CheckAndIncrementAsync(keycloakId, "post_creation"))
            {
                var remainingRequests = await _rateLimitingService.GetRemainingRequestsAsync(keycloakId, "post_creation");
                return StatusCode(429, new { 
                    message = "Too many requests. Please try again later.", 
                    remainingRequests = remainingRequests,
                    resetTime = DateTime.UtcNow.AddMinutes(1) // Based on 1 minute window
                });
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var post = await _postService.CreatePostAsync(keycloakId, createPostDto);
            if (post == null)
                return BadRequest("Failed to create post - user or profile not found");

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid post creation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific post by ID
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <returns>Post details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<PostDto>> GetPost(Guid id)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            var post = await _postService.GetPostByIdAsync(id, keycloakId);
            
            if (post == null)
                return NotFound("Post not found");

            return Ok(post);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied to this post");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates an existing post
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="updatePostDto">Post update data</param>
    /// <returns>Updated post</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<PostDto>> UpdatePost(Guid id, [FromBody] UpdatePostDto updatePostDto)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var post = await _postService.UpdatePostAsync(id, keycloakId, updatePostDto);
            
            if (post == null)
                return NotFound("Post not found");

            return Ok(post);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied - you can only edit your own posts");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid post update request for post {PostId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a post
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePost(Guid id)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var success = await _postService.DeletePostAsync(id, keycloakId);
            
            if (!success)
                return NotFound("Post not found");

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied - you can only delete your own posts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets posts for the activity stream feed
    /// </summary>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of posts per page</param>
    /// <param name="profileType">Filter by profile type name (e.g., "Business", "Personal")</param>
    /// <returns>Paginated list of posts</returns>
    [HttpGet("feed")]
    public async Task<ActionResult<PostFeedDto>> GetActivityStreamFeed(
        [FromQuery] int page = 0, 
        [FromQuery] int pageSize = 20,
        [FromQuery] string? profileType = null)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (pageSize > 100)
                pageSize = 100; // Limit page size

            var (posts, totalCount) = await _postService.GetActivityFeedAsync(keycloakId, page + 1, pageSize, profileType);
            var feed = new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
            
            return Ok(feed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity stream feed");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets posts by a specific profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of posts per page</param>
    /// <returns>Paginated list of posts by the profile</returns>
    [HttpGet("profile/{profileId}")]
    public async Task<ActionResult<PostFeedDto>> GetPostsByProfile(
        Guid profileId,
        [FromQuery] int page = 0, 
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (pageSize > 100)
                pageSize = 100; // Limit page size

            var (posts, totalCount) = await _postService.GetPostsByProfileAsync(profileId, keycloakId, page + 1, pageSize);
            var postFeed = new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
            
            return Ok(postFeed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts by profile {ProfileId}", profileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Searches posts by content
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of posts per page</param>
    /// <returns>Search results</returns>
    [HttpGet("search")]
    public async Task<ActionResult<PostFeedDto>> SearchPosts(
        [FromQuery] string query,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query cannot be empty");

            var keycloakId = GetKeycloakIdFromRequest();
            
            if (pageSize > 100)
                pageSize = 100; // Limit page size

            var (posts, totalCount) = await _postService.SearchPostsAsync(query, keycloakId, page + 1, pageSize);
            var results = new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching posts with query: {Query}", query);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets trending posts
    /// </summary>
    /// <param name="hours">Hours to look back for trending calculation</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of posts per page</param>
    /// <returns>Trending posts</returns>
    [HttpGet("trending")]
    public async Task<ActionResult<PostFeedDto>> GetTrendingPosts(
        [FromQuery] int hours = 24,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (pageSize > 100)
                pageSize = 100; // Limit page size

            if (hours > 168) // 1 week max
                hours = 168;

            // TODO: Implement trending posts in service
            var (posts, totalCount) = await _postService.GetActivityFeedAsync(keycloakId, page + 1, pageSize);
            var trending = new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
            
            return Ok(trending);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending posts");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets post analytics
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <returns>Post analytics data</returns>
    [HttpGet("{id}/analytics")]
    public async Task<ActionResult<PostAnalyticsDto>> GetPostAnalytics(Guid id)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            // TODO: Implement GetPostAnalyticsAsync in service
            var engagement = await _postService.GetPostEngagementAsync(id, keycloakId);
            
            if (engagement == null)
                return NotFound("Post not found or access denied");

            // Convert engagement to analytics format
            var analytics = new PostAnalyticsDto
            {
                PostId = id,
                Views = 0, // TODO: Implement views tracking
                TotalReactions = engagement.TotalReactions,
                ReactionsByType = engagement.ReactionsByType,
                TotalComments = engagement.TotalComments,
                EngagementRate = engagement.EngagementRate,
                PeakEngagementHour = 12, // Placeholder
                EngagementByLocation = new Dictionary<string, int>() // Placeholder
            };

            return Ok(analytics);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied to post analytics");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets recent post activity for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="hoursBack">Hours to look back</param>
    /// <returns>Recent post activities</returns>
    [HttpGet("activity/{profileId}")]
    public Task<ActionResult<IEnumerable<PostActivityDto>>> GetRecentPostActivity(
        Guid profileId,
        [FromQuery] int hoursBack = 24)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (hoursBack > 168) // 1 week max
                hoursBack = 168;

            // TODO: Implement GetRecentPostActivityAsync in service
            // For now, return empty list
            var activities = new List<PostActivityDto>();
            
            return Task.FromResult<ActionResult<IEnumerable<PostActivityDto>>>(Ok(activities));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent post activity for profile {ProfileId}", profileId);
            return Task.FromResult<ActionResult<IEnumerable<PostActivityDto>>>(StatusCode(500, "Internal server error"));
        }
    }

    /// <summary>
    /// Helper method to extract Keycloak ID from request
    /// </summary>
    private string GetKeycloakIdFromRequest()
    {
        // Check for mock authentication header (for integration tests)
        if (Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            return keycloakIdHeader.ToString();
        }

        // Check if user is authenticated via mock middleware
        if (User?.Identity?.IsAuthenticated == true)
        {
            var subClaim = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(subClaim))
            {
                return subClaim;
            }
        }

        // Only return fallback if we have mock auth header (X-Mock-Auth) indicating this is a test scenario
        if (Request.Headers.ContainsKey("X-Mock-Auth"))
        {
            return "mock-keycloak-user-id";
        }

        // No authentication found
        return null!;
    }
}