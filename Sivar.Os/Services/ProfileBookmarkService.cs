using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service implementation for ProfileBookmark business logic
/// </summary>
public class ProfileBookmarkService : IProfileBookmarkService
{
    private readonly IProfileBookmarkRepository _bookmarkRepository;
    private readonly IProfileService _profileService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ProfileBookmarkService> _logger;

    public ProfileBookmarkService(
        IProfileBookmarkRepository bookmarkRepository,
        IProfileService profileService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProfileBookmarkService> logger)
    {
        _bookmarkRepository = bookmarkRepository ?? throw new ArgumentNullException(nameof(bookmarkRepository));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProfileBookmark>> GetMyBookmarksAsync(CancellationToken cancellationToken = default)
    {
        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == null)
        {
            _logger.LogWarning("GetMyBookmarksAsync: No active profile found for current user");
            return Enumerable.Empty<ProfileBookmark>();
        }

        return await _bookmarkRepository.GetByProfileIdAsync(profileId.Value, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Guid>> GetMyBookmarkedPostIdsAsync(CancellationToken cancellationToken = default)
    {
        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == null)
        {
            _logger.LogWarning("GetMyBookmarkedPostIdsAsync: No active profile found for current user");
            return Enumerable.Empty<Guid>();
        }

        return await _bookmarkRepository.GetBookmarkedPostIdsAsync(profileId.Value, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsBookmarkedAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == null)
        {
            return false;
        }

        return await _bookmarkRepository.IsBookmarkedAsync(profileId.Value, postId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ToggleBookmarkAsync(Guid postId, string? note = null, CancellationToken cancellationToken = default)
    {
        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == null)
        {
            _logger.LogWarning("ToggleBookmarkAsync: No active profile found for current user");
            return false;
        }

        var isBookmarked = await _bookmarkRepository.IsBookmarkedAsync(profileId.Value, postId, cancellationToken);
        
        if (isBookmarked)
        {
            var removed = await _bookmarkRepository.RemoveAsync(profileId.Value, postId, cancellationToken);
            _logger.LogInformation("Bookmark removed for profile {ProfileId}, post {PostId}, success: {Success}", 
                profileId.Value, postId, removed);
            return false; // Return new state: not bookmarked
        }
        else
        {
            var bookmark = new ProfileBookmark
            {
                ProfileId = profileId.Value,
                PostId = postId,
                Note = note
            };
            await _bookmarkRepository.AddAsync(bookmark, cancellationToken);
            _logger.LogInformation("Bookmark added for profile {ProfileId}, post {PostId}", profileId.Value, postId);
            return true; // Return new state: bookmarked
        }
    }

    /// <inheritdoc />
    public async Task<ProfileBookmark?> AddBookmarkAsync(Guid postId, string? note = null, CancellationToken cancellationToken = default)
    {
        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == null)
        {
            _logger.LogWarning("AddBookmarkAsync: No active profile found for current user");
            return null;
        }

        // Check if already bookmarked
        var existing = await _bookmarkRepository.GetByProfileAndPostAsync(profileId.Value, postId, cancellationToken);
        if (existing != null)
        {
            _logger.LogInformation("AddBookmarkAsync: Post {PostId} already bookmarked by profile {ProfileId}", 
                postId, profileId.Value);
            return existing;
        }

        var bookmark = new ProfileBookmark
        {
            ProfileId = profileId.Value,
            PostId = postId,
            Note = note
        };

        var result = await _bookmarkRepository.AddAsync(bookmark, cancellationToken);
        _logger.LogInformation("Bookmark created for profile {ProfileId}, post {PostId}", profileId.Value, postId);
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveBookmarkAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == null)
        {
            _logger.LogWarning("RemoveBookmarkAsync: No active profile found for current user");
            return false;
        }

        var result = await _bookmarkRepository.RemoveAsync(profileId.Value, postId, cancellationToken);
        _logger.LogInformation("Bookmark removed for profile {ProfileId}, post {PostId}, success: {Success}", 
            profileId.Value, postId, result);
        return result;
    }

    /// <inheritdoc />
    public async Task<ProfileBookmark?> UpdateNoteAsync(Guid postId, string? note, CancellationToken cancellationToken = default)
    {
        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == null)
        {
            _logger.LogWarning("UpdateNoteAsync: No active profile found for current user");
            return null;
        }

        return await _bookmarkRepository.UpdateNoteAsync(profileId.Value, postId, note, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<ProfileBookmark> Items, int TotalCount)> GetPaginatedAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var profileId = await GetCurrentProfileIdAsync(cancellationToken);
        if (profileId == null)
        {
            _logger.LogWarning("GetPaginatedAsync: No active profile found for current user");
            return (Enumerable.Empty<ProfileBookmark>(), 0);
        }

        return await _bookmarkRepository.GetPaginatedAsync(profileId.Value, page, pageSize, cancellationToken);
    }

    /// <summary>
    /// Gets the current user's active profile ID
    /// </summary>
    private async Task<Guid?> GetCurrentProfileIdAsync(CancellationToken cancellationToken = default)
    {
        var keycloakId = GetKeycloakIdFromContext();
        if (string.IsNullOrEmpty(keycloakId))
        {
            _logger.LogDebug("GetCurrentProfileIdAsync: User not authenticated");
            return null;
        }

        var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        return profile?.Id;
    }

    /// <summary>
    /// Extracts the Keycloak ID from the current HTTP context
    /// </summary>
    private string? GetKeycloakIdFromContext()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext?.User == null)
        {
            return null;
        }

        // Check for mock authentication header (for integration tests)
        if (httpContext.Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            return keycloakIdHeader.ToString();
        }

        // Check if user is authenticated via claims
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var subClaim = httpContext.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(subClaim))
            {
                return subClaim;
            }

            // Fallback: try to find other common claims
            var userIdClaim = httpContext.User.FindFirst("user_id")?.Value 
                           ?? httpContext.User.FindFirst("id")?.Value 
                           ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim;
        }

        return null;
    }
}
