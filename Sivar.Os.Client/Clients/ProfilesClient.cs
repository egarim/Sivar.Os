
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of profiles client
/// </summary>
public class ProfilesClient : BaseClient, IProfilesClient
{
    public ProfilesClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    // My profiles
    public async Task<ProfileDto> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<ProfileDto>("api/profiles/my", cancellationToken);
    }

    public async Task<ProfileDto> CreateMyProfileAsync(CreateProfileDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ProfileDto>("api/profiles/my", request, cancellationToken);
    }

    public async Task<ProfileDto> UpdateMyProfileAsync(UpdateProfileDto request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<ProfileDto>("api/profiles/my", request, cancellationToken);
    }

    public async Task DeleteMyProfileAsync(CancellationToken cancellationToken = default)
    {
        await DeleteAsync("api/profiles/my", cancellationToken);
    }

    public async Task<IEnumerable<ProfileDto>> GetAllMyProfilesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileDto>>("api/profiles/my/all", cancellationToken);
    }

    public async Task<ActiveProfileDto> GetMyActiveProfileAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<ActiveProfileDto>("api/profiles/my/active", cancellationToken);
    }

    public async Task<ActiveProfileDto> SetMyActiveProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await PutAsync<ActiveProfileDto>($"api/profiles/my/{profileId}/set-active", new { }, cancellationToken);
    }

    // Profile management
    public async Task<ProfileDto> CreateProfileAsync(CreateAnyProfileDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ProfileDto>("api/profiles", request, cancellationToken);
    }

    public async Task<ProfileDto> GetProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ProfileDto>($"api/profiles/{profileId}", cancellationToken);
    }

    public async Task<ProfileDto> UpdateProfileAsync(Guid profileId, UpdateProfileDto request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<ProfileDto>($"api/profiles/{profileId}", request, cancellationToken);
    }

    public async Task DeleteProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/profiles/{profileId}", cancellationToken);
    }

    public async Task<ProfileDto> SetProfileActiveAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await PutAsync<ProfileDto>($"api/profiles/{profileId}/set-active", new { }, cancellationToken);
    }

    public async Task<ProfileDto> ActivateProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await PutAsync<ProfileDto>($"api/profiles/{profileId}/activate", new { }, cancellationToken);
    }

    // Discovery
    public async Task<IEnumerable<ProfileSummaryDto>> GetPublicProfilesAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileSummaryDto>>($"api/profiles/public?pageSize={pageSize}&pageNumber={pageNumber}", cancellationToken);
    }

    public async Task<IEnumerable<ProfileSearchDto>> SearchProfilesAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileSearchDto>>($"api/profiles/search?query={Uri.EscapeDataString(query)}&pageSize={pageSize}&pageNumber={pageNumber}", cancellationToken);
    }

    public async Task<IEnumerable<ProfileSummaryDto>> GetProfilesByLocationAsync(string location, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileSummaryDto>>($"api/profiles/by-location?location={Uri.EscapeDataString(location)}&pageSize={pageSize}&pageNumber={pageNumber}", cancellationToken);
    }

    public async Task<IEnumerable<ProfileSummaryDto>> GetProfilesByTagsAsync(string tags, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileSummaryDto>>($"api/profiles/by-tags?tags={Uri.EscapeDataString(tags)}&pageSize={pageSize}&pageNumber={pageNumber}", cancellationToken);
    }

    public async Task<IEnumerable<ProfileSummaryDto>> GetPopularProfilesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileSummaryDto>>($"api/profiles/popular?limit={limit}", cancellationToken);
    }

    public async Task<IEnumerable<ProfileSummaryDto>> GetRecentProfilesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileSummaryDto>>($"api/profiles/recent?limit={limit}", cancellationToken);
    }

    public async Task<ProfileStatisticsDto> GetProfileStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<ProfileStatisticsDto>("api/profiles/statistics", cancellationToken);
    }
}
