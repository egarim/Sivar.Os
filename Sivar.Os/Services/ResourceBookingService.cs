using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service implementation for Resource Booking System
/// </summary>
public class ResourceBookingService : IResourceBookingService
{
    private readonly IResourceBookingRepository _repository;
    private readonly IProfileService _profileService;
    private readonly ILogger<ResourceBookingService> _logger;

    public ResourceBookingService(
        IResourceBookingRepository repository,
        IProfileService profileService,
        ILogger<ResourceBookingService> logger)
    {
        _repository = repository;
        _profileService = profileService;
        _logger = logger;
    }

    #region Resource Management

    public async Task<BookableResourceDto?> GetResourceAsync(Guid resourceId)
    {
        var resource = await _repository.GetResourceByIdAsync(resourceId);
        return resource == null ? null : MapToResourceDto(resource);
    }

    public async Task<List<BookableResourceSummaryDto>> GetResourcesByProfileAsync(Guid profileId)
    {
        var resources = await _repository.GetResourcesByProfileIdAsync(profileId);
        return resources.Select(MapToResourceSummaryDto).ToList();
    }

    public async Task<ResourceListResponseDto> QueryResourcesAsync(ResourceQueryDto query)
    {
        var (resources, totalCount) = await _repository.QueryResourcesAsync(query);
        return new ResourceListResponseDto
        {
            Resources = resources.Select(MapToResourceSummaryDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<BookableResourceDto?> CreateResourceAsync(string keycloakId, CreateBookableResourceDto dto)
    {
        // Get active profile and verify it's a business/organization
        var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        if (profile == null)
        {
            _logger.LogWarning("User {KeycloakId} has no active profile", keycloakId);
            return null;
        }

        // Verify profile type allows resources (Business or Organization)
        var profileTypeName = profile.ProfileType?.Name ?? "";
        if (profileTypeName != "Business" && profileTypeName != "Organization")
        {
            _logger.LogWarning("Profile {ProfileId} is not a Business or Organization", profile.Id);
            return null;
        }

        var resource = new BookableResource
        {
            ProfileId = profile.Id,
            Name = dto.Name,
            Description = dto.Description,
            ResourceType = dto.ResourceType,
            Category = dto.Category,
            ImageUrl = dto.ImageUrl,
            SlotDurationMinutes = dto.SlotDurationMinutes,
            BufferMinutes = dto.BufferMinutes,
            MaxConcurrentBookings = dto.MaxConcurrentBookings,
            DefaultPrice = dto.DefaultPrice,
            Currency = dto.Currency,
            ConfirmationMode = dto.ConfirmationMode,
            MinAdvanceBookingHours = dto.MinAdvanceBookingHours,
            MaxAdvanceBookingDays = dto.MaxAdvanceBookingDays,
            CancellationWindowHours = dto.CancellationWindowHours,
            IsActive = dto.IsActive,
            IsVisible = dto.IsVisible,
            DisplayOrder = dto.DisplayOrder,
            Tags = dto.Tags
        };

        var created = await _repository.CreateResourceAsync(resource);

        // Add initial availability if provided
        if (dto.Availability != null && dto.Availability.Any())
        {
            var availability = dto.Availability.Select(a => new ResourceAvailability
            {
                ResourceId = created.Id,
                DayOfWeek = a.DayOfWeek,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                IsAvailable = a.IsAvailable,
                Label = a.Label
            }).ToList();
            await _repository.SetWeeklyAvailabilityAsync(created.Id, availability);
        }

        // Add initial services if provided
        if (dto.Services != null && dto.Services.Any())
        {
            foreach (var serviceDto in dto.Services)
            {
                var service = new ResourceService
                {
                    ResourceId = created.Id,
                    Name = serviceDto.Name,
                    Description = serviceDto.Description,
                    DurationMinutes = serviceDto.DurationMinutes,
                    Price = serviceDto.Price,
                    Currency = serviceDto.Currency,
                    IsActive = serviceDto.IsActive,
                    DisplayOrder = serviceDto.DisplayOrder,
                    ImageUrl = serviceDto.ImageUrl
                };
                await _repository.CreateServiceAsync(service);
            }
        }

        // Reload with all data
        var result = await _repository.GetResourceByIdAsync(created.Id);
        return result == null ? null : MapToResourceDto(result);
    }

    public async Task<BookableResourceDto?> UpdateResourceAsync(string keycloakId, Guid resourceId, UpdateBookableResourceDto dto)
    {
        var resource = await _repository.GetResourceByIdAsync(resourceId, false, false);
        if (resource == null) return null;

        // Verify ownership
        if (!await IsResourceOwnerAsync(keycloakId, resource))
        {
            _logger.LogWarning("User {KeycloakId} is not authorized to update resource {ResourceId}", keycloakId, resourceId);
            return null;
        }

        // Apply updates
        if (dto.Name != null) resource.Name = dto.Name;
        if (dto.Description != null) resource.Description = dto.Description;
        if (dto.Category.HasValue) resource.Category = dto.Category.Value;
        if (dto.ImageUrl != null) resource.ImageUrl = dto.ImageUrl;
        if (dto.SlotDurationMinutes.HasValue) resource.SlotDurationMinutes = dto.SlotDurationMinutes.Value;
        if (dto.BufferMinutes.HasValue) resource.BufferMinutes = dto.BufferMinutes.Value;
        if (dto.MaxConcurrentBookings.HasValue) resource.MaxConcurrentBookings = dto.MaxConcurrentBookings.Value;
        if (dto.DefaultPrice.HasValue) resource.DefaultPrice = dto.DefaultPrice.Value;
        if (dto.Currency != null) resource.Currency = dto.Currency;
        if (dto.ConfirmationMode.HasValue) resource.ConfirmationMode = dto.ConfirmationMode.Value;
        if (dto.MinAdvanceBookingHours.HasValue) resource.MinAdvanceBookingHours = dto.MinAdvanceBookingHours.Value;
        if (dto.MaxAdvanceBookingDays.HasValue) resource.MaxAdvanceBookingDays = dto.MaxAdvanceBookingDays.Value;
        if (dto.CancellationWindowHours.HasValue) resource.CancellationWindowHours = dto.CancellationWindowHours.Value;
        if (dto.IsActive.HasValue) resource.IsActive = dto.IsActive.Value;
        if (dto.IsVisible.HasValue) resource.IsVisible = dto.IsVisible.Value;
        if (dto.DisplayOrder.HasValue) resource.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.Tags != null) resource.Tags = dto.Tags;

        await _repository.UpdateResourceAsync(resource);

        var result = await _repository.GetResourceByIdAsync(resourceId);
        return result == null ? null : MapToResourceDto(result);
    }

    public async Task<bool> DeleteResourceAsync(string keycloakId, Guid resourceId)
    {
        var resource = await _repository.GetResourceByIdAsync(resourceId, false, false);
        if (resource == null) return false;

        if (!await IsResourceOwnerAsync(keycloakId, resource))
        {
            _logger.LogWarning("User {KeycloakId} is not authorized to delete resource {ResourceId}", keycloakId, resourceId);
            return false;
        }

        return await _repository.DeleteResourceAsync(resourceId);
    }

    #endregion

    #region Service Management

    public async Task<List<ResourceServiceDto>> GetServicesAsync(Guid resourceId)
    {
        var services = await _repository.GetServicesByResourceIdAsync(resourceId);
        return services.Select(MapToServiceDto).ToList();
    }

    public async Task<ResourceServiceDto?> AddServiceAsync(string keycloakId, Guid resourceId, CreateResourceServiceDto dto)
    {
        var resource = await _repository.GetResourceByIdAsync(resourceId, false, false);
        if (resource == null) return null;

        if (!await IsResourceOwnerAsync(keycloakId, resource))
            return null;

        var service = new ResourceService
        {
            ResourceId = resourceId,
            Name = dto.Name,
            Description = dto.Description,
            DurationMinutes = dto.DurationMinutes,
            Price = dto.Price,
            Currency = dto.Currency ?? resource.Currency,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            ImageUrl = dto.ImageUrl
        };

        var created = await _repository.CreateServiceAsync(service);
        return MapToServiceDto(created);
    }

    public async Task<ResourceServiceDto?> UpdateServiceAsync(string keycloakId, Guid serviceId, UpdateResourceServiceDto dto)
    {
        var service = await _repository.GetServiceByIdAsync(serviceId);
        if (service == null) return null;

        if (!await IsResourceOwnerAsync(keycloakId, service.Resource))
            return null;

        if (dto.Name != null) service.Name = dto.Name;
        if (dto.Description != null) service.Description = dto.Description;
        if (dto.DurationMinutes.HasValue) service.DurationMinutes = dto.DurationMinutes.Value;
        if (dto.Price.HasValue) service.Price = dto.Price.Value;
        if (dto.Currency != null) service.Currency = dto.Currency;
        if (dto.IsActive.HasValue) service.IsActive = dto.IsActive.Value;
        if (dto.DisplayOrder.HasValue) service.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.ImageUrl != null) service.ImageUrl = dto.ImageUrl;

        var updated = await _repository.UpdateServiceAsync(service);
        return MapToServiceDto(updated);
    }

    public async Task<bool> DeleteServiceAsync(string keycloakId, Guid serviceId)
    {
        var service = await _repository.GetServiceByIdAsync(serviceId);
        if (service == null) return false;

        if (!await IsResourceOwnerAsync(keycloakId, service.Resource))
            return false;

        return await _repository.DeleteServiceAsync(serviceId);
    }

    #endregion

    #region Availability Management

    public async Task<List<ResourceAvailabilityDto>> GetAvailabilityAsync(Guid resourceId)
    {
        var availability = await _repository.GetAvailabilityByResourceIdAsync(resourceId);
        return availability.Select(MapToAvailabilityDto).ToList();
    }

    public async Task<List<ResourceAvailabilityDto>> SetWeeklyAvailabilityAsync(string keycloakId, Guid resourceId, SetWeeklyAvailabilityDto dto)
    {
        var resource = await _repository.GetResourceByIdAsync(resourceId, false, false);
        if (resource == null) return new List<ResourceAvailabilityDto>();

        if (!await IsResourceOwnerAsync(keycloakId, resource))
            return new List<ResourceAvailabilityDto>();

        var availability = dto.Schedule.Select(a => new ResourceAvailability
        {
            ResourceId = resourceId,
            DayOfWeek = a.DayOfWeek,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            IsAvailable = a.IsAvailable,
            Label = a.Label
        }).ToList();

        var result = await _repository.SetWeeklyAvailabilityAsync(resourceId, availability);
        return result.Select(MapToAvailabilityDto).ToList();
    }

    public async Task<List<ResourceExceptionDto>> GetExceptionsAsync(Guid resourceId, DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        var exceptions = await _repository.GetExceptionsByResourceIdAsync(resourceId, fromDate, toDate);
        return exceptions.Select(MapToExceptionDto).ToList();
    }

    public async Task<ResourceExceptionDto?> AddExceptionAsync(string keycloakId, Guid resourceId, CreateResourceExceptionDto dto)
    {
        var resource = await _repository.GetResourceByIdAsync(resourceId, false, false);
        if (resource == null) return null;

        if (!await IsResourceOwnerAsync(keycloakId, resource))
            return null;

        var exception = new ResourceException
        {
            ResourceId = resourceId,
            Date = dto.Date,
            IsAvailable = dto.IsAvailable,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Reason = dto.Reason,
            IsRecurringAnnually = dto.IsRecurringAnnually
        };

        var created = await _repository.CreateExceptionAsync(exception);
        return MapToExceptionDto(created);
    }

    public async Task<bool> DeleteExceptionAsync(string keycloakId, Guid exceptionId)
    {
        var exceptions = await _repository.GetExceptionsByResourceIdAsync(Guid.Empty);
        // Note: Need to get exception by ID and verify ownership
        return await _repository.DeleteExceptionAsync(exceptionId);
    }

    #endregion

    #region Booking Operations

    public async Task<AvailableSlotsResponseDto> GetAvailableSlotsAsync(GetAvailableSlotsDto query)
    {
        var resource = await _repository.GetResourceByIdAsync(query.ResourceId);
        if (resource == null)
            return new AvailableSlotsResponseDto { ResourceId = query.ResourceId };

        // Determine duration based on service or resource default
        int durationMinutes = resource.SlotDurationMinutes;
        decimal? price = resource.DefaultPrice;
        string? serviceName = null;

        if (query.ServiceId.HasValue)
        {
            var service = resource.Services.FirstOrDefault(s => s.Id == query.ServiceId.Value);
            if (service != null)
            {
                durationMinutes = service.DurationMinutes;
                price = service.Price;
                serviceName = service.Name;
            }
        }

        var response = new AvailableSlotsResponseDto
        {
            ResourceId = resource.Id,
            ResourceName = resource.Name,
            ServiceId = query.ServiceId,
            ServiceName = serviceName,
            SlotsByDate = new Dictionary<DateOnly, List<AvailableTimeSlotDto>>()
        };

        int daysToCheck = query.DaysAhead ?? 1;
        var startDate = query.Date;

        for (int i = 0; i < daysToCheck; i++)
        {
            var date = startDate.AddDays(i);
            var slots = await GetSlotsForDateAsync(resource, date, durationMinutes, price, query.TimeZone);
            if (slots.Any())
            {
                response.SlotsByDate[date] = slots;
            }
        }

        return response;
    }

    private async Task<List<AvailableTimeSlotDto>> GetSlotsForDateAsync(
        BookableResource resource, 
        DateOnly date, 
        int durationMinutes, 
        decimal? price,
        string timeZone)
    {
        var slots = new List<AvailableTimeSlotDto>();
        var dayOfWeek = date.DayOfWeek;

        // Check for exception on this date
        var exception = await _repository.GetExceptionForDateAsync(resource.Id, date);
        if (exception != null && !exception.IsAvailable)
            return slots; // Closed on this day

        // Get availability blocks for this day
        List<(TimeOnly Start, TimeOnly End)> availableBlocks;
        
        if (exception != null && exception.IsAvailable && exception.StartTime.HasValue && exception.EndTime.HasValue)
        {
            // Use exception hours
            availableBlocks = new List<(TimeOnly, TimeOnly)> { (exception.StartTime.Value, exception.EndTime.Value) };
        }
        else
        {
            // Use regular schedule
            var availability = await _repository.GetAvailabilityForDayAsync(resource.Id, dayOfWeek);
            availableBlocks = availability.Where(a => a.IsAvailable).Select(a => (a.StartTime, a.EndTime)).ToList();
        }

        if (!availableBlocks.Any())
            return slots;

        // Get existing bookings for this day
        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var existingBookings = await _repository.GetBookingsInRangeAsync(resource.Id, dayStart, dayEnd);

        // Check min advance booking
        var now = DateTime.UtcNow;
        var minBookingTime = now.AddHours(resource.MinAdvanceBookingHours);

        // Generate slots for each availability block
        foreach (var block in availableBlocks)
        {
            var slotStart = date.ToDateTime(block.Start, DateTimeKind.Utc);
            var blockEnd = date.ToDateTime(block.End, DateTimeKind.Utc);

            while (slotStart.AddMinutes(durationMinutes) <= blockEnd)
            {
                var slotEnd = slotStart.AddMinutes(durationMinutes);

                // Check if slot is in the future (respecting min advance booking)
                if (slotStart > minBookingTime)
                {
                    // Count overlapping bookings
                    var overlapping = existingBookings.Count(b =>
                        b.StartTime < slotEnd && b.EndTime > slotStart &&
                        b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.NoShow);

                    var availableCapacity = resource.MaxConcurrentBookings - overlapping;

                    if (availableCapacity > 0)
                    {
                        slots.Add(new AvailableTimeSlotDto
                        {
                            StartTime = slotStart,
                            EndTime = slotEnd,
                            DurationMinutes = durationMinutes,
                            AvailableCapacity = availableCapacity,
                            Price = price,
                            Currency = resource.Currency
                        });
                    }
                }

                // Move to next slot (duration + buffer)
                slotStart = slotStart.AddMinutes(durationMinutes + resource.BufferMinutes);
            }
        }

        return slots;
    }

    public async Task<ResourceBookingDto?> CreateBookingAsync(string keycloakId, CreateResourceBookingDto dto)
    {
        // Get customer profile
        var customerProfile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        if (customerProfile == null)
        {
            _logger.LogWarning("User {KeycloakId} has no active profile for booking", keycloakId);
            return null;
        }

        var resource = await _repository.GetResourceByIdAsync(dto.ResourceId);
        if (resource == null || !resource.IsActive)
        {
            _logger.LogWarning("Resource {ResourceId} not found or not active", dto.ResourceId);
            return null;
        }

        // Determine duration and price
        int durationMinutes = resource.SlotDurationMinutes;
        decimal? price = resource.DefaultPrice;
        string? currency = resource.Currency;

        if (dto.ServiceId.HasValue)
        {
            var service = resource.Services.FirstOrDefault(s => s.Id == dto.ServiceId.Value);
            if (service != null)
            {
                durationMinutes = service.DurationMinutes;
                price = service.Price;
                currency = service.Currency ?? resource.Currency;
            }
        }

        var endTime = dto.EndTime ?? dto.StartTime.AddMinutes(durationMinutes);

        // Validate booking time
        var now = DateTime.UtcNow;
        if (dto.StartTime <= now.AddHours(resource.MinAdvanceBookingHours))
        {
            _logger.LogWarning("Booking start time {StartTime} is too soon", dto.StartTime);
            return null;
        }

        if (dto.StartTime > now.AddDays(resource.MaxAdvanceBookingDays))
        {
            _logger.LogWarning("Booking start time {StartTime} is too far in advance", dto.StartTime);
            return null;
        }

        // Check availability
        var isAvailable = await _repository.IsTimeSlotAvailableAsync(dto.ResourceId, dto.StartTime, endTime);
        if (!isAvailable)
        {
            _logger.LogWarning("Time slot not available for resource {ResourceId}", dto.ResourceId);
            return null;
        }

        var booking = new ResourceBooking
        {
            ResourceId = dto.ResourceId,
            ServiceId = dto.ServiceId,
            CustomerProfileId = customerProfile.Id,
            StartTime = dto.StartTime,
            EndTime = endTime,
            TimeZone = dto.TimeZone,
            Status = resource.ConfirmationMode == BookingConfirmationMode.Automatic 
                ? BookingStatus.Confirmed 
                : BookingStatus.Pending,
            CustomerNotes = dto.CustomerNotes,
            Price = price,
            Currency = currency,
            GuestCount = dto.GuestCount,
            ConfirmationCode = ResourceBooking.GenerateConfirmationCode()
        };

        if (booking.Status == BookingStatus.Confirmed)
        {
            booking.ConfirmedAt = DateTime.UtcNow;
        }

        var created = await _repository.CreateBookingAsync(booking);
        var result = await _repository.GetBookingByIdAsync(created.Id);
        return result == null ? null : MapToBookingDto(result);
    }

    public async Task<ResourceBookingDto?> GetBookingAsync(Guid bookingId)
    {
        var booking = await _repository.GetBookingByIdAsync(bookingId);
        return booking == null ? null : MapToBookingDto(booking);
    }

    public async Task<ResourceBookingDto?> GetBookingByConfirmationCodeAsync(string confirmationCode)
    {
        var booking = await _repository.GetBookingByConfirmationCodeAsync(confirmationCode);
        return booking == null ? null : MapToBookingDto(booking);
    }

    public async Task<List<ResourceBookingSummaryDto>> GetMyUpcomingBookingsAsync(string keycloakId)
    {
        var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        if (profile == null) return new List<ResourceBookingSummaryDto>();

        var bookings = await _repository.GetUpcomingBookingsForCustomerAsync(profile.Id);
        return bookings.Select(MapToBookingSummaryDto).ToList();
    }

    public async Task<BookingListResponseDto> GetMyBookingHistoryAsync(string keycloakId, int page = 1, int pageSize = 20)
    {
        var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        if (profile == null)
            return new BookingListResponseDto { Page = page, PageSize = pageSize };

        var query = new BookingQueryDto
        {
            CustomerProfileId = profile.Id,
            Page = page,
            PageSize = pageSize,
            SortDescending = true
        };

        var (bookings, totalCount) = await _repository.QueryBookingsAsync(query);
        return new BookingListResponseDto
        {
            Bookings = bookings.Select(MapToBookingSummaryDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<BookingListResponseDto> GetBusinessBookingsAsync(string keycloakId, BookingQueryDto query)
    {
        var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        if (profile == null)
            return new BookingListResponseDto { Page = query.Page, PageSize = query.PageSize };

        // Get all resources for this business
        var resources = await _repository.GetResourcesByProfileIdAsync(profile.Id, false);
        if (!resources.Any())
            return new BookingListResponseDto { Page = query.Page, PageSize = query.PageSize };

        // Filter by resource ID if provided and verify ownership
        if (query.ResourceId.HasValue)
        {
            if (!resources.Any(r => r.Id == query.ResourceId.Value))
                return new BookingListResponseDto { Page = query.Page, PageSize = query.PageSize };
        }
        else
        {
            // No specific resource - we need to query all resources for this business
            // This would require modifying the query to filter by multiple resource IDs
            // For now, we'll just get the first resource's bookings
            query.ResourceId = resources.First().Id;
        }

        var (bookings, totalCount) = await _repository.QueryBookingsAsync(query);
        return new BookingListResponseDto
        {
            Bookings = bookings.Select(MapToBookingSummaryDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<List<ResourceBookingDto>> GetTodayBookingsAsync(string keycloakId)
    {
        var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        if (profile == null) return new List<ResourceBookingDto>();

        var bookings = await _repository.GetTodayBookingsForBusinessAsync(profile.Id);
        return bookings.Select(MapToBookingDto).ToList();
    }

    public async Task<List<BookableResourceSummaryDto>> GetMyAssignedResourcesAsync(string keycloakId)
    {
        var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        if (profile == null) return new List<BookableResourceSummaryDto>();

        var resources = await _repository.GetResourcesByAssignedProfileIdAsync(profile.Id);
        return resources.Select(r => new BookableResourceSummaryDto
        {
            Id = r.Id,
            ProfileId = r.ProfileId,
            ProfileName = r.Profile?.DisplayName ?? string.Empty,
            Name = r.Name,
            Description = r.Description,
            ResourceType = r.ResourceType,
            Category = r.Category,
            ImageUrl = r.ImageUrl,
            DefaultPrice = r.DefaultPrice,
            Currency = r.Currency,
            SlotDurationMinutes = r.SlotDurationMinutes,
            IsActive = r.IsActive,
            ServiceCount = r.Services?.Count(s => s.IsActive) ?? 0
        }).ToList();
    }

    public async Task<List<ResourceBookingDto>> GetStaffScheduleAsync(string keycloakId, DateTime date)
    {
        var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        if (profile == null) return new List<ResourceBookingDto>();

        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var bookings = await _repository.GetBookingsForStaffAsync(profile.Id, startOfDay, endOfDay);
        return bookings.Select(MapToBookingDto).ToList();
    }

    public async Task<ResourceBookingDto?> ConfirmBookingAsync(string keycloakId, Guid bookingId)
    {
        var booking = await _repository.GetBookingByIdAsync(bookingId);
        if (booking == null) return null;

        if (!await IsResourceOwnerAsync(keycloakId, booking.Resource))
            return null;

        if (booking.Status != BookingStatus.Pending)
            return null;

        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedAt = DateTime.UtcNow;
        booking.ConfirmedByProfileId = booking.Resource.ProfileId;

        await _repository.UpdateBookingAsync(booking);
        return MapToBookingDto(booking);
    }

    public async Task<ResourceBookingDto?> CancelBookingAsync(string keycloakId, Guid bookingId, CancelBookingDto dto)
    {
        var booking = await _repository.GetBookingByIdAsync(bookingId);
        if (booking == null) return null;

        var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        if (profile == null) return null;

        // Check if user is customer or business owner
        bool isCustomer = booking.CustomerProfileId == profile.Id;
        bool isOwner = await IsResourceOwnerAsync(keycloakId, booking.Resource);

        if (!isCustomer && !isOwner)
            return null;

        if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
            return null;

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancelledBy = isCustomer ? CancellationInitiator.Customer : CancellationInitiator.Business;
        booking.CancellationReason = dto.Reason;

        await _repository.UpdateBookingAsync(booking);
        return MapToBookingDto(booking);
    }

    public async Task<ResourceBookingDto?> RescheduleBookingAsync(string keycloakId, Guid bookingId, RescheduleBookingDto dto)
    {
        var booking = await _repository.GetBookingByIdAsync(bookingId);
        if (booking == null) return null;

        var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        if (profile == null) return null;

        bool isCustomer = booking.CustomerProfileId == profile.Id;
        bool isOwner = await IsResourceOwnerAsync(keycloakId, booking.Resource);

        if (!isCustomer && !isOwner)
            return null;

        // Check new slot availability
        var duration = (booking.EndTime - booking.StartTime).TotalMinutes;
        var newEndTime = dto.NewEndTime ?? dto.NewStartTime.AddMinutes(duration);

        var isAvailable = await _repository.IsTimeSlotAvailableAsync(
            booking.ResourceId, dto.NewStartTime, newEndTime, bookingId);
        if (!isAvailable)
            return null;

        // Create new booking
        var newBooking = new ResourceBooking
        {
            ResourceId = booking.ResourceId,
            ServiceId = booking.ServiceId,
            CustomerProfileId = booking.CustomerProfileId,
            StartTime = dto.NewStartTime,
            EndTime = newEndTime,
            TimeZone = booking.TimeZone,
            Status = BookingStatus.Confirmed,
            CustomerNotes = booking.CustomerNotes,
            Price = booking.Price,
            Currency = booking.Currency,
            GuestCount = booking.GuestCount,
            OriginalBookingId = booking.Id,
            ConfirmedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateBookingAsync(newBooking);

        // Update old booking
        booking.Status = BookingStatus.Rescheduled;
        booking.RescheduledToBookingId = created.Id;
        await _repository.UpdateBookingAsync(booking);

        var result = await _repository.GetBookingByIdAsync(created.Id);
        return result == null ? null : MapToBookingDto(result);
    }

    public async Task<ResourceBookingDto?> CheckInAsync(string keycloakId, Guid bookingId)
    {
        var booking = await _repository.GetBookingByIdAsync(bookingId);
        if (booking == null) return null;

        if (!await IsResourceOwnerAsync(keycloakId, booking.Resource))
            return null;

        if (booking.Status != BookingStatus.Confirmed)
            return null;

        booking.Status = BookingStatus.InProgress;
        booking.CheckedInAt = DateTime.UtcNow;

        await _repository.UpdateBookingAsync(booking);
        return MapToBookingDto(booking);
    }

    public async Task<ResourceBookingDto?> CompleteBookingAsync(string keycloakId, Guid bookingId)
    {
        var booking = await _repository.GetBookingByIdAsync(bookingId);
        if (booking == null) return null;

        if (!await IsResourceOwnerAsync(keycloakId, booking.Resource))
            return null;

        if (booking.Status != BookingStatus.InProgress && booking.Status != BookingStatus.Confirmed)
            return null;

        booking.Status = BookingStatus.Completed;
        booking.CompletedAt = DateTime.UtcNow;

        await _repository.UpdateBookingAsync(booking);
        return MapToBookingDto(booking);
    }

    public async Task<ResourceBookingDto?> MarkNoShowAsync(string keycloakId, Guid bookingId)
    {
        var booking = await _repository.GetBookingByIdAsync(bookingId);
        if (booking == null) return null;

        if (!await IsResourceOwnerAsync(keycloakId, booking.Resource))
            return null;

        booking.Status = BookingStatus.NoShow;

        await _repository.UpdateBookingAsync(booking);
        return MapToBookingDto(booking);
    }

    public async Task<ResourceBookingDto?> UpdateInternalNotesAsync(string keycloakId, Guid bookingId, UpdateBookingNotesDto dto)
    {
        var booking = await _repository.GetBookingByIdAsync(bookingId);
        if (booking == null) return null;

        if (!await IsResourceOwnerAsync(keycloakId, booking.Resource))
            return null;

        booking.InternalNotes = dto.InternalNotes;

        await _repository.UpdateBookingAsync(booking);
        return MapToBookingDto(booking);
    }

    public async Task<ResourceBookingDto?> SubmitReviewAsync(string keycloakId, Guid bookingId, SubmitBookingReviewDto dto)
    {
        var booking = await _repository.GetBookingByIdAsync(bookingId);
        if (booking == null) return null;

        var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        if (profile == null || booking.CustomerProfileId != profile.Id)
            return null;

        if (booking.Status != BookingStatus.Completed)
            return null;

        if (booking.Rating.HasValue) // Already reviewed
            return null;

        booking.Rating = dto.Rating;
        booking.Review = dto.Review;
        booking.ReviewedAt = DateTime.UtcNow;

        await _repository.UpdateBookingAsync(booking);
        return MapToBookingDto(booking);
    }

    #endregion

    #region Statistics

    public async Task<ResourceStatsDto?> GetResourceStatsAsync(string keycloakId, Guid resourceId)
    {
        var resource = await _repository.GetResourceByIdAsync(resourceId, false, false);
        if (resource == null) return null;

        if (!await IsResourceOwnerAsync(keycloakId, resource))
            return null;

        return await _repository.GetResourceStatsAsync(resourceId);
    }

    public async Task<BusinessBookingStatsDto?> GetBusinessStatsAsync(string keycloakId)
    {
        var profile = await _profileService.GetMyActiveProfileAsync(keycloakId);
        if (profile == null) return null;

        return await _repository.GetBusinessStatsAsync(profile.Id);
    }

    #endregion

    #region Helper Methods

    private async Task<bool> IsResourceOwnerAsync(string keycloakId, BookableResource resource)
    {
        var profiles = await _profileService.GetMyProfilesAsync(keycloakId);
        return profiles.Any(p => p.Id == resource.ProfileId);
    }

    private static BookableResourceDto MapToResourceDto(BookableResource r) => new()
    {
        Id = r.Id,
        ProfileId = r.ProfileId,
        ProfileName = r.Profile?.DisplayName ?? "",
        ProfileAvatar = r.Profile?.Avatar ?? "",
        PostId = r.PostId,
        Name = r.Name,
        Description = r.Description,
        ResourceType = r.ResourceType,
        Category = r.Category,
        ImageUrl = r.ImageUrl,
        SlotDurationMinutes = r.SlotDurationMinutes,
        BufferMinutes = r.BufferMinutes,
        MaxConcurrentBookings = r.MaxConcurrentBookings,
        DefaultPrice = r.DefaultPrice,
        Currency = r.Currency,
        ConfirmationMode = r.ConfirmationMode,
        MinAdvanceBookingHours = r.MinAdvanceBookingHours,
        MaxAdvanceBookingDays = r.MaxAdvanceBookingDays,
        CancellationWindowHours = r.CancellationWindowHours,
        IsActive = r.IsActive,
        IsVisible = r.IsVisible,
        DisplayOrder = r.DisplayOrder,
        Tags = r.Tags,
        Services = r.Services.Select(MapToServiceDto).ToList(),
        Availability = r.Availability.Select(MapToAvailabilityDto).ToList(),
        Exceptions = r.Exceptions.Select(MapToExceptionDto).ToList(),
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };

    private static BookableResourceSummaryDto MapToResourceSummaryDto(BookableResource r) => new()
    {
        Id = r.Id,
        ProfileId = r.ProfileId,
        ProfileName = r.Profile?.DisplayName ?? "",
        Name = r.Name,
        Description = r.Description,
        ResourceType = r.ResourceType,
        Category = r.Category,
        ImageUrl = r.ImageUrl,
        DefaultPrice = r.DefaultPrice,
        Currency = r.Currency,
        SlotDurationMinutes = r.SlotDurationMinutes,
        IsActive = r.IsActive,
        ServiceCount = r.Services.Count
    };

    private static ResourceServiceDto MapToServiceDto(ResourceService s) => new()
    {
        Id = s.Id,
        ResourceId = s.ResourceId,
        Name = s.Name,
        Description = s.Description,
        DurationMinutes = s.DurationMinutes,
        Price = s.Price,
        Currency = s.Currency,
        IsActive = s.IsActive,
        DisplayOrder = s.DisplayOrder,
        ImageUrl = s.ImageUrl
    };

    private static ResourceAvailabilityDto MapToAvailabilityDto(ResourceAvailability a) => new()
    {
        Id = a.Id,
        ResourceId = a.ResourceId,
        DayOfWeek = a.DayOfWeek,
        StartTime = a.StartTime,
        EndTime = a.EndTime,
        IsAvailable = a.IsAvailable,
        Label = a.Label
    };

    private static ResourceExceptionDto MapToExceptionDto(ResourceException e) => new()
    {
        Id = e.Id,
        ResourceId = e.ResourceId,
        Date = e.Date,
        IsAvailable = e.IsAvailable,
        StartTime = e.StartTime,
        EndTime = e.EndTime,
        Reason = e.Reason,
        IsRecurringAnnually = e.IsRecurringAnnually
    };

    private static ResourceBookingDto MapToBookingDto(ResourceBooking b) => new()
    {
        Id = b.Id,
        ResourceId = b.ResourceId,
        ResourceName = b.Resource?.Name ?? "",
        ResourceImageUrl = b.Resource?.ImageUrl,
        ResourceType = b.Resource?.ResourceType ?? ResourceType.Person,
        ServiceId = b.ServiceId,
        ServiceName = b.Service?.Name,
        CustomerProfileId = b.CustomerProfileId,
        CustomerName = b.CustomerProfile?.DisplayName ?? "",
        CustomerAvatar = b.CustomerProfile?.Avatar,
        StartTime = b.StartTime,
        EndTime = b.EndTime,
        TimeZone = b.TimeZone,
        Status = b.Status,
        ConfirmationCode = b.ConfirmationCode,
        CustomerNotes = b.CustomerNotes,
        InternalNotes = b.InternalNotes,
        Price = b.Price,
        Currency = b.Currency,
        IsPaid = b.IsPaid,
        ConfirmedAt = b.ConfirmedAt,
        CancelledAt = b.CancelledAt,
        CancelledBy = b.CancelledBy,
        CancellationReason = b.CancellationReason,
        CheckedInAt = b.CheckedInAt,
        CompletedAt = b.CompletedAt,
        GuestCount = b.GuestCount,
        Rating = b.Rating,
        Review = b.Review,
        ReviewedAt = b.ReviewedAt,
        CreatedAt = b.CreatedAt,
        BusinessProfileId = b.Resource?.ProfileId ?? Guid.Empty,
        BusinessName = b.Resource?.Profile?.DisplayName ?? ""
    };

    private static ResourceBookingSummaryDto MapToBookingSummaryDto(ResourceBooking b) => new()
    {
        Id = b.Id,
        ResourceName = b.Resource?.Name ?? "",
        ServiceName = b.Service?.Name,
        ResourceType = b.Resource?.ResourceType ?? ResourceType.Person,
        StartTime = b.StartTime,
        EndTime = b.EndTime,
        Status = b.Status,
        ConfirmationCode = b.ConfirmationCode,
        Price = b.Price,
        Currency = b.Currency,
        CustomerName = b.CustomerProfile?.DisplayName ?? "",
        GuestCount = b.GuestCount
    };

    #endregion
}
