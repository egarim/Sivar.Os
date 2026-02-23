namespace PhotoBooking.Shared.Entities;

/// <summary>
/// Business profile (photo studio, salon, etc.)
/// </summary>
public class BusinessProfile : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }
    
    // Contact info
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? Website { get; set; }
    
    // Location
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string Country { get; set; } = "SV"; // Default El Salvador
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // Settings
    public string PrimaryLanguage { get; set; } = "es";
    public string TimeZone { get; set; } = "America/El_Salvador";
    public string Currency { get; set; } = "USD";
    
    // Relations
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
