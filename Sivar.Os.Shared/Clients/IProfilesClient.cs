using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client for profile operations
/// </summary>
public interface IProfilesClient
{
    // My profiles (authenticated user)
    Task<ProfileDto> GetMyProfileAsync(CancellationToken cancellationToken = default);
    Task<ProfileDto> CreateMyProfileAsync(CreateProfileDto request, CancellationToken cancellationToken = default);
    Task<ProfileDto> UpdateMyProfileAsync(UpdateProfileDto request, CancellationToken cancellationToken = default);
    Task DeleteMyProfileAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProfileDto>> GetAllMyProfilesAsync(CancellationToken cancellationToken = default);
    Task<ActiveProfileDto> GetMyActiveProfileAsync(CancellationToken cancellationToken = default);
    Task<ActiveProfileDto> SetMyActiveProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    // Profile management (admin)
    Task<ProfileDto> CreateProfileAsync(CreateAnyProfileDto request, CancellationToken cancellationToken = default);
    Task<ProfileDto> GetProfileAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<ProfileDto> UpdateProfileAsync(Guid profileId, UpdateProfileDto request, CancellationToken cancellationToken = default);
    Task DeleteProfileAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<ProfileDto> SetProfileActiveAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<ProfileDto> ActivateProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    // Discovery
    Task<IEnumerable<ProfileSummaryDto>> GetPublicProfilesAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<ProfileDto> GetProfileByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProfileSearchDto>> SearchProfilesAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProfileSummaryDto>> GetProfilesByLocationAsync(string location, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProfileSummaryDto>> GetProfilesByTagsAsync(string tags, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProfileSummaryDto>> GetPopularProfilesAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProfileSummaryDto>> GetRecentProfilesAsync(int limit = 10, CancellationToken cancellationToken = default);

    // Statistics
    Task<ProfileStatisticsDto> GetProfileStatisticsAsync(CancellationToken cancellationToken = default);
}
