
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for ProfileType entity operations (admin functions)
/// </summary>
public interface IProfileTypeRepository : IBaseRepository<ProfileType>
{
    /// <summary>
    /// Gets a profile type by its unique name
    /// </summary>
    /// <param name="name">Profile type name (e.g., "PersonalProfile")</param>
    /// <returns>ProfileType if found, null otherwise</returns>
    Task<ProfileType?> GetByNameAsync(string name);

    /// <summary>
    /// Gets all active profile types ordered by sort order
    /// </summary>
    /// <returns>Collection of active profile types</returns>
    Task<IEnumerable<ProfileType>> GetActiveProfileTypesAsync();

    /// <summary>
    /// Checks if a profile type name already exists
    /// </summary>
    /// <param name="name">Profile type name to check</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates)</param>
    /// <returns>True if name exists, false otherwise</returns>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null);

    /// <summary>
    /// Gets profile types with their associated profiles count
    /// </summary>
    /// <returns>Collection of profile types with usage statistics</returns>
    Task<IEnumerable<(ProfileType ProfileType, int ProfileCount)>> GetProfileTypesWithUsageAsync();

    /// <summary>
    /// Gets all profile types including inactive ones (admin view)
    /// </summary>
    /// <returns>Collection of all profile types</returns>
    Task<IEnumerable<ProfileType>> GetAllProfileTypesAsync();

    /// <summary>
    /// Activates a profile type
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <returns>True if activated, false if not found</returns>
    Task<bool> ActivateAsync(Guid id);

    /// <summary>
    /// Deactivates a profile type
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <returns>True if deactivated, false if not found</returns>
    Task<bool> DeactivateAsync(Guid id);

    /// <summary>
    /// Updates the sort order of profile types
    /// </summary>
    /// <param name="sortOrders">Dictionary of ProfileType ID to new sort order</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateSortOrdersAsync(Dictionary<Guid, int> sortOrders);

    /// <summary>
    /// Gets profile type by ID with profiles included
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <returns>ProfileType with profiles if found, null otherwise</returns>
    Task<ProfileType?> GetWithProfilesAsync(Guid id);

    /// <summary>
    /// Gets the next available sort order value
    /// </summary>
    /// <returns>Next sort order value</returns>
    Task<int> GetNextSortOrderAsync();
}