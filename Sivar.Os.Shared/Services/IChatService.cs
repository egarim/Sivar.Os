
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for AI chat functionality
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Create a new conversation for a profile
    /// </summary>
    /// <param name="dto">Conversation creation data</param>
    /// <returns>Created conversation</returns>
    Task<ConversationDto> CreateConversationAsync(CreateConversationDto dto);

    /// <summary>
    /// Get all conversations for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <returns>List of conversations</returns>
    Task<List<ConversationDto>> GetProfileConversationsAsync(Guid profileId);

    /// <summary>
    /// Get a specific conversation with its messages
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="page">Page number for messages</param>
    /// <param name="pageSize">Messages per page</param>
    /// <returns>Conversation with messages</returns>
    Task<(ConversationDto Conversation, List<ChatMessageDto> Messages)> GetConversationWithMessagesAsync(
        Guid conversationId, 
        int page = 1, 
        int pageSize = 50);

    /// <summary>
    /// Send a message and get AI response
    /// </summary>
    /// <param name="dto">Message data</param>
    /// <param name="profileId">Profile ID (for security)</param>
    /// <returns>Chat response with both user and AI messages</returns>
    Task<ChatResponseDto> SendMessageAsync(SendMessageDto dto, Guid profileId);

    /// <summary>
    /// Delete a conversation (soft delete)
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="profileId">Profile ID (for security)</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteConversationAsync(Guid conversationId, Guid profileId);

    /// <summary>
    /// Set a conversation as active
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="profileId">Profile ID</param>
    /// <returns>True if successful</returns>
    Task<bool> SetActiveConversationAsync(Guid conversationId, Guid profileId);

    /// <summary>
    /// Get the active conversation for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <returns>Active conversation or null</returns>
    Task<ConversationDto?> GetActiveConversationAsync(Guid profileId);
}
