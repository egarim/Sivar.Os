using System.Text.Json;

using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service implementation for Profile management (PersonalProfile focus)
/// </summary>
public class ProfileService : IProfileService
{
    private readonly IProfileRepository _profileRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProfileTypeRepository _profileTypeRepository;
    private readonly IProfileMetadataValidator _metadataValidator;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        IProfileRepository profileRepository, 
        IUserRepository userRepository,
        IProfileTypeRepository profileTypeRepository,
        IProfileMetadataValidator metadataValidator,
        IFileStorageService fileStorageService,
        ILogger<ProfileService> logger)
    {
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _profileTypeRepository = profileTypeRepository ?? throw new ArgumentNullException(nameof(profileTypeRepository));
        _metadataValidator = metadataValidator ?? throw new ArgumentNullException(nameof(metadataValidator));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Profile Type Helpers

    /// <summary>
    /// Gets a profile type ID by its name
    /// </summary>
    /// <param name="profileTypeName">Name of the profile type (e.g., "PersonalProfile")</param>
    /// <returns>Profile type ID if found, null otherwise</returns>
    private async Task<Guid?> GetProfileTypeIdByNameAsync(string profileTypeName)
    {
        var profileType = await _profileTypeRepository.GetByNameAsync(profileTypeName);
        return profileType?.Id;
    }

    /// <summary>
    /// Gets the default profile type ID (PersonalProfile)
    /// </summary>
    /// <returns>PersonalProfile type ID, or Guid.Empty if not found</returns>
    private async Task<Guid> GetDefaultProfileTypeIdAsync()
    {
        var personalTypeId = await GetProfileTypeIdByNameAsync("PersonalProfile");
        return personalTypeId ?? Guid.Empty;
    }

    #endregion

    /// <summary>
    /// Gets the current user's personal profile
    /// </summary>
    public async Task<ProfileDto?> GetMyProfileAsync(string keycloakId)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ProfileService.GetMyProfileAsync] START - RequestId={RequestId}, KeycloakId={KeycloakId}", 
            requestId, keycloakId ?? "NULL");

        if (string.IsNullOrWhiteSpace(keycloakId))
        {
            _logger.LogWarning("[ProfileService.GetMyProfileAsync] NULL_KEYCLOAK_ID - RequestId={RequestId}", requestId);
            return null;
        }

        var personalProfileTypeId = await GetProfileTypeIdByNameAsync("PersonalProfile");
        if (personalProfileTypeId == null)
        {
            _logger.LogWarning("[ProfileService.GetMyProfileAsync] PERSONAL_PROFILE_TYPE_NOT_FOUND - RequestId={RequestId}", requestId);
            return null;
        }

        var profiles = await _profileRepository.GetProfilesByKeycloakIdAsync(keycloakId);
        var personalProfile = profiles.FirstOrDefault(p => p.ProfileTypeId == personalProfileTypeId.Value);

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        if (personalProfile != null)
        {
            _logger.LogInformation("[ProfileService.GetMyProfileAsync] SUCCESS - ProfileId={ProfileId}, RequestId={RequestId}, Duration={Duration}ms", 
                personalProfile.Id, requestId, elapsed);
        }
        else
        {
            _logger.LogInformation("[ProfileService.GetMyProfileAsync] NO_PROFILE_FOUND - KeycloakId={KeycloakId}, RequestId={RequestId}, Duration={Duration}ms", 
                keycloakId, requestId, elapsed);
        }

        return personalProfile != null ? await MapToProfileDtoAsync(personalProfile) : null;
    }

    /// <summary>
    /// Creates a personal profile for the current user
    /// </summary>
    public async Task<ProfileDto?> CreateMyProfileAsync(string keycloakId, CreateProfileDto createDto)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ProfileService.CreateMyProfileAsync] START - RequestId={RequestId}, KeycloakId={KeycloakId}, DisplayName={DisplayName}", 
            requestId, keycloakId ?? "NULL", createDto?.DisplayName);

        if (string.IsNullOrWhiteSpace(keycloakId) || createDto == null)
        {
            _logger.LogWarning("[ProfileService.CreateMyProfileAsync] INVALID_INPUT - KeycloakId={KeycloakId}, CreateDto={CreateDto}, RequestId={RequestId}", 
                keycloakId ?? "NULL", createDto != null ? "PRESENT" : "NULL", requestId);
            return null;
        }

        // Validate profile data
        var validation = await ValidateProfileDataAsync(createDto);
        if (!validation.IsValid)
        {
            _logger.LogWarning("[ProfileService.CreateMyProfileAsync] VALIDATION_FAILED - Errors={Errors}, RequestId={RequestId}", 
                string.Join(", ", validation.Errors), requestId);
            return null;
        }

        // Get user
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
        {
            _logger.LogWarning("[ProfileService.CreateMyProfileAsync] USER_NOT_FOUND - KeycloakId={KeycloakId}, RequestId={RequestId}", 
                keycloakId, requestId);
            return null;
        }

        _logger.LogInformation("[ProfileService.CreateMyProfileAsync] User found - UserId={UserId}, RequestId={RequestId}", 
            user.Id, requestId);

        // Check if user already has a personal profile
        if (await UserHasPersonalProfileAsync(keycloakId))
        {
            _logger.LogWarning("[ProfileService.CreateMyProfileAsync] PROFILE_ALREADY_EXISTS - KeycloakId={KeycloakId}, RequestId={RequestId}", 
                keycloakId, requestId);
            return null; // User already has a personal profile
        }

        // Get PersonalProfile type ID
        var personalProfileTypeId = await GetProfileTypeIdByNameAsync("PersonalProfile");
        if (personalProfileTypeId == null)
        {
            _logger.LogError("[ProfileService.CreateMyProfileAsync] PROFILE_TYPE_NOT_FOUND - ProfileTypeName=PersonalProfile, RequestId={RequestId}", 
                requestId);
            return null; // PersonalProfile type not found
        }

        // Create profile
        var profile = new Profile
        {
            UserId = user.Id,
            ProfileTypeId = personalProfileTypeId.Value,
            DisplayName = createDto.DisplayName,
            Bio = createDto.Bio,
            Avatar = createDto.Avatar,
            Location = createDto.Location,
            VisibilityLevel = createDto.IsPublic ? VisibilityLevel.Public : VisibilityLevel.Private,
            IsActive = true // Set as active since it's likely their only profile
        };

        await _profileRepository.AddAsync(profile);
        await _profileRepository.SaveChangesAsync();

        _logger.LogInformation("[ProfileService.CreateMyProfileAsync] Profile created - ProfileId={ProfileId}, RequestId={RequestId}", 
            profile.Id, requestId);

        // Load the profile with related data
        var createdProfile = await _profileRepository.GetWithRelatedDataAsync(profile.Id);
        
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation("[ProfileService.CreateMyProfileAsync] SUCCESS - ProfileId={ProfileId}, UserId={UserId}, RequestId={RequestId}, Duration={Duration}ms", 
            profile.Id, user.Id, requestId, elapsed);

        return createdProfile != null ? await MapToProfileDtoAsync(createdProfile) : null;
    }

    /// <summary>
    /// Updates the current user's personal profile
    /// </summary>
    public async Task<ProfileDto?> UpdateMyProfileAsync(string keycloakId, UpdateProfileDto updateDto)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ProfileService.UpdateMyProfileAsync] START - RequestId={RequestId}, KeycloakId={KeycloakId}, DisplayName={DisplayName}", 
            requestId, keycloakId ?? "NULL", updateDto?.DisplayName);

        if (string.IsNullOrWhiteSpace(keycloakId) || updateDto == null)
        {
            _logger.LogWarning("[ProfileService.UpdateMyProfileAsync] INVALID_INPUT - RequestId={RequestId}", requestId);
            return null;
        }

        // Get PersonalProfile type ID
        var personalProfileTypeId = await GetProfileTypeIdByNameAsync("PersonalProfile");
        if (personalProfileTypeId == null)
        {
            _logger.LogError("[ProfileService.UpdateMyProfileAsync] PROFILE_TYPE_NOT_FOUND - RequestId={RequestId}", requestId);
            return null;
        }

        // Get user's personal profile
        var profiles = await _profileRepository.GetProfilesByKeycloakIdAsync(keycloakId);
        var personalProfile = profiles.FirstOrDefault(p => p.ProfileTypeId == personalProfileTypeId.Value);

        if (personalProfile == null)
        {
            _logger.LogWarning("[ProfileService.UpdateMyProfileAsync] PROFILE_NOT_FOUND - KeycloakId={KeycloakId}, RequestId={RequestId}", 
                keycloakId, requestId);
            return null;
        }

        _logger.LogInformation("[ProfileService.UpdateMyProfileAsync] Profile found - ProfileId={ProfileId}, RequestId={RequestId}", 
            personalProfile.Id, requestId);

        // Get user
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
        {
            _logger.LogWarning("[ProfileService.UpdateMyProfileAsync] USER_NOT_FOUND - KeycloakId={KeycloakId}, RequestId={RequestId}", 
                keycloakId, requestId);
            return null;
        }

        // Check for duplicate display names within user's profiles (excluding the current profile)
        var existingProfiles = await _profileRepository.GetProfilesByUserIdAsync(user.Id, includeInactive: true);
        if (existingProfiles.Any(p => p.Id != personalProfile.Id && 
                                      p.DisplayName.Equals(updateDto.DisplayName, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("[ProfileService.UpdateMyProfileAsync] DUPLICATE_DISPLAY_NAME - DisplayName={DisplayName}, RequestId={RequestId}", 
                updateDto.DisplayName, requestId);
            return null; // A profile with this display name already exists for this user
        }

        // Update profile
        personalProfile.DisplayName = updateDto.DisplayName;
        personalProfile.Bio = updateDto.Bio;
        personalProfile.Avatar = updateDto.Avatar;
        personalProfile.Location = updateDto.Location;
        personalProfile.VisibilityLevel = updateDto.IsPublic ? VisibilityLevel.Public : VisibilityLevel.Private;

        await _profileRepository.UpdateAsync(personalProfile);
        await _profileRepository.SaveChangesAsync();

        _logger.LogInformation("[ProfileService.UpdateMyProfileAsync] Profile updated - ProfileId={ProfileId}, RequestId={RequestId}", 
            personalProfile.Id, requestId);

        // Load updated profile with related data
        var updatedProfile = await _profileRepository.GetWithRelatedDataAsync(personalProfile.Id);
        
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation("[ProfileService.UpdateMyProfileAsync] SUCCESS - ProfileId={ProfileId}, RequestId={RequestId}, Duration={Duration}ms", 
            personalProfile.Id, requestId, elapsed);

        return updatedProfile != null ? await MapToProfileDtoAsync(updatedProfile) : null;
    }

    /// <summary>
    /// Deletes the current user's personal profile
    /// </summary>
    public async Task<bool> DeleteMyProfileAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        var personalProfileTypeId = await GetProfileTypeIdByNameAsync("PersonalProfile");
        if (personalProfileTypeId == null)
            return false;

        var profiles = await _profileRepository.GetProfilesByKeycloakIdAsync(keycloakId);
        var personalProfile = profiles.FirstOrDefault(p => p.ProfileTypeId == personalProfileTypeId.Value);

        if (personalProfile == null)
            return false;

        var result = await _profileRepository.DeleteAsync(personalProfile.Id);
        if (result)
            await _profileRepository.SaveChangesAsync();

        return result;
    }

    /// <summary>
    /// Gets all profiles for the current user
    /// </summary>
    public async Task<IEnumerable<ProfileDto>> GetMyProfilesAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return new List<ProfileDto>();

        var profiles = await _profileRepository.GetProfilesByKeycloakIdAsync(keycloakId);
        var profileDtos = new List<ProfileDto>();

        foreach (var profile in profiles)
        {
            var dto = await MapToProfileDtoAsync(profile);
            if (dto != null)
                profileDtos.Add(dto);
        }

        return profileDtos;
    }

    /// <summary>
    /// Gets the current user's active profile
    /// </summary>
    public async Task<ProfileDto?> GetMyActiveProfileAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return null;

        var activeProfile = await _profileRepository.GetActiveProfileByKeycloakIdAsync(keycloakId);
        return activeProfile != null ? await MapToProfileDtoAsync(activeProfile) : null;
    }

    /// <summary>
    /// Sets a profile as active for the current user
    /// </summary>
    public async Task<bool> SetActiveProfileAsync(string keycloakId, Guid profileId)
    {
        _logger.LogInformation("[SetActiveProfileAsync] ========== START ==========");
        _logger.LogInformation("[SetActiveProfileAsync] keycloakId={KeycloakId}, profileId={ProfileId}", keycloakId, profileId);
        
        if (string.IsNullOrWhiteSpace(keycloakId))
        {
            _logger.LogWarning("[SetActiveProfileAsync] Invalid keycloakId");
            return false;
        }

        // SIMPLE DIRECT FIX: Directly set ActiveProfileId on user without validation
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
        {
            _logger.LogWarning("[SetActiveProfileAsync] User not found for keycloakId={KeycloakId}", keycloakId);
            return false;
        }

        _logger.LogInformation("[SetActiveProfileAsync] DIRECT FIX - Setting ActiveProfileId directly");
        _logger.LogInformation("[SetActiveProfileAsync] User: Id={UserId}", user.Id);
        _logger.LogInformation("[SetActiveProfileAsync] BEFORE: ActiveProfileId={OldValue}", user.ActiveProfileId?.ToString() ?? "NULL");
        
        // Set active profile directly
        user.ActiveProfileId = profileId;
        user.UpdatedAt = DateTime.UtcNow;
        
        _logger.LogInformation("[SetActiveProfileAsync] AFTER (in-memory): ActiveProfileId={NewValue}", user.ActiveProfileId?.ToString() ?? "NULL");
        
        // Update and save
        await _userRepository.UpdateAsync(user);
        var changes = await _userRepository.SaveChangesAsync();
        _logger.LogInformation("[SetActiveProfileAsync] SaveChangesAsync returned: {Changes} entities affected", changes);
        
        // VERIFY: Fetch user again to confirm persistence
        var verifyUser = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        _logger.LogInformation("[SetActiveProfileAsync] VERIFICATION: After save, ActiveProfileId={Value}", 
            verifyUser?.ActiveProfileId?.ToString() ?? "NULL");

        _logger.LogInformation("[SetActiveProfileAsync] ========== END ==========");
        return true;
    }

    /// <summary>
    /// Gets a public profile by ID
    /// </summary>
    public async Task<ProfileDto?> GetPublicProfileAsync(Guid profileId)
    {
        var profile = await _profileRepository.GetWithRelatedDataAsync(profileId);
        if (profile == null || profile.VisibilityLevel != VisibilityLevel.Public)
            return null;

        // Increment view count
        await _profileRepository.IncrementViewCountAsync(profileId);
        await _profileRepository.SaveChangesAsync();

        return await MapToProfileDtoAsync(profile);
    }

    /// <summary>
    /// Gets public profiles with pagination
    /// </summary>
    public async Task<PagedResult<ProfileSummaryDto>> GetPublicProfilesAsync(int page = 1, int pageSize = 20)
    {
        var (profiles, totalCount) = await _profileRepository.GetPublicProfilesAsync(page, pageSize);

        var summaries = profiles.Select(MapToProfileSummaryDto).ToList();

        return new PagedResult<ProfileSummaryDto>
        {
            Items = summaries,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalCount
        };
    }

    /// <summary>
    /// Searches public profiles by display name or bio
    /// </summary>
    public async Task<PagedResult<ProfileSummaryDto>> SearchProfilesAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        var (profiles, totalCount) = await _profileRepository.SearchProfilesAsync(searchTerm, page, pageSize);

        var summaries = profiles.Select(MapToProfileSummaryDto).ToList();

        return new PagedResult<ProfileSummaryDto>
        {
            Items = summaries,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalCount
        };
    }

    /// <summary>
    /// Gets profiles by location
    /// </summary>
    public async Task<IEnumerable<ProfileSummaryDto>> GetProfilesByLocationAsync(string locationQuery)
    {
        if (string.IsNullOrWhiteSpace(locationQuery))
            return new List<ProfileSummaryDto>();

        var profiles = await _profileRepository.GetProfilesByLocationAsync(locationQuery);
        return profiles.Select(MapToProfileSummaryDto);
    }

    /// <summary>
    /// Gets the most recent public profiles
    /// </summary>
    public async Task<IEnumerable<ProfileSummaryDto>> GetRecentProfilesAsync(int count = 10)
    {
        var profiles = await _profileRepository.GetRecentProfilesAsync(count);
        return profiles.Select(MapToProfileSummaryDto);
    }

    /// <summary>
    /// Increments the view count for a profile
    /// </summary>
    public async Task<bool> IncrementProfileViewAsync(Guid profileId)
    {
        var result = await _profileRepository.IncrementViewCountAsync(profileId);
        if (result)
            await _profileRepository.SaveChangesAsync();

        return result;
    }

    /// <summary>
    /// Checks if the current user already has a personal profile
    /// </summary>
    public async Task<bool> UserHasPersonalProfileAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
            return false;

        var personalProfileTypeId = await GetProfileTypeIdByNameAsync("PersonalProfile");
        if (personalProfileTypeId == null)
            return false;

        return await _profileRepository.UserHasProfileOfTypeAsync(user.Id, personalProfileTypeId.Value);
    }

    /// <summary>
    /// Validates profile data before creation/update
    /// </summary>
    public Task<ValidationResult> ValidateProfileDataAsync(CreateProfileDto profileData)
    {
        if (profileData == null)
            return Task.FromResult(ValidationResult.Failure("Profile data is required"));

        var errors = new List<string>();

        // Validate display name
        if (string.IsNullOrWhiteSpace(profileData.DisplayName))
            errors.Add("Display name is required");
        else if (profileData.DisplayName.Length > 200)
            errors.Add("Display name cannot exceed 200 characters");

        // Validate bio length
        if (!string.IsNullOrEmpty(profileData.Bio) && profileData.Bio.Length > 1000)
            errors.Add("Bio cannot exceed 1000 characters");

        // Validate avatar URL
        if (!string.IsNullOrEmpty(profileData.Avatar) && profileData.Avatar.Length > 500)
            errors.Add("Avatar URL cannot exceed 500 characters");

        return Task.FromResult(errors.Any() 
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success());
    }

    /// <summary>
    /// Enhanced profile validation for creation with business rules
    /// </summary>
    public async Task<ValidationResult> ValidateProfileCreationAsync(CreateProfileDto profileData, string userKeycloakId, Guid? profileTypeId = null)
    {
        if (profileData == null)
            return ValidationResult.Failure("Profile data is required");

        if (string.IsNullOrWhiteSpace(userKeycloakId))
            return ValidationResult.Failure("User identification is required");

        var errors = new List<string>();

        // Basic data validation
        var basicValidation = await ValidateProfileDataAsync(profileData);
        if (!basicValidation.IsValid)
            errors.AddRange(basicValidation.Errors);

        // Get user
        var user = await _userRepository.GetByKeycloakIdAsync(userKeycloakId);
        if (user == null)
        {
            errors.Add("User not found");
            return ValidationResult.Failure(errors.ToArray());
        }

        // Determine profile type from metadata if not provided
        var targetProfileTypeId = profileTypeId ?? await DetermineProfileTypeFromMetadataAsync(profileData.Metadata);

        // Validate metadata if provided
        if (!string.IsNullOrWhiteSpace(profileData.Metadata))
        {
            var profileType = await _profileTypeRepository.GetByIdAsync(targetProfileTypeId);
            if (profileType != null)
            {
                var metadataValidation = await _metadataValidator.ValidateMetadataAsync(profileData.Metadata, profileType);
                if (!metadataValidation.IsValid)
                {
                    errors.AddRange(metadataValidation.Errors);
                    
                    // Add field-specific errors
                    foreach (var fieldError in metadataValidation.FieldErrors)
                    {
                        foreach (var error in fieldError.Value)
                        {
                            errors.Add($"{fieldError.Key}: {error}");
                        }
                    }
                }
            }
        }

        // Check if user already has a profile of this type (for now, limit to one per type)
        var hasExistingProfile = await _profileRepository.UserHasProfileOfTypeAsync(user.Id, targetProfileTypeId);
        if (hasExistingProfile)
        {
            errors.Add("User already has a profile of this type");
        }

        // Check for duplicate display names within user's profiles
        var existingProfiles = await _profileRepository.GetProfilesByUserIdAsync(user.Id, includeInactive: true);
        if (existingProfiles.Any(p => p.DisplayName.Equals(profileData.DisplayName, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("A profile with this display name already exists for this user");
        }

        // Validate location if provided
        if (profileData.Location != null)
        {
            if (string.IsNullOrWhiteSpace(profileData.Location.City) && 
                string.IsNullOrWhiteSpace(profileData.Location.State) && 
                string.IsNullOrWhiteSpace(profileData.Location.Country))
            {
                errors.Add("If location is provided, at least city, state, or country must be specified");
            }
        }

        return errors.Any() 
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }

    /// <summary>
    /// Implements active profile management business logic
    /// </summary>
    public async Task<ValidationResult> ValidateActiveProfileSwitchAsync(Guid profileId, string userKeycloakId)
    {
        if (string.IsNullOrWhiteSpace(userKeycloakId))
            return ValidationResult.Failure("User identification is required");

        var errors = new List<string>();

        // Get user
        var user = await _userRepository.GetByKeycloakIdAsync(userKeycloakId);
        if (user == null)
        {
            errors.Add("User not found");
            return ValidationResult.Failure(errors.ToArray());
        }

        // Verify profile exists and belongs to user
        var profile = await _profileRepository.GetByIdAsync(profileId);
        if (profile == null)
        {
            errors.Add("Profile not found");
        }
        else if (profile.UserId != user.Id)
        {
            errors.Add("Profile does not belong to this user");
        }
        else if (!profile.IsActive)
        {
            errors.Add("Cannot set inactive profile as active");
        }

        return errors.Any() 
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }

    /// <summary>
    /// Ensures only one active profile per user (business rule enforcement)
    /// </summary>
    public async Task<bool> EnforceOneActiveProfileRuleAsync(Guid newActiveProfileId, Guid userId)
    {
        try
        {
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] ========== START ==========");
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] userId={UserId}, profileId={ProfileId}", userId, newActiveProfileId);
            
            // Get the user
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("[🔵 EnforceOneActiveProfileRuleAsync] ❌ User not found");
                return false;
            }

            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] ✅ User found. Current ActiveProfileId={ActiveProfileId}", 
                user.ActiveProfileId?.ToString() ?? "NULL");

            // Get all user's profiles
            var userProfiles = await _profileRepository.GetProfilesByUserIdAsync(userId, includeInactive: true);
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] Found {Count} profiles", userProfiles.Count());
            
            // Deactivate all profiles except the target one
            var profilesToDeactivate = userProfiles.Where(p => p.Id != newActiveProfileId && p.IsActive).ToList();
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] Deactivating {Count} profiles", profilesToDeactivate.Count);
            
            foreach (var profile in profilesToDeactivate)
            {
                profile.IsActive = false;
                _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] Deactivating profile: {ProfileId}", profile.Id);
                await _profileRepository.UpdateAsync(profile);
            }

            // Activate the target profile
            var targetProfile = userProfiles.FirstOrDefault(p => p.Id == newActiveProfileId);
            if (targetProfile == null)
            {
                _logger.LogWarning("[🔵 EnforceOneActiveProfileRuleAsync] ❌ Target profile not found: {ProfileId}", newActiveProfileId);
                return false;
            }

            targetProfile.IsActive = true;
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] Activating profile: {ProfileId}", targetProfile.Id);
            await _profileRepository.UpdateAsync(targetProfile);

            // Set the user's active profile ID
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] Setting User.ActiveProfileId");
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync]   BEFORE: {OldValue}", user.ActiveProfileId?.ToString() ?? "NULL");
            
            user.ActiveProfileId = newActiveProfileId;
            user.UpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync]   AFTER (in-memory): {NewValue}", user.ActiveProfileId?.ToString() ?? "NULL");
            
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] Calling UpdateAsync on user...");
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] ✅ UpdateAsync completed");

            // Save all changes to database
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] Calling SaveChangesAsync()...");
            var changes = await _userRepository.SaveChangesAsync();
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] ✅ SaveChangesAsync returned: {Changes} entities affected", changes);
            
            // VERIFICATION: Immediately re-fetch from database to confirm persistence
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] VERIFICATION: Re-fetching user from database...");
            var verifyUser = await _userRepository.GetByIdAsync(userId);
            if (verifyUser == null)
            {
                _logger.LogError("[🔵 EnforceOneActiveProfileRuleAsync] ❌❌❌ VERIFICATION FAILED: User not found after update!");
                return false;
            }
            
            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] VERIFICATION RESULT: ActiveProfileId={ActiveProfileId}", 
                verifyUser.ActiveProfileId?.ToString() ?? "❌NULL❌");

            if (verifyUser.ActiveProfileId == newActiveProfileId)
            {
                _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] ✅✅✅ PERSISTENCE CONFIRMED - Value was saved to database!");
            }
            else
            {
                _logger.LogError("[🔵 EnforceOneActiveProfileRuleAsync] ❌❌❌ PERSISTENCE FAILED - Value was NOT saved! Expected={Expected}, Got={Got}",
                    newActiveProfileId, verifyUser.ActiveProfileId?.ToString() ?? "NULL");
            }

            _logger.LogInformation("[🔵 EnforceOneActiveProfileRuleAsync] ========== SUCCESS ==========");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[🔵 EnforceOneActiveProfileRuleAsync] ❌ EXCEPTION: {Message}", ex.Message);
            _logger.LogError("[🔵 EnforceOneActiveProfileRuleAsync] Exception Stack: {StackTrace}", ex.StackTrace);
            return false;
        }
    }

    /// <summary>
    /// Gets profile statistics (admin only)
    /// </summary>
    public async Task<ProfileStatisticsDto> GetProfileStatisticsAsync()
    {
        var stats = await _profileRepository.GetProfileStatisticsAsync();

        return new ProfileStatisticsDto
        {
            TotalProfiles = stats.TotalProfiles,
            PublicProfiles = stats.PublicProfiles,
            ActiveProfiles = stats.ActiveProfiles,
            NewProfilesLast30Days = stats.NewProfilesLast30Days,
            AverageViewCount = stats.AverageViewCount,
            ProfilesByType = new Dictionary<string, int>
            {
                { "PersonalProfile", stats.TotalProfiles } // For now, all are personal profiles
            }
        };
    }

    /// <summary>
    /// Creates a profile for a user (supports all profile types)
    /// </summary>
    public async Task<ProfileDto?> CreateProfileAsync(CreateProfileDto createDto, string userKeycloakId)
    {
        if (createDto == null || string.IsNullOrWhiteSpace(userKeycloakId))
            return null;

        // Enhanced validation with business rules
        var validation = await ValidateProfileCreationAsync(createDto, userKeycloakId);
        if (!validation.IsValid)
            return null;

        // Get user
        var user = await _userRepository.GetByKeycloakIdAsync(userKeycloakId);
        if (user == null)
            return null;

        // Determine profile type based on metadata content
        var profileTypeId = await DetermineProfileTypeFromMetadataAsync(createDto.Metadata);
        var profileType = await _profileTypeRepository.GetByIdAsync(profileTypeId);
        if (profileType == null)
            return null;

        // Metadata validation is now handled in ValidateProfileCreationAsync

        // Determine visibility level (prioritize VisibilityLevel over IsPublic)
        var visibilityLevel = createDto.VisibilityLevel ?? 
            (createDto.IsPublic ? VisibilityLevel.Public : VisibilityLevel.Private);

        // Create profile (supports any profile type, not just personal)
        var profile = new Profile
        {
            UserId = user.Id,
            ProfileTypeId = profileTypeId, // Dynamically determined from metadata
            DisplayName = createDto.DisplayName,
            Bio = createDto.Bio,
            Avatar = createDto.Avatar,
            Location = createDto.Location,
            VisibilityLevel = visibilityLevel,
            IsActive = false, // Will be set active explicitly
            Tags = createDto.Tags ?? new List<string>(),
            Metadata = createDto.Metadata ?? "{}"
        };

        // Set social media links if provided
        if (createDto.SocialMediaLinks?.Any() == true)
        {
            profile.SetSocialMediaLinks(createDto.SocialMediaLinks);
        }

        await _profileRepository.AddAsync(profile);
        await _profileRepository.SaveChangesAsync();

        // If this is the user's first profile, automatically set it as active
        var userProfiles = await _profileRepository.GetProfilesByUserIdAsync(user.Id, includeInactive: true);
        _logger.LogInformation("[CreateProfileAsync] User {UserId} has {Count} profiles total", user.Id, userProfiles.Count());
        if (userProfiles.Count() == 1)
        {
            _logger.LogInformation("[CreateProfileAsync] This is first profile! Calling EnforceOneActiveProfileRuleAsync({ProfileId}, {UserId})", profile.Id, user.Id);
            var enforceResult = await EnforceOneActiveProfileRuleAsync(profile.Id, user.Id);
            _logger.LogInformation("[CreateProfileAsync] EnforceOneActiveProfileRuleAsync returned: {Result}", enforceResult);
        }
        else
        {
            _logger.LogWarning("[CreateProfileAsync] ❌ NOT calling EnforceOneActiveProfileRuleAsync because profile count != 1. Count={Count}", userProfiles.Count());
        }

        // Load the profile with related data
        var createdProfile = await _profileRepository.GetWithRelatedDataAsync(profile.Id);
        return createdProfile != null ? await MapToProfileDtoAsync(createdProfile) : null;
    }

    /// <summary>
    /// Updates a specific profile by ID (with ownership validation)
    /// </summary>
    public async Task<ProfileDto?> UpdateProfileAsync(Guid profileId, UpdateProfileDto updateDto, string userKeycloakId)
    {
        if (updateDto == null || string.IsNullOrWhiteSpace(userKeycloakId))
            return null;

        // Get user
        var user = await _userRepository.GetByKeycloakIdAsync(userKeycloakId);
        if (user == null)
            return null;

        // Get profile and verify ownership
        var profile = await _profileRepository.GetByIdAsync(profileId);
        if (profile == null || profile.UserId != user.Id)
            return null; // Profile not found or user doesn't own it

        // Check for duplicate display names within user's profiles (excluding the current profile)
        var existingProfiles = await _profileRepository.GetProfilesByUserIdAsync(user.Id, includeInactive: true);
        if (existingProfiles.Any(p => p.Id != profileId && 
                                      p.DisplayName.Equals(updateDto.DisplayName, StringComparison.OrdinalIgnoreCase)))
        {
            return null; // A profile with this display name already exists for this user
        }

        // Get profile type for metadata validation
        var profileType = await _profileTypeRepository.GetByIdAsync(profile.ProfileTypeId);
        if (profileType == null)
            return null;

        // Validate metadata if provided
        if (!string.IsNullOrWhiteSpace(updateDto.Metadata))
        {
            var metadataValidation = await _metadataValidator.ValidateMetadataAsync(updateDto.Metadata, profileType);
            if (!metadataValidation.IsValid)
                return null; // Could add specific error handling here
        }

        // Determine visibility level (prioritize VisibilityLevel over IsPublic)
        var visibilityLevel = updateDto.VisibilityLevel ?? 
            (updateDto.IsPublic ? VisibilityLevel.Public : VisibilityLevel.Private);

        // Update profile
        profile.DisplayName = updateDto.DisplayName;
        profile.Bio = updateDto.Bio;
        profile.Avatar = updateDto.Avatar;
        profile.Location = updateDto.Location;
        profile.VisibilityLevel = visibilityLevel;
        profile.Tags = updateDto.Tags ?? new List<string>();
        profile.Metadata = updateDto.Metadata ?? "{}";

        // Update social media links if provided
        if (updateDto.SocialMediaLinks?.Any() == true)
        {
            profile.SetSocialMediaLinks(updateDto.SocialMediaLinks);
        }

        await _profileRepository.UpdateAsync(profile);
        await _profileRepository.SaveChangesAsync();

        // Load updated profile with related data
        var updatedProfile = await _profileRepository.GetWithRelatedDataAsync(profile.Id);
        return updatedProfile != null ? await MapToProfileDtoAsync(updatedProfile) : null;
    }

    /// <summary>
    /// Deletes a specific profile by ID (with ownership validation)
    /// </summary>
    public async Task<bool> DeleteProfileAsync(Guid profileId, string userKeycloakId)
    {
        if (string.IsNullOrWhiteSpace(userKeycloakId))
            return false;

        // Get user
        var user = await _userRepository.GetByKeycloakIdAsync(userKeycloakId);
        if (user == null)
            return false;

        // Get profile and verify ownership
        var profile = await _profileRepository.GetByIdAsync(profileId);
        if (profile == null || profile.UserId != user.Id)
            return false; // Profile not found or user doesn't own it

        var result = await _profileRepository.DeleteAsync(profileId);
        if (result)
            await _profileRepository.SaveChangesAsync();

        return result;
    }

    /// <summary>
    /// Searches profiles by tags
    /// </summary>
    public async Task<PagedResult<ProfileSummaryDto>> SearchProfilesByTagsAsync(string[] tags, bool matchAll = false, int page = 1, int pageSize = 20)
    {
        if (tags == null || tags.Length == 0)
        {
            return new PagedResult<ProfileSummaryDto>
            {
                Items = new List<ProfileSummaryDto>(),
                Page = page,
                PageSize = pageSize,
                TotalItems = 0
            };
        }

        var (profiles, totalCount) = await _profileRepository.SearchProfilesByTagsAsync(tags, matchAll, page, pageSize);
        var summaries = profiles.Select(MapToProfileSummaryDto).ToList();

        return new PagedResult<ProfileSummaryDto>
        {
            Items = summaries,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalCount
        };
    }

    /// <summary>
    /// Gets popular profiles by view count
    /// </summary>
    public async Task<IEnumerable<ProfileSummaryDto>> GetPopularProfilesAsync(int count = 10)
    {
        var profiles = await _profileRepository.GetPopularProfilesAsync(count);
        return profiles.Select(MapToProfileSummaryDto);
    }

    /// <summary>
    /// Gets user profile usage statistics
    /// </summary>
    public async Task<UserProfileUsageDto> GetUserProfileUsageAsync(string userKeycloakId)
    {
        if (string.IsNullOrWhiteSpace(userKeycloakId))
        {
            return new UserProfileUsageDto();
        }

        // Get user
        var user = await _userRepository.GetByKeycloakIdAsync(userKeycloakId);
        if (user == null)
        {
            return new UserProfileUsageDto();
        }

        var stats = await _profileRepository.GetUserProfileUsageAsync(user.Id);

        return new UserProfileUsageDto
        {
            TotalProfiles = stats.TotalProfiles,
            ActiveProfiles = stats.ActiveProfiles,
            PublicProfiles = stats.PublicProfiles,
            MostRecentUpdate = stats.MostRecentUpdate
        };
    }

    /// <summary>
    /// Maps Profile entity to ProfileDto
    /// </summary>
    private Task<ProfileDto> MapToProfileDtoAsync(Profile profile)
    {
        ProfileTypeDto? profileTypeDto = null;
        if (profile.ProfileType != null)
        {
            profileTypeDto = new ProfileTypeDto
            {
                Id = profile.ProfileType.Id,
                Name = profile.ProfileType.Name,
                DisplayName = profile.ProfileType.DisplayName,
                Description = profile.ProfileType.Description,
                IsActive = profile.ProfileType.IsActive,
                SortOrder = profile.ProfileType.SortOrder,
                FeatureFlags = profile.ProfileType.FeatureFlags,
                CreatedAt = profile.ProfileType.CreatedAt,
                UpdatedAt = profile.ProfileType.UpdatedAt
            };
        }

        return Task.FromResult(new ProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            ProfileTypeId = profile.ProfileTypeId,
            ProfileType = profileTypeDto,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            Avatar = profile.Avatar,
            Location = profile.Location,
            LocationDisplay = profile.LocationDisplay,
            IsActive = profile.IsActive,
            IsPublic = profile.VisibilityLevel == VisibilityLevel.Public,
            VisibilityLevel = profile.VisibilityLevel,
            ViewCount = profile.ViewCount,
            Tags = profile.Tags,
            SocialMediaLinks = profile.GetSocialMediaLinks(),
            Metadata = profile.Metadata,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        });
    }

    /// <summary>
    /// Maps Profile entity to ProfileSummaryDto
    /// </summary>
    private static ProfileSummaryDto MapToProfileSummaryDto(Profile profile)
    {
        var bioPreview = string.IsNullOrEmpty(profile.Bio) 
            ? string.Empty 
            : profile.Bio.Length <= 100 
                ? profile.Bio 
                : profile.Bio.Substring(0, 97) + "...";

        return new ProfileSummaryDto
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            BioPreview = bioPreview,
            Avatar = profile.Avatar,
            LocationDisplay = profile.LocationDisplay,
            ProfileType = profile.ProfileType?.DisplayName ?? "Unknown",
            IsActive = profile.IsActive,
            ViewCount = profile.ViewCount,
            CreatedAt = profile.CreatedAt
        };
    }

    /// <summary>
    /// Determines the appropriate profile type based on metadata content
    /// </summary>
    private async Task<Guid> DetermineProfileTypeFromMetadataAsync(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return await GetDefaultProfileTypeIdAsync();

        try
        {
            using var document = JsonDocument.Parse(metadata);
            var root = document.RootElement;

            // Organization profile indicators (check first to avoid conflicts)
            if (root.TryGetProperty("organizationType", out _) ||
                root.TryGetProperty("missionStatement", out _) ||
                root.TryGetProperty("foundingDate", out _) ||
                root.TryGetProperty("memberCount", out _) ||
                root.TryGetProperty("chapters", out _))
            {
                var orgTypeId = await GetProfileTypeIdByNameAsync("OrganizationProfile");
                return orgTypeId ?? await GetDefaultProfileTypeIdAsync();
            }

            // Business profile indicators  
            if (root.TryGetProperty("industry", out _) ||
                root.TryGetProperty("companySize", out _) ||
                root.TryGetProperty("foundedYear", out _) ||
                root.TryGetProperty("businessType", out _) ||
                root.TryGetProperty("revenue", out _) ||
                root.TryGetProperty("employees", out _))
            {
                var businessTypeId = await GetProfileTypeIdByNameAsync("BusinessProfile");
                return businessTypeId ?? await GetDefaultProfileTypeIdAsync();
            }

            // Default to personal profile
            return await GetDefaultProfileTypeIdAsync();
        }
        catch (JsonException)
        {
            // Invalid JSON - default to personal
            return await GetDefaultProfileTypeIdAsync();
        }
    }

    /// <summary>
    /// Gets metadata template for a specific profile type
    /// </summary>
    public async Task<object?> GetMetadataTemplateAsync(Guid profileTypeId)
    {
        try
        {
            // Check if profile type exists
            var profileType = await _profileTypeRepository.GetByIdAsync(profileTypeId);
            if (profileType == null)
                return null;

            // Return template based on profile type name
            return profileType.Name switch
            {
                "PersonalProfile" => new
                {
                    interests = new string[] { },
                    skills = new string[] { },
                    hobbies = new string[] { },
                    education = new
                    {
                        degree = "",
                        institution = "",
                        graduationYear = 0
                    },
                    experience = new
                    {
                        position = "",
                        company = "",
                        years = 0
                    }
                },
                "BusinessProfile" => new
                {
                    industry = "",
                    companySize = "",
                    foundedYear = 0,
                    businessType = "",
                    revenue = "",
                    employees = 0,
                    services = new string[] { },
                    markets = new string[] { }
                },
                "OrganizationProfile" => new
                {
                    organizationType = "",
                    missionStatement = "",
                    foundingDate = "",
                    memberCount = 0,
                    chapters = new string[] { },
                    programs = new string[] { },
                    focus_areas = new string[] { }
                },
                _ => new { } // Generic template for unknown profile types
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Uploads an avatar image for a profile
    /// </summary>
    public async Task<string?> UploadAvatarAsync(Guid profileId, string keycloakId, Stream fileStream, string fileName, string contentType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keycloakId) || fileStream == null || string.IsNullOrWhiteSpace(fileName))
                return null;

            // Get user and verify profile ownership
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            if (user == null)
                return null;

            var profile = await _profileRepository.GetByIdAsync(profileId);
            if (profile == null || profile.UserId != user.Id)
                return null;

            // Validate file type (images only)
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(contentType?.ToLowerInvariant()))
                return null;

            // Validate file size (max 5MB)
            if (fileStream.Length > 5 * 1024 * 1024)
                return null;

            // Create file upload request
            var uploadRequest = new FileUploadRequest
            {
                FileStream = fileStream,
                FileName = fileName,
                ContentType = contentType ?? "application/octet-stream",
                Container = "profile-avatars",
                Metadata = new Dictionary<string, string>
                {
                    ["ProfileId"] = profileId.ToString(),
                    ["UserId"] = user.Id.ToString(),
                    ["UploadType"] = "Avatar"
                }
            };

            // Upload the file
            var uploadResult = await _fileStorageService.UploadFileAsync(uploadRequest);
            if (uploadResult == null || string.IsNullOrWhiteSpace(uploadResult.FileId))
                return null;

            // Delete old avatar if exists
            if (!string.IsNullOrWhiteSpace(profile.AvatarFileId))
            {
                await _fileStorageService.DeleteFileAsync(profile.AvatarFileId);
            }

            // Update profile with new avatar file ID and URL
            profile.AvatarFileId = uploadResult.FileId;
            profile.Avatar = uploadResult.Url;

            await _profileRepository.UpdateAsync(profile);
            await _profileRepository.SaveChangesAsync();

            return uploadResult.FileId;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Deletes the avatar image for a profile
    /// </summary>
    public async Task<bool> DeleteAvatarAsync(Guid profileId, string keycloakId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keycloakId))
                return false;

            // Get user and verify profile ownership
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            if (user == null)
                return false;

            var profile = await _profileRepository.GetByIdAsync(profileId);
            if (profile == null || profile.UserId != user.Id)
                return false;

            // Check if profile has an avatar to delete
            if (string.IsNullOrWhiteSpace(profile.AvatarFileId))
                return true; // No avatar to delete, consider it success

            // Delete the file from storage
            var deleteSuccess = await _fileStorageService.DeleteFileAsync(profile.AvatarFileId);

            // Clear avatar fields regardless of storage delete result (in case file was already gone)
            profile.AvatarFileId = null;
            profile.Avatar = string.Empty;

            await _profileRepository.UpdateAsync(profile);
            await _profileRepository.SaveChangesAsync();

            return deleteSuccess;
        }
        catch (Exception)
        {
            return false;
        }
    }
}