using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Exceptions to the regular weekly availability schedule.
/// Used for holidays, vacations, special hours, or blocked dates.
/// </summary>
public class ResourceException : BaseEntity
{
    /// <summary>
    /// The resource this exception applies to
    /// </summary>
    public virtual Guid ResourceId { get; set; }
    public virtual BookableResource Resource { get; set; } = null!;

    /// <summary>
    /// The specific date this exception applies to
    /// </summary>
    public virtual DateOnly Date { get; set; }

    /// <summary>
    /// Whether the resource is available on this date
    /// false = completely unavailable (holiday, vacation)
    /// true = available with special hours (use StartTime/EndTime)
    /// </summary>
    public virtual bool IsAvailable { get; set; }

    /// <summary>
    /// Start time if available with special hours (ignored if IsAvailable = false)
    /// </summary>
    public virtual TimeOnly? StartTime { get; set; }

    /// <summary>
    /// End time if available with special hours (ignored if IsAvailable = false)
    /// </summary>
    public virtual TimeOnly? EndTime { get; set; }

    /// <summary>
    /// Reason for the exception
    /// </summary>
    [StringLength(500)]
    public virtual string? Reason { get; set; }

    /// <summary>
    /// Whether this is a recurring annual exception (e.g., Christmas, New Year)
    /// If true, applies to the same date every year
    /// </summary>
    public virtual bool IsRecurringAnnually { get; set; } = false;
}
