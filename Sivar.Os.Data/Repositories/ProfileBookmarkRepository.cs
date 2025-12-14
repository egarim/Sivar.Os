using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for ProfileBookmark data access
/// </summary>
public class ProfileBookmarkRepository : BaseRepository<ProfileBookmark>, IProfileBookmarkRepository
{
    public ProfileBookmarkRepository(SivarDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProfileBookmark>> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.ProfileId == profileId && !b.IsDeleted)
            .Include(b => b.Post)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileBookmark?> GetByProfileAndPostAsync(Guid profileId, Guid postId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(b => b.ProfileId == profileId && b.PostId == postId && !b.IsDeleted, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsBookmarkedAsync(Guid profileId, Guid postId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(b => b.ProfileId == profileId && b.PostId == postId && !b.IsDeleted, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Guid>> GetBookmarkedPostIdsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.ProfileId == profileId && !b.IsDeleted)
            .Select(b => b.PostId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public new async Task<ProfileBookmark> AddAsync(ProfileBookmark bookmark, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(bookmark, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return bookmark;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(Guid profileId, Guid postId, CancellationToken cancellationToken = default)
    {
        var bookmark = await GetByProfileAndPostAsync(profileId, postId, cancellationToken);
        if (bookmark == null)
            return false;

        // Soft delete
        bookmark.IsDeleted = true;
        bookmark.DeletedAt = DateTime.UtcNow;
        bookmark.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<ProfileBookmark?> UpdateNoteAsync(Guid profileId, Guid postId, string? note, CancellationToken cancellationToken = default)
    {
        var bookmark = await GetByProfileAndPostAsync(profileId, postId, cancellationToken);
        if (bookmark == null)
            return null;

        bookmark.Note = note;
        bookmark.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        return bookmark;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<ProfileBookmark> Items, int TotalCount)> GetPaginatedAsync(
        Guid profileId, 
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(b => b.ProfileId == profileId && !b.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(b => b.Post)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
