using System.ComponentModel.DataAnnotations;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.DTOs;

#region Resource DTOs

/// <summary>
/// Full bookable resource DTO for detailed views
/// </summary>
public class BookableResourceDto
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string ProfileAvatar { get; set; } = string.Empty;
    public Guid? PostId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ResourceType ResourceType { get; set; }
    public ResourceCategory Category { get; set; }
    public string? ImageUrl { get; set; }
    
    public int SlotDurationMinutes { get; set; }
    public int BufferMinutes { get; set; }
    public int MaxConcurrentBookings { get; set; }
    
    public decimal? DefaultPrice { get; set; }
    public string Currency { get; set; } = "USD";
    
    public BookingConfirmationMode ConfirmationMode { get; set; }
    public int MinAdvanceBookingHours { get; set; }
    public int MaxAdvanceBookingDays { get; set; }
    public int CancellationWindowHours { get; set; }
    
    public bool IsActive { get; set; }
    public bool IsVisible { get; set; }
    public int DisplayOrder { get; set; }
    
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    public List<ResourceServiceDto> Services { get; set; } = new();
    public List<ResourceAvailabilityDto> Availability { get; set; } = new();
    public List<ResourceExceptionDto> Exceptions { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Summary DTO for resource listings
/// </summary>
public class BookableResourceSummaryDto
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ResourceType ResourceType { get; set; }
    public ResourceCategory Category { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? DefaultPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public int SlotDurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public int ServiceCount { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

/// <summary>
/// DTO for creating a new bookable resource
/// </summary>
public class CreateBookableResourceDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public ResourceType ResourceType { get; set; }
    
    public ResourceCategory Category { get; set; } = ResourceCategory.Other;
    
    [StringLength(500)]
    public string? ImageUrl { get; set; }
    
    [Range(5, 480)]
    public int SlotDurationMinutes { get; set; } = 30;
    
    [Range(0, 60)]
    public int BufferMinutes { get; set; } = 0;
    
    [Range(1, 100)]
    public int MaxConcurrentBookings { get; set; } = 1;
    
    [Range(0, 10000)]
    public decimal? DefaultPrice { get; set; }
    
    [StringLength(3)]
    public string Currency { get; set; } = "USD";
    
    public BookingConfirmationMode ConfirmationMode { get; set; } = BookingConfirmationMode.Automatic;
    
    [Range(0, 168)]
    public int MinAdvanceBookingHours { get; set; } = 1;
    
    [Range(1, 365)]
    public int MaxAdvanceBookingDays { get; set; } = 30;
    
    [Range(0, 168)]
    public int CancellationWindowHours { get; set; } = 24;
    
    public bool IsActive { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Initial weekly availability schedule
    /// </summary>
    public List<CreateResourceAvailabilityDto>? Availability { get; set; }
    
    /// <summary>
    /// Initial services offered
    /// </summary>
    public List<CreateResourceServiceDto>? Services { get; set; }
}

/// <summary>
/// DTO for updating a bookable resource
/// </summary>
public class UpdateBookableResourceDto
{
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public ResourceCategory? Category { get; set; }
    
    [StringLength(500)]
    public string? ImageUrl { get; set; }
    
    [Range(5, 480)]
    public int? SlotDurationMinutes { get; set; }
    
    [Range(0, 60)]
    public int? BufferMinutes { get; set; }
    
    [Range(1, 100)]
    public int? MaxConcurrentBookings { get; set; }
    
    [Range(0, 10000)]
    public decimal? DefaultPrice { get; set; }
    
    [StringLength(3)]
    public string? Currency { get; set; }
    
    public BookingConfirmationMode? ConfirmationMode { get; set; }
    
    [Range(0, 168)]
    public int? MinAdvanceBookingHours { get; set; }
    
    [Range(1, 365)]
    public int? MaxAdvanceBookingDays { get; set; }
    
    [Range(0, 168)]
    public int? CancellationWindowHours { get; set; }
    
    public bool? IsActive { get; set; }
    public bool? IsVisible { get; set; }
    public int? DisplayOrder { get; set; }
    
    public string[]? Tags { get; set; }
}

#endregion

#region Resource Service DTOs

/// <summary>
/// Full service DTO
/// </summary>
public class ResourceServiceDto
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public string? ImageUrl { get; set; }
}

/// <summary>
/// DTO for creating a service
/// </summary>
public class CreateResourceServiceDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [Range(5, 480)]
    public int DurationMinutes { get; set; }
    
    [Required]
    [Range(0, 10000)]
    public decimal Price { get; set; }
    
    [StringLength(3)]
    public string? Currency { get; set; }
    
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    
    [StringLength(500)]
    public string? ImageUrl { get; set; }
}

/// <summary>
/// DTO for updating a service
/// </summary>
public class UpdateResourceServiceDto
{
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Range(5, 480)]
    public int? DurationMinutes { get; set; }
    
    [Range(0, 10000)]
    public decimal? Price { get; set; }
    
    [StringLength(3)]
    public string? Currency { get; set; }
    
    public bool? IsActive { get; set; }
    public int? DisplayOrder { get; set; }
    
    [StringLength(500)]
    public string? ImageUrl { get; set; }
}

#endregion

#region Availability DTOs

/// <summary>
/// Full availability DTO
/// </summary>
public class ResourceAvailabilityDto
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public System.DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public string? Label { get; set; }
}

/// <summary>
/// DTO for creating availability
/// </summary>
public class CreateResourceAvailabilityDto
{
    [Required]
    public System.DayOfWeek DayOfWeek { get; set; }
    
    [Required]
    public TimeOnly StartTime { get; set; }
    
    [Required]
    public TimeOnly EndTime { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    [StringLength(100)]
    public string? Label { get; set; }
}

/// <summary>
/// DTO for updating availability
/// </summary>
public class UpdateResourceAvailabilityDto
{
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool? IsAvailable { get; set; }
    
    [StringLength(100)]
    public string? Label { get; set; }
}

/// <summary>
/// Bulk update for weekly schedule
/// </summary>
public class SetWeeklyAvailabilityDto
{
    [Required]
    public List<CreateResourceAvailabilityDto> Schedule { get; set; } = new();
}

#endregion

#region Exception DTOs

/// <summary>
/// Full exception DTO
/// </summary>
public class ResourceExceptionDto
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsAvailable { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Reason { get; set; }
    public bool IsRecurringAnnually { get; set; }
}

/// <summary>
/// DTO for creating an exception
/// </summary>
public class CreateResourceExceptionDto
{
    [Required]
    public DateOnly Date { get; set; }
    
    public bool IsAvailable { get; set; } = false;
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    
    [StringLength(500)]
    public string? Reason { get; set; }
    
    public bool IsRecurringAnnually { get; set; } = false;
}

#endregion

#region Booking DTOs

/// <summary>
/// Full booking DTO for detailed views
/// </summary>
public class ResourceBookingDto
{
    public Guid Id { get; set; }
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string? ResourceImageUrl { get; set; }
    public ResourceType ResourceType { get; set; }
    
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    
    public Guid CustomerProfileId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAvatar { get; set; }
    
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string TimeZone { get; set; } = "UTC";
    
    public BookingStatus Status { get; set; }
    public string ConfirmationCode { get; set; } = string.Empty;
    
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }
    
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public bool IsPaid { get; set; }
    
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public CancellationInitiator? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }
    
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public int GuestCount { get; set; }
    
    public int? Rating { get; set; }
    public string? Review { get; set; }
    public DateTime? ReviewedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Business info
    public Guid BusinessProfileId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
}

/// <summary>
/// Summary DTO for booking listings
/// </summary>
public class ResourceBookingSummaryDto
{
    public Guid Id { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public ResourceType ResourceType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingStatus Status { get; set; }
    public string ConfirmationCode { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
}

/// <summary>
/// DTO for creating a booking
/// </summary>
public class CreateResourceBookingDto
{
    [Required]
    public Guid ResourceId { get; set; }
    
    public Guid? ServiceId { get; set; }
    
    [Required]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// Optional - if not provided, calculated from service/resource duration
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    [StringLength(100)]
    public string TimeZone { get; set; } = "UTC";
    
    [StringLength(1000)]
    public string? CustomerNotes { get; set; }
    
    [Range(1, 20)]
    public int GuestCount { get; set; } = 1;
}

/// <summary>
/// DTO for rescheduling a booking
/// </summary>
public class RescheduleBookingDto
{
    [Required]
    public DateTime NewStartTime { get; set; }
    
    public DateTime? NewEndTime { get; set; }
    
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for cancelling a booking
/// </summary>
public class CancelBookingDto
{
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for submitting a review
/// </summary>
public class SubmitBookingReviewDto
{
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }
    
    [StringLength(2000)]
    public string? Review { get; set; }
}

/// <summary>
/// DTO for updating internal notes (business side)
/// </summary>
public class UpdateBookingNotesDto
{
    [StringLength(1000)]
    public string? InternalNotes { get; set; }
}

#endregion

#region Time Slot DTOs

/// <summary>
/// Represents an available time slot for booking
/// </summary>
public class AvailableTimeSlotDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public int AvailableCapacity { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
}

/// <summary>
/// Query parameters for getting available slots
/// </summary>
public class GetAvailableSlotsDto
{
    [Required]
    public Guid ResourceId { get; set; }
    
    public Guid? ServiceId { get; set; }
    
    [Required]
    public DateOnly Date { get; set; }
    
    /// <summary>
    /// If true, returns slots for the next N days instead of a single day
    /// </summary>
    public int? DaysAhead { get; set; }
    
    [StringLength(100)]
    public string TimeZone { get; set; } = "UTC";
}

/// <summary>
/// Response with available slots grouped by date
/// </summary>
public class AvailableSlotsResponseDto
{
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public Dictionary<DateOnly, List<AvailableTimeSlotDto>> SlotsByDate { get; set; } = new();
}

#endregion

#region Query DTOs

/// <summary>
/// Query parameters for listing resources
/// </summary>
public class ResourceQueryDto
{
    public Guid? ProfileId { get; set; }
    public ResourceType? ResourceType { get; set; }
    public ResourceCategory? Category { get; set; }
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public decimal? MaxPrice { get; set; }
    public string[]? Tags { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "DisplayOrder";
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// Query parameters for listing bookings
/// </summary>
public class BookingQueryDto
{
    public Guid? ResourceId { get; set; }
    public Guid? ServiceId { get; set; }
    public Guid? CustomerProfileId { get; set; }
    public BookingStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsPaid { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "StartTime";
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// Response for paginated resource list
/// </summary>
public class ResourceListResponseDto
{
    public List<BookableResourceSummaryDto> Resources { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Response for paginated booking list
/// </summary>
public class BookingListResponseDto
{
    public List<ResourceBookingSummaryDto> Bookings { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

#endregion

#region Stats DTOs

/// <summary>
/// Statistics for a resource
/// </summary>
public class ResourceStatsDto
{
    public Guid ResourceId { get; set; }
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int NoShowBookings { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public string Currency { get; set; } = "USD";
    public double BookingCompletionRate { get; set; }
    public double CancellationRate { get; set; }
}

/// <summary>
/// Dashboard stats for a business
/// </summary>
public class BusinessBookingStatsDto
{
    public Guid ProfileId { get; set; }
    public int TotalResources { get; set; }
    public int ActiveResources { get; set; }
    public int TodayBookings { get; set; }
    public int UpcomingBookings { get; set; }
    public int PendingConfirmations { get; set; }
    public decimal ThisMonthRevenue { get; set; }
    public string Currency { get; set; } = "USD";
    public List<ResourceStatsDto> ResourceStats { get; set; } = new();
}

#endregion
