using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for Resource Booking System
/// Handles business logic for bookable resources and appointments
/// </summary>
public interface IResourceBookingService
{
    #region Resource Management

    /// <summary>
    /// Gets a resource by ID
    /// </summary>
    Task<BookableResourceDto?> GetResourceAsync(Guid resourceId);

    /// <summary>
    /// Gets all resources for a business profile
    /// </summary>
    Task<List<BookableResourceSummaryDto>> GetResourcesByProfileAsync(Guid profileId);

    /// <summary>
    /// Queries resources with filters
    /// </summary>
    Task<ResourceListResponseDto> QueryResourcesAsync(ResourceQueryDto query);

    /// <summary>
    /// Creates a new bookable resource
    /// Only business/organization profiles can create resources
    /// </summary>
    Task<BookableResourceDto?> CreateResourceAsync(string keycloakId, CreateBookableResourceDto dto);

    /// <summary>
    /// Updates a resource
    /// </summary>
    Task<BookableResourceDto?> UpdateResourceAsync(string keycloakId, Guid resourceId, UpdateBookableResourceDto dto);

    /// <summary>
    /// Deletes a resource
    /// </summary>
    Task<bool> DeleteResourceAsync(string keycloakId, Guid resourceId);

    #endregion

    #region Service Management

    /// <summary>
    /// Gets services for a resource
    /// </summary>
    Task<List<ResourceServiceDto>> GetServicesAsync(Guid resourceId);

    /// <summary>
    /// Adds a service to a resource
    /// </summary>
    Task<ResourceServiceDto?> AddServiceAsync(string keycloakId, Guid resourceId, CreateResourceServiceDto dto);

    /// <summary>
    /// Updates a service
    /// </summary>
    Task<ResourceServiceDto?> UpdateServiceAsync(string keycloakId, Guid serviceId, UpdateResourceServiceDto dto);

    /// <summary>
    /// Deletes a service
    /// </summary>
    Task<bool> DeleteServiceAsync(string keycloakId, Guid serviceId);

    #endregion

    #region Availability Management

    /// <summary>
    /// Gets the weekly availability for a resource
    /// </summary>
    Task<List<ResourceAvailabilityDto>> GetAvailabilityAsync(Guid resourceId);

    /// <summary>
    /// Sets the weekly availability for a resource (replaces existing)
    /// </summary>
    Task<List<ResourceAvailabilityDto>> SetWeeklyAvailabilityAsync(string keycloakId, Guid resourceId, SetWeeklyAvailabilityDto dto);

    /// <summary>
    /// Gets exceptions for a resource
    /// </summary>
    Task<List<ResourceExceptionDto>> GetExceptionsAsync(Guid resourceId, DateOnly? fromDate = null, DateOnly? toDate = null);

    /// <summary>
    /// Adds an exception (holiday, blocked date, special hours)
    /// </summary>
    Task<ResourceExceptionDto?> AddExceptionAsync(string keycloakId, Guid resourceId, CreateResourceExceptionDto dto);

    /// <summary>
    /// Deletes an exception
    /// </summary>
    Task<bool> DeleteExceptionAsync(string keycloakId, Guid exceptionId);

    #endregion

    #region Booking Operations

    /// <summary>
    /// Gets available time slots for a resource/service on a specific date
    /// </summary>
    Task<AvailableSlotsResponseDto> GetAvailableSlotsAsync(GetAvailableSlotsDto query);

    /// <summary>
    /// Creates a new booking
    /// </summary>
    Task<ResourceBookingDto?> CreateBookingAsync(string keycloakId, CreateResourceBookingDto dto);

    /// <summary>
    /// Gets a booking by ID
    /// </summary>
    Task<ResourceBookingDto?> GetBookingAsync(Guid bookingId);

    /// <summary>
    /// Gets a booking by confirmation code
    /// </summary>
    Task<ResourceBookingDto?> GetBookingByConfirmationCodeAsync(string confirmationCode);

    /// <summary>
    /// Gets upcoming bookings for the current user (as customer)
    /// </summary>
    Task<List<ResourceBookingSummaryDto>> GetMyUpcomingBookingsAsync(string keycloakId);

    /// <summary>
    /// Gets booking history for the current user (as customer)
    /// </summary>
    Task<BookingListResponseDto> GetMyBookingHistoryAsync(string keycloakId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets bookings for a business (as owner)
    /// </summary>
    Task<BookingListResponseDto> GetBusinessBookingsAsync(string keycloakId, BookingQueryDto query);

    /// <summary>
    /// Gets today's bookings for a business
    /// </summary>
    Task<List<ResourceBookingDto>> GetTodayBookingsAsync(string keycloakId);

    /// <summary>
    /// Gets resources assigned to the current user (for staff view).
    /// Staff members can see which resources they are assigned to.
    /// </summary>
    Task<List<BookableResourceSummaryDto>> GetMyAssignedResourcesAsync(string keycloakId);

    /// <summary>
    /// Gets the staff schedule - bookings for all resources assigned to this user.
    /// Used for staff members to view their personal schedule.
    /// </summary>
    Task<List<ResourceBookingDto>> GetStaffScheduleAsync(string keycloakId, DateTime date);

    /// <summary>
    /// Confirms a pending booking (business action)
    /// </summary>
    Task<ResourceBookingDto?> ConfirmBookingAsync(string keycloakId, Guid bookingId);

    /// <summary>
    /// Cancels a booking (can be customer or business)
    /// </summary>
    Task<ResourceBookingDto?> CancelBookingAsync(string keycloakId, Guid bookingId, CancelBookingDto dto);

    /// <summary>
    /// Reschedules a booking to a new time
    /// </summary>
    Task<ResourceBookingDto?> RescheduleBookingAsync(string keycloakId, Guid bookingId, RescheduleBookingDto dto);

    /// <summary>
    /// Checks in a customer (business action)
    /// </summary>
    Task<ResourceBookingDto?> CheckInAsync(string keycloakId, Guid bookingId);

    /// <summary>
    /// Marks a booking as completed (business action)
    /// </summary>
    Task<ResourceBookingDto?> CompleteBookingAsync(string keycloakId, Guid bookingId);

    /// <summary>
    /// Marks a booking as no-show (business action)
    /// </summary>
    Task<ResourceBookingDto?> MarkNoShowAsync(string keycloakId, Guid bookingId);

    /// <summary>
    /// Updates internal notes (business action)
    /// </summary>
    Task<ResourceBookingDto?> UpdateInternalNotesAsync(string keycloakId, Guid bookingId, UpdateBookingNotesDto dto);

    /// <summary>
    /// Submits a review for a completed booking (customer action)
    /// </summary>
    Task<ResourceBookingDto?> SubmitReviewAsync(string keycloakId, Guid bookingId, SubmitBookingReviewDto dto);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets statistics for a resource
    /// </summary>
    Task<ResourceStatsDto?> GetResourceStatsAsync(string keycloakId, Guid resourceId);

    /// <summary>
    /// Gets booking statistics for a business
    /// </summary>
    Task<BusinessBookingStatsDto?> GetBusinessStatsAsync(string keycloakId);

    #endregion
}
