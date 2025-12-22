using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// A bookable resource owned by a Business or Organization profile.
/// Resources can be people (barber, doctor) or objects (table, room, equipment).
/// This is a special type of Post that supports time-slot bookings.
/// </summary>
public class BookableResource : BaseEntity
{
    /// <summary>
    /// The business/organization profile that owns this resource
    /// </summary>
    public virtual Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// Reference to the associated Post (PostType = Resource)
    /// The Post contains the public-facing content (title, description, images)
    /// </summary>
    public virtual Guid? PostId { get; set; }
    public virtual Post? Post { get; set; }

    /// <summary>
    /// Name of the resource (e.g., "Carlos - Senior Barber", "Table 5", "Conference Room A")
    /// </summary>
    [Required]
    [StringLength(200)]
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short description of the resource
    /// </summary>
    [StringLength(1000)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// Type of resource: Person, Object, Space, Equipment
    /// </summary>
    public virtual ResourceType ResourceType { get; set; }

    /// <summary>
    /// Category for common resource types
    /// </summary>
    public virtual ResourceCategory Category { get; set; } = ResourceCategory.Other;

    /// <summary>
    /// Avatar/image URL for the resource
    /// For Person type: photo of the person
    /// For Object/Space: image of the item/room
    /// </summary>
    [StringLength(500)]
    public virtual string? ImageUrl { get; set; }

    /// <summary>
    /// Duration of each booking slot in minutes
    /// e.g., 30 for a haircut, 60 for a massage, 15 for a quick consultation
    /// </summary>
    public virtual int SlotDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Buffer time between bookings in minutes
    /// Allows for cleanup, preparation, or transition time
    /// </summary>
    public virtual int BufferMinutes { get; set; } = 0;

    /// <summary>
    /// Maximum number of concurrent bookings for this resource
    /// Usually 1, but could be more for group classes or shared spaces
    /// </summary>
    public virtual int MaxConcurrentBookings { get; set; } = 1;

    /// <summary>
    /// Default price for a booking (can be overridden per service)
    /// </summary>
    public virtual decimal? DefaultPrice { get; set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    [StringLength(3)]
    public virtual string Currency { get; set; } = "USD";

    /// <summary>
    /// Whether bookings are auto-confirmed or require manual approval
    /// </summary>
    public virtual BookingConfirmationMode ConfirmationMode { get; set; } = BookingConfirmationMode.Automatic;

    /// <summary>
    /// Minimum hours in advance a booking can be made
    /// e.g., 2 means customers must book at least 2 hours before the slot
    /// </summary>
    public virtual int MinAdvanceBookingHours { get; set; } = 1;

    /// <summary>
    /// Maximum days in advance a booking can be made
    /// e.g., 30 means customers can book up to 30 days ahead
    /// </summary>
    public virtual int MaxAdvanceBookingDays { get; set; } = 30;

    /// <summary>
    /// Hours before the appointment when customer can still cancel without penalty
    /// </summary>
    public virtual int CancellationWindowHours { get; set; } = 24;

    /// <summary>
    /// Whether this resource is currently accepting bookings
    /// </summary>
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this resource is visible to the public
    /// (can be hidden while setting up or for internal resources)
    /// </summary>
    public virtual bool IsVisible { get; set; } = true;

    /// <summary>
    /// Display order for listing resources (lower = first)
    /// </summary>
    public virtual int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Custom metadata as JSON (for resource-specific data)
    /// e.g., specialties for a barber, capacity for a room
    /// </summary>
    public virtual string? MetadataJson { get; set; }

    /// <summary>
    /// Tags for search and categorization
    /// </summary>
    public virtual string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Services this resource can provide (for Person type)
    /// e.g., a barber might offer: Haircut, Beard Trim, Shave
    /// </summary>
    public virtual ICollection<ResourceService> Services { get; set; } = new ObservableCollection<ResourceService>();

    /// <summary>
    /// Weekly availability schedule
    /// </summary>
    public virtual ICollection<ResourceAvailability> Availability { get; set; } = new ObservableCollection<ResourceAvailability>();

    /// <summary>
    /// Exceptions to regular availability (holidays, special hours, blocked dates)
    /// </summary>
    public virtual ICollection<ResourceException> Exceptions { get; set; } = new ObservableCollection<ResourceException>();

    /// <summary>
    /// All bookings for this resource
    /// </summary>
    public virtual ICollection<ResourceBooking> Bookings { get; set; } = new ObservableCollection<ResourceBooking>();
}
