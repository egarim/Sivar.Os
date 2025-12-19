namespace Sivar.Os.Shared.Enums;

/// <summary>
/// Type of bookable resource
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// A person who provides services (barber, doctor, trainer, consultant)
    /// </summary>
    Person = 1,

    /// <summary>
    /// A physical object that can be reserved (table, equipment, vehicle)
    /// </summary>
    Object = 2,

    /// <summary>
    /// A space or room (meeting room, court, studio, office)
    /// </summary>
    Space = 3,

    /// <summary>
    /// Equipment that can be rented (tools, machines, sports gear)
    /// </summary>
    Equipment = 4
}

/// <summary>
/// Category of bookable resource for common business types
/// </summary>
public enum ResourceCategory
{
    // Person-based services
    Barber = 1,
    Hairdresser = 2,
    MassageTherapist = 3,
    Doctor = 4,
    Dentist = 5,
    PersonalTrainer = 6,
    Consultant = 7,
    Tutor = 8,
    Photographer = 9,
    Lawyer = 10,

    // Object-based resources
    Table = 50,
    Chair = 51,
    Booth = 52,
    Vehicle = 53,
    Bike = 54,
    Scooter = 55,

    // Space-based resources
    MeetingRoom = 100,
    ConferenceRoom = 101,
    Studio = 102,
    TennisCourt = 103,
    BasketballCourt = 104,
    SwimmingLane = 105,
    GolfTeeTime = 106,
    ParkingSpot = 107,
    EventSpace = 108,
    PrivateDiningRoom = 109,

    // Equipment
    Camera = 150,
    Projector = 151,
    SoundSystem = 152,
    Printer = 153,
    Computer = 154,
    GymEquipment = 155,

    // Generic
    Other = 999
}

/// <summary>
/// Status of a resource booking
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// Booking request submitted, awaiting confirmation
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Booking confirmed by the business
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// Booking cancelled by customer or business
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Service/reservation completed successfully
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Customer did not show up
    /// </summary>
    NoShow = 5,

    /// <summary>
    /// Customer is currently being served / using the resource
    /// </summary>
    InProgress = 6,

    /// <summary>
    /// Booking was rescheduled to a new time
    /// </summary>
    Rescheduled = 7
}

/// <summary>
/// Who cancelled a booking
/// </summary>
public enum CancellationInitiator
{
    Customer = 1,
    Business = 2,
    System = 3
}

/// <summary>
/// Booking confirmation mode for the resource
/// </summary>
public enum BookingConfirmationMode
{
    /// <summary>
    /// Bookings are automatically confirmed
    /// </summary>
    Automatic = 1,

    /// <summary>
    /// Bookings require manual approval by business
    /// </summary>
    ManualApproval = 2
}
