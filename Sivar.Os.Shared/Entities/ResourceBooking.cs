using System.ComponentModel.DataAnnotations;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// A booking/reservation made by a customer for a bookable resource.
/// Represents an appointment, reservation, or scheduled use of the resource.
/// </summary>
public class ResourceBooking : BaseEntity
{
    /// <summary>
    /// The resource being booked
    /// </summary>
    public virtual Guid ResourceId { get; set; }
    public virtual BookableResource Resource { get; set; } = null!;

    /// <summary>
    /// The specific service being booked (optional, for Person-type resources with multiple services)
    /// </summary>
    public virtual Guid? ServiceId { get; set; }
    public virtual ResourceService? Service { get; set; }

    /// <summary>
    /// The profile of the customer making the booking
    /// </summary>
    public virtual Guid CustomerProfileId { get; set; }
    public virtual Profile CustomerProfile { get; set; } = null!;

    /// <summary>
    /// Start time of the booking (UTC)
    /// </summary>
    public virtual DateTime StartTime { get; set; }

    /// <summary>
    /// End time of the booking (UTC)
    /// </summary>
    public virtual DateTime EndTime { get; set; }

    /// <summary>
    /// Timezone of the booking (for display purposes)
    /// </summary>
    [StringLength(100)]
    public virtual string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Current status of the booking
    /// </summary>
    public virtual BookingStatus Status { get; set; } = BookingStatus.Pending;

    /// <summary>
    /// Unique confirmation code for the booking
    /// </summary>
    [StringLength(20)]
    public virtual string ConfirmationCode { get; set; } = string.Empty;

    /// <summary>
    /// Customer notes or special requests
    /// </summary>
    [StringLength(1000)]
    public virtual string? CustomerNotes { get; set; }

    /// <summary>
    /// Internal notes from the business (not visible to customer)
    /// </summary>
    [StringLength(1000)]
    public virtual string? InternalNotes { get; set; }

    /// <summary>
    /// Price for this booking
    /// </summary>
    public virtual decimal? Price { get; set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    [StringLength(3)]
    public virtual string? Currency { get; set; }

    /// <summary>
    /// Whether the booking has been paid
    /// </summary>
    public virtual bool IsPaid { get; set; } = false;

    /// <summary>
    /// Payment transaction ID (if applicable)
    /// </summary>
    [StringLength(100)]
    public virtual string? PaymentTransactionId { get; set; }

    /// <summary>
    /// When the booking was confirmed (if status = Confirmed)
    /// </summary>
    public virtual DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// Who confirmed the booking
    /// </summary>
    public virtual Guid? ConfirmedByProfileId { get; set; }

    /// <summary>
    /// When the booking was cancelled (if status = Cancelled)
    /// </summary>
    public virtual DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Who cancelled the booking
    /// </summary>
    public virtual CancellationInitiator? CancelledBy { get; set; }

    /// <summary>
    /// Reason for cancellation
    /// </summary>
    [StringLength(500)]
    public virtual string? CancellationReason { get; set; }

    /// <summary>
    /// When the customer checked in / arrived
    /// </summary>
    public virtual DateTime? CheckedInAt { get; set; }

    /// <summary>
    /// When the service was completed
    /// </summary>
    public virtual DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Number of guests (for table reservations, group bookings)
    /// </summary>
    public virtual int GuestCount { get; set; } = 1;

    /// <summary>
    /// Whether a reminder has been sent
    /// </summary>
    public virtual bool ReminderSent { get; set; } = false;

    /// <summary>
    /// When the reminder was sent
    /// </summary>
    public virtual DateTime? ReminderSentAt { get; set; }

    /// <summary>
    /// If this booking was rescheduled, the original booking ID
    /// </summary>
    public virtual Guid? OriginalBookingId { get; set; }
    public virtual ResourceBooking? OriginalBooking { get; set; }

    /// <summary>
    /// If this booking was rescheduled to a new time, the new booking ID
    /// </summary>
    public virtual Guid? RescheduledToBookingId { get; set; }
    public virtual ResourceBooking? RescheduledToBooking { get; set; }

    /// <summary>
    /// Customer rating after completion (1-5)
    /// </summary>
    public virtual int? Rating { get; set; }

    /// <summary>
    /// Customer review after completion
    /// </summary>
    [StringLength(2000)]
    public virtual string? Review { get; set; }

    /// <summary>
    /// When the review was submitted
    /// </summary>
    public virtual DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public virtual string? MetadataJson { get; set; }

    /// <summary>
    /// Generates a unique confirmation code
    /// </summary>
    public static string GenerateConfirmationCode()
    {
        // Format: XXXX-XXXX (letters and numbers)
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excluding confusing chars
        var random = new Random();
        var code = new char[9];
        for (int i = 0; i < 9; i++)
        {
            if (i == 4)
                code[i] = '-';
            else
                code[i] = chars[random.Next(chars.Length)];
        }
        return new string(code);
    }
}
