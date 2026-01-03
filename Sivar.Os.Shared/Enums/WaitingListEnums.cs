namespace Sivar.Os.Shared.Enums;

/// <summary>
/// Status of a user in the waiting list
/// </summary>
public enum WaitingListStatus
{
    /// <summary>
    /// User registered but phone not verified yet
    /// </summary>
    PendingVerification = 0,

    /// <summary>
    /// Phone verified, waiting for admin approval
    /// </summary>
    Waiting = 1,

    /// <summary>
    /// Approved - can access the full app
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Rejected - denied access
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Expired - never completed verification within time limit
    /// </summary>
    Expired = 4
}

/// <summary>
/// Channel used for phone verification
/// </summary>
public enum VerificationChannel
{
    /// <summary>
    /// Standard SMS text message
    /// </summary>
    SMS = 0,

    /// <summary>
    /// WhatsApp message (preferred for Latin America)
    /// </summary>
    WhatsApp = 1
}

/// <summary>
/// Status of a phone verification attempt
/// </summary>
public enum VerificationStatus
{
    /// <summary>
    /// OTP sent, waiting for user to enter code
    /// </summary>
    Pending = 0,

    /// <summary>
    /// User entered correct code, phone verified
    /// </summary>
    Verified = 1,

    /// <summary>
    /// OTP expired (5 minutes)
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Too many failed attempts
    /// </summary>
    Failed = 3
}
