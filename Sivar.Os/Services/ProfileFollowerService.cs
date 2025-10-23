using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service for managing profile follower relationships
/// </summary>
public class ProfileFollowerService : IProfileFollowerService
{
    private readonly IProfileFollowerRepository _followerRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<ProfileFollowerService> _logger;

    public ProfileFollowerService(
        IProfileFollowerRepository followerRepository,
        IProfileRepository profileRepository,
        ILogger<ProfileFollowerService> logger)
    {
        _followerRepository = followerRepository ?? throw new ArgumentNullException(nameof(followerRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Follow a profile
    /// </summary>
    public async Task<FollowResultDto> FollowProfileAsync(Guid followerProfileId, Guid profileToFollowId)
    {
        try
        {
            // Validate that profiles are different
            if (followerProfileId == profileToFollowId)
            {
                return new FollowResultDto
                {
                    Success = false,
                    Message = "A profile cannot follow itself"
                };
            }

            // Check if both profiles exist
            var followerProfile = await _profileRepository.GetByIdAsync(followerProfileId);
            var profileToFollow = await _profileRepository.GetByIdAsync(profileToFollowId);

            if (followerProfile == null)
            {
                return new FollowResultDto
                {
                    Success = false,
                    Message = "Follower profile not found"
                };
            }

            if (profileToFollow == null)
            {
                return new FollowResultDto
                {
                    Success = false,
                    Message = "Profile to follow not found"
                };
            }

            // Check if already following
            var existingRelation = await _followerRepository.GetFollowRelationshipAsync(followerProfileId, profileToFollowId);
            
            if (existingRelation != null)
            {
                if (existingRelation.IsActive)
                {
                    return new FollowResultDto
                    {
                        Success = false,
                        Message = "Already following this profile"
                    };
                }
                else
                {
                    // Reactivate existing relationship
                    existingRelation.IsActive = true;
                    existingRelation.FollowedAt = DateTime.UtcNow;
                    existingRelation.UpdatedAt = DateTime.UtcNow;
                    
                    await _followerRepository.UpdateAsync(existingRelation);
                    await _followerRepository.SaveChangesAsync();

                    return new FollowResultDto
                    {
                        Success = true,
                        Message = "Successfully followed profile",
                        FollowRelation = MapToFollowDto(existingRelation)
                    };
                }
            }

            // Create new follow relationship
            var newFollowRelation = new ProfileFollower
            {
                Id = Guid.NewGuid(),
                FollowerProfileId = followerProfileId,
                FollowedProfileId = profileToFollowId,
                FollowedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            newFollowRelation.ValidateNotSelfFollow();

            await _followerRepository.AddAsync(newFollowRelation);
            await _followerRepository.SaveChangesAsync();

            _logger.LogInformation("Profile {FollowerId} started following profile {FollowedId}", 
                followerProfileId, profileToFollowId);

            return new FollowResultDto
            {
                Success = true,
                Message = "Successfully followed profile",
                FollowRelation = MapToFollowDto(newFollowRelation)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error following profile {ProfileId} by {FollowerId}", 
                profileToFollowId, followerProfileId);
            
            return new FollowResultDto
            {
                Success = false,
                Message = "An error occurred while following the profile"
            };
        }
    }

    /// <summary>
    /// Unfollow a profile
    /// </summary>
    public async Task<FollowResultDto> UnfollowProfileAsync(Guid followerProfileId, Guid profileToUnfollowId)
    {
        try
        {
            var existingRelation = await _followerRepository.GetFollowRelationshipAsync(followerProfileId, profileToUnfollowId);
            
            if (existingRelation == null || !existingRelation.IsActive)
            {
                return new FollowResultDto
                {
                    Success = false,
                    Message = "Not currently following this profile"
                };
            }

            // Deactivate the relationship (soft delete)
            existingRelation.IsActive = false;
            existingRelation.UpdatedAt = DateTime.UtcNow;
            
            await _followerRepository.UpdateAsync(existingRelation);
            await _followerRepository.SaveChangesAsync();

            _logger.LogInformation("Profile {FollowerId} unfollowed profile {FollowedId}", 
                followerProfileId, profileToUnfollowId);

            return new FollowResultDto
            {
                Success = true,
                Message = "Successfully unfollowed profile"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unfollowing profile {ProfileId} by {FollowerId}", 
                profileToUnfollowId, followerProfileId);
            
            return new FollowResultDto
            {
                Success = false,
                Message = "An error occurred while unfollowing the profile"
            };
        }
    }

    /// <summary>
    /// Get all followers of a profile
    /// </summary>
    public async Task<IEnumerable<FollowerProfileDto>> GetFollowersAsync(Guid profileId, Guid? currentUserProfileId = null)
    {
        var followers = await _followerRepository.GetFollowersByProfileIdAsync(profileId);
        var result = new List<FollowerProfileDto>();

        foreach (var follower in followers)
        {
            bool isFollowingBack = false;
            if (currentUserProfileId.HasValue)
            {
                isFollowingBack = await _followerRepository.IsFollowingAsync(profileId, follower.FollowerProfileId);
            }

            result.Add(new FollowerProfileDto
            {
                Id = follower.FollowerProfile.Id,
                DisplayName = follower.FollowerProfile.DisplayName,
                Avatar = follower.FollowerProfile.Avatar,
                Bio = follower.FollowerProfile.Bio,
                FollowedAt = follower.FollowedAt,
                IsFollowingBack = isFollowingBack
            });
        }

        return result;
    }

    /// <summary>
    /// Get all profiles that a profile is following
    /// </summary>
    public async Task<IEnumerable<FollowingProfileDto>> GetFollowingAsync(Guid profileId, Guid? currentUserProfileId = null)
    {
        var following = await _followerRepository.GetFollowingByProfileIdAsync(profileId);
        var result = new List<FollowingProfileDto>();

        foreach (var followed in following)
        {
            bool isFollowingBack = false;
            if (currentUserProfileId.HasValue)
            {
                isFollowingBack = await _followerRepository.IsFollowingAsync(followed.FollowedProfileId, profileId);
            }

            result.Add(new FollowingProfileDto
            {
                Id = followed.FollowedProfile.Id,
                DisplayName = followed.FollowedProfile.DisplayName,
                Avatar = followed.FollowedProfile.Avatar,
                Bio = followed.FollowedProfile.Bio,
                FollowedAt = followed.FollowedAt,
                IsFollowingBack = isFollowingBack
            });
        }

        return result;
    }

    /// <summary>
    /// Get follower statistics for a profile
    /// </summary>
    public async Task<FollowerStatsDto> GetFollowerStatsAsync(Guid profileId, Guid? currentUserProfileId = null)
    {
        var followersCount = await _followerRepository.GetFollowerCountAsync(profileId);
        var followingCount = await _followerRepository.GetFollowingCountAsync(profileId);

        bool isFollowedByCurrentUser = false;
        if (currentUserProfileId.HasValue)
        {
            isFollowedByCurrentUser = await _followerRepository.IsFollowingAsync(currentUserProfileId.Value, profileId);
        }

        return new FollowerStatsDto
        {
            ProfileId = profileId,
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            IsFollowedByCurrentUser = isFollowedByCurrentUser
        };
    }

    /// <summary>
    /// Check if one profile is following another
    /// </summary>
    public async Task<bool> IsFollowingAsync(Guid followerProfileId, Guid followedProfileId)
    {
        return await _followerRepository.IsFollowingAsync(followerProfileId, followedProfileId);
    }

    /// <summary>
    /// Get mutual followers between two profiles
    /// </summary>
    public async Task<IEnumerable<FollowerProfileDto>> GetMutualFollowersAsync(Guid profileId1, Guid profileId2)
    {
        var mutualFollowers = await _followerRepository.GetMutualFollowersAsync(profileId1, profileId2);
        
        return mutualFollowers.Select(mf => new FollowerProfileDto
        {
            Id = mf.FollowerProfile.Id,
            DisplayName = mf.FollowerProfile.DisplayName,
            Avatar = mf.FollowerProfile.Avatar,
            Bio = mf.FollowerProfile.Bio,
            FollowedAt = mf.FollowedAt,
            IsFollowingBack = false // This would need additional logic to determine
        });
    }

    private static ProfileFollowerDto MapToFollowDto(ProfileFollower followRelation)
    {
        return new ProfileFollowerDto
        {
            Id = followRelation.Id,
            FollowerProfileId = followRelation.FollowerProfileId,
            FollowedProfileId = followRelation.FollowedProfileId,
            FollowedAt = followRelation.FollowedAt,
            IsActive = followRelation.IsActive
        };
    }
}