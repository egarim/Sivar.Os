namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Weekly availability schedule for a bookable resource.
/// Defines the regular working hours for each day of the week.
/// </summary>
public class ResourceAvailability : BaseEntity
{
    /// <summary>
    /// The resource this availability belongs to
    /// </summary>
    public virtual Guid ResourceId { get; set; }
    public virtual BookableResource Resource { get; set; } = null!;

    /// <summary>
    /// Day of the week (0 = Sunday, 1 = Monday, ..., 6 = Saturday)
    /// </summary>
    public virtual DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Start time for this availability block (e.g., 09:00)
    /// </summary>
    public virtual TimeOnly StartTime { get; set; }

    /// <summary>
    /// End time for this availability block (e.g., 17:00)
    /// </summary>
    public virtual TimeOnly EndTime { get; set; }

    /// <summary>
    /// Whether the resource is available during this time block
    /// (allows for lunch breaks by having multiple blocks per day)
    /// </summary>
    public virtual bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Optional label for this time block (e.g., "Morning Shift", "After Lunch")
    /// </summary>
    public virtual string? Label { get; set; }
}
