
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of reactions client
/// </summary>
public class ReactionsClient : BaseClient, IReactionsClient
{
    public ReactionsClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    public async Task<ReactionDto> AddPostReactionAsync(CreatePostReactionDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ReactionDto>("api/reactions/post", request, cancellationToken);
    }

    public async Task<ReactionDto> AddCommentReactionAsync(CreateCommentReactionDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ReactionDto>("api/reactions/comment", request, cancellationToken);
    }

    public async Task RemovePostReactionAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/reactions/post/{postId}", cancellationToken);
    }

    public async Task RemoveCommentReactionAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/reactions/comment/{commentId}", cancellationToken);
    }

    public async Task<ReactionDto> GetReactionAsync(Guid reactionId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ReactionDto>($"api/reactions/{reactionId}", cancellationToken);
    }

    public async Task<IEnumerable<ReactionDto>> GetPostReactionsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ReactionDto>>($"api/reactions/post/{postId}", cancellationToken);
    }

    public async Task<IEnumerable<ReactionDto>> GetCommentReactionsAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ReactionDto>>($"api/reactions/comment/{commentId}", cancellationToken);
    }

    public async Task<IEnumerable<ReactionDto>> GetProfileReactionsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ReactionDto>>($"api/reactions/profile/{profileId}", cancellationToken);
    }

    public async Task<PostReactionSummaryDto> GetPostReactionAnalyticsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PostReactionSummaryDto>($"api/reactions/analytics/post/{postId}", cancellationToken);
    }

    public async Task<ProfileEngagementDto> GetProfileEngagementAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ProfileEngagementDto>($"api/reactions/engagement/{profileId}", cancellationToken);
    }

    public async Task<IEnumerable<ReactionActivityDto>> GetProfileReactionActivityAsync(Guid profileId, int days = 30, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ReactionActivityDto>>($"api/reactions/activity/{profileId}?days={days}", cancellationToken);
    }
}
