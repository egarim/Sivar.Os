using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client for reaction operations
/// </summary>
public interface IReactionsClient
{
    // Add reactions
    Task<ReactionDto> AddPostReactionAsync(CreatePostReactionDto request, CancellationToken cancellationToken = default);
    Task<ReactionDto> AddCommentReactionAsync(CreateCommentReactionDto request, CancellationToken cancellationToken = default);

    // Remove reactions
    Task RemovePostReactionAsync(Guid postId, CancellationToken cancellationToken = default);
    Task RemoveCommentReactionAsync(Guid commentId, CancellationToken cancellationToken = default);

    // Get reactions
    Task<ReactionDto> GetReactionAsync(Guid reactionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReactionDto>> GetPostReactionsAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReactionDto>> GetCommentReactionsAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReactionDto>> GetProfileReactionsAsync(Guid profileId, CancellationToken cancellationToken = default);

    // Analytics
    Task<PostReactionSummaryDto> GetPostReactionAnalyticsAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<ProfileEngagementDto> GetProfileEngagementAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReactionActivityDto>> GetProfileReactionActivityAsync(Guid profileId, int days = 30, CancellationToken cancellationToken = default);
}
