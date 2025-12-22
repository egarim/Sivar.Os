using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// Device-related context detected from browser.
/// Auto-populated via JavaScript interop.
/// </summary>
public record DeviceContext
{
    /// <summary>
    /// IANA timezone identifier (e.g., "America/El_Salvador", "America/New_York")
    /// Detected from browser's Intl.DateTimeFormat().resolvedOptions().timeZone
    /// </summary>
    [StringLength(100)]
    public string TimeZone { get; init; } = "UTC";

    /// <summary>
    /// Device's current local time with offset
    /// </summary>
    public DateTimeOffset LocalDateTime { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timezone offset in minutes from UTC (e.g., -360 for CST)
    /// Negative values are west of UTC, positive are east
    /// </summary>
    public int TimeZoneOffsetMinutes { get; init; }

    /// <summary>
    /// Device type: "mobile", "tablet", or "desktop"
    /// Detected from screen size and user agent
    /// </summary>
    [StringLength(20)]
    public string DeviceType { get; init; } = "desktop";

    /// <summary>
    /// Browser's preferred language (e.g., "es-SV", "en-US")
    /// </summary>
    [StringLength(20)]
    public string Language { get; init; } = "en";

    /// <summary>
    /// User agent string for debugging/analytics
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; init; }

    /// <summary>
    /// Whether this context has been initialized from the browser
    /// </summary>
    public bool IsInitialized { get; init; }
}

/// <summary>
/// Location context for the profile, distinguishing selected vs device location.
/// Selected location is where the user WANTS to search.
/// Device location is where the user IS physically located.
/// </summary>
public record ProfileLocationContext
{
    /// <summary>
    /// User-selected location for searches (can be different from GPS).
    /// This is where the user WANTS to search.
    /// Set via city selection or manual input.
    /// </summary>
    public ChatLocationContext? SelectedLocation { get; init; }

    /// <summary>
    /// Actual device GPS location (if available).
    /// This is where the user IS physically located.
    /// Set via GPS/browser geolocation.
    /// </summary>
    public ChatLocationContext? DeviceLocation { get; init; }

    /// <summary>
    /// Returns the effective location for searches.
    /// Prefers SelectedLocation, falls back to DeviceLocation.
    /// </summary>
    public ChatLocationContext? EffectiveLocation => SelectedLocation ?? DeviceLocation;

    /// <summary>
    /// Whether any location is available (either selected or device)
    /// </summary>
    public bool HasLocation => EffectiveLocation?.IsValid == true;

    /// <summary>
    /// Whether user has explicitly selected a different location than device GPS
    /// </summary>
    public bool HasSelectedLocation => SelectedLocation?.IsValid == true;

    /// <summary>
    /// Whether device GPS location is available
    /// </summary>
    public bool HasDeviceLocation => DeviceLocation?.IsValid == true;
}

/// <summary>
/// Complete profile context including location, device, and time information.
/// This is the main DTO consumed by components for context-aware features.
/// Profile-centric: context changes when user switches profiles.
/// </summary>
public record ProfileContext
{
    /// <summary>
    /// The active profile ID this context belongs to
    /// </summary>
    public Guid ProfileId { get; init; }

    /// <summary>
    /// Location context (selected + device locations)
    /// </summary>
    public ProfileLocationContext Location { get; init; } = new();

    /// <summary>
    /// Device context (timezone, device type, language)
    /// </summary>
    public DeviceContext Device { get; init; } = new();

    /// <summary>
    /// When this context was last updated
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Helper to get effective timezone (from device context)
    /// </summary>
    public string TimeZone => Device.TimeZone;

    /// <summary>
    /// Helper to get current local time (from device context)
    /// </summary>
    public DateTimeOffset LocalTime => Device.LocalDateTime;

    /// <summary>
    /// Helper to check if context is fully initialized
    /// </summary>
    public bool IsInitialized => Device.IsInitialized;

    /// <summary>
    /// Creates a ChatLocationContext with populated timezone and local time.
    /// Ready to pass to ChatRequest.
    /// </summary>
    public ChatLocationContext? ToChatLocationContext()
    {
        var effectiveLocation = Location.EffectiveLocation;
        if (effectiveLocation == null)
            return null;

        return effectiveLocation with
        {
            TimeZone = Device.TimeZone,
            UserLocalTime = Device.LocalDateTime.ToString("o") // ISO 8601
        };
    }
}

/// <summary>
/// Raw device context from JavaScript interop.
/// Used internally by ProfileContextService.
/// </summary>
public record DeviceContextJs
{
    public string? TimeZone { get; init; }
    public string? LocalDateTime { get; init; }
    public int TimeZoneOffsetMinutes { get; init; }
    public string? DeviceType { get; init; }
    public string? Language { get; init; }
    public string? UserAgent { get; init; }

    /// <summary>
    /// Convert to typed DeviceContext
    /// </summary>
    public DeviceContext ToDeviceContext()
    {
        DateTimeOffset parsedDateTime = DateTimeOffset.UtcNow;
        if (!string.IsNullOrEmpty(LocalDateTime))
        {
            DateTimeOffset.TryParse(LocalDateTime, out parsedDateTime);
        }

        return new DeviceContext
        {
            TimeZone = TimeZone ?? "UTC",
            LocalDateTime = parsedDateTime,
            TimeZoneOffsetMinutes = TimeZoneOffsetMinutes,
            DeviceType = DeviceType ?? "desktop",
            Language = Language ?? "en",
            UserAgent = UserAgent,
            IsInitialized = true
        };
    }
}
