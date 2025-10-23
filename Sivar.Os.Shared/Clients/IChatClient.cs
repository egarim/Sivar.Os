

using Sivar.Os.Shared.DTOs;

namespace Sivar.Shared.Clients.Chat;

/// <summary>
/// Client for AI chat operations (conversations, messages, saved results)
/// </summary>
public interface IChatClient
{
    // Conversations
    Task<IEnumerable<ConversationDto>> GetConversationsAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<ConversationDto> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<ConversationDto> CreateConversationAsync(CreateConversationDto request, CancellationToken cancellationToken = default);
    Task<ConversationDto> UpdateConversationAsync(Guid conversationId, UpdateConversationDto request, CancellationToken cancellationToken = default);
    Task DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);

    // Chat Messages
    Task<ChatResponseDto> SendMessageAsync(Guid conversationId, SendMessageDto request, CancellationToken cancellationToken = default);

    // Saved Results
    Task<IEnumerable<SavedResultDto>> GetSavedResultsAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<SavedResultDto> SaveResultAsync(Guid profileId, CreateSavedResultDto request, CancellationToken cancellationToken = default);
    Task DeleteSavedResultAsync(Guid profileId, Guid resultId, CancellationToken cancellationToken = default);
    Task DeleteAllSavedResultsAsync(Guid profileId, CancellationToken cancellationToken = default);
}
