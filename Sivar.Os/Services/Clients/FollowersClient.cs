
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

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

    // Profile-specific queries
    public async Task<FollowerStatsDto> GetStatsForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetStatsForProfileAsync: {ProfileId}", profileId);
        // For server-side rendering, we don't have current user context - API will handle it
        return await _profileFollowerService.GetFollowerStatsAsync(profileId, null);
    }

    public async Task<IEnumerable<FollowerProfileDto>> GetFollowersForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetFollowersForProfileAsync: {ProfileId}", profileId);
        return await _profileFollowerService.GetFollowersAsync(profileId, null);
    }

    public async Task<IEnumerable<FollowingProfileDto>> GetFollowingForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetFollowingForProfileAsync: {ProfileId}", profileId);
        return await _profileFollowerService.GetFollowingAsync(profileId, null);
    }
}
