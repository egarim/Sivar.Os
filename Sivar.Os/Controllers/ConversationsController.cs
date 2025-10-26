using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;


namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for managing AI chat conversations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IProfileService _profileService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IConversationRepository conversationRepository,
        IChatMessageRepository chatMessageRepository,
        IProfileService profileService,
        ILogger<ConversationsController> logger)
    {
        _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
        _chatMessageRepository = chatMessageRepository ?? throw new ArgumentNullException(nameof(chatMessageRepository));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all conversations for a specific profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <returns>List of conversations</returns>
    [HttpGet("profiles/{profileId:guid}")]
    public async Task<ActionResult<IEnumerable<ConversationDto>>> GetProfileConversations(Guid profileId)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            // Verify profile ownership
            var profile = await _profileService.GetPublicProfileAsync(profileId);
            if (profile == null)
                return NotFound("Profile not found");

            // TODO: Add proper ownership validation when authentication is implemented
            // For now, we'll just check if the profile exists

            var conversations = await _conversationRepository.GetProfileConversationsAsync(profileId);
            
            var conversationDtos = new List<ConversationDto>();
            foreach (var conversation in conversations)
            {
                var messageCount = await _chatMessageRepository.GetMessageCountAsync(conversation.Id);
                
                conversationDtos.Add(new ConversationDto
                {
                    Id = conversation.Id,
                    ProfileId = conversation.ProfileId,
                    Title = conversation.Title,
                    LastMessageAt = conversation.LastMessageAt,
                    IsActive = conversation.IsActive,
                    CreatedAt = conversation.CreatedAt,
                    MessageCount = messageCount
                });
            }

            return Ok(conversationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations for profile {ProfileId}", profileId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all messages in a conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>List of chat messages</returns>
    [HttpGet("{conversationId:guid}/messages")]
    public async Task<ActionResult<IEnumerable<ChatMessageDto>>> GetConversationMessages(Guid conversationId)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
                return NotFound("Conversation not found");

            // TODO: Verify user has access to this conversation
            // For now, we'll just check if conversation exists

            var messages = await _chatMessageRepository.GetConversationMessagesAsync(conversationId);
            
            var messageDtos = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                Role = m.Role,
                Content = m.Content,
                StructuredResponse = m.StructuredResponse,
                MessageOrder = m.MessageOrder,
                CreatedAt = m.CreatedAt
            }).ToList();

            return Ok(messageDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new conversation for a profile
    /// </summary>
    /// <param name="createDto">Conversation creation data</param>
    /// <returns>Created conversation</returns>
    [HttpPost]
    public async Task<ActionResult<ConversationDto>> CreateConversation([FromBody] CreateConversationDto createDto)
    {
        try
        {
            if (createDto == null)
                return BadRequest("Conversation data is required");

            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            // Verify profile exists
            var profile = await _profileService.GetPublicProfileAsync(createDto.ProfileId);
            if (profile == null)
                return NotFound("Profile not found");

            // TODO: Verify user owns this profile when authentication is implemented

            var conversation = new Conversation
            {
                ProfileId = createDto.ProfileId,
                Title = createDto.Title,
                IsActive = true,
                LastMessageAt = DateTime.UtcNow
            };

            await _conversationRepository.AddAsync(conversation);
            await _conversationRepository.SaveChangesAsync();

            var conversationDto = new ConversationDto
            {
                Id = conversation.Id,
                ProfileId = conversation.ProfileId,
                Title = conversation.Title,
                LastMessageAt = conversation.LastMessageAt,
                IsActive = conversation.IsActive,
                CreatedAt = conversation.CreatedAt,
                MessageCount = 0
            };

            return CreatedAtAction(
                nameof(GetConversationMessages),
                new { conversationId = conversation.Id },
                conversationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates a conversation's title
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="updateDto">Update data containing new title</param>
    /// <returns>Updated conversation</returns>
    [HttpPut("{conversationId:guid}")]
    public async Task<ActionResult<ConversationDto>> UpdateConversationTitle(
        Guid conversationId,
        [FromBody] UpdateConversationDto updateDto)
    {
        try
        {
            if (updateDto == null)
                return BadRequest("Update data is required");

            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
                return NotFound("Conversation not found");

            // TODO: Verify user owns this conversation when authentication is implemented

            conversation.Title = updateDto.Title;
            await _conversationRepository.UpdateAsync(conversation);
            await _conversationRepository.SaveChangesAsync();

            var messageCount = await _chatMessageRepository.GetMessageCountAsync(conversation.Id);
            
            var conversationDto = new ConversationDto
            {
                Id = conversation.Id,
                ProfileId = conversation.ProfileId,
                Title = conversation.Title,
                LastMessageAt = conversation.LastMessageAt,
                IsActive = conversation.IsActive,
                CreatedAt = conversation.CreatedAt,
                MessageCount = messageCount
            };

            return Ok(conversationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversation {ConversationId}", conversationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Soft deletes a conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{conversationId:guid}")]
    public async Task<ActionResult> DeleteConversation(Guid conversationId)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized("User not authenticated");

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
                return NotFound("Conversation not found");

            // TODO: Verify user owns this conversation when authentication is implemented

            await _conversationRepository.DeleteAsync(conversationId);
            await _conversationRepository.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation {ConversationId}", conversationId);
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
/// DTO for updating conversation title
/// </summary>
public record UpdateConversationDto
{
    /// <summary>
    /// New title for the conversation
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;
}
