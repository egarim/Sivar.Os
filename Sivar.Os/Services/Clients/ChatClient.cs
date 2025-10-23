
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of chat client
/// </summary>
public class ChatClient : BaseRepositoryClient, ISivarChatClient
{
    private readonly IChatService _chatService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly ILogger<ChatClient> _logger;

    public ChatClient(
        IChatService chatService,
        IConversationRepository conversationRepository,
        IChatMessageRepository chatMessageRepository,
        ILogger<ChatClient> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _chatMessageRepository = chatMessageRepository ?? throw new ArgumentNullException(nameof(chatMessageRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Conversations
    public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetConversationsAsync for profile {ProfileId}", profileId);
        return new List<ConversationDto>();
    }

    public async Task<ConversationDto> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetConversationAsync: {ConversationId}", conversationId);
        return new ConversationDto { Id = conversationId };
    }

    public async Task<ConversationDto> CreateConversationAsync(CreateConversationDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CreateConversationAsync");
        return new ConversationDto { Id = Guid.NewGuid() };
    }

    public async Task<ConversationDto> UpdateConversationAsync(Guid conversationId, UpdateConversationDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UpdateConversationAsync: {ConversationId}", conversationId);
        return new ConversationDto { Id = conversationId };
    }

    public async Task DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DeleteConversationAsync: {ConversationId}", conversationId);
    }

    // Chat Messages
    public async Task<ChatResponseDto> SendMessageAsync(Guid conversationId, SendMessageDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SendMessageAsync: {ConversationId}", conversationId);
        return new ChatResponseDto 
        { 
            UserMessage = new ChatMessageDto { Id = Guid.NewGuid() },
            AssistantMessage = new ChatMessageDto { Id = Guid.NewGuid() },
            ConversationId = conversationId
        };
    }

    // Saved Results
    public async Task<IEnumerable<SavedResultDto>> GetSavedResultsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetSavedResultsAsync for profile {ProfileId}", profileId);
        return new List<SavedResultDto>();
    }

    public async Task<SavedResultDto> SaveResultAsync(Guid profileId, CreateSavedResultDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SaveResultAsync for profile {ProfileId}", profileId);
        return new SavedResultDto { Id = Guid.NewGuid() };
    }

    public async Task DeleteSavedResultAsync(Guid profileId, Guid resultId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DeleteSavedResultAsync: {ProfileId}, {ResultId}", profileId, resultId);
    }

    public async Task DeleteAllSavedResultsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DeleteAllSavedResultsAsync for profile {ProfileId}", profileId);
    }
}
