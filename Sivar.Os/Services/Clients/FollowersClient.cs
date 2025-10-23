using Sivar.Core.Clients.Followers;
using Sivar.Core.DTOs;
using Sivar.Core.Interfaces;
using Sivar.Core.Repositories;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of followers client
/// </summary>
public class FollowersClient : BaseRepositoryClient, IFollowersClient
{
    private readonly IProfileFollowerService _profileFollowerService;
    private readonly IProfileFollowerRepository _profileFollowerRepository;
    private readonly ILogger<FollowersClient> _logger;

    public FollowersClient(
        IProfileFollowerService profileFollowerService,
        IProfileFollowerRepository profileFollowerRepository,
        ILogger<FollowersClient> logger)
    {
        _profileFollowerService = profileFollowerService ?? throw new ArgumentNullException(nameof(profileFollowerService));
        _profileFollowerRepository = profileFollowerRepository ?? throw new ArgumentNullException(nameof(profileFollowerRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Follow operations
    public async Task<FollowResultDto> FollowAsync(FollowActionDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("FollowAsync");
        return new FollowResultDto();
    }

    public async Task UnfollowAsync(Guid profileToUnfollowId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UnfollowAsync: {ProfileId}", profileToUnfollowId);
    }

    // Query operations
    public async Task<IEnumerable<FollowerProfileDto>> GetFollowersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetFollowersAsync");
        return new List<FollowerProfileDto>();
    }

    public async Task<IEnumerable<FollowingProfileDto>> GetFollowingAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetFollowingAsync");
        return new List<FollowingProfileDto>();
    }

    public async Task<FollowerStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetStatsAsync");
        return new FollowerStatsDto();
    }

    // Status checks
    public async Task<bool> GetFollowingStatusAsync(Guid targetProfileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetFollowingStatusAsync: {ProfileId}", targetProfileId);
        return false;
    }

    public async Task<IEnumerable<ProfileFollowerDto>> GetMutualFollowersAsync(Guid otherProfileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetMutualFollowersAsync: {ProfileId}", otherProfileId);
        return new List<ProfileFollowerDto>();
    }
}
