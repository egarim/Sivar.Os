using Sivar.Os.Shared.DTOs.ValueObjects;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// Data Transfer Object for Profile entity
/// </summary>
public class ProfileDto
{
    /// <summary>
    /// Profile's unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID who owns this profile
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Profile type ID
    /// </summary>
    public Guid ProfileTypeId { get; set; }

    /// <summary>
    /// Profile type information
    /// </summary>
    public ProfileTypeDto? ProfileType { get; set; }

    /// <summary>
    /// Display name for the profile
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Unique URL-friendly handle (e.g., "jose-ojeda")
    /// </summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>
    /// Biography or description
    /// </summary>
    public string Bio { get; set; } = string.Empty;

    /// <summary>
    /// Avatar image URL or path
    /// </summary>
    public string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// File ID for the avatar image stored in file storage service
    /// </summary>
    public string? AvatarFileId { get; set; }

    /// <summary>
    /// Geographic location
    /// </summary>
    public Location? Location { get; set; }

    /// <summary>
    /// Formatted location display string
    /// </summary>
    public string LocationDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is the user's active profile
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Indicates if the profile is publicly visible (for backward compatibility)
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Profile visibility level
    /// </summary>
    public VisibilityLevel VisibilityLevel { get; set; }

    /// <summary>
    /// Profile view count
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Profile tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Social media links (as key-value pairs)
    /// </summary>
    public Dictionary<string, string> SocialMediaLinks { get; set; } = new();

    /// <summary>
    /// Dynamic metadata for the profile (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Date and time when the profile was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time when the profile was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new personal profile
/// </summary>
public class CreateProfileDto
{
    /// <summary>
    /// Display name for the profile (required)
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Biography or description (optional)
    /// </summary>
    public string Bio { get; set; } = string.Empty;

    /// <summary>
    /// Avatar image URL or path (optional)
    /// </summary>
    public string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// File ID for the avatar image stored in file storage service (optional)
    /// </summary>
    public string? AvatarFileId { get; set; }

    /// <summary>
    /// Geographic location (optional)
    /// </summary>
    public Location? Location { get; set; }

    /// <summary>
    /// Indicates if the profile should be publicly visible (for backward compatibility)
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Profile visibility level (alternative to IsPublic)
    /// </summary>
    public VisibilityLevel? VisibilityLevel { get; set; }

    /// <summary>
    /// Profile tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Social media links (as key-value pairs)
    /// </summary>
    public Dictionary<string, string> SocialMediaLinks { get; set; } = new();

    /// <summary>
    /// Dynamic metadata for the profile (JSON string)
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// DTO for updating an existing profile
/// </summary>
public class UpdateProfileDto
{
    /// <summary>
    /// Display name for the profile
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Biography or description
    /// </summary>
    public string Bio { get; set; } = string.Empty;

    /// <summary>
    /// Avatar image URL or path
    /// </summary>
    public string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// File ID for the avatar image stored in file storage service
    /// </summary>
    public string? AvatarFileId { get; set; }

    /// <summary>
    /// Geographic location
    /// </summary>
    public Location? Location { get; set; }

    /// <summary>
    /// Indicates if the profile should be publicly visible (for backward compatibility)
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Profile visibility level (alternative to IsPublic)
    /// </summary>
    public VisibilityLevel? VisibilityLevel { get; set; }

    /// <summary>
    /// Profile tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Social media links (as key-value pairs)
    /// </summary>
    public Dictionary<string, string> SocialMediaLinks { get; set; } = new();

    /// <summary>
    /// Dynamic metadata for the profile (JSON string)
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Simplified profile DTO for listing views
/// </summary>
public class ProfileSummaryDto
{
    /// <summary>
    /// Profile's unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display name for the profile
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Unique URL-friendly handle (e.g., "jose-ojeda")
    /// </summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>
    /// Short bio preview (truncated)
    /// </summary>
    public string BioPreview { get; set; } = string.Empty;

    /// <summary>
    /// Avatar image URL or path
    /// </summary>
    public string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// Formatted location display string
    /// </summary>
    public string LocationDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Profile type display name
    /// </summary>
    public string ProfileType { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is the user's active profile
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Profile view count
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Date and time when the profile was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for profile search queries
/// </summary>
public class ProfileSearchDto
{
    /// <summary>
    /// Search term for display name or bio
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Tags to search for
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// Whether profile must contain all tags (true) or any tags (false)
    /// </summary>
    public bool MatchAllTags { get; set; } = false;

    /// <summary>
    /// Location to search by
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Profile type filter
    /// </summary>
    public Guid? ProfileTypeId { get; set; }

    /// <summary>
    /// Visibility level filter
    /// </summary>
    public VisibilityLevel? VisibilityLevel { get; set; }

    /// <summary>
    /// Minimum view count filter
    /// </summary>
    public int? MinViewCount { get; set; }

    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of results per page
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// DTO for active profile operations
/// </summary>
public class ActiveProfileDto
{
    /// <summary>
    /// Profile's unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display name for the profile
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Profile type information
    /// </summary>
    public ProfileTypeDto? ProfileType { get; set; }

    /// <summary>
    /// Avatar image URL or path
    /// </summary>
    public string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// File ID for the avatar image stored in file storage service
    /// </summary>
    public string? AvatarFileId { get; set; }

    /// <summary>
    /// Formatted location display string
    /// </summary>
    public string LocationDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when set as active
    /// </summary>
    public DateTime ActivatedAt { get; set; }

    /// <summary>
    /// Indicates if this profile is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for comprehensive profile creation (supports all profile types)
/// </summary>
public class CreateAnyProfileDto
{
    /// <summary>
    /// Profile type ID
    /// </summary>
    public Guid ProfileTypeId { get; set; }

    /// <summary>
    /// Display name for the profile (required)
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Biography or description (optional)
    /// </summary>
    public string Bio { get; set; } = string.Empty;

    /// <summary>
    /// Avatar image URL or path (optional)
    /// </summary>
    public string Avatar { get; set; } = string.Empty;

    /// <summary>
    /// File ID for the avatar image stored in file storage service (optional)
    /// </summary>
    public string? AvatarFileId { get; set; }

    /// <summary>
    /// Geographic location (optional)
    /// </summary>
    public Location? Location { get; set; }

    /// <summary>
    /// Profile visibility level
    /// </summary>
    public VisibilityLevel VisibilityLevel { get; set; } = VisibilityLevel.Public;

    /// <summary>
    /// Profile tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Social media links (as key-value pairs)
    /// </summary>
    public Dictionary<string, string> SocialMediaLinks { get; set; } = new();

    /// <summary>
    /// Dynamic metadata for the profile (JSON string)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether to set as active immediately (optional)
    /// </summary>
    public bool SetAsActive { get; set; } = false;
}