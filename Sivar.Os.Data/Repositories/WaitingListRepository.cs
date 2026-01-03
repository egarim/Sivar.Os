using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for WaitingListEntry operations
/// </summary>
public class WaitingListRepository : BaseRepository<WaitingListEntry>, IWaitingListRepository
{
    public WaitingListRepository(SivarDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<WaitingListEntry?> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(e => e.User)
            .Include(e => e.ReferredBy)
            .FirstOrDefaultAsync(e => e.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<WaitingListEntry?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Email.ToLower() == email.ToLower());
    }

    /// <inheritdoc />
    public async Task<WaitingListEntry?> GetByPhoneNumberAsync(string phoneNumber)
    {
        return await _dbSet
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.PhoneNumber == phoneNumber);
    }

    /// <inheritdoc />
    public async Task<WaitingListEntry?> GetByReferralCodeAsync(string referralCode)
    {
        return await _dbSet
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.ReferralCode == referralCode);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<WaitingListEntry> Entries, int TotalCount)> GetByStatusAsync(
        WaitingListStatus status,
        int page = 1,
        int pageSize = 20)
    {
        var query = _dbSet
            .Include(e => e.User)
            .Where(e => e.Status == status)
            .OrderBy(e => e.Position)
            .ThenBy(e => e.JoinedAt);

        var totalCount = await query.CountAsync();
        var entries = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (entries, totalCount);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<WaitingListEntry> Entries, int TotalCount)> GetQueueAsync(
        int page = 1,
        int pageSize = 20)
    {
        var query = _dbSet
            .Include(e => e.User)
            .Where(e => e.Status == WaitingListStatus.Waiting)
            .OrderBy(e => e.Position);

        var totalCount = await query.CountAsync();
        var entries = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (entries, totalCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WaitingListEntry>> GetReferralsAsync(Guid referrerUserId)
    {
        return await _dbSet
            .Include(e => e.User)
            .Where(e => e.ReferredByUserId == referrerUserId)
            .OrderByDescending(e => e.JoinedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WaitingListEntry>> GetNextInQueueAsync(int count)
    {
        return await _dbSet
            .Include(e => e.User)
            .Where(e => e.Status == WaitingListStatus.Waiting)
            .OrderBy(e => e.Position)
            .Take(count)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetMaxPositionAsync()
    {
        var hasEntries = await _dbSet
            .Where(e => e.Status == WaitingListStatus.Waiting)
            .AnyAsync();

        if (!hasEntries)
            return 0;

        return await _dbSet
            .Where(e => e.Status == WaitingListStatus.Waiting)
            .MaxAsync(e => e.Position);
    }

    /// <inheritdoc />
    public async Task<(int PendingVerification, int Waiting, int Approved, int Rejected)> GetStatsAsync()
    {
        var stats = await _dbSet
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return (
            PendingVerification: stats.FirstOrDefault(s => s.Status == WaitingListStatus.PendingVerification)?.Count ?? 0,
            Waiting: stats.FirstOrDefault(s => s.Status == WaitingListStatus.Waiting)?.Count ?? 0,
            Approved: stats.FirstOrDefault(s => s.Status == WaitingListStatus.Approved)?.Count ?? 0,
            Rejected: stats.FirstOrDefault(s => s.Status == WaitingListStatus.Rejected)?.Count ?? 0
        );
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(e => e.Email.ToLower() == email.ToLower());
    }

    /// <inheritdoc />
    public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
    {
        return await _dbSet.AnyAsync(e => e.PhoneNumber == phoneNumber);
    }

    /// <inheritdoc />
    public async Task<bool> IncrementReferralCountAsync(Guid referrerUserId)
    {
        var entry = await _dbSet.FirstOrDefaultAsync(e => e.UserId == referrerUserId);
        if (entry == null)
            return false;

        entry.ReferralCount++;
        return true;
    }

    /// <inheritdoc />
    public async Task<int> ReorderQueueAfterApprovalAsync(int removedPosition)
    {
        var entriesToUpdate = await _dbSet
            .Where(e => e.Status == WaitingListStatus.Waiting && e.Position > removedPosition)
            .ToListAsync();

        foreach (var entry in entriesToUpdate)
        {
            entry.Position--;
        }

        return entriesToUpdate.Count;
    }
}
