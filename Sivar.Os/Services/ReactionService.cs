
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service implementation for Reaction management in the activity stream
/// Provides business logic layer for reaction operations with analytics and engagement features
/// </summary>
public class ReactionService : IReactionService
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IPostRepository _postRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<ReactionService> _logger;

    public ReactionService(
        IReactionRepository reactionRepository,
        IPostRepository postRepository,
        ICommentRepository commentRepository,
        IUserRepository userRepository,
        IProfileRepository profileRepository,
        ILogger<ReactionService> logger)
    {
        _reactionRepository = reactionRepository ?? throw new ArgumentNullException(nameof(reactionRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Toggles a reaction on a post for the authenticated user's active profile
    /// </summary>
    public async Task<ReactionResultDto?> TogglePostReactionAsync(string keycloakId, Guid postId, ReactionType reactionType)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ReactionService.TogglePostReactionAsync] START - RequestId={RequestId}, PostId={PostId}, ReactionType={ReactionType}, KeycloakId={KeycloakId}", 
            requestId, postId, reactionType, keycloakId ?? "NULL");

        if (string.IsNullOrWhiteSpace(keycloakId))
        {
            _logger.LogWarning("[ReactionService.TogglePostReactionAsync] NULL_KEYCLOAK_ID - RequestId={RequestId}", requestId);
            return null;
        }

        // Get user and their active profile
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
        {
            _logger.LogWarning("[ReactionService.TogglePostReactionAsync] USER_NOT_FOUND - RequestId={RequestId}, KeycloakId={KeycloakId}", requestId, keycloakId);
            return null;
        }

        if (user.ActiveProfile == null)
        {
            _logger.LogWarning("[ReactionService.TogglePostReactionAsync] NO_ACTIVE_PROFILE - RequestId={RequestId}, UserId={UserId}", requestId, user.Id);
            return null;
        }

        _logger.LogInformation("[ReactionService.TogglePostReactionAsync] User found - RequestId={RequestId}, UserId={UserId}, ProfileId={ProfileId}", 
            requestId, user.Id, user.ActiveProfile.Id);

        // Check if user can react to this post
        if (!await CanUserReactToPostAsync(postId, keycloakId))
        {
            _logger.LogWarning("[ReactionService.TogglePostReactionAsync] REACTION_NOT_ALLOWED - RequestId={RequestId}, PostId={PostId}, ProfileId={ProfileId}", 
                requestId, postId, user.ActiveProfile.Id);
            return null;
        }

        try
        {
            var result = await _reactionRepository.ToggleReactionAsync(user.ActiveProfile.Id, reactionType, postId, null);
            _logger.LogInformation("[ReactionService.TogglePostReactionAsync] Reaction toggled - RequestId={RequestId}, Action={Action}, ReactionId={ReactionId}", 
                requestId, result.Action, result.Reaction?.Id ?? Guid.Empty);

            var updatedCounts = await _reactionRepository.GetReactionCountsByPostAsync(postId);
            _logger.LogInformation("[ReactionService.TogglePostReactionAsync] Reaction counts updated - RequestId={RequestId}, TotalReactions={Total}", 
                requestId, updatedCounts.Sum(r => r.Value));

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ReactionService.TogglePostReactionAsync] SUCCESS - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms", 
                requestId, postId, elapsed);

            return new ReactionResultDto
            {
                Action = result.Action,
                ReactionType = reactionType,
                Reaction = result.Reaction != null ? await MapToReactionDtoAsync(result.Reaction) : null,
                UpdatedCounts = updatedCounts
            };
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ReactionService.TogglePostReactionAsync] ERROR - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms", 
                requestId, postId, elapsed);
            return null;
        }
    }

    /// <summary>
    /// Toggles a reaction on a comment for the authenticated user's active profile
    /// </summary>
    public async Task<ReactionResultDto?> ToggleCommentReactionAsync(string keycloakId, Guid commentId, ReactionType reactionType)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ReactionService.ToggleCommentReactionAsync] START - RequestId={RequestId}, CommentId={CommentId}, ReactionType={ReactionType}, KeycloakId={KeycloakId}", 
            requestId, commentId, reactionType, keycloakId ?? "NULL");

        if (string.IsNullOrWhiteSpace(keycloakId))
        {
            _logger.LogWarning("[ReactionService.ToggleCommentReactionAsync] NULL_KEYCLOAK_ID - RequestId={RequestId}", requestId);
            return null;
        }

        // Get user and their active profile
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
        {
            _logger.LogWarning("[ReactionService.ToggleCommentReactionAsync] USER_NOT_FOUND - RequestId={RequestId}, KeycloakId={KeycloakId}", requestId, keycloakId);
            return null;
        }

        if (user.ActiveProfile == null)
        {
            _logger.LogWarning("[ReactionService.ToggleCommentReactionAsync] NO_ACTIVE_PROFILE - RequestId={RequestId}, UserId={UserId}", requestId, user.Id);
            return null;
        }

        _logger.LogInformation("[ReactionService.ToggleCommentReactionAsync] User found - RequestId={RequestId}, UserId={UserId}, ProfileId={ProfileId}", 
            requestId, user.Id, user.ActiveProfile.Id);

        // Check if user can react to this comment
        if (!await CanUserReactToCommentAsync(commentId, keycloakId))
        {
            _logger.LogWarning("[ReactionService.ToggleCommentReactionAsync] REACTION_NOT_ALLOWED - RequestId={RequestId}, CommentId={CommentId}, ProfileId={ProfileId}", 
                requestId, commentId, user.ActiveProfile.Id);
            return null;
        }

        try
        {
            var result = await _reactionRepository.ToggleReactionAsync(user.ActiveProfile.Id, reactionType, null, commentId);
            _logger.LogInformation("[ReactionService.ToggleCommentReactionAsync] Reaction toggled - RequestId={RequestId}, Action={Action}, ReactionId={ReactionId}", 
                requestId, result.Action, result.Reaction?.Id ?? Guid.Empty);

            var updatedCounts = await _reactionRepository.GetReactionCountsByCommentAsync(commentId);
            _logger.LogInformation("[ReactionService.ToggleCommentReactionAsync] Reaction counts updated - RequestId={RequestId}, TotalReactions={Total}", 
                requestId, updatedCounts.Sum(r => r.Value));

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ReactionService.ToggleCommentReactionAsync] SUCCESS - RequestId={RequestId}, CommentId={CommentId}, Duration={Duration}ms", 
                requestId, commentId, elapsed);

            return new ReactionResultDto
            {
                Action = result.Action,
                ReactionType = reactionType,
                Reaction = result.Reaction != null ? await MapToReactionDtoAsync(result.Reaction) : null,
                UpdatedCounts = updatedCounts
            };
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ReactionService.ToggleCommentReactionAsync] ERROR - RequestId={RequestId}, CommentId={CommentId}, Duration={Duration}ms", 
                requestId, commentId, elapsed);
            return null;
        }
    }

    /// <summary>
    /// Gets reaction counts and user's reaction for a post
    /// </summary>
    public async Task<PostReactionSummaryDto> GetPostReactionSummaryAsync(Guid postId, string? requestingKeycloakId = null)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ReactionService.GetPostReactionSummaryAsync] START - RequestId={RequestId}, PostId={PostId}, RequestingKeycloakId={KeycloakId}", 
            requestId, postId, requestingKeycloakId ?? "NULL");

        try
        {
            var reactionCounts = await _reactionRepository.GetReactionCountsByPostAsync(postId);
            _logger.LogInformation("[ReactionService.GetPostReactionSummaryAsync] Reaction counts retrieved - RequestId={RequestId}, TotalReactions={Total}", 
                requestId, reactionCounts.Sum(r => r.Value));

            ReactionType? userReaction = null;

            if (!string.IsNullOrWhiteSpace(requestingKeycloakId))
            {
                var user = await _userRepository.GetByKeycloakIdAsync(requestingKeycloakId);
                if (user?.ActiveProfile != null)
                {
                    var reaction = await _reactionRepository.GetUserReactionToPostAsync(postId, user.ActiveProfile.Id);
                    userReaction = reaction?.ReactionType;
                    _logger.LogInformation("[ReactionService.GetPostReactionSummaryAsync] User reaction found - RequestId={RequestId}, UserReaction={Reaction}", 
                        requestId, userReaction?.ToString() ?? "NONE");
                }
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ReactionService.GetPostReactionSummaryAsync] SUCCESS - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms", 
                requestId, postId, elapsed);

            return new PostReactionSummaryDto
            {
                PostId = postId,
                TotalReactions = reactionCounts.Sum(r => r.Value),
                ReactionCounts = reactionCounts,
                UserReaction = userReaction,
                TopReactionType = reactionCounts.OrderByDescending(r => r.Value).FirstOrDefault().Key,
                HasUserReacted = userReaction.HasValue
            };
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ReactionService.GetPostReactionSummaryAsync] ERROR - RequestId={RequestId}, PostId={PostId}, Duration={Duration}ms", 
                requestId, postId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Gets reaction counts and user's reaction for a comment
    /// </summary>
    public async Task<CommentReactionSummaryDto> GetCommentReactionSummaryAsync(Guid commentId, string? requestingKeycloakId = null)
    {
        var reactionCounts = await _reactionRepository.GetReactionCountsByCommentAsync(commentId);
        ReactionType? userReaction = null;

        if (!string.IsNullOrWhiteSpace(requestingKeycloakId))
        {
            var user = await _userRepository.GetByKeycloakIdAsync(requestingKeycloakId);
            if (user?.ActiveProfile != null)
            {
                var reaction = await _reactionRepository.GetUserReactionToCommentAsync(commentId, user.ActiveProfile.Id);
                userReaction = reaction?.ReactionType;
            }
        }

        return new CommentReactionSummaryDto
        {
            CommentId = commentId,
            TotalReactions = reactionCounts.Sum(r => r.Value),
            ReactionCounts = reactionCounts,
            UserReaction = userReaction,
            TopReactionType = reactionCounts.OrderByDescending(r => r.Value).FirstOrDefault().Key,
            HasUserReacted = userReaction.HasValue
        };
    }

    /// <summary>
    /// Gets detailed reaction analytics for a post (author only)
    /// </summary>
    public async Task<PostReactionAnalyticsDto?> GetPostReactionAnalyticsAsync(Guid postId, string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return null;

        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            return null;

        // Check if user is the author
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user?.ActiveProfile?.Id != post.ProfileId)
            return null;

        try
        {
            var reactionCounts = await _reactionRepository.GetReactionCountsByPostAsync(postId);
            var timelineAnalytics = await _reactionRepository.GetReactionTimelineAsync(postId, 1);
            var topReactors = await _reactionRepository.GetTopReactorsToProfileAsync(Guid.Empty, DateTime.UtcNow.AddDays(-30), 10);

            var trends = new List<ReactionTrendDto>();
            foreach (var analytics in timelineAnalytics)
            {
                trends.Add(new ReactionTrendDto
                {
                    Hour = analytics.TimeSlot,
                    ReactionCounts = analytics.ReactionsByType,
                    TotalCount = analytics.ReactionsByType.Sum(r => r.Value)
                });
            }

            // TODO: Implement GetFirstReactionDateAsync and GetLatestReactionDateAsync in repository
            var firstReaction = DateTime.UtcNow.AddDays(-30); // Placeholder
            var lastReaction = DateTime.UtcNow; // Placeholder

            return new PostReactionAnalyticsDto
            {
                PostId = postId,
                TotalReactions = reactionCounts.Sum(r => r.Value),
                ReactionsByType = reactionCounts,
                ReactionTrends = trends,
                PeakReactionHour = trends.OrderByDescending(t => t.TotalCount).FirstOrDefault()?.Hour.Hour ?? 0,
                AvgReactionsPerHour = trends.Any() ? trends.Average(t => t.TotalCount) : 0,
                TopReactors = await MapTopReactorsToProfileDtos(topReactors),
                FirstReactionAt = firstReaction,
                LastReactionAt = lastReaction
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets reactions by a specific profile with pagination
    /// </summary>
    public async Task<(IEnumerable<ReactionDto> Reactions, int TotalCount)> GetReactionsByProfileAsync(
        Guid profileId, 
        string? requestingKeycloakId = null, 
        ReactionType? reactionType = null,
        int page = 1, 
        int pageSize = 20)
    {
        var (reactions, totalCount) = await _reactionRepository.GetByProfileAsync(profileId, page, pageSize);
        
        var reactionDtos = new List<ReactionDto>();
        foreach (var reaction in reactions)
        {
            if (reactionType == null || reaction.ReactionType == reactionType)
            {
                // Check if user can view the target content
                bool canView = false;
                if (reaction.PostId.HasValue)
                {
                    canView = !string.IsNullOrEmpty(requestingKeycloakId) && await CanUserReactToPostAsync(reaction.PostId.Value, requestingKeycloakId);
                }
                else if (reaction.CommentId.HasValue)
                {
                    canView = !string.IsNullOrEmpty(requestingKeycloakId) && await CanUserReactToCommentAsync(reaction.CommentId.Value, requestingKeycloakId);
                }

                if (canView)
                {
                    var dto = await MapToReactionDtoAsync(reaction);
                    if (dto != null)
                        reactionDtos.Add(dto);
                }
            }
        }

        return (reactionDtos, totalCount);
    }

    /// <summary>
    /// Gets who reacted to a post with specific reaction type
    /// </summary>
    public async Task<(IEnumerable<ReactionWithProfileDto> Reactions, int TotalCount)> GetPostReactionsByTypeAsync(
        Guid postId, 
        ReactionType reactionType, 
        string? requestingKeycloakId = null,
        int page = 1, 
        int pageSize = 20)
    {
        // Check if user can view this post
        if (string.IsNullOrEmpty(requestingKeycloakId) || !await CanUserReactToPostAsync(postId, requestingKeycloakId))
            return (Enumerable.Empty<ReactionWithProfileDto>(), 0);

        var (reactions, totalCount) = await _reactionRepository.GetByPostAsync(postId, page, pageSize, reactionType: reactionType);
        
        var reactionDtos = new List<ReactionWithProfileDto>();
        foreach (var reaction in reactions)
        {
            var dto = await MapToReactionWithProfileDtoAsync(reaction);
            if (dto != null)
                reactionDtos.Add(dto);
        }

        return (reactionDtos, totalCount);
    }

    /// <summary>
    /// Gets who reacted to a comment with specific reaction type
    /// </summary>
    public async Task<(IEnumerable<ReactionWithProfileDto> Reactions, int TotalCount)> GetCommentReactionsByTypeAsync(
        Guid commentId, 
        ReactionType reactionType, 
        string? requestingKeycloakId = null,
        int page = 1, 
        int pageSize = 20)
    {
        // Check if user can view this comment
        if (string.IsNullOrEmpty(requestingKeycloakId) || !await CanUserReactToCommentAsync(commentId, requestingKeycloakId))
            return (Enumerable.Empty<ReactionWithProfileDto>(), 0);

        var (reactions, totalCount) = await _reactionRepository.GetByCommentAsync(commentId, page, pageSize, reactionType: reactionType);
        
        var reactionDtos = new List<ReactionWithProfileDto>();
        foreach (var reaction in reactions)
        {
            var dto = await MapToReactionWithProfileDtoAsync(reaction);
            if (dto != null)
                reactionDtos.Add(dto);
        }

        return (reactionDtos, totalCount);
    }

    /// <summary>
    /// Gets reaction engagement statistics for a profile's content
    /// </summary>
    public async Task<ProfileReactionEngagementDto?> GetProfileReactionEngagementAsync(Guid profileId, string keycloakId, int daysBack = 30)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return null;

        // Check if user owns the profile
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user?.ActiveProfile?.Id != profileId)
            return null;

        try
        {
            var engagement = await _reactionRepository.GetEngagementStatisticsAsync(profileId, DateTime.UtcNow.AddDays(-daysBack));
            var topPosts = await _reactionRepository.GetMostReactedPostsAsync(DateTime.UtcNow.AddDays(-daysBack), DateTime.UtcNow, null, 0, 5);

            return new ProfileReactionEngagementDto
            {
                ProfileId = profileId,
                AnalysisDays = daysBack,
                TotalReactionsReceived = engagement.TotalReactions,
                TotalReactionsGiven = 0, // TODO: Implement reactions given tracking
                ReactionsReceivedByType = new Dictionary<ReactionType, int> { { engagement.MostUsedReactionType, engagement.TotalReactions } },
                ReactionsGivenByType = new Dictionary<ReactionType, int>(),
                AvgReactionsPerPost = engagement.ReactionsPerPost,
                TopEngagingPosts = topPosts.Posts.Select(p => p.PostId).ToList(),
                EngagementRate = engagement.EngagementRate,
                MostReceivedReactionType = engagement.MostUsedReactionType,
                MostGivenReactionType = ReactionType.Like // Placeholder
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets recent reaction activity for a profile (for notifications)
    /// </summary>
    public async Task<IEnumerable<ReactionActivityDto>> GetRecentReactionActivityAsync(Guid profileId, int hoursBack = 24)
    {
        var activities = await _reactionRepository.GetRecentByProfileAsync(profileId, DateTime.UtcNow.AddHours(-hoursBack), 100);
        
        var activityDtos = new List<ReactionActivityDto>();
        foreach (var activity in activities)
        {
            string contentPreview = "";
            if (activity.PostId.HasValue)
            {
                var post = await _postRepository.GetByIdAsync(activity.PostId.Value);
                contentPreview = post?.Content?.Length > 50 ? post.Content.Substring(0, 50) + "..." : post?.Content ?? "";
            }
            else if (activity.CommentId.HasValue)
            {
                var comment = await _commentRepository.GetByIdAsync(activity.CommentId.Value);
                contentPreview = comment?.Content?.Length > 50 ? comment.Content.Substring(0, 50) + "..." : comment?.Content ?? "";
            }

            activityDtos.Add(new ReactionActivityDto
            {
                ReactionId = activity.Id,
                PostId = activity.PostId,
                CommentId = activity.CommentId,
                Reactor = MapToProfileDto(activity.Profile!),
                ReactionType = activity.ReactionType,
                ActivityType = ReactionActivityType.Added, // TODO: Determine activity type based on tracking
                ActivityAt = activity.CreatedAt,
                TargetContentPreview = contentPreview
            });
        }

        return activityDtos;
    }

    /// <summary>
    /// Validates if a user can react to a specific post
    /// </summary>
    public async Task<bool> CanUserReactToPostAsync(Guid postId, string requestingKeycloakId)
    {
        if (string.IsNullOrWhiteSpace(requestingKeycloakId))
            return false;

        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            return false;

        // Public posts can be reacted to by anyone
        if (post.Visibility == VisibilityLevel.Public)
            return true;

        var user = await _userRepository.GetByKeycloakIdAsync(requestingKeycloakId);
        if (user?.ActiveProfile == null)
            return false;

        // Author can always react to their own posts
        if (user.ActiveProfile.Id == post.ProfileId)
            return true;

        // TODO: Implement follower-based reactions when ProfileFollower system is integrated
        switch (post.Visibility)
        {
            case VisibilityLevel.ConnectionsOnly:
                // Check if requesting user follows the post author
                return false; // For now, return false until follower system is integrated
            
            case VisibilityLevel.Private:
                return false;
            
            default:
                return false;
        }
    }

    /// <summary>
    /// Validates if a user can react to a specific comment
    /// </summary>
    public async Task<bool> CanUserReactToCommentAsync(Guid commentId, string requestingKeycloakId)
    {
        if (string.IsNullOrWhiteSpace(requestingKeycloakId))
            return false;

        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
            return false;

        // Check if user can view the parent post
        return await CanUserReactToPostAsync(comment.PostId, requestingKeycloakId);
    }

    #region Private Helper Methods

    /// <summary>
    /// Maps a Reaction entity to ReactionDto
    /// </summary>
    private async Task<ReactionDto?> MapToReactionDtoAsync(Reaction reaction)
    {
        if (reaction.Profile == null)
        {
            var profile = await _profileRepository.GetByIdAsync(reaction.ProfileId);
            if (profile == null)
                return null;
            reaction.Profile = profile;
        }

        return new ReactionDto
        {
            Id = reaction.Id,
            Profile = MapToProfileDto(reaction.Profile),
            PostId = reaction.PostId,
            CommentId = reaction.CommentId,
            ReactionType = reaction.ReactionType,
            CreatedAt = reaction.CreatedAt,
            UpdatedAt = reaction.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a Reaction entity to ReactionWithProfileDto
    /// </summary>
    private async Task<ReactionWithProfileDto?> MapToReactionWithProfileDtoAsync(Reaction reaction)
    {
        if (reaction.Profile == null)
        {
            var profile = await _profileRepository.GetByIdAsync(reaction.ProfileId);
            if (profile == null)
                return null;
            reaction.Profile = profile;
        }

        return new ReactionWithProfileDto
        {
            Id = reaction.Id,
            Profile = MapToProfileDto(reaction.Profile),
            ReactionType = reaction.ReactionType,
            CreatedAt = reaction.CreatedAt
        };
    }

    /// <summary>
    /// Maps a Profile entity to ProfileDto (simplified version)
    /// </summary>
    private ProfileDto MapToProfileDto(Profile profile)
    {
        // TODO: Use proper ProfileService mapping when available
        return new ProfileDto
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            Avatar = profile.Avatar ?? "",
            Bio = profile.Bio ?? "",
            // Add other profile properties as needed
        };
    }

    /// <summary>
    /// Maps a list of Profile entities to ProfileDtos
    /// </summary>
    private List<ProfileDto> MapToProfileDtos(IEnumerable<Profile> profiles)
    {
        var profileDtos = new List<ProfileDto>();
        foreach (var profile in profiles)
        {
            profileDtos.Add(MapToProfileDto(profile));
        }
        return profileDtos;
    }

    /// <summary>
    /// Helper method to map TopReactor objects to ProfileDto objects
    /// </summary>
    private async Task<List<ProfileDto>> MapTopReactorsToProfileDtos(IEnumerable<TopReactor> topReactors)
    {
        var profileDtos = new List<ProfileDto>();
        foreach (var reactor in topReactors)
        {
            // Fetch the actual profile entity from the repository
            var profile = await _profileRepository.GetByIdAsync(reactor.ProfileId);
            if (profile != null)
            {
                profileDtos.Add(MapToProfileDto(profile));
            }
        }
        return profileDtos;
    }

    #endregion
}