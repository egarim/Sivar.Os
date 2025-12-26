using Microsoft.EntityFrameworkCore;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Implementation of activity service for managing activity stream events
/// </summary>
public class ActivityService : IActivityService
{
    private readonly IActivityRepository _activityRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IPostRepository _postRepository;
    private readonly IProfileFollowerRepository _profileFollowerRepository;

    public ActivityService(
        IActivityRepository activityRepository,
        IProfileRepository profileRepository,
        IPostRepository postRepository,
        IProfileFollowerRepository profileFollowerRepository)
    {
        _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _profileFollowerRepository = profileFollowerRepository ?? throw new ArgumentNullException(nameof(profileFollowerRepository));
    }

    public async Task<Activity> CreateActivityAsync(
        Guid actorId,
        ActivityVerb verb,
        string objectType,
        Guid objectId,
        string? targetType = null,
        Guid? targetId = null,
        VisibilityLevel? visibility = null,
        CancellationToken cancellationToken = default)
    {
        var actor = await _profileRepository.GetByIdAsync(actorId);

        if (actor == null)
        {
            throw new InvalidOperationException($"Actor profile not found: {actorId}");
        }

        var activity = new Activity
        {
            ActorId = actorId,
            Verb = verb,
            ObjectType = objectType,
            ObjectId = objectId,
            TargetType = targetType,
            TargetId = targetId,
            Visibility = visibility ?? VisibilityLevel.Public,
            PublishedAt = DateTime.UtcNow,
            IsPublished = true,
            Summary = string.Empty // Will be generated
        };

        // Generate summary
        activity.Summary = activity.GenerateSummary(actor.DisplayName);

        await _activityRepository.AddAsync(activity);
        await _activityRepository.SaveChangesAsync();

        return activity;
    }

    public async Task<Activity> RecordPostCreatedAsync(Post post, string? postSnapshotJson = null, CancellationToken cancellationToken = default)
    {
        var actor = await _profileRepository.GetByIdAsync(post.ProfileId);

        if (actor == null)
        {
            throw new InvalidOperationException($"Actor profile not found: {post.ProfileId}");
        }

        var activity = new Activity
        {
            ActorId = post.ProfileId,
            Verb = ActivityVerb.Create,
            ObjectType = "Post",
            ObjectId = post.Id,
            Visibility = post.Visibility,
            PublishedAt = DateTime.UtcNow,
            IsPublished = true,
            PostSnapshotJson = postSnapshotJson, // Store the denormalized post data
            Summary = string.Empty
        };

        activity.Summary = activity.GenerateSummary(actor.DisplayName);

        await _activityRepository.AddAsync(activity);
        await _activityRepository.SaveChangesAsync();

        return activity;
    }

    public async Task<Activity> RecordCommentAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        // Get the post to determine visibility
        var post = await _postRepository.GetByIdAsync(comment.PostId);

        return await CreateActivityAsync(
            actorId: comment.ProfileId,
            verb: ActivityVerb.Comment,
            objectType: "Comment",
            objectId: comment.Id,
            targetType: "Post",
            targetId: comment.PostId,
            visibility: post?.Visibility ?? VisibilityLevel.Public,
            cancellationToken: cancellationToken);
    }

    public async Task<Activity> RecordReactionAsync(Reaction reaction, CancellationToken cancellationToken = default)
    {
        VisibilityLevel visibility = VisibilityLevel.Public;
        string objectType;
        Guid objectId;

        if (reaction.PostId.HasValue)
        {
            var post = await _postRepository.GetByIdAsync(reaction.PostId.Value);
            visibility = post?.Visibility ?? VisibilityLevel.Public;
            objectType = "Post";
            objectId = reaction.PostId.Value;
        }
        else if (reaction.CommentId.HasValue)
        {
            objectType = "Comment";
            objectId = reaction.CommentId.Value;
        }
        else
        {
            throw new InvalidOperationException("Reaction must have either PostId or CommentId");
        }

        return await CreateActivityAsync(
            actorId: reaction.ProfileId,
            verb: ActivityVerb.Like,
            objectType: objectType,
            objectId: objectId,
            visibility: visibility,
            cancellationToken: cancellationToken);
    }

    public async Task<Activity> RecordFollowAsync(ProfileFollower follower, CancellationToken cancellationToken = default)
    {
        return await CreateActivityAsync(
            actorId: follower.FollowerProfileId,
            verb: ActivityVerb.Follow,
            objectType: "Profile",
            objectId: follower.FollowedProfileId,
            visibility: VisibilityLevel.Public,
            cancellationToken: cancellationToken);
    }

    public async Task<Activity> RecordShareAsync(Guid actorId, Post post, CancellationToken cancellationToken = default)
    {
        return await CreateActivityAsync(
            actorId: actorId,
            verb: ActivityVerb.Share,
            objectType: "Post",
            objectId: post.Id,
            visibility: post.Visibility,
            cancellationToken: cancellationToken);
    }

    public async Task<List<Activity>> GetFeedActivitiesAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Get user's active profile
        var userProfile = await _profileRepository.GetActiveProfileByUserIdAsync(userId);

        if (userProfile == null)
        {
            return new List<Activity>();
        }

        // Get profiles the user follows
        var followers = await _profileFollowerRepository.GetFollowingByProfileIdAsync(userProfile.Id);
        var followedProfileIds = followers.Select(f => f.FollowedProfileId).ToList();

        // Include own profile
        followedProfileIds.Add(userProfile.Id);

        // Get activities from followed profiles
        var (activities, _) = await _activityRepository.GetFeedActivitiesAsync(
            followedProfileIds, 
            page, 
            pageSize);

        return activities.ToList();
    }

    public async Task<List<Activity>> GetProfileActivitiesAsync(
        Guid profileId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (activities, _) = await _activityRepository.GetByActorAsync(profileId, page, pageSize);
        return activities.ToList();
    }

    public async Task<List<Activity>> GetObjectActivitiesAsync(
        string objectType,
        Guid objectId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (activities, _) = await _activityRepository.GetByObjectAsync(objectType, objectId, page, pageSize);
        return activities.ToList();
    }
}
