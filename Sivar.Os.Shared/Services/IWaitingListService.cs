using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Result of joining the waiting list
/// </summary>
public record JoinWaitingListResult(
    bool Success,
    int Position,
    string ReferralCode,
    string? ErrorMessage = null
);

/// <summary>
/// Result of phone verification in the waiting list context
/// </summary>
public record VerifyPhoneResult(
    bool Success,
    int Position,
    string ReferralCode,
    string? ErrorMessage = null
);

/// <summary>
/// Waiting list status information
/// </summary>
public record WaitingListStatusDto(
    WaitingListStatus Status,
    int Position,
    int TotalWaiting,
    string ReferralCode,
    int ReferralCount,
    DateTime JoinedAt,
    DateTime? ApprovedAt
);

/// <summary>
/// Service for managing the waiting list and phone verification flow
/// </summary>
public interface IWaitingListService
{
    /// <summary>
    /// Create a pending waiting list entry for a new user
    /// Called after Keycloak registration, before phone verification
    /// </summary>
    /// <param name="userId">The local User ID</param>
    /// <param name="email">User's email</param>
    /// <param name="keycloakId">Keycloak user ID</param>
    /// <returns>The created entry</returns>
    Task<WaitingListEntry> CreatePendingEntryAsync(Guid userId, string email, string keycloakId);

    /// <summary>
    /// Request phone verification OTP
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="phoneNumber">Phone in E.164 format</param>
    /// <param name="countryCode">ISO country code</param>
    /// <returns>Result with channel used (SMS/WhatsApp)</returns>
    Task<(bool Success, VerificationChannel Channel, string? Error)> RequestPhoneVerificationAsync(
        Guid userId, 
        string phoneNumber, 
        string countryCode);

    /// <summary>
    /// Verify phone OTP and add user to waiting queue
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="phoneNumber">Phone number</param>
    /// <param name="code">OTP code</param>
    /// <param name="referralCode">Optional referral code used</param>
    /// <returns>Verification result with queue position</returns>
    Task<VerifyPhoneResult> VerifyPhoneAndJoinQueueAsync(
        Guid userId, 
        string phoneNumber, 
        string code,
        string? referralCode = null);

    /// <summary>
    /// Get user's waiting list status
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Status information or null if not in list</returns>
    Task<WaitingListStatusDto?> GetStatusAsync(Guid userId);

    /// <summary>
    /// Get waiting list entry by user ID
    /// </summary>
    Task<WaitingListEntry?> GetEntryByUserIdAsync(Guid userId);

    /// <summary>
    /// Approve a user (admin action)
    /// </summary>
    /// <param name="userId">User ID to approve</param>
    /// <param name="approvedBy">Admin username</param>
    /// <returns>Success status</returns>
    Task<bool> ApproveUserAsync(Guid userId, string approvedBy);

    /// <summary>
    /// Approve multiple users in batch (admin action)
    /// </summary>
    /// <param name="userIds">List of user IDs</param>
    /// <param name="approvedBy">Admin username</param>
    /// <returns>Number of users approved</returns>
    Task<int> ApproveUsersAsync(IEnumerable<Guid> userIds, string approvedBy);

    /// <summary>
    /// Approve next N users in queue (admin action)
    /// </summary>
    /// <param name="count">Number to approve</param>
    /// <param name="approvedBy">Admin username</param>
    /// <returns>Number of users approved</returns>
    Task<int> ApproveNextInQueueAsync(int count, string approvedBy);

    /// <summary>
    /// Reject a user (admin action)
    /// </summary>
    /// <param name="userId">User ID to reject</param>
    /// <param name="reason">Rejection reason</param>
    /// <returns>Success status</returns>
    Task<bool> RejectUserAsync(Guid userId, string? reason = null);

    /// <summary>
    /// Check if a referral code is valid
    /// </summary>
    /// <param name="referralCode">The code to check</param>
    /// <returns>True if valid and can be used</returns>
    Task<bool> IsReferralCodeValidAsync(string referralCode);

    /// <summary>
    /// Get statistics for admin dashboard
    /// </summary>
    Task<WaitingListStats> GetStatsAsync();
}

/// <summary>
/// Waiting list statistics for admin dashboard
/// </summary>
public record WaitingListStats(
    int TotalSignups,
    int PendingVerification,
    int WaitingApproval,
    int ApprovedTotal,
    int ApprovedToday,
    int ApprovedThisWeek,
    int RejectedTotal,
    Dictionary<string, int> SignupsByCountry
);
