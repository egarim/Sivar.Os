namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for displaying contact information with pre-built action URL
/// </summary>
public class ContactDisplayDto
{
    /// <summary>
    /// Contact info ID (for tracking clicks, etc.)
    /// </summary>
    public Guid ContactId { get; set; }

    /// <summary>
    /// Contact type key (e.g., "whatsapp", "phone", "email")
    /// </summary>
    public string TypeKey { get; set; } = string.Empty;

    /// <summary>
    /// Localized display name (e.g., "WhatsApp", "Llamar", "Email")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Icon (emoji or icon name)
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// MudBlazor icon constant (for Blazor components)
    /// </summary>
    public string? MudBlazorIcon { get; set; }

    /// <summary>
    /// CSS/hex color for styling
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Category for grouping: "messaging", "social", "phone", "email", "web", "location", "delivery"
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The raw contact value (phone, email, username, etc.)
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Optional label (e.g., "Ventas", "Soporte")
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Pre-built action URL ready to use in href
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Whether to open in new tab
    /// </summary>
    public bool OpenInNewTab { get; set; }

    /// <summary>
    /// Whether this requires a mobile device
    /// </summary>
    public bool MobileOnly { get; set; }

    /// <summary>
    /// Regional popularity score (for sorting)
    /// </summary>
    public int RegionalPopularity { get; set; }

    /// <summary>
    /// Sort order within category
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Optional notes about this contact
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Grouped contacts by category for UI rendering
/// </summary>
public class ContactGroupDto
{
    /// <summary>
    /// Category key (e.g., "messaging", "phone", "social")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Display name for category (e.g., "Mensajería", "Teléfono", "Redes Sociales")
    /// </summary>
    public string CategoryDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Sort order for category
    /// </summary>
    public int CategorySortOrder { get; set; }

    /// <summary>
    /// Contacts in this category
    /// </summary>
    public List<ContactDisplayDto> Contacts { get; set; } = new();
}
