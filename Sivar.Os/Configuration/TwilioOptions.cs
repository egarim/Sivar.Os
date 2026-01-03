namespace Sivar.Os.Configuration;

/// <summary>
/// Configuration options for Twilio Verify API
/// Used for phone verification via SMS and WhatsApp
/// </summary>
public class TwilioOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Twilio";

    /// <summary>
    /// Twilio Account SID (starts with AC)
    /// </summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>
    /// Twilio Auth Token
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Twilio Verify Service SID (starts with VA)
    /// This is the specific Verify service created in Twilio console
    /// </summary>
    public string VerifyServiceSid { get; set; } = string.Empty;

    /// <summary>
    /// Whether Twilio integration is enabled
    /// Set to false in development to use mock verification
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Mock OTP code to use when Twilio is disabled (development only)
    /// </summary>
    public string MockOtpCode { get; set; } = "123456";

    /// <summary>
    /// Countries that should use WhatsApp for verification
    /// ISO 3166-1 alpha-2 codes (e.g., SV, GT, HN)
    /// </summary>
    public string[] WhatsAppCountries { get; set; } = new[]
    {
        // Central America
        "SV", "GT", "HN", "NI", "CR", "PA", "BZ",
        // Mexico
        "MX",
        // South America
        "CO", "PE", "AR", "CL", "EC", "VE", "BO", "PY", "UY", "BR"
    };

    /// <summary>
    /// Check if a country should use WhatsApp for verification
    /// </summary>
    public bool ShouldUseWhatsApp(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return false;

        return WhatsAppCountries.Contains(countryCode.ToUpperInvariant());
    }

    /// <summary>
    /// Validate configuration
    /// </summary>
    public bool IsValid()
    {
        if (!Enabled)
            return true; // Mock mode doesn't need credentials

        return !string.IsNullOrWhiteSpace(AccountSid)
            && !string.IsNullOrWhiteSpace(AuthToken)
            && !string.IsNullOrWhiteSpace(VerifyServiceSid);
    }
}
