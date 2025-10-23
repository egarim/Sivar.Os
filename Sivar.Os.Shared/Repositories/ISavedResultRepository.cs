
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for saved result data access
/// </summary>
public interface ISavedResultRepository : IBaseRepository<SavedResult>
{
    /// <summary>
    /// Get saved results for a specific profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="resultType">Optional filter by result type</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>List of saved results</returns>
    Task<List<SavedResult>> GetProfileSavedResultsAsync(Guid profileId, string? resultType = null, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get saved results from a specific conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>List of saved results</returns>
    Task<List<SavedResult>> GetConversationSavedResultsAsync(Guid conversationId);

    /// <summary>
    /// Delete all saved results for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <returns>Number of results deleted</returns>
    Task<int> ClearProfileSavedResultsAsync(Guid profileId);
}
