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
        try
        {
            if (messageContent == null)
                return BadRequest("Message content is required");

            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            // Verify conversation exists
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
                return NotFound("Conversation not found");

            // Verify profile exists and user has access
            var profile = await _profileService.GetPublicProfileAsync(conversation.ProfileId);
            if (profile == null)
                return NotFound("Profile not found");

            // TODO: Verify user owns this profile when authentication is implemented

            // Create the SendMessageDto with conversationId
            var sendDto = new SendMessageDto
            {
                ConversationId = conversationId,
                Content = messageContent.Content
            };

            // Send message to ChatService
            var response = await _chatService.SendMessageAsync(sendDto, conversation.ProfileId);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when sending message to conversation {ConversationId}", conversationId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to conversation {ConversationId}", conversationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Placeholder method to extract Keycloak ID from JWT token
    /// TODO: Implement actual JWT token parsing when Keycloak is integrated
    /// </summary>
    private string GetKeycloakIdFromRequest()
    {
        // For development/testing purposes, return a mock value
        // In production, this would extract the "sub" claim from the JWT token
        return "mock-keycloak-user-id";
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
}
