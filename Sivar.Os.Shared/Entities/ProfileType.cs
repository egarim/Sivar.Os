using System.Collections.ObjectModel;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a type of profile that can be created by users
/// This is an entity (not enum) to allow for extensibility and dynamic profile types
/// </summary>
public class ProfileType : BaseEntity
{
    /// <summary>
    /// Unique name identifier for the profile type (e.g., "PersonalProfile")
    /// </summary>
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the profile type (e.g., "Personal Profile")
    /// </summary>
    public virtual string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this profile type is used for
    /// </summary>
    public virtual string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this profile type is currently available for creation
    /// </summary>
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order for displaying profile types
    /// </summary>
    public virtual int SortOrder { get; set; } = 0;

    /// <summary>
    /// Feature flags for this profile type (stored as JSON for flexibility)
    /// </summary>
    public virtual string FeatureFlags { get; set; } = "{}";

    /// <summary>
    /// Collection of profiles of this type
    /// </summary>
    public virtual ICollection<Profile> Profiles { get; set; } = new ObservableCollection<Profile>();

    /// <summary>
    /// Checks if a specific feature is enabled for this profile type
    /// </summary>
    /// <param name="featureName">Name of the feature to check</param>
    /// <returns>True if the feature is enabled, false otherwise</returns>
    public bool HasFeature(string featureName)
    {
        // TODO: Implement JSON parsing of FeatureFlags
        // For now, PersonalProfile will have basic features only
        return featureName switch
        {
            "AllowsDisplayName" => true,
            "AllowsBio" => true,
            "AllowsAvatar" => true,
            "AllowsLocation" => true,
            "AllowsBookings" => false,
            "AllowsProducts" => false,
            "AllowsContactInfo" => true,
            _ => false
        };
    }
}