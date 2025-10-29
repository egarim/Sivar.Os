
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of followers client
/// </summary>
public class FollowersClient : BaseClient, IFollowersClient
{
    public FollowersClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    public async Task<FollowResultDto> FollowAsync(FollowActionDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<FollowResultDto>("api/followers/follow", request, cancellationToken);
    }

    public async Task UnfollowAsync(Guid profileToUnfollowId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/followers/follow/{profileToUnfollowId}", cancellationToken);
    }

    public async Task<IEnumerable<FollowerProfileDto>> GetFollowersAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<FollowerProfileDto>>("api/followers/followers", cancellationToken);
    }

    public async Task<IEnumerable<FollowingProfileDto>> GetFollowingAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<FollowingProfileDto>>("api/followers/following", cancellationToken);
    }

    public async Task<FollowerStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<FollowerStatsDto>("api/followers/stats", cancellationToken);
    }

    public async Task<bool> GetFollowingStatusAsync(Guid targetProfileId, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<dynamic>($"api/followers/following/{targetProfileId}/status", cancellationToken);
        return result?.isFollowing ?? false;
    }

    public async Task<IEnumerable<ProfileFollowerDto>> GetMutualFollowersAsync(Guid otherProfileId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ProfileFollowerDto>>($"api/followers/mutual/{otherProfileId}", cancellationToken);
    }

    public async Task<FollowerStatsDto> GetStatsForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<FollowerStatsDto>($"api/followers/profiles/{profileId}/stats", cancellationToken);
    }

    public async Task<IEnumerable<FollowerProfileDto>> GetFollowersForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<FollowerProfileDto>>($"api/followers/profiles/{profileId}/followers", cancellationToken);
    }

    public async Task<IEnumerable<FollowingProfileDto>> GetFollowingForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<FollowingProfileDto>>($"api/followers/profiles/{profileId}/following", cancellationToken);
    }
}
