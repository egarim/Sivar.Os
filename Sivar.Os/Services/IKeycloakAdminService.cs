using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Services;

/// <summary>
/// Result of a Keycloak Admin API operation
/// </summary>
public record KeycloakOperationResult(
    bool Success,
    string? ErrorMessage = null
);

/// <summary>
/// Keycloak user representation (simplified)
/// </summary>
public class KeycloakUser
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool EmailVerified { get; set; }
    public Dictionary<string, List<string>> Attributes { get; set; } = new();
}

/// <summary>
/// Service for interacting with Keycloak Admin API
/// Used to update user attributes after phone verification
/// </summary>
public interface IKeycloakAdminService
{
    /// <summary>
    /// Get a user by their Keycloak ID (sub claim)
    /// </summary>
    /// <param name="keycloakId">The Keycloak user ID</param>
    /// <returns>User data or null if not found</returns>
    Task<KeycloakUser?> GetUserByIdAsync(string keycloakId);

    /// <summary>
    /// Get a user by their email address
    /// </summary>
    /// <param name="email">The user's email</param>
    /// <returns>User data or null if not found</returns>
    Task<KeycloakUser?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Update user attributes in Keycloak
    /// </summary>
    /// <param name="keycloakId">The Keycloak user ID</param>
    /// <param name="attributes">Dictionary of attributes to update</param>
    /// <returns>Operation result</returns>
    Task<KeycloakOperationResult> UpdateUserAttributesAsync(
        string keycloakId, 
        Dictionary<string, string> attributes);

    /// <summary>
    /// Set user's phone verification status
    /// </summary>
    /// <param name="keycloakId">The Keycloak user ID</param>
    /// <param name="phoneNumber">The verified phone number</param>
    /// <param name="countryCode">ISO country code</param>
    /// <returns>Operation result</returns>
    Task<KeycloakOperationResult> SetPhoneVerifiedAsync(
        string keycloakId, 
        string phoneNumber, 
        string countryCode);

    /// <summary>
    /// Update user's waiting list status
    /// </summary>
    /// <param name="keycloakId">The Keycloak user ID</param>
    /// <param name="status">New waiting list status</param>
    /// <returns>Operation result</returns>
    Task<KeycloakOperationResult> UpdateWaitingListStatusAsync(
        string keycloakId, 
        WaitingListStatus status);

    /// <summary>
    /// Check if the Keycloak Admin service is enabled and configured
    /// </summary>
    bool IsEnabled { get; }
}
