
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for managing profile follower relationships
/// </summary>
public interface IProfileFollowerService
{
    /// <summary>
    /// Follow a profile
    /// </summary>
    Task<FollowResultDto> FollowProfileAsync(Guid followerProfileId, Guid profileToFollowId);

    /// <summary>
    /// Unfollow a profile
    /// </summary>
    Task<FollowResultDto> UnfollowProfileAsync(Guid followerProfileId, Guid profileToUnfollowId);

    /// <summary>
    /// Get all followers of a profile
    /// </summary>
    Task<IEnumerable<FollowerProfileDto>> GetFollowersAsync(Guid profileId, Guid? currentUserProfileId = null);

    /// <summary>
    /// Get all profiles that a profile is following
    /// </summary>
    Task<IEnumerable<FollowingProfileDto>> GetFollowingAsync(Guid profileId, Guid? currentUserProfileId = null);

    /// <summary>
    /// Get follower statistics for a profile
    /// </summary>
    Task<FollowerStatsDto> GetFollowerStatsAsync(Guid profileId, Guid? currentUserProfileId = null);

    /// <summary>
    /// Check if one profile is following another
    /// </summary>
    Task<bool> IsFollowingAsync(Guid followerProfileId, Guid followedProfileId);

    /// <summary>
    /// Get mutual followers between two profiles
    /// </summary>
    Task<IEnumerable<FollowerProfileDto>> GetMutualFollowersAsync(Guid profileId1, Guid profileId2);
}