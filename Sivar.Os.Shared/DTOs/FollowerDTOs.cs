namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for profile follower relationship information
/// </summary>
public class ProfileFollowerDto
{
    public Guid Id { get; set; }
    public Guid FollowerProfileId { get; set; }
    public Guid FollowedProfileId { get; set; }
    public DateTime FollowedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for follower profile summary information
/// </summary>
public class FollowerProfileDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public DateTime FollowedAt { get; set; }
    public bool IsFollowingBack { get; set; }
}

/// <summary>
/// DTO for following profile summary information
/// </summary>
public class FollowingProfileDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public DateTime FollowedAt { get; set; }
    public bool IsFollowingBack { get; set; }
}

/// <summary>
/// DTO for follow/unfollow operations
/// </summary>
public class FollowActionDto
{
    public Guid ProfileToFollowId { get; set; }
}

/// <summary>
/// DTO for follow operation result
/// </summary>
public class FollowResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ProfileFollowerDto? FollowRelation { get; set; }
}

/// <summary>
/// DTO for followers statistics
/// </summary>
public class FollowerStatsDto
{
    public Guid ProfileId { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public bool IsFollowedByCurrentUser { get; set; }
}