
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for Profile entity operations
/// </summary>
public interface IProfileRepository : IBaseRepository<Profile>
{
    /// <summary>
    /// Gets all profiles for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeInactive">Whether to include inactive profiles</param>
    /// <returns>Collection of user's profiles</returns>
    Task<IEnumerable<Profile>> GetProfilesByUserIdAsync(Guid userId, bool includeInactive = false);

    /// <summary>
    /// Gets all profiles for a user by their Keycloak ID
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>Collection of user's profiles</returns>
    Task<IEnumerable<Profile>> GetProfilesByKeycloakIdAsync(string keycloakId);

    /// <summary>
    /// Gets the active profile for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Active profile if found, null otherwise</returns>
    Task<Profile?> GetActiveProfileByUserIdAsync(Guid userId);

    /// <summary>
    /// Gets the active profile for a user by their Keycloak ID
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <returns>Active profile if found, null otherwise</returns>
    Task<Profile?> GetActiveProfileByKeycloakIdAsync(string keycloakId);

    /// <summary>
    /// Gets a profile by its unique handle (URL-friendly identifier)
    /// </summary>
    /// <param name="handle">URL-friendly handle (e.g., "jose-ojeda")</param>
    /// <returns>Matching profile if found, null otherwise</returns>
    Task<Profile?> GetByHandleAsync(string handle);

    /// <summary>
    /// Gets profiles by profile type
    /// </summary>
    /// <param name="profileTypeId">Profile type ID</param>
    /// <returns>Collection of profiles of the specified type</returns>
    Task<IEnumerable<Profile>> GetProfilesByTypeAsync(Guid profileTypeId);

    /// <summary>
    /// Gets public profiles with pagination
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of profiles per page</param>
    /// <returns>Collection of public profiles</returns>
    Task<(IEnumerable<Profile> Profiles, int TotalCount)> GetPublicProfilesAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Sets a profile as active for a user (deactivates others)
    /// </summary>
    /// <param name="profileId">Profile ID to activate</param>
    /// <param name="userId">User ID (for verification)</param>
    /// <returns>True if set as active, false if not found or not owned by user</returns>
    Task<bool> SetAsActiveProfileAsync(Guid profileId, Guid userId);

    /// <summary>
    /// Gets profile with related entities (User, ProfileType)
    /// </summary>
    /// <param name="id">Profile ID</param>
    /// <returns>Profile with related data if found, null otherwise</returns>
    Task<Profile?> GetWithRelatedDataAsync(Guid id);

    /// <summary>
    /// Increments the view count for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <returns>True if updated, false if not found</returns>
    Task<bool> IncrementViewCountAsync(Guid profileId);

    /// <summary>
    /// Searches profiles by display name or bio content
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of profiles per page</param>
    /// <returns>Collection of matching profiles</returns>
    Task<(IEnumerable<Profile> Profiles, int TotalCount)> SearchProfilesAsync(string searchTerm, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets profiles by location (city, state, or country)
    /// </summary>
    /// <param name="locationQuery">Location search term</param>
    /// <returns>Collection of profiles matching the location</returns>
    Task<IEnumerable<Profile>> GetProfilesByLocationAsync(string locationQuery);

    /// <summary>
    /// Checks if a user already has a profile of a specific type
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="profileTypeId">Profile type ID</param>
    /// <returns>True if user has a profile of this type, false otherwise</returns>
    Task<bool> UserHasProfileOfTypeAsync(Guid userId, Guid profileTypeId);

    /// <summary>
    /// Gets the most recently created profiles
    /// </summary>
    /// <param name="count">Number of profiles to retrieve</param>
    /// <returns>Collection of recent profiles</returns>
    Task<IEnumerable<Profile>> GetRecentProfilesAsync(int count = 10);

    /// <summary>
    /// Gets profile statistics
    /// </summary>
    /// <returns>Profile statistics tuple</returns>
    Task<(int TotalProfiles, int PublicProfiles, int ActiveProfiles, int NewProfilesLast30Days, double AverageViewCount)> GetProfileStatisticsAsync();

    /// <summary>
    /// Searches profiles by tags
    /// </summary>
    /// <param name="tags">Array of tags to search for</param>
    /// <param name="matchAll">Whether profile must contain all tags (true) or any tags (false)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of profiles per page</param>
    /// <returns>Collection of profiles with matching tags</returns>
    Task<(IEnumerable<Profile> Profiles, int TotalCount)> SearchProfilesByTagsAsync(string[] tags, bool matchAll = false, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets profiles by multiple criteria for advanced filtering
    /// </summary>
    /// <param name="profileTypeId">Optional profile type filter</param>
    /// <param name="visibilityLevel">Optional visibility level filter</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="locationQuery">Optional location search term</param>
    /// <param name="createdAfter">Optional date filter for creation</param>
    /// <param name="minViewCount">Optional minimum view count filter</param>
    /// <returns>Collection of profiles matching criteria</returns>
    Task<IEnumerable<Profile>> GetProfilesByCriteriaAsync(
        Guid? profileTypeId = null,
        VisibilityLevel? visibilityLevel = null,
        bool? isActive = null,
        string? locationQuery = null,
        DateTime? createdAfter = null,
        int? minViewCount = null);

    /// <summary>
    /// Gets the most popular profiles by view count
    /// </summary>
    /// <param name="count">Number of popular profiles to retrieve</param>
    /// <returns>Collection of popular profiles ordered by view count</returns>
    Task<IEnumerable<Profile>> GetPopularProfilesAsync(int count = 10);

    /// <summary>
    /// Gets user profile usage statistics
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User profile statistics</returns>
    Task<(int TotalProfiles, int ActiveProfiles, int PublicProfiles, DateTime? MostRecentUpdate)> GetUserProfileUsageAsync(Guid userId);
}