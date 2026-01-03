using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Services;

/// <summary>
/// Result of sending a verification OTP
/// </summary>
public record SendVerificationResult(
    bool Success,
    string? TwilioVerificationSid,
    VerificationChannel Channel,
    string? ErrorMessage = null
);

/// <summary>
/// Result of checking a verification OTP
/// </summary>
public record CheckVerificationResult(
    bool Success,
    bool IsValid,
    string? ErrorMessage = null
);

/// <summary>
/// Service for phone verification using Twilio Verify API
/// Supports SMS and WhatsApp channels based on country
/// </summary>
public interface ITwilioVerifyService
{
    /// <summary>
    /// Send a verification OTP to the specified phone number
    /// Channel (SMS or WhatsApp) is determined by country code
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format (e.g., +50378901234)</param>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code (e.g., SV, US)</param>
    /// <returns>Result containing success status and Twilio verification SID</returns>
    Task<SendVerificationResult> SendVerificationAsync(string phoneNumber, string countryCode);

    /// <summary>
    /// Check if the provided OTP code is valid for the phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format</param>
    /// <param name="code">The OTP code entered by the user</param>
    /// <returns>Result indicating if the code is valid</returns>
    Task<CheckVerificationResult> CheckVerificationAsync(string phoneNumber, string code);

    /// <summary>
    /// Get the verification channel that will be used for a country
    /// </summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code</param>
    /// <returns>SMS or WhatsApp channel</returns>
    VerificationChannel GetChannelForCountry(string countryCode);

    /// <summary>
    /// Check if the Twilio service is enabled and properly configured
    /// </summary>
    bool IsEnabled { get; }
}
