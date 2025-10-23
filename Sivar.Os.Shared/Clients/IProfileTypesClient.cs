using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client for profile type operations
/// </summary>
public interface IProfileTypesClient
{
    // Query operations
    Task<IEnumerable<ProfileTypeDto>> GetActiveProfileTypesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProfileTypeDto>> GetAllProfileTypesAsync(CancellationToken cancellationToken = default);
    Task<ProfileTypeDto> GetProfileTypeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProfileTypeDto>> GetProfileTypesWithUsageAsync(CancellationToken cancellationToken = default);

    // CRUD operations (admin)
    Task<ProfileTypeDto> CreateProfileTypeAsync(CreateProfileTypeDto request, CancellationToken cancellationToken = default);
    Task<ProfileTypeDto> UpdateProfileTypeAsync(Guid id, UpdateProfileTypeDto request, CancellationToken cancellationToken = default);
    Task DeleteProfileTypeAsync(Guid id, CancellationToken cancellationToken = default);

    // Status operations (admin)
    Task<ProfileTypeDto> ActivateProfileTypeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProfileTypeDto> DeactivateProfileTypeAsync(Guid id, CancellationToken cancellationToken = default);

    // Ordering (admin)
    Task UpdateSortOrdersAsync(Dictionary<Guid, int> sortOrders, CancellationToken cancellationToken = default);

    // Validation
    Task<bool> CheckNameAvailabilityAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
