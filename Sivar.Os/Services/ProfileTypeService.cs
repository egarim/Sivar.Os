using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service implementation for ProfileType management (admin functions)
/// </summary>
public class ProfileTypeService : IProfileTypeService
{
    private readonly IProfileTypeRepository _profileTypeRepository;
    private readonly ILogger<ProfileTypeService> _logger;

    public ProfileTypeService(IProfileTypeRepository profileTypeRepository, ILogger<ProfileTypeService> logger)
    {
        _profileTypeRepository = profileTypeRepository ?? throw new ArgumentNullException(nameof(profileTypeRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all active profile types available for users
    /// </summary>
    public async Task<IEnumerable<ProfileTypeDto>> GetActiveProfileTypesAsync()
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.GetActiveProfileTypesAsync] START - RequestId={RequestId}",
            requestId);

        try
        {
            var profileTypes = await _profileTypeRepository.GetActiveProfileTypesAsync();

            _logger.LogInformation("[ProfileTypeService.GetActiveProfileTypesAsync] Retrieved {ProfileTypeCount} active profile types - RequestId={RequestId}",
                profileTypes.Count(), requestId);

            var dtos = profileTypes.Select(MapToProfileTypeDto).ToList();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.GetActiveProfileTypesAsync] SUCCESS - RequestId={RequestId}, Count={Count}, Duration={Duration}ms",
                requestId, dtos.Count, elapsed);

            return dtos;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.GetActiveProfileTypesAsync] ERROR - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Gets all profile types including inactive ones (admin only)
    /// </summary>
    public async Task<IEnumerable<ProfileTypeDto>> GetAllProfileTypesAsync()
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.GetAllProfileTypesAsync] START - RequestId={RequestId} (Admin operation)",
            requestId);

        try
        {
            var profileTypes = await _profileTypeRepository.GetAllProfileTypesAsync();

            _logger.LogInformation("[ProfileTypeService.GetAllProfileTypesAsync] Retrieved {ProfileTypeCount} profile types (including inactive) - RequestId={RequestId}",
                profileTypes.Count(), requestId);

            var dtos = profileTypes.Select(MapToProfileTypeDto).ToList();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.GetAllProfileTypesAsync] SUCCESS - RequestId={RequestId}, Count={Count}, Duration={Duration}ms",
                requestId, dtos.Count, elapsed);

            return dtos;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.GetAllProfileTypesAsync] ERROR - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Gets a profile type by ID
    /// </summary>
    public async Task<ProfileTypeDto?> GetProfileTypeByIdAsync(Guid id)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.GetProfileTypeByIdAsync] START - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}",
            requestId, id);

        try
        {
            var profileType = await _profileTypeRepository.GetByIdAsync(id);

            if (profileType == null)
            {
                _logger.LogWarning("[ProfileTypeService.GetProfileTypeByIdAsync] Profile type not found - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}",
                    requestId, id);
                return null;
            }

            _logger.LogInformation("[ProfileTypeService.GetProfileTypeByIdAsync] Profile type found - RequestId={RequestId}, Name={Name}, IsActive={IsActive}",
                requestId, profileType.Name, profileType.IsActive);

            var dto = MapToProfileTypeDto(profileType);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.GetProfileTypeByIdAsync] SUCCESS - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, Duration={Duration}ms",
                requestId, id, elapsed);

            return dto;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.GetProfileTypeByIdAsync] ERROR - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, Duration={Duration}ms",
                requestId, id, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Gets a profile type by name
    /// </summary>
    public async Task<ProfileTypeDto?> GetProfileTypeByNameAsync(string name)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.GetProfileTypeByNameAsync] START - RequestId={RequestId}, Name={Name}",
            requestId, name ?? "NULL");

        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("[ProfileTypeService.GetProfileTypeByNameAsync] Invalid name parameter - RequestId={RequestId}",
                    requestId);
                return null;
            }

            var profileType = await _profileTypeRepository.GetByNameAsync(name);

            if (profileType == null)
            {
                _logger.LogWarning("[ProfileTypeService.GetProfileTypeByNameAsync] Profile type not found - RequestId={RequestId}, Name={Name}",
                    requestId, name);
                return null;
            }

            _logger.LogInformation("[ProfileTypeService.GetProfileTypeByNameAsync] Profile type found - RequestId={RequestId}, Name={Name}, IsActive={IsActive}",
                requestId, name, profileType.IsActive);

            var dto = MapToProfileTypeDto(profileType);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.GetProfileTypeByNameAsync] SUCCESS - RequestId={RequestId}, Name={Name}, Duration={Duration}ms",
                requestId, name, elapsed);

            return dto;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.GetProfileTypeByNameAsync] ERROR - RequestId={RequestId}, Name={Name}, Duration={Duration}ms",
                requestId, name ?? "NULL", elapsed);
            throw;
        }
    }

    /// <summary>
    /// Creates a new profile type (admin only)
    /// </summary>
    public async Task<ProfileTypeDto?> CreateProfileTypeAsync(CreateProfileTypeDto createDto)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.CreateProfileTypeAsync] START - RequestId={RequestId}, Name={Name} (Admin operation)",
            requestId, createDto?.Name ?? "NULL");

        try
        {
            if (createDto == null)
            {
                _logger.LogWarning("[ProfileTypeService.CreateProfileTypeAsync] Invalid DTO - RequestId={RequestId}",
                    requestId);
                throw new ArgumentNullException(nameof(createDto));
            }

            _logger.LogInformation("[ProfileTypeService.CreateProfileTypeAsync] Validating name uniqueness - RequestId={RequestId}, Name={Name}",
                requestId, createDto.Name);

            // Validate name is unique
            if (await _profileTypeRepository.NameExistsAsync(createDto.Name))
            {
                _logger.LogWarning("[ProfileTypeService.CreateProfileTypeAsync] Name already exists - RequestId={RequestId}, Name={Name}",
                    requestId, createDto.Name);
                return null; // Name already exists
            }

            _logger.LogInformation("[ProfileTypeService.CreateProfileTypeAsync] Name is unique - RequestId={RequestId}, Name={Name}",
                requestId, createDto.Name);

            // Get next sort order if not specified
            var sortOrder = createDto.SortOrder > 0 ? createDto.SortOrder 
                : await _profileTypeRepository.GetNextSortOrderAsync();

            _logger.LogInformation("[ProfileTypeService.CreateProfileTypeAsync] Determined sort order - RequestId={RequestId}, SortOrder={SortOrder}",
                requestId, sortOrder);

            var profileType = new ProfileType
            {
                Name = createDto.Name,
                DisplayName = createDto.DisplayName,
                Description = createDto.Description,
                SortOrder = sortOrder,
                FeatureFlags = createDto.FeatureFlags,
                IsActive = true
            };

            _logger.LogInformation("[ProfileTypeService.CreateProfileTypeAsync] Adding profile type to repository - RequestId={RequestId}, Name={Name}, DisplayName={DisplayName}",
                requestId, createDto.Name, createDto.DisplayName);

            await _profileTypeRepository.AddAsync(profileType);
            await _profileTypeRepository.SaveChangesAsync();

            _logger.LogInformation("[ProfileTypeService.CreateProfileTypeAsync] Profile type created - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, Name={Name}",
                requestId, profileType.Id, profileType.Name);

            var dto = MapToProfileTypeDto(profileType);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.CreateProfileTypeAsync] SUCCESS - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, Duration={Duration}ms",
                requestId, profileType.Id, elapsed);

            return dto;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.CreateProfileTypeAsync] ERROR - RequestId={RequestId}, Name={Name}, Duration={Duration}ms",
                requestId, createDto?.Name ?? "NULL", elapsed);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing profile type (admin only)
    /// </summary>
    public async Task<ProfileTypeDto?> UpdateProfileTypeAsync(Guid id, UpdateProfileTypeDto updateDto)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.UpdateProfileTypeAsync] START - RequestId={RequestId}, ProfileTypeId={ProfileTypeId} (Admin operation)",
            requestId, id);

        try
        {
            if (updateDto == null)
            {
                _logger.LogWarning("[ProfileTypeService.UpdateProfileTypeAsync] Invalid DTO - RequestId={RequestId}",
                    requestId);
                throw new ArgumentNullException(nameof(updateDto));
            }

            var profileType = await _profileTypeRepository.GetByIdAsync(id);
            if (profileType == null)
            {
                _logger.LogWarning("[ProfileTypeService.UpdateProfileTypeAsync] Profile type not found - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}",
                    requestId, id);
                return null;
            }

            _logger.LogInformation("[ProfileTypeService.UpdateProfileTypeAsync] Profile type found - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, CurrentName={CurrentName}",
                requestId, id, profileType.Name);

            // Update properties
            _logger.LogInformation("[ProfileTypeService.UpdateProfileTypeAsync] Updating properties - RequestId={RequestId}, DisplayName={DisplayName}, SortOrder={SortOrder}, IsActive={IsActive}",
                requestId, updateDto.DisplayName, updateDto.SortOrder, updateDto.IsActive);

            profileType.DisplayName = updateDto.DisplayName;
            profileType.Description = updateDto.Description;
            profileType.SortOrder = updateDto.SortOrder;
            profileType.FeatureFlags = updateDto.FeatureFlags;
            profileType.IsActive = updateDto.IsActive;

            await _profileTypeRepository.UpdateAsync(profileType);
            await _profileTypeRepository.SaveChangesAsync();

            _logger.LogInformation("[ProfileTypeService.UpdateProfileTypeAsync] Profile type updated - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}",
                requestId, id);

            var dto = MapToProfileTypeDto(profileType);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.UpdateProfileTypeAsync] SUCCESS - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, Duration={Duration}ms",
                requestId, id, elapsed);

            return dto;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.UpdateProfileTypeAsync] ERROR - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, Duration={Duration}ms",
                requestId, id, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Deletes (soft delete) a profile type (admin only)
    /// </summary>
    public async Task<bool> DeleteProfileTypeAsync(Guid id)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.DeleteProfileTypeAsync] START - RequestId={RequestId}, ProfileTypeId={ProfileTypeId} (Admin operation)",
            requestId, id);

        try
        {
            var result = await _profileTypeRepository.DeleteAsync(id);

            if (!result)
            {
                _logger.LogWarning("[ProfileTypeService.DeleteProfileTypeAsync] Profile type deletion failed - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}",
                    requestId, id);
                return false;
            }

            _logger.LogInformation("[ProfileTypeService.DeleteProfileTypeAsync] Deletion succeeded, saving changes - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}",
                requestId, id);

            await _profileTypeRepository.SaveChangesAsync();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.DeleteProfileTypeAsync] SUCCESS - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, Duration={Duration}ms",
                requestId, id, elapsed);

            return true;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.DeleteProfileTypeAsync] ERROR - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, Duration={Duration}ms",
                requestId, id, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Activates a profile type (admin only)
    /// </summary>
    public async Task<bool> ActivateProfileTypeAsync(Guid id)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.ActivateProfileTypeAsync] START - RequestId={RequestId}, ProfileTypeId={ProfileTypeId} (Admin operation)",
            requestId, id);

        try
        {
            var result = await _profileTypeRepository.ActivateAsync(id);

            if (!result)
            {
                _logger.LogWarning("[ProfileTypeService.ActivateProfileTypeAsync] Profile type activation failed - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}",
                    requestId, id);
                return false;
            }

            _logger.LogInformation("[ProfileTypeService.ActivateProfileTypeAsync] Activation succeeded, saving changes - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}",
                requestId, id);

            await _profileTypeRepository.SaveChangesAsync();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.ActivateProfileTypeAsync] SUCCESS - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, Duration={Duration}ms",
                requestId, id, elapsed);

            return true;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.ActivateProfileTypeAsync] ERROR - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, Duration={Duration}ms",
                requestId, id, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Deactivates a profile type (admin only)
    /// </summary>
    public async Task<bool> DeactivateProfileTypeAsync(Guid id)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.DeactivateProfileTypeAsync] START - RequestId={RequestId}, ProfileTypeId={ProfileTypeId} (Admin operation)",
            requestId, id);

        try
        {
            var result = await _profileTypeRepository.DeactivateAsync(id);

            if (!result)
            {
                _logger.LogWarning("[ProfileTypeService.DeactivateProfileTypeAsync] Profile type deactivation failed - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}",
                    requestId, id);
                return false;
            }

            _logger.LogInformation("[ProfileTypeService.DeactivateProfileTypeAsync] Deactivation succeeded, saving changes - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}",
                requestId, id);

            await _profileTypeRepository.SaveChangesAsync();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.DeactivateProfileTypeAsync] SUCCESS - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, Duration={Duration}ms",
                requestId, id, elapsed);

            return true;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.DeactivateProfileTypeAsync] ERROR - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}, Duration={Duration}ms",
                requestId, id, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Gets profile types with usage statistics (admin only)
    /// </summary>
    public async Task<IEnumerable<ProfileTypeDto>> GetProfileTypesWithUsageAsync()
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.GetProfileTypesWithUsageAsync] START - RequestId={RequestId} (Admin operation)",
            requestId);

        try
        {
            var profileTypesWithUsage = await _profileTypeRepository.GetProfileTypesWithUsageAsync();

            _logger.LogInformation("[ProfileTypeService.GetProfileTypesWithUsageAsync] Retrieved {ProfileTypeCount} profile types with usage stats - RequestId={RequestId}",
                profileTypesWithUsage.Count(), requestId);

            var dtos = profileTypesWithUsage.Select(x => 
            {
                var dto = MapToProfileTypeDto(x.ProfileType);
                dto.ProfileCount = x.ProfileCount;
                return dto;
            }).ToList();

            _logger.LogInformation("[ProfileTypeService.GetProfileTypesWithUsageAsync] Mapped DTOs with profile counts - RequestId={RequestId}",
                requestId);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.GetProfileTypesWithUsageAsync] SUCCESS - RequestId={RequestId}, Count={Count}, Duration={Duration}ms",
                requestId, dtos.Count, elapsed);

            return dtos;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.GetProfileTypesWithUsageAsync] ERROR - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Updates sort orders for profile types (admin only)
    /// </summary>
    public async Task<bool> UpdateSortOrdersAsync(Dictionary<Guid, int> sortOrders)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.UpdateSortOrdersAsync] START - RequestId={RequestId}, UpdateCount={UpdateCount} (Admin operation)",
            requestId, sortOrders?.Count ?? 0);

        try
        {
            if (sortOrders == null || !sortOrders.Any())
            {
                _logger.LogWarning("[ProfileTypeService.UpdateSortOrdersAsync] Empty sort orders - RequestId={RequestId}",
                    requestId);
                return false;
            }

            _logger.LogInformation("[ProfileTypeService.UpdateSortOrdersAsync] Updating sort orders for {ProfileTypeCount} profile types - RequestId={RequestId}",
                sortOrders.Count, requestId);

            var result = await _profileTypeRepository.UpdateSortOrdersAsync(sortOrders);

            if (!result)
            {
                _logger.LogWarning("[ProfileTypeService.UpdateSortOrdersAsync] Sort order update failed - RequestId={RequestId}, Count={Count}",
                    requestId, sortOrders.Count);
                return false;
            }

            _logger.LogInformation("[ProfileTypeService.UpdateSortOrdersAsync] Sort order update succeeded, saving changes - RequestId={RequestId}",
                requestId);

            await _profileTypeRepository.SaveChangesAsync();

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.UpdateSortOrdersAsync] SUCCESS - RequestId={RequestId}, UpdatedCount={UpdatedCount}, Duration={Duration}ms",
                requestId, sortOrders.Count, elapsed);

            return true;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.UpdateSortOrdersAsync] ERROR - RequestId={RequestId}, Count={Count}, Duration={Duration}ms",
                requestId, sortOrders?.Count ?? 0, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Validates if a profile type name is available
    /// </summary>
    public async Task<bool> IsNameAvailableAsync(string name, Guid? excludeId = null)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.IsNameAvailableAsync] START - RequestId={RequestId}, Name={Name}, ExcludeId={ExcludeId}",
            requestId, name ?? "NULL", excludeId?.ToString() ?? "NULL");

        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("[ProfileTypeService.IsNameAvailableAsync] Invalid name parameter - RequestId={RequestId}",
                    requestId);
                return false;
            }

            var nameExists = await _profileTypeRepository.NameExistsAsync(name, excludeId);
            var isAvailable = !nameExists;

            _logger.LogInformation("[ProfileTypeService.IsNameAvailableAsync] Name availability check - RequestId={RequestId}, Name={Name}, Available={Available}",
                requestId, name, isAvailable);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.IsNameAvailableAsync] SUCCESS - RequestId={RequestId}, Name={Name}, Available={Available}, Duration={Duration}ms",
                requestId, name, isAvailable, elapsed);

            return isAvailable;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.IsNameAvailableAsync] ERROR - RequestId={RequestId}, Name={Name}, Duration={Duration}ms",
                requestId, name ?? "NULL", elapsed);
            throw;
        }
    }

    /// <summary>
    /// Gets the PersonalProfile type (default type)
    /// </summary>
    public async Task<ProfileTypeDto?> GetPersonalProfileTypeAsync()
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[ProfileTypeService.GetPersonalProfileTypeAsync] START - RequestId={RequestId}",
            requestId);

        try
        {
            var personalProfile = await _profileTypeRepository.GetByNameAsync("PersonalProfile");

            if (personalProfile == null)
            {
                _logger.LogWarning("[ProfileTypeService.GetPersonalProfileTypeAsync] PersonalProfile type not found - RequestId={RequestId}",
                    requestId);
                return null;
            }

            _logger.LogInformation("[ProfileTypeService.GetPersonalProfileTypeAsync] PersonalProfile type found - RequestId={RequestId}, ProfileTypeId={ProfileTypeId}",
                requestId, personalProfile.Id);

            var dto = MapToProfileTypeDto(personalProfile);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ProfileTypeService.GetPersonalProfileTypeAsync] SUCCESS - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);

            return dto;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ProfileTypeService.GetPersonalProfileTypeAsync] ERROR - RequestId={RequestId}, Duration={Duration}ms",
                requestId, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Maps ProfileType entity to ProfileTypeDto
    /// </summary>
    private static ProfileTypeDto MapToProfileTypeDto(ProfileType profileType)
    {
        return new ProfileTypeDto
        {
            Id = profileType.Id,
            Name = profileType.Name,
            DisplayName = profileType.DisplayName,
            Description = profileType.Description,
            IsActive = profileType.IsActive,
            SortOrder = profileType.SortOrder,
            FeatureFlags = profileType.FeatureFlags,
            CreatedAt = profileType.CreatedAt,
            UpdatedAt = profileType.UpdatedAt,
            ProfileCount = 0 // Will be set by calling method if needed
        };
    }
}