using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for user management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets current user information (auto-registers if new)
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[UsersController.GetCurrentUser] START - RequestId={RequestId}", requestId);

        try
        {
            // TODO: Extract Keycloak ID from JWT token when authentication is implemented
            // For now, we'll use a placeholder method
            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[UsersController.GetCurrentUser] KeycloakId extracted: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);
            
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[UsersController.GetCurrentUser] UNAUTHORIZED - No KeycloakId, RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            var user = await _userService.GetCurrentUserAsync(keycloakId);
            
            if (user == null)
            {
                _logger.LogInformation("[UsersController.GetCurrentUser] User not found, auto-registering - KeycloakId={KeycloakId}, RequestId={RequestId}", 
                    keycloakId, requestId);
                
                // Auto-register new user from Keycloak claims
                var newUserDto = CreateUserDtoFromKeycloakClaims();
                user = await _userService.GetOrCreateUserFromKeycloakAsync(newUserDto);
                
                _logger.LogInformation("[UsersController.GetCurrentUser] New user created - UserId={UserId}, RequestId={RequestId}", 
                    user?.Id, requestId);
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[UsersController.GetCurrentUser] SUCCESS - UserId={UserId}, RequestId={RequestId}, Duration={Duration}ms", 
                user?.Id, requestId, elapsed);

            return Ok(user);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[UsersController.GetCurrentUser] ERROR - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates user preferences
    /// </summary>
    /// <param name="updateDto">Updated user preferences</param>
    /// <returns>Updated user information</returns>
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateUserPreferences([FromBody] UpdateUserPreferencesDto updateDto)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[UsersController.UpdateUserPreferences] START - RequestId={RequestId}", requestId);

        try
        {
            if (updateDto == null)
            {
                _logger.LogWarning("[UsersController.UpdateUserPreferences] BAD_REQUEST - Null updateDto, RequestId={RequestId}", requestId);
                return BadRequest("Update data is required");
            }

            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[UsersController.UpdateUserPreferences] KeycloakId: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[UsersController.UpdateUserPreferences] UNAUTHORIZED - RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            var updatedUser = await _userService.UpdateUserPreferencesAsync(keycloakId, updateDto);
            
            if (updatedUser == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[UsersController.UpdateUserPreferences] NOT_FOUND - KeycloakId={KeycloakId}, RequestId={RequestId}, Duration={Duration}ms", 
                    keycloakId, requestId, elapsed);
                return NotFound("User not found");
            }

            var successElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[UsersController.UpdateUserPreferences] SUCCESS - UserId={UserId}, RequestId={RequestId}, Duration={Duration}ms", 
                updatedUser.Id, requestId, successElapsed);

            return Ok(updatedUser);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[UsersController.UpdateUserPreferences] ERROR - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all users (admin only)
    /// </summary>
    /// <returns>List of all users</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        try
        {
            // TODO: Add authorization check for admin role when authentication is implemented
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets users by role (admin only)
    /// </summary>
    /// <param name="role">User role to filter by</param>
    /// <returns>List of users with specified role</returns>
    [HttpGet("by-role/{role}")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByRole(UserRole role)
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            var users = await _userService.GetUsersByRoleAsync(role);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by role");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets user statistics (admin only)
    /// </summary>
    /// <returns>User statistics</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<UserStatisticsDto>> GetUserStatistics()
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            var statistics = await _userService.GetUserStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user statistics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deactivates a user account (admin only)
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>Success status</returns>
    [HttpPut("{keycloakId}/deactivate")]
    public async Task<ActionResult> DeactivateUser(string keycloakId)
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            if (string.IsNullOrWhiteSpace(keycloakId))
                return BadRequest("Keycloak ID is required");

            var result = await _userService.DeactivateUserAsync(keycloakId);
            
            if (!result)
                return NotFound("User not found");

            return Ok(new { message = "User deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Reactivates a user account (admin only)
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>Success status</returns>
    [HttpPut("{keycloakId}/reactivate")]
    public async Task<ActionResult> ReactivateUser(string keycloakId)
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            if (string.IsNullOrWhiteSpace(keycloakId))
                return BadRequest("Keycloak ID is required");

            var result = await _userService.ReactivateUserAsync(keycloakId);
            
            if (!result)
                return NotFound("User not found");

            return Ok(new { message = "User reactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating user");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Extracts Keycloak ID from JWT token or mock header
    /// </summary>
    private string GetKeycloakIdFromRequest()
    {
        // Check for mock authentication header (for integration tests)
        if (Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            return keycloakIdHeader.ToString();
        }

        // Check if user is authenticated via JWT Bearer token
        if (User?.Identity?.IsAuthenticated == true)
        {
            // Try to get the "sub" (subject) claim - the standard Keycloak user ID claim
            var keycloakIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("sub")?.Value
                               ?? User.FindFirst("user_id")?.Value;
            
            if (!string.IsNullOrEmpty(keycloakIdClaim))
            {
                _logger.LogInformation($"[UsersController] Extracted Keycloak ID from JWT: {keycloakIdClaim}");
                return keycloakIdClaim;
            }
        }

        // Only return fallback if we have mock auth header (X-Mock-Auth) indicating this is a test scenario
        if (Request.Headers.ContainsKey("X-Mock-Auth"))
        {
            return "mock-keycloak-user-id";
        }

        // No authentication found
        return null!;
    }

    /// <summary>
    /// Placeholder method to check if current user is administrator
    /// TODO: Implement actual role checking when Keycloak is integrated
    /// </summary>
    private bool IsAdministrator()
    {
        // For development/testing purposes, return true
        // In production, this would check the role claim from JWT token
        return true;
    }

    /// <summary>
    /// Creates user DTO from Keycloak claims in JWT token
    /// </summary>
    private CreateUserFromKeycloakDto CreateUserDtoFromKeycloakClaims()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            // Fallback for unauthenticated requests
            return new CreateUserFromKeycloakDto
            {
                KeycloakId = "mock-keycloak-user-id",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.RegisteredUser,
                PreferredLanguage = "en"
            };
        }

        // Extract claims from JWT token
        var keycloakId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value
                      ?? User.FindFirst("user_id")?.Value
                      ?? "unknown-id";

        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                 ?? User.FindFirst("email")?.Value
                 ?? User.FindFirst("preferred_username")?.Value
                 ?? "user@example.com";

        var firstName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value
                     ?? User.FindFirst("given_name")?.Value
                     ?? User.FindFirst("firstName")?.Value
                     ?? "";

        var lastName = User.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value
                    ?? User.FindFirst("family_name")?.Value
                    ?? User.FindFirst("lastName")?.Value
                    ?? "";

        _logger.LogInformation($"[UsersController] Creating user from Keycloak claims: KeycloakId={keycloakId}, Email={email}, FirstName={firstName}, LastName={lastName}");

        return new CreateUserFromKeycloakDto
        {
            KeycloakId = keycloakId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = UserRole.RegisteredUser,
            PreferredLanguage = "en"
        };
    }
}