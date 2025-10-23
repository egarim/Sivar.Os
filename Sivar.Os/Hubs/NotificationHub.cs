using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;


namespace Sivar.Os.Hubs;

/// <summary>
/// SignalR hub for real-time notification delivery
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserIdFromContext();
        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to a group based on their user ID
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to NotificationHub with connection {ConnectionId}", 
                userId, Context.ConnectionId);
        }
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserIdFromContext();
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        }

        if (exception != null)
        {
            _logger.LogError(exception, "User disconnected with error");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client can call this to mark a notification as read
    /// </summary>
    /// <param name="notificationId">ID of the notification to mark as read</param>
    public async Task MarkNotificationAsRead(Guid notificationId)
    {
        var userId = GetUserIdFromContext();
        _logger.LogDebug("User {UserId} marking notification {NotificationId} as read via SignalR", 
            userId, notificationId);
        
        // The actual marking as read is handled by the NotificationService via the API
        // This is just for logging/tracking
        await Task.CompletedTask;
    }

    /// <summary>
    /// Get the user ID from the hub context
    /// </summary>
    private string? GetUserIdFromContext()
    {
        // Try to get from 'sub' claim first (standard JWT claim)
        var subClaim = Context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(subClaim))
        {
            return subClaim;
        }

        // Fallback to other possible claim names
        var keycloakIdClaim = Context.User?.FindFirst("keycloak_id")?.Value;
        if (!string.IsNullOrEmpty(keycloakIdClaim))
        {
            return keycloakIdClaim;
        }

        return null;
    }
}
