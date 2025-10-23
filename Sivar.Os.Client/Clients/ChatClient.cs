
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of AI chat client
/// </summary>
public class ChatClient : BaseClient, ISivarChatClient
{
    public ChatClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ConversationDto>>($"api/conversations/profiles/{profileId}", cancellationToken);
    }

    public async Task<ConversationDto> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<ConversationDto>($"api/conversations/{conversationId}/messages", cancellationToken);
    }

    public async Task<ConversationDto> CreateConversationAsync(CreateConversationDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ConversationDto>("api/conversations", request, cancellationToken);
    }

    public async Task<ConversationDto> UpdateConversationAsync(Guid conversationId, UpdateConversationDto request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<ConversationDto>($"api/conversations/{conversationId}", request, cancellationToken);
    }

    public async Task DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/conversations/{conversationId}", cancellationToken);
    }

    public async Task<ChatResponseDto> SendMessageAsync(Guid conversationId, SendMessageDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<ChatResponseDto>($"api/conversations/{conversationId}/chatmessages", request, cancellationToken);
    }

    public async Task<IEnumerable<SavedResultDto>> GetSavedResultsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<SavedResultDto>>($"api/profiles/{profileId}/savedresults", cancellationToken);
    }

    public async Task<SavedResultDto> SaveResultAsync(Guid profileId, CreateSavedResultDto request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<SavedResultDto>($"api/profiles/{profileId}/savedresults", request, cancellationToken);
    }

    public async Task DeleteSavedResultAsync(Guid profileId, Guid resultId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/profiles/{profileId}/savedresults/{resultId}", cancellationToken);
    }

    public async Task DeleteAllSavedResultsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        await DeleteAsync($"api/profiles/{profileId}/savedresults", cancellationToken);
    }
}
