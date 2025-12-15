using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for AdTransaction entity operations
/// </summary>
public interface IAdTransactionRepository : IBaseRepository<AdTransaction>
{
    /// <summary>
    /// Gets transactions for a profile ordered by timestamp
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="limit">Maximum number of transactions to return</param>
    /// <returns>List of transactions, newest first</returns>
    Task<List<AdTransaction>> GetByProfileIdAsync(Guid profileId, int limit = 50);

    /// <summary>
    /// Gets total spend for a profile within a date range
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <returns>Total amount spent</returns>
    Task<decimal> GetTotalSpendAsync(Guid profileId, DateTime from, DateTime to);
}
