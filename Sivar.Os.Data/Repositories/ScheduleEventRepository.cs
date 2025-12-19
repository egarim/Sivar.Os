using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for schedule event operations
/// </summary>
public class ScheduleEventRepository : IScheduleEventRepository
{
    private readonly SivarDbContext _context;
    private readonly ILogger<ScheduleEventRepository> _logger;

    public ScheduleEventRepository(SivarDbContext context, ILogger<ScheduleEventRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Event CRUD

    public async Task<ScheduleEvent?> GetByIdAsync(Guid eventId, bool includeAttendees = false, bool includeReminders = false)
    {
        var query = _context.ScheduleEvents
            .Include(e => e.Profile)
            .Include(e => e.RecurrenceRule)
            .AsQueryable();

        if (includeAttendees)
        {
            query = query.Include(e => e.Attendees).ThenInclude(a => a.Profile);
        }

        if (includeReminders)
        {
            query = query.Include(e => e.Reminders);
        }

        return await query.FirstOrDefaultAsync(e => e.Id == eventId);
    }

    public async Task<(IEnumerable<ScheduleEvent> Events, int TotalCount)> GetProfileEventsAsync(
        Guid profileId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.ScheduleEvents
            .Include(e => e.Profile)
            .Include(e => e.RecurrenceRule)
            .Where(e => e.ProfileId == profileId);

        if (startDate.HasValue)
        {
            query = query.Where(e => e.EndTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.StartTime <= endDate.Value);
        }

        var totalCount = await query.CountAsync();

        var events = await query
            .OrderBy(e => e.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (events, totalCount);
    }

    public async Task<IEnumerable<ScheduleEvent>> GetEventsInRangeAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? profileId = null,
        EventVisibility? visibility = null)
    {
        var query = _context.ScheduleEvents
            .Include(e => e.Profile)
            .Where(e => e.StartTime <= endDate && e.EndTime >= startDate)
            .Where(e => e.Status != EventStatus.Cancelled);

        if (profileId.HasValue)
        {
            query = query.Where(e => e.ProfileId == profileId.Value);
        }

        if (visibility.HasValue)
        {
            query = query.Where(e => e.Visibility == visibility.Value);
        }

        return await query.OrderBy(e => e.StartTime).ToListAsync();
    }

    public async Task<(IEnumerable<ScheduleEvent> Events, int TotalCount)> GetPublicEventsAsync(
        DateTime? startDate = null,
        EventType? eventType = null,
        string? category = null,
        double? latitude = null,
        double? longitude = null,
        double? radiusKm = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.ScheduleEvents
            .Include(e => e.Profile)
            .Where(e => e.Visibility == EventVisibility.Public)
            .Where(e => e.Status == EventStatus.Confirmed);

        var now = startDate ?? DateTime.UtcNow;
        query = query.Where(e => e.EndTime >= now);

        if (eventType.HasValue)
        {
            query = query.Where(e => e.EventType == eventType.Value);
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(e => e.Category == category);
        }

        // Location-based filtering (if coordinates provided)
        if (latitude.HasValue && longitude.HasValue && radiusKm.HasValue)
        {
            // Simple bounding box filter (for performance)
            // More accurate distance calculation would use PostGIS
            var latDelta = radiusKm.Value / 111.0; // ~111km per degree latitude
            var lonDelta = radiusKm.Value / (111.0 * Math.Cos(latitude.Value * Math.PI / 180));

            query = query.Where(e => e.Latitude.HasValue && e.Longitude.HasValue &&
                e.Latitude >= latitude.Value - latDelta &&
                e.Latitude <= latitude.Value + latDelta &&
                e.Longitude >= longitude.Value - lonDelta &&
                e.Longitude <= longitude.Value + lonDelta);
        }

        var totalCount = await query.CountAsync();

        var events = await query
            .OrderBy(e => e.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (events, totalCount);
    }

    public async Task<(IEnumerable<ScheduleEvent> Events, int TotalCount)> GetAttendingEventsAsync(
        Guid profileId,
        DateTime? startDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.EventAttendees
            .Include(a => a.Event)
            .ThenInclude(e => e!.Profile)
            .Where(a => a.ProfileId == profileId)
            .Where(a => a.Status == AttendeeStatus.Accepted)
            .Select(a => a.Event!);

        var now = startDate ?? DateTime.UtcNow;
        query = query.Where(e => e.EndTime >= now);

        var totalCount = await query.CountAsync();

        var events = await query
            .OrderBy(e => e.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (events, totalCount);
    }

    public async Task<IEnumerable<ScheduleEvent>> GetUpcomingEventsAsync(Guid profileId, int limit = 10)
    {
        var now = DateTime.UtcNow;

        // Get events owned by profile
        var ownedEvents = await _context.ScheduleEvents
            .Include(e => e.Profile)
            .Where(e => e.ProfileId == profileId)
            .Where(e => e.EndTime >= now)
            .Where(e => e.Status != EventStatus.Cancelled)
            .OrderBy(e => e.StartTime)
            .Take(limit)
            .ToListAsync();

        // Get events profile is attending
        var attendingEventIds = await _context.EventAttendees
            .Where(a => a.ProfileId == profileId)
            .Where(a => a.Status == AttendeeStatus.Accepted)
            .Select(a => a.EventId)
            .ToListAsync();

        var attendingEvents = await _context.ScheduleEvents
            .Include(e => e.Profile)
            .Where(e => attendingEventIds.Contains(e.Id))
            .Where(e => e.EndTime >= now)
            .Where(e => e.Status != EventStatus.Cancelled)
            .OrderBy(e => e.StartTime)
            .Take(limit)
            .ToListAsync();

        // Combine, dedupe, and sort
        return ownedEvents
            .Union(attendingEvents)
            .DistinctBy(e => e.Id)
            .OrderBy(e => e.StartTime)
            .Take(limit);
    }

    public async Task<ScheduleEvent> CreateAsync(ScheduleEvent scheduleEvent)
    {
        _context.ScheduleEvents.Add(scheduleEvent);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("[ScheduleEventRepository] Created event {EventId} for profile {ProfileId}", 
            scheduleEvent.Id, scheduleEvent.ProfileId);
        
        return scheduleEvent;
    }

    public async Task<ScheduleEvent> UpdateAsync(ScheduleEvent scheduleEvent)
    {
        _context.ScheduleEvents.Update(scheduleEvent);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("[ScheduleEventRepository] Updated event {EventId}", scheduleEvent.Id);
        
        return scheduleEvent;
    }

    public async Task<bool> DeleteAsync(Guid eventId)
    {
        var scheduleEvent = await _context.ScheduleEvents.FindAsync(eventId);
        if (scheduleEvent == null)
        {
            return false;
        }

        scheduleEvent.IsDeleted = true;
        scheduleEvent.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("[ScheduleEventRepository] Deleted event {EventId}", eventId);
        
        return true;
    }

    public async Task<(IEnumerable<ScheduleEvent> Events, int TotalCount)> SearchAsync(
        string query,
        DateTime? startDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var searchQuery = _context.ScheduleEvents
            .Include(e => e.Profile)
            .Where(e => e.Visibility == EventVisibility.Public)
            .Where(e => e.Status == EventStatus.Confirmed)
            .Where(e => EF.Functions.ILike(e.Title, $"%{query}%") ||
                       (e.Description != null && EF.Functions.ILike(e.Description, $"%{query}%")));

        if (startDate.HasValue)
        {
            searchQuery = searchQuery.Where(e => e.EndTime >= startDate.Value);
        }

        var totalCount = await searchQuery.CountAsync();

        var events = await searchQuery
            .OrderBy(e => e.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (events, totalCount);
    }

    #endregion

    #region Attendees

    public async Task<IEnumerable<EventAttendee>> GetAttendeesAsync(Guid eventId, AttendeeStatus? status = null)
    {
        var query = _context.EventAttendees
            .Include(a => a.Profile)
            .Where(a => a.EventId == eventId);

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        return await query.OrderBy(a => a.CreatedAt).ToListAsync();
    }

    public async Task<int> GetAttendeeCountAsync(Guid eventId, AttendeeStatus? status = null)
    {
        var query = _context.EventAttendees.Where(a => a.EventId == eventId);

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        return await query.CountAsync();
    }

    public async Task<EventAttendee?> GetAttendeeAsync(Guid eventId, Guid profileId)
    {
        return await _context.EventAttendees
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(a => a.EventId == eventId && a.ProfileId == profileId);
    }

    public async Task<EventAttendee> AddAttendeeAsync(EventAttendee attendee)
    {
        _context.EventAttendees.Add(attendee);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("[ScheduleEventRepository] Added attendee {ProfileId} to event {EventId}", 
            attendee.ProfileId, attendee.EventId);
        
        return attendee;
    }

    public async Task<EventAttendee> UpdateAttendeeAsync(EventAttendee attendee)
    {
        _context.EventAttendees.Update(attendee);
        await _context.SaveChangesAsync();
        return attendee;
    }

    public async Task<bool> RemoveAttendeeAsync(Guid eventId, Guid profileId)
    {
        var attendee = await _context.EventAttendees
            .FirstOrDefaultAsync(a => a.EventId == eventId && a.ProfileId == profileId);
        
        if (attendee == null)
        {
            return false;
        }

        attendee.IsDeleted = true;
        attendee.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("[ScheduleEventRepository] Removed attendee {ProfileId} from event {EventId}", 
            profileId, eventId);
        
        return true;
    }

    #endregion

    #region Reminders

    public async Task<IEnumerable<EventReminder>> GetRemindersAsync(Guid eventId)
    {
        return await _context.EventReminders
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.ReminderTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<EventReminder>> GetPendingRemindersAsync(DateTime beforeTime)
    {
        return await _context.EventReminders
            .Include(r => r.Event)
            .Include(r => r.Profile)
            .Where(r => !r.IsSent)
            .Where(r => r.ReminderTime <= beforeTime)
            .OrderBy(r => r.ReminderTime)
            .ToListAsync();
    }

    public async Task<EventReminder> AddReminderAsync(EventReminder reminder)
    {
        _context.EventReminders.Add(reminder);
        await _context.SaveChangesAsync();
        return reminder;
    }

    public async Task<bool> MarkReminderSentAsync(Guid reminderId)
    {
        var reminder = await _context.EventReminders.FindAsync(reminderId);
        if (reminder == null)
        {
            return false;
        }

        reminder.IsSent = true;
        reminder.SentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteReminderAsync(Guid reminderId)
    {
        var reminder = await _context.EventReminders.FindAsync(reminderId);
        if (reminder == null)
        {
            return false;
        }

        reminder.IsDeleted = true;
        reminder.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Recurrence

    public async Task<RecurrenceRule?> GetRecurrenceRuleAsync(Guid eventId)
    {
        return await _context.RecurrenceRules
            .FirstOrDefaultAsync(r => r.EventId == eventId);
    }

    public async Task<RecurrenceRule> CreateRecurrenceRuleAsync(RecurrenceRule rule)
    {
        _context.RecurrenceRules.Add(rule);
        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<RecurrenceRule> UpdateRecurrenceRuleAsync(RecurrenceRule rule)
    {
        _context.RecurrenceRules.Update(rule);
        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<bool> DeleteRecurrenceRuleAsync(Guid ruleId)
    {
        var rule = await _context.RecurrenceRules.FindAsync(ruleId);
        if (rule == null)
        {
            return false;
        }

        rule.IsDeleted = true;
        rule.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion
}
