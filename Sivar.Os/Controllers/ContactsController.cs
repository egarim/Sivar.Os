using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for business contact information
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly IBusinessContactInfoRepository _contactRepository;
    private readonly IContactUrlBuilder _contactUrlBuilder;
    private readonly ILogger<ContactsController> _logger;

    public ContactsController(
        IBusinessContactInfoRepository contactRepository,
        IContactUrlBuilder contactUrlBuilder,
        ILogger<ContactsController> logger)
    {
        _contactRepository = contactRepository ?? throw new ArgumentNullException(nameof(contactRepository));
        _contactUrlBuilder = contactUrlBuilder ?? throw new ArgumentNullException(nameof(contactUrlBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets contacts for a profile
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="regionCode">User's region code for popularity sorting (default: SV)</param>
    /// <returns>List of contacts with action URLs</returns>
    [HttpGet("profile/{profileId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ContactDisplayDto>>> GetContactsByProfile(
        Guid profileId, 
        [FromQuery] string regionCode = "SV")
    {
        try
        {
            _logger.LogInformation("[ContactsController.GetContactsByProfile] ProfileId={ProfileId}, RegionCode={RegionCode}", 
                profileId, regionCode);

            var contacts = await _contactRepository.GetByProfileIdWithTypesAsync(profileId);
            
            if (!contacts.Any())
            {
                _logger.LogInformation("[ContactsController.GetContactsByProfile] No contacts found for ProfileId={ProfileId}", profileId);
                return Ok(new List<ContactDisplayDto>());
            }

            var displayContacts = contacts
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

            _logger.LogInformation("[ContactsController.GetContactsByProfile] Returning {Count} contacts for ProfileId={ProfileId}", 
                displayContacts.Count, profileId);

            return Ok(displayContacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContactsController.GetContactsByProfile] Error getting contacts for ProfileId={ProfileId}", profileId);
            return StatusCode(500, "Error retrieving contacts");
        }
    }

    /// <summary>
    /// Gets contacts by category for a profile
    /// </summary>
    [HttpGet("profile/{profileId:guid}/category/{category}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ContactDisplayDto>>> GetContactsByCategory(
        Guid profileId, 
        string category,
        [FromQuery] string regionCode = "SV")
    {
        try
        {
            var contacts = await _contactRepository.GetByProfileAndCategoryAsync(profileId, category);
            
            var displayContacts = contacts
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

            return Ok(displayContacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContactsController.GetContactsByCategory] Error ProfileId={ProfileId}, Category={Category}", 
                profileId, category);
            return StatusCode(500, "Error retrieving contacts");
        }
    }

    /// <summary>
    /// Gets primary contact of a specific type for a profile
    /// </summary>
    [HttpGet("profile/{profileId:guid}/primary/{typeKey}")]
    [AllowAnonymous]
    public async Task<ActionResult<ContactDisplayDto?>> GetPrimaryContact(
        Guid profileId, 
        string typeKey)
    {
        try
        {
            var contact = await _contactRepository.GetPrimaryByTypeAsync(profileId, typeKey);
            
            if (contact == null || contact.ContactType == null)
            {
                return NotFound();
            }

            var displayContact = new ContactDisplayDto
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

            return Ok(displayContact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContactsController.GetPrimaryContact] Error ProfileId={ProfileId}, TypeKey={TypeKey}", 
                profileId, typeKey);
            return StatusCode(500, "Error retrieving contact");
        }
    }
}
