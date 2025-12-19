using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// A specific service offered by a bookable resource.
/// For example, a barber might offer: Haircut (30 min, $20), Beard Trim (15 min, $10), Full Package (45 min, $30)
/// </summary>
public class ResourceService : BaseEntity
{
    /// <summary>
    /// The resource that offers this service
    /// </summary>
    public virtual Guid ResourceId { get; set; }
    public virtual BookableResource Resource { get; set; } = null!;

    /// <summary>
    /// Name of the service
    /// </summary>
    [Required]
    [StringLength(200)]
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the service includes
    /// </summary>
    [StringLength(1000)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// Duration of this service in minutes (overrides resource default)
    /// </summary>
    public virtual int DurationMinutes { get; set; }

    /// <summary>
    /// Price for this specific service
    /// </summary>
    public virtual decimal Price { get; set; }

    /// <summary>
    /// Currency code (ISO 4217), defaults to resource currency
    /// </summary>
    [StringLength(3)]
    public virtual string? Currency { get; set; }

    /// <summary>
    /// Whether this service is currently available
    /// </summary>
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for listing services (lower = first)
    /// </summary>
    public virtual int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Image URL for this service (optional)
    /// </summary>
    [StringLength(500)]
    public virtual string? ImageUrl { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public virtual string? MetadataJson { get; set; }
}
