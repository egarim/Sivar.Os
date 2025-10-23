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
        try
        {
            // TODO: Extract Keycloak ID from JWT token when authentication is implemented
            // For now, we'll use a placeholder method
            var keycloakId = GetKeycloakIdFromRequest();
            
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var user = await _userService.GetCurrentUserAsync(keycloakId);
            
            if (user == null)
            {
                // Auto-register new user from Keycloak claims
                var newUserDto = CreateUserDtoFromKeycloakClaims();
                user = await _userService.GetOrCreateUserFromKeycloakAsync(newUserDto);
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
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
        try
        {
            if (updateDto == null)
                return BadRequest("Update data is required");

            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var updatedUser = await _userService.UpdateUserPreferencesAsync(keycloakId, updateDto);
            
            if (updatedUser == null)
                return NotFound("User not found");

            return Ok(updatedUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user preferences");
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
    /// Placeholder method to extract Keycloak ID from JWT token
    /// TODO: Implement actual JWT token parsing when Keycloak is integrated
    /// </summary>
    private string GetKeycloakIdFromRequest()
    {
        // For development/testing purposes, return a mock value
        // In production, this would extract the "sub" claim from the JWT token
        return "mock-keycloak-user-id";
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
    /// Placeholder method to create user DTO from Keycloak claims
    /// TODO: Implement actual claim parsing when Keycloak is integrated
    /// </summary>
    private CreateUserFromKeycloakDto CreateUserDtoFromKeycloakClaims()
    {
        // For development/testing purposes, return mock data
        // In production, this would extract claims from JWT token
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
}