using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for ProfileBookmark operations
/// </summary>
public interface IProfileBookmarkRepository
{
    /// <summary>
    /// Get all bookmarks for a specific profile
    /// </summary>
    Task<IEnumerable<ProfileBookmark>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific bookmark by profile and post
    /// </summary>
    Task<ProfileBookmark?> GetByProfileAndPostAsync(Guid profileId, Guid postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a post is bookmarked by a profile
    /// </summary>
    Task<bool> IsBookmarkedAsync(Guid profileId, Guid postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all bookmarked post IDs for a profile
    /// </summary>
    Task<IEnumerable<Guid>> GetBookmarkedPostIdsAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new bookmark
    /// </summary>
    Task<ProfileBookmark> AddAsync(ProfileBookmark bookmark, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a bookmark
    /// </summary>
    Task<bool> RemoveAsync(Guid profileId, Guid postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update bookmark note
    /// </summary>
    Task<ProfileBookmark?> UpdateNoteAsync(Guid profileId, Guid postId, string? note, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated bookmarks for a profile
    /// </summary>
    Task<(IEnumerable<ProfileBookmark> Items, int TotalCount)> GetPaginatedAsync(
        Guid profileId, 
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);
}
