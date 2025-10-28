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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[UserAuthenticationService.AuthenticateUserAsync] START - RequestId={RequestId}, Timestamp={Timestamp}, KeycloakId={KeycloakId}, Email={Email}, FirstName={FirstName}, LastName={LastName}",
            requestId, startTime, keycloakId, authInfo?.Email, authInfo?.FirstName, authInfo?.LastName);

        try
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(keycloakId))
            {
                _logger.LogError("[UserAuthenticationService.AuthenticateUserAsync] VALIDATION ERROR - RequestId={RequestId}, KeycloakIdNull=true",
                    requestId);
                throw new ArgumentException("KeycloakId cannot be null or empty", nameof(keycloakId));
            }

            if (authInfo == null)
            {
                _logger.LogError("[UserAuthenticationService.AuthenticateUserAsync] VALIDATION ERROR - RequestId={RequestId}, AuthInfoNull=true",
                    requestId);
                throw new ArgumentNullException(nameof(authInfo), "Authentication info cannot be null");
            }

            _logger.LogDebug("[UserAuthenticationService.AuthenticateUserAsync] Input validation passed - RequestId={RequestId}, KeycloakId={KeycloakId}",
                requestId, keycloakId);

            // Validate dependencies
            if (_userRepository == null)
            {
                _logger.LogError("[UserAuthenticationService.AuthenticateUserAsync] CRITICAL - RequestId={RequestId}, UserRepositoryNull=true",
                    requestId);
                throw new InvalidOperationException("UserRepository is not configured");
            }

            if (_profileService == null)
            {
                _logger.LogError("[UserAuthenticationService.AuthenticateUserAsync] CRITICAL - RequestId={RequestId}, ProfileServiceNull=true",
                    requestId);
                throw new InvalidOperationException("ProfileService is not configured");
            }

            // Check if user exists
            _logger.LogInformation("[UserAuthenticationService.AuthenticateUserAsync] Checking for existing user - RequestId={RequestId}, KeycloakId={KeycloakId}",
                requestId, keycloakId);

            var existingUser = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            
            if (existingUser != null)
            {
                _logger.LogInformation("[UserAuthenticationService.AuthenticateUserAsync] Existing user found - RequestId={RequestId}, KeycloakId={KeycloakId}, UserId={UserId}",
                    requestId, keycloakId, existingUser.Id);

                // User exists - get their active profile
                var activeProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);
                
                _logger.LogDebug("[UserAuthenticationService.AuthenticateUserAsync] Retrieved active profile - RequestId={RequestId}, KeycloakId={KeycloakId}, HasActiveProfile={HasActiveProfile}",
                    requestId, keycloakId, activeProfile != null);

                // If no active profile is set, try to find and set the first available profile
                if (activeProfile == null && existingUser.ActiveProfileId == null)
                {
                    _logger.LogWarning("[UserAuthenticationService.AuthenticateUserAsync] No active profile found, attempting to find and set one - RequestId={RequestId}, KeycloakId={KeycloakId}",
                        requestId, keycloakId);
                    
                    var userProfiles = await _profileService.GetMyProfilesAsync(keycloakId);
                    var profileList = userProfiles?.ToList();
                    var profileCount = profileList?.Count ?? 0;
                    
                    _logger.LogInformation("[UserAuthenticationService.AuthenticateUserAsync] Retrieved user profiles - RequestId={RequestId}, KeycloakId={KeycloakId}, ProfileCount={ProfileCount}",
                        requestId, keycloakId, profileCount);

                    if (profileCount > 0)
                    {
                        var firstProfile = profileList!.FirstOrDefault();
                        if (firstProfile != null)
                        {
                            _logger.LogInformation("[UserAuthenticationService.AuthenticateUserAsync] Setting first profile as active - RequestId={RequestId}, KeycloakId={KeycloakId}, ProfileId={ProfileId}",
                                requestId, keycloakId, firstProfile.Id);

                            await _profileService.SetActiveProfileAsync(keycloakId, firstProfile.Id);
                            activeProfile = firstProfile;

                            _logger.LogInformation("[UserAuthenticationService.AuthenticateUserAsync] Profile set as active - RequestId={RequestId}, KeycloakId={KeycloakId}, ProfileId={ProfileId}",
                                requestId, keycloakId, firstProfile.Id);
                        }
                    }
                }
                
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("[UserAuthenticationService.AuthenticateUserAsync] AUTHENTICATED (existing) - RequestId={RequestId}, KeycloakId={KeycloakId}, UserId={UserId}, IsNewUser=false, Duration={Duration}ms",
                    requestId, keycloakId, existingUser.Id, elapsed);

                return new UserAuthenticationResult
                {
                    IsSuccess = true,
                    User = MapToDto(existingUser, requestId),
                    ActiveProfile = activeProfile,
                    IsNewUser = false
                };
            }

            _logger.LogInformation("[UserAuthenticationService.AuthenticateUserAsync] New user detected - RequestId={RequestId}, KeycloakId={KeycloakId}, Creating user and profile",
                requestId, keycloakId);

            // New user - create user and default profile
            var newUser = await CreateNewUserAsync(keycloakId, authInfo, requestId);
            var defaultProfile = await CreateDefaultProfileAsync(newUser, authInfo, requestId);

            var elapsed2 = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[UserAuthenticationService.AuthenticateUserAsync] AUTHENTICATED (new) - RequestId={RequestId}, KeycloakId={KeycloakId}, UserId={UserId}, IsNewUser=true, Duration={Duration}ms",
                requestId, keycloakId, newUser.Id, elapsed2);

            return new UserAuthenticationResult
            {
                IsSuccess = true,
                User = MapToDto(newUser, requestId),
                ActiveProfile = defaultProfile,
                IsNewUser = true
            };
        }
        catch (ArgumentException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[UserAuthenticationService.AuthenticateUserAsync] VALIDATION ERROR - RequestId={RequestId}, KeycloakId={KeycloakId}, Duration={Duration}ms",
                requestId, keycloakId, elapsed);
            
            return new UserAuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[UserAuthenticationService.AuthenticateUserAsync] EXCEPTION - RequestId={RequestId}, KeycloakId={KeycloakId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, keycloakId, ex.GetType().Name, elapsed);
            
            return new UserAuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "Authentication failed: " + ex.Message
            };
        }
    }

    private async Task<User> CreateNewUserAsync(string keycloakId, UserAuthenticationInfo authInfo, Guid requestId)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[UserAuthenticationService.CreateNewUserAsync] START - RequestId={RequestId}, Timestamp={Timestamp}, KeycloakId={KeycloakId}, Email={Email}",
            requestId, startTime, keycloakId, authInfo?.Email);

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(keycloakId))
            {
                _logger.LogError("[UserAuthenticationService.CreateNewUserAsync] VALIDATION ERROR - RequestId={RequestId}, KeycloakIdNull=true",
                    requestId);
                throw new ArgumentException("KeycloakId cannot be null or empty", nameof(keycloakId));
            }

            if (authInfo == null)
            {
                _logger.LogError("[UserAuthenticationService.CreateNewUserAsync] VALIDATION ERROR - RequestId={RequestId}, AuthInfoNull=true",
                    requestId);
                throw new ArgumentNullException(nameof(authInfo), "UserAuthenticationInfo cannot be null");
            }

            _logger.LogDebug("[UserAuthenticationService.CreateNewUserAsync] Input validation passed - RequestId={RequestId}",
                requestId);

            var userRole = DetermineUserRole(authInfo.Role, requestId);

            _logger.LogInformation("[UserAuthenticationService.CreateNewUserAsync] Determined user role - RequestId={RequestId}, KeycloakId={KeycloakId}, Role={Role}",
                requestId, keycloakId, userRole);

            var userId = Guid.NewGuid();
            var nowUtc = DateTime.UtcNow;

            var user = new User
            {
                Id = userId,
                KeycloakId = keycloakId,
                Email = authInfo.Email,
                FirstName = authInfo.FirstName,
                LastName = authInfo.LastName,
                Role = userRole,
                IsActive = true,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            };

            _logger.LogInformation("[UserAuthenticationService.CreateNewUserAsync] User object created - RequestId={RequestId}, UserId={UserId}, Email={Email}, FullName={FullName}, Role={Role}",
                requestId, userId, user.Email, $"{user.FirstName} {user.LastName}", user.Role);

            // Validate repository is available
            if (_userRepository == null)
            {
                _logger.LogError("[UserAuthenticationService.CreateNewUserAsync] CRITICAL - RequestId={RequestId}, UserRepositoryNull=true",
                    requestId);
                throw new InvalidOperationException("UserRepository is not configured");
            }

            _logger.LogInformation("[UserAuthenticationService.CreateNewUserAsync] Adding user to repository - RequestId={RequestId}, UserId={UserId}, KeycloakId={KeycloakId}",
                requestId, userId, keycloakId);

            await _userRepository.AddAsync(user);
            
            _logger.LogDebug("[UserAuthenticationService.CreateNewUserAsync] Saving changes to repository - RequestId={RequestId}, UserId={UserId}",
                requestId, userId);

            await _userRepository.SaveChangesAsync();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[UserAuthenticationService.CreateNewUserAsync] SUCCESS - RequestId={RequestId}, UserId={UserId}, KeycloakId={KeycloakId}, Role={Role}, Duration={Duration}ms",
                requestId, userId, keycloakId, user.Role, elapsed);
            
            return user;
        }
        catch (ArgumentException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[UserAuthenticationService.CreateNewUserAsync] VALIDATION ERROR - RequestId={RequestId}, KeycloakId={KeycloakId}, Duration={Duration}ms",
                requestId, keycloakId, elapsed);
            throw;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[UserAuthenticationService.CreateNewUserAsync] EXCEPTION - RequestId={RequestId}, KeycloakId={KeycloakId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, keycloakId, ex.GetType().Name, elapsed);
            throw;
        }
    }

    private async Task<ProfileDto?> CreateDefaultProfileAsync(User user, UserAuthenticationInfo authInfo, Guid requestId)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[UserAuthenticationService.CreateDefaultProfileAsync] START - RequestId={RequestId}, Timestamp={Timestamp}, UserId={UserId}, KeycloakId={KeycloakId}",
            requestId, startTime, user?.Id, user?.KeycloakId);

        try
        {
            // Validate inputs
            if (user == null)
            {
                _logger.LogError("[UserAuthenticationService.CreateDefaultProfileAsync] VALIDATION ERROR - RequestId={RequestId}, UserNull=true",
                    requestId);
                throw new ArgumentNullException(nameof(user), "User cannot be null");
            }

            if (string.IsNullOrWhiteSpace(user.KeycloakId))
            {
                _logger.LogError("[UserAuthenticationService.CreateDefaultProfileAsync] VALIDATION ERROR - RequestId={RequestId}, KeycloakIdNull=true",
                    requestId);
                throw new InvalidOperationException("User KeycloakId cannot be null or empty");
            }

            if (authInfo == null)
            {
                _logger.LogError("[UserAuthenticationService.CreateDefaultProfileAsync] VALIDATION ERROR - RequestId={RequestId}, AuthInfoNull=true",
                    requestId);
                throw new ArgumentNullException(nameof(authInfo), "UserAuthenticationInfo cannot be null");
            }

            if (_profileService == null)
            {
                _logger.LogError("[UserAuthenticationService.CreateDefaultProfileAsync] CRITICAL - RequestId={RequestId}, ProfileServiceNull=true",
                    requestId);
                throw new InvalidOperationException("ProfileService is not configured");
            }

            _logger.LogDebug("[UserAuthenticationService.CreateDefaultProfileAsync] Input validation passed - RequestId={RequestId}, UserId={UserId}",
                requestId, user.Id);

            var displayName = FormatDisplayName(authInfo.FirstName, authInfo.LastName, requestId);

            var createDto = new CreateProfileDto
            {
                DisplayName = displayName,
                Bio = "Welcome to Sivar! This is your default profile.",
                Metadata = "{}",
                Tags = new List<string> { "new-user" },
                VisibilityLevel = VisibilityLevel.Public
            };

            _logger.LogInformation("[UserAuthenticationService.CreateDefaultProfileAsync] Creating profile - RequestId={RequestId}, UserId={UserId}, KeycloakId={KeycloakId}, DisplayName={DisplayName}",
                requestId, user.Id, user.KeycloakId, displayName);

            var profile = await _profileService.CreateProfileAsync(createDto, user.KeycloakId);

            _logger.LogInformation("[UserAuthenticationService.CreateDefaultProfileAsync] Profile created - RequestId={RequestId}, UserId={UserId}, KeycloakId={KeycloakId}, ProfileId={ProfileId}, ProfileExists={ProfileExists}",
                requestId, user.Id, user.KeycloakId, profile?.Id, profile != null);
            
            // Set as active profile
            if (profile != null)
            {
                _logger.LogInformation("[UserAuthenticationService.CreateDefaultProfileAsync] Setting profile as active - RequestId={RequestId}, UserId={UserId}, ProfileId={ProfileId}",
                    requestId, user.Id, profile.Id);

                try
                {
                    var setActiveResult = await _profileService.SetActiveProfileAsync(user.KeycloakId, profile.Id);

                    _logger.LogInformation("[UserAuthenticationService.CreateDefaultProfileAsync] Profile set as active - RequestId={RequestId}, UserId={UserId}, ProfileId={ProfileId}, Result={Result}",
                        requestId, user.Id, profile.Id, setActiveResult);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[UserAuthenticationService.CreateDefaultProfileAsync] Failed to set profile as active (non-critical) - RequestId={RequestId}, UserId={UserId}, ProfileId={ProfileId}",
                        requestId, user.Id, profile.Id);
                    // Don't throw - profile was created successfully, just couldn't set as active
                }
            }
            else
            {
                _logger.LogWarning("[UserAuthenticationService.CreateDefaultProfileAsync] Profile creation returned null - RequestId={RequestId}, UserId={UserId}, KeycloakId={KeycloakId}",
                    requestId, user.Id, user.KeycloakId);
            }
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[UserAuthenticationService.CreateDefaultProfileAsync] SUCCESS - RequestId={RequestId}, UserId={UserId}, ProfileCreated={ProfileCreated}, ProfileId={ProfileId}, Duration={Duration}ms",
                requestId, user.Id, profile != null, profile?.Id, elapsed);

            return profile;
        }
        catch (ArgumentNullException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[UserAuthenticationService.CreateDefaultProfileAsync] VALIDATION ERROR - RequestId={RequestId}, UserId={UserId}, Duration={Duration}ms",
                requestId, user?.Id, elapsed);
            return null;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[UserAuthenticationService.CreateDefaultProfileAsync] EXCEPTION - RequestId={RequestId}, UserId={UserId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, user?.Id, ex.GetType().Name, elapsed);
            return null;
        }
    }

    private static UserRole DetermineUserRole(string? roleString, Guid requestId)
    {
        var role = roleString?.ToLower() switch
        {
            "administrator" or "admin" => UserRole.Administrator,
            "moderator" => UserRole.Administrator, // Map moderator to administrator since Moderator role doesn't exist
            _ => UserRole.RegisteredUser
        };

        return role;
    }

    /// <summary>
    /// Formats display name from first and last names with proper trimming.
    /// </summary>
    private static string FormatDisplayName(string? firstName, string? lastName, Guid requestId)
    {
        var first = (firstName ?? "").Trim();
        var last = (lastName ?? "").Trim();

        if (!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(last))
        {
            return $"{first} {last}";
        }

        if (!string.IsNullOrEmpty(first))
        {
            return first;
        }

        if (!string.IsNullOrEmpty(last))
        {
            return last;
        }

        return "User";
    }

    /// <summary>
    /// Maps User entity to UserDto
    /// </summary>
    private UserDto MapToDto(User user, Guid requestId)
    {
        _logger.LogInformation("[UserAuthenticationService.MapToDto] START - RequestId={RequestId}, UserId={UserId}, KeycloakId={KeycloakId}",
            requestId, user?.Id, user?.KeycloakId);

        try
        {
            // Validate input
            if (user == null)
            {
                _logger.LogError("[UserAuthenticationService.MapToDto] VALIDATION ERROR - RequestId={RequestId}, UserNull=true",
                    requestId);
                throw new ArgumentNullException(nameof(user), "User cannot be null");
            }

            if (string.IsNullOrWhiteSpace(user.KeycloakId))
            {
                _logger.LogError("[UserAuthenticationService.MapToDto] VALIDATION ERROR - RequestId={RequestId}, KeycloakIdNull=true",
                    requestId);
                throw new InvalidOperationException("User KeycloakId cannot be null or empty");
            }

            var dto = new UserDto
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

            _logger.LogDebug("[UserAuthenticationService.MapToDto] Dto created - RequestId={RequestId}, UserId={UserId}, Email={Email}, FullName={FullName}",
                requestId, dto.Id, dto.Email, dto.FullName);

            _logger.LogInformation("[UserAuthenticationService.MapToDto] SUCCESS - RequestId={RequestId}, UserId={UserId}, Role={Role}, IsAdministrator={IsAdministrator}",
                requestId, dto.Id, dto.Role, dto.IsAdministrator);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserAuthenticationService.MapToDto] EXCEPTION - RequestId={RequestId}, UserId={UserId}, ExceptionType={ExceptionType}",
                requestId, user?.Id, ex.GetType().Name);
            throw;
        }
    }
}

