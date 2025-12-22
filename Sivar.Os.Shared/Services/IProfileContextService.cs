using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service for managing the active profile's context (location, device, time).
/// Profile-centric: context changes when user switches profiles.
/// 
/// Key concepts:
/// - SelectedLocation: Where user WANTS to search (user choice)
/// - DeviceLocation: Where user IS physically (GPS)
/// - DeviceContext: Timezone, device type, language (auto-detected)
/// </summary>
public interface IProfileContextService
{
    /// <summary>
    /// Current profile context (location + device + time).
    /// Null if not initialized.
    /// </summary>
    ProfileContext? CurrentContext { get; }

    /// <summary>
    /// Event fired when context changes (profile switch, location change, etc.)
    /// </summary>
    event Func<ProfileContext?, Task>? OnContextChanged;

    /// <summary>
    /// Initialize the service for a specific profile.
    /// Detects device context and loads saved location from storage.
    /// Should be called when profile becomes active.
    /// </summary>
    /// <param name="profileId">The active profile ID</param>
    Task InitializeAsync(Guid profileId);

    /// <summary>
    /// Refresh device context (timezone, device type, etc.).
    /// Call this if user changes system settings or on page visibility change.
    /// </summary>
    Task RefreshDeviceContextAsync();

    /// <summary>
    /// Request GPS location from browser and set as device location.
    /// Requires user permission.
    /// </summary>
    /// <returns>True if location was obtained, false if denied or failed</returns>
    Task<bool> RequestDeviceLocationAsync();

    /// <summary>
    /// Set user-selected location (different from GPS).
    /// Used when user wants to search in a different area.
    /// </summary>
    /// <param name="location">The selected location</param>
    Task SetSelectedLocationAsync(ChatLocationContext location);

    /// <summary>
    /// Clear user-selected location (revert to device location for searches).
    /// </summary>
    Task ClearSelectedLocationAsync();

    /// <summary>
    /// Get ChatLocationContext with populated timezone and local time.
    /// Ready to pass to ChatRequest.
    /// </summary>
    /// <returns>Location context with timezone, or null if no location</returns>
    ChatLocationContext? GetChatLocationContext();

    /// <summary>
    /// Handle profile switch - reload context for new profile.
    /// Clears selected location and reloads from storage.
    /// </summary>
    /// <param name="newProfileId">The new active profile ID</param>
    Task OnProfileSwitchedAsync(Guid newProfileId);

    /// <summary>
    /// Get the device timezone (even if location is not set).
    /// Useful for schedule/booking default timezone.
    /// </summary>
    /// <returns>IANA timezone identifier (e.g., "America/El_Salvador")</returns>
    string GetDeviceTimeZone();

    /// <summary>
    /// Get the device's current local time.
    /// Useful for "today", "tomorrow" calculations.
    /// </summary>
    /// <returns>Current local time with timezone offset</returns>
    DateTimeOffset GetLocalDateTime();

    /// <summary>
    /// Get the device type (mobile, tablet, desktop).
    /// Useful for responsive UI decisions.
    /// </summary>
    /// <returns>Device type string</returns>
    string GetDeviceType();
}
