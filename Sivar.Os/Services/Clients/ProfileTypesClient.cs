using Sivar.Core.Clients.ProfileTypes;
using Sivar.Core.DTOs;
using Sivar.Core.Interfaces;
using Sivar.Core.Repositories;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of profile types client
/// </summary>
public class ProfileTypesClient : BaseRepositoryClient, IProfileTypesClient
{
    private readonly IProfileTypeService _profileTypeService;
    private readonly IProfileTypeRepository _profileTypeRepository;
    private readonly ILogger<ProfileTypesClient> _logger;

    public ProfileTypesClient(
        IProfileTypeService profileTypeService,
        IProfileTypeRepository profileTypeRepository,
        ILogger<ProfileTypesClient> logger)
    {
        _profileTypeService = profileTypeService ?? throw new ArgumentNullException(nameof(profileTypeService));
        _profileTypeRepository = profileTypeRepository ?? throw new ArgumentNullException(nameof(profileTypeRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Query operations
    public async Task<IEnumerable<ProfileTypeDto>> GetActiveProfileTypesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetActiveProfileTypesAsync");
        return new List<ProfileTypeDto>();
    }

    public async Task<IEnumerable<ProfileTypeDto>> GetAllProfileTypesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var profileTypes = await _profileTypeRepository.GetAllAsync();
            _logger.LogInformation("All profile types retrieved: {Count}", profileTypes.Count());
            return profileTypes.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all profile types");
            throw;
        }
    }

    public async Task<ProfileTypeDto> GetProfileTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning("GetProfileTypeAsync called with empty ID");
            return new ProfileTypeDto();
        }

        try
        {
            var profileType = await _profileTypeRepository.GetByIdAsync(id);
            _logger.LogInformation("ProfileType retrieved: {Id}", id);
            return profileType != null ? MapToDto(profileType) : new ProfileTypeDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile type {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<ProfileTypeDto>> GetProfileTypesWithUsageAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetProfileTypesWithUsageAsync");
        return new List<ProfileTypeDto>();
    }

    // CRUD operations (admin)
    public async Task<ProfileTypeDto> CreateProfileTypeAsync(CreateProfileTypeDto request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        _logger.LogInformation("CreateProfileTypeAsync: {Name}", request.Name);
        return new ProfileTypeDto { Id = Guid.NewGuid(), Name = request.Name };
    }

    public async Task<ProfileTypeDto> UpdateProfileTypeAsync(Guid id, UpdateProfileTypeDto request, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || request == null) throw new ArgumentException("Invalid parameters");
        _logger.LogInformation("UpdateProfileTypeAsync: {Id}", id);
        return new ProfileTypeDto { Id = id };
    }

    public async Task DeleteProfileTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning("DeleteProfileTypeAsync called with empty ID");
            return;
        }

        try
        {
            await _profileTypeRepository.DeleteAsync(id);
            _logger.LogInformation("ProfileType deleted: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile type {Id}", id);
            throw;
        }
    }

    // Status operations (admin)
    public async Task<ProfileTypeDto> ActivateProfileTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ActivateProfileTypeAsync: {Id}", id);
        return new ProfileTypeDto { Id = id };
    }

    public async Task<ProfileTypeDto> DeactivateProfileTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DeactivateProfileTypeAsync: {Id}", id);
        return new ProfileTypeDto { Id = id };
    }

    // Ordering (admin)
    public async Task UpdateSortOrdersAsync(Dictionary<Guid, int> sortOrders, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UpdateSortOrdersAsync");
    }

    // Validation
    public async Task<bool> CheckNameAvailabilityAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CheckNameAvailabilityAsync: {Name}", name);
        return true;
    }

    private ProfileTypeDto MapToDto(Core.Entities.ProfileType profileType)
    {
        return new ProfileTypeDto
        {
            Id = profileType.Id,
            Name = profileType.Name,
            Description = profileType.Description
        };
    }
}
