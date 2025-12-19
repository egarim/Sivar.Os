using System.ComponentModel.DataAnnotations;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for displaying schedule events
/// </summary>
public class ScheduleEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAllDay { get; set; }
    public string TimeZone { get; set; } = "America/El_Salvador";
    
    // Owner info
    public Guid ProfileId { get; set; }
    public string ProfileDisplayName { get; set; } = string.Empty;
    public string? ProfileAvatarUrl { get; set; }
    
    // Event details
    public EventType EventType { get; set; }
    public EventVisibility Visibility { get; set; }
    public EventStatus Status { get; set; }
    
    // Location
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? VirtualLink { get; set; }
    public bool IsVirtual { get; set; }
    
    // Display
    public string? CoverImageUrl { get; set; }
    public string? Color { get; set; }
    public string? Category { get; set; }
    
    // Registration
    public int? MaxAttendees { get; set; }
    public bool RequiresRegistration { get; set; }
    public DateTime? RegistrationDeadline { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    
    // Recurrence
    public bool IsRecurring { get; set; }
    public string? RecurrenceDescription { get; set; }
    
    // Stats
    public int AttendeeCount { get; set; }
    public int ViewCount { get; set; }
    
    // User context
    public bool IsOwner { get; set; }
    public AttendeeStatus? MyAttendeeStatus { get; set; }
    
    // Computed
    public bool IsOngoing { get; set; }
    public bool HasEnded { get; set; }
    public bool IsRegistrationOpen { get; set; }
    public TimeSpan Duration { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new schedule event
/// </summary>
public class CreateScheduleEventDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public bool IsAllDay { get; set; }

    [MaxLength(100)]
    public string TimeZone { get; set; } = "America/El_Salvador";

    public EventType EventType { get; set; } = EventType.General;
    
    public EventVisibility Visibility { get; set; } = EventVisibility.Public;

    [MaxLength(500)]
    public string? Location { get; set; }

    public double? Latitude { get; set; }
    
    public double? Longitude { get; set; }

    [MaxLength(500)]
    public string? VirtualLink { get; set; }

    public bool IsVirtual { get; set; }

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }

    public int? MaxAttendees { get; set; }

    public bool RequiresRegistration { get; set; }

    public DateTime? RegistrationDeadline { get; set; }

    public decimal Price { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Recurrence settings (optional)
    /// </summary>
    public CreateRecurrenceRuleDto? Recurrence { get; set; }

    /// <summary>
    /// Initial reminder settings
    /// </summary>
    public List<int>? ReminderMinutesBefore { get; set; }
}

/// <summary>
/// DTO for updating a schedule event
/// </summary>
public class UpdateScheduleEventDto
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(5000)]
    public string? Description { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public bool? IsAllDay { get; set; }

    [MaxLength(100)]
    public string? TimeZone { get; set; }

    public EventType? EventType { get; set; }
    
    public EventVisibility? Visibility { get; set; }
    
    public EventStatus? Status { get; set; }

    [MaxLength(500)]
    public string? Location { get; set; }

    public double? Latitude { get; set; }
    
    public double? Longitude { get; set; }

    [MaxLength(500)]
    public string? VirtualLink { get; set; }

    public bool? IsVirtual { get; set; }

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }

    public int? MaxAttendees { get; set; }

    public bool? RequiresRegistration { get; set; }

    public DateTime? RegistrationDeadline { get; set; }

    public decimal? Price { get; set; }

    [MaxLength(3)]
    public string? Currency { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// For recurring events - apply changes to:
    /// "this" = only this instance
    /// "future" = this and future instances
    /// "all" = all instances
    /// </summary>
    public string? RecurrenceUpdateScope { get; set; }
}

/// <summary>
/// DTO for event summary in calendar view
/// </summary>
public class EventCalendarItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAllDay { get; set; }
    public string? Color { get; set; }
    public EventType EventType { get; set; }
    public EventStatus Status { get; set; }
    public bool IsRecurring { get; set; }
    public Guid ProfileId { get; set; }
}

/// <summary>
/// DTO for creating recurrence rules
/// </summary>
public class CreateRecurrenceRuleDto
{
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Weekly;
    
    public int Interval { get; set; } = 1;
    
    /// <summary>
    /// Days of week (e.g., "MO,WE,FR")
    /// </summary>
    public string? ByDay { get; set; }
    
    /// <summary>
    /// Days of month (e.g., "1,15")
    /// </summary>
    public string? ByMonthDay { get; set; }
    
    /// <summary>
    /// Months (e.g., "1,6,12")
    /// </summary>
    public string? ByMonth { get; set; }
    
    /// <summary>
    /// Number of occurrences (null = infinite)
    /// </summary>
    public int? Count { get; set; }
    
    /// <summary>
    /// End date for recurrence
    /// </summary>
    public DateTime? Until { get; set; }
}

/// <summary>
/// DTO for displaying recurrence rule
/// </summary>
public class RecurrenceRuleDto
{
    public Guid Id { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public int Interval { get; set; }
    public string? ByDay { get; set; }
    public string? ByMonthDay { get; set; }
    public string? ByMonth { get; set; }
    public int? Count { get; set; }
    public DateTime? Until { get; set; }
    public string Description { get; set; } = string.Empty;
    public string RRule { get; set; } = string.Empty;
}

/// <summary>
/// DTO for event attendee
/// </summary>
public class EventAttendeeDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid ProfileId { get; set; }
    public string ProfileDisplayName { get; set; } = string.Empty;
    public string? ProfileAvatarUrl { get; set; }
    public AttendeeStatus Status { get; set; }
    public AttendeeRole Role { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string? Note { get; set; }
    public int GuestCount { get; set; }
}

/// <summary>
/// DTO for RSVP to an event
/// </summary>
public class EventRsvpDto
{
    [Required]
    public AttendeeStatus Status { get; set; }
    
    [MaxLength(500)]
    public string? Note { get; set; }
    
    public int GuestCount { get; set; }
}

/// <summary>
/// DTO for event reminder
/// </summary>
public class EventReminderDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public int MinutesBefore { get; set; }
    public ReminderType ReminderType { get; set; }
    public DateTime ReminderTime { get; set; }
    public bool IsSent { get; set; }
    public string? CustomMessage { get; set; }
}

/// <summary>
/// Query parameters for fetching events
/// </summary>
public class EventQueryDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? ProfileId { get; set; }
    public EventType? EventType { get; set; }
    public EventStatus? Status { get; set; }
    public string? Category { get; set; }
    public bool IncludePrivate { get; set; }
    public bool OnlyAttending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Response for paginated event lists
/// </summary>
public class EventListResponseDto
{
    public List<ScheduleEventDto> Events { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
