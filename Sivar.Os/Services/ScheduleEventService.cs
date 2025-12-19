using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service implementation for schedule event operations
/// </summary>
public class ScheduleEventService : IScheduleEventService
{
    private readonly IScheduleEventRepository _repository;
    private readonly ILogger<ScheduleEventService> _logger;

    public ScheduleEventService(
        IScheduleEventRepository repository,
        ILogger<ScheduleEventService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    #region Event CRUD

    public async Task<ScheduleEventDto?> GetEventAsync(Guid eventId, Guid? viewerProfileId = null)
    {
        var scheduleEvent = await _repository.GetByIdAsync(eventId, includeAttendees: true);
        if (scheduleEvent == null)
        {
            return null;
        }

        // Check visibility
        if (scheduleEvent.Visibility != EventVisibility.Public && 
            viewerProfileId != scheduleEvent.ProfileId)
        {
            // Check if viewer is an attendee
            if (scheduleEvent.Visibility == EventVisibility.AttendeesOnly)
            {
                var isAttendee = scheduleEvent.Attendees?.Any(a => a.ProfileId == viewerProfileId) ?? false;
                if (!isAttendee)
                {
                    return null;
                }
            }
            else if (scheduleEvent.Visibility == EventVisibility.Private)
            {
                return null;
            }
            // TODO: Handle FollowersOnly visibility
        }

        return MapToDto(scheduleEvent, viewerProfileId);
    }

    public async Task<EventListResponseDto> GetProfileEventsAsync(
        Guid profileId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var (events, totalCount) = await _repository.GetProfileEventsAsync(
            profileId, startDate, endDate, page, pageSize);

        return new EventListResponseDto
        {
            Events = events.Select(e => MapToDto(e, profileId)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<EventCalendarItemDto>> GetCalendarEventsAsync(
        Guid profileId,
        DateTime startDate,
        DateTime endDate)
    {
        var events = await _repository.GetEventsInRangeAsync(startDate, endDate, profileId);

        return events.Select(e => new EventCalendarItemDto
        {
            Id = e.Id,
            Title = e.Title,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            IsAllDay = e.IsAllDay,
            Color = e.Color,
            EventType = e.EventType,
            Status = e.Status,
            IsRecurring = e.RecurrenceRuleId.HasValue,
            ProfileId = e.ProfileId
        }).ToList();
    }

    public async Task<EventListResponseDto> GetPublicEventsAsync(
        DateTime? startDate = null,
        EventType? eventType = null,
        string? category = null,
        double? latitude = null,
        double? longitude = null,
        double? radiusKm = null,
        int page = 1,
        int pageSize = 20)
    {
        var (events, totalCount) = await _repository.GetPublicEventsAsync(
            startDate, eventType, category, latitude, longitude, radiusKm, page, pageSize);

        return new EventListResponseDto
        {
            Events = events.Select(e => MapToDto(e, null)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<EventListResponseDto> GetAttendingEventsAsync(
        Guid profileId,
        DateTime? startDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var (events, totalCount) = await _repository.GetAttendingEventsAsync(
            profileId, startDate, page, pageSize);

        return new EventListResponseDto
        {
            Events = events.Select(e => MapToDto(e, profileId)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<ScheduleEventDto>> GetUpcomingEventsAsync(Guid profileId, int limit = 10)
    {
        var events = await _repository.GetUpcomingEventsAsync(profileId, limit);
        return events.Select(e => MapToDto(e, profileId)).ToList();
    }

    public async Task<EventListResponseDto> SearchEventsAsync(
        string query,
        DateTime? startDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var (events, totalCount) = await _repository.SearchAsync(query, startDate, page, pageSize);

        return new EventListResponseDto
        {
            Events = events.Select(e => MapToDto(e, null)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ScheduleEventDto> CreateEventAsync(Guid profileId, CreateScheduleEventDto createDto)
    {
        _logger.LogInformation("[ScheduleEventService] Creating event for profile {ProfileId}: {Title}", 
            profileId, createDto.Title);

        var scheduleEvent = new ScheduleEvent
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            Title = createDto.Title,
            Description = createDto.Description,
            StartTime = createDto.StartTime,
            EndTime = createDto.EndTime,
            IsAllDay = createDto.IsAllDay,
            TimeZone = createDto.TimeZone,
            EventType = createDto.EventType,
            Visibility = createDto.Visibility,
            Status = EventStatus.Confirmed,
            Location = createDto.Location,
            Latitude = createDto.Latitude,
            Longitude = createDto.Longitude,
            VirtualLink = createDto.VirtualLink,
            IsVirtual = createDto.IsVirtual,
            CoverImageUrl = createDto.CoverImageUrl,
            Color = createDto.Color,
            MaxAttendees = createDto.MaxAttendees,
            RequiresRegistration = createDto.RequiresRegistration,
            RegistrationDeadline = createDto.RegistrationDeadline,
            Price = createDto.Price,
            Currency = createDto.Currency,
            Category = createDto.Category
        };

        // Create the event
        var created = await _repository.CreateAsync(scheduleEvent);

        // Add recurrence rule if provided
        if (createDto.Recurrence != null && createDto.Recurrence.Frequency != RecurrenceFrequency.None)
        {
            var rule = new RecurrenceRule
            {
                Id = Guid.NewGuid(),
                EventId = created.Id,
                Frequency = createDto.Recurrence.Frequency,
                Interval = createDto.Recurrence.Interval,
                ByDay = createDto.Recurrence.ByDay,
                ByMonthDay = createDto.Recurrence.ByMonthDay,
                ByMonth = createDto.Recurrence.ByMonth,
                Count = createDto.Recurrence.Count,
                Until = createDto.Recurrence.Until
            };

            await _repository.CreateRecurrenceRuleAsync(rule);
            created.RecurrenceRuleId = rule.Id;
            await _repository.UpdateAsync(created);
        }

        // Add default reminders if requested
        if (createDto.ReminderMinutesBefore?.Any() == true)
        {
            foreach (var minutes in createDto.ReminderMinutesBefore)
            {
                var reminder = new EventReminder
                {
                    Id = Guid.NewGuid(),
                    EventId = created.Id,
                    ProfileId = profileId,
                    MinutesBefore = minutes,
                    ReminderTime = created.StartTime.AddMinutes(-minutes),
                    ReminderType = ReminderType.Push
                };
                await _repository.AddReminderAsync(reminder);
            }
        }

        // Add owner as organizer attendee
        var ownerAttendee = new EventAttendee
        {
            Id = Guid.NewGuid(),
            EventId = created.Id,
            ProfileId = profileId,
            Status = AttendeeStatus.Accepted,
            Role = AttendeeRole.Organizer,
            RespondedAt = DateTime.UtcNow
        };
        await _repository.AddAttendeeAsync(ownerAttendee);

        // Reload with relationships
        var result = await _repository.GetByIdAsync(created.Id, includeAttendees: true);
        return MapToDto(result!, profileId);
    }

    public async Task<ScheduleEventDto?> UpdateEventAsync(Guid eventId, Guid profileId, UpdateScheduleEventDto updateDto)
    {
        var scheduleEvent = await _repository.GetByIdAsync(eventId);
        if (scheduleEvent == null)
        {
            return null;
        }

        // Check ownership
        if (scheduleEvent.ProfileId != profileId)
        {
            _logger.LogWarning("[ScheduleEventService] Profile {ProfileId} attempted to update event {EventId} owned by {OwnerId}",
                profileId, eventId, scheduleEvent.ProfileId);
            return null;
        }

        // Apply updates
        if (updateDto.Title != null) scheduleEvent.Title = updateDto.Title;
        if (updateDto.Description != null) scheduleEvent.Description = updateDto.Description;
        if (updateDto.StartTime.HasValue) scheduleEvent.StartTime = updateDto.StartTime.Value;
        if (updateDto.EndTime.HasValue) scheduleEvent.EndTime = updateDto.EndTime.Value;
        if (updateDto.IsAllDay.HasValue) scheduleEvent.IsAllDay = updateDto.IsAllDay.Value;
        if (updateDto.TimeZone != null) scheduleEvent.TimeZone = updateDto.TimeZone;
        if (updateDto.EventType.HasValue) scheduleEvent.EventType = updateDto.EventType.Value;
        if (updateDto.Visibility.HasValue) scheduleEvent.Visibility = updateDto.Visibility.Value;
        if (updateDto.Status.HasValue) scheduleEvent.Status = updateDto.Status.Value;
        if (updateDto.Location != null) scheduleEvent.Location = updateDto.Location;
        if (updateDto.Latitude.HasValue) scheduleEvent.Latitude = updateDto.Latitude;
        if (updateDto.Longitude.HasValue) scheduleEvent.Longitude = updateDto.Longitude;
        if (updateDto.VirtualLink != null) scheduleEvent.VirtualLink = updateDto.VirtualLink;
        if (updateDto.IsVirtual.HasValue) scheduleEvent.IsVirtual = updateDto.IsVirtual.Value;
        if (updateDto.CoverImageUrl != null) scheduleEvent.CoverImageUrl = updateDto.CoverImageUrl;
        if (updateDto.Color != null) scheduleEvent.Color = updateDto.Color;
        if (updateDto.MaxAttendees.HasValue) scheduleEvent.MaxAttendees = updateDto.MaxAttendees;
        if (updateDto.RequiresRegistration.HasValue) scheduleEvent.RequiresRegistration = updateDto.RequiresRegistration.Value;
        if (updateDto.RegistrationDeadline.HasValue) scheduleEvent.RegistrationDeadline = updateDto.RegistrationDeadline;
        if (updateDto.Price.HasValue) scheduleEvent.Price = updateDto.Price.Value;
        if (updateDto.Currency != null) scheduleEvent.Currency = updateDto.Currency;
        if (updateDto.Category != null) scheduleEvent.Category = updateDto.Category;

        var updated = await _repository.UpdateAsync(scheduleEvent);
        return MapToDto(updated, profileId);
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, Guid profileId)
    {
        var scheduleEvent = await _repository.GetByIdAsync(eventId);
        if (scheduleEvent == null || scheduleEvent.ProfileId != profileId)
        {
            return false;
        }

        return await _repository.DeleteAsync(eventId);
    }

    public async Task<bool> CancelEventAsync(Guid eventId, Guid profileId, string? reason = null)
    {
        var scheduleEvent = await _repository.GetByIdAsync(eventId);
        if (scheduleEvent == null || scheduleEvent.ProfileId != profileId)
        {
            return false;
        }

        scheduleEvent.Status = EventStatus.Cancelled;
        await _repository.UpdateAsync(scheduleEvent);

        // TODO: Notify attendees about cancellation

        _logger.LogInformation("[ScheduleEventService] Event {EventId} cancelled by {ProfileId}. Reason: {Reason}",
            eventId, profileId, reason ?? "No reason provided");

        return true;
    }

    #endregion

    #region RSVP & Attendance

    public async Task<List<EventAttendeeDto>> GetAttendeesAsync(Guid eventId)
    {
        var attendees = await _repository.GetAttendeesAsync(eventId);
        return attendees.Select(MapAttendeeToDto).ToList();
    }

    public async Task<EventAttendeeDto?> RsvpAsync(Guid eventId, Guid profileId, EventRsvpDto rsvpDto)
    {
        var scheduleEvent = await _repository.GetByIdAsync(eventId);
        if (scheduleEvent == null)
        {
            return null;
        }

        // Check if registration is still open
        if (!scheduleEvent.IsRegistrationOpen)
        {
            _logger.LogWarning("[ScheduleEventService] RSVP attempt for event {EventId} but registration is closed", eventId);
            return null;
        }

        // Check max attendees
        if (scheduleEvent.MaxAttendees.HasValue && rsvpDto.Status == AttendeeStatus.Accepted)
        {
            var acceptedCount = await _repository.GetAttendeeCountAsync(eventId, AttendeeStatus.Accepted);
            if (acceptedCount >= scheduleEvent.MaxAttendees.Value)
            {
                _logger.LogWarning("[ScheduleEventService] Event {EventId} is at max capacity", eventId);
                // Could return waitlisted status instead
                return null;
            }
        }

        var existingAttendee = await _repository.GetAttendeeAsync(eventId, profileId);

        if (existingAttendee != null)
        {
            // Update existing RSVP
            existingAttendee.Status = rsvpDto.Status;
            existingAttendee.Note = rsvpDto.Note;
            existingAttendee.GuestCount = rsvpDto.GuestCount;
            existingAttendee.RespondedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAttendeeAsync(existingAttendee);
            return MapAttendeeToDto(updated);
        }
        else
        {
            // Create new RSVP
            var attendee = new EventAttendee
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                ProfileId = profileId,
                Status = rsvpDto.Status,
                Role = AttendeeRole.Attendee,
                Note = rsvpDto.Note,
                GuestCount = rsvpDto.GuestCount,
                RespondedAt = DateTime.UtcNow
            };

            var created = await _repository.AddAttendeeAsync(attendee);
            
            // Reload with profile
            var result = await _repository.GetAttendeeAsync(eventId, profileId);
            return MapAttendeeToDto(result!);
        }
    }

    public async Task<bool> CancelRsvpAsync(Guid eventId, Guid profileId)
    {
        return await _repository.RemoveAttendeeAsync(eventId, profileId);
    }

    public async Task<bool> CheckInAttendeeAsync(Guid eventId, Guid attendeeProfileId, Guid ownerProfileId)
    {
        var scheduleEvent = await _repository.GetByIdAsync(eventId);
        if (scheduleEvent == null || scheduleEvent.ProfileId != ownerProfileId)
        {
            return false;
        }

        var attendee = await _repository.GetAttendeeAsync(eventId, attendeeProfileId);
        if (attendee == null)
        {
            return false;
        }

        attendee.Status = AttendeeStatus.CheckedIn;
        attendee.CheckedInAt = DateTime.UtcNow;
        await _repository.UpdateAttendeeAsync(attendee);

        return true;
    }

    public async Task<AttendeeStatus?> GetRsvpStatusAsync(Guid eventId, Guid profileId)
    {
        var attendee = await _repository.GetAttendeeAsync(eventId, profileId);
        return attendee?.Status;
    }

    #endregion

    #region Reminders

    public async Task<List<EventReminderDto>> GetRemindersAsync(Guid eventId, Guid profileId)
    {
        var reminders = await _repository.GetRemindersAsync(eventId);
        return reminders
            .Where(r => r.ProfileId == profileId)
            .Select(MapReminderToDto)
            .ToList();
    }

    public async Task<EventReminderDto?> AddReminderAsync(Guid eventId, Guid profileId, int minutesBefore, ReminderType type = ReminderType.Push)
    {
        var scheduleEvent = await _repository.GetByIdAsync(eventId);
        if (scheduleEvent == null)
        {
            return null;
        }

        var reminder = new EventReminder
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ProfileId = profileId,
            MinutesBefore = minutesBefore,
            ReminderTime = scheduleEvent.StartTime.AddMinutes(-minutesBefore),
            ReminderType = type
        };

        var created = await _repository.AddReminderAsync(reminder);
        return MapReminderToDto(created);
    }

    public async Task<bool> RemoveReminderAsync(Guid reminderId, Guid profileId)
    {
        // TODO: Verify ownership before deleting
        return await _repository.DeleteReminderAsync(reminderId);
    }

    public async Task ProcessPendingRemindersAsync()
    {
        var now = DateTime.UtcNow;
        var pendingReminders = await _repository.GetPendingRemindersAsync(now);

        foreach (var reminder in pendingReminders)
        {
            try
            {
                // TODO: Send notification based on ReminderType
                _logger.LogInformation("[ScheduleEventService] Sending reminder {ReminderId} for event {EventId} to profile {ProfileId}",
                    reminder.Id, reminder.EventId, reminder.ProfileId);

                await _repository.MarkReminderSentAsync(reminder.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ScheduleEventService] Failed to send reminder {ReminderId}", reminder.Id);
            }
        }
    }

    #endregion

    #region Recurrence

    public async Task<RecurrenceRuleDto?> GetRecurrenceRuleAsync(Guid eventId)
    {
        var rule = await _repository.GetRecurrenceRuleAsync(eventId);
        if (rule == null)
        {
            return null;
        }

        return new RecurrenceRuleDto
        {
            Id = rule.Id,
            Frequency = rule.Frequency,
            Interval = rule.Interval,
            ByDay = rule.ByDay,
            ByMonthDay = rule.ByMonthDay,
            ByMonth = rule.ByMonth,
            Count = rule.Count,
            Until = rule.Until,
            Description = rule.GetDescription(),
            RRule = rule.ToRRule()
        };
    }

    public async Task<List<DateTime>> GenerateRecurrenceInstancesAsync(Guid eventId, DateTime until)
    {
        var scheduleEvent = await _repository.GetByIdAsync(eventId);
        if (scheduleEvent?.RecurrenceRule == null)
        {
            return new List<DateTime>();
        }

        var rule = scheduleEvent.RecurrenceRule;
        var instances = new List<DateTime>();
        var current = scheduleEvent.StartTime;
        var count = 0;
        var maxInstances = rule.Count ?? 365; // Default max

        while (current <= until && count < maxInstances)
        {
            instances.Add(current);
            count++;

            current = rule.Frequency switch
            {
                RecurrenceFrequency.Daily => current.AddDays(rule.Interval),
                RecurrenceFrequency.Weekly => current.AddDays(7 * rule.Interval),
                RecurrenceFrequency.Monthly => current.AddMonths(rule.Interval),
                RecurrenceFrequency.Yearly => current.AddYears(rule.Interval),
                _ => current.AddDays(1)
            };

            if (rule.Until.HasValue && current > rule.Until.Value)
            {
                break;
            }
        }

        return instances;
    }

    #endregion

    #region Analytics

    public async Task IncrementViewCountAsync(Guid eventId)
    {
        var scheduleEvent = await _repository.GetByIdAsync(eventId);
        if (scheduleEvent != null)
        {
            scheduleEvent.ViewCount++;
            await _repository.UpdateAsync(scheduleEvent);
        }
    }

    public async Task<EventStatsDto> GetEventStatsAsync(Guid eventId)
    {
        var attendees = await _repository.GetAttendeesAsync(eventId);
        var attendeeList = attendees.ToList();

        return new EventStatsDto
        {
            TotalAttendees = attendeeList.Count,
            AcceptedCount = attendeeList.Count(a => a.Status == AttendeeStatus.Accepted),
            DeclinedCount = attendeeList.Count(a => a.Status == AttendeeStatus.Declined),
            PendingCount = attendeeList.Count(a => a.Status == AttendeeStatus.Pending),
            TentativeCount = attendeeList.Count(a => a.Status == AttendeeStatus.Tentative),
            CheckedInCount = attendeeList.Count(a => a.Status == AttendeeStatus.CheckedIn),
            TotalRevenue = attendeeList.Sum(a => a.AmountPaid ?? 0)
        };
    }

    #endregion

    #region Mapping

    private ScheduleEventDto MapToDto(ScheduleEvent e, Guid? viewerProfileId)
    {
        var attendeeCount = e.Attendees?.Count(a => a.Status == AttendeeStatus.Accepted) ?? 0;
        var myStatus = viewerProfileId.HasValue
            ? e.Attendees?.FirstOrDefault(a => a.ProfileId == viewerProfileId)?.Status
            : null;

        return new ScheduleEventDto
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            IsAllDay = e.IsAllDay,
            TimeZone = e.TimeZone,
            ProfileId = e.ProfileId,
            ProfileDisplayName = e.Profile?.DisplayName ?? string.Empty,
            ProfileAvatarUrl = e.Profile?.Avatar,
            EventType = e.EventType,
            Visibility = e.Visibility,
            Status = e.Status,
            Location = e.Location,
            Latitude = e.Latitude,
            Longitude = e.Longitude,
            VirtualLink = e.VirtualLink,
            IsVirtual = e.IsVirtual,
            CoverImageUrl = e.CoverImageUrl,
            Color = e.Color,
            Category = e.Category,
            MaxAttendees = e.MaxAttendees,
            RequiresRegistration = e.RequiresRegistration,
            RegistrationDeadline = e.RegistrationDeadline,
            Price = e.Price,
            Currency = e.Currency,
            IsRecurring = e.RecurrenceRuleId.HasValue,
            RecurrenceDescription = e.RecurrenceRule?.GetDescription(),
            AttendeeCount = attendeeCount,
            ViewCount = e.ViewCount,
            IsOwner = viewerProfileId == e.ProfileId,
            MyAttendeeStatus = myStatus,
            IsOngoing = e.IsOngoing,
            HasEnded = e.HasEnded,
            IsRegistrationOpen = e.IsRegistrationOpen,
            Duration = e.Duration,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }

    private EventAttendeeDto MapAttendeeToDto(EventAttendee a)
    {
        return new EventAttendeeDto
        {
            Id = a.Id,
            EventId = a.EventId,
            ProfileId = a.ProfileId,
            ProfileDisplayName = a.Profile?.DisplayName ?? string.Empty,
            ProfileAvatarUrl = a.Profile?.Avatar,
            Status = a.Status,
            Role = a.Role,
            RespondedAt = a.RespondedAt,
            CheckedInAt = a.CheckedInAt,
            Note = a.Note,
            GuestCount = a.GuestCount
        };
    }

    private EventReminderDto MapReminderToDto(EventReminder r)
    {
        return new EventReminderDto
        {
            Id = r.Id,
            EventId = r.EventId,
            MinutesBefore = r.MinutesBefore,
            ReminderType = r.ReminderType,
            ReminderTime = r.ReminderTime,
            IsSent = r.IsSent,
            CustomMessage = r.CustomMessage
        };
    }

    #endregion
}
