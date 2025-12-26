using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of Activities Client
/// </summary>
public class ActivitiesClient : IActivitiesClient
{
    private readonly IActivityService _activityService;
    private readonly IUserService _userService;
    private readonly IPostService _postService;
    private readonly IProfileService _profileService;
    private readonly ICommentService _commentService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ActivitiesClient> _logger;

    public ActivitiesClient(
        IActivityService activityService,
        IUserService userService,
        IPostService postService,
        IProfileService profileService,
        ICommentService commentService,
        AuthenticationStateProvider authStateProvider,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ActivitiesClient> logger)
    {
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ActivityFeedDto> GetFeedActivitiesAsync(
        int pageSize = 20,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[ActivitiesClient.GetFeedActivitiesAsync] Getting feed for page {Page}, size {PageSize}", pageNumber, pageSize);

            // Get authenticated user
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning("[ActivitiesClient.GetFeedActivitiesAsync] User not authenticated");
                return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
            }

            var keycloakId = user.FindFirst("sub")?.Value 
                          ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ActivitiesClient.GetFeedActivitiesAsync] No Keycloak ID found");
                return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
            }

            // Get user from database
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var dbUser = await _userService.GetUserByKeycloakIdAsync(keycloakId);
            _logger.LogInformation("[ActivitiesClient] ⏱️ GetUserByKeycloakIdAsync: {Elapsed}ms", sw.ElapsedMilliseconds);
            
            if (dbUser == null)
            {
                _logger.LogWarning("[ActivitiesClient.GetFeedActivitiesAsync] User not found for Keycloak ID: {KeycloakId}", keycloakId);
                return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
            }

            // Get activities from service
            sw.Restart();
            var activities = await _activityService.GetFeedActivitiesAsync(
                dbUser.Id,
                pageNumber,
                pageSize,
                cancellationToken);
            _logger.LogInformation("[ActivitiesClient] ⏱️ GetFeedActivitiesAsync: {Elapsed}ms", sw.ElapsedMilliseconds);

            if (activities == null || !activities.Any())
            {
                _logger.LogInformation("[ActivitiesClient.GetFeedActivitiesAsync] No activities found");
                return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
            }

            // ⚡ PERFORMANCE: Only fetch posts that DON'T have a snapshot (old activities)
            // Activities with PostSnapshotJson can be deserialized instantly without DB call
            var postActivityIds = activities
                .Where(a => a.ObjectType.Equals("Post", StringComparison.OrdinalIgnoreCase))
                .Where(a => string.IsNullOrEmpty(a.PostSnapshotJson)) // Only fetch for activities without snapshot
                .Select(a => a.ObjectId)
                .Distinct()
                .ToList();
            
            // ⚡ BATCH FETCH: Get all posts in a single query instead of N+1 queries
            sw.Restart();
            var posts = new Dictionary<Guid, PostDto?>();
            if (postActivityIds.Any())
            {
                _logger.LogInformation("[ActivitiesClient] ⏱️ BATCH fetching {Count} posts WITHOUT snapshot", postActivityIds.Count);
                
                // Use batch fetch method - single DB query for all posts!
                posts = await _postService.GetPostsByIdsAsync(postActivityIds);
                
                _logger.LogInformation("[ActivitiesClient] ⏱️ BATCH fetched {Count} posts: {Elapsed}ms", posts.Count, sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("[ActivitiesClient] ⚡ All {Count} activities have PostSnapshotJson - zero DB lookups!", activities.Count);
            }

            // Map activities using pre-fetched data
            var activityDtos = new List<ActivityDto>();
            foreach (var activity in activities)
            {
                var dto = MapActivityToDto(activity, posts);
                activityDtos.Add(dto);
            }

            // Calculate total pages (for now, estimate based on returned count)
            var totalCount = activityDtos.Count;
            var totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;

            var feed = new ActivityFeedDto
            {
                Activities = activityDtos,
                Page = pageNumber - 1,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = Math.Max(totalPages, 1)
            };

            _logger.LogInformation("[ActivitiesClient.GetFeedActivitiesAsync] Returning {Count} activities", activityDtos.Count);
            return feed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ActivitiesClient.GetFeedActivitiesAsync] Error getting activity feed");
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
            var activities = await _activityService.GetProfileActivitiesAsync(
                profileId,
                pageNumber,
                pageSize,
                cancellationToken);

            // ⚡ PERFORMANCE: Only fetch posts without snapshots using BATCH method
            var postActivityIds = activities
                .Where(a => a.ObjectType.Equals("Post", StringComparison.OrdinalIgnoreCase))
                .Where(a => string.IsNullOrEmpty(a.PostSnapshotJson))
                .Select(a => a.ObjectId)
                .Distinct()
                .ToList();
            
            // ⚡ BATCH FETCH: Single query for all posts
            var posts = postActivityIds.Any() 
                ? await _postService.GetPostsByIdsAsync(postActivityIds) 
                : new Dictionary<Guid, PostDto?>();

            var activityDtos = activities.Select(a => MapActivityToDto(a, posts)).ToList();

            var totalCount = activityDtos.Count;
            var totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;

            return new ActivityFeedDto
            {
                Activities = activityDtos,
                Page = pageNumber - 1,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = Math.Max(totalPages, 1)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ActivitiesClient.GetProfileActivitiesAsync] Error getting profile activities");
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
            var activities = await _activityService.GetObjectActivitiesAsync(
                objectType,
                objectId,
                pageNumber,
                pageSize,
                cancellationToken);

            // ⚡ PERFORMANCE: Batch load all posts at once using single query
            var postActivityIds = activities
                .Where(a => a.ObjectType.Equals("Post", StringComparison.OrdinalIgnoreCase))
                .Where(a => string.IsNullOrEmpty(a.PostSnapshotJson))
                .Select(a => a.ObjectId)
                .Distinct()
                .ToList();
            
            // ⚡ BATCH FETCH: Single query for all posts
            var posts = postActivityIds.Any() 
                ? await _postService.GetPostsByIdsAsync(postActivityIds) 
                : new Dictionary<Guid, PostDto?>();

            var activityDtos = activities.Select(a => MapActivityToDto(a, posts)).ToList();

            var totalCount = activityDtos.Count;
            var totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;

            return new ActivityFeedDto
            {
                Activities = activityDtos,
                Page = pageNumber - 1,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = Math.Max(totalPages, 1)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ActivitiesClient.GetObjectActivitiesAsync] Error getting object activities");
            return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
        }
    }

    public async Task<ActivityFeedDto> GetTrendingActivitiesAsync(
        int hoursBack = 24,
        int pageSize = 20,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement trending activities
        _logger.LogWarning("[ActivitiesClient.GetTrendingActivitiesAsync] Not yet implemented");
        return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
    }

    /// <summary>
    /// Maps Activity entity to ActivityDto and enriches with related object data
    /// </summary>
    private async Task<ActivityDto> MapToActivityDto(Shared.Entities.Activity activity)
    {
        var dto = new ActivityDto
        {
            Id = activity.Id,
            ActorId = activity.ActorId,
            Actor = activity.Actor != null ? MapProfileToDto(activity.Actor) : null,
            Verb = activity.Verb,
            ObjectType = activity.ObjectType,
            ObjectId = activity.ObjectId,
            TargetType = activity.TargetType,
            TargetId = activity.TargetId,
            Summary = activity.Summary,
            Metadata = activity.Metadata,
            Visibility = activity.Visibility,
            PublishedAt = activity.PublishedAt,
            EngagementScore = activity.EngagementScore,
            ViewCount = activity.ViewCount
        };

        // Enrich with related object data
        try
        {
            switch (activity.ObjectType.ToLowerInvariant())
            {
                case "post":
                    var post = await _postService.GetPostByIdAsync(activity.ObjectId);
                    dto.Post = post;
                    break;

                case "comment":
                    var comment = await _commentService.GetCommentByIdAsync(activity.ObjectId);
                    dto.Comment = comment;
                    break;

                case "profile":
                    var profile = await _profileService.GetPublicProfileAsync(activity.ObjectId);
                    dto.TargetProfile = profile;
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ActivitiesClient.MapToActivityDto] Error enriching activity {ActivityId} with related object", activity.Id);
        }

        return dto;
    }

    /// <summary>
    /// Maps Activity entity to ActivityDto using pre-fetched posts (fast, no DB calls).
    /// ⚡ PERFORMANCE: First checks PostSnapshotJson (JSONB) for instant deserialization,
    /// falls back to pre-fetched dictionary if snapshot not available.
    /// </summary>
    private ActivityDto MapActivityToDto(Shared.Entities.Activity activity, Dictionary<Guid, PostDto?> posts)
    {
        var dto = new ActivityDto
        {
            Id = activity.Id,
            ActorId = activity.ActorId,
            Actor = activity.Actor != null ? MapProfileToDto(activity.Actor) : null,
            Verb = activity.Verb,
            ObjectType = activity.ObjectType,
            ObjectId = activity.ObjectId,
            TargetType = activity.TargetType,
            TargetId = activity.TargetId,
            Summary = activity.Summary,
            Metadata = activity.Metadata,
            Visibility = activity.Visibility,
            PublishedAt = activity.PublishedAt,
            EngagementScore = activity.EngagementScore,
            ViewCount = activity.ViewCount
        };

        // Use pre-fetched post data
        if (activity.ObjectType.Equals("Post", StringComparison.OrdinalIgnoreCase))
        {
            // ⚡ FAST PATH: Use PostSnapshotJson if available (denormalized JSONB)
            if (!string.IsNullOrEmpty(activity.PostSnapshotJson))
            {
                try
                {
                    dto.Post = JsonSerializer.Deserialize<PostDto>(activity.PostSnapshotJson, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });
                    _logger.LogDebug("[ActivitiesClient] ⚡ Used PostSnapshotJson for ActivityId={ActivityId}", activity.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[ActivitiesClient] Failed to deserialize PostSnapshotJson, falling back to posts dictionary");
                    // Fall through to dictionary lookup
                    if (posts.TryGetValue(activity.ObjectId, out var post))
                    {
                        dto.Post = post;
                    }
                }
            }
            // Fallback: Use pre-fetched dictionary (for older activities without snapshot)
            else if (posts.TryGetValue(activity.ObjectId, out var post))
            {
                dto.Post = post;
            }
        }

        return dto;
    }

    /// <summary>
    /// Maps Profile entity to ProfileDto
    /// </summary>
    private ProfileDto MapProfileToDto(Shared.Entities.Profile profile)
    {
        _logger.LogDebug("[ActivitiesClient.MapProfileToDto] Mapping profile: Id={ProfileId}, DisplayName={DisplayName}, Handle={Handle}",
            profile.Id, profile.DisplayName, profile.Handle);
            
        return new ProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            ProfileTypeId = profile.ProfileTypeId,
            DisplayName = profile.DisplayName,
            Handle = profile.Handle,  // ⭐ CRITICAL: Include Handle for profile navigation
            Bio = profile.Bio,
            Avatar = profile.Avatar,
            AvatarFileId = profile.AvatarFileId,
            IsActive = profile.IsActive,
            VisibilityLevel = profile.VisibilityLevel,
            ViewCount = profile.ViewCount,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
