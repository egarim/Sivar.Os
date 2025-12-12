using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// HTTP client implementation for contact information operations
/// </summary>
public class ContactsClient : BaseClient, IContactsClient
{
    public ContactsClient(HttpClient httpClient, SivarClientOptions options)
        : base(httpClient, options) { }

    /// <inheritdoc />
    public async Task<IEnumerable<ContactDisplayDto>> GetContactsByProfileAsync(
        Guid profileId,
        string regionCode = "SV",
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ContactDisplayDto>>(
            $"api/contacts/profile/{profileId}?regionCode={regionCode}", 
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ContactDisplayDto>> GetContactsByCategoryAsync(
        Guid profileId,
        string category,
        string regionCode = "SV",
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<ContactDisplayDto>>(
            $"api/contacts/profile/{profileId}/category/{category}?regionCode={regionCode}",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ContactDisplayDto?> GetPrimaryContactAsync(
        Guid profileId,
        string typeKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetAsync<ContactDisplayDto?>(
                $"api/contacts/profile/{profileId}/primary/{typeKey}",
                cancellationToken);
        }
        catch
        {
            // Return null if not found
            return null;
        }
    }
}
