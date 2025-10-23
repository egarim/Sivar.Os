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

    public ProfileTypeService(IProfileTypeRepository profileTypeRepository)
    {
        _profileTypeRepository = profileTypeRepository ?? throw new ArgumentNullException(nameof(profileTypeRepository));
    }

    /// <summary>
    /// Gets all active profile types available for users
    /// </summary>
    public async Task<IEnumerable<ProfileTypeDto>> GetActiveProfileTypesAsync()
    {
        var profileTypes = await _profileTypeRepository.GetActiveProfileTypesAsync();
        return profileTypes.Select(MapToProfileTypeDto);
    }

    /// <summary>
    /// Gets all profile types including inactive ones (admin only)
    /// </summary>
    public async Task<IEnumerable<ProfileTypeDto>> GetAllProfileTypesAsync()
    {
        var profileTypes = await _profileTypeRepository.GetAllProfileTypesAsync();
        return profileTypes.Select(MapToProfileTypeDto);
    }

    /// <summary>
    /// Gets a profile type by ID
    /// </summary>
    public async Task<ProfileTypeDto?> GetProfileTypeByIdAsync(Guid id)
    {
        var profileType = await _profileTypeRepository.GetByIdAsync(id);
        return profileType != null ? MapToProfileTypeDto(profileType) : null;
    }

    /// <summary>
    /// Gets a profile type by name
    /// </summary>
    public async Task<ProfileTypeDto?> GetProfileTypeByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var profileType = await _profileTypeRepository.GetByNameAsync(name);
        return profileType != null ? MapToProfileTypeDto(profileType) : null;
    }

    /// <summary>
    /// Creates a new profile type (admin only)
    /// </summary>
    public async Task<ProfileTypeDto?> CreateProfileTypeAsync(CreateProfileTypeDto createDto)
    {
        if (createDto == null)
            throw new ArgumentNullException(nameof(createDto));

        // Validate name is unique
        if (await _profileTypeRepository.NameExistsAsync(createDto.Name))
            return null; // Name already exists

        // Get next sort order if not specified
        var sortOrder = createDto.SortOrder > 0 ? createDto.SortOrder 
            : await _profileTypeRepository.GetNextSortOrderAsync();

        var profileType = new ProfileType
        {
            Name = createDto.Name,
            DisplayName = createDto.DisplayName,
            Description = createDto.Description,
            SortOrder = sortOrder,
            FeatureFlags = createDto.FeatureFlags,
            IsActive = true
        };

        await _profileTypeRepository.AddAsync(profileType);
        await _profileTypeRepository.SaveChangesAsync();

        return MapToProfileTypeDto(profileType);
    }

    /// <summary>
    /// Updates an existing profile type (admin only)
    /// </summary>
    public async Task<ProfileTypeDto?> UpdateProfileTypeAsync(Guid id, UpdateProfileTypeDto updateDto)
    {
        if (updateDto == null)
            throw new ArgumentNullException(nameof(updateDto));

        var profileType = await _profileTypeRepository.GetByIdAsync(id);
        if (profileType == null)
            return null;

        // Update properties
        profileType.DisplayName = updateDto.DisplayName;
        profileType.Description = updateDto.Description;
        profileType.SortOrder = updateDto.SortOrder;
        profileType.FeatureFlags = updateDto.FeatureFlags;
        profileType.IsActive = updateDto.IsActive;

        await _profileTypeRepository.UpdateAsync(profileType);
        await _profileTypeRepository.SaveChangesAsync();

        return MapToProfileTypeDto(profileType);
    }

    /// <summary>
    /// Deletes (soft delete) a profile type (admin only)
    /// </summary>
    public async Task<bool> DeleteProfileTypeAsync(Guid id)
    {
        var result = await _profileTypeRepository.DeleteAsync(id);
        if (result)
            await _profileTypeRepository.SaveChangesAsync();

        return result;
    }

    /// <summary>
    /// Activates a profile type (admin only)
    /// </summary>
    public async Task<bool> ActivateProfileTypeAsync(Guid id)
    {
        var result = await _profileTypeRepository.ActivateAsync(id);
        if (result)
            await _profileTypeRepository.SaveChangesAsync();

        return result;
    }

    /// <summary>
    /// Deactivates a profile type (admin only)
    /// </summary>
    public async Task<bool> DeactivateProfileTypeAsync(Guid id)
    {
        var result = await _profileTypeRepository.DeactivateAsync(id);
        if (result)
            await _profileTypeRepository.SaveChangesAsync();

        return result;
    }

    /// <summary>
    /// Gets profile types with usage statistics (admin only)
    /// </summary>
    public async Task<IEnumerable<ProfileTypeDto>> GetProfileTypesWithUsageAsync()
    {
        var profileTypesWithUsage = await _profileTypeRepository.GetProfileTypesWithUsageAsync();
        return profileTypesWithUsage.Select(x => 
        {
            var dto = MapToProfileTypeDto(x.ProfileType);
            dto.ProfileCount = x.ProfileCount;
            return dto;
        });
    }

    /// <summary>
    /// Updates sort orders for profile types (admin only)
    /// </summary>
    public async Task<bool> UpdateSortOrdersAsync(Dictionary<Guid, int> sortOrders)
    {
        if (sortOrders == null || !sortOrders.Any())
            return false;

        var result = await _profileTypeRepository.UpdateSortOrdersAsync(sortOrders);
        if (result)
            await _profileTypeRepository.SaveChangesAsync();

        return result;
    }

    /// <summary>
    /// Validates if a profile type name is available
    /// </summary>
    public async Task<bool> IsNameAvailableAsync(string name, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return !await _profileTypeRepository.NameExistsAsync(name, excludeId);
    }

    /// <summary>
    /// Gets the PersonalProfile type (default type)
    /// </summary>
    public async Task<ProfileTypeDto?> GetPersonalProfileTypeAsync()
    {
        var personalProfile = await _profileTypeRepository.GetByNameAsync("PersonalProfile");
        return personalProfile != null ? MapToProfileTypeDto(personalProfile) : null;
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