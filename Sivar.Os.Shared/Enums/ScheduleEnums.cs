namespace Sivar.Os.Shared.Enums;

/// <summary>
/// Type of scheduled event
/// </summary>
public enum EventType
{
    /// <summary>
    /// General purpose event
    /// </summary>
    General = 0,

    /// <summary>
    /// Business meeting
    /// </summary>
    Meeting = 1,

    /// <summary>
    /// Appointment with a professional/business
    /// </summary>
    Appointment = 2,

    /// <summary>
    /// Social gathering or party
    /// </summary>
    Social = 3,

    /// <summary>
    /// Workshop or training session
    /// </summary>
    Workshop = 4,

    /// <summary>
    /// Conference or large gathering
    /// </summary>
    Conference = 5,

    /// <summary>
    /// Webinar or online presentation
    /// </summary>
    Webinar = 6,

    /// <summary>
    /// Concert, show, or performance
    /// </summary>
    Concert = 7,

    /// <summary>
    /// Sports event or game
    /// </summary>
    Sports = 8,

    /// <summary>
    /// Community or public event
    /// </summary>
    Community = 9,

    /// <summary>
    /// Religious or spiritual event
    /// </summary>
    Religious = 10,

    /// <summary>
    /// Charity or fundraising event
    /// </summary>
    Charity = 11,

    /// <summary>
    /// Educational class or course
    /// </summary>
    Class = 12,

    /// <summary>
    /// Networking event
    /// </summary>
    Networking = 13,

    /// <summary>
    /// Product launch or announcement
    /// </summary>
    Launch = 14,

    /// <summary>
    /// Sale or promotional event
    /// </summary>
    Sale = 15,

    /// <summary>
    /// Holiday or celebration
    /// </summary>
    Holiday = 16,

    /// <summary>
    /// Personal reminder or task
    /// </summary>
    Reminder = 17,

    /// <summary>
    /// Deadline or due date
    /// </summary>
    Deadline = 18,

    /// <summary>
    /// Recurring business hours availability
    /// </summary>
    Availability = 19
}

/// <summary>
/// Visibility level for events
/// </summary>
public enum EventVisibility
{
    /// <summary>
    /// Visible to everyone
    /// </summary>
    Public = 0,

    /// <summary>
    /// Visible only to the owner
    /// </summary>
    Private = 1,

    /// <summary>
    /// Visible only to followers
    /// </summary>
    FollowersOnly = 2,

    /// <summary>
    /// Visible only to attendees/invitees
    /// </summary>
    AttendeesOnly = 3,

    /// <summary>
    /// Unlisted - accessible via direct link
    /// </summary>
    Unlisted = 4
}

/// <summary>
/// Status of an event
/// </summary>
public enum EventStatus
{
    /// <summary>
    /// Event is scheduled and confirmed
    /// </summary>
    Confirmed = 0,

    /// <summary>
    /// Event is tentative/pending confirmation
    /// </summary>
    Tentative = 1,

    /// <summary>
    /// Event has been cancelled
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Event has been postponed
    /// </summary>
    Postponed = 3,

    /// <summary>
    /// Event is a draft (not published)
    /// </summary>
    Draft = 4,

    /// <summary>
    /// Event has completed
    /// </summary>
    Completed = 5
}

/// <summary>
/// RSVP/attendance status for event attendees
/// </summary>
public enum AttendeeStatus
{
    /// <summary>
    /// Attendee has not responded yet
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Attendee has accepted/confirmed
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Attendee has declined
    /// </summary>
    Declined = 2,

    /// <summary>
    /// Attendee is tentative/maybe
    /// </summary>
    Tentative = 3,

    /// <summary>
    /// Invitation was sent but not delivered
    /// </summary>
    Invited = 4,

    /// <summary>
    /// Attendee checked in at the event
    /// </summary>
    CheckedIn = 5,

    /// <summary>
    /// Attendee was marked as no-show
    /// </summary>
    NoShow = 6,

    /// <summary>
    /// Attendee is on the waitlist
    /// </summary>
    Waitlisted = 7
}

/// <summary>
/// Role of an attendee in an event
/// </summary>
public enum AttendeeRole
{
    /// <summary>
    /// Regular attendee/participant
    /// </summary>
    Attendee = 0,

    /// <summary>
    /// Event organizer
    /// </summary>
    Organizer = 1,

    /// <summary>
    /// Co-organizer with limited permissions
    /// </summary>
    CoOrganizer = 2,

    /// <summary>
    /// Speaker or presenter
    /// </summary>
    Speaker = 3,

    /// <summary>
    /// VIP guest
    /// </summary>
    VIP = 4,

    /// <summary>
    /// Volunteer/helper
    /// </summary>
    Volunteer = 5,

    /// <summary>
    /// Sponsor representative
    /// </summary>
    Sponsor = 6,

    /// <summary>
    /// Media/press
    /// </summary>
    Media = 7
}

/// <summary>
/// Type of reminder notification
/// </summary>
public enum ReminderType
{
    /// <summary>
    /// Push notification
    /// </summary>
    Push = 0,

    /// <summary>
    /// Email notification
    /// </summary>
    Email = 1,

    /// <summary>
    /// SMS text message
    /// </summary>
    SMS = 2,

    /// <summary>
    /// In-app notification only
    /// </summary>
    InApp = 3,

    /// <summary>
    /// WhatsApp message
    /// </summary>
    WhatsApp = 4
}

/// <summary>
/// Recurrence frequency for repeating events
/// </summary>
public enum RecurrenceFrequency
{
    /// <summary>
    /// No recurrence (one-time event)
    /// </summary>
    None = 0,

    /// <summary>
    /// Daily recurrence
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Weekly recurrence
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// Monthly recurrence
    /// </summary>
    Monthly = 3,

    /// <summary>
    /// Yearly recurrence
    /// </summary>
    Yearly = 4
}
