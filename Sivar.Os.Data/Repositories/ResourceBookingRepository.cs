using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for Resource Booking System
/// </summary>
public class ResourceBookingRepository : IResourceBookingRepository
{
    private readonly SivarDbContext _context;

    public ResourceBookingRepository(SivarDbContext context)
    {
        _context = context;
    }

    #region Bookable Resources

    public async Task<BookableResource?> GetResourceByIdAsync(Guid resourceId, bool includeServices = true, bool includeAvailability = true)
    {
        var query = _context.BookableResources
            .Include(r => r.Profile)
            .AsQueryable();

        if (includeServices)
            query = query.Include(r => r.Services.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder));

        if (includeAvailability)
        {
            query = query
                .Include(r => r.Availability.OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime))
                .Include(r => r.Exceptions.Where(e => e.Date >= DateOnly.FromDateTime(DateTime.UtcNow)));
        }

        return await query.FirstOrDefaultAsync(r => r.Id == resourceId);
    }

    public async Task<List<BookableResource>> GetResourcesByProfileIdAsync(Guid profileId, bool activeOnly = true)
    {
        var query = _context.BookableResources
            .Include(r => r.Services.Where(s => !activeOnly || s.IsActive).OrderBy(s => s.DisplayOrder))
            .Where(r => r.ProfileId == profileId);

        if (activeOnly)
            query = query.Where(r => r.IsActive && r.IsVisible);

        return await query.OrderBy(r => r.DisplayOrder).ThenBy(r => r.Name).ToListAsync();
    }

    public async Task<(List<BookableResource> Resources, int TotalCount)> QueryResourcesAsync(ResourceQueryDto query)
    {
        var dbQuery = _context.BookableResources
            .Include(r => r.Profile)
            .Include(r => r.Services.Where(s => s.IsActive))
            .AsQueryable();

        // Apply filters
        if (query.ProfileId.HasValue)
            dbQuery = dbQuery.Where(r => r.ProfileId == query.ProfileId.Value);

        if (query.ResourceType.HasValue)
            dbQuery = dbQuery.Where(r => r.ResourceType == query.ResourceType.Value);

        if (query.Category.HasValue)
            dbQuery = dbQuery.Where(r => r.Category == query.Category.Value);

        if (query.IsActive.HasValue)
            dbQuery = dbQuery.Where(r => r.IsActive == query.IsActive.Value);
        else
            dbQuery = dbQuery.Where(r => r.IsActive && r.IsVisible);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            // Search in resource name, description, tags, AND profile display name/category keys
            dbQuery = dbQuery.Where(r =>
                r.Name.ToLower().Contains(term) ||
                (r.Description != null && r.Description.ToLower().Contains(term)) ||
                r.Tags.Any(t => t.ToLower().Contains(term)) ||
                (r.Profile != null && r.Profile.DisplayName.ToLower().Contains(term)) ||
                (r.Profile != null && r.Profile.CategoryKeys.Any(ck => ck.ToLower().Contains(term))));
        }

        if (query.MaxPrice.HasValue)
            dbQuery = dbQuery.Where(r => r.DefaultPrice <= query.MaxPrice.Value);

        if (query.Tags != null && query.Tags.Length > 0)
            dbQuery = dbQuery.Where(r => r.Tags.Any(t => query.Tags.Contains(t)));

        // Get total count
        var totalCount = await dbQuery.CountAsync();

        // Apply sorting
        dbQuery = query.SortBy?.ToLower() switch
        {
            "name" => query.SortDescending ? dbQuery.OrderByDescending(r => r.Name) : dbQuery.OrderBy(r => r.Name),
            "price" => query.SortDescending ? dbQuery.OrderByDescending(r => r.DefaultPrice) : dbQuery.OrderBy(r => r.DefaultPrice),
            "createdat" => query.SortDescending ? dbQuery.OrderByDescending(r => r.CreatedAt) : dbQuery.OrderBy(r => r.CreatedAt),
            _ => query.SortDescending ? dbQuery.OrderByDescending(r => r.DisplayOrder) : dbQuery.OrderBy(r => r.DisplayOrder)
        };

        // Apply pagination
        var resources = await dbQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (resources, totalCount);
    }

    public async Task<BookableResource> CreateResourceAsync(BookableResource resource)
    {
        resource.CreatedAt = DateTime.UtcNow;
        _context.BookableResources.Add(resource);
        await _context.SaveChangesAsync();
        return resource;
    }

    public async Task<BookableResource> UpdateResourceAsync(BookableResource resource)
    {
        resource.UpdatedAt = DateTime.UtcNow;
        _context.BookableResources.Update(resource);
        await _context.SaveChangesAsync();
        return resource;
    }

    public async Task<bool> DeleteResourceAsync(Guid resourceId, bool hardDelete = false)
    {
        var resource = await _context.BookableResources.FindAsync(resourceId);
        if (resource == null) return false;

        if (hardDelete)
        {
            _context.BookableResources.Remove(resource);
        }
        else
        {
            resource.IsActive = false;
            resource.IsVisible = false;
            resource.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Resource Services

    public async Task<ResourceService?> GetServiceByIdAsync(Guid serviceId)
    {
        return await _context.ResourceServices
            .Include(s => s.Resource)
            .FirstOrDefaultAsync(s => s.Id == serviceId);
    }

    public async Task<List<ResourceService>> GetServicesByResourceIdAsync(Guid resourceId, bool activeOnly = true)
    {
        var query = _context.ResourceServices.Where(s => s.ResourceId == resourceId);

        if (activeOnly)
            query = query.Where(s => s.IsActive);

        return await query.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToListAsync();
    }

    public async Task<ResourceService> CreateServiceAsync(ResourceService service)
    {
        service.CreatedAt = DateTime.UtcNow;
        _context.ResourceServices.Add(service);
        await _context.SaveChangesAsync();
        return service;
    }

    public async Task<ResourceService> UpdateServiceAsync(ResourceService service)
    {
        service.UpdatedAt = DateTime.UtcNow;
        _context.ResourceServices.Update(service);
        await _context.SaveChangesAsync();
        return service;
    }

    public async Task<bool> DeleteServiceAsync(Guid serviceId)
    {
        var service = await _context.ResourceServices.FindAsync(serviceId);
        if (service == null) return false;

        _context.ResourceServices.Remove(service);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Resource Availability

    public async Task<List<ResourceAvailability>> GetAvailabilityByResourceIdAsync(Guid resourceId)
    {
        return await _context.ResourceAvailabilities
            .Where(a => a.ResourceId == resourceId)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<List<ResourceAvailability>> GetAvailabilityForDayAsync(Guid resourceId, System.DayOfWeek dayOfWeek)
    {
        return await _context.ResourceAvailabilities
            .Where(a => a.ResourceId == resourceId && a.DayOfWeek == dayOfWeek && a.IsAvailable)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<List<ResourceAvailability>> SetWeeklyAvailabilityAsync(Guid resourceId, List<ResourceAvailability> availability)
    {
        // Remove existing availability
        var existing = await _context.ResourceAvailabilities
            .Where(a => a.ResourceId == resourceId)
            .ToListAsync();
        _context.ResourceAvailabilities.RemoveRange(existing);

        // Add new availability
        foreach (var a in availability)
        {
            a.ResourceId = resourceId;
            a.CreatedAt = DateTime.UtcNow;
        }
        _context.ResourceAvailabilities.AddRange(availability);

        await _context.SaveChangesAsync();
        return availability;
    }

    public async Task<ResourceAvailability> CreateAvailabilityAsync(ResourceAvailability availability)
    {
        availability.CreatedAt = DateTime.UtcNow;
        _context.ResourceAvailabilities.Add(availability);
        await _context.SaveChangesAsync();
        return availability;
    }

    public async Task<ResourceAvailability> UpdateAvailabilityAsync(ResourceAvailability availability)
    {
        availability.UpdatedAt = DateTime.UtcNow;
        _context.ResourceAvailabilities.Update(availability);
        await _context.SaveChangesAsync();
        return availability;
    }

    public async Task<bool> DeleteAvailabilityAsync(Guid availabilityId)
    {
        var availability = await _context.ResourceAvailabilities.FindAsync(availabilityId);
        if (availability == null) return false;

        _context.ResourceAvailabilities.Remove(availability);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Resource Exceptions

    public async Task<List<ResourceException>> GetExceptionsByResourceIdAsync(Guid resourceId, DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        var query = _context.ResourceExceptions.Where(e => e.ResourceId == resourceId);

        if (fromDate.HasValue)
            query = query.Where(e => e.Date >= fromDate.Value || e.IsRecurringAnnually);

        if (toDate.HasValue)
            query = query.Where(e => e.Date <= toDate.Value || e.IsRecurringAnnually);

        return await query.OrderBy(e => e.Date).ToListAsync();
    }

    public async Task<ResourceException?> GetExceptionForDateAsync(Guid resourceId, DateOnly date)
    {
        // Check for exact date match or recurring annual match
        return await _context.ResourceExceptions
            .Where(e => e.ResourceId == resourceId &&
                       (e.Date == date ||
                        (e.IsRecurringAnnually && e.Date.Month == date.Month && e.Date.Day == date.Day)))
            .FirstOrDefaultAsync();
    }

    public async Task<ResourceException> CreateExceptionAsync(ResourceException exception)
    {
        exception.CreatedAt = DateTime.UtcNow;
        _context.ResourceExceptions.Add(exception);
        await _context.SaveChangesAsync();
        return exception;
    }

    public async Task<ResourceException> UpdateExceptionAsync(ResourceException exception)
    {
        exception.UpdatedAt = DateTime.UtcNow;
        _context.ResourceExceptions.Update(exception);
        await _context.SaveChangesAsync();
        return exception;
    }

    public async Task<bool> DeleteExceptionAsync(Guid exceptionId)
    {
        var exception = await _context.ResourceExceptions.FindAsync(exceptionId);
        if (exception == null) return false;

        _context.ResourceExceptions.Remove(exception);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Resource Bookings

    public async Task<ResourceBooking?> GetBookingByIdAsync(Guid bookingId)
    {
        return await _context.ResourceBookings
            .Include(b => b.Resource)
                .ThenInclude(r => r.Profile)
            .Include(b => b.Service)
            .Include(b => b.CustomerProfile)
            .FirstOrDefaultAsync(b => b.Id == bookingId);
    }

    public async Task<ResourceBooking?> GetBookingByConfirmationCodeAsync(string confirmationCode)
    {
        return await _context.ResourceBookings
            .Include(b => b.Resource)
                .ThenInclude(r => r.Profile)
            .Include(b => b.Service)
            .Include(b => b.CustomerProfile)
            .FirstOrDefaultAsync(b => b.ConfirmationCode == confirmationCode);
    }

    public async Task<(List<ResourceBooking> Bookings, int TotalCount)> QueryBookingsAsync(BookingQueryDto query)
    {
        var dbQuery = _context.ResourceBookings
            .Include(b => b.Resource)
            .Include(b => b.Service)
            .Include(b => b.CustomerProfile)
            .AsQueryable();

        // Apply filters
        if (query.ResourceId.HasValue)
            dbQuery = dbQuery.Where(b => b.ResourceId == query.ResourceId.Value);

        if (query.ServiceId.HasValue)
            dbQuery = dbQuery.Where(b => b.ServiceId == query.ServiceId.Value);

        if (query.CustomerProfileId.HasValue)
            dbQuery = dbQuery.Where(b => b.CustomerProfileId == query.CustomerProfileId.Value);

        if (query.Status.HasValue)
            dbQuery = dbQuery.Where(b => b.Status == query.Status.Value);

        if (query.FromDate.HasValue)
            dbQuery = dbQuery.Where(b => b.StartTime >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            dbQuery = dbQuery.Where(b => b.EndTime <= query.ToDate.Value);

        if (query.IsPaid.HasValue)
            dbQuery = dbQuery.Where(b => b.IsPaid == query.IsPaid.Value);

        // Get total count
        var totalCount = await dbQuery.CountAsync();

        // Apply sorting
        dbQuery = query.SortBy?.ToLower() switch
        {
            "createdat" => query.SortDescending ? dbQuery.OrderByDescending(b => b.CreatedAt) : dbQuery.OrderBy(b => b.CreatedAt),
            "status" => query.SortDescending ? dbQuery.OrderByDescending(b => b.Status) : dbQuery.OrderBy(b => b.Status),
            _ => query.SortDescending ? dbQuery.OrderByDescending(b => b.StartTime) : dbQuery.OrderBy(b => b.StartTime)
        };

        // Apply pagination
        var bookings = await dbQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (bookings, totalCount);
    }

    public async Task<List<ResourceBooking>> GetUpcomingBookingsForCustomerAsync(Guid customerProfileId, int limit = 10)
    {
        return await _context.ResourceBookings
            .Include(b => b.Resource)
                .ThenInclude(r => r.Profile)
            .Include(b => b.Service)
            .Where(b => b.CustomerProfileId == customerProfileId &&
                       b.StartTime >= DateTime.UtcNow &&
                       b.Status != BookingStatus.Cancelled)
            .OrderBy(b => b.StartTime)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<ResourceBooking>> GetUpcomingBookingsForResourceAsync(Guid resourceId, int limit = 50)
    {
        return await _context.ResourceBookings
            .Include(b => b.Service)
            .Include(b => b.CustomerProfile)
            .Where(b => b.ResourceId == resourceId &&
                       b.StartTime >= DateTime.UtcNow &&
                       b.Status != BookingStatus.Cancelled)
            .OrderBy(b => b.StartTime)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<ResourceBooking>> GetBookingsInRangeAsync(Guid resourceId, DateTime start, DateTime end)
    {
        return await _context.ResourceBookings
            .Include(b => b.Service)
            .Include(b => b.CustomerProfile)
            .Where(b => b.ResourceId == resourceId &&
                       b.StartTime < end &&
                       b.EndTime > start &&
                       b.Status != BookingStatus.Cancelled)
            .OrderBy(b => b.StartTime)
            .ToListAsync();
    }

    public async Task<List<ResourceBooking>> GetTodayBookingsForBusinessAsync(Guid businessProfileId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await _context.ResourceBookings
            .Include(b => b.Resource)
            .Include(b => b.Service)
            .Include(b => b.CustomerProfile)
            .Where(b => b.Resource.ProfileId == businessProfileId &&
                       b.StartTime >= today &&
                       b.StartTime < tomorrow)
            .OrderBy(b => b.StartTime)
            .ToListAsync();
    }

    public async Task<bool> IsTimeSlotAvailableAsync(Guid resourceId, DateTime start, DateTime end, Guid? excludeBookingId = null)
    {
        var query = _context.ResourceBookings
            .Where(b => b.ResourceId == resourceId &&
                       b.StartTime < end &&
                       b.EndTime > start &&
                       b.Status != BookingStatus.Cancelled &&
                       b.Status != BookingStatus.NoShow);

        if (excludeBookingId.HasValue)
            query = query.Where(b => b.Id != excludeBookingId.Value);

        // Get the resource to check max concurrent bookings
        var resource = await _context.BookableResources.FindAsync(resourceId);
        if (resource == null) return false;

        var overlappingCount = await query.CountAsync();
        return overlappingCount < resource.MaxConcurrentBookings;
    }

    public async Task<ResourceBooking> CreateBookingAsync(ResourceBooking booking)
    {
        booking.CreatedAt = DateTime.UtcNow;
        if (string.IsNullOrEmpty(booking.ConfirmationCode))
            booking.ConfirmationCode = ResourceBooking.GenerateConfirmationCode();

        _context.ResourceBookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task<ResourceBooking> UpdateBookingAsync(ResourceBooking booking)
    {
        booking.UpdatedAt = DateTime.UtcNow;
        _context.ResourceBookings.Update(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task<ResourceStatsDto> GetResourceStatsAsync(Guid resourceId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var resource = await _context.BookableResources.FindAsync(resourceId);
        if (resource == null)
            return new ResourceStatsDto { ResourceId = resourceId };

        var query = _context.ResourceBookings.Where(b => b.ResourceId == resourceId);

        if (fromDate.HasValue)
            query = query.Where(b => b.StartTime >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(b => b.EndTime <= toDate.Value);

        var bookings = await query.ToListAsync();

        var completed = bookings.Where(b => b.Status == BookingStatus.Completed).ToList();
        var cancelled = bookings.Where(b => b.Status == BookingStatus.Cancelled).ToList();
        var noShow = bookings.Where(b => b.Status == BookingStatus.NoShow).ToList();

        var reviewedBookings = completed.Where(b => b.Rating.HasValue).ToList();

        return new ResourceStatsDto
        {
            ResourceId = resourceId,
            TotalBookings = bookings.Count,
            PendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending),
            ConfirmedBookings = bookings.Count(b => b.Status == BookingStatus.Confirmed),
            CompletedBookings = completed.Count,
            CancelledBookings = cancelled.Count,
            NoShowBookings = noShow.Count,
            AverageRating = reviewedBookings.Any() ? reviewedBookings.Average(b => b.Rating!.Value) : 0,
            ReviewCount = reviewedBookings.Count,
            TotalRevenue = completed.Where(b => b.Price.HasValue).Sum(b => b.Price!.Value),
            Currency = resource.Currency,
            BookingCompletionRate = bookings.Any() ? (double)completed.Count / bookings.Count * 100 : 0,
            CancellationRate = bookings.Any() ? (double)cancelled.Count / bookings.Count * 100 : 0
        };
    }

    public async Task<BusinessBookingStatsDto> GetBusinessStatsAsync(Guid profileId)
    {
        var resources = await _context.BookableResources
            .Where(r => r.ProfileId == profileId)
            .ToListAsync();

        var resourceIds = resources.Select(r => r.Id).ToList();

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var todayBookings = await _context.ResourceBookings
            .CountAsync(b => resourceIds.Contains(b.ResourceId) &&
                           b.StartTime >= today &&
                           b.StartTime < tomorrow);

        var upcomingBookings = await _context.ResourceBookings
            .CountAsync(b => resourceIds.Contains(b.ResourceId) &&
                           b.StartTime >= DateTime.UtcNow &&
                           b.Status == BookingStatus.Confirmed);

        var pendingConfirmations = await _context.ResourceBookings
            .CountAsync(b => resourceIds.Contains(b.ResourceId) &&
                           b.Status == BookingStatus.Pending);

        var thisMonthRevenue = await _context.ResourceBookings
            .Where(b => resourceIds.Contains(b.ResourceId) &&
                       b.Status == BookingStatus.Completed &&
                       b.CompletedAt >= monthStart &&
                       b.CompletedAt < monthEnd &&
                       b.Price.HasValue)
            .SumAsync(b => b.Price ?? 0);

        var currency = resources.FirstOrDefault()?.Currency ?? "USD";

        // Get stats for each resource
        var resourceStats = new List<ResourceStatsDto>();
        foreach (var resource in resources.Take(10)) // Limit to top 10
        {
            var stats = await GetResourceStatsAsync(resource.Id, monthStart, monthEnd);
            resourceStats.Add(stats);
        }

        return new BusinessBookingStatsDto
        {
            ProfileId = profileId,
            TotalResources = resources.Count,
            ActiveResources = resources.Count(r => r.IsActive),
            TodayBookings = todayBookings,
            UpcomingBookings = upcomingBookings,
            PendingConfirmations = pendingConfirmations,
            ThisMonthRevenue = thisMonthRevenue,
            Currency = currency,
            ResourceStats = resourceStats
        };
    }

    public async Task<List<ResourceBooking>> GetBookingsNeedingRemindersAsync(int hoursBeforeBooking)
    {
        var reminderTime = DateTime.UtcNow.AddHours(hoursBeforeBooking);
        var now = DateTime.UtcNow;

        return await _context.ResourceBookings
            .Include(b => b.Resource)
            .Include(b => b.CustomerProfile)
            .Where(b => b.Status == BookingStatus.Confirmed &&
                       !b.ReminderSent &&
                       b.StartTime > now &&
                       b.StartTime <= reminderTime)
            .ToListAsync();
    }

    #endregion
}
