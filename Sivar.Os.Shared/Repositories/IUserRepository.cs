
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for User entity operations with Keycloak integration
/// </summary>
public interface IUserRepository : IBaseRepository<User>
{
    /// <summary>
    /// Gets a user by their Keycloak ID
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier (sub claim)</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetByKeycloakIdAsync(string keycloakId);

    /// <summary>
    /// Gets a user by their email address
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Checks if a user exists by Keycloak ID
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>True if user exists, false otherwise</returns>
    Task<bool> ExistsByKeycloakIdAsync(string keycloakId);

    /// <summary>
    /// Gets all active users
    /// </summary>
    /// <returns>Collection of active users</returns>
    Task<IEnumerable<User>> GetActiveUsersAsync();

    /// <summary>
    /// Gets users by role
    /// </summary>
    /// <param name="role">User role to filter by</param>
    /// <returns>Collection of users with the specified role</returns>
    Task<IEnumerable<User>> GetUsersByRoleAsync(Enums.UserRole role);

    /// <summary>
    /// Updates the user's last login timestamp
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>True if updated, false if user not found</returns>
    Task<bool> UpdateLastLoginAsync(string keycloakId);

    /// <summary>
    /// Gets a user with their profiles included
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>User with profiles if found, null otherwise</returns>
    Task<User?> GetWithProfilesByKeycloakIdAsync(string keycloakId);

    /// <summary>
    /// Deactivates a user account
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>True if deactivated, false if user not found</returns>
    Task<bool> DeactivateUserAsync(string keycloakId);

    /// <summary>
    /// Reactivates a user account
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>True if reactivated, false if user not found</returns>
    Task<bool> ReactivateUserAsync(string keycloakId);

    /// <summary>
    /// Gets user statistics
    /// </summary>
    /// <returns>User statistics</returns>
    Task<(int TotalUsers, int ActiveUsers, int Administrators, int RegisteredUsers, int NewUsersLast30Days)> GetUserStatisticsAsync();
}