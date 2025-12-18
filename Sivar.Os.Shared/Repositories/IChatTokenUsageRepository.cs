using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for AI chat token usage tracking and auditing
/// </summary>
public interface IChatTokenUsageRepository : IBaseRepository<ChatTokenUsage>
{
    /// <summary>
    /// Get token usage records for a specific profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="from">Optional start date filter</param>
    /// <param name="to">Optional end date filter</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>List of token usage records</returns>
    Task<List<ChatTokenUsage>> GetProfileTokenUsageAsync(
        Guid profileId, 
        DateTime? from = null, 
        DateTime? to = null, 
        int page = 1, 
        int pageSize = 50);

    /// <summary>
    /// Get token usage records for a specific conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>List of token usage records for the conversation</returns>
    Task<List<ChatTokenUsage>> GetConversationTokenUsageAsync(Guid conversationId);

    /// <summary>
    /// Get total tokens used by a profile within a date range
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <returns>Total tokens used</returns>
    Task<long> GetTotalTokensUsedAsync(Guid profileId, DateTime from, DateTime to);

    /// <summary>
    /// Get token usage summary for a profile (total input, output, and combined tokens)
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="from">Optional start date filter</param>
    /// <param name="to">Optional end date filter</param>
    /// <returns>Usage summary with input, output, and total tokens</returns>
    Task<(long InputTokens, long OutputTokens, long TotalTokens)> GetTokenUsageSummaryAsync(
        Guid profileId, 
        DateTime? from = null, 
        DateTime? to = null);
}
