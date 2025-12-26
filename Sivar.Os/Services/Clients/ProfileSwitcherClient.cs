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
    
    // Request-scoped caching to prevent concurrent DbContext access
    private ProfileDto? _cachedActiveProfile;
    private bool _activeProfileLoaded;
    private bool _activeProfileLoadedSuccessfully; // Tracks if load was successful (vs auth failure)
    private List<ProfileDto>? _cachedProfiles;
    private bool _profilesLoadedSuccessfully;
    private readonly SemaphoreSlim _profileLock = new(1, 1);
    private readonly SemaphoreSlim _activeProfileLock = new(1, 1);

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

    /// <inheritdoc/>
    /// <remarks>
    /// Server-side implementation doesn't cache state - always fetches fresh from DB per request
    /// </remarks>
    public ProfileDto? CurrentActiveProfile => null;

    /// <inheritdoc/>
    /// <remarks>
    /// Server-side implementation doesn't fire events - state is per-request, not persistent
    /// </remarks>
    public event Action? OnActiveProfileChanged;

    /// <summary>
    /// Get the Keycloak ID from the current user's claims
    /// </summary>
    /// <returns>Keycloak ID or null if not authenticated</returns>
    private string? GetCurrentUserKeycloakId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Check for mock authentication header (for integration tests)
        if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader) == true)
        {
            return keycloakIdHeader.ToString();
        }
        
        // Use "sub" claim which is the standard Keycloak subject identifier
        // This matches how PostsClient and other services extract the Keycloak ID
        var keycloakId = user?.Claims?.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (string.IsNullOrEmpty(keycloakId))
        {
            // Try fallback claims
            keycloakId = user?.FindFirst("user_id")?.Value 
                      ?? user?.FindFirst("id")?.Value 
                      ?? user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        if (string.IsNullOrEmpty(keycloakId))
        {
            _logger.LogDebug("[ProfileSwitcherClient] Unable to extract Keycloak ID from user claims - user may not be fully authenticated yet");
            return null;
        }

        return keycloakId;
    }

    /// <inheritdoc/>
    public async Task<List<ProfileDto>> GetUserProfilesAsync()
    {
        // Use lock to prevent concurrent DbContext access
        await _profileLock.WaitAsync();
        try
        {
            // Return cached value only if load was successful (not an auth failure)
            if (_profilesLoadedSuccessfully && _cachedProfiles != null)
            {
                _logger.LogDebug("[ProfileSwitcherClient] Returning cached profiles ({Count})", _cachedProfiles.Count);
                return _cachedProfiles;
            }
            
            _logger.LogInformation("[ProfileSwitcherClient] Getting user profiles");

            var keycloakId = GetCurrentUserKeycloakId();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogDebug("[ProfileSwitcherClient] No keycloak ID available, returning empty list (will retry on next call)");
                return new List<ProfileDto>();
            }
            
            var profiles = await _profileService.GetMyProfilesAsync(keycloakId);

            var profileList = profiles?.ToList() ?? new List<ProfileDto>();
            _cachedProfiles = profileList;
            _profilesLoadedSuccessfully = true; // Mark as successfully loaded
            
            _logger.LogInformation("[ProfileSwitcherClient] Retrieved {ProfileCount} profiles", profileList.Count);

            return profileList;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("[ProfileSwitcherClient] Authentication error: {Message}", ex.Message);
            // Don't cache on auth failure - allow retry
            return new List<ProfileDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError("[ProfileSwitcherClient] Error getting profiles: {Message}", ex.Message);
            // Don't cache on error - allow retry
            return new List<ProfileDto>();
        }
        finally
        {
            _profileLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ProfileDto?> GetActiveProfileAsync()
    {
        // Use lock to prevent concurrent DbContext access
        await _activeProfileLock.WaitAsync();
        try
        {
            // Return cached value only if load was successful (not an auth failure)
            if (_activeProfileLoadedSuccessfully)
            {
                _logger.LogDebug("[ProfileSwitcherClient] Returning cached active profile: {DisplayName}", 
                    _cachedActiveProfile?.DisplayName ?? "(null)");
                return _cachedActiveProfile;
            }
            
            _logger.LogInformation("[ProfileSwitcherClient] Getting active profile");

            var keycloakId = GetCurrentUserKeycloakId();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogDebug("[ProfileSwitcherClient] No keycloak ID available, returning null (will retry on next call)");
                return null;
            }
            
            _logger.LogInformation("[ProfileSwitcherClient] KeycloakId: {KeycloakId}", keycloakId);
            
            var activeProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);

            // Cache the result - mark as successfully loaded
            _cachedActiveProfile = activeProfile;
            _activeProfileLoaded = true;
            _activeProfileLoadedSuccessfully = true;

            if (activeProfile != null)
            {
                _logger.LogInformation("[ProfileSwitcherClient] Retrieved active profile: {ProfileId}, DisplayName: '{DisplayName}', IsActive: {IsActive}", 
                    activeProfile.Id, activeProfile.DisplayName ?? "(null)", activeProfile.IsActive);
            }
            else
            {
                _logger.LogWarning("[ProfileSwitcherClient] No active profile found for user with KeycloakId: {KeycloakId}", keycloakId);
            }

            return activeProfile;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("[ProfileSwitcherClient] Authentication error: {Message}", ex.Message);
            // Don't cache on auth failure - allow retry
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError("[ProfileSwitcherClient] Error getting active profile: {Message}", ex.Message);
            // Don't cache on error - allow retry
            return null;
        }
        finally
        {
            _activeProfileLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SwitchProfileAsync(Guid profileId)
    {
        try
        {
            _logger.LogInformation("[ProfileSwitcherClient] Switching to profile: {ProfileId}", profileId);

            var keycloakId = GetCurrentUserKeycloakId();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ProfileSwitcherClient] Cannot switch profile - no keycloak ID available");
                return false;
            }
            
            var success = await _profileService.SetActiveProfileAsync(keycloakId, profileId);

            if (success)
            {
                // Invalidate cache on successful switch
                _cachedActiveProfile = null;
                _activeProfileLoaded = false;
                _activeProfileLoadedSuccessfully = false;
                _cachedProfiles = null;
                _profilesLoadedSuccessfully = false;
                
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
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ProfileSwitcherClient] Cannot create profile - no keycloak ID available");
                return null;
            }

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
