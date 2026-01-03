using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sivar.Os.Configuration;
using Sivar.Os.Shared.Enums;
using Twilio;
using Twilio.Rest.Verify.V2.Service;

namespace Sivar.Os.Services;

/// <summary>
/// Twilio Verify API implementation for phone verification
/// Supports SMS and WhatsApp channels based on country
/// </summary>
public class TwilioVerifyService : ITwilioVerifyService
{
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioVerifyService> _logger;
    private bool _initialized;

    public TwilioVerifyService(
        IOptions<TwilioOptions> options,
        ILogger<TwilioVerifyService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        InitializeTwilio();
    }

    private void InitializeTwilio()
    {
        if (_initialized || !_options.Enabled)
            return;

        if (!_options.IsValid())
        {
            _logger.LogWarning("Twilio configuration is invalid. Phone verification will use mock mode.");
            return;
        }

        try
        {
            TwilioClient.Init(_options.AccountSid, _options.AuthToken);
            _initialized = true;
            _logger.LogInformation("Twilio client initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Twilio client");
        }
    }

    /// <inheritdoc />
    public bool IsEnabled => _options.Enabled && _initialized;

    /// <inheritdoc />
    public VerificationChannel GetChannelForCountry(string countryCode)
    {
        return _options.ShouldUseWhatsApp(countryCode)
            ? VerificationChannel.WhatsApp
            : VerificationChannel.SMS;
    }

    /// <inheritdoc />
    public async Task<SendVerificationResult> SendVerificationAsync(string phoneNumber, string countryCode)
    {
        var channel = GetChannelForCountry(countryCode);
        var twilioChannel = channel == VerificationChannel.WhatsApp ? "whatsapp" : "sms";

        _logger.LogInformation(
            "Sending verification to {PhoneNumber} via {Channel} (Country: {CountryCode})",
            MaskPhoneNumber(phoneNumber),
            channel,
            countryCode);

        // Mock mode for development
        if (!IsEnabled)
        {
            _logger.LogWarning("Twilio is disabled. Using mock verification.");
            return new SendVerificationResult(
                Success: true,
                TwilioVerificationSid: $"MOCK_{Guid.NewGuid():N}",
                Channel: channel
            );
        }

        try
        {
            var verification = await VerificationResource.CreateAsync(
                to: phoneNumber,
                channel: twilioChannel,
                pathServiceSid: _options.VerifyServiceSid
            );

            _logger.LogInformation(
                "Verification sent successfully. SID: {Sid}, Status: {Status}",
                verification.Sid,
                verification.Status);

            return new SendVerificationResult(
                Success: verification.Status == "pending",
                TwilioVerificationSid: verification.Sid,
                Channel: channel
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification to {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return new SendVerificationResult(
                Success: false,
                TwilioVerificationSid: null,
                Channel: channel,
                ErrorMessage: ex.Message
            );
        }
    }

    /// <inheritdoc />
    public async Task<CheckVerificationResult> CheckVerificationAsync(string phoneNumber, string code)
    {
        _logger.LogInformation(
            "Checking verification for {PhoneNumber}",
            MaskPhoneNumber(phoneNumber));

        // Mock mode for development
        if (!IsEnabled)
        {
            var isValid = code == _options.MockOtpCode;
            _logger.LogWarning(
                "Twilio is disabled. Mock verification result: {IsValid}",
                isValid);
            
            return new CheckVerificationResult(
                Success: true,
                IsValid: isValid
            );
        }

        try
        {
            var verificationCheck = await VerificationCheckResource.CreateAsync(
                to: phoneNumber,
                code: code,
                pathServiceSid: _options.VerifyServiceSid
            );

            var isValid = verificationCheck.Status == "approved";

            _logger.LogInformation(
                "Verification check completed. Status: {Status}, Valid: {IsValid}",
                verificationCheck.Status,
                isValid);

            return new CheckVerificationResult(
                Success: true,
                IsValid: isValid
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check verification for {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return new CheckVerificationResult(
                Success: false,
                IsValid: false,
                ErrorMessage: ex.Message
            );
        }
    }

    /// <summary>
    /// Mask phone number for logging (show last 4 digits only)
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return "****";

        return $"***{phoneNumber[^4..]}";
    }
}
