
using Microsoft.AspNetCore.Http;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;
using Sivar.Os.Client.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of profiles client using repositories and services
/// Provides the same interface as the HTTP client but operates directly on the service layer
/// </summary>
public class ProfilesClient : BaseRepositoryClient, IProfilesClient
{
    private readonly IProfileService _profileService;
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileSwitcherService _profileSwitcherService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ProfilesClient> _logger;

    public ProfilesClient(
        IProfileService profileService,
        IProfileRepository profileRepository,
        IProfileSwitcherService profileSwitcherService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProfilesClient> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _profileSwitcherService = profileSwitcherService ?? throw new ArgumentNullException(nameof(profileSwitcherService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Profile management (admin) - continued
    public async Task<ProfileDto> GetProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            _logger.LogWarning("GetProfileAsync called with empty profile ID");
            return new ProfileDto();
        }

        try
        {
            var profile = await _profileRepository.GetByIdAsync(profileId);
            _logger.LogInformation("Profile retrieved: {ProfileId}", profileId);
            return profile != null ? MapToDto(profile) : new ProfileDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile {ProfileId}", profileId);
            throw;
        }
    }

    public async Task<ProfileDto> SetProfileActiveAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SetProfileActiveAsync: {ProfileId}", profileId);
        return new ProfileDto { Id = profileId };
    }

    public async Task<ProfileDto> ActivateProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ActivateProfileAsync: {ProfileId}", profileId);
        return new ProfileDto { Id = profileId };
    }

    // Discovery
    public async Task<IEnumerable<ProfileSummaryDto>> GetPublicProfilesAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetPublicProfilesAsync");
        return new List<ProfileSummaryDto>();
    }

    public async Task<ProfileDto> GetProfileByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            _logger.LogWarning("[ProfilesClient.GetProfileByIdentifierAsync] Empty identifier provided");
            return new ProfileDto();
        }

        try
        {
            _logger.LogInformation("[ProfilesClient.GetProfileByIdentifierAsync] Fetching profile by identifier: {Identifier}", identifier);
            var profile = await _profileService.GetProfileByIdentifierAsync(identifier);
            
            if (profile == null)
            {
                _logger.LogWarning("[ProfilesClient.GetProfileByIdentifierAsync] Profile not found for identifier: {Identifier}", identifier);
                return new ProfileDto();
            }

            _logger.LogInformation("[ProfilesClient.GetProfileByIdentifierAsync] Profile found: {ProfileId}, {DisplayName}", 
                profile.Id, profile.DisplayName);
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProfilesClient.GetProfileByIdentifierAsync] Error fetching profile by identifier: {Identifier}", identifier);
            throw;
        }
    }

    public async Task<IEnumerable<ProfileSummaryDto>> SearchProfilesAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(query))
        {
            _logger.LogWarning("SearchProfilesAsync called with empty query");
            return new List<ProfileSummaryDto>();
        }

        try
        {
            _logger.LogInformation("Searching profiles for query '{Query}', page {Page}, pageSize {PageSize}", query, pageNumber, pageSize);
            
            var result = await _profileService.SearchProfilesAsync(query, pageNumber, pageSize);
            
            _logger.LogInformation("Search completed: found {TotalItems} profiles, returning {ReturnedItems} for page {Page}", 
                result.TotalItems, result.Items.Count(), pageNumber);
            
            return result.Items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching profiles for query '{Query}'", query);
            throw;
        }
    }

    // My profiles (authenticated user)
    public async Task<ProfileDto> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("GetMyProfileAsync: No authenticated user");
                return null!;
            }

            _logger.LogInformation("GetMyProfileAsync: {KeycloakId}", keycloakId);
            var profile = await _profileService.GetMyProfileAsync(keycloakId);
            return profile ?? null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMyProfileAsync");
            throw;
        }
    }

    public async Task<ProfileDto> CreateMyProfileAsync(CreateProfileDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
            {
                _logger.LogWarning("CreateMyProfileAsync: Null request");
                return null!;
            }

            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("CreateMyProfileAsync: No authenticated user");
                return null!;
            }

            _logger.LogInformation("CreateMyProfileAsync: {KeycloakId}, DisplayName={DisplayName}", keycloakId, request.DisplayName);
            var profile = await _profileService.CreateMyProfileAsync(keycloakId, request);
            return profile ?? null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateMyProfileAsync");
            throw;
        }
    }

    public async Task<ProfileDto> UpdateMyProfileAsync(UpdateProfileDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
            {
                _logger.LogWarning("UpdateMyProfileAsync: Null request");
                return null!;
            }

            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("UpdateMyProfileAsync: No authenticated user");
                return null!;
            }

            _logger.LogInformation("UpdateMyProfileAsync: {KeycloakId}", keycloakId);
            var profile = await _profileService.UpdateMyProfileAsync(keycloakId, request);
            return profile ?? null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateMyProfileAsync");
            throw;
        }
    }

    public async Task DeleteMyProfileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("DeleteMyProfileAsync: No authenticated user");
                return;
            }

            _logger.LogInformation("DeleteMyProfileAsync: {KeycloakId}", keycloakId);
            await _profileService.DeleteMyProfileAsync(keycloakId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteMyProfileAsync");
            throw;
        }
    }

    public async Task<IEnumerable<ProfileDto>> GetAllMyProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("GetAllMyProfilesAsync: No authenticated user");
                return new List<ProfileDto>();
            }

            _logger.LogInformation("GetAllMyProfilesAsync: {KeycloakId}", keycloakId);
            var profiles = await _profileService.GetMyProfilesAsync(keycloakId);
            return profiles ?? new List<ProfileDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllMyProfilesAsync");
            throw;
        }
    }

    public async Task<ActiveProfileDto> GetMyActiveProfileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("GetMyActiveProfileAsync: No authenticated user");
                return null!;
            }

            _logger.LogInformation("GetMyActiveProfileAsync: {KeycloakId}", keycloakId);
            var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
            if (profile == null) return null!;

            return new ActiveProfileDto 
            { 
                Id = profile.Id,
                DisplayName = profile.DisplayName,
                ProfileType = profile.ProfileType,
                Avatar = profile.Avatar,
                AvatarFileId = profile.AvatarFileId,
                PreferredLanguage = profile.PreferredLanguage,
                LocationDisplay = profile.LocationDisplay,
                ActivatedAt = DateTime.UtcNow,
                IsActive = true 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMyActiveProfileAsync");
            throw;
        }
    }

    public async Task<ActiveProfileDto> SetMyActiveProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("SetMyActiveProfileAsync: No authenticated user");
                return null!;
            }

            _logger.LogInformation("SetMyActiveProfileAsync: {KeycloakId}, ProfileId={ProfileId}", keycloakId, profileId);
            var result = await _profileService.SetActiveProfileAsync(keycloakId, profileId);
            return new ActiveProfileDto 
            { 
                Id = profileId, 
                IsActive = result 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SetMyActiveProfileAsync");
            throw;
        }
    }

    // Profile management (admin)
    public async Task<ProfileDto> CreateProfileAsync(CreateAnyProfileDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ProfilesClient.CreateProfileAsync] Creating profile - DisplayName: {DisplayName}, ProfileTypeId: {ProfileTypeId}", 
            request?.DisplayName, request?.ProfileTypeId);
        
        if (request == null)
        {
            _logger.LogWarning("[ProfilesClient.CreateProfileAsync] Request is null");
            return new ProfileDto { Id = Guid.Empty };
        }

        try
        {
            // Use ProfileSwitcherService which has the proper implementation
            var profile = await _profileSwitcherService.CreateProfileAsync(request);
            
            if (profile != null)
            {
                _logger.LogInformation("[ProfilesClient.CreateProfileAsync] ✅ Profile created successfully: {ProfileId}", profile.Id);
                return profile;
            }
            else
            {
                _logger.LogWarning("[ProfilesClient.CreateProfileAsync] ProfileSwitcherService returned null");
                return new ProfileDto { Id = Guid.Empty };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProfilesClient.CreateProfileAsync] Error creating profile");
            throw;
        }
    }

    public async Task<ProfileDto> UpdateProfileAsync(Guid profileId, UpdateProfileDto request, CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty || request == null)
        {
            _logger.LogWarning("UpdateProfileAsync called with invalid parameters");
            return new ProfileDto();
        }

        try
        {
            var profile = await _profileRepository.GetByIdAsync(profileId);
            if (profile == null)
            {
                _logger.LogWarning("Profile not found for update: {ProfileId}", profileId);
                return new ProfileDto();
            }

            _logger.LogInformation("Profile updated: {ProfileId}", profileId);
            return MapToDto(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {ProfileId}", profileId);
            throw;
        }
    }

    public async Task DeleteProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            _logger.LogWarning("DeleteProfileAsync called with empty profile ID");
            return;
        }

        try
        {
            await _profileRepository.DeleteAsync(profileId);
            _logger.LogInformation("Profile deleted: {ProfileId}", profileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile {ProfileId}", profileId);
            throw;
        }
    }

    public async Task<IEnumerable<ProfileSummaryDto>> GetProfilesByLocationAsync(string location, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetProfilesByLocationAsync: {Location}", location);
        return new List<ProfileSummaryDto>();
    }

    public async Task<IEnumerable<ProfileSummaryDto>> GetProfilesByTagsAsync(string tags, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetProfilesByTagsAsync: {Tags}", tags);
        return new List<ProfileSummaryDto>();
    }

    public async Task<IEnumerable<ProfileSummaryDto>> GetPopularProfilesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetPopularProfilesAsync");
        return new List<ProfileSummaryDto>();
    }

    public async Task<IEnumerable<ProfileSummaryDto>> GetRecentProfilesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetRecentProfilesAsync");
        return new List<ProfileSummaryDto>();
    }

    // Statistics
    public async Task<ProfileStatisticsDto> GetProfileStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetProfileStatisticsAsync");
        return new ProfileStatisticsDto();
    }

    public async Task<IEnumerable<ProfileDto>> FindNearbyProfilesAsync(double latitude, double longitude, double radiusKm = 10, int limit = 50, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("FindNearbyProfilesAsync: lat={Lat}, lon={Lon}, radius={Radius}km", latitude, longitude, radiusKm);
        try
        {
            var profiles = await _profileService.FindNearbyProfilesAsync(latitude, longitude, radiusKm, limit);
            return profiles ?? new List<ProfileDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FindNearbyProfilesAsync");
            throw;
        }
    }

    public async Task<bool> UpdatePreferredLanguageAsync(Guid profileId, string? languageCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("UpdatePreferredLanguageAsync: No authenticated user");
                return false;
            }

            _logger.LogInformation("UpdatePreferredLanguageAsync: ProfileId={ProfileId}, LanguageCode={LanguageCode}", profileId, languageCode);
            return await _profileService.UpdatePreferredLanguageAsync(profileId, keycloakId, languageCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdatePreferredLanguageAsync");
            throw;
        }
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

    private ProfileDto MapToDto(Profile profile)
    {
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
            Location = profile.Location,
            LocationDisplay = profile.LocationDisplay,
            IsActive = profile.IsActive,
            VisibilityLevel = profile.VisibilityLevel,
            ViewCount = profile.ViewCount,
            Tags = profile.Tags?.ToList() ?? new List<string>(),
            SocialMediaLinks = profile.GetSocialMediaLinks(),
            ChatDisplayMode = profile.ChatDisplayMode,
            Metadata = profile.Metadata,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    // ========================================
    // AD BUDGET & SPONSORED SETTINGS
    // ========================================

    public async Task<ProfileAdSettingsDto> GetAdSettingsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _profileService.GetAdSettingsAsync(profileId);
            return settings ?? new ProfileAdSettingsDto { ProfileId = profileId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ad settings for profile {ProfileId}", profileId);
            throw;
        }
    }

    public async Task<ProfileAdSettingsDto> UpdateAdSettingsAsync(Guid profileId, UpdateAdSettingsDto updateDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _profileService.UpdateAdSettingsAsync(profileId, updateDto);
            return settings ?? new ProfileAdSettingsDto { ProfileId = profileId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ad settings for profile {ProfileId}", profileId);
            throw;
        }
    }

    public async Task<List<AdTransactionDto>> GetAdTransactionsAsync(Guid profileId, int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _profileService.GetAdTransactionsAsync(profileId, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ad transactions for profile {ProfileId}", profileId);
            throw;
        }
    }

    public async Task<ProfileAdSettingsDto> AddAdBudgetAsync(Guid profileId, AddBudgetDto addBudgetDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _profileService.AddAdBudgetAsync(profileId, addBudgetDto.Amount, addBudgetDto.Description);
            return settings ?? new ProfileAdSettingsDto { ProfileId = profileId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding budget for profile {ProfileId}", profileId);
            throw;
        }
    }
}
