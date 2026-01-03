using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a user's entry in the waiting list for app access.
/// Tracks their position, verification status, and referral information.
/// </summary>
public class WaitingListEntry : BaseEntity
{
    /// <summary>
    /// Reference to the User entity
    /// </summary>
    public virtual Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to User
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// User's email address (denormalized for quick access)
    /// </summary>
    public virtual string Email { get; set; } = string.Empty;

    /// <summary>
    /// Verified phone number in E.164 format (e.g., +50378901234)
    /// </summary>
    public virtual string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., SV, US, GT)
    /// </summary>
    public virtual string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Current status in the waiting list
    /// </summary>
    public virtual WaitingListStatus Status { get; set; } = WaitingListStatus.PendingVerification;

    /// <summary>
    /// Position in the waiting queue (lower = closer to approval)
    /// </summary>
    public virtual int Position { get; set; }

    /// <summary>
    /// When the user joined the waiting list
    /// </summary>
    public virtual DateTime JoinedAt { get; set; }

    /// <summary>
    /// When the user was approved (null if not yet approved)
    /// </summary>
    public virtual DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Username or ID of admin who approved (null if not yet approved)
    /// </summary>
    public virtual string? ApprovedBy { get; set; }

    /// <summary>
    /// This user's unique referral code to share with others
    /// </summary>
    public virtual string ReferralCode { get; set; } = string.Empty;

    /// <summary>
    /// Referral code used when this user signed up (null if no referral)
    /// </summary>
    public virtual string? UsedReferralCode { get; set; }

    /// <summary>
    /// User ID of who referred this user (null if no referral)
    /// </summary>
    public virtual Guid? ReferredByUserId { get; set; }

    /// <summary>
    /// Navigation property to the referring user's waiting list entry
    /// </summary>
    public virtual WaitingListEntry? ReferredBy { get; set; }

    /// <summary>
    /// Count of users this person has successfully referred
    /// </summary>
    public virtual int ReferralCount { get; set; }

    /// <summary>
    /// Channel used for verification (SMS or WhatsApp)
    /// </summary>
    public virtual VerificationChannel VerificationChannel { get; set; }

    /// <summary>
    /// Optional notes from admin (reason for rejection, etc.)
    /// </summary>
    public virtual string? AdminNotes { get; set; }

    /// <summary>
    /// Generates a unique referral code for this user
    /// </summary>
    public static string GenerateReferralCode()
    {
        // Generate 8-character alphanumeric code
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excluding confusing chars (0, O, I, 1)
        var random = new Random();
        return new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
