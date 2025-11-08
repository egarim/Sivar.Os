
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
        // Return null if request is null
        if (request == null)
            return null!;
            
        return await PostAsync<ProfileDto>("api/profiles/my", request, cancellationToken);
    }

    public async Task<ProfileDto> UpdateMyProfileAsync(UpdateProfileDto request, CancellationToken cancellationToken = default)
    {
        // Return null if request is null
        if (request == null)
            return null!;
            
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
        Console.WriteLine("[ProfilesClient.CreateProfileAsync] Starting profile creation API call");
        Console.WriteLine($"  Endpoint: POST api/profiles");
        Console.WriteLine($"  DisplayName: {request.DisplayName}");
        Console.WriteLine($"  ProfileTypeId: {request.ProfileTypeId}");
        
        try
        {
            var result = await PostAsync<ProfileDto>("api/profiles", request, cancellationToken);
            Console.WriteLine($"[ProfilesClient.CreateProfileAsync] ✅ API returned successfully");
            Console.WriteLine($"  Result ID: {result?.Id}");
            Console.WriteLine($"  Result DisplayName: {result?.DisplayName}");
            return result!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProfilesClient.CreateProfileAsync] ❌ API call failed!");
            Console.WriteLine($"  Exception: {ex.GetType().Name}");
            Console.WriteLine($"  Message: {ex.Message}");
            throw;
        }
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

    public async Task<ProfileDto> GetProfileByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ProfileDto>($"api/profiles/by-identifier/{Uri.EscapeDataString(identifier)}", cancellationToken);
    }

    public async Task<IEnumerable<ProfileSummaryDto>> SearchProfilesAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileSummaryDto>>($"api/profiles/search?query={Uri.EscapeDataString(query)}&pageSize={pageSize}&pageNumber={pageNumber}", cancellationToken);
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

    public async Task<IEnumerable<ProfileDto>> FindNearbyProfilesAsync(double latitude, double longitude, double radiusKm = 10, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileDto>>($"api/profiles/nearby?latitude={latitude}&longitude={longitude}&radiusKm={radiusKm}&limit={limit}", cancellationToken);
    }

    public async Task<bool> UpdatePreferredLanguageAsync(Guid profileId, string? languageCode, CancellationToken cancellationToken = default)
    {
        var request = new { LanguageCode = languageCode };
        var response = await PutAsync<object>($"api/profiles/my/{profileId}/language", request, cancellationToken);
        return response != null;
    }
}
