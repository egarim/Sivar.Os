using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a follower relationship between two profiles
/// </summary>
public class ProfileFollower : BaseEntity
{
    /// <summary>
    /// The profile that is following another profile
    /// </summary>
    public virtual Guid FollowerProfileId { get; set; }
    public virtual Profile FollowerProfile { get; set; } = null!;

    /// <summary>
    /// The profile being followed
    /// </summary>
    public virtual Guid FollowedProfileId { get; set; }
    public virtual Profile FollowedProfile { get; set; } = null!;

    /// <summary>
    /// When the follow relationship was established
    /// </summary>
    public virtual DateTime FollowedAt { get; set; }

    /// <summary>
    /// Whether this follow relationship is active
    /// </summary>
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Validation to ensure a profile cannot follow itself
    /// </summary>
    public void ValidateNotSelfFollow()
    {
        if (FollowerProfileId == FollowedProfileId)
        {
            throw new InvalidOperationException("A profile cannot follow itself");
        }
    }
}