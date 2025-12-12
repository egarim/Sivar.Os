using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for ChatBotSettings entity
/// Phase 0.5: Configurable welcome messages and chat settings
/// </summary>
public class ChatBotSettingsRepository : BaseRepository<ChatBotSettings>, IChatBotSettingsRepository
{
    public ChatBotSettingsRepository(SivarDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<ChatBotSettings?> GetByKeyAsync(string key)
    {
        return await _dbSet
            .Include(s => s.QuickActions.Where(qa => !qa.IsDeleted))
                .ThenInclude(qa => qa.Capability)
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted);
    }

    /// <inheritdoc />
    public async Task<ChatBotSettings?> GetActiveSettingsAsync(string? culture = null, string? regionCode = null)
    {
        var query = _dbSet
            .Include(s => s.QuickActions.Where(qa => !qa.IsDeleted))
                .ThenInclude(qa => qa.Capability)
            .Where(s => s.IsActive && !s.IsDeleted);

        // Try to find the most specific match:
        // 1. Exact culture + region match
        // 2. Culture match (any region)
        // 3. Default (key = "default")

        if (!string.IsNullOrEmpty(culture) && !string.IsNullOrEmpty(regionCode))
        {
            // First try exact culture + region match
            var exactMatch = await query
                .Where(s => s.Culture == culture && s.RegionCode == regionCode)
                .OrderByDescending(s => s.Priority)
                .FirstOrDefaultAsync();

            if (exactMatch != null)
                return exactMatch;
        }

        if (!string.IsNullOrEmpty(culture))
        {
            // Try culture match
            var cultureMatch = await query
                .Where(s => s.Culture == culture && s.RegionCode == null)
                .OrderByDescending(s => s.Priority)
                .FirstOrDefaultAsync();

            if (cultureMatch != null)
                return cultureMatch;
        }

        // Fall back to default
        return await query
            .Where(s => s.Key == "default")
            .OrderByDescending(s => s.Priority)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChatBotSettings>> GetAllActiveAsync()
    {
        return await _dbSet
            .Include(s => s.QuickActions.Where(qa => !qa.IsDeleted))
                .ThenInclude(qa => qa.Capability)
            .Where(s => s.IsActive && !s.IsDeleted)
            .OrderByDescending(s => s.Priority)
            .ThenBy(s => s.Key)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChatBotSettings>> GetByCultureAsync(string culture)
    {
        return await _dbSet
            .Include(s => s.QuickActions.Where(qa => !qa.IsDeleted))
                .ThenInclude(qa => qa.Capability)
            .Where(s => s.Culture == culture && !s.IsDeleted)
            .OrderByDescending(s => s.Priority)
            .ToListAsync();
    }
}
