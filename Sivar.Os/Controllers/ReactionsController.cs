using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for reaction management in the activity stream
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReactionsController : ControllerBase
{
    private readonly IReactionService _reactionService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReactionsController> _logger;

    public ReactionsController(IReactionService reactionService, INotificationService notificationService, ILogger<ReactionsController> logger)
    {
        _reactionService = reactionService ?? throw new ArgumentNullException(nameof(reactionService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Adds or updates a reaction to a post
    /// </summary>
    /// <param name="createReactionDto">Reaction data</param>
    /// <returns>Created or updated reaction</returns>
    [HttpPost("post")]
    public async Task<ActionResult<ReactionDto>> ReactToPost([FromBody] CreatePostReactionDto createReactionDto)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ReactionsController.ReactToPost] START - RequestId={RequestId}, PostId={PostId}, ReactionType={ReactionType}", 
            requestId, createReactionDto?.PostId, createReactionDto?.ReactionType);

        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ReactionsController.ReactToPost] KeycloakId extracted: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ReactionsController.ReactToPost] UNAUTHORIZED - No KeycloakId found, RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("[ReactionsController.ReactToPost] INVALID_MODEL - RequestId={RequestId}, Errors={Errors}", 
                    requestId, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("[ReactionsController.ReactToPost] Calling reaction service - RequestId={RequestId}", requestId);
            var result = await _reactionService.TogglePostReactionAsync(keycloakId, createReactionDto.PostId, createReactionDto.ReactionType);
            
            if (result == null)
            {
                _logger.LogWarning("[ReactionsController.ReactToPost] SERVICE_RETURNED_NULL - PostId={PostId}, RequestId={RequestId}", 
                    createReactionDto.PostId, requestId);
                return BadRequest("Failed to react to post. Please check the post ID and try again.");
            }
            
            _logger.LogInformation("[ReactionsController.ReactToPost] Reaction toggled - Action={Action}, ReactionType={ReactionType}, RequestId={RequestId}", 
                result.Action, result.ReactionType, requestId);
            
            // Create reaction notification if a new reaction was added
            if (result.Action == ReactionAction.Added && result.Reaction != null)
            {
                try
                {
                    _logger.LogInformation("[ReactionsController.ReactToPost] Creating notification - PostId={PostId}, UserId={UserId}, RequestId={RequestId}", 
                        createReactionDto.PostId, result.Reaction.Profile.UserId, requestId);
                    
                    await _notificationService.CreateReactionNotificationAsync(
                        createReactionDto.PostId, 
                        result.Reaction.Profile.UserId, 
                        result.ReactionType.ToString());
                    
                    _logger.LogInformation("[ReactionsController.ReactToPost] Notification created successfully - RequestId={RequestId}", requestId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ReactionsController.ReactToPost] NOTIFICATION_ERROR - PostId={PostId}, UserId={UserId}, RequestId={RequestId}", 
                        createReactionDto.PostId, result.Reaction.Profile.UserId, requestId);
                    // Don't fail the reaction if notification creation fails
                }
            }
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ReactionsController.ReactToPost] SUCCESS - Action={Action}, RequestId={RequestId}, Duration={Duration}ms", 
                result.Action, requestId, elapsed);
            
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning(ex, "[ReactionsController.ReactToPost] ARGUMENT_ERROR - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning(ex, "[ReactionsController.ReactToPost] ACCESS_DENIED - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return Forbid("Access denied - cannot react to this post");
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ReactionsController.ReactToPost] ERROR - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Adds or updates a reaction to a comment
    /// </summary>
    /// <param name="createReactionDto">Reaction data</param>
    /// <returns>Created or updated reaction</returns>
    [HttpPost("comment")]
    public async Task<ActionResult<ReactionDto>> ReactToComment([FromBody] CreateCommentReactionDto createReactionDto)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ReactionsController.ReactToComment] START - RequestId={RequestId}, CommentId={CommentId}, ReactionType={ReactionType}", 
            requestId, createReactionDto?.CommentId, createReactionDto?.ReactionType);

        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ReactionsController.ReactToComment] KeycloakId extracted: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ReactionsController.ReactToComment] UNAUTHORIZED - No KeycloakId found, RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("[ReactionsController.ReactToComment] INVALID_MODEL - RequestId={RequestId}, Errors={Errors}", 
                    requestId, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("[ReactionsController.ReactToComment] Calling reaction service - RequestId={RequestId}", requestId);
            var result = await _reactionService.ToggleCommentReactionAsync(keycloakId, createReactionDto.CommentId, createReactionDto.ReactionType);
            
            if (result == null)
            {
                _logger.LogWarning("[ReactionsController.ReactToComment] SERVICE_RETURNED_NULL - CommentId={CommentId}, RequestId={RequestId}", 
                    createReactionDto.CommentId, requestId);
                return BadRequest("Failed to react to comment. Please check the comment ID and try again.");
            }
            
            _logger.LogInformation("[ReactionsController.ReactToComment] Reaction toggled - Action={Action}, ReactionType={ReactionType}, RequestId={RequestId}", 
                result.Action, result.ReactionType, requestId);
            
            // Create reaction notification if a new reaction was added to a comment
            // Note: For now, we'll skip comment reaction notifications
            // TODO: Implement comment reaction notifications by extending the notification service
            if (result.Action == ReactionAction.Added && result.Reaction != null)
            {
                _logger.LogDebug("[ReactionsController.ReactToComment] Comment reaction added - CommentId={CommentId}, UserId={UserId}, notification skipped (TODO), RequestId={RequestId}", 
                    createReactionDto.CommentId, result.Reaction.Profile.UserId, requestId);
            }
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ReactionsController.ReactToComment] SUCCESS - Action={Action}, RequestId={RequestId}, Duration={Duration}ms", 
                result.Action, requestId, elapsed);
            
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning(ex, "[ReactionsController.ReactToComment] ARGUMENT_ERROR - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning(ex, "[ReactionsController.ReactToComment] ACCESS_DENIED - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return Forbid("Access denied - cannot react to this comment");
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ReactionsController.ReactToComment] ERROR - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Removes a reaction from a post
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("post/{postId}")]
    public async Task<ActionResult> RemovePostReaction(Guid postId)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            // Note: Reaction removal is handled through toggle - if user already has this reaction, it will be removed
            // For now, return method not implemented
            return StatusCode(501, "Reaction removal is handled through toggle endpoint");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing post reaction for post {PostId}", postId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Removes a reaction from a comment
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("comment/{commentId}")]
    public async Task<ActionResult> RemoveCommentReaction(Guid commentId)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            // Note: Reaction removal is handled through toggle - if user already has this reaction, it will be removed
            // For now, return method not implemented
            return StatusCode(501, "Reaction removal is handled through toggle endpoint");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing comment reaction for comment {CommentId}", commentId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific reaction by ID
    /// </summary>
    /// <param name="id">Reaction ID</param>
    /// <returns>Reaction details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ReactionDto>> GetReaction(Guid id)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            // GetReactionByIdAsync method is not available in the interface
            return StatusCode(501, "Get reaction by ID is not implemented");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied to this reaction");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reaction {ReactionId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets reactions for a specific post
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <param name="reactionType">Optional filter by reaction type</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of reactions per page</param>
    /// <returns>Paginated list of reactions</returns>
    [HttpGet("post/{postId}")]
    public async Task<ActionResult<ReactionListDto>> GetPostReactions(
        Guid postId,
        [FromQuery] ReactionType? reactionType = null,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("[ReactionsController.GetPostReactions] START - PostId={PostId}, ReactionType={ReactionType}, Page={Page}, PageSize={PageSize}", 
            postId, reactionType?.ToString() ?? "ALL", page, pageSize);

        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ReactionsController.GetPostReactions] KeycloakId: {KeycloakId}", keycloakId ?? "ANONYMOUS");
            
            if (pageSize > 100)
            {
                _logger.LogInformation("[ReactionsController.GetPostReactions] PageSize capped from {Original} to 100", pageSize);
                pageSize = 100; // Limit page size
            }

            if (reactionType.HasValue)
            {
                _logger.LogInformation("[ReactionsController.GetPostReactions] Fetching reactions by type - Type={Type}", reactionType.Value);
                var (reactions, totalCount) = await _reactionService.GetPostReactionsByTypeAsync(postId, reactionType.Value, keycloakId, page, pageSize);
                
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("[ReactionsController.GetPostReactions] SUCCESS - PostId={PostId}, TotalCount={TotalCount}, Duration={Duration}ms", 
                    postId, totalCount, elapsed);
                
                return Ok(new ReactionListDto 
                { 
                    Reactions = reactions.ToList(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            else
            {
                _logger.LogInformation("[ReactionsController.GetPostReactions] Fetching reaction summary - PostId={PostId}", postId);
                // Get summary if no specific type requested
                var summary = await _reactionService.GetPostReactionSummaryAsync(postId, keycloakId);
                
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("[ReactionsController.GetPostReactions] SUCCESS (Summary) - PostId={PostId}, Duration={Duration}ms", 
                    postId, elapsed);
                
                return Ok(summary);
            }
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ReactionsController.GetPostReactions] ERROR - PostId={PostId}, Duration={Duration}ms", 
                postId, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets reactions for a specific comment
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="reactionType">Optional filter by reaction type</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of reactions per page</param>
    /// <returns>Paginated list of reactions</returns>
    [HttpGet("comment/{commentId}")]
    public async Task<ActionResult<ReactionListDto>> GetCommentReactions(
        Guid commentId,
        [FromQuery] ReactionType? reactionType = null,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (pageSize > 100)
                pageSize = 100; // Limit page size

            if (reactionType.HasValue)
            {
                var (reactions, totalCount) = await _reactionService.GetCommentReactionsByTypeAsync(commentId, reactionType.Value, keycloakId, page, pageSize);
                return Ok(new ReactionListDto 
                { 
                    Reactions = reactions.ToList(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            else
            {
                // Get summary if no specific type requested
                var summary = await _reactionService.GetCommentReactionSummaryAsync(commentId, keycloakId);
                return Ok(summary);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reactions for comment {CommentId}", commentId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets reactions by a specific profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of reactions per page</param>
    /// <returns>Paginated list of reactions by the profile</returns>
    [HttpGet("profile/{profileId}")]
    public async Task<ActionResult<ReactionListDto>> GetReactionsByProfile(
        Guid profileId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (pageSize > 100)
                pageSize = 100; // Limit page size

            var (reactions, totalCount) = await _reactionService.GetReactionsByProfileAsync(profileId, keycloakId, null, page, pageSize);
            
            return Ok(reactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reactions by profile {ProfileId}", profileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets reaction analytics for a post
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <returns>Reaction analytics data</returns>
    [HttpGet("analytics/post/{postId}")]
    public async Task<ActionResult<PostReactionAnalyticsDto>> GetPostReactionAnalytics(Guid postId)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("[ReactionsController.GetPostReactionAnalytics] START - PostId={PostId}", postId);

        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ReactionsController.GetPostReactionAnalytics] KeycloakId: {KeycloakId}", keycloakId ?? "NULL");

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ReactionsController.GetPostReactionAnalytics] UNAUTHORIZED - PostId={PostId}", postId);
                return Unauthorized("User not authenticated");
            }

            var analytics = await _reactionService.GetPostReactionAnalyticsAsync(postId, keycloakId);
            
            if (analytics == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ReactionsController.GetPostReactionAnalytics] NOT_FOUND - PostId={PostId}, Duration={Duration}ms", 
                    postId, elapsed);
                return NotFound("Post not found or access denied");
            }

            var successElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ReactionsController.GetPostReactionAnalytics] SUCCESS - PostId={PostId}, Duration={Duration}ms", 
                postId, successElapsed);

            return Ok(analytics);
        }
        catch (UnauthorizedAccessException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning(ex, "[ReactionsController.GetPostReactionAnalytics] ACCESS_DENIED - PostId={PostId}, Duration={Duration}ms", 
                postId, elapsed);
            return Forbid("Access denied to post reaction analytics");
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ReactionsController.GetPostReactionAnalytics] ERROR - PostId={PostId}, Duration={Duration}ms", 
                postId, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets profile engagement statistics
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="daysBack">Days to look back for analysis</param>
    /// <returns>Profile engagement data</returns>
    [HttpGet("engagement/{profileId}")]
    public async Task<ActionResult<ProfileEngagementDto>> GetProfileEngagement(
        Guid profileId,
        [FromQuery] int daysBack = 30)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (daysBack > 365) // 1 year max
                daysBack = 365;

            var engagement = await _reactionService.GetProfileReactionEngagementAsync(profileId, keycloakId, daysBack);
            
            if (engagement == null)
                return NotFound("Profile not found");

            return Ok(engagement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting engagement for profile {ProfileId}", profileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets recent reaction activity for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="hoursBack">Hours to look back</param>
    /// <returns>Recent reaction activities</returns>
    [HttpGet("activity/{profileId}")]
    public async Task<ActionResult<IEnumerable<ReactionActivityDto>>> GetRecentReactionActivity(
        Guid profileId,
        [FromQuery] int hoursBack = 24)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (hoursBack > 168) // 1 week max
                hoursBack = 168;

            var activities = await _reactionService.GetRecentReactionActivityAsync(profileId, hoursBack);
            
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent reaction activity for profile {ProfileId}", profileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Helper method to extract Keycloak ID from the 'sub' claim (OpenID Connect standard)
    /// </summary>
    private string GetKeycloakIdFromRequest()
    {
        // Check for mock authentication header (for integration tests)
        if (Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            return keycloakIdHeader.ToString();
        }

        // Check if user is authenticated via claims
        if (User?.Identity?.IsAuthenticated == true)
        {
            var subClaim = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(subClaim))
            {
                return subClaim;
            }

            // Fallback: try to find "user_id" or "id" claims if "sub" is not available
            var userIdClaim = User.FindFirst("user_id")?.Value 
                           ?? User.FindFirst("id")?.Value 
                           ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                return userIdClaim;
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