using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client for comment operations
/// </summary>
public interface ICommentsClient
{
    // CRUD operations
    Task<CommentDto> CreateCommentAsync(CreateCommentDto request, CancellationToken cancellationToken = default);
    Task<CommentDto> CreateReplyAsync(Guid parentCommentId, CreateCommentDto request, CancellationToken cancellationToken = default);
    Task<CommentDto> GetCommentAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task<CommentDto> UpdateCommentAsync(Guid commentId, UpdateCommentDto request, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default);

    // Query operations
    Task<IEnumerable<CommentDto>> GetPostCommentsAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CommentDto>> GetCommentRepliesAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CommentDto>> GetCommentThreadAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CommentDto>> GetProfileCommentsAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);

    // Analytics
    Task<IEnumerable<CommentActivityDto>> GetProfileCommentActivityAsync(Guid profileId, int days = 30, CancellationToken cancellationToken = default);
}
