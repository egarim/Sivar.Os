using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for Profile entity operations
/// </summary>
public class ProfileRepository : BaseRepository<Profile>, IProfileRepository
{
    public ProfileRepository(SivarDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets all profiles for a specific user
    /// </summary>
    public async Task<IEnumerable<Profile>> GetProfilesByUserIdAsync(Guid userId, bool includeInactive = false)
    {
        var query = _dbSet
            .Include(p => p.ProfileType)
            .Where(p => p.UserId == userId);

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all profiles for a user by their Keycloak ID
    /// </summary>
    public async Task<IEnumerable<Profile>> GetProfilesByKeycloakIdAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return new List<Profile>();

        return await _dbSet
            .Include(p => p.ProfileType)
            .Include(p => p.User)
            .Where(p => p.User.KeycloakId == keycloakId)
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the active profile for a specific user
    /// </summary>
    public async Task<Profile?> GetActiveProfileByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(p => p.ProfileType)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive);
    }

    /// <summary>
    /// Gets the active profile for a user by their Keycloak ID
    /// </summary>
    public async Task<Profile?> GetActiveProfileByKeycloakIdAsync(string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return null;

        return await _dbSet
            .Include(p => p.ProfileType)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.User.KeycloakId == keycloakId && p.IsActive);
    }

    /// <summary>
    /// Gets a profile by its unique handle (URL-friendly identifier)
    /// Returns the profile regardless of visibility level - visibility check should be done by the caller
    /// </summary>
    public async Task<Profile?> GetByHandleAsync(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return null;

        // Search for profiles with matching Handle (case-insensitive for safety)
        // Visibility check is done by the service layer to allow owners to view their own profiles
        return await _dbSet
            .Include(p => p.User)
            .Include(p => p.ProfileType)
            .FirstOrDefaultAsync(p => p.Handle.ToLower() == handle.ToLower());
    }

    /// <summary>
    /// Checks if a handle already exists in the database
    /// </summary>
    public async Task<bool> HandleExistsAsync(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return false;

        // Check if any profile (public or private) has this handle
        return await _dbSet
            .AnyAsync(p => p.Handle.ToLower() == handle.ToLower());
    }

    /// <summary>
    /// Gets profiles by profile type
    /// </summary>
    public async Task<IEnumerable<Profile>> GetProfilesByTypeAsync(Guid profileTypeId)
    {
        return await _dbSet
            .Include(p => p.User)
            .Include(p => p.ProfileType)
            .Where(p => p.ProfileTypeId == profileTypeId && p.VisibilityLevel == VisibilityLevel.Public)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets public profiles with pagination
    /// </summary>
    public async Task<(IEnumerable<Profile> Profiles, int TotalCount)> GetPublicProfilesAsync(int page = 1, int pageSize = 20)
    {
        var query = _dbSet
            .Include(p => p.User)
            .Include(p => p.ProfileType)
            .Where(p => p.VisibilityLevel == VisibilityLevel.Public);

        var totalCount = await query.CountAsync();

        var profiles = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (profiles, totalCount);
    }

    /// <summary>
    /// Sets a profile as active for a user (deactivates others)
    /// </summary>
    public async Task<bool> SetAsActiveProfileAsync(Guid profileId, Guid userId)
    {
        // First, verify the profile belongs to the user
        var profileToActivate = await _dbSet
            .FirstOrDefaultAsync(p => p.Id == profileId && p.UserId == userId);

        if (profileToActivate == null)
            return false;

        // Deactivate all other profiles for this user
        var userProfiles = await _dbSet
            .Where(p => p.UserId == userId && p.Id != profileId)
            .ToListAsync();

        foreach (var profile in userProfiles)
        {
            profile.SetAsInactive();
        }

        // Activate the selected profile
        profileToActivate.SetAsActive();

        _dbSet.UpdateRange(userProfiles);
        _dbSet.Update(profileToActivate);

        return true;
    }

    /// <summary>
    /// Gets profile with related entities (User, ProfileType)
    /// </summary>
    public async Task<Profile?> GetWithRelatedDataAsync(Guid id)
    {
        return await _dbSet
            .Include(p => p.User)
            .Include(p => p.ProfileType)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Increments the view count for a profile
    /// </summary>
    public async Task<bool> IncrementViewCountAsync(Guid profileId)
    {
        var profile = await GetByIdAsync(profileId);
        if (profile == null || profile.VisibilityLevel != VisibilityLevel.Public)
            return false;

        profile.IncrementViewCount();
        _dbSet.Update(profile);

        return true;
    }

    /// <summary>
    /// Searches profiles by display name or bio content
    /// </summary>
    public async Task<(IEnumerable<Profile> Profiles, int TotalCount)> SearchProfilesAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        Console.WriteLine($"[ProfileRepository.SearchProfilesAsync] START - SearchTerm='{searchTerm}', Page={page}, PageSize={pageSize}");
        
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            Console.WriteLine($"[ProfileRepository.SearchProfilesAsync] Empty search term - falling back to GetPublicProfilesAsync");
            return await GetPublicProfilesAsync(page, pageSize);
        }

        var term = searchTerm.ToLower();
        Console.WriteLine($"[ProfileRepository.SearchProfilesAsync] Building query - SearchTerm='{term}'");

        var query = _dbSet
            .Include(p => p.User)
            .Include(p => p.ProfileType)
            .Where(p => p.VisibilityLevel == VisibilityLevel.Public && (
                p.DisplayName.ToLower().Contains(term) ||
                p.Bio.ToLower().Contains(term)
            ));

        var totalCount = await query.CountAsync();
        Console.WriteLine($"[ProfileRepository.SearchProfilesAsync] Total matching profiles: {totalCount}");

        var profiles = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        Console.WriteLine($"[ProfileRepository.SearchProfilesAsync] Retrieved {profiles.Count} profiles for page {page}");

        return (profiles, totalCount);
    }

    /// <summary>
    /// Gets profiles by location (city, state, or country)
    /// </summary>
    public async Task<IEnumerable<Profile>> GetProfilesByLocationAsync(string locationQuery)
    {
        if (string.IsNullOrWhiteSpace(locationQuery))
            return new List<Profile>();

        var term = locationQuery.ToLower();

        return await _dbSet
            .Include(p => p.User)
            .Include(p => p.ProfileType)
            .Where(p => p.VisibilityLevel == VisibilityLevel.Public && p.Location != null && (
                p.Location.City.ToLower().Contains(term) ||
                p.Location.State.ToLower().Contains(term) ||
                p.Location.Country.ToLower().Contains(term)
            ))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Checks if a user already has a profile of a specific type
    /// </summary>
    public async Task<bool> UserHasProfileOfTypeAsync(Guid userId, Guid profileTypeId)
    {
        return await _dbSet
            .AnyAsync(p => p.UserId == userId && p.ProfileTypeId == profileTypeId);
    }

    /// <summary>
    /// Gets the most recently created profiles
    /// </summary>
    public async Task<IEnumerable<Profile>> GetRecentProfilesAsync(int count = 10)
    {
        return await _dbSet
            .Include(p => p.User)
            .Include(p => p.ProfileType)
            .Where(p => p.VisibilityLevel == VisibilityLevel.Public)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Gets profile statistics
    /// </summary>
    public async Task<(int TotalProfiles, int PublicProfiles, int ActiveProfiles, int NewProfilesLast30Days, double AverageViewCount)> GetProfileStatisticsAsync()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var totalProfiles = await _dbSet.CountAsync();
        var publicProfiles = await _dbSet.CountAsync(p => p.VisibilityLevel == VisibilityLevel.Public);
        var activeProfiles = await _dbSet.CountAsync(p => p.IsActive);
        var newProfilesLast30Days = await _dbSet.CountAsync(p => p.CreatedAt >= thirtyDaysAgo);
        var averageViewCount = await _dbSet.AverageAsync(p => (double)p.ViewCount);

        return (totalProfiles, publicProfiles, activeProfiles, newProfilesLast30Days, averageViewCount);
    }

    /// <summary>
    /// Gets profiles by multiple criteria for advanced filtering
    /// </summary>
    public async Task<IEnumerable<Profile>> GetProfilesByCriteriaAsync(
        Guid? profileTypeId = null,
        VisibilityLevel? visibilityLevel = null,
        bool? isActive = null,
        string? locationQuery = null,
        DateTime? createdAfter = null,
        int? minViewCount = null)
    {
        var query = _dbSet
            .Include(p => p.User)
            .Include(p => p.ProfileType)
            .AsQueryable();

        if (profileTypeId.HasValue)
            query = query.Where(p => p.ProfileTypeId == profileTypeId.Value);

        if (visibilityLevel.HasValue)
            query = query.Where(p => p.VisibilityLevel == visibilityLevel.Value);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(locationQuery))
        {
            var term = locationQuery.ToLower();
            query = query.Where(p => p.Location != null && (
                p.Location.City.ToLower().Contains(term) ||
                p.Location.State.ToLower().Contains(term) ||
                p.Location.Country.ToLower().Contains(term)
            ));
        }

        if (createdAfter.HasValue)
            query = query.Where(p => p.CreatedAt >= createdAfter.Value);

        if (minViewCount.HasValue)
            query = query.Where(p => p.ViewCount >= minViewCount.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Searches profiles by tags
    /// </summary>
    public async Task<(IEnumerable<Profile> Profiles, int TotalCount)> SearchProfilesByTagsAsync(string[] tags, bool matchAll = false, int page = 1, int pageSize = 20)
    {
        if (tags == null || tags.Length == 0)
        {
            return (new List<Profile>(), 0);
        }

        // Clean up tags (trim and convert to lowercase for matching)
        var cleanTags = tags.Where(t => !string.IsNullOrWhiteSpace(t))
                           .Select(t => t.Trim().ToLower())
                           .ToArray();

        if (cleanTags.Length == 0)
        {
            return (new List<Profile>(), 0);
        }

        var query = _dbSet
            .Include(p => p.User)
            .Include(p => p.ProfileType)
            .Where(p => p.VisibilityLevel == VisibilityLevel.Public);

        if (matchAll)
        {
            // Profile must contain ALL specified tags
            foreach (var tag in cleanTags)
            {
                query = query.Where(p => p.Tags.Any(t => t.ToLower().Contains(tag)));
            }
        }
        else
        {
            // Profile must contain ANY of the specified tags
            query = query.Where(p => p.Tags.Any(t => cleanTags.Any(ct => t.ToLower().Contains(ct))));
        }

        var totalCount = await query.CountAsync();

        var profiles = await query
            .OrderByDescending(p => p.ViewCount) // Order by popularity first
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (profiles, totalCount);
    }

    /// <summary>
    /// Gets the most popular profiles by view count
    /// </summary>
    public async Task<IEnumerable<Profile>> GetPopularProfilesAsync(int count = 10)
    {
        return await _dbSet
            .Include(p => p.User)
            .Include(p => p.ProfileType)
            .Where(p => p.VisibilityLevel == VisibilityLevel.Public)
            .OrderByDescending(p => p.ViewCount)
            .ThenByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Gets user profile usage statistics
    /// </summary>
    public async Task<(int TotalProfiles, int ActiveProfiles, int PublicProfiles, DateTime? MostRecentUpdate)> GetUserProfileUsageAsync(Guid userId)
    {
        var profiles = await _dbSet
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return (
            TotalProfiles: profiles.Count,
            ActiveProfiles: profiles.Count(p => p.IsActive),
            PublicProfiles: profiles.Count(p => p.VisibilityLevel == VisibilityLevel.Public),
            MostRecentUpdate: profiles.Any() ? profiles.Max(p => p.UpdatedAt) : null
        );
    }
}