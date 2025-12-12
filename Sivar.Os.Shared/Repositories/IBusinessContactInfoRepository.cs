using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for BusinessContactInfo entity operations
/// </summary>
public interface IBusinessContactInfoRepository : IBaseRepository<BusinessContactInfo>
{
    /// <summary>
    /// Gets all contacts for a specific profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="includeInactive">Whether to include inactive contacts</param>
    /// <returns>Collection of business contacts</returns>
    Task<IEnumerable<BusinessContactInfo>> GetByProfileIdAsync(Guid profileId, bool includeInactive = false);

    /// <summary>
    /// Gets all contacts for a profile with contact types loaded
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <returns>Collection of business contacts with types</returns>
    Task<IEnumerable<BusinessContactInfo>> GetByProfileIdWithTypesAsync(Guid profileId);

    /// <summary>
    /// Gets contacts by profile and category
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="category">Category (e.g., "messaging", "phone")</param>
    /// <returns>Collection of contacts in that category</returns>
    Task<IEnumerable<BusinessContactInfo>> GetByProfileAndCategoryAsync(Guid profileId, string category);

    /// <summary>
    /// Gets the primary contact for a profile by contact type
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="contactTypeKey">Contact type key (e.g., "whatsapp")</param>
    /// <returns>Primary contact if found, null otherwise</returns>
    Task<BusinessContactInfo?> GetPrimaryByTypeAsync(Guid profileId, string contactTypeKey);

    /// <summary>
    /// Gets contacts for multiple profiles (batch load for search results)
    /// </summary>
    /// <param name="profileIds">Collection of profile IDs</param>
    /// <returns>Dictionary mapping profile ID to their contacts</returns>
    Task<Dictionary<Guid, List<BusinessContactInfo>>> GetByProfileIdsAsync(IEnumerable<Guid> profileIds);

    /// <summary>
    /// Adds multiple contacts for a profile
    /// </summary>
    /// <param name="contacts">Contacts to add</param>
    Task AddRangeAsync(IEnumerable<BusinessContactInfo> contacts);

    /// <summary>
    /// Removes all contacts for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    Task DeleteByProfileIdAsync(Guid profileId);
}
