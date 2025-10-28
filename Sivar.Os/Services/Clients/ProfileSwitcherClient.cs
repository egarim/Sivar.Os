using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;
using Sivar.Os.Client.Services;
using Sivar.Os.Shared.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of profile switcher client
/// Uses repositories and services directly instead of HttpClient
/// </summary>
public class ProfileSwitcherClient : BaseRepositoryClient, IProfileSwitcherService
{
    private readonly IProfileService _profileService;
    private readonly IProfileTypeService _profileTypeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ProfileSwitcherClient> _logger;

    public ProfileSwitcherClient(
        IProfileService profileService,
        IProfileTypeService profileTypeService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProfileSwitcherClient> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _profileTypeService = profileTypeService ?? throw new ArgumentNullException(nameof(profileTypeService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get the Keycloak ID from the current user's claims
    /// </summary>
    private string GetCurrentUserKeycloakId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        // Use "sub" claim which is the standard Keycloak subject identifier
        // This matches how PostsClient and other services extract the Keycloak ID
        var keycloakId = user?.Claims?.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (string.IsNullOrEmpty(keycloakId))
        {
            _logger.LogWarning("[ProfileSwitcherClient] Unable to extract Keycloak ID from user claims");
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        return keycloakId;
    }

    /// <inheritdoc/>
    public async Task<List<ProfileDto>> GetUserProfilesAsync()
    {
        try
        {
            _logger.LogInformation("[ProfileSwitcherClient] Getting user profiles");

            var keycloakId = GetCurrentUserKeycloakId();
            var profiles = await _profileService.GetMyProfilesAsync(keycloakId);

            var profileList = profiles?.ToList() ?? new List<ProfileDto>();
            _logger.LogInformation("[ProfileSwitcherClient] Retrieved {ProfileCount} profiles", profileList.Count);

            return profileList;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("[ProfileSwitcherClient] Authentication error: {Message}", ex.Message);
            return new List<ProfileDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError("[ProfileSwitcherClient] Error getting profiles: {Message}", ex.Message);
            return new List<ProfileDto>();
        }
    }

    /// <inheritdoc/>
    public async Task<ProfileDto?> GetActiveProfileAsync()
    {
        try
        {
            _logger.LogInformation("[ProfileSwitcherClient] Getting active profile");

            var keycloakId = GetCurrentUserKeycloakId();
            var activeProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);

            if (activeProfile != null)
            {
                _logger.LogInformation("[ProfileSwitcherClient] Retrieved active profile: {ProfileId}", activeProfile.Id);
            }
            else
            {
                _logger.LogWarning("[ProfileSwitcherClient] No active profile found for user");
            }

            return activeProfile;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("[ProfileSwitcherClient] Authentication error: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError("[ProfileSwitcherClient] Error getting active profile: {Message}", ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SwitchProfileAsync(Guid profileId)
    {
        try
        {
            _logger.LogInformation("[ProfileSwitcherClient] Switching to profile: {ProfileId}", profileId);

            var keycloakId = GetCurrentUserKeycloakId();
            var success = await _profileService.SetActiveProfileAsync(keycloakId, profileId);

            if (success)
            {
                _logger.LogInformation("[ProfileSwitcherClient] Successfully switched to profile: {ProfileId}", profileId);
            }
            else
            {
                _logger.LogWarning("[ProfileSwitcherClient] Failed to switch profile: {ProfileId}", profileId);
            }

            return success;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("[ProfileSwitcherClient] Authentication error: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError("[ProfileSwitcherClient] Error switching profile: {Message}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<ProfileDto?> CreateProfileAsync(CreateAnyProfileDto request)
    {
        try
        {
            _logger.LogInformation("[ProfileSwitcherClient] Creating new profile");

            var keycloakId = GetCurrentUserKeycloakId();

            // Convert CreateAnyProfileDto to CreateProfileDto
            // CreateProfileDto is used by ProfileService which automatically determines profile type from metadata
            var createDto = new CreateProfileDto
            {
                DisplayName = request.DisplayName,
                Bio = request.Bio ?? string.Empty,
                Avatar = request.Avatar ?? string.Empty,
                AvatarFileId = request.AvatarFileId,
                Location = request.Location,
                IsPublic = request.VisibilityLevel == VisibilityLevel.Public,
                VisibilityLevel = request.VisibilityLevel,
                Tags = request.Tags ?? new List<string>(),
                SocialMediaLinks = request.SocialMediaLinks ?? new Dictionary<string, string>(),
                Metadata = request.Metadata
            };

            // Create the profile, passing the ProfileTypeId from the request
            var profile = await _profileService.CreateProfileAsync(createDto, keycloakId, request.ProfileTypeId);

            if (profile != null)
            {
                _logger.LogInformation("[ProfileSwitcherClient] Successfully created profile: {ProfileId}", profile.Id);

                // If SetAsActive is true, set it as the active profile
                if (request.SetAsActive)
                {
                    await _profileService.SetActiveProfileAsync(keycloakId, profile.Id);
                    _logger.LogInformation("[ProfileSwitcherClient] Set profile {ProfileId} as active", profile.Id);
                }
            }
            else
            {
                _logger.LogWarning("[ProfileSwitcherClient] Failed to create profile");
            }

            return profile;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("[ProfileSwitcherClient] Authentication error: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError("[ProfileSwitcherClient] Error creating profile: {Message}", ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ProfileTypeDto>> GetProfileTypesAsync()
    {
        try
        {
            _logger.LogInformation("[ProfileSwitcherClient] Getting profile types");

            var profileTypes = await _profileTypeService.GetActiveProfileTypesAsync();
            var typeList = profileTypes?.ToList() ?? new List<ProfileTypeDto>();

            _logger.LogInformation("[ProfileSwitcherClient] Retrieved {TypeCount} profile types", typeList.Count);

            return typeList;
        }
        catch (Exception ex)
        {
            _logger.LogError("[ProfileSwitcherClient] Error getting profile types: {Message}", ex.Message);
            return new List<ProfileTypeDto>();
        }
    }
}
