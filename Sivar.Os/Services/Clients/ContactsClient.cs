using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of contacts client using repositories
/// </summary>
public class ContactsClient : BaseRepositoryClient, IContactsClient
{
    private readonly IBusinessContactInfoRepository _contactRepository;
    private readonly IContactUrlBuilder _contactUrlBuilder;
    private readonly ILogger<ContactsClient> _logger;

    public ContactsClient(
        IBusinessContactInfoRepository contactRepository,
        IContactUrlBuilder contactUrlBuilder,
        ILogger<ContactsClient> logger)
    {
        _contactRepository = contactRepository ?? throw new ArgumentNullException(nameof(contactRepository));
        _contactUrlBuilder = contactUrlBuilder ?? throw new ArgumentNullException(nameof(contactUrlBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ContactDisplayDto>> GetContactsByProfileAsync(
        Guid profileId,
        string regionCode = "SV",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[ContactsClient.GetContactsByProfileAsync] ProfileId={ProfileId}, Region={Region}", 
                profileId, regionCode);

            var contacts = await _contactRepository.GetByProfileIdWithTypesAsync(profileId);

            if (!contacts.Any())
            {
                _logger.LogDebug("[ContactsClient.GetContactsByProfileAsync] No contacts found for ProfileId={ProfileId}", profileId);
                return Enumerable.Empty<ContactDisplayDto>();
            }

            return contacts
                .Where(c => c.IsActive && c.ContactType != null && c.ContactType.IsActive)
                .OrderBy(c => c.ContactType?.Category)
                .ThenByDescending(c => c.ContactType?.GetRegionalPopularity(regionCode) ?? 50)
                .ThenBy(c => c.SortOrder)
                .Select(c => new ContactDisplayDto
                {
                    TypeKey = c.ContactType?.Key ?? "unknown",
                    DisplayName = c.ContactType?.DisplayName ?? c.Label ?? "Contacto",
                    Icon = c.ContactType?.Icon ?? "📞",
                    MudBlazorIcon = c.ContactType?.MudBlazorIcon,
                    Color = c.ContactType?.Color ?? "#607D8B",
                    Category = c.ContactType?.Category ?? "other",
                    Url = c.ContactType != null
                        ? _contactUrlBuilder.BuildUrl(c.ContactType, c)
                        : string.Empty,
                    Value = c.Value,
                    Label = c.Label,
                    OpenInNewTab = c.ContactType?.OpenInNewTab ?? true,
                    MobileOnly = c.ContactType?.MobileOnly ?? false,
                    SortOrder = c.SortOrder,
                    RegionalPopularity = c.ContactType?.GetRegionalPopularity(regionCode) ?? 50
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContactsClient.GetContactsByProfileAsync] Error for ProfileId={ProfileId}", profileId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ContactDisplayDto>> GetContactsByCategoryAsync(
        Guid profileId,
        string category,
        string regionCode = "SV",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contacts = await _contactRepository.GetByProfileAndCategoryAsync(profileId, category);

            return contacts
                .Where(c => c.IsActive && c.ContactType != null && c.ContactType.IsActive)
                .OrderByDescending(c => c.ContactType?.GetRegionalPopularity(regionCode) ?? 50)
                .ThenBy(c => c.SortOrder)
                .Select(c => new ContactDisplayDto
                {
                    TypeKey = c.ContactType?.Key ?? "unknown",
                    DisplayName = c.ContactType?.DisplayName ?? c.Label ?? "Contacto",
                    Icon = c.ContactType?.Icon ?? "📞",
                    MudBlazorIcon = c.ContactType?.MudBlazorIcon,
                    Color = c.ContactType?.Color ?? "#607D8B",
                    Category = c.ContactType?.Category ?? "other",
                    Url = c.ContactType != null
                        ? _contactUrlBuilder.BuildUrl(c.ContactType, c)
                        : string.Empty,
                    Value = c.Value,
                    Label = c.Label,
                    OpenInNewTab = c.ContactType?.OpenInNewTab ?? true,
                    MobileOnly = c.ContactType?.MobileOnly ?? false,
                    SortOrder = c.SortOrder,
                    RegionalPopularity = c.ContactType?.GetRegionalPopularity(regionCode) ?? 50
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContactsClient.GetContactsByCategoryAsync] Error ProfileId={ProfileId}, Category={Category}",
                profileId, category);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ContactDisplayDto?> GetPrimaryContactAsync(
        Guid profileId,
        string typeKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contact = await _contactRepository.GetPrimaryByTypeAsync(profileId, typeKey);

            if (contact == null || contact.ContactType == null)
            {
                return null;
            }

            return new ContactDisplayDto
            {
                TypeKey = contact.ContactType.Key,
                DisplayName = contact.ContactType.DisplayName ?? contact.Label ?? "Contacto",
                Icon = contact.ContactType.Icon ?? "📞",
                MudBlazorIcon = contact.ContactType.MudBlazorIcon,
                Color = contact.ContactType.Color ?? "#607D8B",
                Category = contact.ContactType.Category ?? "other",
                Url = _contactUrlBuilder.BuildUrl(contact.ContactType, contact),
                Value = contact.Value,
                Label = contact.Label,
                OpenInNewTab = contact.ContactType.OpenInNewTab,
                MobileOnly = contact.ContactType.MobileOnly,
                SortOrder = contact.SortOrder,
                RegionalPopularity = 100 // Primary contact
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContactsClient.GetPrimaryContactAsync] Error ProfileId={ProfileId}, TypeKey={TypeKey}",
                profileId, typeKey);
            throw;
        }
    }
}
