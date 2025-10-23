using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;


namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for managing profile follower relationships
/// </summary>
public class ProfileFollowerRepository : BaseRepository<ProfileFollower>, IProfileFollowerRepository
{
    public ProfileFollowerRepository(SivarDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get all followers of a specific profile
    /// </summary>
    public async Task<IEnumerable<ProfileFollower>> GetFollowersByProfileIdAsync(Guid profileId)
    {
        return await _context.Set<ProfileFollower>()
            .Where(pf => pf.FollowedProfileId == profileId && pf.IsActive)
            .Include(pf => pf.FollowerProfile)
            .OrderByDescending(pf => pf.FollowedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all profiles that a specific profile is following
    /// </summary>
    public async Task<IEnumerable<ProfileFollower>> GetFollowingByProfileIdAsync(Guid profileId)
    {
        return await _context.Set<ProfileFollower>()
            .Where(pf => pf.FollowerProfileId == profileId && pf.IsActive)
            .Include(pf => pf.FollowedProfile)
            .OrderByDescending(pf => pf.FollowedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Check if one profile is following another
    /// </summary>
    public async Task<bool> IsFollowingAsync(Guid followerProfileId, Guid followedProfileId)
    {
        return await _context.Set<ProfileFollower>()
            .AnyAsync(pf => pf.FollowerProfileId == followerProfileId 
                          && pf.FollowedProfileId == followedProfileId 
                          && pf.IsActive);
    }

    /// <summary>
    /// Get a specific follow relationship between two profiles
    /// </summary>
    public async Task<ProfileFollower?> GetFollowRelationshipAsync(Guid followerProfileId, Guid followedProfileId)
    {
        return await _context.Set<ProfileFollower>()
            .FirstOrDefaultAsync(pf => pf.FollowerProfileId == followerProfileId 
                                     && pf.FollowedProfileId == followedProfileId);
    }

    /// <summary>
    /// Get follower count for a profile
    /// </summary>
    public async Task<int> GetFollowerCountAsync(Guid profileId)
    {
        return await _context.Set<ProfileFollower>()
            .CountAsync(pf => pf.FollowedProfileId == profileId && pf.IsActive);
    }

    /// <summary>
    /// Get following count for a profile
    /// </summary>
    public async Task<int> GetFollowingCountAsync(Guid profileId)
    {
        return await _context.Set<ProfileFollower>()
            .CountAsync(pf => pf.FollowerProfileId == profileId && pf.IsActive);
    }

    /// <summary>
    /// Get mutual followers between two profiles
    /// </summary>
    public async Task<IEnumerable<ProfileFollower>> GetMutualFollowersAsync(Guid profileId1, Guid profileId2)
    {
        var profile1Followers = await _context.Set<ProfileFollower>()
            .Where(pf => pf.FollowedProfileId == profileId1 && pf.IsActive)
            .Select(pf => pf.FollowerProfileId)
            .ToListAsync();

        return await _context.Set<ProfileFollower>()
            .Where(pf => pf.FollowedProfileId == profileId2 
                        && pf.IsActive 
                        && profile1Followers.Contains(pf.FollowerProfileId))
            .Include(pf => pf.FollowerProfile)
            .ToListAsync();
    }
}