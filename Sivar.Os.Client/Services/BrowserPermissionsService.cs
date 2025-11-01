using Microsoft.JSInterop;

namespace Sivar.Os.Client.Services;

/// <summary>
/// Service for managing browser permissions (Location, Camera, Microphone, etc.)
/// iOS-style permissions management
/// </summary>
public class BrowserPermissionsService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<BrowserPermissionsService> _logger;
    private IJSObjectReference? _module;

    public BrowserPermissionsService(
        IJSRuntime jsRuntime,
        ILogger<BrowserPermissionsService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        if (_module == null)
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/permissions.js");
        }
        return _module;
    }

    /// <summary>
    /// Gets the current location permission status
    /// </summary>
    /// <returns>PermissionStatus: Granted, Denied, Prompt</returns>
    public async Task<PermissionStatus> GetLocationPermissionStatusAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            var status = await module.InvokeAsync<string>("getLocationPermissionStatus");
            return status?.ToLower() switch
            {
                "granted" => PermissionStatus.Granted,
                "denied" => PermissionStatus.Denied,
                "prompt" => PermissionStatus.Prompt,
                _ => PermissionStatus.Unknown
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get location permission status");
            return PermissionStatus.Denied;
        }
    }

    /// <summary>
    /// Requests location permission and returns current position if granted
    /// </summary>
    /// <returns>GeolocationPosition or null if denied</returns>
    public async Task<GeolocationPosition?> RequestLocationAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<GeolocationPosition?>("requestLocation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request location permission");
            return null;
        }
    }

    /// <summary>
    /// Gets the current position without requesting permission (uses cached permission)
    /// </summary>
    public async Task<GeolocationPosition?> GetCurrentPositionAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<GeolocationPosition?>("getCurrentPosition");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current position");
            return null;
        }
    }

    /// <summary>
    /// Checks if Geolocation API is supported in the browser
    /// </summary>
    public async Task<bool> IsGeolocationSupportedAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<bool>("isGeolocationSupported");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check geolocation support");
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}

/// <summary>
/// Geolocation position from browser
/// </summary>
public class GeolocationPosition
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public double? Altitude { get; set; }
    public double? AltitudeAccuracy { get; set; }
    public double? Heading { get; set; }
    public double? Speed { get; set; }
    public long Timestamp { get; set; }
}

/// <summary>
/// Permission types that can be requested
/// </summary>
public enum PermissionType
{
    Location,
    Camera,
    Microphone,
    Notifications,
    ClipboardRead,
    ClipboardWrite
}

/// <summary>
/// Permission status
/// </summary>
public enum PermissionStatus
{
    Granted,
    Denied,
    Prompt,
    Unknown
}
