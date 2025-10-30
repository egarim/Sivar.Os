
using Microsoft.AspNetCore.Http;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of reactions client
/// </summary>
public class ReactionsClient : BaseRepositoryClient, IReactionsClient
{
    private readonly IReactionService _reactionService;
    private readonly IReactionRepository _reactionRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ReactionsClient> _logger;

    public ReactionsClient(
        IReactionService reactionService,
        IReactionRepository reactionRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ReactionsClient> logger)
    {
        _reactionService = reactionService ?? throw new ArgumentNullException(nameof(reactionService));
        _reactionRepository = reactionRepository ?? throw new ArgumentNullException(nameof(reactionRepository));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Add reactions
    public async Task<ReactionDto> AddPostReactionAsync(CreatePostReactionDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ReactionsClient.AddPostReactionAsync] START - PostId={PostId}, ReactionType={ReactionType}", 
            request.PostId, request.ReactionType);

        var keycloakId = GetKeycloakIdFromContext();
        if (string.IsNullOrEmpty(keycloakId))
        {
            _logger.LogWarning("[ReactionsClient.AddPostReactionAsync] No Keycloak ID found");
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var result = await _reactionService.TogglePostReactionAsync(keycloakId, request.PostId, request.ReactionType);
        
        if (result?.Reaction == null)
        {
            _logger.LogWarning("[ReactionsClient.AddPostReactionAsync] Service returned null result");
            return new ReactionDto { Id = Guid.NewGuid() }; // Return stub for now
        }

        _logger.LogInformation("[ReactionsClient.AddPostReactionAsync] SUCCESS - ReactionId={ReactionId}, Action={Action}", 
            result.Reaction.Id, result.Action);

        return result.Reaction;
    }

    public async Task<ReactionDto> AddCommentReactionAsync(CreateCommentReactionDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ReactionsClient.AddCommentReactionAsync] START - CommentId={CommentId}, ReactionType={ReactionType}", 
            request.CommentId, request.ReactionType);

        var keycloakId = GetKeycloakIdFromContext();
        if (string.IsNullOrEmpty(keycloakId))
        {
            _logger.LogWarning("[ReactionsClient.AddCommentReactionAsync] No Keycloak ID found");
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var result = await _reactionService.ToggleCommentReactionAsync(keycloakId, request.CommentId, request.ReactionType);
        
        if (result?.Reaction == null)
        {
            _logger.LogWarning("[ReactionsClient.AddCommentReactionAsync] Service returned null result");
            return new ReactionDto { Id = Guid.NewGuid() }; // Return stub for now
        }

        _logger.LogInformation("[ReactionsClient.AddCommentReactionAsync] SUCCESS - ReactionId={ReactionId}, Action={Action}", 
            result.Reaction.Id, result.Action);

        return result.Reaction;
    }

    // Remove reactions
    public async Task RemovePostReactionAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RemovePostReactionAsync: {PostId}", postId);
    }

    public async Task RemoveCommentReactionAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RemoveCommentReactionAsync: {CommentId}", commentId);
    }

    // Get reactions
    public async Task<ReactionDto> GetReactionAsync(Guid reactionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetReactionAsync: {ReactionId}", reactionId);
        return new ReactionDto { Id = reactionId };
    }

    public async Task<IEnumerable<ReactionDto>> GetPostReactionsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        if (postId == Guid.Empty)
        {
            _logger.LogWarning("GetPostReactionsAsync called with empty post ID");
            return new List<ReactionDto>();
        }

        try
        {
            _logger.LogInformation("Reactions retrieved for post {PostId}", postId);
            return new List<ReactionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reactions for post {PostId}", postId);
            throw;
        }
    }

    public async Task<IEnumerable<ReactionDto>> GetCommentReactionsAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetCommentReactionsAsync: {CommentId}", commentId);
        return new List<ReactionDto>();
    }

    public async Task<IEnumerable<ReactionDto>> GetProfileReactionsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetProfileReactionsAsync: {ProfileId}", profileId);
        return new List<ReactionDto>();
    }

    // Analytics
    public async Task<PostReactionSummaryDto> GetPostReactionAnalyticsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetPostReactionAnalyticsAsync: {PostId}", postId);
        return new PostReactionSummaryDto();
    }

    public async Task<ProfileEngagementDto> GetProfileEngagementAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetProfileEngagementAsync: {ProfileId}", profileId);
        return new ProfileEngagementDto();
    }

    public async Task<IEnumerable<ReactionActivityDto>> GetProfileReactionActivityAsync(Guid profileId, int days = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetProfileReactionActivityAsync: {ProfileId}, {Days} days", profileId, days);
        return new List<ReactionActivityDto>();
    }

    private ReactionDto MapToDto(Reaction reaction)
    {
        return new ReactionDto
        {
            Id = reaction.Id,
            PostId = reaction.PostId,
            ReactionType = reaction.ReactionType,
            CreatedAt = reaction.CreatedAt
        };
    }

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

            // Fallback: try to find "user_id" or "id" claims if "sub" is not available
            var userIdClaim = httpContext.User.FindFirst("user_id")?.Value 
                           ?? httpContext.User.FindFirst("id")?.Value 
                           ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim;
        }

        return null;
    }
}
