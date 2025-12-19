using System.ComponentModel.DataAnnotations;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents an attendee of a scheduled event.
/// Tracks RSVP status and attendance for events.
/// </summary>
public class EventAttendee : BaseEntity
{
    /// <summary>
    /// The event this attendee is associated with
    /// </summary>
    public virtual Guid EventId { get; set; }

    /// <summary>
    /// Navigation property to the event
    /// </summary>
    public virtual ScheduleEvent? Event { get; set; }

    /// <summary>
    /// The profile attending the event
    /// </summary>
    public virtual Guid ProfileId { get; set; }

    /// <summary>
    /// Navigation property to the attendee's profile
    /// </summary>
    public virtual Profile? Profile { get; set; }

    /// <summary>
    /// RSVP status of the attendee
    /// </summary>
    public virtual AttendeeStatus Status { get; set; } = AttendeeStatus.Pending;

    /// <summary>
    /// Role of the attendee (organizer, speaker, attendee, etc.)
    /// </summary>
    public virtual AttendeeRole Role { get; set; } = AttendeeRole.Attendee;

    /// <summary>
    /// When the attendee responded to the invitation
    /// </summary>
    public virtual DateTime? RespondedAt { get; set; }

    /// <summary>
    /// When the attendee checked in (for in-person events)
    /// </summary>
    public virtual DateTime? CheckedInAt { get; set; }

    /// <summary>
    /// Optional note from the attendee
    /// </summary>
    [MaxLength(500)]
    public virtual string? Note { get; set; }

    /// <summary>
    /// Number of additional guests (plus ones)
    /// </summary>
    public virtual int GuestCount { get; set; }

    /// <summary>
    /// Whether the attendee has been notified
    /// </summary>
    public virtual bool IsNotified { get; set; }

    /// <summary>
    /// Whether the attendee has been reminded
    /// </summary>
    public virtual bool IsReminded { get; set; }

    /// <summary>
    /// Ticket/confirmation number for paid events
    /// </summary>
    [MaxLength(50)]
    public virtual string? ConfirmationNumber { get; set; }

    /// <summary>
    /// Amount paid for the event
    /// </summary>
    public virtual decimal? AmountPaid { get; set; }

    /// <summary>
    /// Payment transaction ID
    /// </summary>
    [MaxLength(100)]
    public virtual string? PaymentTransactionId { get; set; }
}
