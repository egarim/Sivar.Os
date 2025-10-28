

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
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets or creates a user from Keycloak authentication
    /// Auto-registers user if they don't exist
    /// </summary>
    public async Task<UserDto> GetOrCreateUserFromKeycloakAsync(CreateUserFromKeycloakDto keycloakUserDto)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[UserService.GetOrCreateUserFromKeycloakAsync] START - RequestId={RequestId}, KeycloakId={KeycloakId}, Email={Email}", 
            requestId, keycloakUserDto?.KeycloakId ?? "NULL", keycloakUserDto?.Email ?? "NULL");

        if (keycloakUserDto == null)
        {
            _logger.LogError("[UserService.GetOrCreateUserFromKeycloakAsync] NULL_DTO - RequestId={RequestId}", requestId);
            throw new ArgumentNullException(nameof(keycloakUserDto));
        }

        if (string.IsNullOrWhiteSpace(keycloakUserDto.KeycloakId))
        {
            _logger.LogError("[UserService.GetOrCreateUserFromKeycloakAsync] NULL_KEYCLOAK_ID - RequestId={RequestId}", requestId);
            throw new ArgumentException("KeycloakId is required", nameof(keycloakUserDto));
        }

        // Try to get existing user
        var existingUser = await _userRepository.GetByKeycloakIdAsync(keycloakUserDto.KeycloakId);
        if (existingUser != null)
        {
            _logger.LogInformation("[UserService.GetOrCreateUserFromKeycloakAsync] User found - RequestId={RequestId}, UserId={UserId}", 
                requestId, existingUser.Id);

            // Update last login and return existing user
            await _userRepository.UpdateLastLoginAsync(keycloakUserDto.KeycloakId);
            await _userRepository.SaveChangesAsync();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[UserService.GetOrCreateUserFromKeycloakAsync] SUCCESS (existing) - RequestId={RequestId}, UserId={UserId}, Duration={Duration}ms", 
                requestId, existingUser.Id, elapsed);

            return MapToUserDto(existingUser);
        }

        _logger.LogInformation("[UserService.GetOrCreateUserFromKeycloakAsync] User not found, creating new - RequestId={RequestId}, KeycloakId={KeycloakId}, Email={Email}", 
            requestId, keycloakUserDto.KeycloakId, keycloakUserDto.Email);

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

        _logger.LogInformation("[UserService.GetOrCreateUserFromKeycloakAsync] User created and persisted - RequestId={RequestId}, UserId={UserId}, Email={Email}", 
            requestId, newUser.Id, newUser.Email);

        var totalElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation("[UserService.GetOrCreateUserFromKeycloakAsync] SUCCESS (new) - RequestId={RequestId}, UserId={UserId}, Duration={Duration}ms", 
            requestId, newUser.Id, totalElapsed);

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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[UserService.GetCurrentUserAsync] START - RequestId={RequestId}, KeycloakId={KeycloakId}", 
            requestId, keycloakId ?? "NULL");

        if (string.IsNullOrWhiteSpace(keycloakId))
        {
            _logger.LogWarning("[UserService.GetCurrentUserAsync] NULL_KEYCLOAK_ID - RequestId={RequestId}", requestId);
            return null;
        }

        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
        {
            _logger.LogWarning("[UserService.GetCurrentUserAsync] USER_NOT_FOUND - RequestId={RequestId}, KeycloakId={KeycloakId}", 
                requestId, keycloakId);
            return null;
        }

        _logger.LogInformation("[UserService.GetCurrentUserAsync] User found - RequestId={RequestId}, UserId={UserId}, IsActive={IsActive}", 
            requestId, user.Id, user.IsActive);

        // Update last login
        await _userRepository.UpdateLastLoginAsync(keycloakId);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("[UserService.GetCurrentUserAsync] Last login updated - RequestId={RequestId}, UserId={UserId}", 
            requestId, user.Id);

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation("[UserService.GetCurrentUserAsync] SUCCESS - RequestId={RequestId}, UserId={UserId}, Duration={Duration}ms", 
            requestId, user.Id, elapsed);

        return MapToUserDto(user);
    }

    /// <summary>
    /// Updates user preferences
    /// </summary>
    public async Task<UserDto?> UpdateUserPreferencesAsync(string keycloakId, UpdateUserPreferencesDto updateDto)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[UserService.UpdateUserPreferencesAsync] START - RequestId={RequestId}, KeycloakId={KeycloakId}, Language={Language}, TimeZone={TimeZone}", 
            requestId, keycloakId ?? "NULL", updateDto?.PreferredLanguage ?? "NULL", updateDto?.TimeZone ?? "NULL");

        if (string.IsNullOrWhiteSpace(keycloakId) || updateDto == null)
        {
            _logger.LogWarning("[UserService.UpdateUserPreferencesAsync] INVALID_INPUT - RequestId={RequestId}, KeycloakId={KeycloakId}, DtoNull={DtoNull}", 
                requestId, keycloakId ?? "NULL", updateDto == null);
            return null;
        }

        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
        {
            _logger.LogWarning("[UserService.UpdateUserPreferencesAsync] USER_NOT_FOUND - RequestId={RequestId}, KeycloakId={KeycloakId}", 
                requestId, keycloakId);
            return null;
        }

        _logger.LogInformation("[UserService.UpdateUserPreferencesAsync] User found - RequestId={RequestId}, UserId={UserId}", 
            requestId, user.Id);

        var originalLanguage = user.PreferredLanguage;
        var originalTimeZone = user.TimeZone;

        // Update preferences
        if (!string.IsNullOrWhiteSpace(updateDto.PreferredLanguage))
        {
            user.PreferredLanguage = updateDto.PreferredLanguage;
            _logger.LogInformation("[UserService.UpdateUserPreferencesAsync] Language updated - RequestId={RequestId}, Old={Old}, New={New}", 
                requestId, originalLanguage, updateDto.PreferredLanguage);
        }

        if (!string.IsNullOrWhiteSpace(updateDto.TimeZone))
        {
            user.TimeZone = updateDto.TimeZone;
            _logger.LogInformation("[UserService.UpdateUserPreferencesAsync] TimeZone updated - RequestId={RequestId}, Old={Old}, New={New}", 
                requestId, originalTimeZone, updateDto.TimeZone);
        }

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("[UserService.UpdateUserPreferencesAsync] Preferences persisted - RequestId={RequestId}, UserId={UserId}", 
            requestId, user.Id);

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation("[UserService.UpdateUserPreferencesAsync] SUCCESS - RequestId={RequestId}, UserId={UserId}, Duration={Duration}ms", 
            requestId, user.Id, elapsed);

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