
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for User management and auto-registration
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets or creates a user from Keycloak authentication
    /// Auto-registers user if they don't exist
    /// </summary>
    /// <param name="keycloakUserDto">User data from Keycloak JWT claims</param>
    /// <returns>User DTO</returns>
    Task<UserDto> GetOrCreateUserFromKeycloakAsync(CreateUserFromKeycloakDto keycloakUserDto);

    /// <summary>
    /// Gets a user by their Keycloak ID
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>User DTO if found, null otherwise</returns>
    Task<UserDto?> GetUserByKeycloakIdAsync(string keycloakId);

    /// <summary>
    /// Gets current user information and updates last login
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>User DTO if found, null otherwise</returns>
    Task<UserDto?> GetCurrentUserAsync(string keycloakId);

    /// <summary>
    /// Updates user preferences
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <param name="updateDto">Updated preferences</param>
    /// <returns>Updated user DTO if successful, null otherwise</returns>
    Task<UserDto?> UpdateUserPreferencesAsync(string keycloakId, UpdateUserPreferencesDto updateDto);

    /// <summary>
    /// Gets all users (admin only)
    /// </summary>
    /// <returns>Collection of user DTOs</returns>
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Gets users by role (admin only)
    /// </summary>
    /// <param name="role">User role to filter by</param>
    /// <returns>Collection of user DTOs</returns>
    Task<IEnumerable<UserDto>> GetUsersByRoleAsync(Enums.UserRole role);

    /// <summary>
    /// Deactivates a user account (admin only)
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>True if deactivated, false otherwise</returns>
    Task<bool> DeactivateUserAsync(string keycloakId);

    /// <summary>
    /// Reactivates a user account (admin only)
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>True if reactivated, false otherwise</returns>
    Task<bool> ReactivateUserAsync(string keycloakId);

    /// <summary>
    /// Checks if a user exists by Keycloak ID
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>True if user exists, false otherwise</returns>
    Task<bool> UserExistsAsync(string keycloakId);

    /// <summary>
    /// Gets user statistics (admin only)
    /// </summary>
    /// <returns>User statistics object</returns>
    Task<UserStatisticsDto> GetUserStatisticsAsync();
}

/// <summary>
/// DTO for user statistics
/// </summary>
public class UserStatisticsDto
{
    /// <summary>
    /// Total number of users
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Number of active users
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Number of administrator users
    /// </summary>
    public int Administrators { get; set; }

    /// <summary>
    /// Number of registered users
    /// </summary>
    public int RegisteredUsers { get; set; }

    /// <summary>
    /// Number of users created in the last 30 days
    /// </summary>
    public int NewUsersLast30Days { get; set; }
}