using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for conversation data access
/// </summary>
public class ConversationRepository : BaseRepository<Conversation>, IConversationRepository
{
    public ConversationRepository(SivarDbContext context) : base(context)
    {
    }

    public async Task<List<Conversation>> GetProfileConversationsAsync(Guid profileId, bool includeMessages = false)
    {
        var query = _context.Conversations
            .Where(c => !c.IsDeleted && c.ProfileId == profileId);

        if (includeMessages)
        {
            query = query.Include(c => c.Messages.OrderBy(m => m.MessageOrder));
        }

        return await query
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();
    }

    public async Task<Conversation?> GetConversationWithMessagesAsync(Guid conversationId)
    {
        return await _context.Conversations
            .Include(c => c.Messages.OrderBy(m => m.MessageOrder))
            .FirstOrDefaultAsync(c => !c.IsDeleted && c.Id == conversationId);
    }

    public async Task<bool> UpdateLastMessageTimeAsync(Guid conversationId)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && !c.IsDeleted);

        if (conversation == null)
        {
            return false;
        }

        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Conversation?> GetActiveConversationAsync(Guid profileId)
    {
        return await _context.Conversations
            .Include(c => c.Messages.OrderBy(m => m.MessageOrder))
            .FirstOrDefaultAsync(c => !c.IsDeleted && c.ProfileId == profileId && c.IsActive);
    }

    public async Task<bool> SetActiveConversationAsync(Guid conversationId, Guid profileId)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.ProfileId == profileId && !c.IsDeleted);

        if (conversation == null)
        {
            return false;
        }

        // Deactivate all other conversations for this profile
        var otherConversations = await _context.Conversations
            .Where(c => c.ProfileId == profileId && c.Id != conversationId && c.IsActive && !c.IsDeleted)
            .ToListAsync();

        foreach (var other in otherConversations)
        {
            other.IsActive = false;
            other.UpdatedAt = DateTime.UtcNow;
        }

        // Activate the target conversation
        conversation.IsActive = true;
        conversation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}
