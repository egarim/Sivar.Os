using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository for user search behavior tracking.
/// Phase 11: Results Ranking & Personalization
/// </summary>
public interface IUserSearchBehaviorRepository
{
    /// <summary>
    /// Gets user search behavior for a profile
    /// </summary>
    Task<UserSearchBehavior?> GetByProfileIdAsync(Guid profileId);
    
    /// <summary>
    /// Creates a new user search behavior record
    /// </summary>
    Task<UserSearchBehavior> CreateAsync(UserSearchBehavior behavior);
    
    /// <summary>
    /// Updates an existing user search behavior record
    /// </summary>
    Task UpdateAsync(UserSearchBehavior behavior);
    
    /// <summary>
    /// Gets or creates user search behavior for a profile
    /// </summary>
    Task<UserSearchBehavior> GetOrCreateAsync(Guid profileId);
}

/// <summary>
/// Repository for ranking configurations.
/// Phase 11: Results Ranking & Personalization
/// </summary>
public interface IRankingConfigurationRepository
{
    /// <summary>
    /// Gets active ranking configuration for a category (or global default)
    /// </summary>
    Task<RankingConfiguration?> GetByCategoryAsync(string? category);
    
    /// <summary>
    /// Gets the global default ranking configuration
    /// </summary>
    Task<RankingConfiguration?> GetDefaultAsync();
    
    /// <summary>
    /// Gets all active ranking configurations
    /// </summary>
    Task<List<RankingConfiguration>> GetAllActiveAsync();
    
    /// <summary>
    /// Gets a ranking configuration by ID
    /// </summary>
    Task<RankingConfiguration?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Creates a new ranking configuration
    /// </summary>
    Task<RankingConfiguration> CreateAsync(RankingConfiguration config);
    
    /// <summary>
    /// Updates an existing ranking configuration
    /// </summary>
    Task UpdateAsync(RankingConfiguration config);
    
    /// <summary>
    /// Gets A/B test variant configuration (if applicable)
    /// </summary>
    Task<RankingConfiguration?> GetAbTestVariantAsync(string? category, string variantId);
}
