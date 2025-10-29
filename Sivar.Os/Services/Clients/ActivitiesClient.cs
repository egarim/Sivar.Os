using Microsoft.AspNetCore.Components.Authorization;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;
using System.Security.Claims;

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
    private readonly ILogger<ActivitiesClient> _logger;

    public ActivitiesClient(
        IActivityService activityService,
        IUserService userService,
        IPostService postService,
        IProfileService profileService,
        ICommentService commentService,
        AuthenticationStateProvider authStateProvider,
        ILogger<ActivitiesClient> logger)
    {
        _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
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
            var dbUser = await _userService.GetUserByKeycloakIdAsync(keycloakId);
            if (dbUser == null)
            {
                _logger.LogWarning("[ActivitiesClient.GetFeedActivitiesAsync] User not found for Keycloak ID: {KeycloakId}", keycloakId);
                return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
            }

            // Get activities from service
            var activities = await _activityService.GetFeedActivitiesAsync(
                dbUser.Id,
                pageNumber,
                pageSize,
                cancellationToken);

            if (activities == null || !activities.Any())
            {
                _logger.LogInformation("[ActivitiesClient.GetFeedActivitiesAsync] No activities found");
                return new ActivityFeedDto { Page = pageNumber - 1, PageSize = pageSize };
            }

            // Map to DTOs and enrich with related data
            var activityDtos = new List<ActivityDto>();
            foreach (var activity in activities)
            {
                var dto = await MapToActivityDto(activity);
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

            var activityDtos = new List<ActivityDto>();
            foreach (var activity in activities)
            {
                var dto = await MapToActivityDto(activity);
                activityDtos.Add(dto);
            }

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

            var activityDtos = new List<ActivityDto>();
            foreach (var activity in activities)
            {
                var dto = await MapToActivityDto(activity);
                activityDtos.Add(dto);
            }

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
    /// Maps Profile entity to ProfileDto
    /// </summary>
    private ProfileDto MapProfileToDto(Shared.Entities.Profile profile)
    {
        return new ProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            ProfileTypeId = profile.ProfileTypeId,
            DisplayName = profile.DisplayName,
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
