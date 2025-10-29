
using Microsoft.AspNetCore.Http;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of followers client
/// </summary>
public class FollowersClient : BaseRepositoryClient, IFollowersClient
{
    private readonly IProfileFollowerService _profileFollowerService;
    private readonly IProfileFollowerRepository _profileFollowerRepository;
    private readonly IProfileService _profileService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<FollowersClient> _logger;

    public FollowersClient(
        IProfileFollowerService profileFollowerService,
        IProfileFollowerRepository profileFollowerRepository,
        IProfileService profileService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<FollowersClient> logger)
    {
        _profileFollowerService = profileFollowerService ?? throw new ArgumentNullException(nameof(profileFollowerService));
        _profileFollowerRepository = profileFollowerRepository ?? throw new ArgumentNullException(nameof(profileFollowerRepository));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Follow operations
    public async Task<FollowResultDto> FollowAsync(FollowActionDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("FollowAsync: User not authenticated");
                return new FollowResultDto 
                { 
                    Success = false, 
                    Message = "User must be authenticated to follow profiles" 
                };
            }

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);
            if (currentUserProfile == null)
            {
                _logger.LogWarning("FollowAsync: No active profile found for user {KeycloakId}", keycloakId);
                return new FollowResultDto 
                { 
                    Success = false, 
                    Message = "User must have an active profile to follow other profiles" 
                };
            }

            _logger.LogInformation("FollowAsync: User {ProfileId} following {TargetProfileId}", 
                currentUserProfile.Id, request.ProfileToFollowId);

            var result = await _profileFollowerService.FollowProfileAsync(
                currentUserProfile.Id, 
                request.ProfileToFollowId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FollowAsync for profile {ProfileId}", request?.ProfileToFollowId);
            return new FollowResultDto 
            { 
                Success = false, 
                Message = "An error occurred while following the profile" 
            };
        }
    }

    public async Task UnfollowAsync(Guid profileToUnfollowId, CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("UnfollowAsync: User not authenticated");
                return;
            }

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);
            if (currentUserProfile == null)
            {
                _logger.LogWarning("UnfollowAsync: No active profile found for user {KeycloakId}", keycloakId);
                return;
            }

            _logger.LogInformation("UnfollowAsync: User {ProfileId} unfollowing {TargetProfileId}", 
                currentUserProfile.Id, profileToUnfollowId);

            await _profileFollowerService.UnfollowProfileAsync(
                currentUserProfile.Id, 
                profileToUnfollowId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UnfollowAsync for profile {ProfileId}", profileToUnfollowId);
        }
    }

    // Query operations
    public async Task<IEnumerable<FollowerProfileDto>> GetFollowersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("GetFollowersAsync: User not authenticated");
                return new List<FollowerProfileDto>();
            }

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);
            if (currentUserProfile == null)
            {
                _logger.LogWarning("GetFollowersAsync: No active profile found");
                return new List<FollowerProfileDto>();
            }

            return await _profileFollowerService.GetFollowersAsync(currentUserProfile.Id, currentUserProfile.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFollowersAsync");
            return new List<FollowerProfileDto>();
        }
    }

    public async Task<IEnumerable<FollowingProfileDto>> GetFollowingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("GetFollowingAsync: User not authenticated");
                return new List<FollowingProfileDto>();
            }

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);
            if (currentUserProfile == null)
            {
                _logger.LogWarning("GetFollowingAsync: No active profile found");
                return new List<FollowingProfileDto>();
            }

            return await _profileFollowerService.GetFollowingAsync(currentUserProfile.Id, currentUserProfile.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFollowingAsync");
            return new List<FollowingProfileDto>();
        }
    }

    public async Task<FollowerStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("GetStatsAsync: User not authenticated");
                return new FollowerStatsDto();
            }

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);
            if (currentUserProfile == null)
            {
                _logger.LogWarning("GetStatsAsync: No active profile found");
                return new FollowerStatsDto();
            }

            return await _profileFollowerService.GetFollowerStatsAsync(currentUserProfile.Id, currentUserProfile.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetStatsAsync");
            return new FollowerStatsDto();
        }
    }

    // Status checks
    public async Task<bool> GetFollowingStatusAsync(Guid targetProfileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                return false;
            }

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);
            if (currentUserProfile == null)
            {
                return false;
            }

            return await _profileFollowerService.IsFollowingAsync(currentUserProfile.Id, targetProfileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetFollowingStatusAsync for profile {ProfileId}", targetProfileId);
            return false;
        }
    }

    public async Task<IEnumerable<ProfileFollowerDto>> GetMutualFollowersAsync(Guid otherProfileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                return new List<ProfileFollowerDto>();
            }

            var currentUserProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);
            if (currentUserProfile == null)
            {
                return new List<ProfileFollowerDto>();
            }

            // Service returns FollowerProfileDto but interface expects ProfileFollowerDto
            // This seems like a mismatch - for now, return empty list
            // TODO: Fix interface or implement proper mapping
            _logger.LogWarning("GetMutualFollowersAsync: Interface mismatch between FollowerProfileDto and ProfileFollowerDto");
            return new List<ProfileFollowerDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMutualFollowersAsync for profile {ProfileId}", otherProfileId);
            return new List<ProfileFollowerDto>();
        }
    }

    // Profile-specific queries
    public async Task<FollowerStatsDto> GetStatsForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetStatsForProfileAsync: {ProfileId}", profileId);
        // For server-side rendering, we don't have current user context - API will handle it
        return await _profileFollowerService.GetFollowerStatsAsync(profileId, null);
    }

    public async Task<IEnumerable<FollowerProfileDto>> GetFollowersForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetFollowersForProfileAsync: {ProfileId}", profileId);
        return await _profileFollowerService.GetFollowersAsync(profileId, null);
    }

    public async Task<IEnumerable<FollowingProfileDto>> GetFollowingForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetFollowingForProfileAsync: {ProfileId}", profileId);
        return await _profileFollowerService.GetFollowingAsync(profileId, null);
    }

    /// <summary>
    /// Extracts the Keycloak ID from the current HTTP context
    /// </summary>
    /// <returns>The user's Keycloak ID, or null if not authenticated</returns>
    private string? GetKeycloakIdFromContext()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext?.User == null)
        {
            return null;
        }

        // Check for mock authentication header (for integration tests)
        if (httpContext.Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            return keycloakIdHeader.ToString();
        }

        // Check if user is authenticated via claims
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var subClaim = httpContext.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(subClaim))
            {
                return subClaim;
            }

            // Fallback: try to find "user_id" or "id" claims if "sub" is not available
            var userIdClaim = httpContext.User.FindFirst("user_id")?.Value 
                           ?? httpContext.User.FindFirst("id")?.Value 
                           ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim;
        }

        return null;
    }
}
