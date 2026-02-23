namespace PhotoBooking.Shared.Entities;

/// <summary>
/// Weekly availability schedule for a service
/// </summary>
public class ServiceAvailability : BaseEntity
{
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    
    // Day of week (0 = Sunday, 6 = Saturday)
    public int DayOfWeek { get; set; }
    
    // Time slots
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    
    // Status
    public bool IsAvailable { get; set; } = true;
}
