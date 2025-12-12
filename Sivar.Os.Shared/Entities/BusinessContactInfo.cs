using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Contact information for a business/profile.
/// Stored as a collection, allowing multiple contact methods per profile.
/// </summary>
public class BusinessContactInfo : BaseEntity
{
    /// <summary>
    /// The profile/business this contact belongs to
    /// </summary>
    public virtual Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// Reference to contact type catalog
    /// </summary>
    public virtual Guid ContactTypeId { get; set; }
    public virtual ContactType ContactType { get; set; } = null!;

    /// <summary>
    /// The actual value (phone number, email, username, URL, etc.)
    /// </summary>
    [Required, StringLength(500)]
    public virtual string Value { get; set; } = string.Empty;

    /// <summary>
    /// Optional label (e.g., "Ventas", "Soporte", "Personal", "Reservaciones")
    /// </summary>
    [StringLength(100)]
    public virtual string? Label { get; set; }

    /// <summary>
    /// Country code for phone numbers (e.g., "503" for El Salvador, without +)
    /// </summary>
    [StringLength(10)]
    public virtual string? CountryCode { get; set; }

    /// <summary>
    /// Display order (lower = first)
    /// </summary>
    public virtual int SortOrder { get; set; } = 100;

    /// <summary>
    /// Is this the primary contact of this type?
    /// </summary>
    public virtual bool IsPrimary { get; set; }

    /// <summary>
    /// Is this contact method active?
    /// </summary>
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional: Hours when this contact is available (JSON)
    /// Example: {"mon": "08:00-17:00", "tue": "08:00-17:00", ...}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public virtual string? AvailableHours { get; set; }

    /// <summary>
    /// Additional notes (e.g., "Solo WhatsApp, no llamadas", "Emergencias 24/7")
    /// </summary>
    [StringLength(200)]
    public virtual string? Notes { get; set; }
}
