using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for ContactType entity
/// </summary>
public class ContactTypeRepository : BaseRepository<ContactType>, IContactTypeRepository
{
    public ContactTypeRepository(SivarDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<ContactType?> GetByKeyAsync(string key)
    {
        return await _dbSet
            .FirstOrDefaultAsync(ct => ct.Key == key.ToLowerInvariant() && ct.IsActive);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ContactType>> GetAllActiveAsync()
    {
        return await _dbSet
            .Where(ct => ct.IsActive)
            .OrderBy(ct => ct.Category)
            .ThenBy(ct => ct.SortOrder)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ContactType>> GetByCategoryAsync(string category)
    {
        return await _dbSet
            .Where(ct => ct.Category == category.ToLowerInvariant() && ct.IsActive)
            .OrderBy(ct => ct.SortOrder)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ContactType>> GetSortedByRegionalPopularityAsync(string regionCode)
    {
        var contactTypes = await GetAllActiveAsync();
        
        // Sort by regional popularity (higher = first), then by sort order
        return contactTypes
            .OrderBy(ct => GetCategorySortOrder(ct.Category))
            .ThenByDescending(ct => ct.GetRegionalPopularity(regionCode))
            .ThenBy(ct => ct.SortOrder)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<bool> KeyExistsAsync(string key)
    {
        return await _dbSet.AnyAsync(ct => ct.Key == key.ToLowerInvariant());
    }

    /// <summary>
    /// Get sort order for contact categories
    /// </summary>
    private static int GetCategorySortOrder(string category) => category.ToLowerInvariant() switch
    {
        "phone" => 1,
        "messaging" => 2,
        "email" => 3,
        "web" => 4,
        "social" => 5,
        "location" => 6,
        "delivery" => 7,
        _ => 99
    };
}
