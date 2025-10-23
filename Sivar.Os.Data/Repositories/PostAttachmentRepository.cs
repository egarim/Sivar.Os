using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for PostAttachment entity
/// </summary>
public class PostAttachmentRepository : BaseRepository<PostAttachment>, IPostAttachmentRepository
{
    public PostAttachmentRepository(SivarDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get all attachments for a specific post
    /// </summary>
    public async Task<List<PostAttachment>> GetByPostIdAsync(Guid postId)
    {
        return await _context.PostAttachments
            .Where(pa => pa.PostId == postId)
            .ToListAsync();
    }

    /// <summary>
    /// Get attachment by file ID
    /// </summary>
    public async Task<PostAttachment?> GetByFileIdAsync(string fileId)
    {
        return await _context.PostAttachments
            .FirstOrDefaultAsync(pa => pa.FileId == fileId);
    }

    /// <summary>
    /// Delete all attachments for a specific post
    /// </summary>
    public async Task<int> DeleteByPostIdAsync(Guid postId)
    {
        var attachments = await _context.PostAttachments
            .Where(pa => pa.PostId == postId)
            .ToListAsync();

        _context.PostAttachments.RemoveRange(attachments);
        await _context.SaveChangesAsync();
        
        return attachments.Count;
    }

    /// <summary>
    /// Get attachments ordered by display order
    /// </summary>
    public async Task<List<PostAttachment>> GetByPostIdOrderedAsync(Guid postId)
    {
        return await _context.PostAttachments
            .Where(pa => pa.PostId == postId)
            .OrderBy(pa => pa.DisplayOrder)
            .ThenBy(pa => pa.CreatedAt)
            .ToListAsync();
    }
}