using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for user search behavior tracking.
/// Phase 11: Results Ranking & Personalization
/// </summary>
public class UserSearchBehaviorRepository : IUserSearchBehaviorRepository
{
    private readonly SivarDbContext _context;

    public UserSearchBehaviorRepository(SivarDbContext context)
    {
        _context = context;
    }

    public async Task<UserSearchBehavior?> GetByProfileIdAsync(Guid profileId)
    {
        return await _context.UserSearchBehaviors
            .FirstOrDefaultAsync(b => b.ProfileId == profileId && !b.IsDeleted);
    }

    public async Task<UserSearchBehavior> CreateAsync(UserSearchBehavior behavior)
    {
        behavior.CreatedAt = DateTime.UtcNow;
        behavior.UpdatedAt = DateTime.UtcNow;
        _context.UserSearchBehaviors.Add(behavior);
        await _context.SaveChangesAsync();
        return behavior;
    }

    public async Task UpdateAsync(UserSearchBehavior behavior)
    {
        behavior.UpdatedAt = DateTime.UtcNow;
        _context.UserSearchBehaviors.Update(behavior);
        await _context.SaveChangesAsync();
    }

    public async Task<UserSearchBehavior> GetOrCreateAsync(Guid profileId)
    {
        var existing = await GetByProfileIdAsync(profileId);
        if (existing != null) return existing;

        var newBehavior = new UserSearchBehavior
        {
            ProfileId = profileId,
            TotalSearches = 0,
            TotalClicks = 0,
            TotalActions = 0
        };
        
        return await CreateAsync(newBehavior);
    }
}

/// <summary>
/// Repository implementation for ranking configurations.
/// Phase 11: Results Ranking & Personalization
/// </summary>
public class RankingConfigurationRepository : IRankingConfigurationRepository
{
    private readonly SivarDbContext _context;

    public RankingConfigurationRepository(SivarDbContext context)
    {
        _context = context;
    }

    public async Task<RankingConfiguration?> GetByCategoryAsync(string? category)
    {
        if (string.IsNullOrEmpty(category))
            return await GetDefaultAsync();
        
        // Try to find category-specific config
        var categoryConfig = await _context.RankingConfigurations
            .FirstOrDefaultAsync(c => 
                c.Category == category && 
                c.IsActive && 
                !c.IsDeleted &&
                c.AbTestVariant == null);
        
        // Fall back to default if not found
        return categoryConfig ?? await GetDefaultAsync();
    }

    public async Task<RankingConfiguration?> GetDefaultAsync()
    {
        return await _context.RankingConfigurations
            .FirstOrDefaultAsync(c => 
                c.Category == null && 
                c.IsActive && 
                !c.IsDeleted &&
                c.AbTestVariant == null);
    }

    public async Task<List<RankingConfiguration>> GetAllActiveAsync()
    {
        return await _context.RankingConfigurations
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Category)
            .ThenByDescending(c => c.Priority)
            .ToListAsync();
    }

    public async Task<RankingConfiguration?> GetByIdAsync(Guid id)
    {
        return await _context.RankingConfigurations
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
    }

    public async Task<RankingConfiguration> CreateAsync(RankingConfiguration config)
    {
        config.CreatedAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;
        _context.RankingConfigurations.Add(config);
        await _context.SaveChangesAsync();
        return config;
    }

    public async Task UpdateAsync(RankingConfiguration config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        _context.RankingConfigurations.Update(config);
        await _context.SaveChangesAsync();
    }

    public async Task<RankingConfiguration?> GetAbTestVariantAsync(string? category, string variantId)
    {
        return await _context.RankingConfigurations
            .FirstOrDefaultAsync(c => 
                c.Category == category && 
                c.AbTestVariant == variantId &&
                c.IsActive && 
                !c.IsDeleted);
    }
}
