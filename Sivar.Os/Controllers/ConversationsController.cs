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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ConversationsController.GetProfileConversations] START - RequestId={RequestId}, ProfileId={ProfileId}", 
            requestId, profileId);

        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ConversationsController.GetProfileConversations] KeycloakId: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ConversationsController.GetProfileConversations] UNAUTHORIZED - RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            // Verify profile ownership
            var profile = await _profileService.GetPublicProfileAsync(profileId);
            if (profile == null)
            {
                var elapsedNotFound = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ConversationsController.GetProfileConversations] PROFILE_NOT_FOUND - ProfileId={ProfileId}, RequestId={RequestId}, Duration={Duration}ms", 
                    profileId, requestId, elapsedNotFound);
                return NotFound("Profile not found");
            }

            // TODO: Add proper ownership validation when authentication is implemented
            // For now, we'll just check if the profile exists

            var conversations = await _conversationRepository.GetProfileConversationsAsync(profileId);
            _logger.LogInformation("[ConversationsController.GetProfileConversations] Retrieved {Count} conversations - RequestId={RequestId}", 
                conversations.Count(), requestId);
            
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

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ConversationsController.GetProfileConversations] SUCCESS - ProfileId={ProfileId}, ConversationCount={Count}, RequestId={RequestId}, Duration={Duration}ms", 
                profileId, conversationDtos.Count, requestId, elapsed);

            return Ok(conversationDtos);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ConversationsController.GetProfileConversations] ERROR - ProfileId={ProfileId}, RequestId={RequestId}, Duration={Duration}ms", 
                profileId, requestId, elapsed);
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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ConversationsController.GetConversationMessages] START - RequestId={RequestId}, ConversationId={ConversationId}", 
            requestId, conversationId);

        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ConversationsController.GetConversationMessages] KeycloakId: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ConversationsController.GetConversationMessages] UNAUTHORIZED - RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                var elapsedNotFound = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ConversationsController.GetConversationMessages] CONVERSATION_NOT_FOUND - ConversationId={ConversationId}, RequestId={RequestId}, Duration={Duration}ms", 
                    conversationId, requestId, elapsedNotFound);
                return NotFound("Conversation not found");
            }

            // TODO: Verify user has access to this conversation
            // For now, we'll just check if conversation exists

            var messages = await _chatMessageRepository.GetConversationMessagesAsync(conversationId);
            _logger.LogInformation("[ConversationsController.GetConversationMessages] Retrieved {Count} messages - RequestId={RequestId}", 
                messages.Count(), requestId);
            
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

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ConversationsController.GetConversationMessages] SUCCESS - ConversationId={ConversationId}, MessageCount={Count}, RequestId={RequestId}, Duration={Duration}ms", 
                conversationId, messageDtos.Count, requestId, elapsed);

            return Ok(messageDtos);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ConversationsController.GetConversationMessages] ERROR - ConversationId={ConversationId}, RequestId={RequestId}, Duration={Duration}ms", 
                conversationId, requestId, elapsed);
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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ConversationsController.CreateConversation] START - RequestId={RequestId}, ProfileId={ProfileId}, Title={Title}", 
            requestId, createDto?.ProfileId, createDto?.Title);

        try
        {
            if (createDto == null)
            {
                _logger.LogWarning("[ConversationsController.CreateConversation] BAD_REQUEST - Null createDto, RequestId={RequestId}", requestId);
                return BadRequest("Conversation data is required");
            }

            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ConversationsController.CreateConversation] KeycloakId: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ConversationsController.CreateConversation] UNAUTHORIZED - RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            // Verify profile exists
            var profile = await _profileService.GetPublicProfileAsync(createDto.ProfileId);
            if (profile == null)
            {
                var elapsedNotFound = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ConversationsController.CreateConversation] PROFILE_NOT_FOUND - ProfileId={ProfileId}, RequestId={RequestId}, Duration={Duration}ms", 
                    createDto.ProfileId, requestId, elapsedNotFound);
                return NotFound("Profile not found");
            }

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
            
            _logger.LogInformation("[ConversationsController.CreateConversation] Conversation created successfully - ConversationId={ConversationId}, RequestId={RequestId}", 
                conversation.Id, requestId);

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

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ConversationsController.CreateConversation] SUCCESS - ConversationId={ConversationId}, ProfileId={ProfileId}, RequestId={RequestId}, Duration={Duration}ms", 
                conversation.Id, createDto.ProfileId, requestId, elapsed);

            return CreatedAtAction(
                nameof(GetConversationMessages),
                new { conversationId = conversation.Id },
                conversationDto);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ConversationsController.CreateConversation] ERROR - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ConversationsController.UpdateConversationTitle] START - RequestId={RequestId}, ConversationId={ConversationId}, NewTitle={NewTitle}", 
            requestId, conversationId, updateDto?.Title);

        try
        {
            if (updateDto == null)
            {
                _logger.LogWarning("[ConversationsController.UpdateConversationTitle] BAD_REQUEST - Null updateDto, RequestId={RequestId}", requestId);
                return BadRequest("Update data is required");
            }

            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ConversationsController.UpdateConversationTitle] KeycloakId: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ConversationsController.UpdateConversationTitle] UNAUTHORIZED - RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                var elapsedNotFound = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ConversationsController.UpdateConversationTitle] CONVERSATION_NOT_FOUND - ConversationId={ConversationId}, RequestId={RequestId}, Duration={Duration}ms", 
                    conversationId, requestId, elapsedNotFound);
                return NotFound("Conversation not found");
            }

            // TODO: Verify user owns this conversation when authentication is implemented

            conversation.Title = updateDto.Title;
            await _conversationRepository.UpdateAsync(conversation);
            await _conversationRepository.SaveChangesAsync();
            
            _logger.LogInformation("[ConversationsController.UpdateConversationTitle] Title updated successfully - ConversationId={ConversationId}, RequestId={RequestId}", 
                conversationId, requestId);

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

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ConversationsController.UpdateConversationTitle] SUCCESS - ConversationId={ConversationId}, NewTitle={Title}, RequestId={RequestId}, Duration={Duration}ms", 
                conversationId, conversation.Title, requestId, elapsed);

            return Ok(conversationDto);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ConversationsController.UpdateConversationTitle] ERROR - ConversationId={ConversationId}, RequestId={RequestId}, Duration={Duration}ms", 
                conversationId, requestId, elapsed);
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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[ConversationsController.DeleteConversation] START - RequestId={RequestId}, ConversationId={ConversationId}", 
            requestId, conversationId);

        try
        {
            var keycloakId = GetKeycloakIdFromRequest();
            _logger.LogInformation("[ConversationsController.DeleteConversation] KeycloakId: {KeycloakId}, RequestId={RequestId}", 
                keycloakId ?? "NULL", requestId);

            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ConversationsController.DeleteConversation] UNAUTHORIZED - RequestId={RequestId}", requestId);
                return Unauthorized("User not authenticated");
            }

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                var elapsedNotFound = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[ConversationsController.DeleteConversation] CONVERSATION_NOT_FOUND - ConversationId={ConversationId}, RequestId={RequestId}, Duration={Duration}ms", 
                    conversationId, requestId, elapsedNotFound);
                return NotFound("Conversation not found");
            }

            // TODO: Verify user owns this conversation when authentication is implemented

            await _conversationRepository.DeleteAsync(conversationId);
            await _conversationRepository.SaveChangesAsync();
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[ConversationsController.DeleteConversation] SUCCESS - ConversationId={ConversationId}, RequestId={RequestId}, Duration={Duration}ms", 
                conversationId, requestId, elapsed);

            return NoContent();
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ConversationsController.DeleteConversation] ERROR - ConversationId={ConversationId}, RequestId={RequestId}, Duration={Duration}ms", 
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
