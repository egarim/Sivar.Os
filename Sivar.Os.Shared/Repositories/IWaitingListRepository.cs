using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for WaitingListEntry operations
/// </summary>
public interface IWaitingListRepository : IBaseRepository<WaitingListEntry>
{
    /// <summary>
    /// Gets a waiting list entry by user ID
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <returns>Entry if found, null otherwise</returns>
    Task<WaitingListEntry?> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Gets a waiting list entry by email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <returns>Entry if found, null otherwise</returns>
    Task<WaitingListEntry?> GetByEmailAsync(string email);

    /// <summary>
    /// Gets a waiting list entry by phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format</param>
    /// <returns>Entry if found, null otherwise</returns>
    Task<WaitingListEntry?> GetByPhoneNumberAsync(string phoneNumber);

    /// <summary>
    /// Gets a waiting list entry by referral code
    /// </summary>
    /// <param name="referralCode">The referral code</param>
    /// <returns>Entry if found, null otherwise</returns>
    Task<WaitingListEntry?> GetByReferralCodeAsync(string referralCode);

    /// <summary>
    /// Gets entries by status with pagination
    /// </summary>
    /// <param name="status">Status to filter by</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated entries and total count</returns>
    Task<(IEnumerable<WaitingListEntry> Entries, int TotalCount)> GetByStatusAsync(
        WaitingListStatus status,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets all entries in the queue (Waiting status) ordered by position
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated queue entries and total count</returns>
    Task<(IEnumerable<WaitingListEntry> Entries, int TotalCount)> GetQueueAsync(
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Gets entries referred by a specific user
    /// </summary>
    /// <param name="referrerUserId">The referrer's user ID</param>
    /// <returns>List of referred entries</returns>
    Task<IEnumerable<WaitingListEntry>> GetReferralsAsync(Guid referrerUserId);

    /// <summary>
    /// Gets the next N entries in the queue ready for approval
    /// </summary>
    /// <param name="count">Number of entries to get</param>
    /// <returns>Top entries from the queue</returns>
    Task<IEnumerable<WaitingListEntry>> GetNextInQueueAsync(int count);

    /// <summary>
    /// Gets the current maximum position in the queue
    /// </summary>
    /// <returns>Maximum position number, or 0 if queue is empty</returns>
    Task<int> GetMaxPositionAsync();

    /// <summary>
    /// Gets queue statistics
    /// </summary>
    /// <returns>Tuple with counts for each status</returns>
    Task<(int PendingVerification, int Waiting, int Approved, int Rejected)> GetStatsAsync();

    /// <summary>
    /// Checks if an email already exists in the waiting list
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Checks if a phone number already exists in the waiting list
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> PhoneNumberExistsAsync(string phoneNumber);

    /// <summary>
    /// Increments the referral count for a user
    /// </summary>
    /// <param name="referrerUserId">User ID of the referrer</param>
    /// <returns>True if updated, false if not found</returns>
    Task<bool> IncrementReferralCountAsync(Guid referrerUserId);

    /// <summary>
    /// Updates positions after a user is approved (decrements positions for users after them)
    /// </summary>
    /// <param name="removedPosition">The position that was removed from queue</param>
    /// <returns>Number of entries updated</returns>
    Task<int> ReorderQueueAfterApprovalAsync(int removedPosition);
}
