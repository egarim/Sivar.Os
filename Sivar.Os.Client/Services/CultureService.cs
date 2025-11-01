using System.Globalization;
using Microsoft.JSInterop;
using Sivar.Os.Shared.Clients;

namespace Sivar.Os.Client.Services;

/// <summary>
/// Implementation of culture service with priority-based language resolution
/// Priority: Profile Preference > Browser Language > Default (en-US)
/// </summary>
public class CultureService : ICultureService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IProfilesClient _profilesClient;
    private readonly ILogger<CultureService> _logger;
    
    private static readonly CultureInfo DefaultCulture = new("en-US");
    private static readonly CultureInfo[] SupportedCultures = new[]
    {
        new CultureInfo("en-US"),
        new CultureInfo("es-ES")
    };

    public event EventHandler<CultureInfo>? CultureChanged;

    public CultureService(
        IJSRuntime jsRuntime,
        IProfilesClient profilesClient,
        ILogger<CultureService> logger)
    {
        _jsRuntime = jsRuntime;
        _profilesClient = profilesClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets the effective culture based on priority chain
    /// Priority: Profile > Browser > Default
    /// </summary>
    public async Task<CultureInfo> GetEffectiveCultureAsync()
    {
        try
        {
            // Priority 1: Profile preference
            var profileCulture = await GetProfileCultureAsync();
            if (!string.IsNullOrEmpty(profileCulture))
            {
                var culture = ParseCulture(profileCulture);
                if (culture != null)
                {
                    _logger.LogInformation("[CultureService] Using profile culture: {Culture}", culture.Name);
                    return culture;
                }
            }

            // Priority 2: Browser language
            var browserCulture = await GetBrowserCultureAsync();
            if (!string.IsNullOrEmpty(browserCulture))
            {
                var culture = ParseCulture(browserCulture);
                if (culture != null)
                {
                    _logger.LogInformation("[CultureService] Using browser culture: {Culture}", culture.Name);
                    return culture;
                }
            }

            // Priority 3: Default
            _logger.LogInformation("[CultureService] Using default culture: {Culture}", DefaultCulture.Name);
            return DefaultCulture;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CultureService] Error getting effective culture, falling back to default");
            return DefaultCulture;
        }
    }

    /// <summary>
    /// Gets the user's profile language preference
    /// </summary>
    public async Task<string?> GetProfileCultureAsync()
    {
        try
        {
            var profile = await _profilesClient.GetMyActiveProfileAsync();
            if (profile?.PreferredLanguage != null)
            {
                _logger.LogInformation("[CultureService] Profile language: {Language}", profile.PreferredLanguage);
                return profile.PreferredLanguage;
            }

            _logger.LogInformation("[CultureService] No profile language set");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CultureService] Error getting profile culture");
            return null;
        }
    }

    /// <summary>
    /// Sets the user's profile language preference and applies it
    /// </summary>
    public async Task<bool> SetProfileCultureAsync(string? languageCode)
    {
        try
        {
            // Get active profile
            var profile = await _profilesClient.GetMyActiveProfileAsync();
            if (profile == null)
            {
                _logger.LogWarning("[CultureService] No active profile found");
                return false;
            }

            // Update language preference
            var success = await _profilesClient.UpdatePreferredLanguageAsync(profile.Id, languageCode);
            
            if (success)
            {
                _logger.LogInformation("[CultureService] Updated profile language to: {Language}", languageCode ?? "null (browser default)");
                
                // Apply the new culture
                var newCulture = await GetEffectiveCultureAsync();
                await ApplyCultureAsync(newCulture);
                
                return true;
            }

            _logger.LogWarning("[CultureService] Failed to update profile language");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CultureService] Error setting profile culture");
            return false;
        }
    }

    /// <summary>
    /// Gets the browser's preferred language using JavaScript interop
    /// </summary>
    public async Task<string?> GetBrowserCultureAsync()
    {
        try
        {
            var browserLanguage = await _jsRuntime.InvokeAsync<string>("eval", "navigator.language || navigator.userLanguage");
            
            if (!string.IsNullOrEmpty(browserLanguage))
            {
                _logger.LogInformation("[CultureService] Browser language: {Language}", browserLanguage);
                return browserLanguage;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CultureService] Error getting browser culture");
            return null;
        }
    }

    /// <summary>
    /// Gets the default culture (en-US)
    /// </summary>
    public CultureInfo GetDefaultCulture()
    {
        return DefaultCulture;
    }

    /// <summary>
    /// Gets all supported cultures
    /// </summary>
    public CultureInfo[] GetSupportedCultures()
    {
        return SupportedCultures;
    }

    /// <summary>
    /// Applies the culture to the current thread and UI
    /// </summary>
    private async Task ApplyCultureAsync(CultureInfo culture)
    {
        try
        {
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Notify subscribers
            CultureChanged?.Invoke(this, culture);

            // For Blazor WASM, we need to reload to fully apply the culture change
            // Store the culture in localStorage for persistence across reloads
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "selectedCulture", culture.Name);

            _logger.LogInformation("[CultureService] Culture applied: {Culture}", culture.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CultureService] Error applying culture");
        }
    }

    /// <summary>
    /// Parses a culture string and validates it's supported
    /// </summary>
    private CultureInfo? ParseCulture(string? cultureCode)
    {
        if (string.IsNullOrEmpty(cultureCode))
            return null;

        try
        {
            var culture = new CultureInfo(cultureCode);
            
            // Check if it's in our supported list
            if (SupportedCultures.Any(c => c.Name == culture.Name))
            {
                return culture;
            }

            // Check if the base language is supported (e.g., "es" matches "es-ES")
            var baseLanguage = culture.TwoLetterISOLanguageName;
            var supportedCulture = SupportedCultures.FirstOrDefault(c => 
                c.TwoLetterISOLanguageName == baseLanguage);
            
            if (supportedCulture != null)
            {
                _logger.LogInformation("[CultureService] Mapped {Input} to supported culture {Output}", 
                    cultureCode, supportedCulture.Name);
                return supportedCulture;
            }

            _logger.LogWarning("[CultureService] Culture {Culture} not supported", cultureCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CultureService] Invalid culture code: {Culture}", cultureCode);
            return null;
        }
    }
}
