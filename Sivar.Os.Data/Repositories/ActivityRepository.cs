using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for Activity entity operations
/// Provides specialized methods for activity stream functionality
/// </summary>
public class ActivityRepository : BaseRepository<Activity>, IActivityRepository
{
    public ActivityRepository(SivarDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets activities for a user's feed based on followed profiles
    /// </summary>
    public async Task<(IEnumerable<Activity> Activities, int TotalCount)> GetFeedActivitiesAsync(
        List<Guid> followedProfileIds,
        int page = 1,
        int pageSize = 20)
    {
        var query = GetQueryable()
            .Include(a => a.Actor)
            .Where(a => a.IsPublished && 
                       followedProfileIds.Contains(a.ActorId) &&
                       (a.Visibility == VisibilityLevel.Public || 
                        a.Visibility == VisibilityLevel.ConnectionsOnly))
            .OrderByDescending(a => a.PublishedAt)
            .ThenByDescending(a => a.EngagementScore);

        var totalCount = await query.CountAsync();
        var activities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (activities, totalCount);
    }

    /// <summary>
    /// Gets activities by a specific profile (actor)
    /// </summary>
    public async Task<(IEnumerable<Activity> Activities, int TotalCount)> GetByActorAsync(
        Guid actorId,
        int page = 1,
        int pageSize = 20)
    {
        var query = GetQueryable()
            .Include(a => a.Actor)
            .Where(a => a.ActorId == actorId && 
                       a.IsPublished &&
                       a.Visibility == VisibilityLevel.Public)
            .OrderByDescending(a => a.PublishedAt);

        var totalCount = await query.CountAsync();
        var activities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (activities, totalCount);
    }

    /// <summary>
    /// Gets activities on a specific object
    /// </summary>
    public async Task<(IEnumerable<Activity> Activities, int TotalCount)> GetByObjectAsync(
        string objectType,
        Guid objectId,
        int page = 1,
        int pageSize = 20)
    {
        var query = GetQueryable()
            .Include(a => a.Actor)
            .Where(a => a.ObjectType == objectType && 
                       a.ObjectId == objectId &&
                       a.IsPublished)
            .OrderByDescending(a => a.PublishedAt);

        var totalCount = await query.CountAsync();
        var activities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (activities, totalCount);
    }

    /// <summary>
    /// Gets activities by verb type
    /// </summary>
    public async Task<(IEnumerable<Activity> Activities, int TotalCount)> GetByVerbAsync(
        ActivityVerb verb,
        int page = 1,
        int pageSize = 20)
    {
        var query = GetQueryable()
            .Include(a => a.Actor)
            .Where(a => a.Verb == verb && 
                       a.IsPublished &&
                       a.Visibility == VisibilityLevel.Public)
            .OrderByDescending(a => a.PublishedAt);

        var totalCount = await query.CountAsync();
        var activities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (activities, totalCount);
    }

    /// <summary>
    /// Gets activities within a time range
    /// </summary>
    public async Task<(IEnumerable<Activity> Activities, int TotalCount)> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int page = 1,
        int pageSize = 20)
    {
        var query = GetQueryable()
            .Include(a => a.Actor)
            .Where(a => a.PublishedAt >= startDate && 
                       a.PublishedAt <= endDate &&
                       a.IsPublished &&
                       a.Visibility == VisibilityLevel.Public)
            .OrderByDescending(a => a.PublishedAt);

        var totalCount = await query.CountAsync();
        var activities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (activities, totalCount);
    }

    /// <summary>
    /// Gets trending activities based on engagement score
    /// </summary>
    public async Task<(IEnumerable<Activity> Activities, int TotalCount)> GetTrendingAsync(
        DateTime since,
        int page = 1,
        int pageSize = 20)
    {
        var query = GetQueryable()
            .Include(a => a.Actor)
            .Where(a => a.PublishedAt >= since && 
                       a.IsPublished &&
                       a.Visibility == VisibilityLevel.Public)
            .OrderByDescending(a => a.EngagementScore)
            .ThenByDescending(a => a.PublishedAt);

        var totalCount = await query.CountAsync();
        var activities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (activities, totalCount);
    }

    /// <summary>
    /// Gets activities by visibility level
    /// </summary>
    public async Task<(IEnumerable<Activity> Activities, int TotalCount)> GetByVisibilityAsync(
        VisibilityLevel visibility,
        int page = 1,
        int pageSize = 20)
    {
        var query = GetQueryable()
            .Include(a => a.Actor)
            .Where(a => a.Visibility == visibility && a.IsPublished)
            .OrderByDescending(a => a.PublishedAt);

        var totalCount = await query.CountAsync();
        var activities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (activities, totalCount);
    }

    /// <summary>
    /// Updates engagement score for an activity
    /// </summary>
    public async Task UpdateEngagementScoreAsync(Guid activityId, int engagementScore)
    {
        var activity = await GetByIdAsync(activityId);
        if (activity != null)
        {
            activity.EngagementScore = engagementScore;
            activity.UpdatedAt = DateTime.UtcNow;
            await SaveChangesAsync();
        }
    }

    /// <summary>
    /// Increments view count for an activity
    /// </summary>
    public async Task IncrementViewCountAsync(Guid activityId)
    {
        var activity = await GetByIdAsync(activityId);
        if (activity != null)
        {
            activity.IncrementViewCount();
            await SaveChangesAsync();
        }
    }

    /// <summary>
    /// Checks if an activity already exists for a specific actor-verb-object combination
    /// </summary>
    public async Task<bool> ExistsAsync(Guid actorId, ActivityVerb verb, string objectType, Guid objectId)
    {
        return await GetQueryable()
            .AnyAsync(a => a.ActorId == actorId && 
                          a.Verb == verb && 
                          a.ObjectType == objectType && 
                          a.ObjectId == objectId);
    }

    /// <summary>
    /// Gets the most recent activity for a specific object
    /// </summary>
    public async Task<Activity?> GetLatestActivityForObjectAsync(string objectType, Guid objectId)
    {
        return await GetQueryable()
            .Include(a => a.Actor)
            .Where(a => a.ObjectType == objectType && a.ObjectId == objectId)
            .OrderByDescending(a => a.PublishedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets queryable for advanced queries
    /// </summary>
    protected override IQueryable<Activity> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }
}
