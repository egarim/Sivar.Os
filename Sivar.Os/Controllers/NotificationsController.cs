using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for user notifications
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get notifications for the current user
    /// </summary>
    /// <param name="unreadOnly">Only return unread notifications</param>
    /// <param name="type">Filter by notification type</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="priority">Filter by priority level</param>
    /// <returns>List of notifications</returns>
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications(
        [FromQuery] bool? unreadOnly = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] NotificationPriority? priority = null)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var queryParams = new NotificationQueryDto
            {
                UnreadOnly = unreadOnly,
                Type = type,
                Page = page,
                PageSize = pageSize,
                Priority = priority
            };

            var notifications = await _notificationService.GetUserNotificationsAsync(keycloakId, queryParams);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user");
            return StatusCode(500, "An error occurred while retrieving notifications");
        }
    }

    /// <summary>
    /// Get notification summary for the current user
    /// </summary>
    /// <returns>Notification summary with counts and breakdown</returns>
    [HttpGet("summary")]
    public async Task<ActionResult<NotificationSummaryDto>> GetNotificationSummary()
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var summary = await _notificationService.GetNotificationSummaryAsync(keycloakId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification summary for user");
            return StatusCode(500, "An error occurred while retrieving notification summary");
        }
    }

    /// <summary>
    /// Get count of unread notifications for the current user
    /// </summary>
    /// <returns>Count of unread notifications</returns>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var count = await _notificationService.GetUnreadCountAsync(keycloakId);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user");
            return StatusCode(500, "An error occurred while retrieving unread count");
        }
    }

    /// <summary>
    /// Mark a specific notification as read
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <returns>Success status</returns>
    [HttpPut("{id}/read")]
    public async Task<ActionResult> MarkAsRead(Guid id)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var result = await _notificationService.MarkAsReadAsync(id, keycloakId);
            
            if (!result)
            {
                return NotFound("Notification not found or not owned by user");
            }

            return Ok(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return StatusCode(500, "An error occurred while marking notification as read");
        }
    }

    /// <summary>
    /// Mark all notifications as read for the current user
    /// </summary>
    /// <returns>Number of notifications marked as read</returns>
    [HttpPut("read-all")]
    public async Task<ActionResult<int>> MarkAllAsRead()
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var count = await _notificationService.MarkAllAsReadAsync(keycloakId);
            return Ok(new { message = $"Marked {count} notifications as read", count = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, "An error occurred while marking notifications as read");
        }
    }

    /// <summary>
    /// Mark all notifications of a specific type as read
    /// </summary>
    /// <param name="type">Notification type to mark as read</param>
    /// <returns>Number of notifications marked as read</returns>
    [HttpPut("read-type/{type}")]
    public async Task<ActionResult<int>> MarkTypeAsRead([Required] string type)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            if (string.IsNullOrWhiteSpace(type))
                return BadRequest("Notification type is required");

            var count = await _notificationService.MarkTypeAsReadAsync(keycloakId, type);
            return Ok(new { message = $"Marked {count} notifications of type '{type}' as read", count = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notifications of type {Type} as read", type);
            return StatusCode(500, "An error occurred while marking notifications as read");
        }
    }

    /// <summary>
    /// Delete a specific notification (soft delete)
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNotification(Guid id)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var result = await _notificationService.DeleteNotificationAsync(id, keycloakId);
            
            if (!result)
            {
                return NotFound("Notification not found or not owned by user");
            }

            return Ok(new { message = "Notification deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
            return StatusCode(500, "An error occurred while deleting notification");
        }
    }

    /// <summary>
    /// Create a notification (admin/system use only)
    /// </summary>
    /// <param name="createNotificationDto">Notification creation data</param>
    /// <returns>Created notification</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")] // Restrict to admin users
    public async Task<ActionResult<NotificationDto>> CreateNotification([FromBody] CreateNotificationDto createNotificationDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var notification = await _notificationService.CreateNotificationAsync(createNotificationDto);
            
            if (notification == null)
            {
                return BadRequest("Failed to create notification");
            }

            return CreatedAtAction(nameof(GetNotifications), new { }, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            return StatusCode(500, "An error occurred while creating notification");
        }
    }

    /// <summary>
    /// Batch create notifications (admin/system use only)
    /// </summary>
    /// <param name="createNotificationDtos">List of notifications to create</param>
    /// <returns>List of created notifications</returns>
    [HttpPost("batch")]
    [Authorize(Roles = "Admin")] // Restrict to admin users
    public async Task<ActionResult<List<NotificationDto>>> CreateBatchNotifications([FromBody] List<CreateNotificationDto> createNotificationDtos)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (createNotificationDtos.Count > 100)
                return BadRequest("Cannot create more than 100 notifications at once");

            var notifications = await _notificationService.CreateBatchNotificationsAsync(createNotificationDtos);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch notifications");
            return StatusCode(500, "An error occurred while creating notifications");
        }
    }

    /// <summary>
    /// Clean up old notifications (admin only)
    /// </summary>
    /// <param name="olderThanDays">Delete notifications older than this many days</param>
    /// <param name="keepUnread">If true, don't delete unread notifications</param>
    /// <returns>Number of notifications cleaned up</returns>
    [HttpPost("cleanup")]
    [Authorize(Roles = "Admin")] // Restrict to admin users
    public async Task<ActionResult<int>> CleanupOldNotifications(
        [FromQuery] int olderThanDays = 30,
        [FromQuery] bool keepUnread = true)
    {
        try
        {
            if (olderThanDays < 1)
                return BadRequest("olderThanDays must be at least 1");

            var count = await _notificationService.CleanupOldNotificationsAsync(olderThanDays, keepUnread);
            return Ok(new { message = $"Cleaned up {count} old notifications", count = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old notifications");
            return StatusCode(500, "An error occurred while cleaning up notifications");
        }
    }

    /// <summary>
    /// Get Keycloak ID from the 'sub' claim (OpenID Connect standard)
    /// </summary>
    /// <returns>Keycloak ID or null if not found</returns>
    private string? GetKeycloakIdFromRequest()
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
        return null;
    }
}