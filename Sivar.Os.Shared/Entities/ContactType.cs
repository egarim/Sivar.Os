using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Catalog of contact types with URL templates and display configuration.
/// Stored in database for easy extension without code changes.
/// </summary>
public class ContactType : BaseEntity
{
    /// <summary>
    /// Unique key for this contact type (e.g., "phone", "whatsapp", "telegram")
    /// </summary>
    [Required, StringLength(50)]
    public virtual string Key { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI (e.g., "WhatsApp", "Llamar", "Email")
    /// </summary>
    [Required, StringLength(100)]
    public virtual string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Icon identifier - emoji or icon name (e.g., "📞", "💬")
    /// </summary>
    [StringLength(50)]
    public virtual string Icon { get; set; } = string.Empty;

    /// <summary>
    /// MudBlazor icon constant (e.g., "Icons.Material.Filled.Phone")
    /// </summary>
    [StringLength(100)]
    public virtual string? MudBlazorIcon { get; set; }

    /// <summary>
    /// CSS/hex color for the icon/button (e.g., "#25D366" for WhatsApp green)
    /// </summary>
    [StringLength(20)]
    public virtual string? Color { get; set; }

    /// <summary>
    /// URL template with placeholders: {value}, {country_code}, {message}, {subject}, {lat}, {lng}, {name}
    /// Examples:
    /// - Phone: "tel:{country_code}{value}"
    /// - WhatsApp: "https://wa.me/{country_code}{value}?text={message}"
    /// - Telegram: "https://t.me/{value}"
    /// - Email: "mailto:{value}?subject={subject}&amp;body={message}"
    /// </summary>
    [Required, StringLength(500)]
    public virtual string UrlTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Category: "messaging", "social", "phone", "email", "web", "location", "delivery"
    /// </summary>
    [StringLength(30)]
    public virtual string Category { get; set; } = "other";

    /// <summary>
    /// Sort order within category (lower = first)
    /// </summary>
    public virtual int SortOrder { get; set; } = 100;

    /// <summary>
    /// Is this contact type active?
    /// </summary>
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Regional popularity scores (JSON: {"SV": 100, "US": 30, "RU": 5})
    /// Higher = more popular in that region, used for sorting
    /// </summary>
    [Column(TypeName = "jsonb")]
    public virtual string? RegionalPopularity { get; set; }

    /// <summary>
    /// Value validation regex (e.g., for phone numbers)
    /// </summary>
    [StringLength(200)]
    public virtual string? ValidationRegex { get; set; }

    /// <summary>
    /// Placeholder text for input (e.g., "+503 7XXX-XXXX")
    /// </summary>
    [StringLength(100)]
    public virtual string? Placeholder { get; set; }

    /// <summary>
    /// Should this open in a new tab/window?
    /// </summary>
    public virtual bool OpenInNewTab { get; set; } = true;

    /// <summary>
    /// Requires mobile device? (e.g., WhatsApp native, iMessage)
    /// </summary>
    public virtual bool MobileOnly { get; set; } = false;

    /// <summary>
    /// Additional metadata (JSON) for future extensibility
    /// </summary>
    [Column(TypeName = "jsonb")]
    public virtual string? Metadata { get; set; }

    /// <summary>
    /// Navigation property: Business contacts using this type
    /// </summary>
    public virtual ObservableCollection<BusinessContactInfo> BusinessContacts { get; set; } = new ObservableCollection<BusinessContactInfo>();

    /// <summary>
    /// Get regional popularity score for a specific region
    /// </summary>
    public int GetRegionalPopularity(string regionCode)
    {
        if (string.IsNullOrEmpty(RegionalPopularity))
            return 50; // Default middle score

        try
        {
            var popularity = JsonSerializer.Deserialize<Dictionary<string, int>>(RegionalPopularity);
            if (popularity != null && popularity.TryGetValue(regionCode.ToUpperInvariant(), out var score))
                return score;
            return 50;
        }
        catch
        {
            return 50;
        }
    }
}
