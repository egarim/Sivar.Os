
using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of AI chat client with diagnostic logging
/// </summary>
public class ChatClient : BaseClient, ISivarChatClient
{
    private readonly ILogger<ChatClient>? _logger;

    public ChatClient(HttpClient httpClient, SivarClientOptions options, ILogger<ChatClient>? logger = null) 
        : base(httpClient, options)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/conversations/profiles/{profileId}";
        _logger?.LogInformation("[ChatClient.GetConversationsAsync] START - ProfileId={ProfileId}, Endpoint={Endpoint}", profileId, endpoint);
        
        try
        {
            var result = await GetAsync<IEnumerable<ConversationDto>>(endpoint, cancellationToken);
            var resultList = result?.ToList() ?? new List<ConversationDto>();
            _logger?.LogInformation("[ChatClient.GetConversationsAsync] SUCCESS - ProfileId={ProfileId}, Count={Count}", profileId, resultList.Count);
            return resultList;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ChatClient.GetConversationsAsync] FAILED - ProfileId={ProfileId}, Error={Error}", profileId, ex.Message);
            throw;
        }
    }

    public async Task<ConversationDto> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/conversations/{conversationId}/messages";
        _logger?.LogInformation("[ChatClient.GetConversationAsync] START - ConversationId={ConversationId}, Endpoint={Endpoint}", conversationId, endpoint);
        
        try
        {
            var result = await GetAsync<ConversationDto>(endpoint, cancellationToken);
            _logger?.LogInformation("[ChatClient.GetConversationAsync] SUCCESS - ConversationId={ConversationId}", conversationId);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ChatClient.GetConversationAsync] FAILED - ConversationId={ConversationId}, Error={Error}", conversationId, ex.Message);
            throw;
        }
    }

    public async Task<ConversationDto> CreateConversationAsync(CreateConversationDto request, CancellationToken cancellationToken = default)
    {
        var endpoint = "api/conversations";
        _logger?.LogInformation("[ChatClient.CreateConversationAsync] START - ProfileId={ProfileId}, Title={Title}, Endpoint={Endpoint}", 
            request.ProfileId, request.Title, endpoint);
        
        try
        {
            var result = await PostAsync<ConversationDto>(endpoint, request, cancellationToken);
            _logger?.LogInformation("[ChatClient.CreateConversationAsync] SUCCESS - ConversationId={ConversationId}", result?.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ChatClient.CreateConversationAsync] FAILED - ProfileId={ProfileId}, Error={Error}", request.ProfileId, ex.Message);
            throw;
        }
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
        var endpoint = $"api/conversations/{conversationId}/chatmessages";
        _logger?.LogInformation("[ChatClient.SendMessageAsync] START - ConversationId={ConversationId}, ContentLength={Length}, Endpoint={Endpoint}", 
            conversationId, request.Content?.Length ?? 0, endpoint);
        
        try
        {
            var startTime = DateTime.UtcNow;
            var result = await PostAsync<ChatResponseDto>(endpoint, request, cancellationToken);
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            _logger?.LogInformation("[ChatClient.SendMessageAsync] SUCCESS - ConversationId={ConversationId}, Duration={Duration}ms, ResponseLength={Length}", 
                conversationId, elapsed, result?.AssistantMessage?.Content?.Length ?? 0);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[ChatClient.SendMessageAsync] FAILED - ConversationId={ConversationId}, Error={Error}", conversationId, ex.Message);
            throw;
        }
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
