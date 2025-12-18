using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for AI chat token usage tracking and auditing
/// </summary>
public class ChatTokenUsageRepository : BaseRepository<ChatTokenUsage>, IChatTokenUsageRepository
{
    public ChatTokenUsageRepository(SivarDbContext context) : base(context)
    {
    }

    public async Task<List<ChatTokenUsage>> GetProfileTokenUsageAsync(
        Guid profileId, 
        DateTime? from = null, 
        DateTime? to = null, 
        int page = 1, 
        int pageSize = 50)
    {
        var query = _context.ChatTokenUsages
            .Where(t => !t.IsDeleted && t.ProfileId == profileId);

        if (from.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= to.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<ChatTokenUsage>> GetConversationTokenUsageAsync(Guid conversationId)
    {
        return await _context.ChatTokenUsages
            .Where(t => !t.IsDeleted && t.ConversationId == conversationId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<long> GetTotalTokensUsedAsync(Guid profileId, DateTime from, DateTime to)
    {
        return await _context.ChatTokenUsages
            .Where(t => !t.IsDeleted && 
                        t.ProfileId == profileId && 
                        t.CreatedAt >= from && 
                        t.CreatedAt <= to)
            .SumAsync(t => (long)t.TotalTokens);
    }

    public async Task<(long InputTokens, long OutputTokens, long TotalTokens)> GetTokenUsageSummaryAsync(
        Guid profileId, 
        DateTime? from = null, 
        DateTime? to = null)
    {
        var query = _context.ChatTokenUsages
            .Where(t => !t.IsDeleted && t.ProfileId == profileId);

        if (from.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= to.Value);
        }

        var summary = await query
            .GroupBy(t => 1)
            .Select(g => new
            {
                InputTokens = g.Sum(t => (long)t.InputTokens),
                OutputTokens = g.Sum(t => (long)t.OutputTokens),
                TotalTokens = g.Sum(t => (long)t.TotalTokens)
            })
            .FirstOrDefaultAsync();

        return summary != null 
            ? (summary.InputTokens, summary.OutputTokens, summary.TotalTokens) 
            : (0, 0, 0);
    }
}
