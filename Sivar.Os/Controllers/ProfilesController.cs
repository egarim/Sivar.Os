using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for profile management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProfilesController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfilesController> _logger;

    public ProfilesController(IProfileService profileService, ILogger<ProfilesController> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current user's personal profile
    /// </summary>
    /// <returns>User's personal profile</returns>
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<ProfileDto>> GetMyProfile()
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ProfilesController.GetMyProfile] START - RequestId={RequestId}", requestId);

        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ProfilesController.GetMyProfile] KeycloakId extracted: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ProfilesController.GetMyProfile] UNAUTHORIZED - No KeycloakId found, RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            var profile = await _profileService.GetMyProfileAsync(keycloakId);
            
            if (profile == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ProfilesController.GetMyProfile] NOT_FOUND - KeycloakId={KeycloakId}, RequestId={RequestId}, Duration={Duration}ms", 
                    keycloakId, requestId, elapsed);
                return NotFound("Profile not found");
            }

            var successElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfilesController.GetMyProfile] SUCCESS - ProfileId={ProfileId}, RequestId={RequestId}, Duration={Duration}ms", 
                profile.Id, requestId, successElapsed);

            return Ok(profile);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfilesController.GetMyProfile] ERROR - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a personal profile for the current user
    /// </summary>
    /// <param name="createDto">Profile creation data</param>
    /// <returns>Created profile</returns>
    [HttpPost("my")]
    public async Task<ActionResult<ProfileDto>> CreateMyProfile([FromBody] CreateProfileDto createDto)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ProfilesController.CreateMyProfile] START - RequestId={RequestId}, DisplayName={DisplayName}", 
            requestId, createDto?.DisplayName);

        try
        {
            if (createDto == null)
            {
                _logger.LogWarning("[ProfilesController.CreateMyProfile] BAD_REQUEST - Null createDto, RequestId={RequestId}", requestId);
                return BadRequest("Profile data is required");
            }

            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ProfilesController.CreateMyProfile] KeycloakId extracted: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ProfilesController.CreateMyProfile] UNAUTHORIZED - No KeycloakId found, RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            // Validate profile data
            _logger.LogInformation("[ProfilesController.CreateMyProfile] Validating profile data - RequestId={RequestId}", requestId);
            var validation = await _profileService.ValidateProfileDataAsync(createDto);
            if (!validation.IsValid)
            {
                _logger.LogWarning("[ProfilesController.CreateMyProfile] VALIDATION_FAILED - Errors={Errors}, RequestId={RequestId}", 
                    string.Join(", ", validation.Errors), requestId);
                return BadRequest(new { errors = validation.Errors });
            }

            // Check if user already has a personal profile
            _logger.LogInformation("[ProfilesController.CreateMyProfile] Checking for existing profile - RequestId={RequestId}", requestId);
            if (await _profileService.UserHasPersonalProfileAsync(keycloakId))
            {
                _logger.LogWarning("[ProfilesController.CreateMyProfile] CONFLICT - User already has personal profile, KeycloakId={KeycloakId}, RequestId={RequestId}", 
                    keycloakId, requestId);
                return Conflict("User already has a personal profile");
            }

            _logger.LogInformation("[ProfilesController.CreateMyProfile] Creating profile - RequestId={RequestId}", requestId);
            var profile = await _profileService.CreateMyProfileAsync(keycloakId, createDto);
            
            if (profile == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ProfilesController.CreateMyProfile] FAILED - Service returned null, RequestId={RequestId}, Duration={Duration}ms", 
                    requestId, elapsed);
                return BadRequest("Failed to create profile");
            }

            var successElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfilesController.CreateMyProfile] SUCCESS - ProfileId={ProfileId}, RequestId={RequestId}, Duration={Duration}ms", 
                profile.Id, requestId, successElapsed);

            return CreatedAtAction(
                nameof(GetMyProfile), 
                profile);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfilesController.CreateMyProfile] ERROR - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates the current user's personal profile
    /// </summary>
    /// <param name="updateDto">Profile update data</param>
    /// <returns>Updated profile</returns>
    [HttpPut("my")]
    public async Task<ActionResult<ProfileDto>> UpdateMyProfile([FromBody] UpdateProfileDto updateDto)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ProfilesController.UpdateMyProfile] START - RequestId={RequestId}", requestId);

        try
        {
            if (updateDto == null)
            {
                _logger.LogWarning("[ProfilesController.UpdateMyProfile] BAD_REQUEST - Null updateDto, RequestId={RequestId}", requestId);
                return BadRequest("Update data is required");
            }

            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ProfilesController.UpdateMyProfile] KeycloakId extracted: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ProfilesController.UpdateMyProfile] UNAUTHORIZED - No KeycloakId found, RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            _logger.LogInformation("[ProfilesController.UpdateMyProfile] Updating profile - RequestId={RequestId}", requestId);
            var updatedProfile = await _profileService.UpdateMyProfileAsync(keycloakId, updateDto);
            
            if (updatedProfile == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ProfilesController.UpdateMyProfile] NOT_FOUND - KeycloakId={KeycloakId}, RequestId={RequestId}, Duration={Duration}ms", 
                    keycloakId, requestId, elapsed);
                return NotFound("Profile not found");
            }

            var successElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfilesController.UpdateMyProfile] SUCCESS - ProfileId={ProfileId}, RequestId={RequestId}, Duration={Duration}ms", 
                updatedProfile.Id, requestId, successElapsed);

            return Ok(updatedProfile);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfilesController.UpdateMyProfile] ERROR - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes the current user's personal profile
    /// </summary>
    /// <returns>Success status</returns>
    [HttpDelete("my")]
    public async Task<ActionResult> DeleteMyProfile()
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var result = await _profileService.DeleteMyProfileAsync(keycloakId);
            
            if (!result)
                return NotFound("Profile not found");

            return Ok(new { message = "Profile deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user's profile");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all profiles for the current user
    /// </summary>
    /// <returns>List of user's profiles</returns>
    [HttpGet("my/all")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ProfileDto>>> GetMyProfiles()
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var profiles = await _profileService.GetMyProfilesAsync(keycloakId);
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user's profiles");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets the current user's active profile
    /// </summary>
    /// <returns>User's active profile</returns>
    [HttpGet("my/active")]
    [Authorize]
    public async Task<ActionResult<ProfileDto>> GetMyActiveProfile()
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
            
            if (profile == null)
                return NotFound("No active profile found");

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user's active profile");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets the current user's active profile (alternative endpoint)
    /// </summary>
    /// <returns>User's active profile</returns>
    [HttpGet("active")]
    public async Task<ActionResult<ProfileDto>> GetActiveProfile()
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
            
            if (profile == null)
                return NotFound("No active profile found");

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user's active profile");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Sets a profile as active for the current user
    /// </summary>
    /// <param name="profileId">Profile ID to set as active</param>
    /// <returns>Success status</returns>
    [HttpPut("my/{profileId}/set-active")]
    public async Task<ActionResult> SetActiveProfile(Guid profileId)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var result = await _profileService.SetActiveProfileAsync(keycloakId, profileId);
            
            if (!result)
                return NotFound("Profile not found or does not belong to user");

            return Ok(new { message = "Profile set as active successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active profile");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Activates a profile for the current user (ACTION_PLAN.md required endpoint)
    /// </summary>
    /// <param name="id">Profile ID to activate</param>
    /// <returns>Success status</returns>
    [HttpPut("{id}/activate")]
    public async Task<IActionResult> ActivateProfile(Guid id)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var result = await _profileService.SetActiveProfileAsync(keycloakId, id);
            
            if (!result)
                return NotFound("Profile not found or does not belong to user");

            return Ok(new { message = "Profile activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating profile");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new profile for the current user (supports all profile types)
    /// </summary>
    /// <param name="createDto">Profile creation data</param>
    /// <returns>Created profile</returns>
    [HttpPost]
    public async Task<ActionResult<ProfileDto>> CreateProfile([FromBody] CreateAnyProfileDto createDto)
    {
        try
        {
            if (createDto == null)
                return BadRequest("Profile data is required");

            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            // Convert to CreateProfileDto for service
            var createProfileDto = new CreateProfileDto
            {
                DisplayName = createDto.DisplayName,
                Bio = createDto.Bio,
                Avatar = createDto.Avatar,
                AvatarFileId = createDto.AvatarFileId,
                Location = createDto.Location,
                IsPublic = createDto.VisibilityLevel != Sivar.Os.Shared.Enums.VisibilityLevel.Private,
                VisibilityLevel = createDto.VisibilityLevel,
                Tags = createDto.Tags,
                SocialMediaLinks = new Dictionary<string, string>(),
                Metadata = createDto.Metadata
            };

            // Enhanced validation with business rules - pass ProfileTypeId for validation
            var validation = await _profileService.ValidateProfileCreationAsync(createProfileDto, keycloakId, createDto.ProfileTypeId);
            if (!validation.IsValid)
                return BadRequest(new { errors = validation.Errors });

            var profile = await _profileService.CreateProfileAsync(createProfileDto, keycloakId, createDto.ProfileTypeId);
            
            if (profile == null)
                return BadRequest("Failed to create profile");

            return CreatedAtAction(
                nameof(GetProfile), 
                new { id = profile.Id },
                profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific profile by ID (with permissions check)
    /// </summary>
    /// <param name="id">Profile ID</param>
    /// <returns>Profile details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProfileDto>> GetProfile(Guid id)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("[ProfilesController.GetProfile] START - ProfileId={ProfileId}", id);

        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ProfilesController.GetProfile] KeycloakId: {KeycloakId}", keycloakId ?? "ANONYMOUS");
            
            // First try to get as public profile (increments view count)
            _logger.LogInformation("[ProfilesController.GetProfile] Attempting public profile fetch - ProfileId={ProfileId}", id);
            var profile = await _profileService.GetPublicProfileAsync(id);
            
            if (profile == null && !string.IsNullOrEmpty(keycloakId))
            {
                // If not public, check if user owns this profile
                _logger.LogInformation("[ProfilesController.GetProfile] Public profile not found, checking ownership - ProfileId={ProfileId}, KeycloakId={KeycloakId}", 
                    id, keycloakId);
                var userProfiles = await _profileService.GetMyProfilesAsync(keycloakId);
                profile = userProfiles.FirstOrDefault(p => p.Id == id);
                
                if (profile != null)
                {
                    _logger.LogInformation("[ProfilesController.GetProfile] Found as owned profile - ProfileId={ProfileId}", id);
                }
            }
            else if (profile != null)
            {
                _logger.LogInformation("[ProfilesController.GetProfile] Found as public profile - ProfileId={ProfileId}", id);
            }
            
            if (profile == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ProfilesController.GetProfile] NOT_FOUND - ProfileId={ProfileId}, Duration={Duration}ms", 
                    id, elapsed);
                return NotFound("Profile not found");
            }

            var successElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfilesController.GetProfile] SUCCESS - ProfileId={ProfileId}, Duration={Duration}ms", 
                id, successElapsed);

            return Ok(profile);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfilesController.GetProfile] ERROR - ProfileId={ProfileId}, Duration={Duration}ms", 
                id, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a profile by identifier (GUID or slug like "jose-ojeda")
    /// </summary>
    /// <param name="identifier">Profile ID (GUID) or DisplayName slug</param>
    /// <returns>Profile details</returns>
    [HttpGet("by-identifier/{identifier}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProfileDto>> GetProfileByIdentifier(string identifier)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("[ProfilesController.GetProfileByIdentifier] START - Identifier={Identifier}", identifier);

        try
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                _logger.LogWarning("[ProfilesController.GetProfileByIdentifier] Empty identifier provided");
                return BadRequest("Identifier is required");
            }

            var profile = await _profileService.GetProfileByIdentifierAsync(identifier);
            
            if (profile == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ProfilesController.GetProfileByIdentifier] Profile not found - Identifier={Identifier}, Duration={Duration}ms", 
                    identifier, elapsed);
                return NotFound($"Profile not found for identifier: {identifier}");
            }

            var successElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfilesController.GetProfileByIdentifier] SUCCESS - Identifier={Identifier}, ProfileId={ProfileId}, Duration={Duration}ms", 
                identifier, profile.Id, successElapsed);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfilesController.GetProfileByIdentifier] ERROR - Identifier={Identifier}, Duration={Duration}ms", 
                identifier, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates a specific profile by ID (with ownership validation)
    /// </summary>
    /// <param name="id">Profile ID to update</param>
    /// <param name="updateDto">Profile update data</param>
    /// <returns>Updated profile</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ProfileDto>> UpdateProfile(Guid id, [FromBody] UpdateProfileDto updateDto)
    {
        try
        {
            if (updateDto == null)
                return BadRequest("Update data is required");

            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var updatedProfile = await _profileService.UpdateProfileAsync(id, updateDto, keycloakId);
            
            if (updatedProfile == null)
                return NotFound("Profile not found or access denied");

            return Ok(updatedProfile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile {ProfileId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a specific profile by ID (with ownership validation)
    /// </summary>
    /// <param name="id">Profile ID to delete</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProfile(Guid id)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var result = await _profileService.DeleteProfileAsync(id, keycloakId);
            
            if (!result)
                return NotFound("Profile not found or access denied");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile {ProfileId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Sets a profile as active for the current user
    /// </summary>
    /// <param name="id">Profile ID to set as active</param>
    /// <returns>Success status</returns>
    [HttpPut("{id}/set-active")]
    public async Task<ActionResult> SetProfileAsActive(Guid id)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var result = await _profileService.SetActiveProfileAsync(keycloakId, id);
            
            if (!result)
                return NotFound("Profile not found or does not belong to user");

            return Ok(new { message = "Profile set as active successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting profile {ProfileId} as active", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets public profiles with pagination
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of profiles per page</param>
    /// <returns>Paginated list of public profiles</returns>
    [HttpGet("public")]
    public async Task<ActionResult<PagedResult<ProfileSummaryDto>>> GetPublicProfiles(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _profileService.GetPublicProfilesAsync(page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public profiles");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Searches public profiles by display name or bio
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of profiles per page</param>
    /// <returns>Paginated search results</returns>
    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<ProfileSummaryDto>>> SearchProfiles(
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("[ProfilesController.SearchProfiles] START - SearchTerm={SearchTerm}, Page={Page}, PageSize={PageSize}", 
            searchTerm, page, pageSize);

        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _logger.LogWarning("[ProfilesController.SearchProfiles] BAD_REQUEST - Empty search term");
                return BadRequest("Search term is required");
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _profileService.SearchProfilesAsync(searchTerm, page, pageSize);
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var itemCount = result?.Items != null ? result.Items.Count() : 0;
            _logger.LogInformation("[ProfilesController.SearchProfiles] SUCCESS - SearchTerm={SearchTerm}, ResultsCount={Count}, Duration={Duration}ms", 
                searchTerm, itemCount, elapsed);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfilesController.SearchProfiles] ERROR - SearchTerm={SearchTerm}, Duration={Duration}ms", 
                searchTerm, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets profiles by location
    /// </summary>
    /// <param name="location">Location search term</param>
    /// <returns>List of profiles in the specified location</returns>
    [HttpGet("by-location")]
    public async Task<ActionResult<IEnumerable<ProfileSummaryDto>>> GetProfilesByLocation([FromQuery] string location)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest("Location parameter is required");

            var profiles = await _profileService.GetProfilesByLocationAsync(location);
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profiles by location");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets profiles by tags
    /// </summary>
    /// <param name="tags">Comma-separated list of tags to search for</param>
    /// <param name="matchAll">Whether profile must contain all tags (true) or any tags (false)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of profiles per page</param>
    /// <returns>Paginated list of profiles matching the tags</returns>
    [HttpGet("by-tags")]
    public async Task<ActionResult<PagedResult<ProfileSummaryDto>>> GetProfilesByTags(
        [FromQuery] string tags,
        [FromQuery] bool matchAll = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tags))
                return BadRequest("Tags parameter is required");

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // Parse comma-separated tags
            var tagArray = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(t => t.Trim())
                              .Where(t => !string.IsNullOrWhiteSpace(t))
                              .ToArray();

            if (tagArray.Length == 0)
                return BadRequest("At least one valid tag is required");

            var result = await _profileService.SearchProfilesByTagsAsync(tagArray, matchAll, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profiles by tags");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets popular profiles by view count
    /// </summary>
    /// <param name="count">Number of popular profiles to retrieve</param>
    /// <returns>List of popular profiles</returns>
    [HttpGet("popular")]
    public async Task<ActionResult<IEnumerable<ProfileSummaryDto>>> GetPopularProfiles([FromQuery] int count = 10)
    {
        try
        {
            if (count < 1 || count > 50) count = 10;

            var profiles = await _profileService.GetPopularProfilesAsync(count);
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular profiles");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets the most recent public profiles
    /// </summary>
    /// <param name="count">Number of profiles to retrieve</param>
    /// <returns>List of recent profiles</returns>
    [HttpGet("recent")]
    public async Task<ActionResult<IEnumerable<ProfileSummaryDto>>> GetRecentProfiles([FromQuery] int count = 10)
    {
        try
        {
            if (count < 1 || count > 50) count = 10;

            var profiles = await _profileService.GetRecentProfilesAsync(count);
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent profiles");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets profile statistics (admin only)
    /// </summary>
    /// <returns>Profile statistics</returns>
    [HttpGet("statistics")]
    public async Task<ActionResult<ProfileStatisticsDto>> GetProfileStatistics()
    {
        try
        {
            if (!IsAdministrator())
                return Forbid("Administrator access required");

            var statistics = await _profileService.GetProfileStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile statistics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Increments the view count for a profile
    /// </summary>
    /// <param name="id">Profile ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/increment-view")]
    public async Task<ActionResult> IncrementProfileView(Guid id)
    {
        try
        {
            var result = await _profileService.IncrementProfileViewAsync(id);
            
            if (!result)
                return NotFound("Profile not found or not public");

            return Ok(new { message = "Profile view count incremented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing profile view count");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Alternative endpoint for incrementing profile view count
    /// </summary>
    /// <param name="id">Profile ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/view")]
    public async Task<ActionResult> ViewProfile(Guid id)
    {
        try
        {
            var result = await _profileService.IncrementProfileViewAsync(id);
            
            if (!result)
                return NotFound("Profile not found or not public");

            return Ok(new { message = "Profile view recorded" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording profile view");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets user profile usage statistics
    /// </summary>
    /// <returns>User profile usage statistics</returns>
    [HttpGet("my/usage")]
    public async Task<ActionResult<UserProfileUsageDto>> GetMyProfileUsage()
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var usage = await _profileService.GetUserProfileUsageAsync(keycloakId);
            return Ok(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile usage");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Extract Keycloak ID from JWT token with multiple claim type support
    /// </summary>
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

    /// <summary>
    /// Gets metadata template for a specific profile type
    /// </summary>
    /// <param name="profileTypeId">Profile type identifier</param>
    /// <returns>Metadata template for the profile type</returns>
    [HttpGet("metadata-template/{profileTypeId}")]
    public async Task<ActionResult> GetMetadataTemplate(Guid profileTypeId)
    {
        try
        {
            var template = await _profileService.GetMetadataTemplateAsync(profileTypeId);
            
            if (template == null)
                return NotFound("Profile type not found");

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata template for profile type {ProfileTypeId}", profileTypeId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Uploads an avatar image for a profile
    /// </summary>
    /// <param name="id">Profile identifier</param>
    /// <param name="file">Avatar image file</param>
    /// <returns>File upload result with file ID and URL</returns>
    [HttpPost("{id}/avatar")]
    public async Task<ActionResult> UploadAvatar(Guid id, IFormFile file)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType?.ToLowerInvariant()))
                return BadRequest("Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed");

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("File size exceeds 5MB limit");

            // Upload avatar
            using var fileStream = file.OpenReadStream();
            var fileId = await _profileService.UploadAvatarAsync(id, keycloakId, fileStream, file.FileName, file.ContentType ?? "application/octet-stream");

            if (string.IsNullOrWhiteSpace(fileId))
                return BadRequest("Failed to upload avatar. Please check that you own this profile");

            return Ok(new { FileId = fileId, Message = "Avatar uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar for profile {ProfileId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes the avatar image for a profile
    /// </summary>
    /// <param name="id">Profile identifier</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}/avatar")]
    public async Task<ActionResult> DeleteAvatar(Guid id)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var success = await _profileService.DeleteAvatarAsync(id, keycloakId);

            if (!success)
                return BadRequest("Failed to delete avatar. Please check that you own this profile");

            return Ok(new { Message = "Avatar deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting avatar for profile {ProfileId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Finds profiles near a geographic location using PostGIS
    /// </summary>
    /// <param name="latitude">Center point latitude</param>
    /// <param name="longitude">Center point longitude</param>
    /// <param name="radiusKm">Search radius in kilometers (default: 10km, max: 1000km)</param>
    /// <param name="limit">Maximum number of results (default: 50, max: 500)</param>
    /// <returns>List of nearby profiles with distance information</returns>
    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProfileDto>>> FindNearbyProfiles(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusKm = 10,
        [FromQuery] int limit = 50)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "[ProfilesController.FindNearbyProfiles] START - Lat={Lat}, Lon={Lon}, Radius={Radius}km, Limit={Limit}, RequestId={RequestId}",
            latitude, longitude, radiusKm, limit, requestId);

        try
        {
            // Validate parameters
            if (latitude < -90 || latitude > 90)
            {
                _logger.LogWarning("[ProfilesController.FindNearbyProfiles] Invalid latitude: {Lat}", latitude);
                return BadRequest("Latitude must be between -90 and 90");
            }

            if (longitude < -180 || longitude > 180)
            {
                _logger.LogWarning("[ProfilesController.FindNearbyProfiles] Invalid longitude: {Lon}", longitude);
                return BadRequest("Longitude must be between -180 and 180");
            }

            if (radiusKm <= 0 || radiusKm > 1000)
            {
                _logger.LogWarning("[ProfilesController.FindNearbyProfiles] Invalid radius: {Radius}km", radiusKm);
                return BadRequest("Radius must be between 0 and 1000 km");
            }

            if (limit <= 0 || limit > 500)
            {
                _logger.LogWarning("[ProfilesController.FindNearbyProfiles] Invalid limit: {Limit}", limit);
                return BadRequest("Limit must be between 1 and 500");
            }

            var profiles = await _profileService.FindNearbyProfilesAsync(latitude, longitude, radiusKm, limit);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation(
                "[ProfilesController.FindNearbyProfiles] SUCCESS - Found {Count} profiles, RequestId={RequestId}, Duration={Duration}ms",
                profiles.Count(), requestId, elapsed);

            return Ok(profiles);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex,
                "[ProfilesController.FindNearbyProfiles] ERROR - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
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