using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for AdTransaction entity operations
/// </summary>
public class AdTransactionRepository : BaseRepository<AdTransaction>, IAdTransactionRepository
{
    public AdTransactionRepository(SivarDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<List<AdTransaction>> GetByProfileIdAsync(Guid profileId, int limit = 50)
    {
        return await _dbSet
            .Where(t => t.ProfileId == profileId)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalSpendAsync(Guid profileId, DateTime from, DateTime to)
    {
        return await _dbSet
            .Where(t => t.ProfileId == profileId)
            .Where(t => t.TransactionType == AdTransactionType.Click)
            .Where(t => t.Timestamp >= from && t.Timestamp <= to)
            .SumAsync(t => Math.Abs(t.Amount)); // Clicks are negative, so take absolute value
    }
}
