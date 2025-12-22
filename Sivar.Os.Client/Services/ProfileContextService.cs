using Microsoft.JSInterop;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Client.Services;

/// <summary>
/// Service for managing the active profile's context (location, device, time).
/// Profile-centric: context changes when user switches profiles.
/// 
/// Integrates with ChatLocationService for GPS/city location functionality.
/// Adds device context detection (timezone, device type, language) via JS interop.
/// </summary>
public class ProfileContextService : IProfileContextService, IAsyncDisposable
{
    private readonly ChatLocationService _locationService;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ProfileContextService> _logger;

    private ProfileContext? _currentContext;
    private IJSObjectReference? _module;
    private bool _isInitialized;
    private Guid _currentProfileId;

    private const string SelectedLocationStorageKeyPrefix = "sivar_profile_selected_location_";

    /// <inheritdoc />
    public ProfileContext? CurrentContext => _currentContext;

    /// <inheritdoc />
    public event Func<ProfileContext?, Task>? OnContextChanged;

    public ProfileContextService(
        ChatLocationService locationService,
        IJSRuntime jsRuntime,
        ILogger<ProfileContextService> logger)
    {
        _locationService = locationService;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Get the JS module for context interop
    /// </summary>
    private async Task<IJSObjectReference> GetModuleAsync()
    {
        if (_module == null)
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/context-interop.js");
        }
        return _module;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(Guid profileId)
    {
        if (_isInitialized && _currentProfileId == profileId)
        {
            _logger.LogDebug("[ProfileContextService] Already initialized for profile {ProfileId}", profileId);
            return;
        }

        _logger.LogInformation("[ProfileContextService] Initializing for profile {ProfileId}", profileId);
        _currentProfileId = profileId;

        try
        {
            // 1. Detect device context (timezone, device type, etc.)
            var deviceContext = await DetectDeviceContextAsync();

            // 2. Initialize ChatLocationService (loads saved location)
            await _locationService.InitializeAsync();

            // 3. Load selected location for this profile (if any)
            var selectedLocation = await LoadSelectedLocationAsync(profileId);

            // 4. Build profile context
            _currentContext = new ProfileContext
            {
                ProfileId = profileId,
                Device = deviceContext,
                Location = new ProfileLocationContext
                {
                    SelectedLocation = selectedLocation,
                    DeviceLocation = _locationService.CurrentLocation
                },
                LastUpdated = DateTime.UtcNow
            };

            _isInitialized = true;
            _logger.LogInformation(
                "[ProfileContextService] Initialized - TimeZone={TimeZone}, DeviceType={DeviceType}, HasLocation={HasLocation}",
                deviceContext.TimeZone, deviceContext.DeviceType, _currentContext.Location.HasLocation);

            // Subscribe to location changes from ChatLocationService
            _locationService.OnLocationChanged += OnLocationServiceChangedAsync;

            await NotifyContextChangedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProfileContextService] Failed to initialize for profile {ProfileId}", profileId);
            
            // Create minimal context even on error
            _currentContext = new ProfileContext
            {
                ProfileId = profileId,
                Device = new DeviceContext { TimeZone = "America/El_Salvador", IsInitialized = false },
                Location = new ProfileLocationContext(),
                LastUpdated = DateTime.UtcNow
            };
            _isInitialized = true;
        }
    }

    /// <inheritdoc />
    public async Task RefreshDeviceContextAsync()
    {
        if (_currentContext == null)
        {
            _logger.LogWarning("[ProfileContextService] Cannot refresh - not initialized");
            return;
        }

        try
        {
            var deviceContext = await DetectDeviceContextAsync();
            
            _currentContext = _currentContext with
            {
                Device = deviceContext,
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogDebug("[ProfileContextService] Device context refreshed - TimeZone={TimeZone}", 
                deviceContext.TimeZone);

            await NotifyContextChangedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProfileContextService] Failed to refresh device context");
        }
    }

    /// <inheritdoc />
    public async Task<bool> RequestDeviceLocationAsync()
    {
        try
        {
            _logger.LogInformation("[ProfileContextService] Requesting device GPS location");

            var location = await _locationService.RequestGpsLocationAsync();
            if (location == null)
            {
                _logger.LogWarning("[ProfileContextService] GPS location denied or failed");
                return false;
            }

            // Update context with device location
            if (_currentContext != null)
            {
                _currentContext = _currentContext with
                {
                    Location = _currentContext.Location with
                    {
                        DeviceLocation = location
                    },
                    LastUpdated = DateTime.UtcNow
                };

                await NotifyContextChangedAsync();
            }

            _logger.LogInformation("[ProfileContextService] Device location set: {DisplayName}", 
                location.DisplayName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProfileContextService] Failed to request device location");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task SetSelectedLocationAsync(ChatLocationContext location)
    {
        if (_currentContext == null)
        {
            _logger.LogWarning("[ProfileContextService] Cannot set selected location - not initialized");
            return;
        }

        try
        {
            // Mark as selected source
            var selectedLocation = location with { Source = "selected" };

            _currentContext = _currentContext with
            {
                Location = _currentContext.Location with
                {
                    SelectedLocation = selectedLocation
                },
                LastUpdated = DateTime.UtcNow
            };

            // Persist for this profile
            await SaveSelectedLocationAsync(_currentProfileId, selectedLocation);

            // Also update ChatLocationService for compatibility
            await _locationService.SetLocationAsync(selectedLocation);

            _logger.LogInformation("[ProfileContextService] Selected location set: {DisplayName}", 
                location.DisplayName);

            await NotifyContextChangedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProfileContextService] Failed to set selected location");
        }
    }

    /// <inheritdoc />
    public async Task ClearSelectedLocationAsync()
    {
        if (_currentContext == null)
        {
            return;
        }

        try
        {
            _currentContext = _currentContext with
            {
                Location = _currentContext.Location with
                {
                    SelectedLocation = null
                },
                LastUpdated = DateTime.UtcNow
            };

            // Clear from storage
            await ClearSelectedLocationAsync(_currentProfileId);

            _logger.LogInformation("[ProfileContextService] Selected location cleared, reverting to device location");

            await NotifyContextChangedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProfileContextService] Failed to clear selected location");
        }
    }

    /// <inheritdoc />
    public ChatLocationContext? GetChatLocationContext()
    {
        return _currentContext?.ToChatLocationContext();
    }

    /// <inheritdoc />
    public async Task OnProfileSwitchedAsync(Guid newProfileId)
    {
        if (newProfileId == _currentProfileId)
        {
            return;
        }

        _logger.LogInformation("[ProfileContextService] Profile switched from {OldProfile} to {NewProfile}",
            _currentProfileId, newProfileId);

        // Unsubscribe from old events
        _locationService.OnLocationChanged -= OnLocationServiceChangedAsync;

        // Re-initialize for new profile
        _isInitialized = false;
        await InitializeAsync(newProfileId);
    }

    /// <inheritdoc />
    public string GetDeviceTimeZone()
    {
        return _currentContext?.Device.TimeZone ?? "America/El_Salvador";
    }

    /// <inheritdoc />
    public DateTimeOffset GetLocalDateTime()
    {
        return _currentContext?.Device.LocalDateTime ?? DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public string GetDeviceType()
    {
        return _currentContext?.Device.DeviceType ?? "desktop";
    }

    // ==================== Private Methods ====================

    /// <summary>
    /// Detect device context from browser via JS interop
    /// </summary>
    private async Task<DeviceContext> DetectDeviceContextAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            var jsContext = await module.InvokeAsync<DeviceContextJs>("getDeviceContext");
            
            return jsContext.ToDeviceContext();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ProfileContextService] Failed to detect device context, using defaults");
            return new DeviceContext
            {
                TimeZone = "America/El_Salvador",
                LocalDateTime = DateTimeOffset.UtcNow,
                DeviceType = "desktop",
                Language = "es",
                IsInitialized = false
            };
        }
    }

    /// <summary>
    /// Handle location changes from ChatLocationService
    /// </summary>
    private async Task OnLocationServiceChangedAsync(ChatLocationContext? location)
    {
        if (_currentContext == null)
        {
            return;
        }

        // Determine if this is a device location or selected location based on source
        if (location?.Source == "gps")
        {
            _currentContext = _currentContext with
            {
                Location = _currentContext.Location with
                {
                    DeviceLocation = location
                },
                LastUpdated = DateTime.UtcNow
            };
        }
        else if (location != null)
        {
            _currentContext = _currentContext with
            {
                Location = _currentContext.Location with
                {
                    SelectedLocation = location
                },
                LastUpdated = DateTime.UtcNow
            };
        }

        await NotifyContextChangedAsync();
    }

    /// <summary>
    /// Load selected location for a profile from localStorage
    /// </summary>
    private async Task<ChatLocationContext?> LoadSelectedLocationAsync(Guid profileId)
    {
        try
        {
            var key = $"{SelectedLocationStorageKeyPrefix}{profileId}";
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return System.Text.Json.JsonSerializer.Deserialize<ChatLocationContext>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ProfileContextService] Failed to load selected location for profile {ProfileId}", profileId);
            return null;
        }
    }

    /// <summary>
    /// Save selected location for a profile to localStorage
    /// </summary>
    private async Task SaveSelectedLocationAsync(Guid profileId, ChatLocationContext location)
    {
        try
        {
            var key = $"{SelectedLocationStorageKeyPrefix}{profileId}";
            var json = System.Text.Json.JsonSerializer.Serialize(location);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ProfileContextService] Failed to save selected location for profile {ProfileId}", profileId);
        }
    }

    /// <summary>
    /// Clear selected location for a profile from localStorage
    /// </summary>
    private async Task ClearSelectedLocationAsync(Guid profileId)
    {
        try
        {
            var key = $"{SelectedLocationStorageKeyPrefix}{profileId}";
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ProfileContextService] Failed to clear selected location for profile {ProfileId}", profileId);
        }
    }

    /// <summary>
    /// Notify listeners that context has changed
    /// </summary>
    private async Task NotifyContextChangedAsync()
    {
        if (OnContextChanged != null)
        {
            try
            {
                await OnContextChanged.Invoke(_currentContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProfileContextService] Error in OnContextChanged handler");
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _locationService.OnLocationChanged -= OnLocationServiceChangedAsync;

        if (_module != null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Ignore - circuit disconnected
            }
        }
    }
}
