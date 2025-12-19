using System.ComponentModel.DataAnnotations;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a scheduled event or appointment in the calendar system.
/// Events can be created by profiles (personal or business) and can be public or private.
/// </summary>
public class ScheduleEvent : BaseEntity
{
    /// <summary>
    /// Title of the event
    /// </summary>
    [Required]
    [MaxLength(200)]
    public virtual string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the event
    /// </summary>
    [MaxLength(5000)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// Event start date and time (UTC)
    /// </summary>
    public virtual DateTime StartTime { get; set; }

    /// <summary>
    /// Event end date and time (UTC)
    /// </summary>
    public virtual DateTime EndTime { get; set; }

    /// <summary>
    /// Whether this is an all-day event
    /// </summary>
    public virtual bool IsAllDay { get; set; }

    /// <summary>
    /// Timezone identifier for the event (e.g., "America/El_Salvador")
    /// </summary>
    [MaxLength(100)]
    public virtual string TimeZone { get; set; } = "America/El_Salvador";

    /// <summary>
    /// Profile that owns/created this event
    /// </summary>
    public virtual Guid ProfileId { get; set; }

    /// <summary>
    /// Navigation property to the owner profile
    /// </summary>
    public virtual Profile? Profile { get; set; }

    /// <summary>
    /// Type of event
    /// </summary>
    public virtual EventType EventType { get; set; } = EventType.General;

    /// <summary>
    /// Visibility of the event
    /// </summary>
    public virtual EventVisibility Visibility { get; set; } = EventVisibility.Public;

    /// <summary>
    /// Status of the event
    /// </summary>
    public virtual EventStatus Status { get; set; } = EventStatus.Confirmed;

    /// <summary>
    /// Physical location/address of the event
    /// </summary>
    [MaxLength(500)]
    public virtual string? Location { get; set; }

    /// <summary>
    /// Latitude for map integration
    /// </summary>
    public virtual double? Latitude { get; set; }

    /// <summary>
    /// Longitude for map integration
    /// </summary>
    public virtual double? Longitude { get; set; }

    /// <summary>
    /// Virtual meeting link (Zoom, Teams, Google Meet, etc.)
    /// </summary>
    [MaxLength(500)]
    public virtual string? VirtualLink { get; set; }

    /// <summary>
    /// Whether this is a virtual/online event
    /// </summary>
    public virtual bool IsVirtual { get; set; }

    /// <summary>
    /// Cover image URL for the event
    /// </summary>
    [MaxLength(500)]
    public virtual string? CoverImageUrl { get; set; }

    /// <summary>
    /// Color for calendar display (hex color code)
    /// </summary>
    [MaxLength(7)]
    public virtual string? Color { get; set; }

    /// <summary>
    /// Maximum number of attendees (null = unlimited)
    /// </summary>
    public virtual int? MaxAttendees { get; set; }

    /// <summary>
    /// Whether registration/RSVP is required
    /// </summary>
    public virtual bool RequiresRegistration { get; set; }

    /// <summary>
    /// Registration deadline (null = until event starts)
    /// </summary>
    public virtual DateTime? RegistrationDeadline { get; set; }

    /// <summary>
    /// Price for the event (0 = free)
    /// </summary>
    public virtual decimal Price { get; set; }

    /// <summary>
    /// Currency code for the price (e.g., "USD", "SVC")
    /// </summary>
    [MaxLength(3)]
    public virtual string Currency { get; set; } = "USD";

    /// <summary>
    /// Category/tag for the event
    /// </summary>
    [MaxLength(100)]
    public virtual string? Category { get; set; }

    /// <summary>
    /// Recurrence rule ID if this is a recurring event
    /// </summary>
    public virtual Guid? RecurrenceRuleId { get; set; }

    /// <summary>
    /// Navigation property to recurrence rule
    /// </summary>
    public virtual RecurrenceRule? RecurrenceRule { get; set; }

    /// <summary>
    /// For recurring events, the original event ID this instance was generated from
    /// </summary>
    public virtual Guid? ParentEventId { get; set; }

    /// <summary>
    /// Navigation property to parent event (for recurring instances)
    /// </summary>
    public virtual ScheduleEvent? ParentEvent { get; set; }

    /// <summary>
    /// Child event instances (for recurring events)
    /// </summary>
    public virtual ICollection<ScheduleEvent> ChildEvents { get; set; } = new List<ScheduleEvent>();

    /// <summary>
    /// Attendees of this event
    /// </summary>
    public virtual ICollection<EventAttendee> Attendees { get; set; } = new List<EventAttendee>();

    /// <summary>
    /// Reminders for this event
    /// </summary>
    public virtual ICollection<EventReminder> Reminders { get; set; } = new List<EventReminder>();

    /// <summary>
    /// External calendar sync ID (for Google Calendar, Outlook, etc.)
    /// </summary>
    [MaxLength(500)]
    public virtual string? ExternalCalendarId { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public virtual string? Metadata { get; set; }

    /// <summary>
    /// Number of views/impressions
    /// </summary>
    public virtual int ViewCount { get; set; }

    /// <summary>
    /// Check if event is currently happening
    /// </summary>
    public bool IsOngoing => DateTime.UtcNow >= StartTime && DateTime.UtcNow <= EndTime;

    /// <summary>
    /// Check if event has ended
    /// </summary>
    public bool HasEnded => DateTime.UtcNow > EndTime;

    /// <summary>
    /// Check if registration is still open
    /// </summary>
    public bool IsRegistrationOpen => !HasEnded && 
        (RegistrationDeadline == null || DateTime.UtcNow <= RegistrationDeadline);

    /// <summary>
    /// Get the duration of the event
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;
}
