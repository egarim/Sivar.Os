
using Microsoft.Extensions.Caching.Memory;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of chat client that calls ChatService directly.
/// Follows Sivar.Os architecture: Components → Services → Repositories (no controllers)
/// </summary>
public class ChatClient : BaseRepositoryClient, ISivarChatClient
{
    private readonly IChatService _chatService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IChatBotSettingsRepository _settingsRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ChatClient> _logger;

    private const string CacheKeyPrefix = "ChatBotSettings_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ChatClient(
        IChatService chatService,
        IConversationRepository conversationRepository,
        IChatMessageRepository chatMessageRepository,
        IChatBotSettingsRepository settingsRepository,
        IMemoryCache cache,
        ILogger<ChatClient> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _chatMessageRepository = chatMessageRepository ?? throw new ArgumentNullException(nameof(chatMessageRepository));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Conversations
    public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChatClient.GetConversationsAsync] START - ProfileId={ProfileId}", profileId);
        try
        {
            var conversations = await _chatService.GetProfileConversationsAsync(profileId);
            _logger.LogInformation("[ChatClient.GetConversationsAsync] SUCCESS - ProfileId={ProfileId}, Count={Count}", 
                profileId, conversations?.Count ?? 0);
            return conversations ?? new List<ConversationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatClient.GetConversationsAsync] ERROR - ProfileId={ProfileId}", profileId);
            throw;
        }
    }

    public async Task<ConversationDto> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChatClient.GetConversationAsync] START - ConversationId={ConversationId}", conversationId);
        try
        {
            var (conversation, messages) = await _chatService.GetConversationWithMessagesAsync(conversationId);
            
            // Create a new DTO with messages included
            var result = conversation with { Messages = messages };
            
            _logger.LogInformation("[ChatClient.GetConversationAsync] SUCCESS - ConversationId={ConversationId}, MessageCount={Count}", 
                conversationId, messages?.Count ?? 0);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatClient.GetConversationAsync] ERROR - ConversationId={ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<ConversationDto> CreateConversationAsync(CreateConversationDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChatClient.CreateConversationAsync] START - ProfileId={ProfileId}, Title={Title}", 
            request.ProfileId, request.Title);
        try
        {
            var conversation = await _chatService.CreateConversationAsync(request);
            _logger.LogInformation("[ChatClient.CreateConversationAsync] SUCCESS - ConversationId={ConversationId}", 
                conversation.Id);
            return conversation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatClient.CreateConversationAsync] ERROR - ProfileId={ProfileId}", request.ProfileId);
            throw;
        }
    }

    public async Task<ConversationDto> UpdateConversationAsync(Guid conversationId, UpdateConversationDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChatClient.UpdateConversationAsync] START - ConversationId={ConversationId}", conversationId);
        // TODO: Implement UpdateConversationAsync in IChatService when needed
        _logger.LogWarning("[ChatClient.UpdateConversationAsync] NOT_IMPLEMENTED - Returning current conversation");
        var (conversation, _) = await _chatService.GetConversationWithMessagesAsync(conversationId);
        return conversation;
    }

    public async Task DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChatClient.DeleteConversationAsync] START - ConversationId={ConversationId}", conversationId);
        try
        {
            // Get the conversation to find the profile ID
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                _logger.LogWarning("[ChatClient.DeleteConversationAsync] CONVERSATION_NOT_FOUND - ConversationId={ConversationId}", conversationId);
                return;
            }
            
            await _chatService.DeleteConversationAsync(conversationId, conversation.ProfileId);
            _logger.LogInformation("[ChatClient.DeleteConversationAsync] SUCCESS - ConversationId={ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatClient.DeleteConversationAsync] ERROR - ConversationId={ConversationId}", conversationId);
            throw;
        }
    }

    // Chat Messages
    public async Task<ChatResponseDto> SendMessageAsync(Guid conversationId, SendMessageDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChatClient.SendMessageAsync] START - ConversationId={ConversationId}, ContentLength={Length}", 
            conversationId, request.Content?.Length ?? 0);
        try
        {
            // Create a new DTO with the correct conversation ID (init-only property)
            var messageDto = new SendMessageDto
            {
                ConversationId = conversationId,
                Content = request.Content
            };
            
            // Get the conversation to find the profile ID
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                _logger.LogWarning("[ChatClient.SendMessageAsync] CONVERSATION_NOT_FOUND - ConversationId={ConversationId}", conversationId);
                throw new InvalidOperationException($"Conversation {conversationId} not found");
            }
            
            _logger.LogInformation("[ChatClient.SendMessageAsync] Calling ChatService - ConversationId={ConversationId}, ProfileId={ProfileId}", 
                conversationId, conversation.ProfileId);
            
            var response = await _chatService.SendMessageAsync(messageDto, conversation.ProfileId);
            
            _logger.LogInformation("[ChatClient.SendMessageAsync] SUCCESS - ConversationId={ConversationId}, ResponseLength={Length}", 
                conversationId, response.AssistantMessage?.Content?.Length ?? 0);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatClient.SendMessageAsync] ERROR - ConversationId={ConversationId}", conversationId);
            throw;
        }
    }

    // Saved Results
    public async Task<IEnumerable<SavedResultDto>> GetSavedResultsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChatClient.GetSavedResultsAsync] START - ProfileId={ProfileId}", profileId);
        // TODO: Implement when saved results feature is added
        return new List<SavedResultDto>();
    }

    public async Task<SavedResultDto> SaveResultAsync(Guid profileId, CreateSavedResultDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChatClient.SaveResultAsync] START - ProfileId={ProfileId}", profileId);
        // TODO: Implement when saved results feature is added
        return new SavedResultDto { Id = Guid.NewGuid() };
    }

    public async Task DeleteSavedResultAsync(Guid profileId, Guid resultId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChatClient.DeleteSavedResultAsync] START - ProfileId={ProfileId}, ResultId={ResultId}", profileId, resultId);
        // TODO: Implement when saved results feature is added
    }

    public async Task DeleteAllSavedResultsAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChatClient.DeleteAllSavedResultsAsync] START - ProfileId={ProfileId}", profileId);
        // TODO: Implement when saved results feature is added
    }

    /// <summary>
    /// Gets chat bot settings for a culture
    /// Phase 0.5: Configurable welcome messages and chat settings
    /// Phase 0.6: Properly queries database for relational QuickActions
    /// </summary>
    public async Task<ChatBotSettingsDto?> GetSettingsAsync(string? culture = null, string? region = null, bool forceReload = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChatClient.GetSettingsAsync] START - Culture={Culture}, Region={Region}, ForceReload={ForceReload}", culture, region, forceReload);
        
        try
        {
            var cacheKey = $"{CacheKeyPrefix}{culture ?? "default"}_{region ?? "any"}";
            
            // Clear cache if force reload requested
            if (forceReload)
            {
                _cache.Remove(cacheKey);
                _logger.LogInformation("[ChatClient.GetSettingsAsync] Cache cleared for {CacheKey}", cacheKey);
            }
            
            if (!forceReload && _cache.TryGetValue(cacheKey, out ChatBotSettingsDto? cachedSettings) && cachedSettings != null)
            {
                _logger.LogDebug("[ChatClient.GetSettingsAsync] Returning cached settings for {Culture}/{Region}", culture, region);
                return cachedSettings;
            }

            var settings = await _settingsRepository.GetActiveSettingsAsync(culture, region);
            
            if (settings == null)
            {
                _logger.LogInformation("[ChatClient.GetSettingsAsync] No settings found for {Culture}/{Region}, returning defaults", culture, region);
                return GetDefaultSettings(culture);
            }

            var dto = MapToDto(settings);
            _cache.Set(cacheKey, dto, CacheDuration);
            
            _logger.LogInformation("[ChatClient.GetSettingsAsync] SUCCESS - Loaded settings '{Key}' for {Culture}/{Region} with {Count} QuickActionItems", 
                settings.Key, culture, region, dto.QuickActionItems.Count);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatClient.GetSettingsAsync] ERROR - Returning defaults");
            return GetDefaultSettings(culture);
        }
    }

    private ChatBotSettingsDto MapToDto(Sivar.Os.Shared.Entities.ChatBotSettings settings)
    {
        // Log quick actions from database for debugging
        _logger.LogInformation("[ChatClient] 📊 Settings '{Key}' has {Count} QuickAction entities",
            settings.Key, settings.QuickActions?.Count ?? 0);
        
        if (settings.QuickActions != null)
        {
            foreach (var qa in settings.QuickActions)
            {
                _logger.LogInformation("[ChatClient] 📋 QuickAction: Label='{Label}', IsActive={IsActive}, IsDeleted={IsDeleted}",
                    qa.Label, qa.IsActive, qa.IsDeleted);
            }
        }

        // Map QuickAction entities to DTOs - FILTER by IsActive and !IsDeleted
        var quickActionItems = settings.QuickActions?
            .Where(qa => qa.IsActive && !qa.IsDeleted)
            .OrderBy(qa => qa.SortOrder)
            .Select(qa => new QuickActionDto
            {
                Id = qa.Id,
                Label = qa.Label,
                Icon = qa.Icon,
                MudBlazorIcon = qa.MudBlazorIcon,
                Color = qa.Color,
                DefaultQuery = qa.DefaultQuery,
                ContextHint = qa.ContextHint,
                SortOrder = qa.SortOrder,
                RequiresLocation = qa.RequiresLocation,
                IsActive = qa.IsActive,
                CapabilityKey = qa.Capability?.Key
            })
            .ToList() ?? new();
        
        _logger.LogInformation("[ChatClient] ✅ Mapped {Count} active QuickActionItems for settings '{Key}'",
            quickActionItems.Count, settings.Key);

        return new ChatBotSettingsDto
        {
            Id = settings.Id,
            Key = settings.Key,
            Culture = settings.Culture,
            WelcomeMessage = settings.WelcomeMessage,
            HeaderTagline = settings.HeaderTagline,
            BotName = settings.BotName,
            QuickActionItems = quickActionItems,
            SystemPrompt = settings.SystemPrompt,
            ErrorMessage = settings.ErrorMessage,
            ThinkingMessage = settings.ThinkingMessage
        };
    }

    private ChatBotSettingsDto GetDefaultSettings(string? culture)
    {
        return new ChatBotSettingsDto
        {
            Id = Guid.Empty,
            Key = "default",
            Culture = culture ?? "es",
            WelcomeMessage = "¡Hola! Soy tu asistente Sivar AI. Puedo ayudarte a:\n\n🔍 Encontrar negocios y servicios\n📝 Buscar lugares y eventos\n🏪 Descubrir lo mejor de El Salvador\n📋 Guiarte en trámites y papeleos\n\n¡Pregúntame algo como \"pizzerías cerca\" o \"cómo sacar pasaporte\"!",
            HeaderTagline = "Siempre aquí para ayudarte",
            BotName = "Sivar AI Assistant",
            QuickActionItems = new List<QuickActionDto>(),
            ErrorMessage = "Lo siento, ocurrió un error. Por favor intenta de nuevo.",
            ThinkingMessage = "Pensando..."
        };
    }
}
