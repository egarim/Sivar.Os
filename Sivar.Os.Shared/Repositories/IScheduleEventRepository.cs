using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for schedule event operations
/// </summary>
public interface IScheduleEventRepository
{
    #region Event CRUD

    /// <summary>
    /// Get an event by ID
    /// </summary>
    Task<ScheduleEvent?> GetByIdAsync(Guid eventId, bool includeAttendees = false, bool includeReminders = false);

    /// <summary>
    /// Get events for a specific profile
    /// </summary>
    Task<(IEnumerable<ScheduleEvent> Events, int TotalCount)> GetProfileEventsAsync(
        Guid profileId, 
        DateTime? startDate = null, 
        DateTime? endDate = null,
        int page = 1, 
        int pageSize = 20);

    /// <summary>
    /// Get events within a date range (for calendar view)
    /// </summary>
    Task<IEnumerable<ScheduleEvent>> GetEventsInRangeAsync(
        DateTime startDate, 
        DateTime endDate,
        Guid? profileId = null,
        EventVisibility? visibility = null);

    /// <summary>
    /// Get public events (discoverable)
    /// </summary>
    Task<(IEnumerable<ScheduleEvent> Events, int TotalCount)> GetPublicEventsAsync(
        DateTime? startDate = null,
        EventType? eventType = null,
        string? category = null,
        double? latitude = null,
        double? longitude = null,
        double? radiusKm = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Get events the profile is attending
    /// </summary>
    Task<(IEnumerable<ScheduleEvent> Events, int TotalCount)> GetAttendingEventsAsync(
        Guid profileId,
        DateTime? startDate = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Get upcoming events for a profile (owned + attending)
    /// </summary>
    Task<IEnumerable<ScheduleEvent>> GetUpcomingEventsAsync(Guid profileId, int limit = 10);

    /// <summary>
    /// Create a new event
    /// </summary>
    Task<ScheduleEvent> CreateAsync(ScheduleEvent scheduleEvent);

    /// <summary>
    /// Update an event
    /// </summary>
    Task<ScheduleEvent> UpdateAsync(ScheduleEvent scheduleEvent);

    /// <summary>
    /// Delete an event (soft delete)
    /// </summary>
    Task<bool> DeleteAsync(Guid eventId);

    /// <summary>
    /// Search events by title/description
    /// </summary>
    Task<(IEnumerable<ScheduleEvent> Events, int TotalCount)> SearchAsync(
        string query,
        DateTime? startDate = null,
        int page = 1,
        int pageSize = 20);

    #endregion

    #region Attendees

    /// <summary>
    /// Get attendees for an event
    /// </summary>
    Task<IEnumerable<EventAttendee>> GetAttendeesAsync(Guid eventId, AttendeeStatus? status = null);

    /// <summary>
    /// Get attendee count for an event
    /// </summary>
    Task<int> GetAttendeeCountAsync(Guid eventId, AttendeeStatus? status = null);

    /// <summary>
    /// Get a specific attendee record
    /// </summary>
    Task<EventAttendee?> GetAttendeeAsync(Guid eventId, Guid profileId);

    /// <summary>
    /// Add an attendee to an event
    /// </summary>
    Task<EventAttendee> AddAttendeeAsync(EventAttendee attendee);

    /// <summary>
    /// Update an attendee record
    /// </summary>
    Task<EventAttendee> UpdateAttendeeAsync(EventAttendee attendee);

    /// <summary>
    /// Remove an attendee from an event
    /// </summary>
    Task<bool> RemoveAttendeeAsync(Guid eventId, Guid profileId);

    #endregion

    #region Reminders

    /// <summary>
    /// Get reminders for an event
    /// </summary>
    Task<IEnumerable<EventReminder>> GetRemindersAsync(Guid eventId);

    /// <summary>
    /// Get pending reminders that need to be sent
    /// </summary>
    Task<IEnumerable<EventReminder>> GetPendingRemindersAsync(DateTime beforeTime);

    /// <summary>
    /// Add a reminder
    /// </summary>
    Task<EventReminder> AddReminderAsync(EventReminder reminder);

    /// <summary>
    /// Mark reminder as sent
    /// </summary>
    Task<bool> MarkReminderSentAsync(Guid reminderId);

    /// <summary>
    /// Delete a reminder
    /// </summary>
    Task<bool> DeleteReminderAsync(Guid reminderId);

    #endregion

    #region Recurrence

    /// <summary>
    /// Get recurrence rule for an event
    /// </summary>
    Task<RecurrenceRule?> GetRecurrenceRuleAsync(Guid eventId);

    /// <summary>
    /// Create recurrence rule
    /// </summary>
    Task<RecurrenceRule> CreateRecurrenceRuleAsync(RecurrenceRule rule);

    /// <summary>
    /// Update recurrence rule
    /// </summary>
    Task<RecurrenceRule> UpdateRecurrenceRuleAsync(RecurrenceRule rule);

    /// <summary>
    /// Delete recurrence rule
    /// </summary>
    Task<bool> DeleteRecurrenceRuleAsync(Guid ruleId);

    #endregion
}
