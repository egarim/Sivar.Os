

using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of comments client using repositories and services
/// Provides the same interface as the HTTP client but operates directly on the service layer
/// </summary>
public class CommentsClient : BaseRepositoryClient, ICommentsClient
{
    private readonly ICommentService _commentService;
    private readonly ICommentRepository _commentRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CommentsClient> _logger;

    public CommentsClient(
        ICommentService commentService,
        ICommentRepository commentRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CommentsClient> logger)
    {
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // CRUD operations
    public async Task<CommentDto> CreateCommentAsync(CreateCommentDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            _logger.LogWarning("CreateCommentAsync called with null request");
            return null!;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("CreateCommentAsync: No authenticated user");
                return null!;
            }

            _logger.LogInformation("CreateCommentAsync: {KeycloakId}, PostId={PostId}, Content length={Length}", 
                keycloakId, request.PostId, request.Content?.Length ?? 0);
            
            var comment = await _commentService.CreateCommentAsync(keycloakId, request);
            return comment ?? null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment");
            throw;
        }
    }

    public async Task<CommentDto> CreateReplyAsync(Guid parentCommentId, CreateCommentDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            _logger.LogWarning("CreateReplyAsync called with null request");
            return null!;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("CreateReplyAsync: No authenticated user");
                return null!;
            }

            _logger.LogInformation("CreateReplyAsync: {KeycloakId}, ParentCommentId={ParentCommentId}", 
                keycloakId, parentCommentId);

            var createReplyDto = new CreateReplyDto
            {
                Content = request.Content,
                Language = request.Language
            };

            var reply = await _commentService.CreateReplyAsync(keycloakId, parentCommentId, createReplyDto);
            return reply ?? null!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reply to comment {ParentCommentId}", parentCommentId);
            throw;
        }
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
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("DeleteCommentAsync: No authenticated user");
                throw new UnauthorizedAccessException("User not authenticated");
            }

            await _commentService.DeleteCommentAsync(commentId, keycloakId);
            _logger.LogInformation("Comment deleted: {CommentId} by {KeycloakId}", commentId, keycloakId);
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
            var keycloakId = GetKeycloakIdFromContext();
            var result = await _commentService.GetCommentsByPostAsync(postId, keycloakId);
            _logger.LogInformation("Comments retrieved for post {PostId}: {Count} comments", postId, result.TotalCount);
            return result.Comments ?? new List<CommentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comments for post {PostId}", postId);
            throw;
        }
    }

    public async Task<IEnumerable<CommentDto>> GetCommentRepliesAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        if (commentId == Guid.Empty)
        {
            _logger.LogWarning("GetCommentRepliesAsync called with empty comment ID");
            return new List<CommentDto>();
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            var (replies, totalCount) = await _commentService.GetRepliesByCommentAsync(commentId, keycloakId);
            _logger.LogInformation("Replies retrieved for comment {CommentId}: {Count} replies", commentId, totalCount);
            return replies ?? new List<CommentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving replies for comment {CommentId}", commentId);
            throw;
        }
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

    private CommentDto MapToDto(Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            PostId = comment.PostId,
            CreatedAt = comment.CreatedAt
        };
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
        }

        return null;
    }
}
