using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for profile type management (admin only)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProfileTypesController : ControllerBase
{
    private readonly IProfileTypeService _profileTypeService;
    private readonly ILogger<ProfileTypesController> _logger;

    public ProfileTypesController(IProfileTypeService profileTypeService, ILogger<ProfileTypesController> logger)
    {
        _profileTypeService = profileTypeService ?? throw new ArgumentNullException(nameof(profileTypeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all active profile types available for users
    /// </summary>
    /// <returns>List of active profile types</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProfileTypeDto>>> GetActiveProfileTypes()
    {
        try
        {
            var profileTypes = await _profileTypeService.GetActiveProfileTypesAsync();
            return Ok(profileTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active profile types");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all profile types including inactive ones (admin only)
    /// </summary>
    /// <returns>List of all profile types</returns>
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<ProfileTypeDto>>> GetAllProfileTypes()
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            var profileTypes = await _profileTypeService.GetAllProfileTypesAsync();
            return Ok(profileTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all profile types");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific profile type by ID
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <returns>Profile type details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProfileTypeDto>> GetProfileType(Guid id)
    {
        try
        {
            var profileType = await _profileTypeService.GetProfileTypeByIdAsync(id);
            
            if (profileType == null)
                return NotFound("Profile type not found");

            return Ok(profileType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile type {ProfileTypeId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets profile types with usage statistics (admin only)
    /// </summary>
    /// <returns>List of profile types with usage counts</returns>
    [HttpGet("with-usage")]
    public async Task<ActionResult<IEnumerable<ProfileTypeDto>>> GetProfileTypesWithUsage()
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            var profileTypes = await _profileTypeService.GetProfileTypesWithUsageAsync();
            return Ok(profileTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile types with usage");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new profile type (admin only)
    /// </summary>
    /// <param name="createDto">Profile type creation data</param>
    /// <returns>Created profile type</returns>
    [HttpPost]
    public async Task<ActionResult<ProfileTypeDto>> CreateProfileType([FromBody] CreateProfileTypeDto createDto)
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            if (createDto == null)
                return BadRequest("Profile type data is required");

            // Validate name is available
            if (!await _profileTypeService.IsNameAvailableAsync(createDto.Name))
                return Conflict("Profile type name already exists");

            var profileType = await _profileTypeService.CreateProfileTypeAsync(createDto);
            
            if (profileType == null)
                return BadRequest("Failed to create profile type");

            return CreatedAtAction(
                nameof(GetProfileType), 
                new { id = profileType.Id }, 
                profileType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile type");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates an existing profile type (admin only)
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <param name="updateDto">Profile type update data</param>
    /// <returns>Updated profile type</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ProfileTypeDto>> UpdateProfileType(Guid id, [FromBody] UpdateProfileTypeDto updateDto)
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            if (updateDto == null)
                return BadRequest("Update data is required");

            var updatedProfileType = await _profileTypeService.UpdateProfileTypeAsync(id, updateDto);
            
            if (updatedProfileType == null)
                return NotFound("Profile type not found");

            return Ok(updatedProfileType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile type {ProfileTypeId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a profile type (admin only)
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProfileType(Guid id)
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            var result = await _profileTypeService.DeleteProfileTypeAsync(id);
            
            if (!result)
                return NotFound("Profile type not found");

            return Ok(new { message = "Profile type deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile type {ProfileTypeId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Activates a profile type (admin only)
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <returns>Success status</returns>
    [HttpPut("{id}/activate")]
    public async Task<ActionResult> ActivateProfileType(Guid id)
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            var result = await _profileTypeService.ActivateProfileTypeAsync(id);
            
            if (!result)
                return NotFound("Profile type not found");

            return Ok(new { message = "Profile type activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating profile type {ProfileTypeId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deactivates a profile type (admin only)
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <returns>Success status</returns>
    [HttpPut("{id}/deactivate")]
    public async Task<ActionResult> DeactivateProfileType(Guid id)
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            var result = await _profileTypeService.DeactivateProfileTypeAsync(id);
            
            if (!result)
                return NotFound("Profile type not found");

            return Ok(new { message = "Profile type deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating profile type {ProfileTypeId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates sort orders for multiple profile types (admin only)
    /// </summary>
    /// <param name="sortOrderUpdates">Dictionary of profile type ID to new sort order</param>
    /// <returns>Success status</returns>
    [HttpPut("sort-orders")]
    public async Task<ActionResult> UpdateSortOrders([FromBody] Dictionary<Guid, int> sortOrderUpdates)
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            if (sortOrderUpdates == null || !sortOrderUpdates.Any())
                return BadRequest("Sort order updates are required");

            var result = await _profileTypeService.UpdateSortOrdersAsync(sortOrderUpdates);
            
            if (!result)
                return BadRequest("Failed to update sort orders");

            return Ok(new { message = "Sort orders updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile type sort orders");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Checks if a profile type name is available
    /// </summary>
    /// <param name="name">Profile type name to check</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates)</param>
    /// <returns>Availability status</returns>
    [HttpGet("check-name-availability")]
    public async Task<ActionResult<bool>> CheckNameAvailability(
        [FromQuery] string name, 
        [FromQuery] Guid? excludeId = null)
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name parameter is required");

            var isAvailable = await _profileTypeService.IsNameAvailableAsync(name, excludeId);
            return Ok(new { name, isAvailable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking profile type name availability");
            return StatusCode(500, "Internal server error");
        }
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
}