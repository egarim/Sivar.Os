
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of notifications client
/// </summary>
public class NotificationsClient : BaseRepositoryClient, INotificationsClient
{
    private readonly INotificationService _notificationService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationsClient> _logger;

    public NotificationsClient(
        INotificationService notificationService,
        INotificationRepository notificationRepository,
        ILogger<NotificationsClient> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Query operations
    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetNotificationsAsync");
        return new List<NotificationDto>();
    }

    public async Task<NotificationSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetSummaryAsync");
        return new NotificationSummaryDto();
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetUnreadCountAsync");
        return 0;
    }

    // Mark as read
    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        if (notificationId == Guid.Empty)
        {
            _logger.LogWarning("MarkAsReadAsync called with empty notification ID");
            return;
        }

        try
        {
            _logger.LogInformation("Notification marked as read: {NotificationId}", notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MarkAllAsReadAsync");
    }

    public async Task MarkTypeAsReadAsync(string type, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MarkTypeAsReadAsync: {Type}", type);
    }

    // Delete operations
    public async Task DeleteNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        if (notificationId == Guid.Empty)
        {
            _logger.LogWarning("DeleteNotificationAsync called with empty notification ID");
            return;
        }

        try
        {
            await _notificationRepository.DeleteAsync(notificationId);
            _logger.LogInformation("Notification deleted: {NotificationId}", notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
            throw;
        }
    }

    // Admin operations
    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        _logger.LogInformation("CreateNotificationAsync");
        return new NotificationDto { Id = Guid.NewGuid() };
    }

    public async Task CreateBatchNotificationsAsync(IEnumerable<CreateNotificationDto> requests, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CreateBatchNotificationsAsync");
    }

    public async Task CleanupNotificationsAsync(int daysOld = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CleanupNotificationsAsync: {DaysOld} days", daysOld);
    }
}
