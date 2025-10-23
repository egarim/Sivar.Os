using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;


namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for User entity operations with Keycloak integration
/// </summary>
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(SivarDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets a user by their Keycloak ID
    /// </summary>
    public async Task<User?> GetByKeycloakIdAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return null;

        return await _dbSet
            .Include(u => u.ActiveProfile)
                .ThenInclude(p => p.ProfileType)
            .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
    }

    /// <summary>
    /// Gets a user by their email address
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    /// <summary>
    /// Checks if a user exists by Keycloak ID
    /// </summary>
    public async Task<bool> ExistsByKeycloakIdAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        return await _dbSet
            .AnyAsync(u => u.KeycloakId == keycloakId);
    }

    /// <summary>
    /// Gets all active users
    /// </summary>
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    /// <summary>
    /// Gets users by role
    /// </summary>
    public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
    {
        return await _dbSet
            .Where(u => u.Role == role && u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    /// <summary>
    /// Updates the user's last login timestamp
    /// </summary>
    public async Task<bool> UpdateLastLoginAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        var user = await GetByKeycloakIdAsync(keycloakId);
        if (user == null)
            return false;

        user.UpdateLastLogin();
        _dbSet.Update(user);
        
        return true;
    }

    /// <summary>
    /// Gets a user with their profiles included
    /// </summary>
    public async Task<User?> GetWithProfilesByKeycloakIdAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return null;

        return await _dbSet
            .Include(u => u.Profiles)
                .ThenInclude(p => p.ProfileType)
            .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
    }

    /// <summary>
    /// Deactivates a user account
    /// </summary>
    public async Task<bool> DeactivateUserAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        var user = await GetByKeycloakIdAsync(keycloakId);
        if (user == null)
            return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(user);

        return true;
    }

    /// <summary>
    /// Reactivates a user account
    /// </summary>
    public async Task<bool> ReactivateUserAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        var user = await _dbSet
            .IgnoreQueryFilters() // Include inactive users
            .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId && !u.IsDeleted);

        if (user == null)
            return false;

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(user);

        return true;
    }

    /// <summary>
    /// Gets user statistics
    /// </summary>
    public async Task<(int TotalUsers, int ActiveUsers, int Administrators, int RegisteredUsers, int NewUsersLast30Days)> GetUserStatisticsAsync()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var totalUsers = await _dbSet.CountAsync();
        var activeUsers = await _dbSet.CountAsync(u => u.IsActive);
        var administrators = await _dbSet.CountAsync(u => u.Role == UserRole.Administrator && u.IsActive);
        var registeredUsers = await _dbSet.CountAsync(u => u.Role == UserRole.RegisteredUser && u.IsActive);
        var newUsersLast30Days = await _dbSet.CountAsync(u => u.CreatedAt >= thirtyDaysAgo);

        return (totalUsers, activeUsers, administrators, registeredUsers, newUsersLast30Days);
    }

    /// <summary>
    /// Gets users created within a date range
    /// </summary>
    public async Task<IEnumerable<User>> GetUsersCreatedBetweenAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Searches users by name or email (admin function)
    /// </summary>
    public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetActiveUsersAsync();

        var term = searchTerm.ToLower();

        return await _dbSet
            .Where(u => u.IsActive && (
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term)
            ))
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }
}