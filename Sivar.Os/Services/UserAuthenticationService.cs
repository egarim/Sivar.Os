using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service for managing user authentication flow and automatic profile creation
/// </summary>
public class UserAuthenticationService : IUserAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IProfileService _profileService;
    private readonly ILogger<UserAuthenticationService> _logger;

    public UserAuthenticationService(
        IUserRepository userRepository,
        IProfileService profileService,
        ILogger<UserAuthenticationService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles user authentication flow - creates user and default profile if needed
    /// </summary>
    public async Task<UserAuthenticationResult> AuthenticateUserAsync(string keycloakId, UserAuthenticationInfo authInfo)
    {
        try
        {
            // Check if user exists
            var existingUser = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            
            if (existingUser != null)
            {
                // User exists - get their active profile
                var activeProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);                
                return new UserAuthenticationResult
                {
                    IsSuccess = true,
                    User = MapToDto(existingUser),
                    ActiveProfile = activeProfile,
                    IsNewUser = false
                };
            }

            // New user - create user and default profile
            var newUser = await CreateNewUserAsync(keycloakId, authInfo);
            var defaultProfile = await CreateDefaultProfileAsync(newUser, authInfo);

            _logger.LogInformation("Created new user {KeycloakId} with default profile {ProfileId}", 
                keycloakId, defaultProfile?.Id);

            return new UserAuthenticationResult
            {
                IsSuccess = true,
                User = MapToDto(newUser),
                ActiveProfile = defaultProfile,
                IsNewUser = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user authentication for {KeycloakId}", keycloakId);
            
            return new UserAuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "Authentication failed"
            };
        }
    }

    private async Task<User> CreateNewUserAsync(string keycloakId, UserAuthenticationInfo authInfo)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            KeycloakId = keycloakId,
            Email = authInfo.Email,
            FirstName = authInfo.FirstName,
            LastName = authInfo.LastName,
            Role = DetermineUserRole(authInfo.Role),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        
        return user;
    }

    private async Task<ProfileDto?> CreateDefaultProfileAsync(User user, UserAuthenticationInfo authInfo)
    {
        var createDto = new CreateProfileDto
        {
            DisplayName = $"{authInfo.FirstName} {authInfo.LastName}",
            Bio = "Welcome to Sivar! This is your default profile.",
            Metadata = "{}",
            Tags = new List<string> { "new-user" },
            VisibilityLevel = VisibilityLevel.Public
        };

        try
        {
            var profile = await _profileService.CreateProfileAsync(createDto, user.KeycloakId);
            
            // Set as active profile
            if (profile != null)
            {
                await _profileService.SetActiveProfileAsync(user.KeycloakId, profile.Id);
            }
            
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create default profile for user {KeycloakId}", user.KeycloakId);
            return null;
        }
    }

    private static UserRole DetermineUserRole(string roleString)
    {
        return roleString?.ToLower() switch
        {
            "administrator" or "admin" => UserRole.Administrator,
            "moderator" => UserRole.Administrator, // Map moderator to administrator since Moderator role doesn't exist
            _ => UserRole.RegisteredUser
        };
    }

    /// <summary>
    /// Maps User entity to UserDto
    /// </summary>
    private UserDto MapToDto(User user)
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

