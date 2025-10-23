

using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of comments client
/// </summary>
public class CommentsClient : BaseClient, ICommentsClient
{
    public CommentsClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    public async Task<CommentDto> CreateCommentAsync(CreateCommentDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CommentDto>("api/comments", request, cancellationToken);
    }

    public async Task<CommentDto> CreateReplyAsync(Guid parentCommentId, CreateCommentDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<CommentDto>($"api/comments/{parentCommentId}/reply", request, cancellationToken);
    }

    public async Task<CommentDto> GetCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<CommentDto>($"api/comments/{commentId}", cancellationToken);
    }

    public async Task<CommentDto> UpdateCommentAsync(Guid commentId, UpdateCommentDto request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<CommentDto>($"api/comments/{commentId}", request, cancellationToken);
    }

    public async Task DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/comments/{commentId}", cancellationToken);
    }

    public async Task<IEnumerable<CommentDto>> GetPostCommentsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<CommentDto>>($"api/comments/post/{postId}", cancellationToken);
    }

    public async Task<IEnumerable<CommentDto>> GetCommentRepliesAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<CommentDto>>($"api/comments/{commentId}/replies", cancellationToken);
    }

    public async Task<IEnumerable<CommentDto>> GetCommentThreadAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<CommentDto>>($"api/comments/{commentId}/thread", cancellationToken);
    }

    public async Task<IEnumerable<CommentDto>> GetProfileCommentsAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<CommentDto>>($"api/comments/profile/{profileId}?pageSize={pageSize}&pageNumber={pageNumber}", cancellationToken);
    }

    public async Task<IEnumerable<CommentActivityDto>> GetProfileCommentActivityAsync(Guid profileId, int days = 30, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<CommentActivityDto>>($"api/comments/activity/{profileId}?days={days}", cancellationToken);
    }
}
