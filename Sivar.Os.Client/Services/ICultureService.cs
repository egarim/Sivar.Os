using System.Globalization;

namespace Sivar.Os.Client.Services;

/// <summary>
/// Service for managing culture/language preferences with priority-based resolution
/// Priority: Profile Preference > Browser Language > Default (en-US)
/// </summary>
public interface ICultureService
{
    /// <summary>
    /// Gets the effective culture to use based on priority chain
    /// </summary>
    Task<CultureInfo> GetEffectiveCultureAsync();

    /// <summary>
    /// Gets the user's profile language preference
    /// </summary>
    Task<string?> GetProfileCultureAsync();

    /// <summary>
    /// Sets the user's profile language preference and applies it
    /// </summary>
    Task<bool> SetProfileCultureAsync(string? languageCode);

    /// <summary>
    /// Gets the browser's preferred language
    /// </summary>
    Task<string?> GetBrowserCultureAsync();

    /// <summary>
    /// Gets the default culture (en-US)
    /// </summary>
    CultureInfo GetDefaultCulture();

    /// <summary>
    /// Gets all supported cultures
    /// </summary>
    CultureInfo[] GetSupportedCultures();

    /// <summary>
    /// Event raised when culture changes
    /// </summary>
    event EventHandler<CultureInfo>? CultureChanged;
}
