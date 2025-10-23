using System.Text.Json;

using Sivar.Os.Shared.Configuration;
using Sivar.Os.Shared.DTOs;
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

    public ProfileService(
        IProfileRepository profileRepository, 
        IUserRepository userRepository,
        IProfileTypeRepository profileTypeRepository,
        IProfileMetadataValidator metadataValidator,
        IFileStorageService fileStorageService)
    {
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _profileTypeRepository = profileTypeRepository ?? throw new ArgumentNullException(nameof(profileTypeRepository));
        _metadataValidator = metadataValidator ?? throw new ArgumentNullException(nameof(metadataValidator));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
    }

    /// <summary>
    /// Gets the current user's personal profile
    /// </summary>
    public async Task<ProfileDto?> GetMyProfileAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return null;

        var profiles = await _profileRepository.GetProfilesByKeycloakIdAsync(keycloakId);
        var personalProfile = profiles.FirstOrDefault(p => p.ProfileTypeId == SeedData.PersonalProfileTypeId);

        return personalProfile != null ? await MapToProfileDtoAsync(personalProfile) : null;
    }

    /// <summary>
    /// Creates a personal profile for the current user
    /// </summary>
    public async Task<ProfileDto?> CreateMyProfileAsync(string keycloakId, CreateProfileDto createDto)
    {
        if (string.IsNullOrWhiteSpace(keycloakId) || createDto == null)
            return null;

        // Validate profile data
        var validation = await ValidateProfileDataAsync(createDto);
        if (!validation.IsValid)
            return null;

        // Get user
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
            return null;

        // Check if user already has a personal profile
        if (await UserHasPersonalProfileAsync(keycloakId))
            return null; // User already has a personal profile

        // Create profile
        var profile = new Profile
        {
            UserId = user.Id,
            ProfileTypeId = SeedData.PersonalProfileTypeId,
            DisplayName = createDto.DisplayName,
            Bio = createDto.Bio,
            Avatar = createDto.Avatar,
            Location = createDto.Location,
            VisibilityLevel = createDto.IsPublic ? VisibilityLevel.Public : VisibilityLevel.Private,
            IsActive = true // Set as active since it's likely their only profile
        };

        await _profileRepository.AddAsync(profile);
        await _profileRepository.SaveChangesAsync();

        // Load the profile with related data
        var createdProfile = await _profileRepository.GetWithRelatedDataAsync(profile.Id);
        return createdProfile != null ? await MapToProfileDtoAsync(createdProfile) : null;
    }

    /// <summary>
    /// Updates the current user's personal profile
    /// </summary>
    public async Task<ProfileDto?> UpdateMyProfileAsync(string keycloakId, UpdateProfileDto updateDto)
    {
        if (string.IsNullOrWhiteSpace(keycloakId) || updateDto == null)
            return null;

        // Get user's personal profile
        var profiles = await _profileRepository.GetProfilesByKeycloakIdAsync(keycloakId);
        var personalProfile = profiles.FirstOrDefault(p => p.ProfileTypeId == SeedData.PersonalProfileTypeId);

        if (personalProfile == null)
            return null;

        // Get user
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
            return null;

        // Check for duplicate display names within user's profiles (excluding the current profile)
        var existingProfiles = await _profileRepository.GetProfilesByUserIdAsync(user.Id, includeInactive: true);
        if (existingProfiles.Any(p => p.Id != personalProfile.Id && 
                                      p.DisplayName.Equals(updateDto.DisplayName, StringComparison.OrdinalIgnoreCase)))
        {
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

        // Load updated profile with related data
        var updatedProfile = await _profileRepository.GetWithRelatedDataAsync(personalProfile.Id);
        return updatedProfile != null ? await MapToProfileDtoAsync(updatedProfile) : null;
    }

    /// <summary>
    /// Deletes the current user's personal profile
    /// </summary>
    public async Task<bool> DeleteMyProfileAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        var profiles = await _profileRepository.GetProfilesByKeycloakIdAsync(keycloakId);
        var personalProfile = profiles.FirstOrDefault(p => p.ProfileTypeId == SeedData.PersonalProfileTypeId);

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
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        // Enhanced validation with business rules
        var validation = await ValidateActiveProfileSwitchAsync(profileId, keycloakId);
        if (!validation.IsValid)
            return false;

        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
            return false;

        // Enforce one active profile rule
        return await EnforceOneActiveProfileRuleAsync(profileId, user.Id);
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

        return await _profileRepository.UserHasProfileOfTypeAsync(user.Id, SeedData.PersonalProfileTypeId);
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
        var targetProfileTypeId = profileTypeId ?? DetermineProfileTypeFromMetadata(profileData.Metadata);

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
            // Get all user's profiles
            var userProfiles = await _profileRepository.GetProfilesByUserIdAsync(userId, includeInactive: true);
            
            // Deactivate all profiles first
            foreach (var profile in userProfiles)
            {
                if (profile.Id != newActiveProfileId && profile.IsActive)
                {
                    profile.IsActive = false;
                    await _profileRepository.UpdateAsync(profile);
                }
            }

            // Activate the target profile
            var targetProfile = userProfiles.FirstOrDefault(p => p.Id == newActiveProfileId);
            if (targetProfile != null)
            {
                targetProfile.IsActive = true;
                await _profileRepository.UpdateAsync(targetProfile);
            }

            await _profileRepository.SaveChangesAsync();
            return true;
        }
        catch
        {
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
        var profileTypeId = DetermineProfileTypeFromMetadata(createDto.Metadata);
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
        if (userProfiles.Count() == 1)
        {
            await EnforceOneActiveProfileRuleAsync(profile.Id, user.Id);
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
    private static Guid DetermineProfileTypeFromMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return SeedData.PersonalProfileTypeId;

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
                return SeedData.OrganizationProfileTypeId;
            }

            // Business profile indicators  
            if (root.TryGetProperty("industry", out _) ||
                root.TryGetProperty("companySize", out _) ||
                root.TryGetProperty("foundedYear", out _) ||
                root.TryGetProperty("businessType", out _) ||
                root.TryGetProperty("revenue", out _) ||
                root.TryGetProperty("employees", out _))
            {
                return SeedData.BusinessProfileTypeId;
            }

            // Default to personal profile
            return SeedData.PersonalProfileTypeId;
        }
        catch (JsonException)
        {
            // Invalid JSON - default to personal
            return SeedData.PersonalProfileTypeId;
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

            // Return template based on profile type
            if (profileTypeId == SeedData.PersonalProfileTypeId)
            {
                return new
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
                };
            }
            else if (profileTypeId == SeedData.BusinessProfileTypeId)
            {
                return new
                {
                    industry = "",
                    companySize = "",
                    foundedYear = 0,
                    businessType = "",
                    revenue = "",
                    employees = 0,
                    services = new string[] { },
                    markets = new string[] { }
                };
            }
            else if (profileTypeId == SeedData.OrganizationProfileTypeId)
            {
                return new
                {
                    organizationType = "",
                    missionStatement = "",
                    foundingDate = "",
                    memberCount = 0,
                    chapters = new string[] { },
                    programs = new string[] { },
                    focus_areas = new string[] { }
                };
            }

            // Generic template for unknown profile types
            return new { };
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