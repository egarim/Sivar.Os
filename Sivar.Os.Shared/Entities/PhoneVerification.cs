using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a phone verification attempt using Twilio Verify API.
/// Tracks OTP requests and verification status.
/// </summary>
public class PhoneVerification : BaseEntity
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
    /// Phone number in E.164 format (e.g., +50378901234)
    /// </summary>
    public virtual string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., SV, US, GT)
    /// </summary>
    public virtual string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Channel used for this verification (SMS or WhatsApp)
    /// </summary>
    public virtual VerificationChannel Channel { get; set; }

    /// <summary>
    /// Current status of this verification attempt
    /// </summary>
    public virtual VerificationStatus Status { get; set; } = VerificationStatus.Pending;

    /// <summary>
    /// Twilio Verification SID for this attempt
    /// </summary>
    public virtual string? TwilioVerificationSid { get; set; }

    /// <summary>
    /// When the OTP was requested
    /// </summary>
    public virtual DateTime RequestedAt { get; set; }

    /// <summary>
    /// When the OTP expires (typically 5 minutes after request)
    /// </summary>
    public virtual DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Number of verification attempts made with this OTP
    /// </summary>
    public virtual int AttemptCount { get; set; }

    /// <summary>
    /// When the phone was successfully verified (null if not verified)
    /// </summary>
    public virtual DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// IP address of the requester (for rate limiting and security)
    /// </summary>
    public virtual string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the requester (for security logging)
    /// </summary>
    public virtual string? UserAgent { get; set; }

    /// <summary>
    /// Check if this verification has expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Check if max attempts exceeded (5 attempts max)
    /// </summary>
    public bool IsMaxAttemptsExceeded => AttemptCount >= 5;

    /// <summary>
    /// Default OTP expiry in minutes
    /// </summary>
    public const int DefaultExpiryMinutes = 5;

    /// <summary>
    /// Maximum verification attempts per OTP
    /// </summary>
    public const int MaxAttempts = 5;
}
