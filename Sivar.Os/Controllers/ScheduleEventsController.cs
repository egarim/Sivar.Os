using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for schedule event operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScheduleEventsController : ControllerBase
{
    private readonly IScheduleEventService _scheduleEventService;
    private readonly IProfileService _profileService;
    private readonly ILogger<ScheduleEventsController> _logger;

    public ScheduleEventsController(
        IScheduleEventService scheduleEventService,
        IProfileService profileService,
        ILogger<ScheduleEventsController> logger)
    {
        _scheduleEventService = scheduleEventService;
        _profileService = profileService;
        _logger = logger;
    }

    #region Event CRUD

    /// <summary>
    /// Get a specific event by ID
    /// </summary>
    [HttpGet("{eventId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ScheduleEventDto>> GetEvent(Guid eventId)
    {
        var profileId = await GetCurrentProfileIdAsync();
        var scheduleEvent = await _scheduleEventService.GetEventAsync(eventId, profileId);

        if (scheduleEvent == null)
        {
            return NotFound();
        }

        // Increment view count
        await _scheduleEventService.IncrementViewCountAsync(eventId);

        return Ok(scheduleEvent);
    }

    /// <summary>
    /// Get events for the current profile
    /// </summary>
    [HttpGet("my-events")]
    public async Task<ActionResult<EventListResponseDto>> GetMyEvents(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var result = await _scheduleEventService.GetProfileEventsAsync(
            profileId.Value, startDate, endDate, page, pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Get events for a specific profile
    /// </summary>
    [HttpGet("profile/{profileId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<EventListResponseDto>> GetProfileEvents(
        Guid profileId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _scheduleEventService.GetProfileEventsAsync(
            profileId, startDate, endDate, page, pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Get calendar events for a date range
    /// </summary>
    [HttpGet("calendar")]
    public async Task<ActionResult<List<EventCalendarItemDto>>> GetCalendarEvents(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var result = await _scheduleEventService.GetCalendarEventsAsync(
            profileId.Value, startDate, endDate);

        return Ok(result);
    }

    /// <summary>
    /// Get public/discoverable events
    /// </summary>
    [HttpGet("discover")]
    [AllowAnonymous]
    public async Task<ActionResult<EventListResponseDto>> DiscoverEvents(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] EventType? eventType = null,
        [FromQuery] string? category = null,
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        [FromQuery] double? radiusKm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _scheduleEventService.GetPublicEventsAsync(
            startDate, eventType, category, latitude, longitude, radiusKm, page, pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Get events the current user is attending
    /// </summary>
    [HttpGet("attending")]
    public async Task<ActionResult<EventListResponseDto>> GetAttendingEvents(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var result = await _scheduleEventService.GetAttendingEventsAsync(
            profileId.Value, startDate, page, pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Get upcoming events for the current profile
    /// </summary>
    [HttpGet("upcoming")]
    public async Task<ActionResult<List<ScheduleEventDto>>> GetUpcomingEvents(
        [FromQuery] int limit = 10)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var result = await _scheduleEventService.GetUpcomingEventsAsync(profileId.Value, limit);
        return Ok(result);
    }

    /// <summary>
    /// Search events
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<EventListResponseDto>> SearchEvents(
        [FromQuery] string query,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return BadRequest("Query must be at least 2 characters");
        }

        var result = await _scheduleEventService.SearchEventsAsync(query, startDate, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Create a new event
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ScheduleEventDto>> CreateEvent([FromBody] CreateScheduleEventDto createDto)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        // Validate times
        if (createDto.EndTime <= createDto.StartTime)
        {
            return BadRequest("End time must be after start time");
        }

        var result = await _scheduleEventService.CreateEventAsync(profileId.Value, createDto);
        return CreatedAtAction(nameof(GetEvent), new { eventId = result.Id }, result);
    }

    /// <summary>
    /// Update an event
    /// </summary>
    [HttpPut("{eventId:guid}")]
    public async Task<ActionResult<ScheduleEventDto>> UpdateEvent(
        Guid eventId,
        [FromBody] UpdateScheduleEventDto updateDto)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var result = await _scheduleEventService.UpdateEventAsync(eventId, profileId.Value, updateDto);
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete an event
    /// </summary>
    [HttpDelete("{eventId:guid}")]
    public async Task<ActionResult> DeleteEvent(Guid eventId)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var success = await _scheduleEventService.DeleteEventAsync(eventId, profileId.Value);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Cancel an event
    /// </summary>
    [HttpPost("{eventId:guid}/cancel")]
    public async Task<ActionResult> CancelEvent(Guid eventId, [FromBody] CancelEventRequest? request = null)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var success = await _scheduleEventService.CancelEventAsync(eventId, profileId.Value, request?.Reason);
        if (!success)
        {
            return NotFound();
        }

        return Ok();
    }

    #endregion

    #region RSVP & Attendance

    /// <summary>
    /// Get attendees for an event
    /// </summary>
    [HttpGet("{eventId:guid}/attendees")]
    [AllowAnonymous]
    public async Task<ActionResult<List<EventAttendeeDto>>> GetAttendees(Guid eventId)
    {
        var result = await _scheduleEventService.GetAttendeesAsync(eventId);
        return Ok(result);
    }

    /// <summary>
    /// RSVP to an event
    /// </summary>
    [HttpPost("{eventId:guid}/rsvp")]
    public async Task<ActionResult<EventAttendeeDto>> Rsvp(
        Guid eventId,
        [FromBody] EventRsvpDto rsvpDto)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var result = await _scheduleEventService.RsvpAsync(eventId, profileId.Value, rsvpDto);
        if (result == null)
        {
            return BadRequest("Unable to RSVP. Event may be full or registration closed.");
        }

        return Ok(result);
    }

    /// <summary>
    /// Cancel RSVP
    /// </summary>
    [HttpDelete("{eventId:guid}/rsvp")]
    public async Task<ActionResult> CancelRsvp(Guid eventId)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var success = await _scheduleEventService.CancelRsvpAsync(eventId, profileId.Value);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Get my RSVP status for an event
    /// </summary>
    [HttpGet("{eventId:guid}/my-rsvp")]
    public async Task<ActionResult<AttendeeStatus?>> GetMyRsvpStatus(Guid eventId)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var status = await _scheduleEventService.GetRsvpStatusAsync(eventId, profileId.Value);
        return Ok(status);
    }

    /// <summary>
    /// Check in an attendee (for event owners)
    /// </summary>
    [HttpPost("{eventId:guid}/attendees/{attendeeProfileId:guid}/check-in")]
    public async Task<ActionResult> CheckInAttendee(Guid eventId, Guid attendeeProfileId)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var success = await _scheduleEventService.CheckInAttendeeAsync(eventId, attendeeProfileId, profileId.Value);
        if (!success)
        {
            return NotFound();
        }

        return Ok();
    }

    #endregion

    #region Reminders

    /// <summary>
    /// Get my reminders for an event
    /// </summary>
    [HttpGet("{eventId:guid}/reminders")]
    public async Task<ActionResult<List<EventReminderDto>>> GetReminders(Guid eventId)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var result = await _scheduleEventService.GetRemindersAsync(eventId, profileId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Add a reminder
    /// </summary>
    [HttpPost("{eventId:guid}/reminders")]
    public async Task<ActionResult<EventReminderDto>> AddReminder(
        Guid eventId,
        [FromBody] AddReminderRequest request)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var result = await _scheduleEventService.AddReminderAsync(
            eventId, profileId.Value, request.MinutesBefore, request.ReminderType);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a reminder
    /// </summary>
    [HttpDelete("reminders/{reminderId:guid}")]
    public async Task<ActionResult> DeleteReminder(Guid reminderId)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        var success = await _scheduleEventService.RemoveReminderAsync(reminderId, profileId.Value);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    #endregion

    #region Analytics

    /// <summary>
    /// Get event statistics (for event owners)
    /// </summary>
    [HttpGet("{eventId:guid}/stats")]
    public async Task<ActionResult<EventStatsDto>> GetEventStats(Guid eventId)
    {
        var profileId = await GetCurrentProfileIdAsync();
        if (profileId == null)
        {
            return Unauthorized();
        }

        // TODO: Verify ownership

        var result = await _scheduleEventService.GetEventStatsAsync(eventId);
        return Ok(result);
    }

    #endregion

    #region Helpers

    private async Task<Guid?> GetCurrentProfileIdAsync()
    {
        try
        {
            var keycloakId = User.Claims
                .FirstOrDefault(c => c.Type == "sub" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                ?.Value;

            if (string.IsNullOrEmpty(keycloakId))
            {
                return null;
            }

            var activeProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);
            return activeProfile?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScheduleEventsController] Error getting current profile ID");
            return null;
        }
    }

    #endregion
}

/// <summary>
/// Request for cancelling an event
/// </summary>
public class CancelEventRequest
{
    public string? Reason { get; set; }
}

/// <summary>
/// Request for adding a reminder
/// </summary>
public class AddReminderRequest
{
    public int MinutesBefore { get; set; } = 30;
    public ReminderType ReminderType { get; set; } = ReminderType.Push;
}
