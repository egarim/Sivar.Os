namespace PhotoBooking.Shared.Entities;

/// <summary>
/// Service offered by a business (wedding photography, haircut, etc.)
/// </summary>
public class Service : BaseEntity
{
    public Guid BusinessProfileId { get; set; }
    public BusinessProfile BusinessProfile { get; set; } = null!;
    
    // Basic info
    public string Name { get; set; } = string.Empty;
    public string NameEs { get; set; } = string.Empty; // Spanish name
    public string Description { get; set; } = string.Empty;
    public string DescriptionEs { get; set; } = string.Empty; // Spanish description
    
    // Media
    public List<string> PhotoUrls { get; set; } = new();
    public string? VideoUrl { get; set; }
    
    // Pricing
    public decimal Price { get; set; }
    public string PricingType { get; set; } = "fixed"; // fixed, hourly, custom
    public string? PricingNotes { get; set; }
    
    // Duration
    public int DurationMinutes { get; set; }
    public string? DurationNotes { get; set; }
    
    // Availability
    public bool RequiresBooking { get; set; } = true;
    public int MaxAdvanceBookingDays { get; set; } = 90;
    public int MinAdvanceBookingHours { get; set; } = 24;
    
    // Category
    public string Category { get; set; } = string.Empty;
    
    // Status
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    
    // Relations
    public ICollection<ServiceAvailability> Availability { get; set; } = new List<ServiceAvailability>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
