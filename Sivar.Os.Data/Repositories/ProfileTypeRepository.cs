using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;


namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for ProfileType entity operations (admin functions)
/// </summary>
public class ProfileTypeRepository : BaseRepository<ProfileType>, IProfileTypeRepository
{
    public ProfileTypeRepository(SivarDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets a profile type by its unique name
    /// </summary>
    public async Task<ProfileType?> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(pt => pt.Name.ToLower() == name.ToLower());
    }

    /// <summary>
    /// Gets all active profile types ordered by sort order
    /// </summary>
    public async Task<IEnumerable<ProfileType>> GetActiveProfileTypesAsync()
    {
        return await _dbSet
            .Where(pt => pt.IsActive)
            .OrderBy(pt => pt.SortOrder)
            .ThenBy(pt => pt.DisplayName)
            .ToListAsync();
    }

    /// <summary>
    /// Checks if a profile type name already exists
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var query = _dbSet.Where(pt => pt.Name.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(pt => pt.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// Gets profile types with their associated profiles count
    /// </summary>
    public async Task<IEnumerable<(ProfileType ProfileType, int ProfileCount)>> GetProfileTypesWithUsageAsync()
    {
        return await _dbSet
            .Select(pt => new
            {
                ProfileType = pt,
                ProfileCount = pt.Profiles.Count(p => !p.IsDeleted)
            })
            .OrderBy(x => x.ProfileType.SortOrder)
            .ThenBy(x => x.ProfileType.DisplayName)
            .ToListAsync()
            .ContinueWith(task => task.Result.Select(x => (x.ProfileType, x.ProfileCount)));
    }

    /// <summary>
    /// Gets all profile types including inactive ones (admin view)
    /// </summary>
    public async Task<IEnumerable<ProfileType>> GetAllProfileTypesAsync()
    {
        return await _dbSet
            .IgnoreQueryFilters() // Include soft deleted
            .Where(pt => !pt.IsDeleted) // But exclude hard deleted
            .OrderBy(pt => pt.SortOrder)
            .ThenBy(pt => pt.DisplayName)
            .ToListAsync();
    }

    /// <summary>
    /// Activates a profile type
    /// </summary>
    public async Task<bool> ActivateAsync(Guid id)
    {
        var profileType = await GetByIdAsync(id);
        if (profileType == null)
            return false;

        profileType.IsActive = true;
        profileType.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(profileType);

        return true;
    }

    /// <summary>
    /// Deactivates a profile type
    /// </summary>
    public async Task<bool> DeactivateAsync(Guid id)
    {
        var profileType = await GetByIdAsync(id);
        if (profileType == null)
            return false;

        profileType.IsActive = false;
        profileType.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(profileType);

        return true;
    }

    /// <summary>
    /// Updates the sort order of profile types
    /// </summary>
    public async Task<bool> UpdateSortOrdersAsync(Dictionary<Guid, int> sortOrders)
    {
        if (sortOrders == null || !sortOrders.Any())
            return false;

        var profileTypeIds = sortOrders.Keys.ToList();
        var profileTypes = await _dbSet
            .Where(pt => profileTypeIds.Contains(pt.Id))
            .ToListAsync();

        foreach (var profileType in profileTypes)
        {
            if (sortOrders.TryGetValue(profileType.Id, out int sortOrder))
            {
                profileType.SortOrder = sortOrder;
                profileType.UpdatedAt = DateTime.UtcNow;
            }
        }

        _dbSet.UpdateRange(profileTypes);
        return true;
    }

    /// <summary>
    /// Gets profile type by ID with profiles included
    /// </summary>
    public async Task<ProfileType?> GetWithProfilesAsync(Guid id)
    {
        return await _dbSet
            .Include(pt => pt.Profiles.Where(p => !p.IsDeleted))
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(pt => pt.Id == id);
    }

    /// <summary>
    /// Gets the next available sort order for new profile types
    /// </summary>
    public async Task<int> GetNextSortOrderAsync()
    {
        var maxSortOrder = await _dbSet
            .MaxAsync(pt => (int?)pt.SortOrder) ?? 0;

        return maxSortOrder + 10; // Leave gaps for reordering
    }

    /// <summary>
    /// Searches profile types by name or display name
    /// </summary>
    public async Task<IEnumerable<ProfileType>> SearchProfileTypesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetActiveProfileTypesAsync();

        var term = searchTerm.ToLower();

        return await _dbSet
            .Where(pt => pt.IsActive && (
                pt.Name.ToLower().Contains(term) ||
                pt.DisplayName.ToLower().Contains(term) ||
                pt.Description.ToLower().Contains(term)
            ))
            .OrderBy(pt => pt.SortOrder)
            .ThenBy(pt => pt.DisplayName)
            .ToListAsync();
    }
}