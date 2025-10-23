

using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service implementation for User management and auto-registration
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Gets or creates a user from Keycloak authentication
    /// Auto-registers user if they don't exist
    /// </summary>
    public async Task<UserDto> GetOrCreateUserFromKeycloakAsync(CreateUserFromKeycloakDto keycloakUserDto)
    {
        if (keycloakUserDto == null)
            throw new ArgumentNullException(nameof(keycloakUserDto));

        if (string.IsNullOrWhiteSpace(keycloakUserDto.KeycloakId))
            throw new ArgumentException("KeycloakId is required", nameof(keycloakUserDto));

        // Try to get existing user
        var existingUser = await _userRepository.GetByKeycloakIdAsync(keycloakUserDto.KeycloakId);
        if (existingUser != null)
        {
            // Update last login and return existing user
            await _userRepository.UpdateLastLoginAsync(keycloakUserDto.KeycloakId);
            await _userRepository.SaveChangesAsync();
            return MapToUserDto(existingUser);
        }

        // Create new user (auto-registration)
        var newUser = new User
        {
            Email = keycloakUserDto.Email,
            KeycloakId = keycloakUserDto.KeycloakId,
            FirstName = keycloakUserDto.FirstName,
            LastName = keycloakUserDto.LastName,
            Role = keycloakUserDto.Role,
            PreferredLanguage = keycloakUserDto.PreferredLanguage,
            IsActive = true,
            LastLoginAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(newUser);
        await _userRepository.SaveChangesAsync();

        return MapToUserDto(newUser);
    }

    /// <summary>
    /// Gets a user by their Keycloak ID
    /// </summary>
    public async Task<UserDto?> GetUserByKeycloakIdAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return null;

        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        return user != null ? MapToUserDto(user) : null;
    }

    /// <summary>
    /// Gets current user information and updates last login
    /// </summary>
    public async Task<UserDto?> GetCurrentUserAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return null;

        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
            return null;

        // Update last login
        await _userRepository.UpdateLastLoginAsync(keycloakId);
        await _userRepository.SaveChangesAsync();

        return MapToUserDto(user);
    }

    /// <summary>
    /// Updates user preferences
    /// </summary>
    public async Task<UserDto?> UpdateUserPreferencesAsync(string keycloakId, UpdateUserPreferencesDto updateDto)
    {
        if (string.IsNullOrWhiteSpace(keycloakId) || updateDto == null)
            return null;

        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
            return null;

        // Update preferences
        if (!string.IsNullOrWhiteSpace(updateDto.PreferredLanguage))
            user.PreferredLanguage = updateDto.PreferredLanguage;

        if (!string.IsNullOrWhiteSpace(updateDto.TimeZone))
            user.TimeZone = updateDto.TimeZone;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return MapToUserDto(user);
    }

    /// <summary>
    /// Gets all users (admin only)
    /// </summary>
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToUserDto);
    }

    /// <summary>
    /// Gets users by role (admin only)
    /// </summary>
    public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(UserRole role)
    {
        var users = await _userRepository.GetUsersByRoleAsync(role);
        return users.Select(MapToUserDto);
    }

    /// <summary>
    /// Deactivates a user account (admin only)
    /// </summary>
    public async Task<bool> DeactivateUserAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        var result = await _userRepository.DeactivateUserAsync(keycloakId);
        if (result)
            await _userRepository.SaveChangesAsync();

        return result;
    }

    /// <summary>
    /// Reactivates a user account (admin only)
    /// </summary>
    public async Task<bool> ReactivateUserAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        var result = await _userRepository.ReactivateUserAsync(keycloakId);
        if (result)
            await _userRepository.SaveChangesAsync();

        return result;
    }

    /// <summary>
    /// Checks if a user exists by Keycloak ID
    /// </summary>
    public async Task<bool> UserExistsAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        return await _userRepository.ExistsByKeycloakIdAsync(keycloakId);
    }

    /// <summary>
    /// Gets user statistics (admin only)
    /// </summary>
    public async Task<UserStatisticsDto> GetUserStatisticsAsync()
    {
        var stats = await _userRepository.GetUserStatisticsAsync();

        return new UserStatisticsDto
        {
            TotalUsers = stats.TotalUsers,
            ActiveUsers = stats.ActiveUsers,
            Administrators = stats.Administrators,
            RegisteredUsers = stats.RegisteredUsers,
            NewUsersLast30Days = stats.NewUsersLast30Days
        };
    }

    /// <summary>
    /// Maps User entity to UserDto
    /// </summary>
    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            KeycloakId = user.KeycloakId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            PreferredLanguage = user.PreferredLanguage,
            TimeZone = user.TimeZone,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsAdministrator = user.IsAdministrator
        };
    }
}