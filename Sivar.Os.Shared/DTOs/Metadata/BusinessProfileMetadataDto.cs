using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.DTOs.Metadata;

/// <summary>
/// Metadata DTO for Business Profile type
/// </summary>
public class BusinessProfileMetadataDto
{
    /// <summary>
    /// Business registration number or tax ID
    /// </summary>
    [StringLength(50, ErrorMessage = "Business registration cannot exceed 50 characters")]
    public string BusinessRegistration { get; set; } = string.Empty;

    /// <summary>
    /// Industry or business category
    /// </summary>
    [Required(ErrorMessage = "Industry is required for business profiles")]
    [StringLength(100, ErrorMessage = "Industry cannot exceed 100 characters")]
    public string Industry { get; set; } = string.Empty;

    /// <summary>
    /// Business operating hours
    /// </summary>
    public BusinessHours OperatingHours { get; set; } = new();

    /// <summary>
    /// Services offered by the business
    /// </summary>
    public List<BusinessService> Services { get; set; } = new();

    /// <summary>
    /// Products offered by the business
    /// </summary>
    public List<BusinessProduct> Products { get; set; } = new();

    /// <summary>
    /// Payment methods accepted
    /// </summary>
    public List<string> PaymentMethods { get; set; } = new();

    /// <summary>
    /// Business certifications and licenses
    /// </summary>
    public List<string> Certifications { get; set; } = new();

    /// <summary>
    /// Number of employees
    /// </summary>
    public string EmployeeCount { get; set; } = string.Empty;

    /// <summary>
    /// Year the business was founded
    /// </summary>
    public int? YearFounded { get; set; }

    /// <summary>
    /// Business mission statement
    /// </summary>
    [StringLength(500, ErrorMessage = "Mission statement cannot exceed 500 characters")]
    public string Mission { get; set; } = string.Empty;

    /// <summary>
    /// Delivery areas or service regions
    /// </summary>
    public List<string> ServiceAreas { get; set; } = new();

    /// <summary>
    /// Whether the business offers online services
    /// </summary>
    public bool OffersOnlineServices { get; set; } = false;

    /// <summary>
    /// Whether appointments are required
    /// </summary>
    public bool RequiresAppointment { get; set; } = false;
}

/// <summary>
/// Business operating hours structure
/// </summary>
public class BusinessHours
{
    public DayHours Monday { get; set; } = new();
    public DayHours Tuesday { get; set; } = new();
    public DayHours Wednesday { get; set; } = new();
    public DayHours Thursday { get; set; } = new();
    public DayHours Friday { get; set; } = new();
    public DayHours Saturday { get; set; } = new();
    public DayHours Sunday { get; set; } = new();

    /// <summary>
    /// Special hours for holidays or events
    /// </summary>
    public List<SpecialHours> SpecialDates { get; set; } = new();
}

/// <summary>
/// Hours for a specific day
/// </summary>
public class DayHours
{
    /// <summary>
    /// Whether the business is open on this day
    /// </summary>
    public bool IsOpen { get; set; } = false;

    /// <summary>
    /// Opening time (24-hour format, e.g., "09:00")
    /// </summary>
    public string OpenTime { get; set; } = string.Empty;

    /// <summary>
    /// Closing time (24-hour format, e.g., "17:00")
    /// </summary>
    public string CloseTime { get; set; } = string.Empty;

    /// <summary>
    /// Break periods during the day
    /// </summary>
    public List<BreakPeriod> Breaks { get; set; } = new();
}

/// <summary>
/// Break period during business hours
/// </summary>
public class BreakPeriod
{
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Description { get; set; } = "Break";
}

/// <summary>
/// Special hours for specific dates
/// </summary>
public class SpecialHours
{
    public DateTime Date { get; set; }
    public bool IsOpen { get; set; }
    public string OpenTime { get; set; } = string.Empty;
    public string CloseTime { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Service offered by the business
/// </summary>
public class BusinessService
{
    /// <summary>
    /// Service name
    /// </summary>
    [Required]
    [StringLength(200, ErrorMessage = "Service name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Service description
    /// </summary>
    [StringLength(1000, ErrorMessage = "Service description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Service price (can be range like "$50-$100")
    /// </summary>
    [StringLength(50, ErrorMessage = "Price cannot exceed 50 characters")]
    public string Price { get; set; } = string.Empty;

    /// <summary>
    /// Service duration (e.g., "1 hour", "30 minutes")
    /// </summary>
    [StringLength(50, ErrorMessage = "Duration cannot exceed 50 characters")]
    public string Duration { get; set; } = string.Empty;

    /// <summary>
    /// Whether this service is currently available
    /// </summary>
    public bool IsAvailable { get; set; } = true;
}

/// <summary>
/// Product offered by the business
/// </summary>
public class BusinessProduct
{
    /// <summary>
    /// Product name
    /// </summary>
    [Required]
    [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Product description
    /// </summary>
    [StringLength(1000, ErrorMessage = "Product description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Product price
    /// </summary>
    [StringLength(50, ErrorMessage = "Price cannot exceed 50 characters")]
    public string Price { get; set; } = string.Empty;

    /// <summary>
    /// Product category
    /// </summary>
    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Whether this product is in stock
    /// </summary>
    public bool IsInStock { get; set; } = true;
}