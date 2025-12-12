using Microsoft.JSInterop;
using Sivar.Os.Shared.DTOs;
using System.Net.Http.Json;

namespace Sivar.Os.Client.Services;

/// <summary>
/// Service for managing chat location context.
/// Handles location detection, storage, and retrieval for proximity-aware searches.
/// Phase 0: Location-Aware Chat
/// </summary>
public class ChatLocationService : IAsyncDisposable
{
    private readonly BrowserPermissionsService _permissionsService;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ChatLocationService> _logger;
    
    private ChatLocationContext? _currentLocation;
    private bool _isInitialized;
    private const string StorageKey = "sivar_chat_location";

    /// <summary>
    /// Event fired when location changes
    /// </summary>
    public event Func<ChatLocationContext?, Task>? OnLocationChanged;

    /// <summary>
    /// Pre-defined cities in El Salvador for quick selection
    /// </summary>
    public static readonly List<CityOption> SalvadoranCities = new()
    {
        new("San Salvador", "San Salvador", "El Salvador", 13.6929, -89.2182),
        new("Santa Ana", "Santa Ana", "El Salvador", 13.9946, -89.5597),
        new("San Miguel", "San Miguel", "El Salvador", 13.4833, -88.1833),
        new("Santa Tecla", "La Libertad", "El Salvador", 13.6769, -89.2797),
        new("Soyapango", "San Salvador", "El Salvador", 13.7167, -89.1500),
        new("Mejicanos", "San Salvador", "El Salvador", 13.7403, -89.2131),
        new("Apopa", "San Salvador", "El Salvador", 13.8000, -89.1833),
        new("Antiguo Cuscatlán", "La Libertad", "El Salvador", 13.6667, -89.2500),
        new("La Libertad", "La Libertad", "El Salvador", 13.4883, -89.3228),
        new("Suchitoto", "Cuscatlán", "El Salvador", 13.9333, -89.0333),
        new("Chalatenango", "Chalatenango", "El Salvador", 14.0333, -88.9333),
        new("Usulután", "Usulután", "El Salvador", 13.3500, -88.4333),
        new("Ahuachapán", "Ahuachapán", "El Salvador", 13.9167, -89.8500),
        new("San Vicente", "San Vicente", "El Salvador", 13.6333, -88.7833),
        new("Zacatecoluca", "La Paz", "El Salvador", 13.5000, -88.8667),
    };

    public ChatLocationService(
        BrowserPermissionsService permissionsService,
        IJSRuntime jsRuntime,
        ILogger<ChatLocationService> logger)
    {
        _permissionsService = permissionsService;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current location context
    /// </summary>
    public ChatLocationContext? CurrentLocation => _currentLocation;

    /// <summary>
    /// Whether a valid location is set
    /// </summary>
    public bool HasLocation => _currentLocation?.IsValid == true;

    /// <summary>
    /// Initialize the service and load saved location from localStorage
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            // Try to load saved location from localStorage
            var savedLocation = await LoadFromStorageAsync();
            if (savedLocation != null)
            {
                _currentLocation = savedLocation;
                _logger.LogInformation("[ChatLocationService] Loaded saved location: {DisplayName}", 
                    savedLocation.DisplayName);
            }
            
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatLocationService] Failed to initialize");
            _isInitialized = true; // Mark as initialized even on error
        }
    }

    /// <summary>
    /// Request location from browser GPS
    /// </summary>
    /// <returns>Location context if successful, null if denied or failed</returns>
    public async Task<ChatLocationContext?> RequestGpsLocationAsync()
    {
        try
        {
            _logger.LogInformation("[ChatLocationService] Requesting GPS location...");
            
            var position = await _permissionsService.RequestLocationAsync();
            if (position == null)
            {
                _logger.LogWarning("[ChatLocationService] GPS location denied or failed");
                return null;
            }

            // Reverse geocode to get city name
            var location = await ReverseGeocodeAsync(position.Latitude, position.Longitude);
            
            if (location == null)
            {
                // Fallback: create location with just coordinates
                location = new ChatLocationContext
                {
                    Latitude = position.Latitude,
                    Longitude = position.Longitude,
                    AccuracyMeters = position.Accuracy,
                    Source = "gps",
                    DisplayName = $"{position.Latitude:F4}, {position.Longitude:F4}"
                };
            }
            else
            {
                // Add GPS-specific fields
                location = location with
                {
                    Latitude = position.Latitude,
                    Longitude = position.Longitude,
                    AccuracyMeters = position.Accuracy,
                    Source = "gps"
                };
            }

            await SetLocationAsync(location);
            return location;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatLocationService] GPS location request failed");
            return null;
        }
    }

    /// <summary>
    /// Set location from a pre-defined city selection
    /// </summary>
    public async Task<ChatLocationContext> SetCityLocationAsync(CityOption city)
    {
        var location = new ChatLocationContext
        {
            Latitude = city.Latitude,
            Longitude = city.Longitude,
            City = city.Name,
            State = city.State,
            Country = city.Country,
            DisplayName = $"{city.Name}, {city.State}",
            Source = "selected"
        };

        await SetLocationAsync(location);
        return location;
    }

    /// <summary>
    /// Set location manually
    /// </summary>
    public async Task SetLocationAsync(ChatLocationContext location)
    {
        _currentLocation = location;
        await SaveToStorageAsync(location);
        
        _logger.LogInformation("[ChatLocationService] Location set: {DisplayName} (Source: {Source})", 
            location.DisplayName, location.Source);

        // Notify listeners
        if (OnLocationChanged != null)
        {
            await OnLocationChanged.Invoke(location);
        }
    }

    /// <summary>
    /// Clear the current location
    /// </summary>
    public async Task ClearLocationAsync()
    {
        _currentLocation = null;
        await ClearStorageAsync();
        
        _logger.LogInformation("[ChatLocationService] Location cleared");

        if (OnLocationChanged != null)
        {
            await OnLocationChanged.Invoke(null);
        }
    }

    /// <summary>
    /// Check GPS permission status
    /// </summary>
    public async Task<PermissionStatus> GetGpsPermissionStatusAsync()
    {
        return await _permissionsService.GetLocationPermissionStatusAsync();
    }

    /// <summary>
    /// Attempt reverse geocoding to get city/state from coordinates
    /// Uses Nominatim (OpenStreetMap) as fallback
    /// </summary>
    private async Task<ChatLocationContext?> ReverseGeocodeAsync(double latitude, double longitude)
    {
        try
        {
            // First, try to match against known Salvadoran cities (fast, no API call)
            var nearestCity = FindNearestKnownCity(latitude, longitude, maxDistanceKm: 20);
            if (nearestCity != null)
            {
                return new ChatLocationContext
                {
                    City = nearestCity.Name,
                    State = nearestCity.State,
                    Country = nearestCity.Country,
                    DisplayName = $"{nearestCity.Name}, {nearestCity.State}",
                    Source = "detected"
                };
            }

            // Fallback: use Nominatim API for reverse geocoding
            // Note: In production, consider using a cached/proxied version
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "SivarOs/1.0");
            
            var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude}&lon={longitude}&zoom=14";
            var response = await httpClient.GetFromJsonAsync<NominatimResponse>(url);
            
            if (response?.Address != null)
            {
                return new ChatLocationContext
                {
                    City = response.Address.City ?? response.Address.Town ?? response.Address.Village,
                    State = response.Address.State,
                    Country = response.Address.Country,
                    DisplayName = response.DisplayName?.Split(',').FirstOrDefault() ?? "Unknown",
                    Source = "detected"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChatLocationService] Reverse geocoding failed");
        }

        return null;
    }

    /// <summary>
    /// Find the nearest known city within a maximum distance
    /// </summary>
    private CityOption? FindNearestKnownCity(double lat, double lng, double maxDistanceKm)
    {
        CityOption? nearest = null;
        double minDistance = double.MaxValue;

        foreach (var city in SalvadoranCities)
        {
            var distance = CalculateDistanceKm(lat, lng, city.Latitude, city.Longitude);
            if (distance < minDistance && distance <= maxDistanceKm)
            {
                minDistance = distance;
                nearest = city;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Calculate distance between two points using Haversine formula
    /// </summary>
    private static double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; // Earth's radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private async Task SaveToStorageAsync(ChatLocationContext location)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(location);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChatLocationService] Failed to save location to localStorage");
        }
    }

    public async Task<ChatLocationContext?> LoadFromStorageAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json)) return null;
            
            return System.Text.Json.JsonSerializer.Deserialize<ChatLocationContext>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChatLocationService] Failed to load location from localStorage");
            return null;
        }
    }

    private async Task ClearStorageAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChatLocationService] Failed to clear location from localStorage");
        }
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Pre-defined city option for quick selection
/// </summary>
public record CityOption(string Name, string State, string Country, double Latitude, double Longitude)
{
    public string DisplayName => $"{Name}, {State}";
}

/// <summary>
/// Nominatim API response for reverse geocoding
/// </summary>
internal class NominatimResponse
{
    public string? DisplayName { get; set; }
    public NominatimAddress? Address { get; set; }
}

internal class NominatimAddress
{
    public string? City { get; set; }
    public string? Town { get; set; }
    public string? Village { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
}
