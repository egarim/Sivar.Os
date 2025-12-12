using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for BusinessContactInfo entity
/// </summary>
public class BusinessContactInfoRepository : BaseRepository<BusinessContactInfo>, IBusinessContactInfoRepository
{
    public BusinessContactInfoRepository(SivarDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BusinessContactInfo>> GetByProfileIdAsync(Guid profileId, bool includeInactive = false)
    {
        var query = _dbSet.Where(bc => bc.ProfileId == profileId);
        
        if (!includeInactive)
            query = query.Where(bc => bc.IsActive);

        return await query
            .OrderBy(bc => bc.SortOrder)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BusinessContactInfo>> GetByProfileIdWithTypesAsync(Guid profileId)
    {
        return await _dbSet
            .Include(bc => bc.ContactType)
            .Where(bc => bc.ProfileId == profileId && bc.IsActive)
            .OrderBy(bc => bc.SortOrder)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BusinessContactInfo>> GetByProfileAndCategoryAsync(Guid profileId, string category)
    {
        return await _dbSet
            .Include(bc => bc.ContactType)
            .Where(bc => bc.ProfileId == profileId 
                      && bc.IsActive 
                      && bc.ContactType.Category == category.ToLowerInvariant())
            .OrderBy(bc => bc.SortOrder)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<BusinessContactInfo?> GetPrimaryByTypeAsync(Guid profileId, string contactTypeKey)
    {
        return await _dbSet
            .Include(bc => bc.ContactType)
            .Where(bc => bc.ProfileId == profileId 
                      && bc.IsActive 
                      && bc.ContactType.Key == contactTypeKey.ToLowerInvariant()
                      && bc.IsPrimary)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, List<BusinessContactInfo>>> GetByProfileIdsAsync(IEnumerable<Guid> profileIds)
    {
        var profileIdList = profileIds.ToList();
        
        var contacts = await _dbSet
            .Include(bc => bc.ContactType)
            .Where(bc => profileIdList.Contains(bc.ProfileId) && bc.IsActive)
            .OrderBy(bc => bc.SortOrder)
            .ToListAsync();

        return contacts
            .GroupBy(bc => bc.ProfileId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<BusinessContactInfo> contacts)
    {
        await _dbSet.AddRangeAsync(contacts);
    }

    /// <inheritdoc />
    public async Task DeleteByProfileIdAsync(Guid profileId)
    {
        var contacts = await _dbSet.Where(bc => bc.ProfileId == profileId).ToListAsync();
        _dbSet.RemoveRange(contacts);
    }
}
