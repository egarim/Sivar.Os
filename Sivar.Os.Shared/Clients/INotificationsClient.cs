using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client for notification operations
/// </summary>
public interface INotificationsClient
{
    // Query operations
    Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<NotificationSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    // Mark as read
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
    Task MarkTypeAsReadAsync(string type, CancellationToken cancellationToken = default);

    // Delete operations
    Task DeleteNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);

    // Admin operations
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto request, CancellationToken cancellationToken = default);
    Task CreateBatchNotificationsAsync(IEnumerable<CreateNotificationDto> requests, CancellationToken cancellationToken = default);
    Task CleanupNotificationsAsync(int daysOld = 30, CancellationToken cancellationToken = default);
}
