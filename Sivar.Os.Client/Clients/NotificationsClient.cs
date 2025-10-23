

using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of notifications client
/// </summary>
public class NotificationsClient : BaseClient, INotificationsClient
{
    public NotificationsClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int pageSize = 50, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<NotificationDto>>($"api/notifications?pageSize={pageSize}&pageNumber={pageNumber}", cancellationToken);
    }

    public async Task<NotificationSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<NotificationSummaryDto>("api/notifications/summary", cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        // API returns the count directly as an integer
        return await GetAsync<int>("api/notifications/unread-count", cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        await PutAsync<object>($"api/notifications/{notificationId}/read", new { }, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        await PutAsync<object>("api/notifications/read-all", new { }, cancellationToken);
    }

    public async Task MarkTypeAsReadAsync(string type, CancellationToken cancellationToken = default)
    {
        await PutAsync<object>($"api/notifications/read-type/{type}", new { }, cancellationToken);
    }

    public async Task DeleteNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/notifications/{notificationId}", cancellationToken);
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<NotificationDto>("api/notifications", request, cancellationToken);
    }

    public async Task CreateBatchNotificationsAsync(IEnumerable<CreateNotificationDto> requests, CancellationToken cancellationToken = default)
    {
        await PostAsync<object>("api/notifications/batch", requests, cancellationToken);
    }

    public async Task CleanupNotificationsAsync(int daysOld = 30, CancellationToken cancellationToken = default)
    {
        await PostAsync<object>($"api/notifications/cleanup?daysOld={daysOld}", new { }, cancellationToken);
    }
}
