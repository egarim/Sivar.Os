using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;


namespace Sivar.Os.Controllers;

/// <summary>
/// Controller for managing profile follower relationships
/// </summary>
[ApiController]
[Route("api/profiles/{profileId:guid}")]
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
    /// Get all followers of a profile
    /// </summary>
    [HttpGet("followers")]
    public async Task<ActionResult<IEnumerable<FollowerProfileDto>>> GetFollowers(Guid profileId)
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
    /// Get all profiles that a profile is following
    /// </summary>
    [HttpGet("following")]
    public async Task<ActionResult<IEnumerable<FollowingProfileDto>>> GetFollowing(Guid profileId)
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
    /// Get follower statistics for a profile
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<FollowerStatsDto>> GetFollowerStats(Guid profileId)
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

            var stats = await _followerService.GetFollowerStatsAsync(profileId, currentUserProfileId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting follower stats for profile {ProfileId}", profileId);
            return StatusCode(500, "An error occurred while retrieving follower statistics");
        }
    }

    /// <summary>
    /// Follow a profile
    /// </summary>
    [HttpPost("follow")]
    public async Task<ActionResult<FollowResultDto>> FollowProfile(Guid profileId, [FromBody] FollowActionDto followAction)
    {
        try
        {
            var currentUserKeycloakId = GetCurrentUserKeycloakId();
            if (string.IsNullOrEmpty(currentUserKeycloakId))
            {
                return Unauthorized("User must be authenticated to follow profiles");
            }

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(currentUserKeycloakId);
            if (currentUserProfile == null)
            {
                return BadRequest("User must have an active profile to follow other profiles");
            }

            var result = await _followerService.FollowProfileAsync(currentUserProfile.Id, followAction.ProfileToFollowId);
            
            if (result.Success)
            {
                // Create follow notification for the profile being followed
                try
                {
                    await _notificationService.CreateFollowNotificationAsync(
                        followAction.ProfileToFollowId,
                        currentUserProfile.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating follow notification for profile {ProfileId} followed by user {UserId}", 
                        followAction.ProfileToFollowId, currentUserProfile.UserId);
                    // Don't fail the follow operation if notification creation fails
                }

                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error following profile {ProfileId}", followAction?.ProfileToFollowId);
            return StatusCode(500, "An error occurred while following the profile");
        }
    }

    /// <summary>
    /// Unfollow a profile
    /// </summary>
    [HttpDelete("follow/{profileToUnfollowId:guid}")]
    public async Task<ActionResult<FollowResultDto>> UnfollowProfile(Guid profileId, Guid profileToUnfollowId)
    {
        try
        {
            var currentUserKeycloakId = GetCurrentUserKeycloakId();
            if (string.IsNullOrEmpty(currentUserKeycloakId))
            {
                return Unauthorized("User must be authenticated to unfollow profiles");
            }

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(currentUserKeycloakId);
            if (currentUserProfile == null)
            {
                return BadRequest("User must have an active profile to unfollow other profiles");
            }

            var result = await _followerService.UnfollowProfileAsync(currentUserProfile.Id, profileToUnfollowId);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unfollowing profile {ProfileId}", profileToUnfollowId);
            return StatusCode(500, "An error occurred while unfollowing the profile");
        }
    }

    /// <summary>
    /// Check if current user is following a profile
    /// </summary>
    [HttpGet("following/{targetProfileId:guid}/status")]
    public async Task<ActionResult<bool>> IsFollowing(Guid profileId, Guid targetProfileId)
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
    /// Get mutual followers between two profiles
    /// </summary>
    [HttpGet("mutual/{otherProfileId:guid}")]
    public async Task<ActionResult<IEnumerable<FollowerProfileDto>>> GetMutualFollowers(Guid profileId, Guid otherProfileId)
    {
        try
        {
            var mutualFollowers = await _followerService.GetMutualFollowersAsync(profileId, otherProfileId);
            return Ok(mutualFollowers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mutual followers for profiles {ProfileId1} and {ProfileId2}", profileId, otherProfileId);
            return StatusCode(500, "An error occurred while retrieving mutual followers");
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