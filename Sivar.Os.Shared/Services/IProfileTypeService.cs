
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for ProfileType management (admin functions)
/// </summary>
public interface IProfileTypeService
{
    /// <summary>
    /// Gets all active profile types available for users
    /// </summary>
    /// <returns>Collection of active profile type DTOs</returns>
    Task<IEnumerable<ProfileTypeDto>> GetActiveProfileTypesAsync();

    /// <summary>
    /// Gets all profile types including inactive ones (admin only)
    /// </summary>
    /// <returns>Collection of all profile type DTOs</returns>
    Task<IEnumerable<ProfileTypeDto>> GetAllProfileTypesAsync();

    /// <summary>
    /// Gets a profile type by ID
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <returns>Profile type DTO if found, null otherwise</returns>
    Task<ProfileTypeDto?> GetProfileTypeByIdAsync(Guid id);

    /// <summary>
    /// Gets a profile type by name
    /// </summary>
    /// <param name="name">Profile type name</param>
    /// <returns>Profile type DTO if found, null otherwise</returns>
    Task<ProfileTypeDto?> GetProfileTypeByNameAsync(string name);

    /// <summary>
    /// Creates a new profile type (admin only)
    /// </summary>
    /// <param name="createDto">Profile type creation data</param>
    /// <returns>Created profile type DTO if successful, null otherwise</returns>
    Task<ProfileTypeDto?> CreateProfileTypeAsync(CreateProfileTypeDto createDto);

    /// <summary>
    /// Updates an existing profile type (admin only)
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <param name="updateDto">Profile type update data</param>
    /// <returns>Updated profile type DTO if successful, null otherwise</returns>
    Task<ProfileTypeDto?> UpdateProfileTypeAsync(Guid id, UpdateProfileTypeDto updateDto);

    /// <summary>
    /// Deletes (soft delete) a profile type (admin only)
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <returns>True if deleted, false otherwise</returns>
    Task<bool> DeleteProfileTypeAsync(Guid id);

    /// <summary>
    /// Activates a profile type (admin only)
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <returns>True if activated, false otherwise</returns>
    Task<bool> ActivateProfileTypeAsync(Guid id);

    /// <summary>
    /// Deactivates a profile type (admin only)
    /// </summary>
    /// <param name="id">Profile type ID</param>
    /// <returns>True if deactivated, false otherwise</returns>
    Task<bool> DeactivateProfileTypeAsync(Guid id);

    /// <summary>
    /// Gets profile types with usage statistics (admin only)
    /// </summary>
    /// <returns>Collection of profile types with their usage counts</returns>
    Task<IEnumerable<ProfileTypeDto>> GetProfileTypesWithUsageAsync();

    /// <summary>
    /// Updates sort orders for profile types (admin only)
    /// </summary>
    /// <param name="sortOrders">Dictionary of profile type ID to new sort order</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateSortOrdersAsync(Dictionary<Guid, int> sortOrders);

    /// <summary>
    /// Validates if a profile type name is available
    /// </summary>
    /// <param name="name">Profile type name to check</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates)</param>
    /// <returns>True if name is available, false if already exists</returns>
    Task<bool> IsNameAvailableAsync(string name, Guid? excludeId = null);

    /// <summary>
    /// Gets the PersonalProfile type (default type)
    /// </summary>
    /// <returns>PersonalProfile type DTO</returns>
    Task<ProfileTypeDto?> GetPersonalProfileTypeAsync();
}