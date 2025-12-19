using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Services;
using System.Security.Claims;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for Resource Booking System
/// Handles bookable resources, services, availability, and bookings
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ResourceBookingsController : ControllerBase
{
    private readonly IResourceBookingService _bookingService;
    private readonly ILogger<ResourceBookingsController> _logger;

    public ResourceBookingsController(
        IResourceBookingService bookingService,
        ILogger<ResourceBookingsController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    private string? GetKeycloakId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value;

    #region Resources

    /// <summary>
    /// Get a resource by ID
    /// </summary>
    [HttpGet("resources/{resourceId:guid}")]
    public async Task<ActionResult<BookableResourceDto>> GetResource(Guid resourceId)
    {
        var resource = await _bookingService.GetResourceAsync(resourceId);
        if (resource == null)
            return NotFound();
        return Ok(resource);
    }

    /// <summary>
    /// Get all resources for a profile
    /// </summary>
    [HttpGet("resources/profile/{profileId:guid}")]
    public async Task<ActionResult<List<BookableResourceSummaryDto>>> GetResourcesByProfile(Guid profileId)
    {
        var resources = await _bookingService.GetResourcesByProfileAsync(profileId);
        return Ok(resources);
    }

    /// <summary>
    /// Query resources with filters
    /// </summary>
    [HttpGet("resources")]
    public async Task<ActionResult<ResourceListResponseDto>> QueryResources([FromQuery] ResourceQueryDto query)
    {
        var result = await _bookingService.QueryResourcesAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new bookable resource
    /// </summary>
    [Authorize]
    [HttpPost("resources")]
    public async Task<ActionResult<BookableResourceDto>> CreateResource([FromBody] CreateBookableResourceDto dto)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var resource = await _bookingService.CreateResourceAsync(keycloakId, dto);
        if (resource == null)
            return BadRequest("Unable to create resource. Ensure you have a Business or Organization profile.");

        return CreatedAtAction(nameof(GetResource), new { resourceId = resource.Id }, resource);
    }

    /// <summary>
    /// Update a resource
    /// </summary>
    [Authorize]
    [HttpPut("resources/{resourceId:guid}")]
    public async Task<ActionResult<BookableResourceDto>> UpdateResource(Guid resourceId, [FromBody] UpdateBookableResourceDto dto)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var resource = await _bookingService.UpdateResourceAsync(keycloakId, resourceId, dto);
        if (resource == null)
            return NotFound();

        return Ok(resource);
    }

    /// <summary>
    /// Delete a resource
    /// </summary>
    [Authorize]
    [HttpDelete("resources/{resourceId:guid}")]
    public async Task<IActionResult> DeleteResource(Guid resourceId)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var result = await _bookingService.DeleteResourceAsync(keycloakId, resourceId);
        if (!result)
            return NotFound();

        return NoContent();
    }

    #endregion

    #region Services

    /// <summary>
    /// Get services for a resource
    /// </summary>
    [HttpGet("resources/{resourceId:guid}/services")]
    public async Task<ActionResult<List<ResourceServiceDto>>> GetServices(Guid resourceId)
    {
        var services = await _bookingService.GetServicesAsync(resourceId);
        return Ok(services);
    }

    /// <summary>
    /// Add a service to a resource
    /// </summary>
    [Authorize]
    [HttpPost("resources/{resourceId:guid}/services")]
    public async Task<ActionResult<ResourceServiceDto>> AddService(Guid resourceId, [FromBody] CreateResourceServiceDto dto)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var service = await _bookingService.AddServiceAsync(keycloakId, resourceId, dto);
        if (service == null)
            return BadRequest("Unable to add service.");

        return Ok(service);
    }

    /// <summary>
    /// Update a service
    /// </summary>
    [Authorize]
    [HttpPut("services/{serviceId:guid}")]
    public async Task<ActionResult<ResourceServiceDto>> UpdateService(Guid serviceId, [FromBody] UpdateResourceServiceDto dto)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var service = await _bookingService.UpdateServiceAsync(keycloakId, serviceId, dto);
        if (service == null)
            return NotFound();

        return Ok(service);
    }

    /// <summary>
    /// Delete a service
    /// </summary>
    [Authorize]
    [HttpDelete("services/{serviceId:guid}")]
    public async Task<IActionResult> DeleteService(Guid serviceId)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var result = await _bookingService.DeleteServiceAsync(keycloakId, serviceId);
        if (!result)
            return NotFound();

        return NoContent();
    }

    #endregion

    #region Availability

    /// <summary>
    /// Get weekly availability for a resource
    /// </summary>
    [HttpGet("resources/{resourceId:guid}/availability")]
    public async Task<ActionResult<List<ResourceAvailabilityDto>>> GetAvailability(Guid resourceId)
    {
        var availability = await _bookingService.GetAvailabilityAsync(resourceId);
        return Ok(availability);
    }

    /// <summary>
    /// Set weekly availability for a resource
    /// </summary>
    [Authorize]
    [HttpPut("resources/{resourceId:guid}/availability")]
    public async Task<ActionResult<List<ResourceAvailabilityDto>>> SetWeeklyAvailability(
        Guid resourceId, 
        [FromBody] SetWeeklyAvailabilityDto dto)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var availability = await _bookingService.SetWeeklyAvailabilityAsync(keycloakId, resourceId, dto);
        return Ok(availability);
    }

    /// <summary>
    /// Get exceptions for a resource
    /// </summary>
    [HttpGet("resources/{resourceId:guid}/exceptions")]
    public async Task<ActionResult<List<ResourceExceptionDto>>> GetExceptions(
        Guid resourceId, 
        [FromQuery] DateOnly? fromDate = null, 
        [FromQuery] DateOnly? toDate = null)
    {
        var exceptions = await _bookingService.GetExceptionsAsync(resourceId, fromDate, toDate);
        return Ok(exceptions);
    }

    /// <summary>
    /// Add an exception (holiday, blocked date, special hours)
    /// </summary>
    [Authorize]
    [HttpPost("resources/{resourceId:guid}/exceptions")]
    public async Task<ActionResult<ResourceExceptionDto>> AddException(
        Guid resourceId, 
        [FromBody] CreateResourceExceptionDto dto)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var exception = await _bookingService.AddExceptionAsync(keycloakId, resourceId, dto);
        if (exception == null)
            return BadRequest("Unable to add exception.");

        return Ok(exception);
    }

    /// <summary>
    /// Delete an exception
    /// </summary>
    [Authorize]
    [HttpDelete("exceptions/{exceptionId:guid}")]
    public async Task<IActionResult> DeleteException(Guid exceptionId)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var result = await _bookingService.DeleteExceptionAsync(keycloakId, exceptionId);
        if (!result)
            return NotFound();

        return NoContent();
    }

    #endregion

    #region Available Slots

    /// <summary>
    /// Get available time slots for booking
    /// </summary>
    [HttpGet("resources/{resourceId:guid}/slots")]
    public async Task<ActionResult<AvailableSlotsResponseDto>> GetAvailableSlots(
        Guid resourceId,
        [FromQuery] DateOnly date,
        [FromQuery] Guid? serviceId = null,
        [FromQuery] int? daysAhead = null,
        [FromQuery] string timeZone = "UTC")
    {
        var query = new GetAvailableSlotsDto
        {
            ResourceId = resourceId,
            ServiceId = serviceId,
            Date = date,
            DaysAhead = daysAhead,
            TimeZone = timeZone
        };

        var slots = await _bookingService.GetAvailableSlotsAsync(query);
        return Ok(slots);
    }

    #endregion

    #region Bookings

    /// <summary>
    /// Create a new booking
    /// </summary>
    [Authorize]
    [HttpPost("bookings")]
    public async Task<ActionResult<ResourceBookingDto>> CreateBooking([FromBody] CreateResourceBookingDto dto)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var booking = await _bookingService.CreateBookingAsync(keycloakId, dto);
        if (booking == null)
            return BadRequest("Unable to create booking. The time slot may not be available.");

        return CreatedAtAction(nameof(GetBooking), new { bookingId = booking.Id }, booking);
    }

    /// <summary>
    /// Get a booking by ID
    /// </summary>
    [HttpGet("bookings/{bookingId:guid}")]
    public async Task<ActionResult<ResourceBookingDto>> GetBooking(Guid bookingId)
    {
        var booking = await _bookingService.GetBookingAsync(bookingId);
        if (booking == null)
            return NotFound();
        return Ok(booking);
    }

    /// <summary>
    /// Get a booking by confirmation code
    /// </summary>
    [HttpGet("bookings/confirmation/{confirmationCode}")]
    public async Task<ActionResult<ResourceBookingDto>> GetBookingByConfirmationCode(string confirmationCode)
    {
        var booking = await _bookingService.GetBookingByConfirmationCodeAsync(confirmationCode);
        if (booking == null)
            return NotFound();
        return Ok(booking);
    }

    /// <summary>
    /// Get my upcoming bookings (as customer)
    /// </summary>
    [Authorize]
    [HttpGet("bookings/my/upcoming")]
    public async Task<ActionResult<List<ResourceBookingSummaryDto>>> GetMyUpcomingBookings()
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var bookings = await _bookingService.GetMyUpcomingBookingsAsync(keycloakId);
        return Ok(bookings);
    }

    /// <summary>
    /// Get my booking history (as customer)
    /// </summary>
    [Authorize]
    [HttpGet("bookings/my/history")]
    public async Task<ActionResult<BookingListResponseDto>> GetMyBookingHistory(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var result = await _bookingService.GetMyBookingHistoryAsync(keycloakId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get bookings for my business
    /// </summary>
    [Authorize]
    [HttpGet("bookings/business")]
    public async Task<ActionResult<BookingListResponseDto>> GetBusinessBookings([FromQuery] BookingQueryDto query)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var result = await _bookingService.GetBusinessBookingsAsync(keycloakId, query);
        return Ok(result);
    }

    /// <summary>
    /// Get today's bookings for my business
    /// </summary>
    [Authorize]
    [HttpGet("bookings/business/today")]
    public async Task<ActionResult<List<ResourceBookingDto>>> GetTodayBookings()
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var bookings = await _bookingService.GetTodayBookingsAsync(keycloakId);
        return Ok(bookings);
    }

    /// <summary>
    /// Confirm a pending booking (business action)
    /// </summary>
    [Authorize]
    [HttpPost("bookings/{bookingId:guid}/confirm")]
    public async Task<ActionResult<ResourceBookingDto>> ConfirmBooking(Guid bookingId)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var booking = await _bookingService.ConfirmBookingAsync(keycloakId, bookingId);
        if (booking == null)
            return BadRequest("Unable to confirm booking.");

        return Ok(booking);
    }

    /// <summary>
    /// Cancel a booking
    /// </summary>
    [Authorize]
    [HttpPost("bookings/{bookingId:guid}/cancel")]
    public async Task<ActionResult<ResourceBookingDto>> CancelBooking(
        Guid bookingId, 
        [FromBody] CancelBookingDto dto)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var booking = await _bookingService.CancelBookingAsync(keycloakId, bookingId, dto);
        if (booking == null)
            return BadRequest("Unable to cancel booking.");

        return Ok(booking);
    }

    /// <summary>
    /// Reschedule a booking
    /// </summary>
    [Authorize]
    [HttpPost("bookings/{bookingId:guid}/reschedule")]
    public async Task<ActionResult<ResourceBookingDto>> RescheduleBooking(
        Guid bookingId, 
        [FromBody] RescheduleBookingDto dto)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var booking = await _bookingService.RescheduleBookingAsync(keycloakId, bookingId, dto);
        if (booking == null)
            return BadRequest("Unable to reschedule booking. The new time slot may not be available.");

        return Ok(booking);
    }

    /// <summary>
    /// Check in a customer (business action)
    /// </summary>
    [Authorize]
    [HttpPost("bookings/{bookingId:guid}/check-in")]
    public async Task<ActionResult<ResourceBookingDto>> CheckIn(Guid bookingId)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var booking = await _bookingService.CheckInAsync(keycloakId, bookingId);
        if (booking == null)
            return BadRequest("Unable to check in.");

        return Ok(booking);
    }

    /// <summary>
    /// Mark booking as completed (business action)
    /// </summary>
    [Authorize]
    [HttpPost("bookings/{bookingId:guid}/complete")]
    public async Task<ActionResult<ResourceBookingDto>> CompleteBooking(Guid bookingId)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var booking = await _bookingService.CompleteBookingAsync(keycloakId, bookingId);
        if (booking == null)
            return BadRequest("Unable to complete booking.");

        return Ok(booking);
    }

    /// <summary>
    /// Mark as no-show (business action)
    /// </summary>
    [Authorize]
    [HttpPost("bookings/{bookingId:guid}/no-show")]
    public async Task<ActionResult<ResourceBookingDto>> MarkNoShow(Guid bookingId)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var booking = await _bookingService.MarkNoShowAsync(keycloakId, bookingId);
        if (booking == null)
            return BadRequest("Unable to mark as no-show.");

        return Ok(booking);
    }

    /// <summary>
    /// Update internal notes (business action)
    /// </summary>
    [Authorize]
    [HttpPut("bookings/{bookingId:guid}/notes")]
    public async Task<ActionResult<ResourceBookingDto>> UpdateNotes(
        Guid bookingId, 
        [FromBody] UpdateBookingNotesDto dto)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var booking = await _bookingService.UpdateInternalNotesAsync(keycloakId, bookingId, dto);
        if (booking == null)
            return NotFound();

        return Ok(booking);
    }

    /// <summary>
    /// Submit a review for a completed booking (customer action)
    /// </summary>
    [Authorize]
    [HttpPost("bookings/{bookingId:guid}/review")]
    public async Task<ActionResult<ResourceBookingDto>> SubmitReview(
        Guid bookingId, 
        [FromBody] SubmitBookingReviewDto dto)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var booking = await _bookingService.SubmitReviewAsync(keycloakId, bookingId, dto);
        if (booking == null)
            return BadRequest("Unable to submit review. Ensure the booking is completed and not already reviewed.");

        return Ok(booking);
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get statistics for a resource
    /// </summary>
    [Authorize]
    [HttpGet("resources/{resourceId:guid}/stats")]
    public async Task<ActionResult<ResourceStatsDto>> GetResourceStats(Guid resourceId)
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var stats = await _bookingService.GetResourceStatsAsync(keycloakId, resourceId);
        if (stats == null)
            return NotFound();

        return Ok(stats);
    }

    /// <summary>
    /// Get booking statistics for my business
    /// </summary>
    [Authorize]
    [HttpGet("stats/business")]
    public async Task<ActionResult<BusinessBookingStatsDto>> GetBusinessStats()
    {
        var keycloakId = GetKeycloakId();
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var stats = await _bookingService.GetBusinessStatsAsync(keycloakId);
        if (stats == null)
            return NotFound();

        return Ok(stats);
    }

    #endregion
}
