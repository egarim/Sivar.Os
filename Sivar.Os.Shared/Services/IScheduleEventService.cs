using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for schedule event operations
/// </summary>
public interface IScheduleEventService
{
    #region Event CRUD

    /// <summary>
    /// Get an event by ID
    /// </summary>
    Task<ScheduleEventDto?> GetEventAsync(Guid eventId, Guid? viewerProfileId = null);

    /// <summary>
    /// Get events for a profile
    /// </summary>
    Task<EventListResponseDto> GetProfileEventsAsync(
        Guid profileId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Get events for calendar view (within date range)
    /// </summary>
    Task<List<EventCalendarItemDto>> GetCalendarEventsAsync(
        Guid profileId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Get public/discoverable events
    /// </summary>
    Task<EventListResponseDto> GetPublicEventsAsync(
        DateTime? startDate = null,
        EventType? eventType = null,
        string? category = null,
        double? latitude = null,
        double? longitude = null,
        double? radiusKm = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Get events the user is attending
    /// </summary>
    Task<EventListResponseDto> GetAttendingEventsAsync(
        Guid profileId,
        DateTime? startDate = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Get upcoming events for dashboard widget
    /// </summary>
    Task<List<ScheduleEventDto>> GetUpcomingEventsAsync(Guid profileId, int limit = 10);

    /// <summary>
    /// Search events
    /// </summary>
    Task<EventListResponseDto> SearchEventsAsync(
        string query,
        DateTime? startDate = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Create a new event
    /// </summary>
    Task<ScheduleEventDto> CreateEventAsync(Guid profileId, CreateScheduleEventDto createDto);

    /// <summary>
    /// Update an event
    /// </summary>
    Task<ScheduleEventDto?> UpdateEventAsync(Guid eventId, Guid profileId, UpdateScheduleEventDto updateDto);

    /// <summary>
    /// Delete an event
    /// </summary>
    Task<bool> DeleteEventAsync(Guid eventId, Guid profileId);

    /// <summary>
    /// Cancel an event
    /// </summary>
    Task<bool> CancelEventAsync(Guid eventId, Guid profileId, string? reason = null);

    #endregion

    #region RSVP & Attendance

    /// <summary>
    /// Get attendees for an event
    /// </summary>
    Task<List<EventAttendeeDto>> GetAttendeesAsync(Guid eventId);

    /// <summary>
    /// RSVP to an event
    /// </summary>
    Task<EventAttendeeDto?> RsvpAsync(Guid eventId, Guid profileId, EventRsvpDto rsvpDto);

    /// <summary>
    /// Cancel RSVP
    /// </summary>
    Task<bool> CancelRsvpAsync(Guid eventId, Guid profileId);

    /// <summary>
    /// Check in attendee (for event owners)
    /// </summary>
    Task<bool> CheckInAttendeeAsync(Guid eventId, Guid attendeeProfileId, Guid ownerProfileId);

    /// <summary>
    /// Get RSVP status for a profile
    /// </summary>
    Task<AttendeeStatus?> GetRsvpStatusAsync(Guid eventId, Guid profileId);

    #endregion

    #region Reminders

    /// <summary>
    /// Get reminders for an event
    /// </summary>
    Task<List<EventReminderDto>> GetRemindersAsync(Guid eventId, Guid profileId);

    /// <summary>
    /// Add a reminder
    /// </summary>
    Task<EventReminderDto?> AddReminderAsync(Guid eventId, Guid profileId, int minutesBefore, ReminderType type = ReminderType.Push);

    /// <summary>
    /// Remove a reminder
    /// </summary>
    Task<bool> RemoveReminderAsync(Guid reminderId, Guid profileId);

    /// <summary>
    /// Process pending reminders (for background job)
    /// </summary>
    Task ProcessPendingRemindersAsync();

    #endregion

    #region Recurrence

    /// <summary>
    /// Get recurrence rule for an event
    /// </summary>
    Task<RecurrenceRuleDto?> GetRecurrenceRuleAsync(Guid eventId);

    /// <summary>
    /// Generate recurring event instances
    /// </summary>
    Task<List<DateTime>> GenerateRecurrenceInstancesAsync(Guid eventId, DateTime until);

    #endregion

    #region Analytics

    /// <summary>
    /// Increment view count
    /// </summary>
    Task IncrementViewCountAsync(Guid eventId);

    /// <summary>
    /// Get event statistics
    /// </summary>
    Task<EventStatsDto> GetEventStatsAsync(Guid eventId);

    #endregion
}

/// <summary>
/// Event statistics DTO
/// </summary>
public class EventStatsDto
{
    public int TotalAttendees { get; set; }
    public int AcceptedCount { get; set; }
    public int DeclinedCount { get; set; }
    public int PendingCount { get; set; }
    public int TentativeCount { get; set; }
    public int CheckedInCount { get; set; }
    public int ViewCount { get; set; }
    public decimal TotalRevenue { get; set; }
}
