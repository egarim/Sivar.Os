namespace PhotoBooking.Shared.Entities;

/// <summary>
/// Customer booking/appointment
/// </summary>
public class Booking : BaseEntity
{
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    
    public Guid BusinessProfileId { get; set; }
    public BusinessProfile BusinessProfile { get; set; } = null!;
    
    // Customer info
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    
    // Booking details
    public DateTime BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    
    // Location (if different from business)
    public string? EventLocation { get; set; }
    public string? EventAddress { get; set; }
    public double? EventLatitude { get; set; }
    public double? EventLongitude { get; set; }
    
    // Notes
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }
    
    // Status
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    
    // WhatsApp conversation reference
    public string? WhatsAppConversationId { get; set; }
    
    // Reminders
    public bool ReminderSent { get; set; }
    public DateTime? ReminderSentAt { get; set; }
}

public enum BookingStatus
{
    Pending,
    Confirmed,
    InProgress,
    Completed,
    Cancelled,
    NoShow
}
