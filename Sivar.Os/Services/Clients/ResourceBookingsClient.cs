using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of Resource Bookings client.
/// Delegates all operations to IResourceBookingService for unified business logic.
/// </summary>
public class ResourceBookingsClient : BaseRepositoryClient, IResourceBookingsClient
{
    private readonly IResourceBookingService _resourceBookingService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ResourceBookingsClient> _logger;
    private static readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);

    public ResourceBookingsClient(
        IResourceBookingService resourceBookingService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ResourceBookingsClient> logger)
    {
        _resourceBookingService = resourceBookingService ?? throw new ArgumentNullException(nameof(resourceBookingService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Resource Management

    public async Task<BookableResourceDto?> GetResourceAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        if (resourceId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.GetResourceAsync] Called with empty resource ID");
            return null;
        }

        try
        {
            _logger.LogInformation("[ResourceBookingsClient.GetResourceAsync] ResourceId={ResourceId}", resourceId);
            return await _resourceBookingService.GetResourceAsync(resourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetResourceAsync] Error getting resource {ResourceId}", resourceId);
            throw;
        }
    }

    public async Task<List<BookableResourceSummaryDto>> GetResourcesByProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.GetResourcesByProfileAsync] Called with empty profile ID");
            return new List<BookableResourceSummaryDto>();
        }

        try
        {
            _logger.LogInformation("[ResourceBookingsClient.GetResourcesByProfileAsync] ProfileId={ProfileId}", profileId);
            return await _resourceBookingService.GetResourcesByProfileAsync(profileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetResourcesByProfileAsync] Error getting resources for profile {ProfileId}", profileId);
            throw;
        }
    }

    public async Task<ResourceListResponseDto> QueryResourcesAsync(ResourceQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[ResourceBookingsClient.QueryResourcesAsync] Query={@Query}", query);
            return await _resourceBookingService.QueryResourcesAsync(query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.QueryResourcesAsync] Error querying resources");
            throw;
        }
    }

    public async Task<BookableResourceDto?> CreateResourceAsync(CreateBookableResourceDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            _logger.LogWarning("[ResourceBookingsClient.CreateResourceAsync] Called with null request");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.CreateResourceAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.CreateResourceAsync] KeycloakId={KeycloakId}, ResourceName={Name}", 
                keycloakId, request.Name);
            
            return await _resourceBookingService.CreateResourceAsync(keycloakId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.CreateResourceAsync] Error creating resource");
            throw;
        }
    }

    public async Task<BookableResourceDto?> UpdateResourceAsync(Guid resourceId, UpdateBookableResourceDto request, CancellationToken cancellationToken = default)
    {
        if (resourceId == Guid.Empty || request == null)
        {
            _logger.LogWarning("[ResourceBookingsClient.UpdateResourceAsync] Called with invalid parameters");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.UpdateResourceAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.UpdateResourceAsync] KeycloakId={KeycloakId}, ResourceId={ResourceId}", 
                keycloakId, resourceId);
            
            return await _resourceBookingService.UpdateResourceAsync(keycloakId, resourceId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.UpdateResourceAsync] Error updating resource {ResourceId}", resourceId);
            throw;
        }
    }

    public async Task<bool> DeleteResourceAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        if (resourceId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.DeleteResourceAsync] Called with empty resource ID");
            return false;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.DeleteResourceAsync] No authenticated user");
                return false;
            }

            _logger.LogInformation("[ResourceBookingsClient.DeleteResourceAsync] KeycloakId={KeycloakId}, ResourceId={ResourceId}", 
                keycloakId, resourceId);
            
            return await _resourceBookingService.DeleteResourceAsync(keycloakId, resourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.DeleteResourceAsync] Error deleting resource {ResourceId}", resourceId);
            throw;
        }
    }

    #endregion

    #region Service Management

    public async Task<List<ResourceServiceDto>> GetServicesAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        if (resourceId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.GetServicesAsync] Called with empty resource ID");
            return new List<ResourceServiceDto>();
        }

        try
        {
            _logger.LogInformation("[ResourceBookingsClient.GetServicesAsync] ResourceId={ResourceId}", resourceId);
            return await _resourceBookingService.GetServicesAsync(resourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetServicesAsync] Error getting services for resource {ResourceId}", resourceId);
            throw;
        }
    }

    public async Task<ResourceServiceDto?> AddServiceAsync(Guid resourceId, CreateResourceServiceDto request, CancellationToken cancellationToken = default)
    {
        if (resourceId == Guid.Empty || request == null)
        {
            _logger.LogWarning("[ResourceBookingsClient.AddServiceAsync] Called with invalid parameters");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.AddServiceAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.AddServiceAsync] KeycloakId={KeycloakId}, ResourceId={ResourceId}, ServiceName={Name}", 
                keycloakId, resourceId, request.Name);
            
            return await _resourceBookingService.AddServiceAsync(keycloakId, resourceId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.AddServiceAsync] Error adding service to resource {ResourceId}", resourceId);
            throw;
        }
    }

    public async Task<ResourceServiceDto?> UpdateServiceAsync(Guid serviceId, UpdateResourceServiceDto request, CancellationToken cancellationToken = default)
    {
        if (serviceId == Guid.Empty || request == null)
        {
            _logger.LogWarning("[ResourceBookingsClient.UpdateServiceAsync] Called with invalid parameters");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.UpdateServiceAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.UpdateServiceAsync] KeycloakId={KeycloakId}, ServiceId={ServiceId}", 
                keycloakId, serviceId);
            
            return await _resourceBookingService.UpdateServiceAsync(keycloakId, serviceId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.UpdateServiceAsync] Error updating service {ServiceId}", serviceId);
            throw;
        }
    }

    public async Task<bool> DeleteServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        if (serviceId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.DeleteServiceAsync] Called with empty service ID");
            return false;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.DeleteServiceAsync] No authenticated user");
                return false;
            }

            _logger.LogInformation("[ResourceBookingsClient.DeleteServiceAsync] KeycloakId={KeycloakId}, ServiceId={ServiceId}", 
                keycloakId, serviceId);
            
            return await _resourceBookingService.DeleteServiceAsync(keycloakId, serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.DeleteServiceAsync] Error deleting service {ServiceId}", serviceId);
            throw;
        }
    }

    #endregion

    #region Availability Management

    public async Task<List<ResourceAvailabilityDto>> GetAvailabilityAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        if (resourceId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.GetAvailabilityAsync] Called with empty resource ID");
            return new List<ResourceAvailabilityDto>();
        }

        try
        {
            _logger.LogInformation("[ResourceBookingsClient.GetAvailabilityAsync] ResourceId={ResourceId}", resourceId);
            return await _resourceBookingService.GetAvailabilityAsync(resourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetAvailabilityAsync] Error getting availability for resource {ResourceId}", resourceId);
            throw;
        }
    }

    public async Task<List<ResourceAvailabilityDto>> SetWeeklyAvailabilityAsync(Guid resourceId, SetWeeklyAvailabilityDto request, CancellationToken cancellationToken = default)
    {
        if (resourceId == Guid.Empty || request == null)
        {
            _logger.LogWarning("[ResourceBookingsClient.SetWeeklyAvailabilityAsync] Called with invalid parameters");
            return new List<ResourceAvailabilityDto>();
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.SetWeeklyAvailabilityAsync] No authenticated user");
                return new List<ResourceAvailabilityDto>();
            }

            _logger.LogInformation("[ResourceBookingsClient.SetWeeklyAvailabilityAsync] KeycloakId={KeycloakId}, ResourceId={ResourceId}", 
                keycloakId, resourceId);
            
            return await _resourceBookingService.SetWeeklyAvailabilityAsync(keycloakId, resourceId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.SetWeeklyAvailabilityAsync] Error setting availability for resource {ResourceId}", resourceId);
            throw;
        }
    }

    public async Task<List<ResourceExceptionDto>> GetExceptionsAsync(Guid resourceId, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken cancellationToken = default)
    {
        if (resourceId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.GetExceptionsAsync] Called with empty resource ID");
            return new List<ResourceExceptionDto>();
        }

        try
        {
            _logger.LogInformation("[ResourceBookingsClient.GetExceptionsAsync] ResourceId={ResourceId}, FromDate={From}, ToDate={To}", 
                resourceId, fromDate, toDate);
            return await _resourceBookingService.GetExceptionsAsync(resourceId, fromDate, toDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetExceptionsAsync] Error getting exceptions for resource {ResourceId}", resourceId);
            throw;
        }
    }

    public async Task<ResourceExceptionDto?> AddExceptionAsync(Guid resourceId, CreateResourceExceptionDto request, CancellationToken cancellationToken = default)
    {
        if (resourceId == Guid.Empty || request == null)
        {
            _logger.LogWarning("[ResourceBookingsClient.AddExceptionAsync] Called with invalid parameters");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.AddExceptionAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.AddExceptionAsync] KeycloakId={KeycloakId}, ResourceId={ResourceId}, Date={Date}", 
                keycloakId, resourceId, request.Date);
            
            return await _resourceBookingService.AddExceptionAsync(keycloakId, resourceId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.AddExceptionAsync] Error adding exception to resource {ResourceId}", resourceId);
            throw;
        }
    }

    public async Task<bool> DeleteExceptionAsync(Guid exceptionId, CancellationToken cancellationToken = default)
    {
        if (exceptionId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.DeleteExceptionAsync] Called with empty exception ID");
            return false;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.DeleteExceptionAsync] No authenticated user");
                return false;
            }

            _logger.LogInformation("[ResourceBookingsClient.DeleteExceptionAsync] KeycloakId={KeycloakId}, ExceptionId={ExceptionId}", 
                keycloakId, exceptionId);
            
            return await _resourceBookingService.DeleteExceptionAsync(keycloakId, exceptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.DeleteExceptionAsync] Error deleting exception {ExceptionId}", exceptionId);
            throw;
        }
    }

    #endregion

    #region Time Slots

    public async Task<AvailableSlotsResponseDto> GetAvailableSlotsAsync(Guid resourceId, DateOnly date, Guid? serviceId = null, CancellationToken cancellationToken = default)
    {
        if (resourceId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.GetAvailableSlotsAsync] Called with empty resource ID");
            return new AvailableSlotsResponseDto
            {
                ResourceId = resourceId,
                SlotsByDate = new Dictionary<DateOnly, List<AvailableTimeSlotDto>>()
            };
        }

        try
        {
            _logger.LogInformation("[ResourceBookingsClient.GetAvailableSlotsAsync] ResourceId={ResourceId}, Date={Date}, ServiceId={ServiceId}", 
                resourceId, date, serviceId);
            
            var query = new GetAvailableSlotsDto
            {
                ResourceId = resourceId,
                Date = date,
                ServiceId = serviceId
            };
            
            return await _resourceBookingService.GetAvailableSlotsAsync(query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetAvailableSlotsAsync] Error getting slots for resource {ResourceId}", resourceId);
            throw;
        }
    }

    #endregion

    #region Booking Operations

    public async Task<ResourceBookingDto?> CreateBookingAsync(CreateResourceBookingDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            _logger.LogWarning("[ResourceBookingsClient.CreateBookingAsync] Called with null request");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.CreateBookingAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.CreateBookingAsync] KeycloakId={KeycloakId}, ResourceId={ResourceId}, StartTime={StartTime}", 
                keycloakId, request.ResourceId, request.StartTime);
            
            return await _resourceBookingService.CreateBookingAsync(keycloakId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.CreateBookingAsync] Error creating booking");
            throw;
        }
    }

    public async Task<ResourceBookingDto?> GetBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.GetBookingAsync] Called with empty booking ID");
            return null;
        }

        try
        {
            _logger.LogInformation("[ResourceBookingsClient.GetBookingAsync] BookingId={BookingId}", bookingId);
            return await _resourceBookingService.GetBookingAsync(bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetBookingAsync] Error getting booking {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<ResourceBookingDto?> GetBookingByConfirmationCodeAsync(string confirmationCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(confirmationCode))
        {
            _logger.LogWarning("[ResourceBookingsClient.GetBookingByConfirmationCodeAsync] Called with empty confirmation code");
            return null;
        }

        try
        {
            _logger.LogInformation("[ResourceBookingsClient.GetBookingByConfirmationCodeAsync] ConfirmationCode={Code}", confirmationCode);
            return await _resourceBookingService.GetBookingByConfirmationCodeAsync(confirmationCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetBookingByConfirmationCodeAsync] Error getting booking by code {Code}", confirmationCode);
            throw;
        }
    }

    public async Task<List<ResourceBookingSummaryDto>> GetMyUpcomingBookingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.GetMyUpcomingBookingsAsync] No authenticated user");
                return new List<ResourceBookingSummaryDto>();
            }

            _logger.LogInformation("[ResourceBookingsClient.GetMyUpcomingBookingsAsync] KeycloakId={KeycloakId}", keycloakId);
            return await _resourceBookingService.GetMyUpcomingBookingsAsync(keycloakId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetMyUpcomingBookingsAsync] Error getting upcoming bookings");
            throw;
        }
    }

    public async Task<BookingListResponseDto> GetMyBookingHistoryAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.GetMyBookingHistoryAsync] No authenticated user");
                return new BookingListResponseDto
                {
                    Bookings = new List<ResourceBookingSummaryDto>(),
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize
                };
            }

            _logger.LogInformation("[ResourceBookingsClient.GetMyBookingHistoryAsync] KeycloakId={KeycloakId}, Page={Page}, PageSize={PageSize}", 
                keycloakId, page, pageSize);
            return await _resourceBookingService.GetMyBookingHistoryAsync(keycloakId, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetMyBookingHistoryAsync] Error getting booking history");
            throw;
        }
    }

    public async Task<BookingListResponseDto> GetBusinessBookingsAsync(BookingQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.GetBusinessBookingsAsync] No authenticated user");
                return new BookingListResponseDto
                {
                    Bookings = new List<ResourceBookingSummaryDto>(),
                    TotalCount = 0,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            }

            _logger.LogInformation("[ResourceBookingsClient.GetBusinessBookingsAsync] KeycloakId={KeycloakId}, Query={@Query}", 
                keycloakId, query);
            return await _resourceBookingService.GetBusinessBookingsAsync(keycloakId, query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetBusinessBookingsAsync] Error getting business bookings");
            throw;
        }
    }

    public async Task<List<ResourceBookingDto>> GetTodayBookingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.GetTodayBookingsAsync] No authenticated user");
                return new List<ResourceBookingDto>();
            }

            _logger.LogInformation("[ResourceBookingsClient.GetTodayBookingsAsync] KeycloakId={KeycloakId}", keycloakId);
            return await _resourceBookingService.GetTodayBookingsAsync(keycloakId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetTodayBookingsAsync] Error getting today's bookings");
            throw;
        }
    }

    public async Task<List<BookableResourceSummaryDto>> GetMyAssignedResourcesAsync(CancellationToken cancellationToken = default)
    {
        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.GetMyAssignedResourcesAsync] No authenticated user");
                return new List<BookableResourceSummaryDto>();
            }

            _logger.LogInformation("[ResourceBookingsClient.GetMyAssignedResourcesAsync] KeycloakId={KeycloakId}", keycloakId);
            return await _resourceBookingService.GetMyAssignedResourcesAsync(keycloakId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetMyAssignedResourcesAsync] Error getting assigned resources");
            throw;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<List<ResourceBookingDto>> GetStaffScheduleAsync(DateTime? date = null, CancellationToken cancellationToken = default)
    {
        await _dbLock.WaitAsync(cancellationToken);
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.GetStaffScheduleAsync] No authenticated user");
                return new List<ResourceBookingDto>();
            }

            var targetDate = date ?? DateTime.UtcNow;
            _logger.LogInformation("[ResourceBookingsClient.GetStaffScheduleAsync] KeycloakId={KeycloakId}, Date={Date}", 
                keycloakId, targetDate.Date);
            return await _resourceBookingService.GetStaffScheduleAsync(keycloakId, targetDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetStaffScheduleAsync] Error getting staff schedule");
            throw;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<ResourceBookingDto?> ConfirmBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.ConfirmBookingAsync] Called with empty booking ID");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.ConfirmBookingAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.ConfirmBookingAsync] KeycloakId={KeycloakId}, BookingId={BookingId}", 
                keycloakId, bookingId);
            return await _resourceBookingService.ConfirmBookingAsync(keycloakId, bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.ConfirmBookingAsync] Error confirming booking {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<ResourceBookingDto?> CancelBookingAsync(Guid bookingId, CancelBookingDto request, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.CancelBookingAsync] Called with empty booking ID");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.CancelBookingAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.CancelBookingAsync] KeycloakId={KeycloakId}, BookingId={BookingId}", 
                keycloakId, bookingId);
            return await _resourceBookingService.CancelBookingAsync(keycloakId, bookingId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.CancelBookingAsync] Error cancelling booking {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<ResourceBookingDto?> RescheduleBookingAsync(Guid bookingId, RescheduleBookingDto request, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty || request == null)
        {
            _logger.LogWarning("[ResourceBookingsClient.RescheduleBookingAsync] Called with invalid parameters");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.RescheduleBookingAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.RescheduleBookingAsync] KeycloakId={KeycloakId}, BookingId={BookingId}, NewStart={NewStart}", 
                keycloakId, bookingId, request.NewStartTime);
            return await _resourceBookingService.RescheduleBookingAsync(keycloakId, bookingId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.RescheduleBookingAsync] Error rescheduling booking {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<ResourceBookingDto?> CheckInAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.CheckInAsync] Called with empty booking ID");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.CheckInAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.CheckInAsync] KeycloakId={KeycloakId}, BookingId={BookingId}", 
                keycloakId, bookingId);
            return await _resourceBookingService.CheckInAsync(keycloakId, bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.CheckInAsync] Error checking in booking {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<ResourceBookingDto?> CompleteBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.CompleteBookingAsync] Called with empty booking ID");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.CompleteBookingAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.CompleteBookingAsync] KeycloakId={KeycloakId}, BookingId={BookingId}", 
                keycloakId, bookingId);
            return await _resourceBookingService.CompleteBookingAsync(keycloakId, bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.CompleteBookingAsync] Error completing booking {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<ResourceBookingDto?> MarkNoShowAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.MarkNoShowAsync] Called with empty booking ID");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.MarkNoShowAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.MarkNoShowAsync] KeycloakId={KeycloakId}, BookingId={BookingId}", 
                keycloakId, bookingId);
            return await _resourceBookingService.MarkNoShowAsync(keycloakId, bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.MarkNoShowAsync] Error marking no-show for booking {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<ResourceBookingDto?> UpdateInternalNotesAsync(Guid bookingId, UpdateBookingNotesDto request, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.UpdateInternalNotesAsync] Called with empty booking ID");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.UpdateInternalNotesAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.UpdateInternalNotesAsync] KeycloakId={KeycloakId}, BookingId={BookingId}", 
                keycloakId, bookingId);
            return await _resourceBookingService.UpdateInternalNotesAsync(keycloakId, bookingId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.UpdateInternalNotesAsync] Error updating notes for booking {BookingId}", bookingId);
            throw;
        }
    }

    public async Task<ResourceBookingDto?> SubmitReviewAsync(Guid bookingId, SubmitBookingReviewDto request, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty || request == null)
        {
            _logger.LogWarning("[ResourceBookingsClient.SubmitReviewAsync] Called with invalid parameters");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.SubmitReviewAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.SubmitReviewAsync] KeycloakId={KeycloakId}, BookingId={BookingId}, Rating={Rating}", 
                keycloakId, bookingId, request.Rating);
            return await _resourceBookingService.SubmitReviewAsync(keycloakId, bookingId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.SubmitReviewAsync] Error submitting review for booking {BookingId}", bookingId);
            throw;
        }
    }

    #endregion

    #region Statistics

    public async Task<ResourceStatsDto?> GetResourceStatsAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        if (resourceId == Guid.Empty)
        {
            _logger.LogWarning("[ResourceBookingsClient.GetResourceStatsAsync] Called with empty resource ID");
            return null;
        }

        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.GetResourceStatsAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.GetResourceStatsAsync] KeycloakId={KeycloakId}, ResourceId={ResourceId}", 
                keycloakId, resourceId);
            return await _resourceBookingService.GetResourceStatsAsync(keycloakId, resourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetResourceStatsAsync] Error getting stats for resource {ResourceId}", resourceId);
            throw;
        }
    }

    public async Task<BusinessBookingStatsDto?> GetBusinessStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keycloakId = GetKeycloakIdFromContext();
            if (string.IsNullOrEmpty(keycloakId))
            {
                _logger.LogWarning("[ResourceBookingsClient.GetBusinessStatsAsync] No authenticated user");
                return null;
            }

            _logger.LogInformation("[ResourceBookingsClient.GetBusinessStatsAsync] KeycloakId={KeycloakId}", keycloakId);
            return await _resourceBookingService.GetBusinessStatsAsync(keycloakId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ResourceBookingsClient.GetBusinessStatsAsync] Error getting business stats");
            throw;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Extracts the Keycloak ID from the current HTTP context
    /// </summary>
    private string? GetKeycloakIdFromContext()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext?.User == null)
        {
            return null;
        }

        // Check for mock authentication header (for integration tests)
        if (httpContext.Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        {
            return keycloakIdHeader.ToString();
        }

        // Check if user is authenticated via claims
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var subClaim = httpContext.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(subClaim))
            {
                return subClaim;
            }

            // Fallback: try to find other common claims
            var userIdClaim = httpContext.User.FindFirst("user_id")?.Value 
                           ?? httpContext.User.FindFirst("id")?.Value 
                           ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim;
        }

        return null;
    }

    #endregion
}
