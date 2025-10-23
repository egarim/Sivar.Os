using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;
using System.Text.Json;
using AiChatMessage = Microsoft.Extensions.AI.ChatMessage;
using EntityChatMessage = Sivar.Os.Shared.Entities.ChatMessage;

namespace Sivar.Os.Services;

/// <summary>
/// Service for managing AI chat conversations
/// </summary>
public class ChatService : IChatService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IChatClient _chatClient;
    private readonly ChatFunctionService _functionService;
    private readonly ChatServiceOptions _options;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IConversationRepository conversationRepository,
        IChatMessageRepository messageRepository,
        IProfileRepository profileRepository,
        IChatClient chatClient,
        ChatFunctionService functionService,
        IOptions<ChatServiceOptions> options,
        ILogger<ChatService> logger)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _functionService = functionService ?? throw new ArgumentNullException(nameof(functionService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ConversationDto> CreateConversationAsync(CreateConversationDto dto)
    {
        try
        {
            // Verify profile exists
            var profile = await _profileRepository.GetByIdAsync(dto.ProfileId);
            if (profile == null)
            {
                throw new InvalidOperationException($"Profile {dto.ProfileId} not found");
            }

            var conversation = new Conversation
            {
                ProfileId = dto.ProfileId,
                Title = dto.Title,
                LastMessageAt = DateTime.UtcNow,
                IsActive = false // Will be activated manually
            };

            var created = await _conversationRepository.AddAsync(conversation);
            
            _logger.LogInformation("Created conversation {ConversationId} for profile {ProfileId}", 
                created.Id, dto.ProfileId);

            return MapToDto(created, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation for profile {ProfileId}", dto.ProfileId);
            throw;
        }
    }

    public async Task<List<ConversationDto>> GetProfileConversationsAsync(Guid profileId)
    {
        try
        {
            var conversations = await _conversationRepository.GetProfileConversationsAsync(profileId, includeMessages: false);
            
            var dtos = new List<ConversationDto>();
            foreach (var conv in conversations)
            {
                var messageCount = await _messageRepository.GetMessageCountAsync(conv.Id);
                dtos.Add(MapToDto(conv, messageCount));
            }

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations for profile {ProfileId}", profileId);
            throw;
        }
    }

    public async Task<(ConversationDto Conversation, List<ChatMessageDto> Messages)> GetConversationWithMessagesAsync(
        Guid conversationId, 
        int page = 1, 
        int pageSize = 50)
    {
        try
        {
            var conversation = await _conversationRepository.GetConversationWithMessagesAsync(conversationId);
            if (conversation == null)
            {
                throw new InvalidOperationException($"Conversation {conversationId} not found");
            }

            var messages = await _messageRepository.GetConversationMessagesAsync(conversationId, page, pageSize);
            var messageCount = await _messageRepository.GetMessageCountAsync(conversationId);

            var conversationDto = MapToDto(conversation, messageCount);
            var messageDtos = messages.Select(MapMessageToDto).ToList();

            return (conversationDto, messageDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation {ConversationId} with messages", conversationId);
            throw;
        }
    }

    public async Task<ChatResponseDto> SendMessageAsync(SendMessageDto dto, Guid profileId)
    {
        try
        {
            // Verify conversation belongs to profile
            var conversation = await _conversationRepository.GetConversationWithMessagesAsync(dto.ConversationId);
            if (conversation == null)
            {
                throw new InvalidOperationException($"Conversation {dto.ConversationId} not found");
            }

            if (conversation.ProfileId != profileId)
            {
                throw new UnauthorizedAccessException("Conversation does not belong to this profile");
            }

            // Check message limit
            var messageCount = await _messageRepository.GetMessageCountAsync(dto.ConversationId);
            if (messageCount >= _options.MaxMessagesPerConversation)
            {
                throw new InvalidOperationException($"Conversation has reached maximum message limit of {_options.MaxMessagesPerConversation}");
            }

            // Save user message
            var userMessageOrder = await _messageRepository.GetNextMessageOrderAsync(dto.ConversationId);
            var userMessage = new EntityChatMessage
            {
                ConversationId = dto.ConversationId,
                Role = "user",
                Content = dto.Content,
                MessageOrder = userMessageOrder
            };

            var savedUserMessage = await _messageRepository.AddAsync(userMessage);

            // Set current profile for function calls
            _functionService.SetCurrentProfile(profileId);

            // Build chat history for context
            var chatHistory = BuildChatHistory(conversation);
            chatHistory.Add(new AiChatMessage(ChatRole.User, dto.Content));

            // Create AI functions
            var functions = new[]
            {
                AIFunctionFactory.Create(_functionService.SearchProfiles),
                AIFunctionFactory.Create(_functionService.SearchPosts),
                AIFunctionFactory.Create(_functionService.GetPostDetails),
                AIFunctionFactory.Create(_functionService.FollowProfile),
                AIFunctionFactory.Create(_functionService.UnfollowProfile),
                AIFunctionFactory.Create(_functionService.GetMyProfile)
            };

            // Get AI response with function calling support
            var chatOptions = new ChatOptions
            {
                MaxOutputTokens = _options.MaxTokens,
                Temperature = (float)_options.Temperature,
                Tools = functions
            };

            var response = await _chatClient.CompleteAsync(chatHistory, chatOptions);

            // Save assistant message
            var assistantMessageOrder = await _messageRepository.GetNextMessageOrderAsync(dto.ConversationId);
            var assistantMessage = new EntityChatMessage
            {
                ConversationId = dto.ConversationId,
                Role = "assistant",
                Content = response.Message.Text ?? string.Empty,
                StructuredResponse = null, // We'll add structured responses in Phase 3
                MessageOrder = assistantMessageOrder
            };

            var savedAssistantMessage = await _messageRepository.AddAsync(assistantMessage);

            // Update conversation last message time
            await _conversationRepository.UpdateLastMessageTimeAsync(dto.ConversationId);

            _logger.LogInformation("Processed message in conversation {ConversationId}", dto.ConversationId);

            return new ChatResponseDto
            {
                UserMessage = MapMessageToDto(savedUserMessage),
                AssistantMessage = MapMessageToDto(savedAssistantMessage),
                ConversationId = dto.ConversationId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message in conversation {ConversationId}", dto.ConversationId);
            throw;
        }
    }

    public async Task<bool> DeleteConversationAsync(Guid conversationId, Guid profileId)
    {
        try
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                return false;
            }

            if (conversation.ProfileId != profileId)
            {
                throw new UnauthorizedAccessException("Conversation does not belong to this profile");
            }

            await _conversationRepository.DeleteAsync(conversationId);
            
            _logger.LogInformation("Deleted conversation {ConversationId}", conversationId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<bool> SetActiveConversationAsync(Guid conversationId, Guid profileId)
    {
        try
        {
            var result = await _conversationRepository.SetActiveConversationAsync(conversationId, profileId);
            
            if (result)
            {
                _logger.LogInformation("Set conversation {ConversationId} as active for profile {ProfileId}", 
                    conversationId, profileId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<ConversationDto?> GetActiveConversationAsync(Guid profileId)
    {
        try
        {
            var conversation = await _conversationRepository.GetActiveConversationAsync(profileId);
            if (conversation == null)
            {
                return null;
            }

            var messageCount = await _messageRepository.GetMessageCountAsync(conversation.Id);
            return MapToDto(conversation, messageCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active conversation for profile {ProfileId}", profileId);
            throw;
        }
    }

    // Helper methods

    private List<AiChatMessage> BuildChatHistory(Conversation conversation)
    {
        var history = new List<AiChatMessage>();

        // Add system message
        history.Add(new AiChatMessage(ChatRole.System, 
            "You are a helpful AI assistant for the Sivar social network platform. " +
            "You can help users find profiles, search posts, and perform social actions. " +
            "Respond in a friendly and concise manner."));

        // Add conversation messages
        var messages = conversation.Messages.OrderBy(m => m.MessageOrder).ToList();
        foreach (var msg in messages)
        {
            var role = msg.Role.ToLower() == "user" ? ChatRole.User : ChatRole.Assistant;
            history.Add(new AiChatMessage(role, msg.Content));
        }

        return history;
    }

    private ConversationDto MapToDto(Conversation conversation, int messageCount)
    {
        return new ConversationDto
        {
            Id = conversation.Id,
            ProfileId = conversation.ProfileId,
            Title = conversation.Title,
            LastMessageAt = conversation.LastMessageAt,
            IsActive = conversation.IsActive,
            CreatedAt = conversation.CreatedAt,
            MessageCount = messageCount
        };
    }

    private ChatMessageDto MapMessageToDto(EntityChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Role = message.Role,
            Content = message.Content,
            StructuredResponse = message.StructuredResponse,
            MessageOrder = message.MessageOrder,
            CreatedAt = message.CreatedAt
        };
    }
}
