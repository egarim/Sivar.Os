using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for Activity entity operations
/// Provides specialized methods for activity stream functionality
/// </summary>
public interface IActivityRepository : IBaseRepository<Activity>
{
    /// <summary>
    /// Gets activities for a user's feed based on followed profiles
    /// </summary>
    Task<(IEnumerable<Activity> Activities, int TotalCount)> GetFeedActivitiesAsync(
        List<Guid> followedProfileIds,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets activities by a specific profile (actor)
    /// </summary>
    Task<(IEnumerable<Activity> Activities, int TotalCount)> GetByActorAsync(
        Guid actorId,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets activities on a specific object
    /// </summary>
    Task<(IEnumerable<Activity> Activities, int TotalCount)> GetByObjectAsync(
        string objectType,
        Guid objectId,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets activities by verb type
    /// </summary>
    Task<(IEnumerable<Activity> Activities, int TotalCount)> GetByVerbAsync(
        ActivityVerb verb,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets activities within a time range
    /// </summary>
    Task<(IEnumerable<Activity> Activities, int TotalCount)> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets trending activities based on engagement score
    /// </summary>
    Task<(IEnumerable<Activity> Activities, int TotalCount)> GetTrendingAsync(
        DateTime since,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets activities by visibility level
    /// </summary>
    Task<(IEnumerable<Activity> Activities, int TotalCount)> GetByVisibilityAsync(
        VisibilityLevel visibility,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Updates engagement score for an activity
    /// </summary>
    Task UpdateEngagementScoreAsync(Guid activityId, int engagementScore);

    /// <summary>
    /// Increments view count for an activity
    /// </summary>
    Task IncrementViewCountAsync(Guid activityId);

    /// <summary>
    /// Checks if an activity already exists for a specific actor-verb-object combination
    /// </summary>
    Task<bool> ExistsAsync(Guid actorId, ActivityVerb verb, string objectType, Guid objectId);

    /// <summary>
    /// Gets the most recent activity for a specific object
    /// </summary>
    Task<Activity?> GetLatestActivityForObjectAsync(string objectType, Guid objectId);
}
