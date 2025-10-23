using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for saved result data access
/// </summary>
public class SavedResultRepository : BaseRepository<SavedResult>, ISavedResultRepository
{
    public SavedResultRepository(SivarDbContext context) : base(context)
    {
    }

    public async Task<List<SavedResult>> GetProfileSavedResultsAsync(Guid profileId, string? resultType = null, int page = 1, int pageSize = 20)
    {
        var query = _context.SavedResults
            .Where(sr => !sr.IsDeleted && sr.ProfileId == profileId);

        if (!string.IsNullOrEmpty(resultType))
        {
            query = query.Where(sr => sr.ResultType == resultType);
        }

        return await query
            .OrderByDescending(sr => sr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<SavedResult>> GetConversationSavedResultsAsync(Guid conversationId)
    {
        return await _context.SavedResults
            .Where(sr => !sr.IsDeleted && sr.ConversationId == conversationId)
            .OrderByDescending(sr => sr.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> ClearProfileSavedResultsAsync(Guid profileId)
    {
        var results = await _context.SavedResults
            .Where(sr => !sr.IsDeleted && sr.ProfileId == profileId)
            .ToListAsync();

        var count = results.Count;
        var now = DateTime.UtcNow;

        foreach (var result in results)
        {
            result.IsDeleted = true;
            result.DeletedAt = now;
            result.UpdatedAt = now;
        }

        if (count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return count;
    }
}
