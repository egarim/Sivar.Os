using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for managing saved AI chat results
/// </summary>
[ApiController]
[Route("api/profiles/{profileId:guid}/[controller]")]
public class SavedResultsController : ControllerBase
{
    private readonly ISavedResultService _savedResultService;
    private readonly IProfileService _profileService;
    private readonly ILogger<SavedResultsController> _logger;

    public SavedResultsController(
        ISavedResultService savedResultService,
        IProfileService profileService,
        ILogger<SavedResultsController> logger)
    {
        _savedResultService = savedResultService ?? throw new ArgumentNullException(nameof(savedResultService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all saved results for a profile with optional type filter
    /// </summary>
    /// <param name="profileId">Profile ID from route</param>
    /// <param name="resultType">Optional result type to filter by</param>
    /// <returns>List of saved results</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SavedResultDto>>> GetProfileSavedResults(
        Guid profileId,
        [FromQuery] string? resultType = null)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            // Verify profile exists
            var profile = await _profileService.GetPublicProfileAsync(profileId);
            if (profile == null)
                return NotFound("Profile not found");

            // TODO: Verify user owns this profile when authentication is implemented

            var results = await _savedResultService.GetProfileSavedResultsAsync(profileId, resultType);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved results for profile {ProfileId}", profileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Saves a chat result for later reference
    /// </summary>
    /// <param name="profileId">Profile ID from route</param>
    /// <param name="createDto">Saved result creation data</param>
    /// <returns>Created saved result</returns>
    [HttpPost]
    public async Task<ActionResult<SavedResultDto>> SaveResult(
        Guid profileId,
        [FromBody] CreateSavedResultDto createDto)
    {
        try
        {
            if (createDto == null)
                return BadRequest("Saved result data is required");

            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            // Verify profile exists
            var profile = await _profileService.GetPublicProfileAsync(profileId);
            if (profile == null)
                return NotFound("Profile not found");

            // TODO: Verify user owns this profile when authentication is implemented

            var savedResult = await _savedResultService.SaveResultAsync(profileId, createDto);

            return CreatedAtAction(
                nameof(GetProfileSavedResults),
                new { profileId = profileId },
                savedResult);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when saving result for profile {ProfileId}", profileId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving result for profile {ProfileId}", profileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a specific saved result
    /// </summary>
    /// <param name="profileId">Profile ID from route</param>
    /// <param name="resultId">Saved result ID to delete</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{resultId:guid}")]
    public async Task<ActionResult> DeleteSavedResult(Guid profileId, Guid resultId)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            // Verify profile exists
            var profile = await _profileService.GetPublicProfileAsync(profileId);
            if (profile == null)
                return NotFound("Profile not found");

            // TODO: Verify user owns this profile when authentication is implemented

            var success = await _savedResultService.DeleteSavedResultAsync(resultId, profileId);
            
            if (!success)
                return NotFound("Saved result not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved result {ResultId} for profile {ProfileId}", resultId, profileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Clears all saved results for a profile
    /// </summary>
    /// <param name="profileId">Profile ID from route</param>
    /// <returns>Number of deleted results</returns>
    [HttpDelete]
    public async Task<ActionResult<int>> ClearSavedResults(Guid profileId)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            // Verify profile exists
            var profile = await _profileService.GetPublicProfileAsync(profileId);
            if (profile == null)
                return NotFound("Profile not found");

            // TODO: Verify user owns this profile when authentication is implemented

            var deletedCount = await _savedResultService.ClearProfileSavedResultsAsync(profileId);

            return Ok(new { deletedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing saved results for profile {ProfileId}", profileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get Keycloak ID from the 'sub' claim (OpenID Connect standard)
    /// </summary>
    /// <returns>Keycloak ID or empty string if not found</returns>
    private string GetKeycloakIdFromRequest()
    {
        // Check for mock authentication header (for integration tests)
        if (Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            return keycloakIdHeader.ToString();
        }

        // Check if user is authenticated via claims
        if (User?.Identity?.IsAuthenticated == true)
        {
            var subClaim = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(subClaim))
            {
                return subClaim;
            }

            // Fallback: try to find "user_id" or "id" claims if "sub" is not available
            var userIdClaim = User.FindFirst("user_id")?.Value 
                           ?? User.FindFirst("id")?.Value 
                           ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                return userIdClaim;
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
}
