namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// Data Transfer Object for ProfileType entity
/// </summary>
public class ProfileTypeDto
{
    /// <summary>
    /// ProfileType's unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique name identifier for the profile type
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the profile type
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the profile type
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this profile type is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Sort order for displaying profile types
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Feature flags as JSON string
    /// </summary>
    public string FeatureFlags { get; set; } = "{}";

    /// <summary>
    /// Number of profiles using this type (for admin view)
    /// </summary>
    public int ProfileCount { get; set; }

    /// <summary>
    /// Date and time when the profile type was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time when the profile type was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new profile type
/// </summary>
public class CreateProfileTypeDto
{
    /// <summary>
    /// Unique name identifier for the profile type
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the profile type
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the profile type
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Sort order for displaying profile types
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Feature flags as JSON string
    /// </summary>
    public string FeatureFlags { get; set; } = "{}";
}

/// <summary>
/// DTO for updating an existing profile type
/// </summary>
public class UpdateProfileTypeDto
{
    /// <summary>
    /// Display name for the profile type
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the profile type
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Sort order for displaying profile types
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Feature flags as JSON string
    /// </summary>
    public string FeatureFlags { get; set; } = "{}";

    /// <summary>
    /// Indicates if this profile type is active
    /// </summary>
    public bool IsActive { get; set; }
}