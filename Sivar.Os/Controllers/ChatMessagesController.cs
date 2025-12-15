using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for sending messages to AI chat
/// </summary>
[ApiController]
[Route("api/conversations/{conversationId:guid}/[controller]")]
public class ChatMessagesController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IProfileService _profileService;
    private readonly ILogger<ChatMessagesController> _logger;

    public ChatMessagesController(
        IChatService chatService,
        IConversationRepository conversationRepository,
        IProfileService profileService,
        ILogger<ChatMessagesController> logger)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a message to the AI chat and receives a response
    /// </summary>
    /// <param name="conversationId">Conversation ID from route</param>
    /// <param name="messageContent">Message content DTO</param>
    /// <returns>Chat response with user message and AI response</returns>
    [HttpPost]
    public async Task<ActionResult<ChatResponseDto>> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageContentDto messageContent)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ChatMessagesController.SendMessage] START - RequestId={RequestId}, ConversationId={ConversationId}, MessageLength={Length}", 
            requestId, conversationId, messageContent?.Content?.Length ?? 0);

        try
        {
            if (messageContent == null)
            {
                _logger.LogWarning("[ChatMessagesController.SendMessage] BAD_REQUEST - Null message content, RequestId={RequestId}", requestId);
                return BadRequest("Message content is required");
            }

            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ChatMessagesController.SendMessage] KeycloakId: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ChatMessagesController.SendMessage] UNAUTHORIZED - RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            // Verify conversation exists
            _logger.LogInformation("[ChatMessagesController.SendMessage] Verifying conversation - RequestId={RequestId}", requestId);
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ChatMessagesController.SendMessage] CONVERSATION_NOT_FOUND - ConversationId={ConversationId}, RequestId={RequestId}, Duration={Duration}ms", 
                    conversationId, requestId, elapsed);
                return NotFound("Conversation not found");
            }

            // Verify profile exists and user has access
            var profile = await _profileService.GetPublicProfileAsync(conversation.ProfileId);
            if (profile == null)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ChatMessagesController.SendMessage] PROFILE_NOT_FOUND - ProfileId={ProfileId}, RequestId={RequestId}, Duration={Duration}ms", 
                    conversation.ProfileId, requestId, elapsed);
                return NotFound("Profile not found");
            }

            // TODO: Verify user owns this profile when authentication is implemented

            // Create the SendMessageDto with conversationId and location
            var sendDto = new SendMessageDto
            {
                ConversationId = conversationId,
                Content = messageContent.Content,
                Location = messageContent.Location // Pass through location for proximity-aware searches
            };

            // Send message to ChatService
            _logger.LogInformation("[ChatMessagesController.SendMessage] Sending to AI service - RequestId={RequestId}", requestId);
            var response = await _chatService.SendMessageAsync(sendDto, conversation.ProfileId);

            var successElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ChatMessagesController.SendMessage] SUCCESS - ConversationId={ConversationId}, RequestId={RequestId}, Duration={Duration}ms", 
                conversationId, requestId, successElapsed);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogWarning(ex, "[ChatMessagesController.SendMessage] INVALID_OPERATION - ConversationId={ConversationId}, RequestId={RequestId}, Duration={Duration}ms", 
                conversationId, requestId, elapsed);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ChatMessagesController.SendMessage] ERROR - ConversationId={ConversationId}, RequestId={RequestId}, Duration={Duration}ms", 
                conversationId, requestId, elapsed);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Placeholder method to extract Keycloak ID from JWT token
    /// TODO: Implement actual JWT token parsing when Keycloak is integrated
    /// </summary>
    private string GetKeycloakIdFromRequest()
    {
        // Check for mock authentication header (for integration tests)
        if (Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            return keycloakIdHeader.ToString();
        }

        // Check if user is authenticated via claims
        if (User?.Identity?.IsAuthenticated == true)
        {
            var subClaim = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(subClaim))
            {
                return subClaim;
            }

            // Fallback: try to find "user_id" or "id" claims if "sub" is not available
            var userIdClaim = User.FindFirst("user_id")?.Value 
                           ?? User.FindFirst("id")?.Value 
                           ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                return userIdClaim;
            }
        }

        // Only return fallback if we have mock auth header (X-Mock-Auth) indicating this is a test scenario
        if (Request.Headers.ContainsKey("X-Mock-Auth"))
        {
            return "mock-keycloak-user-id";
        }

        // No authentication found
        return null!;
    }
}

/// <summary>
/// DTO for sending just the message content (conversationId comes from route)
/// </summary>
public record SendMessageContentDto
{
    /// <summary>
    /// User's message content (text only)
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(5000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Optional location context for proximity-aware searches.
    /// </summary>
    public ChatLocationContext? Location { get; init; }
}
