using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Agents.AI;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;
using System.Text.Json;
using AiChatMessage = Microsoft.Extensions.AI.ChatMessage;
using EntityChatMessage = Sivar.Os.Shared.Entities.ChatMessage;

namespace Sivar.Os.Services;

/// <summary>
/// Service for managing AI chat conversations using Microsoft Agent Framework.
/// Phase 6: Now includes intent-based routing for better query handling.
/// Phase 10: Now uses AgentFactory for dynamic agent loading.
/// Phase 11: Token allowance tracking and enforcement.
/// </summary>
public class ChatService : IChatService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IChatTokenUsageRepository _tokenUsageRepository;
    private readonly IAgentFactory _agentFactory;
    private readonly ChatFunctionService _functionService;
    private readonly IIntentClassifier _intentClassifier;
    private readonly ChatServiceOptions _options;
    private readonly ISearchResultService _searchResultService;
    private readonly IAiCostService _aiCostService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IConversationRepository conversationRepository,
        IChatMessageRepository messageRepository,
        IProfileRepository profileRepository,
        IChatTokenUsageRepository tokenUsageRepository,
        IAgentFactory agentFactory,
        ChatFunctionService functionService,
        IIntentClassifier intentClassifier,
        IOptions<ChatServiceOptions> options,
        ISearchResultService searchResultService,
        IAiCostService aiCostService,
        ILogger<ChatService> logger)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _tokenUsageRepository = tokenUsageRepository ?? throw new ArgumentNullException(nameof(tokenUsageRepository));
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _functionService = functionService ?? throw new ArgumentNullException(nameof(functionService));
        _intentClassifier = intentClassifier ?? throw new ArgumentNullException(nameof(intentClassifier));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _searchResultService = searchResultService ?? throw new ArgumentNullException(nameof(searchResultService));
        _aiCostService = aiCostService ?? throw new ArgumentNullException(nameof(aiCostService));
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
            
            // CRITICAL: Must save changes to persist the conversation to the database
            await _conversationRepository.SaveChangesAsync();
            
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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ChatService.SendMessageAsync] START - RequestId={RequestId}, ConversationId={ConversationId}, ProfileId={ProfileId}, MessageLength={Length}", 
            requestId, dto?.ConversationId, profileId, dto?.Content?.Length ?? 0);

        try
        {
            if (dto == null)
            {
                _logger.LogWarning("[ChatService.SendMessageAsync] NULL_DTO - RequestId={RequestId}", requestId);
                throw new ArgumentNullException(nameof(dto));
            }

            // Verify conversation belongs to profile
            var conversation = await _conversationRepository.GetConversationWithMessagesAsync(dto.ConversationId);
            if (conversation == null)
            {
                _logger.LogWarning("[ChatService.SendMessageAsync] CONVERSATION_NOT_FOUND - ConversationId={ConversationId}, RequestId={RequestId}", 
                    dto.ConversationId, requestId);
                throw new InvalidOperationException($"Conversation {dto.ConversationId} not found");
            }

            if (conversation.ProfileId != profileId)
            {
                _logger.LogWarning("[ChatService.SendMessageAsync] UNAUTHORIZED - ConversationId={ConversationId}, ProfileId={ProfileId}, RequestId={RequestId}", 
                    dto.ConversationId, profileId, requestId);
                throw new UnauthorizedAccessException("Conversation does not belong to this profile");
            }

            // Phase 11: Token Allowance Check - Get profile and verify token allowance
            var profile = await _profileRepository.GetByIdAsync(profileId);
            if (profile == null)
            {
                _logger.LogWarning("[ChatService.SendMessageAsync] PROFILE_NOT_FOUND - ProfileId={ProfileId}, RequestId={RequestId}", 
                    profileId, requestId);
                throw new InvalidOperationException($"Profile {profileId} not found");
            }

            // Check and reset token period if needed, then verify allowance
            profile.CheckAndResetTokenPeriodIfNeeded();
            if (!profile.HasTokenAllowance())
            {
                _logger.LogWarning("[ChatService.SendMessageAsync] TOKEN_ALLOWANCE_EXCEEDED - ProfileId={ProfileId}, Limit={Limit}, Used={Used}, ResetsAt={ResetsAt}, RequestId={RequestId}", 
                    profileId, profile.TokenAllowanceLimit, profile.TokensUsedThisPeriod, profile.TokenAllowanceResetsAt, requestId);
                throw new InvalidOperationException($"Token allowance exceeded. Your allowance of {profile.TokenAllowanceLimit:N0} tokens resets on {profile.TokenAllowanceResetsAt:g} UTC.");
            }

            _logger.LogInformation("[ChatService.SendMessageAsync] Token allowance verified - ProfileId={ProfileId}, Remaining={Remaining}, RequestId={RequestId}", 
                profileId, profile.TokensRemaining, requestId);

            _logger.LogInformation("[ChatService.SendMessageAsync] Conversation verified - ConversationId={ConversationId}, RequestId={RequestId}", 
                conversation.Id, requestId);

            // Check message limit
            var messageCount = await _messageRepository.GetMessageCountAsync(dto.ConversationId);
            if (messageCount >= _options.MaxMessagesPerConversation)
            {
                _logger.LogWarning("[ChatService.SendMessageAsync] MESSAGE_LIMIT_REACHED - ConversationId={ConversationId}, MessageCount={Count}, Limit={Limit}, RequestId={RequestId}", 
                    dto.ConversationId, messageCount, _options.MaxMessagesPerConversation, requestId);
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
            await _messageRepository.SaveChangesAsync(); // Persist user message immediately
            
            _logger.LogInformation("[ChatService.SendMessageAsync] User message saved - MessageId={MessageId}, Order={Order}, RequestId={RequestId}", 
                savedUserMessage.Id, userMessageOrder, requestId);

            // Set current profile for function calls
            _functionService.SetCurrentProfile(profileId);

            // Set location context for proximity-aware searches (Phase 0)
            if (dto.Location != null && dto.Location.IsValid)
            {
                _functionService.SetCurrentLocation(dto.Location.Latitude, dto.Location.Longitude);
                _logger.LogInformation("[ChatService.SendMessageAsync] Location context set - City={City}, Lat={Lat}, Lng={Lng}, RequestId={RequestId}", 
                    dto.Location.City, dto.Location.Latitude, dto.Location.Longitude, requestId);
            }
            else
            {
                _functionService.SetCurrentLocation(null, null);
                _logger.LogDebug("[ChatService.SendMessageAsync] No location context provided - RequestId={RequestId}", requestId);
            }

            // Phase 2: Clear last search results before AI agent call
            _functionService.ClearLastSearchResults();

            // Phase 6: Intent Classification - Classify user intent before AI agent call
            var intentClassification = _intentClassifier.ClassifyIntent(dto.Content);
            _logger.LogInformation("[ChatService.SendMessageAsync] Intent classified - Intent={Intent}, Confidence={Confidence:F2}, Entity={Entity}, RequestId={RequestId}",
                intentClassification.Intent, intentClassification.Confidence, intentClassification.Entity, requestId);

            // Build chat history for context (include location for AI awareness)
            var chatHistory = BuildChatHistory(conversation, dto.Location);
            
            // Phase 6: Enhance user message with intent context for better AI routing
            var enhancedContent = EnhanceMessageWithIntent(dto.Content, intentClassification);
            chatHistory.Add(new AiChatMessage(ChatRole.User, enhancedContent));
            
            _logger.LogInformation("[ChatService.SendMessageAsync] Chat history built - HistoryCount={Count}, RequestId={RequestId}", 
                chatHistory.Count, requestId);

            _logger.LogInformation("[ChatService.SendMessageAsync] Calling AI Agent - Intent={Intent}, RequestId={RequestId}", 
                intentClassification.Intent, requestId);

            var aiStartTime = DateTime.UtcNow;
            
            // Phase 10: Get agent based on intent (routes to specialized agents)
            var agent = await _agentFactory.GetAgentForIntentAsync(dto.Content);
            
            // Use AIAgent.RunAsync with chat history - agent already has tools configured
            var agentResponse = await agent.RunAsync(chatHistory);
            var aiElapsed = (DateTime.UtcNow - aiStartTime).TotalMilliseconds;
            
            // Phase 11: Extract token usage from agent response
            var inputTokens = (int)(agentResponse.Usage?.InputTokenCount ?? 0);
            var outputTokens = (int)(agentResponse.Usage?.OutputTokenCount ?? 0);
            var totalTokens = (int)(agentResponse.Usage?.TotalTokenCount ?? (inputTokens + outputTokens));
            
            _logger.LogInformation("[ChatService.SendMessageAsync] Token usage - Input={InputTokens}, Output={OutputTokens}, Total={TotalTokens}, RequestId={RequestId}", 
                inputTokens, outputTokens, totalTokens, requestId);

            // Phase 11: Record token usage and update profile allowance
            if (totalTokens > 0)
            {
                // Update profile token usage
                profile.TokensUsedThisPeriod += totalTokens;
                profile.TotalTokensUsed += totalTokens;
                profile.UpdatedAt = DateTime.UtcNow;
                await _profileRepository.UpdateAsync(profile);
                await _profileRepository.SaveChangesAsync();

                // Determine model name based on provider
                var modelName = _options.Provider.Equals("openai", StringComparison.OrdinalIgnoreCase) 
                    ? _options.OpenAI.ModelId 
                    : _options.Ollama.ModelId;

                // Calculate estimated cost for this interaction
                decimal? estimatedCost = null;
                try
                {
                    var costResult = await _aiCostService.CalculateCostAsync(modelName, inputTokens, outputTokens);
                    estimatedCost = costResult.TotalCost;
                    _logger.LogDebug("[ChatService] Cost calculated - Model={Model}, Input={Input}, Output={Output}, Cost=${Cost:F6}",
                        modelName, inputTokens, outputTokens, estimatedCost);
                }
                catch (Exception costEx)
                {
                    _logger.LogWarning(costEx, "[ChatService] Failed to calculate cost for model {Model}", modelName);
                }

                // Create audit record with cost
                var tokenUsageRecord = new ChatTokenUsage
                {
                    ProfileId = profileId,
                    ConversationId = dto.ConversationId,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    TotalTokens = totalTokens,
                    ModelName = modelName,
                    Intent = intentClassification.Intent.ToString(),
                    MessagePreview = dto.Content.Length > 100 ? dto.Content.Substring(0, 100) : dto.Content,
                    EstimatedCost = estimatedCost,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _tokenUsageRepository.AddAsync(tokenUsageRecord);
                await _tokenUsageRepository.SaveChangesAsync();

                // Update conversation totals (denormalized for performance)
                conversation.TotalInputTokens += inputTokens;
                conversation.TotalOutputTokens += outputTokens;
                conversation.TotalTokens += totalTokens;
                conversation.TotalCost += estimatedCost ?? 0m;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _conversationRepository.UpdateAsync(conversation);
                await _conversationRepository.SaveChangesAsync();

                _logger.LogInformation("[ChatService.SendMessageAsync] Token usage recorded - ProfileId={ProfileId}, TotalUsedThisPeriod={Used}, Remaining={Remaining}, RequestId={RequestId}", 
                    profileId, profile.TokensUsedThisPeriod, profile.TokensRemaining, requestId);
            }

            // Extract text response from agent
            var responseText = agentResponse.ToString() ?? string.Empty;
            
            _logger.LogInformation("[ChatService.SendMessageAsync] AI Agent response received - ResponseLength={Length}, AIDuration={Duration}ms, Intent={Intent}, RequestId={RequestId}", 
                responseText.Length, aiElapsed, intentClassification.Intent, requestId);

            // Save assistant message
            var assistantMessageOrder = await _messageRepository.GetNextMessageOrderAsync(dto.ConversationId);
            var assistantMessage = new EntityChatMessage
            {
                ConversationId = dto.ConversationId,
                Role = "assistant",
                Content = responseText,
                StructuredResponse = null, // We'll add structured responses in Phase 3
                MessageOrder = assistantMessageOrder
            };

            var savedAssistantMessage = await _messageRepository.AddAsync(assistantMessage);
            await _messageRepository.SaveChangesAsync(); // Persist assistant message
            
            _logger.LogInformation("[ChatService.SendMessageAsync] Assistant message saved - MessageId={MessageId}, Order={Order}, RequestId={RequestId}", 
                savedAssistantMessage.Id, assistantMessageOrder, requestId);

            // Update conversation last message time
            await _conversationRepository.UpdateLastMessageTimeAsync(dto.ConversationId);

            // Phase 2: Unified Structured Search Pipeline
            // First, check if the AI agent's function calls produced structured results
            SearchResultsCollectionDto? structuredResults = _functionService.LastSearchResults;
            
            if (structuredResults?.HasResults == true)
            {
                _logger.LogInformation("[ChatService.SendMessageAsync] Phase 2: AI Agent function call returned {Count} structured results - RequestId={RequestId}", 
                    structuredResults.TotalCount, requestId);
            }
            else if (IsSearchQuery(dto.Content))
            {
                // Fallback: If the AI agent didn't call a search function but this looks like a search query,
                // perform a hybrid search (this maintains backward compatibility during transition)
                try
                {
                    _logger.LogInformation("[ChatService.SendMessageAsync] Phase 2 fallback: Performing hybrid search for query - RequestId={RequestId}", requestId);
                    structuredResults = await _searchResultService.HybridSearchAsync(new HybridSearchRequestDto
                    {
                        Query = dto.Content,
                        Limit = 10,
                        SemanticWeight = 0.5f,
                        FullTextWeight = 0.3f,
                        GeoWeight = 0.2f
                    });

                    if (structuredResults?.HasResults == true)
                    {
                        _logger.LogInformation("[ChatService.SendMessageAsync] Phase 2 fallback: Hybrid search returned {Count} results - RequestId={RequestId}", 
                            structuredResults.TotalCount, requestId);
                    }
                }
                catch (Exception searchEx)
                {
                    _logger.LogWarning(searchEx, "[ChatService.SendMessageAsync] Phase 2 fallback: Structured search failed, continuing with text response - RequestId={RequestId}", requestId);
                    // Don't fail the whole request, just don't include structured results
                }
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatService.SendMessageAsync] SUCCESS - ConversationId={ConversationId}, UserMessageId={UserMsgId}, AIMessageId={AIMsgId}, HasStructuredResults={HasResults}, RequestId={RequestId}, TotalDuration={Duration}ms, AIDuration={AIDuration}ms", 
                dto.ConversationId, savedUserMessage.Id, savedAssistantMessage.Id, structuredResults?.HasResults == true, requestId, elapsed, aiElapsed);

            return new ChatResponseDto
            {
                UserMessage = MapMessageToDto(savedUserMessage),
                AssistantMessage = MapMessageToDto(savedAssistantMessage),
                ConversationId = dto.ConversationId,
                SearchResults = structuredResults,
                // Phase 11: Token usage information
                InputTokens = inputTokens > 0 ? inputTokens : null,
                OutputTokens = outputTokens > 0 ? outputTokens : null,
                TotalTokens = totalTokens > 0 ? totalTokens : null,
                TokensRemaining = profile.TokensRemaining,
                AllowanceResetsAt = profile.TokenAllowanceResetsAt
            };
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatService.SendMessageAsync] ERROR - ConversationId={ConversationId}, RequestId={RequestId}, Duration={Duration}ms", 
                dto?.ConversationId, requestId, elapsed);
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

    private List<AiChatMessage> BuildChatHistory(Conversation conversation, ChatLocationContext? location = null)
    {
        var history = new List<AiChatMessage>();

        // Build system message with location context and language rules
        var systemPrompt = "You are a helpful AI assistant for the Sivar social network platform. " +
            "You can help users find profiles, search posts, and perform social actions. " +
            "Respond in a friendly and concise manner. " +
            "IMPORTANT: When showing links, always use RELATIVE URLs (starting with /) not absolute URLs. " +
            "For example, use '/post/abc-123' not 'https://example.com/post/abc-123'." +
            "\n\nLANGUAGE: ALWAYS respond in the SAME LANGUAGE as the user's LAST message. " +
            "If they write in Spanish, respond in Spanish. If they write in English, respond in English. " +
            "If they write in Russian, respond in Russian. Mirror their language exactly, regardless of system settings.";

        // Add location context to system prompt if available
        if (location != null && location.IsValid)
        {
            systemPrompt += $"\n\nLOCATION CONTEXT: The user is currently located in {location.City}, {location.State ?? location.Country}. " +
                "When searching for businesses, restaurants, or services:\n" +
                $"1. If the user specifies a DIFFERENT city in their query (e.g., 'pizza en Santa Tecla'), use THAT city instead.\n" +
                $"2. If the user does NOT specify a city (e.g., 'busco pizzerías'), use their current location '{location.City}' as the default.\n" +
                "3. Do NOT ask which city they want - either use the city they mentioned OR default to their current location.";
        }

        // Add system message
        history.Add(new AiChatMessage(ChatRole.System, systemPrompt));

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

    /// <summary>
    /// Determines if a user query is a search-type query that should return structured results
    /// </summary>
    private bool IsSearchQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;

        var lowerQuery = query.ToLowerInvariant();

        // Spanish search keywords
        var spanishKeywords = new[]
        {
            "buscar", "busca", "encuentra", "encontrar", "dónde", "donde",
            "cerca", "cercano", "cercanos", "cercana", "cercanas",
            "recomienda", "recomendaciones", "sugerir", "sugiere",
            "mejor", "mejores", "top", "popular", "populares",
            "restaurante", "restaurantes", "comida", "comer",
            "tienda", "tiendas", "negocio", "negocios",
            "hotel", "hoteles", "hospedaje",
            "banco", "bancos", "farmacia", "farmacias",
            "trámite", "tramite", "trámites", "tramites",
            "servicio", "servicios", "evento", "eventos",
            "playa", "playas", "turismo", "turistico", "turístico",
            "pupusa", "pupusas", "típico", "tipico", "salvadoreño",
            "café", "cafetería", "cafeteria",
            "gobierno", "alcaldía", "ministerio"
        };

        // English search keywords
        var englishKeywords = new[]
        {
            "find", "search", "looking for", "where", "nearby", "near me",
            "recommend", "recommendations", "suggest", "best", "top",
            "restaurant", "restaurants", "food", "eat",
            "store", "stores", "business", "businesses",
            "hotel", "hotels", "lodging",
            "bank", "banks", "pharmacy", "pharmacies",
            "procedure", "procedures", "service", "services",
            "event", "events", "beach", "beaches", "tourism"
        };

        return spanishKeywords.Any(k => lowerQuery.Contains(k)) ||
               englishKeywords.Any(k => lowerQuery.Contains(k));
    }

    /// <summary>
    /// Phase 6: Enhances the user message with intent hints for better AI agent routing.
    /// This helps the AI choose the most appropriate function to call.
    /// </summary>
    private string EnhanceMessageWithIntent(string originalMessage, IntentClassificationDto intent)
    {
        // For high-confidence intents, add a hint to guide the AI agent
        if (intent.Confidence < 0.7f)
        {
            return originalMessage; // Low confidence, let AI decide
        }

        var hint = intent.Intent switch
        {
            UserIntent.ContactLookup => 
                $"[INTENT: User wants contact information. Use GetContactInfo function.]\n{originalMessage}",
            
            UserIntent.HoursQuery => 
                $"[INTENT: User wants business hours. Use GetBusinessHours function.]\n{originalMessage}",
            
            UserIntent.DirectionsRequest => 
                $"[INTENT: User wants location/directions. Use GetDirections function.]\n{originalMessage}",
            
            UserIntent.ProcedureHelp => 
                $"[INTENT: User wants procedure/requirements help. Use GetProcedureInfo function.]\n{originalMessage}",
            
            UserIntent.BusinessSearch => 
                $"[INTENT: User searching for businesses/places. Use SearchPosts or FindBusinesses function.]\n{originalMessage}",
            
            UserIntent.EventSearch => 
                $"[INTENT: User searching for events. Use SearchPosts function with event focus.]\n{originalMessage}",
            
            UserIntent.Greeting => 
                originalMessage, // No hint needed for greetings
            
            UserIntent.GeneralQuestion => 
                originalMessage, // Let AI handle naturally
            
            _ => originalMessage
        };

        return hint;
    }
}
