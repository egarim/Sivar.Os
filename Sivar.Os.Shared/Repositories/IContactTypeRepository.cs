using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for ContactType entity operations
/// </summary>
public interface IContactTypeRepository : IBaseRepository<ContactType>
{
    /// <summary>
    /// Gets a contact type by its unique key
    /// </summary>
    /// <param name="key">Contact type key (e.g., "whatsapp", "phone")</param>
    /// <returns>Contact type if found, null otherwise</returns>
    Task<ContactType?> GetByKeyAsync(string key);

    /// <summary>
    /// Gets all active contact types
    /// </summary>
    /// <returns>Collection of active contact types</returns>
    Task<IEnumerable<ContactType>> GetAllActiveAsync();

    /// <summary>
    /// Gets contact types by category
    /// </summary>
    /// <param name="category">Category (e.g., "messaging", "social", "phone")</param>
    /// <returns>Collection of contact types in that category</returns>
    Task<IEnumerable<ContactType>> GetByCategoryAsync(string category);

    /// <summary>
    /// Gets contact types sorted by regional popularity
    /// </summary>
    /// <param name="regionCode">ISO country code (e.g., "SV", "US", "RU")</param>
    /// <returns>Contact types sorted by popularity in that region</returns>
    Task<IEnumerable<ContactType>> GetSortedByRegionalPopularityAsync(string regionCode);

    /// <summary>
    /// Checks if a key already exists
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if key exists, false otherwise</returns>
    Task<bool> KeyExistsAsync(string key);
}
