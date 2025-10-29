using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Clients;
using Swashbuckle.AspNetCore.Annotations;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for activity stream management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ActivitiesController : ControllerBase
{
    private readonly IActivitiesClient _activitiesClient;
    private readonly ILogger<ActivitiesController> _logger;

    public ActivitiesController(
        IActivitiesClient activitiesClient,
        ILogger<ActivitiesController> logger)
    {
        _activitiesClient = activitiesClient ?? throw new ArgumentNullException(nameof(activitiesClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the activity feed for the authenticated user
    /// </summary>
    /// <remarks>
    /// Returns a paginated feed of activities from profiles the user follows.
    /// Activities include posts, comments, likes, follows, shares, and more.
    /// 
    /// Sample request:
    ///
    ///     GET /api/activities/feed?page=0&amp;pageSize=20
    ///
    /// </remarks>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of activities per page (max 100)</param>
    /// <returns>Paginated list of activities</returns>
    /// <response code="200">Activity feed retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("feed")]
    [SwaggerOperation(
        Summary = "Get activity feed",
        Description = "Returns a paginated feed of activities from profiles the user follows",
        Tags = new[] { "Activities" }
    )]
    [SwaggerResponse(200, "Activity feed retrieved successfully", typeof(ActivityFeedDto))]
    [SwaggerResponse(401, "User not authenticated")]
    public async Task<ActionResult<ActivityFeedDto>> GetFeed(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("[ActivitiesController.GetFeed] API ENDPOINT CALLED - page={Page}, pageSize={PageSize}", page, pageSize);

            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ActivitiesController.GetFeed] Keycloak ID extracted: {KeycloakId}", keycloakId ?? "NULL");

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ActivitiesController.GetFeed] ❌ User not authenticated");
                return Unauthorized(new { error = "User not authenticated" });
            }

            if (pageSize > 100)
                pageSize = 100; // Limit page size

            _logger.LogInformation("[ActivitiesController.GetFeed] Calling ActivitiesClient.GetFeedActivitiesAsync with pageSize={PageSize}, pageNumber={Page}",
                pageSize, page);

            var feed = await _activitiesClient.GetFeedActivitiesAsync(pageSize, page);

            if (feed == null)
            {
                _logger.LogWarning("[ActivitiesController.GetFeed] ActivitiesClient returned null");
                return Ok(new ActivityFeedDto
                {
                    Activities = new List<ActivityDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0
                });
            }

            _logger.LogInformation("[ActivitiesController.GetFeed] Returning feed DTO with {Count} activities", feed.Activities?.Count ?? 0);
            return Ok(feed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ActivitiesController.GetFeed] Error getting activity feed");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets activities by a specific profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of activities per page (max 100)</param>
    /// <returns>Paginated list of activities by the profile</returns>
    /// <response code="200">Activities retrieved successfully</response>
    /// <response code="404">Profile not found</response>
    [HttpGet("profile/{profileId}")]
    [SwaggerOperation(
        Summary = "Get activities by profile",
        Description = "Returns a paginated list of activities performed by a specific profile",
        Tags = new[] { "Activities" }
    )]
    [SwaggerResponse(200, "Activities retrieved successfully", typeof(ActivityFeedDto))]
    [SwaggerResponse(404, "Profile not found")]
    public async Task<ActionResult<ActivityFeedDto>> GetProfileActivities(
        Guid profileId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("[ActivitiesController.GetProfileActivities] Getting activities for profile {ProfileId}, page={Page}, pageSize={PageSize}",
                profileId, page, pageSize);

            if (pageSize > 100)
                pageSize = 100; // Limit page size

            var feed = await _activitiesClient.GetProfileActivitiesAsync(profileId, pageSize, page);

            if (feed == null)
            {
                _logger.LogWarning("[ActivitiesController.GetProfileActivities] ActivitiesClient returned null");
                return Ok(new ActivityFeedDto
                {
                    Activities = new List<ActivityDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0
                });
            }

            _logger.LogInformation("[ActivitiesController.GetProfileActivities] Returning {Count} activities", feed.Activities?.Count ?? 0);
            return Ok(feed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ActivitiesController.GetProfileActivities] Error getting activities for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets activities related to a specific object (post, comment, etc.)
    /// </summary>
    /// <param name="objectType">Type of object (Post, Comment, Profile, etc.)</param>
    /// <param name="objectId">Object ID</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of activities per page (max 100)</param>
    /// <returns>Paginated list of activities related to the object</returns>
    /// <response code="200">Activities retrieved successfully</response>
    [HttpGet("object/{objectType}/{objectId}")]
    [SwaggerOperation(
        Summary = "Get activities by object",
        Description = "Returns activities related to a specific object (e.g., all likes/comments on a post)",
        Tags = new[] { "Activities" }
    )]
    [SwaggerResponse(200, "Activities retrieved successfully", typeof(ActivityFeedDto))]
    public async Task<ActionResult<ActivityFeedDto>> GetObjectActivities(
        string objectType,
        Guid objectId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("[ActivitiesController.GetObjectActivities] Getting activities for {ObjectType} {ObjectId}, page={Page}, pageSize={PageSize}",
                objectType, objectId, page, pageSize);

            if (pageSize > 100)
                pageSize = 100; // Limit page size

            var feed = await _activitiesClient.GetObjectActivitiesAsync(objectType, objectId, pageSize, page);

            if (feed == null)
            {
                _logger.LogWarning("[ActivitiesController.GetObjectActivities] ActivitiesClient returned null");
                return Ok(new ActivityFeedDto
                {
                    Activities = new List<ActivityDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0
                });
            }

            _logger.LogInformation("[ActivitiesController.GetObjectActivities] Returning {Count} activities", feed.Activities?.Count ?? 0);
            return Ok(feed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ActivitiesController.GetObjectActivities] Error getting activities for {ObjectType} {ObjectId}",
                objectType, objectId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets trending activities
    /// </summary>
    /// <param name="hours">Hours to look back for trending calculation (max 168)</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="pageSize">Number of activities per page (max 100)</param>
    /// <returns>Trending activities sorted by engagement</returns>
    /// <response code="200">Trending activities retrieved successfully</response>
    [HttpGet("trending")]
    [SwaggerOperation(
        Summary = "Get trending activities",
        Description = "Returns activities sorted by engagement score within the specified time window",
        Tags = new[] { "Activities" }
    )]
    [SwaggerResponse(200, "Trending activities retrieved successfully", typeof(ActivityFeedDto))]
    public async Task<ActionResult<ActivityFeedDto>> GetTrending(
        [FromQuery] int hours = 24,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("[ActivitiesController.GetTrending] Getting trending activities, hours={Hours}, page={Page}, pageSize={PageSize}",
                hours, page, pageSize);

            if (pageSize > 100)
                pageSize = 100; // Limit page size

            if (hours > 168) // 1 week max
                hours = 168;

            var feed = await _activitiesClient.GetTrendingActivitiesAsync(hours, pageSize, page);

            if (feed == null)
            {
                _logger.LogWarning("[ActivitiesController.GetTrending] ActivitiesClient returned null");
                return Ok(new ActivityFeedDto
                {
                    Activities = new List<ActivityDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0
                });
            }

            _logger.LogInformation("[ActivitiesController.GetTrending] Returning {Count} trending activities", feed.Activities?.Count ?? 0);
            return Ok(feed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ActivitiesController.GetTrending] Error getting trending activities");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Helper method to extract Keycloak ID from request
    /// </summary>
    private string GetKeycloakIdFromRequest()
    {
        _logger.LogInformation("[GetKeycloakIdFromRequest] Starting Keycloak ID extraction...");

        // Check for mock authentication header (for integration tests)
        if (Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            _logger.LogInformation($"[GetKeycloakIdFromRequest] ✓ Found X-Keycloak-Id header: {keycloakIdHeader}");
            return keycloakIdHeader.ToString();
        }

        _logger.LogInformation("[GetKeycloakIdFromRequest] No X-Keycloak-Id header found");

        // Check if user is authenticated via claims
        _logger.LogInformation($"[GetKeycloakIdFromRequest] User.Identity?.IsAuthenticated = {User?.Identity?.IsAuthenticated}");

        if (User?.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation($"[GetKeycloakIdFromRequest] User is authenticated. Total claims: {User.Claims.Count()}");

            // Try "sub" claim first (OpenID Connect standard)
            var subClaim = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(subClaim))
            {
                _logger.LogInformation($"[GetKeycloakIdFromRequest] ✓ Found 'sub' claim: {subClaim}");
                return subClaim;
            }

            // Fallback: try to find "user_id" or "id" claims
            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogInformation($"[GetKeycloakIdFromRequest] ✓ Found 'user_id' claim: {userIdClaim}");
                return userIdClaim;
            }

            var idClaim = User.FindFirst("id")?.Value;
            if (!string.IsNullOrEmpty(idClaim))
            {
                _logger.LogInformation($"[GetKeycloakIdFromRequest] ✓ Found 'id' claim: {idClaim}");
                return idClaim;
            }

            var nameIdentifierClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(nameIdentifierClaim))
            {
                _logger.LogInformation($"[GetKeycloakIdFromRequest] ✓ Found NameIdentifier claim: {nameIdentifierClaim}");
                return nameIdentifierClaim;
            }
        }
        else
        {
            _logger.LogWarning("[GetKeycloakIdFromRequest] User is NOT authenticated!");
        }

        // Only return fallback if we have mock auth header (X-Mock-Auth)
        if (Request.Headers.ContainsKey("X-Mock-Auth"))
        {
            _logger.LogInformation("[GetKeycloakIdFromRequest] ✓ Using mock auth header");
            return "mock-keycloak-user-id";
        }

        // No authentication found
        _logger.LogError("[GetKeycloakIdFromRequest] ❌ NO KEYCLOAK ID FOUND - returning null!");
        return null!;
    }
}
