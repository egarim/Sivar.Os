using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// Data Transfer Object for User entity
/// </summary>
public class UserDto
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Keycloak user identifier
    /// </summary>
    public string KeycloakId { get; set; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's role in the system
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Indicates if the user is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// User's preferred language
    /// </summary>
    public string PreferredLanguage { get; set; } = "en";

    /// <summary>
    /// User's timezone
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Date and time when the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time of user's last login
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Indicates if the user has administrator privileges
    /// </summary>
    public bool IsAdministrator { get; set; }
}

/// <summary>
/// DTO for updating user preferences
/// </summary>
public class UpdateUserPreferencesDto
{
    /// <summary>
    /// User's preferred language
    /// </summary>
    public string PreferredLanguage { get; set; } = string.Empty;

    /// <summary>
    /// User's timezone
    /// </summary>
    public string TimeZone { get; set; } = string.Empty;
}

/// <summary>
/// DTO for creating a user during auto-registration
/// </summary>
public class CreateUserFromKeycloakDto
{
    /// <summary>
    /// User's email address from Keycloak
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Keycloak user identifier (sub claim)
    /// </summary>
    public string KeycloakId { get; set; } = string.Empty;

    /// <summary>
    /// User's first name from Keycloak
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name from Keycloak
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's role (determined from Keycloak roles)
    /// </summary>
    public UserRole Role { get; set; } = UserRole.RegisteredUser;

    /// <summary>
    /// User's preferred language
    /// </summary>
    public string PreferredLanguage { get; set; } = "en";
}