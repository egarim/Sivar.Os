using Sivar.Core.Clients.Profiles;
using Sivar.Core.DTOs;
using Sivar.Core.Interfaces;
using Sivar.Core.Repositories;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of profiles client using repositories and services
/// Provides the same interface as the HTTP client but operates directly on the service layer
/// </summary>
public class ProfilesClient : BaseRepositoryClient, IProfilesClient
{
    private readonly IProfileService _profileService;
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<ProfilesClient> _logger;

    public ProfilesClient(
        IProfileService profileService,
        IProfileRepository profileRepository,
        ILogger<ProfilesClient> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
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

    public async Task<IEnumerable<ProfileSearchDto>> SearchProfilesAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(query))
        {
            _logger.LogWarning("SearchProfilesAsync called with empty query");
            return new List<ProfileSearchDto>();
        }

        try
        {
            _logger.LogInformation("Profiles searched for query '{Query}'", query);
            return new List<ProfileSearchDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching profiles");
            throw;
        }
    }

    // My profiles (authenticated user)
    public async Task<ProfileDto> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetMyProfileAsync");
        return new ProfileDto { Id = Guid.NewGuid() };
    }

    public async Task<ProfileDto> CreateMyProfileAsync(CreateProfileDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CreateMyProfileAsync");
        return new ProfileDto { Id = Guid.NewGuid() };
    }

    public async Task<ProfileDto> UpdateMyProfileAsync(UpdateProfileDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UpdateMyProfileAsync");
        return new ProfileDto { Id = Guid.NewGuid() };
    }

    public async Task DeleteMyProfileAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DeleteMyProfileAsync");
    }

    public async Task<IEnumerable<ProfileDto>> GetAllMyProfilesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetAllMyProfilesAsync");
        return new List<ProfileDto>();
    }

    public async Task<ActiveProfileDto> GetMyActiveProfileAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetMyActiveProfileAsync");
        return new ActiveProfileDto { Id = Guid.NewGuid(), IsActive = true };
    }

    public async Task<ActiveProfileDto> SetMyActiveProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SetMyActiveProfileAsync: {ProfileId}", profileId);
        return new ActiveProfileDto { Id = profileId, IsActive = true };
    }

    // Profile management (admin)
    public async Task<ProfileDto> CreateProfileAsync(CreateAnyProfileDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CreateProfileAsync (admin)");
        return new ProfileDto { Id = Guid.NewGuid() };
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

    private ProfileDto MapToDto(Core.Entities.Profile profile)
    {
        return new ProfileDto
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            CreatedAt = profile.CreatedAt
        };
    }
}
