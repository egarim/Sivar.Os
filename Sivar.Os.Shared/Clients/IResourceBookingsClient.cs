using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client interface for Resource Booking System operations
/// Used by Blazor components to manage bookable resources and appointments
/// </summary>
public interface IResourceBookingsClient
{
    #region Resource Management

    /// <summary>
    /// Gets a resource by ID
    /// </summary>
    Task<BookableResourceDto?> GetResourceAsync(Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all resources for a business profile
    /// </summary>
    Task<List<BookableResourceSummaryDto>> GetResourcesByProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries resources with filters
    /// </summary>
    Task<ResourceListResponseDto> QueryResourcesAsync(ResourceQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new bookable resource
    /// </summary>
    Task<BookableResourceDto?> CreateResourceAsync(CreateBookableResourceDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a resource
    /// </summary>
    Task<BookableResourceDto?> UpdateResourceAsync(Guid resourceId, UpdateBookableResourceDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a resource
    /// </summary>
    Task<bool> DeleteResourceAsync(Guid resourceId, CancellationToken cancellationToken = default);

    #endregion

    #region Service Management

    /// <summary>
    /// Gets services for a resource
    /// </summary>
    Task<List<ResourceServiceDto>> GetServicesAsync(Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a service to a resource
    /// </summary>
    Task<ResourceServiceDto?> AddServiceAsync(Guid resourceId, CreateResourceServiceDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a service
    /// </summary>
    Task<ResourceServiceDto?> UpdateServiceAsync(Guid serviceId, UpdateResourceServiceDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a service
    /// </summary>
    Task<bool> DeleteServiceAsync(Guid serviceId, CancellationToken cancellationToken = default);

    #endregion

    #region Availability Management

    /// <summary>
    /// Gets the weekly availability for a resource
    /// </summary>
    Task<List<ResourceAvailabilityDto>> GetAvailabilityAsync(Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the weekly availability for a resource (replaces existing)
    /// </summary>
    Task<List<ResourceAvailabilityDto>> SetWeeklyAvailabilityAsync(Guid resourceId, SetWeeklyAvailabilityDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets exceptions for a resource
    /// </summary>
    Task<List<ResourceExceptionDto>> GetExceptionsAsync(Guid resourceId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an exception (holiday, blocked date, special hours)
    /// </summary>
    Task<ResourceExceptionDto?> AddExceptionAsync(Guid resourceId, CreateResourceExceptionDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an exception
    /// </summary>
    Task<bool> DeleteExceptionAsync(Guid exceptionId, CancellationToken cancellationToken = default);

    #endregion

    #region Time Slots

    /// <summary>
    /// Gets available time slots for a resource on a specific date
    /// </summary>
    Task<AvailableSlotsResponseDto> GetAvailableSlotsAsync(Guid resourceId, DateOnly date, Guid? serviceId = null, CancellationToken cancellationToken = default);

    #endregion

    #region Booking Operations

    /// <summary>
    /// Creates a new booking
    /// </summary>
    Task<ResourceBookingDto?> CreateBookingAsync(CreateResourceBookingDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a booking by ID
    /// </summary>
    Task<ResourceBookingDto?> GetBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a booking by confirmation code
    /// </summary>
    Task<ResourceBookingDto?> GetBookingByConfirmationCodeAsync(string confirmationCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets upcoming bookings for the current user (as customer)
    /// </summary>
    Task<List<ResourceBookingSummaryDto>> GetMyUpcomingBookingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets booking history for the current user (as customer)
    /// </summary>
    Task<BookingListResponseDto> GetMyBookingHistoryAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bookings for a business (as owner)
    /// </summary>
    Task<BookingListResponseDto> GetBusinessBookingsAsync(BookingQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets today's bookings for a business
    /// </summary>
    Task<List<ResourceBookingDto>> GetTodayBookingsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Staff Schedule

    /// <summary>
    /// Gets resources assigned to the current user (for staff members).
    /// Returns empty list if user is not assigned to any resources.
    /// </summary>
    Task<List<BookableResourceSummaryDto>> GetMyAssignedResourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets staff schedule for a specific date.
    /// Returns bookings for all resources assigned to the current user.
    /// </summary>
    Task<List<ResourceBookingDto>> GetStaffScheduleAsync(DateTime? date = null, CancellationToken cancellationToken = default);

    #endregion

    #region Booking Actions

    /// <summary>
    /// Confirms a pending booking (business action)
    /// </summary>
    Task<ResourceBookingDto?> ConfirmBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a booking (can be customer or business)
    /// </summary>
    Task<ResourceBookingDto?> CancelBookingAsync(Guid bookingId, CancelBookingDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reschedules a booking to a new time
    /// </summary>
    Task<ResourceBookingDto?> RescheduleBookingAsync(Guid bookingId, RescheduleBookingDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks in a customer (business action)
    /// </summary>
    Task<ResourceBookingDto?> CheckInAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a booking as completed (business action)
    /// </summary>
    Task<ResourceBookingDto?> CompleteBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a booking as no-show (business action)
    /// </summary>
    Task<ResourceBookingDto?> MarkNoShowAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates internal notes (business action)
    /// </summary>
    Task<ResourceBookingDto?> UpdateInternalNotesAsync(Guid bookingId, UpdateBookingNotesDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a review for a completed booking (customer action)
    /// </summary>
    Task<ResourceBookingDto?> SubmitReviewAsync(Guid bookingId, SubmitBookingReviewDto request, CancellationToken cancellationToken = default);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets statistics for a resource
    /// </summary>
    Task<ResourceStatsDto?> GetResourceStatsAsync(Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets booking statistics for a business
    /// </summary>
    Task<BusinessBookingStatsDto?> GetBusinessStatsAsync(CancellationToken cancellationToken = default);

    #endregion
}
