
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for Profile management (PersonalProfile focus)
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Gets the current user's personal profile
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>Profile DTO if found, null otherwise</returns>
    Task<ProfileDto?> GetMyProfileAsync(string keycloakId);

    /// <summary>
    /// Creates a personal profile for the current user
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <param name="createDto">Profile creation data</param>
    /// <returns>Created profile DTO if successful, null otherwise</returns>
    Task<ProfileDto?> CreateMyProfileAsync(string keycloakId, CreateProfileDto createDto);

    /// <summary>
    /// Updates the current user's personal profile
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <param name="updateDto">Profile update data</param>
    /// <returns>Updated profile DTO if successful, null otherwise</returns>
    Task<ProfileDto?> UpdateMyProfileAsync(string keycloakId, UpdateProfileDto updateDto);

    /// <summary>
    /// Deletes the current user's personal profile
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>True if deleted, false otherwise</returns>
    Task<bool> DeleteMyProfileAsync(string keycloakId);

    /// <summary>
    /// Gets all profiles for the current user
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>Collection of user's profile DTOs</returns>
    Task<IEnumerable<ProfileDto>> GetMyProfilesAsync(string keycloakId);

    /// <summary>
    /// Gets the current user's active profile
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>Active profile DTO if found, null otherwise</returns>
    Task<ProfileDto?> GetMyActiveProfileAsync(string keycloakId);

    /// <summary>
    /// Sets a profile as active for the current user
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <param name="profileId">Profile ID to set as active</param>
    /// <returns>True if set as active, false otherwise</returns>
    Task<bool> SetActiveProfileAsync(string keycloakId, Guid profileId);

    /// <summary>
    /// Gets a public profile by ID
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <returns>Profile DTO if found and public, null otherwise</returns>
    Task<ProfileDto?> GetPublicProfileAsync(Guid profileId);

    /// <summary>
    /// Gets public profiles with pagination
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of profiles per page</param>
    /// <returns>Paginated collection of public profile summaries</returns>
    Task<PagedResult<ProfileSummaryDto>> GetPublicProfilesAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Searches public profiles by display name or bio
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of profiles per page</param>
    /// <returns>Paginated collection of matching profile summaries</returns>
    Task<PagedResult<ProfileSummaryDto>> SearchProfilesAsync(string searchTerm, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets profiles by location
    /// </summary>
    /// <param name="locationQuery">Location search term</param>
    /// <returns>Collection of matching profile summaries</returns>
    Task<IEnumerable<ProfileSummaryDto>> GetProfilesByLocationAsync(string locationQuery);

    /// <summary>
    /// Gets the most recent public profiles
    /// </summary>
    /// <param name="count">Number of profiles to retrieve</param>
    /// <returns>Collection of recent profile summaries</returns>
    Task<IEnumerable<ProfileSummaryDto>> GetRecentProfilesAsync(int count = 10);

    /// <summary>
    /// Increments the view count for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <returns>True if updated, false otherwise</returns>
    Task<bool> IncrementProfileViewAsync(Guid profileId);

    /// <summary>
    /// Checks if the current user already has a personal profile
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>True if user has a personal profile, false otherwise</returns>
    Task<bool> UserHasPersonalProfileAsync(string keycloakId);

    /// <summary>
    /// Validates profile data before creation/update
    /// </summary>
    /// <param name="profileData">Profile data to validate</param>
    /// <returns>Validation result with errors if any</returns>
    Task<ValidationResult> ValidateProfileDataAsync(CreateProfileDto profileData);

    /// <summary>
    /// Enhanced profile validation for creation with business rules
    /// </summary>
    /// <param name="profileData">Profile data to validate</param>
    /// <param name="userKeycloakId">Keycloak user identifier</param>
    /// <param name="profileTypeId">Optional profile type ID</param>
    /// <returns>Validation result with business rule checks</returns>
    Task<ValidationResult> ValidateProfileCreationAsync(CreateProfileDto profileData, string userKeycloakId, Guid? profileTypeId = null);

    /// <summary>
    /// Validates active profile switching with business rules
    /// </summary>
    /// <param name="profileId">Profile ID to set as active</param>
    /// <param name="userKeycloakId">Keycloak user identifier</param>
    /// <returns>Validation result with business rule checks</returns>
    Task<ValidationResult> ValidateActiveProfileSwitchAsync(Guid profileId, string userKeycloakId);

    /// <summary>
    /// Enforces the one active profile per user business rule
    /// </summary>
    /// <param name="newActiveProfileId">Profile ID to set as active</param>
    /// <param name="userId">User ID</param>
    /// <returns>True if successfully enforced, false otherwise</returns>
    Task<bool> EnforceOneActiveProfileRuleAsync(Guid newActiveProfileId, Guid userId);

    /// <summary>
    /// Gets profile statistics (admin only)
    /// </summary>
    /// <returns>Profile statistics object</returns>
    Task<ProfileStatisticsDto> GetProfileStatisticsAsync();

    /// <summary>
    /// Creates a profile for a user (supports all profile types)
    /// </summary>
    /// <param name="createDto">Profile creation data</param>
    /// <param name="userKeycloakId">Keycloak user identifier</param>
    /// <param name="specifiedProfileTypeId">Optional ProfileTypeId to use directly instead of determining from metadata</param>
    /// <returns>Created profile DTO if successful, null otherwise</returns>
    Task<ProfileDto?> CreateProfileAsync(CreateProfileDto createDto, string userKeycloakId, Guid? specifiedProfileTypeId = null);

    /// <summary>
    /// Updates a specific profile by ID (with ownership validation)
    /// </summary>
    /// <param name="profileId">Profile ID to update</param>
    /// <param name="updateDto">Profile update data</param>
    /// <param name="userKeycloakId">Keycloak user identifier</param>
    /// <returns>Updated profile DTO if successful, null otherwise</returns>
    Task<ProfileDto?> UpdateProfileAsync(Guid profileId, UpdateProfileDto updateDto, string userKeycloakId);

    /// <summary>
    /// Deletes a specific profile by ID (with ownership validation)
    /// </summary>
    /// <param name="profileId">Profile ID to delete</param>
    /// <param name="userKeycloakId">Keycloak user identifier</param>
    /// <returns>True if deleted, false otherwise</returns>
    Task<bool> DeleteProfileAsync(Guid profileId, string userKeycloakId);

    /// <summary>
    /// Searches profiles by tags
    /// </summary>
    /// <param name="tags">Array of tags to search for</param>
    /// <param name="matchAll">Whether profile must contain all tags (true) or any tags (false)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of profiles per page</param>
    /// <returns>Paginated collection of matching profiles</returns>
    Task<PagedResult<ProfileSummaryDto>> SearchProfilesByTagsAsync(string[] tags, bool matchAll = false, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets popular profiles by view count
    /// </summary>
    /// <param name="count">Number of popular profiles to retrieve</param>
    /// <returns>Collection of popular profiles</returns>
    Task<IEnumerable<ProfileSummaryDto>> GetPopularProfilesAsync(int count = 10);

    /// <summary>
    /// Gets user profile usage statistics
    /// </summary>
    /// <param name="userKeycloakId">Keycloak user identifier</param>
    /// <returns>User profile usage statistics</returns>
    Task<UserProfileUsageDto> GetUserProfileUsageAsync(string userKeycloakId);

    /// <summary>
    /// Gets metadata template for a specific profile type
    /// </summary>
    /// <param name="profileTypeId">Profile type identifier</param>
    /// <returns>Metadata template as JSON object, null if not found</returns>
    Task<object?> GetMetadataTemplateAsync(Guid profileTypeId);

    /// <summary>
    /// Uploads an avatar image for a profile
    /// </summary>
    /// <param name="profileId">Profile identifier</param>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <param name="fileStream">Image file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">File content type</param>
    /// <returns>File ID if upload successful, null otherwise</returns>
    Task<string?> UploadAvatarAsync(Guid profileId, string keycloakId, Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// Deletes the avatar image for a profile
    /// </summary>
    /// <param name="profileId">Profile identifier</param>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>True if delete successful, false otherwise</returns>
    Task<bool> DeleteAvatarAsync(Guid profileId, string keycloakId);
}

/// <summary>
/// Generic paged result container
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Items in the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Validation result for profile operations
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates if the validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors
    /// </summary>
    /// <param name="errors">Validation error messages</param>
    public static ValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// DTO for profile statistics
/// </summary>
public class ProfileStatisticsDto
{
    /// <summary>
    /// Total number of profiles
    /// </summary>
    public int TotalProfiles { get; set; }

    /// <summary>
    /// Number of public profiles
    /// </summary>
    public int PublicProfiles { get; set; }

    /// <summary>
    /// Number of active profiles
    /// </summary>
    public int ActiveProfiles { get; set; }

    /// <summary>
    /// Number of profiles created in the last 30 days
    /// </summary>
    public int NewProfilesLast30Days { get; set; }

    /// <summary>
    /// Average profile view count
    /// </summary>
    public double AverageViewCount { get; set; }

    /// <summary>
    /// Profile statistics by type
    /// </summary>
    public Dictionary<string, int> ProfilesByType { get; set; } = new();
}

/// <summary>
/// DTO for user profile usage statistics
/// </summary>
public class UserProfileUsageDto
{
    /// <summary>
    /// Total number of profiles for the user
    /// </summary>
    public int TotalProfiles { get; set; }

    /// <summary>
    /// Number of active profiles for the user
    /// </summary>
    public int ActiveProfiles { get; set; }

    /// <summary>
    /// Number of public profiles for the user
    /// </summary>
    public int PublicProfiles { get; set; }

    /// <summary>
    /// Most recent profile update date
    /// </summary>
    public DateTime? MostRecentUpdate { get; set; }
}