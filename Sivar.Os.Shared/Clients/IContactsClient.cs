using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client interface for contact information operations
/// </summary>
public interface IContactsClient
{
    /// <summary>
    /// Gets all contacts for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="regionCode">User's region code for popularity sorting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of contacts with action URLs</returns>
    Task<IEnumerable<ContactDisplayDto>> GetContactsByProfileAsync(
        Guid profileId, 
        string regionCode = "SV", 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets contacts by category for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="category">Contact category (messaging, social, phone, email, web, reservations, other)</param>
    /// <param name="regionCode">User's region code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered list of contacts</returns>
    Task<IEnumerable<ContactDisplayDto>> GetContactsByCategoryAsync(
        Guid profileId, 
        string category, 
        string regionCode = "SV",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary contact of a specific type for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="typeKey">Contact type key (whatsapp, phone, email, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Primary contact or null if not found</returns>
    Task<ContactDisplayDto?> GetPrimaryContactAsync(
        Guid profileId, 
        string typeKey, 
        CancellationToken cancellationToken = default);
}
