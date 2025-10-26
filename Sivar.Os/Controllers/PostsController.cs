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
            _logger.LogInformation("=== POST CREATE REQUEST RECEIVED ===");
            _logger.LogInformation($"[CreatePost] POST request received at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            
            // Extract Keycloak ID with detailed logging
            _logger.LogInformation("[CreatePost] Step 1: Extracting Keycloak ID from request");
            _logger.LogInformation($"[CreatePost] User.Identity?.IsAuthenticated = {User?.Identity?.IsAuthenticated}");
            _logger.LogInformation($"[CreatePost] Available claims count = {User?.Claims.Count()}");
            
            foreach (var claim in User?.Claims ?? Enumerable.Empty<System.Security.Claims.Claim>())
            {
                _logger.LogInformation($"[CreatePost] Claim: {claim.Type} = {claim.Value}");
            }
            
            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation($"[CreatePost] Extracted KeycloakId = '{keycloakId}'");
            
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[CreatePost] ❌ FAILED: KeycloakId is NULL or EMPTY - User not authenticated!");
                return Unauthorized(new { error = "User not authenticated", keycloakId = keycloakId });
            }

            _logger.LogInformation($"[CreatePost] ✓ KeycloakId validated: {keycloakId}");
            
            // Validate request body
            _logger.LogInformation("[CreatePost] Step 2: Validating request body");
            _logger.LogInformation($"[CreatePost] Content length = {createPostDto?.Content?.Length ?? 0}");
            _logger.LogInformation($"[CreatePost] Visibility = {createPostDto?.Visibility}");
            
            if (createPostDto == null)
            {
                _logger.LogWarning("[CreatePost] ❌ FAILED: CreatePostDto is null");
                return BadRequest(new { error = "Post data is required" });
            }

            // Check rate limit before processing
            _logger.LogInformation("[CreatePost] Step 3: Checking rate limit");
            if (!await _rateLimitingService.CheckAndIncrementAsync(keycloakId, "post_creation"))
            {
                _logger.LogWarning($"[CreatePost] ❌ FAILED: Rate limit exceeded for user {keycloakId}");
                var remainingRequests = await _rateLimitingService.GetRemainingRequestsAsync(keycloakId, "post_creation");
                return StatusCode(429, new { 
                    message = "Too many requests. Please try again later.", 
                    remainingRequests = remainingRequests,
                    resetTime = DateTime.UtcNow.AddMinutes(1)
                });
            }

            _logger.LogInformation($"[CreatePost] ✓ Rate limit check passed");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"[CreatePost] ❌ FAILED: ModelState is invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning($"[CreatePost] ModelState Error: {error.ErrorMessage}");
                }
                return BadRequest(ModelState);
            }

            // Create post via service
            _logger.LogInformation("[CreatePost] Step 4: Calling PostService.CreatePostAsync");
            _logger.LogInformation($"[CreatePost] Parameters: keycloakId='{keycloakId}', content='{createPostDto.Content?.Substring(0, Math.Min(50, createPostDto.Content?.Length ?? 0))}...'");
            
            var post = await _postService.CreatePostAsync(keycloakId, createPostDto);
            
            if (post == null)
            {
                _logger.LogError("[CreatePost] ❌ FAILED: PostService returned NULL - user or profile not found");
                return BadRequest(new { error = "Failed to create post - user or profile not found", keycloakId = keycloakId });
            }

            _logger.LogInformation($"[CreatePost] ✅ SUCCESS: Post created with ID = {post.Id}");
            _logger.LogInformation($"[CreatePost] Post content = '{post.Content?.Substring(0, Math.Min(50, post.Content?.Length ?? 0))}...'");
            _logger.LogInformation($"[CreatePost] Post profile = {post.Profile?.DisplayName}");
            _logger.LogInformation("=== POST CREATE REQUEST COMPLETED SUCCESSFULLY ===");

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "[CreatePost] ❌ ArgumentException: Invalid post creation request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreatePost] ❌ Exception: Error creating post - {ExceptionMessage}", ex.Message);
            _logger.LogError($"[CreatePost] Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
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
            _logger.LogInformation("[PostsController.GetActivityStreamFeed] API ENDPOINT CALLED - page={Page}, pageSize={PageSize}, profileType={ProfileType}", page, pageSize, profileType);
            
            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[PostsController.GetActivityStreamFeed] Keycloak ID extracted: {KeycloakId}", keycloakId ?? "NULL");
            
            if (pageSize > 100)
                pageSize = 100; // Limit page size

            _logger.LogInformation("[PostsController.GetActivityStreamFeed] Calling PostService.GetActivityFeedAsync with keycloakId={KeycloakId}", keycloakId);
            var (posts, totalCount) = await _postService.GetActivityFeedAsync(keycloakId, page + 1, pageSize, profileType);
            
            _logger.LogInformation("[PostsController.GetActivityStreamFeed] PostService returned {Count} posts (totalCount: {TotalCount})", posts.Count(), totalCount);
            
            var feed = new PostFeedDto
            {
                Posts = posts.ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
            
            _logger.LogInformation("[PostsController.GetActivityStreamFeed] Returning feed DTO with {Count} posts", feed.Posts.Count);
            return Ok(feed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PostsController.GetActivityStreamFeed] Error getting activity stream feed");
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
        _logger.LogInformation("[GetKeycloakIdFromRequest] Starting Keycloak ID extraction...");

        // Check for mock authentication header (for integration tests)
        if (Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            _logger.LogInformation($"[GetKeycloakIdFromRequest] ✓ Found X-Keycloak-Id header: {keycloakIdHeader}");
            return keycloakIdHeader.ToString();
        }

        _logger.LogInformation("[GetKeycloakIdFromRequest] No X-Keycloak-Id header found");

        // Check if user is authenticated via claims
        _logger.LogInformation($"[GetKeycloakIdFromRequest] User.Identity?.IsAuthenticated = {User?.Identity?.IsAuthenticated}");
        
        if (User?.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation($"[GetKeycloakIdFromRequest] User is authenticated. Total claims: {User.Claims.Count()}");
            
            // Log all available claims
            foreach (var claim in User.Claims)
            {
                _logger.LogInformation($"[GetKeycloakIdFromRequest]   Claim: {claim.Type} = {claim.Value}");
            }

            // Try "sub" claim first (OpenID Connect standard)
            var subClaim = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(subClaim))
            {
                _logger.LogInformation($"[GetKeycloakIdFromRequest] ✓ Found 'sub' claim: {subClaim}");
                return subClaim;
            }
            
            _logger.LogInformation("[GetKeycloakIdFromRequest] 'sub' claim not found or empty");

            // Fallback: try to find "user_id" or "id" claims if "sub" is not available
            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogInformation($"[GetKeycloakIdFromRequest] ✓ Found 'user_id' claim: {userIdClaim}");
                return userIdClaim;
            }
            
            _logger.LogInformation("[GetKeycloakIdFromRequest] 'user_id' claim not found");

            var idClaim = User.FindFirst("id")?.Value;
            if (!string.IsNullOrEmpty(idClaim))
            {
                _logger.LogInformation($"[GetKeycloakIdFromRequest] ✓ Found 'id' claim: {idClaim}");
                return idClaim;
            }
            
            _logger.LogInformation("[GetKeycloakIdFromRequest] 'id' claim not found");

            var nameIdentifierClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(nameIdentifierClaim))
            {
                _logger.LogInformation($"[GetKeycloakIdFromRequest] ✓ Found NameIdentifier claim: {nameIdentifierClaim}");
                return nameIdentifierClaim;
            }
            
            _logger.LogInformation("[GetKeycloakIdFromRequest] NameIdentifier claim not found");
        }
        else
        {
            _logger.LogWarning("[GetKeycloakIdFromRequest] User is NOT authenticated!");
        }

        // Only return fallback if we have mock auth header (X-Mock-Auth) indicating this is a test scenario
        if (Request.Headers.ContainsKey("X-Mock-Auth"))
        {
            _logger.LogInformation("[GetKeycloakIdFromRequest] ✓ Using mock auth header");
            return "mock-keycloak-user-id";
        }

        // No authentication found
        _logger.LogError("[GetKeycloakIdFromRequest] ❌ NO KEYCLOAK ID FOUND - returning null!");
        return null!;
    }
}