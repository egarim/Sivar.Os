using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for waiting list and phone verification
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WaitingListController : ControllerBase
{
    private readonly IWaitingListService _waitingListService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<WaitingListController> _logger;

    public WaitingListController(
        IWaitingListService waitingListService,
        IUserRepository userRepository,
        ILogger<WaitingListController> logger)
    {
        _waitingListService = waitingListService;
        _userRepository = userRepository;
        _logger = logger;
    }

    #region Public Endpoints

    /// <summary>
    /// Request phone verification OTP
    /// </summary>
    [HttpPost("verify/request")]
    [Authorize]
    public async Task<ActionResult<RequestVerificationResponse>> RequestVerification(
        [FromBody] RequestVerificationRequest request)
    {
        try
        {
            var userId = await GetUserIdFromRequest();
            if (userId == null)
            {
                return Unauthorized("User not found");
            }

            var (success, channel, error) = await _waitingListService.RequestPhoneVerificationAsync(
                userId.Value, request.PhoneNumber, request.CountryCode);

            if (!success)
            {
                return BadRequest(new RequestVerificationResponse(false, null, error));
            }

            return Ok(new RequestVerificationResponse(true, channel.ToString(), null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting verification");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Verify phone OTP and join waiting queue
    /// </summary>
    [HttpPost("verify/confirm")]
    [Authorize]
    public async Task<ActionResult<VerifyConfirmResponse>> ConfirmVerification(
        [FromBody] VerifyConfirmRequest request)
    {
        try
        {
            var userId = await GetUserIdFromRequest();
            if (userId == null)
            {
                return Unauthorized("User not found");
            }

            var result = await _waitingListService.VerifyPhoneAndJoinQueueAsync(
                userId.Value, request.PhoneNumber, request.Code, request.ReferralCode);

            if (!result.Success)
            {
                return BadRequest(new VerifyConfirmResponse(false, 0, null, result.ErrorMessage));
            }

            return Ok(new VerifyConfirmResponse(true, result.Position, result.ReferralCode, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming verification");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get current user's waiting list status
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<WaitingListStatusResponse>> GetStatus()
    {
        try
        {
            var userId = await GetUserIdFromRequest();
            if (userId == null)
            {
                return Unauthorized("User not found");
            }

            var status = await _waitingListService.GetStatusAsync(userId.Value);
            if (status == null)
            {
                return NotFound("Not in waiting list");
            }

            return Ok(new WaitingListStatusResponse(
                status.Status.ToString(),
                status.Position,
                status.TotalWaiting,
                status.ReferralCode,
                status.ReferralCount,
                status.JoinedAt,
                status.ApprovedAt
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Check if a referral code is valid
    /// </summary>
    [HttpGet("referral/validate/{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<ReferralValidationResponse>> ValidateReferralCode(string code)
    {
        try
        {
            var isValid = await _waitingListService.IsReferralCodeValidAsync(code);
            return Ok(new ReferralValidationResponse(isValid));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating referral code");
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Admin Endpoints

    /// <summary>
    /// Get waiting list statistics (admin only)
    /// </summary>
    [HttpGet("admin/stats")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<WaitingListStats>> GetStats()
    {
        try
        {
            var stats = await _waitingListService.GetStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Approve a user (admin only)
    /// </summary>
    [HttpPost("admin/approve/{userId:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> ApproveUser(Guid userId)
    {
        try
        {
            var adminName = User.Identity?.Name ?? "admin";
            var success = await _waitingListService.ApproveUserAsync(userId, adminName);

            if (!success)
            {
                return NotFound("User not found in waiting list");
            }

            return Ok(new { message = "User approved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Approve next N users in queue (admin only)
    /// </summary>
    [HttpPost("admin/approve-next/{count:int}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> ApproveNextInQueue(int count)
    {
        try
        {
            if (count <= 0 || count > 100)
            {
                return BadRequest("Count must be between 1 and 100");
            }

            var adminName = User.Identity?.Name ?? "admin";
            var approved = await _waitingListService.ApproveNextInQueueAsync(count, adminName);

            return Ok(new { approved, requested = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving next {Count} users", count);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Reject a user (admin only)
    /// </summary>
    [HttpPost("admin/reject/{userId:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> RejectUser(Guid userId, [FromBody] RejectRequest? request)
    {
        try
        {
            var success = await _waitingListService.RejectUserAsync(userId, request?.Reason);

            if (!success)
            {
                return NotFound("User not found in waiting list");
            }

            return Ok(new { message = "User rejected" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Helper Methods

    private async Task<Guid?> GetUserIdFromRequest()
    {
        // Check for mock authentication header (for integration tests)
        if (Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakIdHeader.ToString());
            return user?.Id;
        }

        // Check if user is authenticated via JWT Bearer token
        if (User?.Identity?.IsAuthenticated == true)
        {
            var keycloakId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            if (!string.IsNullOrEmpty(keycloakId))
            {
                var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
                return user?.Id;
            }
        }

        return null;
    }

    #endregion
}

#region Request/Response DTOs

public record RequestVerificationRequest(string PhoneNumber, string CountryCode);
public record RequestVerificationResponse(bool Success, string? Channel, string? Error);

public record VerifyConfirmRequest(string PhoneNumber, string Code, string? ReferralCode);
public record VerifyConfirmResponse(bool Success, int Position, string? ReferralCode, string? Error);

public record WaitingListStatusResponse(
    string Status,
    int Position,
    int TotalWaiting,
    string ReferralCode,
    int ReferralCount,
    DateTime JoinedAt,
    DateTime? ApprovedAt
);

public record ReferralValidationResponse(bool IsValid);

public record RejectRequest(string? Reason);

#endregion
