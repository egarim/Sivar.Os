using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a user in the system with Keycloak integration
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// User's email address (from Keycloak)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Keycloak user identifier (sub claim from JWT)
    /// </summary>
    public string KeycloakId { get; set; } = string.Empty;

    /// <summary>
    /// User's first name (from Keycloak claims)
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name (from Keycloak claims)
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's role in the system
    /// </summary>
    public UserRole Role { get; set; } = UserRole.RegisteredUser;

    /// <summary>
    /// Indicates if the user is active in the system
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date and time of user's last login
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// User's preferred language (ISO code, e.g., "en", "es")
    /// </summary>
    public string PreferredLanguage { get; set; } = "en";

    /// <summary>
    /// User's timezone (e.g., "UTC", "America/New_York")
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// The ID of the user's currently active profile
    /// </summary>
    public Guid? ActiveProfileId { get; set; }

    /// <summary>
    /// Navigation property for the active profile
    /// </summary>
    public virtual Profile? ActiveProfile { get; set; }

    /// <summary>
    /// Collection of profiles owned by this user
    /// </summary>
    public virtual ICollection<Profile> Profiles { get; set; } = new List<Profile>();

    /// <summary>
    /// Full name derived from first and last names
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Updates the last login timestamp
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the user has administrator privileges
    /// </summary>
    public bool IsAdministrator => Role == UserRole.Administrator;

    /// <summary>
    /// Sets the active profile for the user
    /// </summary>
    /// <param name="profileId">The ID of the profile to set as active</param>
    /// <returns>True if the profile was set as active, false if the profile doesn't belong to this user</returns>
    public bool SetActiveProfile(Guid profileId)
    {
        // Check if the profile belongs to this user
        if (!Profiles.Any(p => p.Id == profileId))
            return false;

        // Deactivate current active profile
        if (ActiveProfileId.HasValue)
        {
            var currentActive = Profiles.FirstOrDefault(p => p.Id == ActiveProfileId);
            currentActive?.SetAsInactive();
        }

        // Set new active profile
        ActiveProfileId = profileId;
        var newActiveProfile = Profiles.FirstOrDefault(p => p.Id == profileId);
        newActiveProfile?.SetAsActive();

        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Clears the active profile (sets to none)
    /// </summary>
    public void ClearActiveProfile()
    {
        if (ActiveProfileId.HasValue)
        {
            var currentActive = Profiles.FirstOrDefault(p => p.Id == ActiveProfileId);
            currentActive?.SetAsInactive();
            ActiveProfileId = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets the user's active profile or the first available profile if none is set
    /// </summary>
    /// <returns>The active profile or null if no profiles exist</returns>
    public Profile? GetActiveProfileOrDefault()
    {
        if (ActiveProfile != null)
            return ActiveProfile;

        // If no active profile is set, return the first available profile
        return Profiles.OrderBy(p => p.CreatedAt).FirstOrDefault();
    }

    /// <summary>
    /// Checks if the user has any profiles
    /// </summary>
    public bool HasProfiles => Profiles?.Any() == true;

    /// <summary>
    /// Gets the count of profiles for each profile type
    /// </summary>
    /// <returns>Dictionary of ProfileType name to count</returns>
    public Dictionary<string, int> GetProfileCountsByType()
    {
        return Profiles
            .GroupBy(p => p.ProfileType.Name)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}