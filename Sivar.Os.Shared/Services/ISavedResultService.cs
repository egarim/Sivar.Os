
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for managing saved AI chat results
/// </summary>
public interface ISavedResultService
{
    /// <summary>
    /// Save a chat result for later reference
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="dto">Saved result data</param>
    /// <returns>Created saved result</returns>
    Task<SavedResultDto> SaveResultAsync(Guid profileId, CreateSavedResultDto dto);

    /// <summary>
    /// Get all saved results for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="resultType">Optional filter by result type</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>List of saved results</returns>
    Task<List<SavedResultDto>> GetProfileSavedResultsAsync(Guid profileId, string? resultType = null, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get saved results from a specific conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="profileId">Profile ID (for security)</param>
    /// <returns>List of saved results</returns>
    Task<List<SavedResultDto>> GetConversationSavedResultsAsync(Guid conversationId, Guid profileId);

    /// <summary>
    /// Delete a saved result
    /// </summary>
    /// <param name="savedResultId">Saved result ID</param>
    /// <param name="profileId">Profile ID (for security)</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteSavedResultAsync(Guid savedResultId, Guid profileId);

    /// <summary>
    /// Clear all saved results for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <returns>Number of results deleted</returns>
    Task<int> ClearProfileSavedResultsAsync(Guid profileId);
}
