using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client interface for Activity Stream operations
/// </summary>
public interface IActivitiesClient
{
    /// <summary>
    /// Gets the activity feed for the authenticated user
    /// Shows activities from profiles the user follows
    /// </summary>
    Task<ActivityFeedDto> GetFeedActivitiesAsync(
        int pageSize = 20,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities for a specific profile
    /// Shows what a profile has done (their activity history)
    /// </summary>
    Task<ActivityFeedDto> GetProfileActivitiesAsync(
        Guid profileId,
        int pageSize = 20,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities on a specific object (e.g., all activities on a post)
    /// </summary>
    Task<ActivityFeedDto> GetObjectActivitiesAsync(
        string objectType,
        Guid objectId,
        int pageSize = 20,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trending activities based on engagement score
    /// </summary>
    Task<ActivityFeedDto> GetTrendingActivitiesAsync(
        int hoursBack = 24,
        int pageSize = 20,
        int pageNumber = 1,
        CancellationToken cancellationToken = default);
}
