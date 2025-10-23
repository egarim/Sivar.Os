

using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for conversation data access
/// </summary>
public interface IConversationRepository : IBaseRepository<Conversation>
{
    /// <summary>
    /// Get all conversations for a specific profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="includeMessages">Whether to include messages in the result</param>
    /// <returns>List of conversations</returns>
    Task<List<Conversation>> GetProfileConversationsAsync(Guid profileId, bool includeMessages = false);

    /// <summary>
    /// Get a conversation by ID with messages
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>Conversation with messages, or null if not found</returns>
    Task<Conversation?> GetConversationWithMessagesAsync(Guid conversationId);

    /// <summary>
    /// Update the last message timestamp for a conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateLastMessageTimeAsync(Guid conversationId);

    /// <summary>
    /// Get active conversation for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <returns>Active conversation or null if none</returns>
    Task<Conversation?> GetActiveConversationAsync(Guid profileId);

    /// <summary>
    /// Set a conversation as active and deactivate others for the profile
    /// </summary>
    /// <param name="conversationId">Conversation ID to activate</param>
    /// <param name="profileId">Profile ID</param>
    /// <returns>True if successful</returns>
    Task<bool> SetActiveConversationAsync(Guid conversationId, Guid profileId);
}
