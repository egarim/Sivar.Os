using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client for follower operations
/// </summary>
public interface IFollowersClient
{
    // Follow operations
    Task<FollowResultDto> FollowAsync(FollowActionDto request, CancellationToken cancellationToken = default);
    Task UnfollowAsync(Guid profileToUnfollowId, CancellationToken cancellationToken = default);

    // Query operations
    Task<IEnumerable<FollowerProfileDto>> GetFollowersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<FollowingProfileDto>> GetFollowingAsync(CancellationToken cancellationToken = default);
    Task<FollowerStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);

    // Status checks
    Task<bool> GetFollowingStatusAsync(Guid targetProfileId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProfileFollowerDto>> GetMutualFollowersAsync(Guid otherProfileId, CancellationToken cancellationToken = default);
}
