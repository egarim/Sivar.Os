
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for managing profile follower relationships
/// </summary>
public interface IProfileFollowerRepository : IBaseRepository<ProfileFollower>
{
    /// <summary>
    /// Get all followers of a specific profile
    /// </summary>
    Task<IEnumerable<ProfileFollower>> GetFollowersByProfileIdAsync(Guid profileId);

    /// <summary>
    /// Get all profiles that a specific profile is following
    /// </summary>
    Task<IEnumerable<ProfileFollower>> GetFollowingByProfileIdAsync(Guid profileId);

    /// <summary>
    /// Check if one profile is following another
    /// </summary>
    Task<bool> IsFollowingAsync(Guid followerProfileId, Guid followedProfileId);

    /// <summary>
    /// Get a specific follow relationship between two profiles
    /// </summary>
    Task<ProfileFollower?> GetFollowRelationshipAsync(Guid followerProfileId, Guid followedProfileId);

    /// <summary>
    /// Get follower count for a profile
    /// </summary>
    Task<int> GetFollowerCountAsync(Guid profileId);

    /// <summary>
    /// Get following count for a profile
    /// </summary>
    Task<int> GetFollowingCountAsync(Guid profileId);

    /// <summary>
    /// Get mutual followers between two profiles
    /// </summary>
    Task<IEnumerable<ProfileFollower>> GetMutualFollowersAsync(Guid profileId1, Guid profileId2);
}