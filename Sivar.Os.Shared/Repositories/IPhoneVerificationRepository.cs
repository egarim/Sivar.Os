using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for PhoneVerification operations
/// </summary>
public interface IPhoneVerificationRepository : IBaseRepository<PhoneVerification>
{
    /// <summary>
    /// Gets the latest verification for a user
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <returns>Latest verification if found, null otherwise</returns>
    Task<PhoneVerification?> GetLatestByUserIdAsync(Guid userId);

    /// <summary>
    /// Gets the latest pending verification for a user
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <returns>Latest pending verification if found, null otherwise</returns>
    Task<PhoneVerification?> GetLatestPendingByUserIdAsync(Guid userId);

    /// <summary>
    /// Gets a verification by Twilio SID
    /// </summary>
    /// <param name="twilioSid">Twilio verification SID</param>
    /// <returns>Verification if found, null otherwise</returns>
    Task<PhoneVerification?> GetByTwilioSidAsync(string twilioSid);

    /// <summary>
    /// Gets verifications for a phone number (for rate limiting)
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format</param>
    /// <param name="since">Time window start</param>
    /// <returns>List of verifications in the time window</returns>
    Task<IEnumerable<PhoneVerification>> GetByPhoneNumberSinceAsync(string phoneNumber, DateTime since);

    /// <summary>
    /// Gets verifications from an IP address (for rate limiting)
    /// </summary>
    /// <param name="ipAddress">IP address</param>
    /// <param name="since">Time window start</param>
    /// <returns>List of verifications in the time window</returns>
    Task<IEnumerable<PhoneVerification>> GetByIpAddressSinceAsync(string ipAddress, DateTime since);

    /// <summary>
    /// Checks if a user has a verified phone number
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <returns>True if user has verified phone, false otherwise</returns>
    Task<bool> HasVerifiedPhoneAsync(Guid userId);

    /// <summary>
    /// Gets the verified phone number for a user
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <returns>Verified phone verification record if exists</returns>
    Task<PhoneVerification?> GetVerifiedByUserIdAsync(Guid userId);

    /// <summary>
    /// Expires all pending verifications for a user
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <returns>Number of expired verifications</returns>
    Task<int> ExpirePendingVerificationsAsync(Guid userId);

    /// <summary>
    /// Gets count of verification attempts for a user in time window
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <param name="since">Time window start</param>
    /// <returns>Number of attempts</returns>
    Task<int> GetAttemptCountAsync(Guid userId, DateTime since);
}
