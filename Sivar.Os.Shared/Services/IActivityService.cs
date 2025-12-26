using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service for creating and managing activity stream events
/// Automatically generates activities when users interact with content
/// </summary>
public interface IActivityService
{
    /// <summary>
    /// Creates an activity record for an action
    /// </summary>
    Task<Activity> CreateActivityAsync(
        Guid actorId,
        ActivityVerb verb,
        string objectType,
        Guid objectId,
        string? targetType = null,
        Guid? targetId = null,
        VisibilityLevel? visibility = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a post creation activity
    /// </summary>
    /// <param name="post">The created post entity.</param>
    /// <param name="postSnapshotJson">Optional JSON-serialized PostDto for denormalized feed loading.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Activity> RecordPostCreatedAsync(Post post, string? postSnapshotJson = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a comment activity
    /// </summary>
    Task<Activity> RecordCommentAsync(Comment comment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a reaction activity
    /// </summary>
    Task<Activity> RecordReactionAsync(Reaction reaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a follow activity
    /// </summary>
    Task<Activity> RecordFollowAsync(ProfileFollower follower, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a share activity
    /// </summary>
    Task<Activity> RecordShareAsync(Guid actorId, Post post, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities for a user's feed
    /// </summary>
    Task<List<Activity>> GetFeedActivitiesAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities by a specific profile
    /// </summary>
    Task<List<Activity>> GetProfileActivitiesAsync(
        Guid profileId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activities on a specific object
    /// </summary>
    Task<List<Activity>> GetObjectActivitiesAsync(
        string objectType,
        Guid objectId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
