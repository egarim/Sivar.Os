using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for chat message data access
/// </summary>
public class ChatMessageRepository : BaseRepository<ChatMessage>, IChatMessageRepository
{
    public ChatMessageRepository(SivarDbContext context) : base(context)
    {
    }

    public async Task<List<ChatMessage>> GetConversationMessagesAsync(Guid conversationId, int page = 1, int pageSize = 50)
    {
        return await _context.ChatMessages
            .Where(m => !m.IsDeleted && m.ConversationId == conversationId)
            .OrderBy(m => m.MessageOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetNextMessageOrderAsync(Guid conversationId)
    {
        var maxOrder = await _context.ChatMessages
            .Where(m => !m.IsDeleted && m.ConversationId == conversationId)
            .MaxAsync(m => (int?)m.MessageOrder);

        return (maxOrder ?? -1) + 1;
    }

    public async Task<int> GetMessageCountAsync(Guid conversationId)
    {
        return await _context.ChatMessages
            .Where(m => !m.IsDeleted && m.ConversationId == conversationId)
            .CountAsync();
    }
}
