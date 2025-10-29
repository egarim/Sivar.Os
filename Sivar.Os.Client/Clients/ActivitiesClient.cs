using System.Net.Http.Json;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Client-side HTTP implementation of Activities Client for Blazor WebAssembly
/// </summary>
public class ActivitiesClient : IActivitiesClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ActivitiesClient> _logger;

    public ActivitiesClient(HttpClient httpClient, ILogger<ActivitiesClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ActivityFeedDto> GetFeedActivitiesAsync(
        int pageSize = 20,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[ActivitiesClient] Getting activity feed - Page: {Page}, Size: {PageSize}", pageNumber, pageSize);

            var response = await _httpClient.GetAsync(
                $"/api/activities/feed?pageSize={pageSize}&pageNumber={pageNumber}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[ActivitiesClient] Failed to get activity feed. Status: {Status}", response.StatusCode);
                return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
            }

            var feed = await response.Content.ReadFromJsonAsync<ActivityFeedDto>(cancellationToken: cancellationToken);
            
            _logger.LogInformation("[ActivitiesClient] Successfully loaded {Count} activities", feed?.Activities?.Count ?? 0);
            
            return feed ?? new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ActivitiesClient] Error getting activity feed");
            return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
        }
    }

    public async Task<ActivityFeedDto> GetProfileActivitiesAsync(
        Guid profileId,
        int pageSize = 20,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/activities/profile/{profileId}?pageSize={pageSize}&pageNumber={pageNumber}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[ActivitiesClient] Failed to get profile activities. Status: {Status}", response.StatusCode);
                return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
            }

            var feed = await response.Content.ReadFromJsonAsync<ActivityFeedDto>(cancellationToken: cancellationToken);
            return feed ?? new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ActivitiesClient] Error getting profile activities for {ProfileId}", profileId);
            return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
        }
    }

    public async Task<ActivityFeedDto> GetObjectActivitiesAsync(
        string objectType,
        Guid objectId,
        int pageSize = 20,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/activities/object/{objectType}/{objectId}?pageSize={pageSize}&pageNumber={pageNumber}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[ActivitiesClient] Failed to get object activities. Status: {Status}", response.StatusCode);
                return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
            }

            var feed = await response.Content.ReadFromJsonAsync<ActivityFeedDto>(cancellationToken: cancellationToken);
            return feed ?? new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ActivitiesClient] Error getting object activities for {ObjectType}/{ObjectId}", objectType, objectId);
            return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
        }
    }

    public async Task<ActivityFeedDto> GetTrendingActivitiesAsync(
        int hoursBack = 24,
        int pageSize = 20,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/activities/trending?hoursBack={hoursBack}&pageSize={pageSize}&pageNumber={pageNumber}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[ActivitiesClient] Failed to get trending activities. Status: {Status}", response.StatusCode);
                return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
            }

            var feed = await response.Content.ReadFromJsonAsync<ActivityFeedDto>(cancellationToken: cancellationToken);
            return feed ?? new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ActivitiesClient] Error getting trending activities");
            return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
        }
    }
}
