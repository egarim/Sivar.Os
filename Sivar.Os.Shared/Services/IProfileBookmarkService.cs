using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for ProfileBookmark business logic
/// </summary>
public interface IProfileBookmarkService
{
    /// <summary>
    /// Get all bookmarks for the current user's profile
    /// </summary>
    Task<IEnumerable<ProfileBookmark>> GetMyBookmarksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all bookmarked post IDs for the current user's profile
    /// </summary>
    Task<IEnumerable<Guid>> GetMyBookmarkedPostIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a post is bookmarked by the current user
    /// </summary>
    Task<bool> IsBookmarkedAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggle bookmark for a post (add if not exists, remove if exists)
    /// </summary>
    Task<bool> ToggleBookmarkAsync(Guid postId, string? note = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a bookmark for a post
    /// </summary>
    Task<ProfileBookmark?> AddBookmarkAsync(Guid postId, string? note = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a bookmark for a post
    /// </summary>
    Task<bool> RemoveBookmarkAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the note for a bookmark
    /// </summary>
    Task<ProfileBookmark?> UpdateNoteAsync(Guid postId, string? note, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated bookmarks for the current user
    /// </summary>
    Task<(IEnumerable<ProfileBookmark> Items, int TotalCount)> GetPaginatedAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);
}
