using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for comment management in the activity stream
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(ICommentService commentService, INotificationService notificationService, ILogger<CommentsController> logger)
    {
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new comment on a post
    /// </summary>
    /// <param name="createCommentDto">Comment creation data</param>
    /// <returns>Created comment</returns>
    [HttpPost]
    public async Task<ActionResult<CommentDto>> CreateComment([FromBody] CreateCommentDto createCommentDto)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var comment = await _commentService.CreateCommentAsync(keycloakId, createCommentDto);
            
            if (comment == null)
                return BadRequest("Failed to create comment - user, profile or post not found");

            // Create comment notification
            try
            {
                await _notificationService.CreateCommentNotificationAsync(
                    comment.PostId, 
                    comment.Profile.UserId, 
                    comment.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment notification for comment {CommentId} on post {PostId}", 
                    comment.Id, comment.PostId);
                // Don't fail the comment creation if notification creation fails
            }

            return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, comment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid comment creation request");
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied - cannot comment on this post");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a reply to an existing comment
    /// </summary>
    /// <param name="parentCommentId">Parent comment ID</param>
    /// <param name="createReplyDto">Reply creation data</param>
    /// <returns>Created reply</returns>
    [HttpPost("{parentCommentId}/reply")]
    public async Task<ActionResult<CommentDto>> CreateReply(Guid parentCommentId, [FromBody] CreateReplyDto createReplyDto)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var reply = await _commentService.CreateReplyAsync(keycloakId, parentCommentId, createReplyDto);
            if (reply == null)
            {
                return BadRequest("Failed to create reply. Please check the parent comment ID and try again.");
            }
            
            // Create comment notification for the reply
            try
            {
                await _notificationService.CreateCommentNotificationAsync(
                    reply.PostId, 
                    reply.Profile.UserId, 
                    reply.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment notification for reply {CommentId} on post {PostId}", 
                    reply.Id, reply.PostId);
                // Don't fail the reply creation if notification creation fails
            }
            
            return CreatedAtAction(nameof(GetComment), new { id = reply.Id }, reply);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid reply creation request");
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied - cannot reply to this comment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reply");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific comment by ID
    /// </summary>
    /// <param name="id">Comment ID</param>
    /// <returns>Comment details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<CommentDto>> GetComment(Guid id)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            var comment = await _commentService.GetCommentByIdAsync(id, keycloakId);
            
            if (comment == null)
                return NotFound("Comment not found");

            return Ok(comment);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied to this comment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment {CommentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates an existing comment
    /// </summary>
    /// <param name="id">Comment ID</param>
    /// <param name="updateCommentDto">Comment update data</param>
    /// <returns>Updated comment</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<CommentDto>> UpdateComment(Guid id, [FromBody] UpdateCommentDto updateCommentDto)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var comment = await _commentService.UpdateCommentAsync(id, keycloakId, updateCommentDto);
            
            if (comment == null)
                return NotFound("Comment not found");

            return Ok(comment);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied - you can only edit your own comments");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid comment update request for comment {CommentId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment {CommentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a comment
    /// </summary>
    /// <param name="id">Comment ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteComment(Guid id)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var success = await _commentService.DeleteCommentAsync(id, keycloakId);
            
            if (!success)
                return NotFound("Comment not found");

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("Access denied - you can only delete your own comments");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets comments for a specific post
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of comments per page</param>
    /// <returns>Paginated list of comments</returns>
    [HttpGet("post/{postId}")]
    public async Task<ActionResult<CommentThreadDto>> GetCommentsByPost(
        Guid postId,
        [FromQuery] int page = 0, 
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (pageSize > 100)
                pageSize = 100; // Limit page size

            var comments = await _commentService.GetCommentsByPostAsync(postId, keycloakId, page, pageSize);
            
            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for post {PostId}", postId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets replies to a specific comment
    /// </summary>
    /// <param name="commentId">Parent comment ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of replies per page</param>
    /// <returns>Paginated list of replies</returns>
    [HttpGet("{commentId}/replies")]
    public async Task<ActionResult<CommentThreadDto>> GetReplies(
        Guid commentId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (pageSize > 100)
                pageSize = 100; // Limit page size

            var (replies, totalCount) = await _commentService.GetRepliesByCommentAsync(commentId, keycloakId, page, pageSize);
            
            return Ok(replies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting replies for comment {CommentId}", commentId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets comment thread statistics
    /// </summary>
    /// <param name="commentId">Root comment ID</param>
    /// <returns>Comment thread statistics</returns>
    [HttpGet("{commentId}/thread")]
    public async Task<ActionResult<CommentThreadStatsDto>> GetCommentThread(Guid commentId)
    {
        try
        {
            var threadStats = await _commentService.GetCommentThreadStatsAsync(commentId);
            
            if (threadStats == null)
                return NotFound("Comment not found");

            return Ok(threadStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment thread stats for {CommentId}", commentId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets comments by a specific profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of comments per page</param>
    /// <returns>Paginated list of comments by the profile</returns>
    [HttpGet("profile/{profileId}")]
    public async Task<ActionResult<CommentThreadDto>> GetCommentsByProfile(
        Guid profileId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (pageSize > 100)
                pageSize = 100; // Limit page size

            var comments = await _commentService.GetCommentsByProfileAsync(profileId, keycloakId, page, pageSize);
            
            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments by profile {ProfileId}", profileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets recent comment activity for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="hoursBack">Hours to look back</param>
    /// <returns>Recent comment activities</returns>
    [HttpGet("activity/{profileId}")]
    public async Task<ActionResult<IEnumerable<CommentActivityDto>>> GetRecentCommentActivity(
        Guid profileId,
        [FromQuery] int hoursBack = 24)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (hoursBack > 168) // 1 week max
                hoursBack = 168;

            var activities = await _commentService.GetRecentCommentActivityAsync(profileId, hoursBack);
            
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent comment activity for profile {ProfileId}", profileId);
            return StatusCode(500, "Internal server error");
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