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
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reactionService.TogglePostReactionAsync(keycloakId, createReactionDto.PostId, createReactionDto.ReactionType);
            if (result == null)
            {
                return BadRequest("Failed to react to post. Please check the post ID and try again.");
            }
            
            // Create reaction notification if a new reaction was added
            if (result.Action == ReactionAction.Added && result.Reaction != null)
            {
                try
                {
                    await _notificationService.CreateReactionNotificationAsync(
                        createReactionDto.PostId, 
                        result.Reaction.Profile.UserId, 
                        result.ReactionType.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating reaction notification for post {PostId} by user {UserId}", 
                        createReactionDto.PostId, result.Reaction.Profile.UserId);
                    // Don't fail the reaction if notification creation fails
                }
            }
            
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid post reaction request");
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied - cannot react to this post");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reacting to post");
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
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reactionService.ToggleCommentReactionAsync(keycloakId, createReactionDto.CommentId, createReactionDto.ReactionType);
            if (result == null)
            {
                return BadRequest("Failed to react to comment. Please check the comment ID and try again.");
            }
            
            // Create reaction notification if a new reaction was added to a comment
            // Note: For now, we'll skip comment reaction notifications
            // TODO: Implement comment reaction notifications by extending the notification service
            if (result.Action == ReactionAction.Added && result.Reaction != null)
            {
                _logger.LogDebug("Comment reaction added for comment {CommentId} by user {UserId} - notification skipped (TODO)", 
                    createReactionDto.CommentId, result.Reaction.Profile.UserId);
            }
            
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid comment reaction request");
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied - cannot react to this comment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reacting to comment");
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
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (pageSize > 100)
                pageSize = 100; // Limit page size

            if (reactionType.HasValue)
            {
                var (reactions, totalCount) = await _reactionService.GetPostReactionsByTypeAsync(postId, reactionType.Value, keycloakId, page, pageSize);
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
                var summary = await _reactionService.GetPostReactionSummaryAsync(postId, keycloakId);
                return Ok(summary);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reactions for post {PostId}", postId);
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
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var analytics = await _reactionService.GetPostReactionAnalyticsAsync(postId, keycloakId);
            
            if (analytics == null)
                return NotFound("Post not found or access denied");

            return Ok(analytics);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied to post reaction analytics");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reaction analytics for post {PostId}", postId);
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
        return User.FindFirst("sub")?.Value ?? string.Empty;
    }
}