using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service for building contact action URLs and sorting contacts by regional popularity
/// </summary>
public class ContactUrlBuilder : IContactUrlBuilder
{
    private readonly IContactTypeRepository _contactTypeRepository;
    private readonly IBusinessContactInfoRepository _contactInfoRepository;

    public ContactUrlBuilder(
        IContactTypeRepository contactTypeRepository,
        IBusinessContactInfoRepository contactInfoRepository)
    {
        _contactTypeRepository = contactTypeRepository;
        _contactInfoRepository = contactInfoRepository;
    }

    /// <inheritdoc />
    public string BuildUrl(
        ContactType contactType,
        BusinessContactInfo contact,
        string? message = null,
        string? subject = null,
        double? latitude = null,
        double? longitude = null,
        string? businessName = null)
    {
        var url = contactType.UrlTemplate;

        // Replace value placeholder
        url = url.Replace("{value}", Uri.EscapeDataString(contact.Value));

        // Replace country code (for phone-based contacts)
        var countryCode = !string.IsNullOrEmpty(contact.CountryCode) 
            ? contact.CountryCode 
            : "503"; // Default to El Salvador
        url = url.Replace("{country_code}", countryCode);

        // Replace message/subject placeholders (for messaging/email)
        url = url.Replace("{message}", Uri.EscapeDataString(message ?? ""));
        url = url.Replace("{subject}", Uri.EscapeDataString(subject ?? ""));

        // Replace location placeholders (for maps/navigation)
        if (latitude.HasValue && longitude.HasValue)
        {
            url = url.Replace("{lat}", latitude.Value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
            url = url.Replace("{lng}", longitude.Value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            // Remove unused location placeholders
            url = url.Replace("{lat}", "");
            url = url.Replace("{lng}", "");
        }

        // Replace business name placeholder
        url = url.Replace("{name}", Uri.EscapeDataString(businessName ?? ""));

        // Clean up any empty query parameters
        url = CleanupUrl(url);

        return url;
    }

    /// <inheritdoc />
    public async Task<List<ContactDisplayDto>> GetContactsForProfileAsync(
        Guid profileId,
        string? userRegion = null,
        double? latitude = null,
        double? longitude = null,
        string? businessName = null)
    {
        var contacts = await _contactInfoRepository.GetByProfileIdWithTypesAsync(profileId);
        var region = userRegion ?? "SV"; // Default to El Salvador

        var displayList = contacts
            .Select(c => BuildContactDisplayDto(c.ContactType, c, region, latitude, longitude, businessName))
            .ToList();

        // Sort by category order, then regional popularity, then sort order
        return displayList
            .OrderBy(c => GetCategorySortOrder(c.Category))
            .ThenByDescending(c => c.RegionalPopularity)
            .ThenBy(c => c.SortOrder)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, List<ContactDisplayDto>>> GetContactsForProfilesAsync(
        IEnumerable<Guid> profileIds,
        string? userRegion = null)
    {
        var contactsByProfile = await _contactInfoRepository.GetByProfileIdsAsync(profileIds);
        var region = userRegion ?? "SV";

        var result = new Dictionary<Guid, List<ContactDisplayDto>>();

        foreach (var kvp in contactsByProfile)
        {
            var displayList = kvp.Value
                .Select(c => BuildContactDisplayDto(c.ContactType, c, region))
                .OrderBy(c => GetCategorySortOrder(c.Category))
                .ThenByDescending(c => c.RegionalPopularity)
                .ThenBy(c => c.SortOrder)
                .ToList();

            result[kvp.Key] = displayList;
        }

        return result;
    }

    /// <inheritdoc />
    public ContactDisplayDto BuildContactDisplayDto(
        ContactType contactType,
        BusinessContactInfo contact,
        string? userRegion = null,
        double? latitude = null,
        double? longitude = null,
        string? businessName = null)
    {
        var region = userRegion ?? "SV";

        return new ContactDisplayDto
        {
            ContactId = contact.Id,
            TypeKey = contactType.Key,
            DisplayName = contactType.DisplayName,
            Icon = contactType.Icon,
            MudBlazorIcon = contactType.MudBlazorIcon,
            Color = contactType.Color,
            Category = contactType.Category,
            Value = contact.Value,
            Label = contact.Label,
            Url = BuildUrl(contactType, contact, null, null, latitude, longitude, businessName),
            OpenInNewTab = contactType.OpenInNewTab,
            MobileOnly = contactType.MobileOnly,
            RegionalPopularity = contactType.GetRegionalPopularity(region),
            SortOrder = contact.SortOrder,
            Notes = contact.Notes
        };
    }

    /// <inheritdoc />
    public string GetCategoryDisplayName(string category) => category.ToLowerInvariant() switch
    {
        "phone" => "Teléfono",
        "messaging" => "Mensajería",
        "email" => "Correo",
        "web" => "Web",
        "social" => "Redes Sociales",
        "location" => "Ubicación",
        "delivery" => "Delivery",
        _ => "Otros"
    };

    /// <inheritdoc />
    public List<ContactGroupDto> GroupByCategory(List<ContactDisplayDto> contacts)
    {
        return contacts
            .GroupBy(c => c.Category)
            .Select(g => new ContactGroupDto
            {
                Category = g.Key,
                CategoryDisplayName = GetCategoryDisplayName(g.Key),
                CategorySortOrder = GetCategorySortOrder(g.Key),
                Contacts = g.OrderByDescending(c => c.RegionalPopularity)
                           .ThenBy(c => c.SortOrder)
                           .ToList()
            })
            .OrderBy(g => g.CategorySortOrder)
            .ToList();
    }

    /// <summary>
    /// Get sort order for contact categories
    /// </summary>
    private static int GetCategorySortOrder(string category) => category.ToLowerInvariant() switch
    {
        "phone" => 1,
        "messaging" => 2,
        "email" => 3,
        "web" => 4,
        "social" => 5,
        "location" => 6,
        "delivery" => 7,
        _ => 99
    };

    /// <summary>
    /// Clean up URL by removing empty query parameters
    /// </summary>
    private static string CleanupUrl(string url)
    {
        // Remove empty query parameters like ?text= or &body=
        url = System.Text.RegularExpressions.Regex.Replace(url, @"[?&][^=]+=(?=&|$)", "");
        
        // Fix double ampersands
        url = url.Replace("&&", "&");
        
        // Remove trailing ? or &
        url = url.TrimEnd('?', '&');
        
        // Fix ?& at start of query string
        url = url.Replace("?&", "?");

        return url;
    }
}
