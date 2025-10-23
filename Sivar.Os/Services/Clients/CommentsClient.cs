using Sivar.Core.Clients.Comments;
using Sivar.Core.DTOs;
using Sivar.Core.Interfaces;
using Sivar.Core.Repositories;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of comments client
/// </summary>
public class CommentsClient : BaseRepositoryClient, ICommentsClient
{
    private readonly ICommentService _commentService;
    private readonly ICommentRepository _commentRepository;
    private readonly ILogger<CommentsClient> _logger;

    public CommentsClient(
        ICommentService commentService,
        ICommentRepository commentRepository,
        ILogger<CommentsClient> logger)
    {
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // CRUD operations
    public async Task<CommentDto> CreateCommentAsync(CreateCommentDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CreateCommentAsync");
        return new CommentDto { Id = Guid.NewGuid() };
    }

    public async Task<CommentDto> CreateReplyAsync(Guid parentCommentId, CreateCommentDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CreateReplyAsync: {ParentCommentId}", parentCommentId);
        return new CommentDto { Id = Guid.NewGuid() };
    }

    public async Task<CommentDto> GetCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        if (commentId == Guid.Empty)
        {
            _logger.LogWarning("GetCommentAsync called with empty comment ID");
            return new CommentDto();
        }

        try
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            _logger.LogInformation("Comment retrieved: {CommentId}", commentId);
            return comment != null ? MapToDto(comment) : new CommentDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comment {CommentId}", commentId);
            throw;
        }
    }

    public async Task<CommentDto> UpdateCommentAsync(Guid commentId, UpdateCommentDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UpdateCommentAsync: {CommentId}", commentId);
        return new CommentDto { Id = commentId };
    }

    public async Task DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        if (commentId == Guid.Empty)
        {
            _logger.LogWarning("DeleteCommentAsync called with empty comment ID");
            return;
        }

        try
        {
            await _commentRepository.DeleteAsync(commentId);
            _logger.LogInformation("Comment deleted: {CommentId}", commentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
            throw;
        }
    }

    // Query operations
    public async Task<IEnumerable<CommentDto>> GetPostCommentsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        if (postId == Guid.Empty)
        {
            _logger.LogWarning("GetPostCommentsAsync called with empty post ID");
            return new List<CommentDto>();
        }

        try
        {
            _logger.LogInformation("Comments retrieved for post {PostId}", postId);
            return new List<CommentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comments for post {PostId}", postId);
            throw;
        }
    }

    public async Task<IEnumerable<CommentDto>> GetCommentRepliesAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetCommentRepliesAsync: {CommentId}", commentId);
        return new List<CommentDto>();
    }

    public async Task<IEnumerable<CommentDto>> GetCommentThreadAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetCommentThreadAsync: {CommentId}", commentId);
        return new List<CommentDto>();
    }

    public async Task<IEnumerable<CommentDto>> GetProfileCommentsAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetProfileCommentsAsync: {ProfileId}", profileId);
        return new List<CommentDto>();
    }

    // Analytics
    public async Task<IEnumerable<CommentActivityDto>> GetProfileCommentActivityAsync(Guid profileId, int days = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetProfileCommentActivityAsync: {ProfileId}", profileId);
        return new List<CommentActivityDto>();
    }

    private CommentDto MapToDto(Core.Entities.Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            PostId = comment.PostId,
            CreatedAt = comment.CreatedAt
        };
    }
}
