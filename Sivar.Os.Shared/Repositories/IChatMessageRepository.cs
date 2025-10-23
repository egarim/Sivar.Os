
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for chat message data access
/// </summary>
public interface IChatMessageRepository : IBaseRepository<ChatMessage>
{
    /// <summary>
    /// Get messages for a specific conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of messages per page</param>
    /// <returns>List of messages ordered by MessageOrder</returns>
    Task<List<ChatMessage>> GetConversationMessagesAsync(Guid conversationId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Get the next message order number for a conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>Next message order number</returns>
    Task<int> GetNextMessageOrderAsync(Guid conversationId);

    /// <summary>
    /// Get total message count for a conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>Total number of messages</returns>
    Task<int> GetMessageCountAsync(Guid conversationId);
}
