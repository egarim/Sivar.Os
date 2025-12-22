using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for Resource Booking System
/// </summary>
public interface IResourceBookingRepository
{
    #region Bookable Resources

    /// <summary>
    /// Gets a resource by ID with all related data
    /// </summary>
    Task<BookableResource?> GetResourceByIdAsync(Guid resourceId, bool includeServices = true, bool includeAvailability = true);

    /// <summary>
    /// Gets all resources for a business profile
    /// </summary>
    Task<List<BookableResource>> GetResourcesByProfileIdAsync(Guid profileId, bool activeOnly = true);

    /// <summary>
    /// Gets all resources assigned to a staff member's profile.
    /// Used for staff members to see their own schedule.
    /// </summary>
    Task<List<BookableResource>> GetResourcesByAssignedProfileIdAsync(Guid assignedProfileId);

    /// <summary>
    /// Queries resources with filters
    /// </summary>
    Task<(List<BookableResource> Resources, int TotalCount)> QueryResourcesAsync(ResourceQueryDto query);

    /// <summary>
    /// Creates a new bookable resource
    /// </summary>
    Task<BookableResource> CreateResourceAsync(BookableResource resource);

    /// <summary>
    /// Updates a resource
    /// </summary>
    Task<BookableResource> UpdateResourceAsync(BookableResource resource);

    /// <summary>
    /// Deletes a resource (soft delete via IsActive = false, or hard delete)
    /// </summary>
    Task<bool> DeleteResourceAsync(Guid resourceId, bool hardDelete = false);

    #endregion

    #region Resource Services

    /// <summary>
    /// Gets a service by ID
    /// </summary>
    Task<ResourceService?> GetServiceByIdAsync(Guid serviceId);

    /// <summary>
    /// Gets all services for a resource
    /// </summary>
    Task<List<ResourceService>> GetServicesByResourceIdAsync(Guid resourceId, bool activeOnly = true);

    /// <summary>
    /// Creates a new service
    /// </summary>
    Task<ResourceService> CreateServiceAsync(ResourceService service);

    /// <summary>
    /// Updates a service
    /// </summary>
    Task<ResourceService> UpdateServiceAsync(ResourceService service);

    /// <summary>
    /// Deletes a service
    /// </summary>
    Task<bool> DeleteServiceAsync(Guid serviceId);

    #endregion

    #region Resource Availability

    /// <summary>
    /// Gets availability for a resource
    /// </summary>
    Task<List<ResourceAvailability>> GetAvailabilityByResourceIdAsync(Guid resourceId);

    /// <summary>
    /// Gets availability for a specific day
    /// </summary>
    Task<List<ResourceAvailability>> GetAvailabilityForDayAsync(Guid resourceId, System.DayOfWeek dayOfWeek);

    /// <summary>
    /// Sets the weekly availability schedule (replaces existing)
    /// </summary>
    Task<List<ResourceAvailability>> SetWeeklyAvailabilityAsync(Guid resourceId, List<ResourceAvailability> availability);

    /// <summary>
    /// Creates a single availability entry
    /// </summary>
    Task<ResourceAvailability> CreateAvailabilityAsync(ResourceAvailability availability);

    /// <summary>
    /// Updates an availability entry
    /// </summary>
    Task<ResourceAvailability> UpdateAvailabilityAsync(ResourceAvailability availability);

    /// <summary>
    /// Deletes an availability entry
    /// </summary>
    Task<bool> DeleteAvailabilityAsync(Guid availabilityId);

    #endregion

    #region Resource Exceptions

    /// <summary>
    /// Gets exceptions for a resource within a date range
    /// </summary>
    Task<List<ResourceException>> GetExceptionsByResourceIdAsync(Guid resourceId, DateOnly? fromDate = null, DateOnly? toDate = null);

    /// <summary>
    /// Gets exception for a specific date
    /// </summary>
    Task<ResourceException?> GetExceptionForDateAsync(Guid resourceId, DateOnly date);

    /// <summary>
    /// Creates an exception
    /// </summary>
    Task<ResourceException> CreateExceptionAsync(ResourceException exception);

    /// <summary>
    /// Updates an exception
    /// </summary>
    Task<ResourceException> UpdateExceptionAsync(ResourceException exception);

    /// <summary>
    /// Deletes an exception
    /// </summary>
    Task<bool> DeleteExceptionAsync(Guid exceptionId);

    #endregion

    #region Resource Bookings

    /// <summary>
    /// Gets a booking by ID with related data
    /// </summary>
    Task<ResourceBooking?> GetBookingByIdAsync(Guid bookingId);

    /// <summary>
    /// Gets a booking by confirmation code
    /// </summary>
    Task<ResourceBooking?> GetBookingByConfirmationCodeAsync(string confirmationCode);

    /// <summary>
    /// Queries bookings with filters
    /// </summary>
    Task<(List<ResourceBooking> Bookings, int TotalCount)> QueryBookingsAsync(BookingQueryDto query);

    /// <summary>
    /// Gets upcoming bookings for a customer
    /// </summary>
    Task<List<ResourceBooking>> GetUpcomingBookingsForCustomerAsync(Guid customerProfileId, int limit = 10);

    /// <summary>
    /// Gets upcoming bookings for a resource
    /// </summary>
    Task<List<ResourceBooking>> GetUpcomingBookingsForResourceAsync(Guid resourceId, int limit = 50);

    /// <summary>
    /// Gets bookings for a resource within a time range
    /// </summary>
    Task<List<ResourceBooking>> GetBookingsInRangeAsync(Guid resourceId, DateTime start, DateTime end);

    /// <summary>
    /// Gets today's bookings for a business profile
    /// </summary>
    Task<List<ResourceBooking>> GetTodayBookingsForBusinessAsync(Guid businessProfileId);

    /// <summary>
    /// Gets bookings for all resources assigned to a staff member's profile.
    /// Used for staff members to view their personal schedule.
    /// </summary>
    Task<List<ResourceBooking>> GetBookingsForStaffAsync(Guid assignedProfileId, DateTime start, DateTime end);

    /// <summary>
    /// Checks if a time slot is available
    /// </summary>
    Task<bool> IsTimeSlotAvailableAsync(Guid resourceId, DateTime start, DateTime end, Guid? excludeBookingId = null);

    /// <summary>
    /// Creates a new booking
    /// </summary>
    Task<ResourceBooking> CreateBookingAsync(ResourceBooking booking);

    /// <summary>
    /// Updates a booking
    /// </summary>
    Task<ResourceBooking> UpdateBookingAsync(ResourceBooking booking);

    /// <summary>
    /// Gets booking statistics for a resource
    /// </summary>
    Task<ResourceStatsDto> GetResourceStatsAsync(Guid resourceId, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Gets booking statistics for a business profile
    /// </summary>
    Task<BusinessBookingStatsDto> GetBusinessStatsAsync(Guid profileId);

    /// <summary>
    /// Gets bookings that need reminders sent
    /// </summary>
    Task<List<ResourceBooking>> GetBookingsNeedingRemindersAsync(int hoursBeforeBooking);

    #endregion
}
