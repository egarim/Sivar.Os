using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service for managing profile ad budgets
/// </summary>
public interface IProfileAdBudgetService
{
    /// <summary>
    /// Record a sponsored impression (free, just for stats)
    /// </summary>
    /// <param name="profileId">Profile that was shown as sponsored</param>
    /// <param name="searchQuery">The search query that triggered the impression</param>
    /// <param name="position">Position in search results</param>
    Task RecordImpressionAsync(Guid profileId, string searchQuery, int position);

    /// <summary>
    /// Record a sponsored click and deduct budget
    /// </summary>
    /// <param name="profileId">Profile that was clicked</param>
    /// <param name="actualCost">Actual cost to deduct (from second-price auction)</param>
    /// <param name="clickedByUserId">User who clicked (for fraud detection)</param>
    /// <param name="searchQuery">Search query that led to the click</param>
    /// <param name="position">Position in search results</param>
    /// <param name="ipAddress">IP address for fraud detection (will be hashed)</param>
    /// <returns>True if click was recorded and budget deducted</returns>
    Task<bool> RecordClickAsync(
        Guid profileId,
        decimal actualCost,
        Guid? clickedByUserId,
        string searchQuery,
        int position,
        string? ipAddress = null);

    /// <summary>
    /// Add budget to a profile (for top-ups, refunds, bonuses)
    /// </summary>
    /// <param name="profileId">Profile to add budget to</param>
    /// <param name="amount">Amount to add</param>
    /// <param name="transactionType">Type of transaction (TopUp, Bonus, Refund, Adjustment)</param>
    /// <param name="description">Description of the transaction</param>
    /// <param name="addedByUserId">User who added the budget (admin, etc.)</param>
    /// <returns>New balance after addition</returns>
    Task<decimal> AddBudgetAsync(
        Guid profileId,
        decimal amount,
        AdTransactionType transactionType,
        string description,
        Guid? addedByUserId = null);

    /// <summary>
    /// Get current budget for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <returns>Current budget balance</returns>
    Task<decimal> GetBudgetAsync(Guid profileId);

    /// <summary>
    /// Check if profile can receive sponsored clicks (has budget and enabled)
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <returns>True if profile can be shown as sponsored</returns>
    Task<bool> CanShowAsSponsoredAsync(Guid profileId);

    /// <summary>
    /// Reset daily spend counters for all profiles (run daily at midnight)
    /// </summary>
    /// <returns>Number of profiles reset</returns>
    Task<int> ResetDailySpendAsync();

    /// <summary>
    /// Update quality scores based on CTR (run periodically)
    /// </summary>
    /// <returns>Number of profiles updated</returns>
    Task<int> UpdateQualityScoresAsync();

    /// <summary>
    /// Get transaction history for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="limit">Maximum number of transactions to return</param>
    /// <returns>List of transactions, newest first</returns>
    Task<List<AdTransaction>> GetTransactionHistoryAsync(Guid profileId, int limit = 50);
}
