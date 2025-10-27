using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;


namespace Sivar.Os.Controllers;

/// <summary>
/// Controller for managing profile follower relationships
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FollowersController : ControllerBase
{
    private readonly IProfileFollowerService _followerService;
    private readonly IProfileService _profileService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<FollowersController> _logger;

    public FollowersController(
        IProfileFollowerService followerService,
        IProfileService profileService,
        INotificationService notificationService,
        ILogger<FollowersController> logger)
    {
        _followerService = followerService ?? throw new ArgumentNullException(nameof(followerService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all followers of the current user's active profile
    /// </summary>
    [HttpGet("followers")]
    public async Task<ActionResult<IEnumerable<FollowerProfileDto>>> GetFollowers()
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("[FollowersController.GetFollowers] START");

        try
        {
            // Get current user's profile ID if available
            var currentUserKeycloakId = GetCurrentUserKeycloakId();
            _logger.LogInformation("[FollowersController.GetFollowers] KeycloakId: {KeycloakId}", currentUserKeycloakId ?? "ANONYMOUS");
            
            Guid? currentUserProfileId = null;
            
            if (!string.IsNullOrEmpty(currentUserKeycloakId))
            {
                var currentUserProfile = await _profileService.GetMyActiveProfileAsync(currentUserKeycloakId);
                currentUserProfileId = currentUserProfile?.Id;
                _logger.LogInformation("[FollowersController.GetFollowers] Active ProfileId: {ProfileId}", currentUserProfileId?.ToString() ?? "NULL");
            }

            if (currentUserProfileId == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[FollowersController.GetFollowers] NO_ACTIVE_PROFILE - Duration={Duration}ms", elapsed);
                return BadRequest("User must have an active profile");
            }

            var followers = await _followerService.GetFollowersAsync(currentUserProfileId.Value, currentUserProfileId);
            var followersList = followers.ToList();
            
            var successElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[FollowersController.GetFollowers] SUCCESS - Count={Count}, Duration={Duration}ms", 
                followersList.Count, successElapsed);
            
            return Ok(followersList);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FollowersController.GetFollowers] ERROR - Duration={Duration}ms", elapsed);
            return StatusCode(500, "An error occurred while retrieving followers");
        }
    }

    /// <summary>
    /// Get all profiles that the current user is following
    /// </summary>
    [HttpGet("following")]
    public async Task<ActionResult<IEnumerable<FollowingProfileDto>>> GetFollowing()
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("[FollowersController.GetFollowing] START");

        try
        {
            // Get current user's profile ID if available
            var currentUserKeycloakId = GetCurrentUserKeycloakId();
            _logger.LogInformation("[FollowersController.GetFollowing] KeycloakId: {KeycloakId}", currentUserKeycloakId ?? "ANONYMOUS");
            
            Guid? currentUserProfileId = null;
            
            if (!string.IsNullOrEmpty(currentUserKeycloakId))
            {
                var currentUserProfile = await _profileService.GetMyActiveProfileAsync(currentUserKeycloakId);
                currentUserProfileId = currentUserProfile?.Id;
                _logger.LogInformation("[FollowersController.GetFollowing] Active ProfileId: {ProfileId}", currentUserProfileId?.ToString() ?? "NULL");
            }

            if (currentUserProfileId == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[FollowersController.GetFollowing] NO_ACTIVE_PROFILE - Duration={Duration}ms", elapsed);
                return BadRequest("User must have an active profile");
            }

            var following = await _followerService.GetFollowingAsync(currentUserProfileId.Value, currentUserProfileId);
            var followingList = following.ToList();
            
            var successElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[FollowersController.GetFollowing] SUCCESS - Count={Count}, Duration={Duration}ms", 
                followingList.Count, successElapsed);
            
            return Ok(followingList);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FollowersController.GetFollowing] ERROR - Duration={Duration}ms", elapsed);
            return StatusCode(500, "An error occurred while retrieving following");
        }
    }

    /// <summary>
    /// Get follower statistics for the current user's active profile
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<FollowerStatsDto>> GetFollowerStats()
    {
        try
        {
            // Get current user's profile ID if available
            var currentUserKeycloakId = GetCurrentUserKeycloakId();
            Guid? currentUserProfileId = null;
            
            if (!string.IsNullOrEmpty(currentUserKeycloakId))
            {
                var currentUserProfile = await _profileService.GetMyActiveProfileAsync(currentUserKeycloakId);
                currentUserProfileId = currentUserProfile?.Id;
            }

            if (currentUserProfileId == null)
                return BadRequest("User must have an active profile");

            var stats = await _followerService.GetFollowerStatsAsync(currentUserProfileId.Value, currentUserProfileId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting follower stats");
            return StatusCode(500, "An error occurred while retrieving follower statistics");
        }
    }

    /// <summary>
    /// Follow a profile
    /// </summary>
    [HttpPost("follow")]
    public async Task<ActionResult<FollowResultDto>> FollowProfile([FromBody] FollowActionDto followAction)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[FollowersController.FollowProfile] START - RequestId={RequestId}, TargetProfileId={TargetProfileId}", 
            requestId, followAction?.ProfileToFollowId);

        try
        {
            var currentUserKeycloakId = GetCurrentUserKeycloakId();
            _logger.LogInformation("[FollowersController.FollowProfile] KeycloakId extracted: {KeycloakId}, RequestId={RequestId}", 
                currentUserKeycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(currentUserKeycloakId))
            {
                _logger.LogWarning("[FollowersController.FollowProfile] UNAUTHORIZED - No KeycloakId, RequestId={RequestId}", requestId);
                return Unauthorized("User must be authenticated to follow profiles");
            }

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(currentUserKeycloakId);
            if (currentUserProfile == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[FollowersController.FollowProfile] NO_ACTIVE_PROFILE - KeycloakId={KeycloakId}, RequestId={RequestId}, Duration={Duration}ms", 
                    currentUserKeycloakId, requestId, elapsed);
                return BadRequest("User must have an active profile to follow other profiles");
            }

            _logger.LogInformation("[FollowersController.FollowProfile] Active profile found - ProfileId={ProfileId}, following ProfileId={TargetProfileId}, RequestId={RequestId}", 
                currentUserProfile.Id, followAction.ProfileToFollowId, requestId);

            var result = await _followerService.FollowProfileAsync(currentUserProfile.Id, followAction.ProfileToFollowId);
            
            if (result.Success)
            {
                _logger.LogInformation("[FollowersController.FollowProfile] Follow successful - FollowerProfileId={FollowerProfileId}, FollowedProfileId={FollowedProfileId}, RequestId={RequestId}", 
                    currentUserProfile.Id, followAction.ProfileToFollowId, requestId);

                // Create follow notification for the profile being followed
                try
                {
                    _logger.LogInformation("[FollowersController.FollowProfile] Creating notification - RequestId={RequestId}", requestId);
                    await _notificationService.CreateFollowNotificationAsync(
                        followAction.ProfileToFollowId,
                        currentUserProfile.UserId);
                    _logger.LogInformation("[FollowersController.FollowProfile] Notification created - RequestId={RequestId}", requestId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[FollowersController.FollowProfile] NOTIFICATION_ERROR - ProfileId={ProfileId}, UserId={UserId}, RequestId={RequestId}", 
                        followAction.ProfileToFollowId, currentUserProfile.UserId, requestId);
                    // Don't fail the follow operation if notification creation fails
                }

                var successElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("[FollowersController.FollowProfile] SUCCESS - RequestId={RequestId}, Duration={Duration}ms", 
                    requestId, successElapsed);
                return Ok(result);
            }
            
            var failedElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning("[FollowersController.FollowProfile] FAILED - Result={Result}, RequestId={RequestId}, Duration={Duration}ms", 
                result.Message, requestId, failedElapsed);
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FollowersController.FollowProfile] ERROR - ProfileId={ProfileId}, RequestId={RequestId}, Duration={Duration}ms", 
                followAction?.ProfileToFollowId, requestId, elapsed);
            return StatusCode(500, "An error occurred while following the profile");
        }
    }

    /// <summary>
    /// Unfollow a profile
    /// </summary>
    [HttpDelete("follow/{profileToUnfollowId:guid}")]
    public async Task<ActionResult<FollowResultDto>> UnfollowProfile(Guid profileToUnfollowId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[FollowersController.UnfollowProfile] START - RequestId={RequestId}, TargetProfileId={TargetProfileId}", 
            requestId, profileToUnfollowId);

        try
        {
            var currentUserKeycloakId = GetCurrentUserKeycloakId();
            _logger.LogInformation("[FollowersController.UnfollowProfile] KeycloakId extracted: {KeycloakId}, RequestId={RequestId}", 
                currentUserKeycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(currentUserKeycloakId))
            {
                _logger.LogWarning("[FollowersController.UnfollowProfile] UNAUTHORIZED - No KeycloakId, RequestId={RequestId}", requestId);
                return Unauthorized("User must be authenticated to unfollow profiles");
            }

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(currentUserKeycloakId);
            if (currentUserProfile == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[FollowersController.UnfollowProfile] NO_ACTIVE_PROFILE - KeycloakId={KeycloakId}, RequestId={RequestId}, Duration={Duration}ms", 
                    currentUserKeycloakId, requestId, elapsed);
                return BadRequest("User must have an active profile to unfollow other profiles");
            }

            _logger.LogInformation("[FollowersController.UnfollowProfile] Unfollowing - FollowerProfileId={FollowerProfileId}, TargetProfileId={TargetProfileId}, RequestId={RequestId}", 
                currentUserProfile.Id, profileToUnfollowId, requestId);

            var result = await _followerService.UnfollowProfileAsync(currentUserProfile.Id, profileToUnfollowId);
            
            if (result.Success)
            {
                var successElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("[FollowersController.UnfollowProfile] SUCCESS - RequestId={RequestId}, Duration={Duration}ms", 
                    requestId, successElapsed);
                return Ok(result);
            }
            
            var failedElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning("[FollowersController.UnfollowProfile] FAILED - Result={Result}, RequestId={RequestId}, Duration={Duration}ms", 
                result.Message, requestId, failedElapsed);
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[FollowersController.UnfollowProfile] ERROR - ProfileId={ProfileId}, RequestId={RequestId}, Duration={Duration}ms", 
                profileToUnfollowId, requestId, elapsed);
            return StatusCode(500, "An error occurred while unfollowing the profile");
        }
    }

    /// <summary>
    /// Check if current user is following a profile
    /// </summary>
    [HttpGet("following/{targetProfileId:guid}/status")]
    public async Task<ActionResult<bool>> IsFollowing(Guid targetProfileId)
    {
        try
        {
            var currentUserKeycloakId = GetCurrentUserKeycloakId();
            if (string.IsNullOrEmpty(currentUserKeycloakId))
            {
                return Ok(false); // Anonymous users are not following anyone
            }

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(currentUserKeycloakId);
            if (currentUserProfile == null)
            {
                return Ok(false);
            }

            var isFollowing = await _followerService.IsFollowingAsync(currentUserProfile.Id, targetProfileId);
            return Ok(isFollowing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking follow status for profile {ProfileId}", targetProfileId);
            return StatusCode(500, "An error occurred while checking follow status");
        }
    }

    /// <summary>
    /// Get mutual followers between current user and another profile
    /// </summary>
    [HttpGet("mutual/{otherProfileId:guid}")]
    public async Task<ActionResult<IEnumerable<FollowerProfileDto>>> GetMutualFollowers(Guid otherProfileId)
    {
        try
        {
            var currentUserKeycloakId = GetCurrentUserKeycloakId();
            if (string.IsNullOrEmpty(currentUserKeycloakId))
                return BadRequest("User must be authenticated");

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(currentUserKeycloakId);
            if (currentUserProfile == null)
                return BadRequest("User must have an active profile");

            var mutualFollowers = await _followerService.GetMutualFollowersAsync(currentUserProfile.Id, otherProfileId);
            return Ok(mutualFollowers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mutual followers for profile {ProfileId}", otherProfileId);
            return StatusCode(500, "An error occurred while retrieving mutual followers");
        }
    }

    // ============================================
    // Profile-Scoped Endpoints (for specific profiles)
    // ============================================

    /// <summary>
    /// Get all followers of a specific profile
    /// </summary>
    [HttpGet("profiles/{profileId:guid}/followers")]
    public async Task<ActionResult<IEnumerable<FollowerProfileDto>>> GetFollowersForProfile(Guid profileId)
    {
        try
        {
            var currentUserKeycloakId = GetCurrentUserKeycloakId();
            Guid? currentUserProfileId = null;
            
            if (!string.IsNullOrEmpty(currentUserKeycloakId))
            {
                var currentUserProfile = await _profileService.GetMyActiveProfileAsync(currentUserKeycloakId);
                currentUserProfileId = currentUserProfile?.Id;
            }

            var followers = await _followerService.GetFollowersAsync(profileId, currentUserProfileId);
            return Ok(followers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting followers for profile {ProfileId}", profileId);
            return StatusCode(500, "An error occurred while retrieving followers");
        }
    }

    /// <summary>
    /// Get all profiles that a specific profile is following
    /// </summary>
    [HttpGet("profiles/{profileId:guid}/following")]
    public async Task<ActionResult<IEnumerable<FollowingProfileDto>>> GetFollowingForProfile(Guid profileId)
    {
        try
        {
            var currentUserKeycloakId = GetCurrentUserKeycloakId();
            Guid? currentUserProfileId = null;
            
            if (!string.IsNullOrEmpty(currentUserKeycloakId))
            {
                var currentUserProfile = await _profileService.GetMyActiveProfileAsync(currentUserKeycloakId);
                currentUserProfileId = currentUserProfile?.Id;
            }

            var following = await _followerService.GetFollowingAsync(profileId, currentUserProfileId);
            return Ok(following);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting following for profile {ProfileId}", profileId);
            return StatusCode(500, "An error occurred while retrieving following");
        }
    }

    /// <summary>
    /// Get follower statistics for a specific profile
    /// </summary>
    [HttpGet("profiles/{profileId:guid}/stats")]
    public async Task<ActionResult<FollowerStatsDto>> GetFollowerStatsForProfile(Guid profileId)
    {
        try
        {
            var currentUserKeycloakId = GetCurrentUserKeycloakId();
            Guid? currentUserProfileId = null;
            
            if (!string.IsNullOrEmpty(currentUserKeycloakId))
            {
                var currentUserProfile = await _profileService.GetMyActiveProfileAsync(currentUserKeycloakId);
                currentUserProfileId = currentUserProfile?.Id;
            }

            var stats = await _followerService.GetFollowerStatsAsync(profileId, currentUserProfileId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting follower stats for profile {ProfileId}", profileId);
            return StatusCode(500, "An error occurred while retrieving follower statistics");
        }
    }

    private string GetCurrentUserKeycloakId()
    {
        // Check for mock authentication header (for integration tests)
        if (Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            return keycloakIdHeader.ToString();
        }

        // Check if user is authenticated via claims
        if (User?.Identity?.IsAuthenticated == true)
        {
            var subClaim = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(subClaim))
            {
                return subClaim;
            }

            // Fallback: try to find "user_id" or "id" claims if "sub" is not available
            var userIdClaim = User.FindFirst("user_id")?.Value 
                           ?? User.FindFirst("id")?.Value 
                           ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                return userIdClaim;
            }
        }

        // Only return fallback if we have mock auth header (X-Mock-Auth) indicating this is a test scenario
        if (Request.Headers.ContainsKey("X-Mock-Auth"))
        {
            return "mock-keycloak-user-id";
        }

        // No authentication found
        return null!;
    }
}