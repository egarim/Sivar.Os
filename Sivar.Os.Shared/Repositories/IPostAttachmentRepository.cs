
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for PostAttachment entity operations
/// </summary>
public interface IPostAttachmentRepository : IBaseRepository<PostAttachment>
{
    /// <summary>
    /// Get all attachments for a specific post
    /// </summary>
    /// <param name="postId">The post ID</param>
    /// <returns>List of attachments for the post</returns>
    Task<List<PostAttachment>> GetByPostIdAsync(Guid postId);

    /// <summary>
    /// Get attachment by file ID
    /// </summary>
    /// <param name="fileId">The file storage ID</param>
    /// <returns>The attachment if found</returns>
    Task<PostAttachment?> GetByFileIdAsync(string fileId);

    /// <summary>
    /// Delete all attachments for a specific post
    /// </summary>
    /// <param name="postId">The post ID</param>
    /// <returns>Number of attachments deleted</returns>
    Task<int> DeleteByPostIdAsync(Guid postId);

    /// <summary>
    /// Get attachments ordered by display order
    /// </summary>
    /// <param name="postId">The post ID</param>
    /// <returns>List of attachments ordered by DisplayOrder</returns>
    Task<List<PostAttachment>> GetByPostIdOrderedAsync(Guid postId);
}