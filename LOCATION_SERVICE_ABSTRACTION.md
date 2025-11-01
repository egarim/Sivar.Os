# Location Service Abstraction Pattern
**Date:** October 31, 2025  
**Pattern:** Provider-agnostic interface with swappable implementations

---

## Architecture Overview

```
Components → ILocationService → [NominatimService | AzureMapsService | GoogleMapsService]
                                          ↑
                                   Selected via DI
```

**Key Principle:** Components **never** know which provider is being used.

---

## 1. Core Interface (Provider-Agnostic)

### Sivar.Os.Shared/Services/ILocationService.cs

```csharp
namespace Sivar.Os.Shared.Services;

/// <summary>
/// Provider-agnostic location service interface.
/// Implementations can use Nominatim, Azure Maps, Google Maps, etc.
/// </summary>
public interface ILocationService
{
    // ==================== GEOCODING ====================
    
    /// <summary>
    /// Converts an address to geographic coordinates.
    /// </summary>
    /// <param name="city">City name</param>
    /// <param name="state">State/province (optional)</param>
    /// <param name="country">Country (optional)</param>
    /// <returns>Coordinates or null if not found</returns>
    Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string city, 
        string? state = null, 
        string? country = null);
    
    /// <summary>
    /// Converts coordinates to an address.
    /// </summary>
    Task<Location?> ReverseGeocodeAsync(double latitude, double longitude);
    
    // ==================== DISTANCE ====================
    
    /// <summary>
    /// Calculates distance between two points (in kilometers).
    /// Uses PostGIS if available, falls back to Haversine formula.
    /// </summary>
    Task<double> CalculateDistanceAsync(
        double lat1, double lng1, 
        double lat2, double lng2);
    
    // ==================== SEARCH ====================
    
    /// <summary>
    /// Finds nearby profiles within radius.
    /// </summary>
    Task<List<ProfileDto>> FindNearbyProfilesAsync(
        double latitude, 
        double longitude, 
        double radiusKm, 
        int limit = 50);
    
    /// <summary>
    /// Finds nearby posts within radius.
    /// </summary>
    Task<List<PostDto>> FindNearbyPostsAsync(
        double latitude, 
        double longitude, 
        double radiusKm, 
        int page = 1, 
        int pageSize = 20);
    
    // ==================== UTILITIES ====================
    
    /// <summary>
    /// Converts lat/long to PostGIS POINT format.
    /// </summary>
    string ToPostGISPoint(double latitude, double longitude);
    
    /// <summary>
    /// Parses PostGIS POINT string to coordinates.
    /// </summary>
    (double Latitude, double Longitude)? ParsePostGISPoint(string? geoLocation);
    
    /// <summary>
    /// Validates if coordinates are within valid ranges.
    /// </summary>
    bool IsValidCoordinates(double latitude, double longitude);
    
    /// <summary>
    /// Gets the current provider name (for debugging/logging).
    /// </summary>
    string ProviderName { get; }
}
```

---

## 2. Base Implementation (Shared Logic)

### Sivar.Os/Services/LocationServiceBase.cs

```csharp
namespace Sivar.Os.Services;

/// <summary>
/// Base class with shared logic for all location service providers.
/// Handles PostGIS queries, distance calculations, etc.
/// </summary>
public abstract class LocationServiceBase : ILocationService
{
    protected readonly SivarDbContext _context;
    protected readonly ILogger _logger;
    
    protected LocationServiceBase(SivarDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }
    
    // ==================== ABSTRACT METHODS (Provider-Specific) ====================
    
    /// <summary>
    /// Provider-specific geocoding implementation.
    /// </summary>
    public abstract Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string city, string? state = null, string? country = null);
    
    /// <summary>
    /// Provider-specific reverse geocoding implementation.
    /// </summary>
    public abstract Task<Location?> ReverseGeocodeAsync(
        double latitude, double longitude);
    
    /// <summary>
    /// Provider name for logging/debugging.
    /// </summary>
    public abstract string ProviderName { get; }
    
    // ==================== SHARED METHODS (All Providers Use These) ====================
    
    public virtual async Task<double> CalculateDistanceAsync(
        double lat1, double lng1, double lat2, double lng2)
    {
        try
        {
            // Try PostGIS first (more accurate)
            var result = await _context.Database
                .SqlQuery<double>($@"
                    SELECT calculate_distance({lat1}, {lng1}, {lat2}, {lng2})
                ")
                .FirstOrDefaultAsync();
            
            return result / 1000.0; // Convert meters to km
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, 
                "PostGIS distance calculation failed, falling back to Haversine");
            
            // Fallback to Haversine formula
            return CalculateHaversineDistance(lat1, lng1, lat2, lng2);
        }
    }
    
    public virtual async Task<List<ProfileDto>> FindNearbyProfilesAsync(
        double latitude, double longitude, double radiusKm, int limit = 50)
    {
        var results = await _context.Database
            .SqlQuery<ProfileLocationResult>($@"
                SELECT * FROM find_nearby_profiles(
                    {latitude}, {longitude}, {radiusKm}, {limit}
                )
            ")
            .ToListAsync();
        
        // Map to DTOs
        var profileIds = results.Select(r => r.ProfileId).ToList();
        var profiles = await _context.Profiles
            .Where(p => profileIds.Contains(p.Id))
            .ToListAsync();
        
        return profiles.Select(p => MapToDto(p, results)).ToList();
    }
    
    public virtual async Task<List<PostDto>> FindNearbyPostsAsync(
        double latitude, double longitude, double radiusKm, 
        int page = 1, int pageSize = 20)
    {
        var results = await _context.Database
            .SqlQuery<PostLocationResult>($@"
                SELECT * FROM find_nearby_posts(
                    {latitude}, {longitude}, {radiusKm}, {pageSize * page}
                )
            ")
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        // Map to DTOs
        var postIds = results.Select(r => r.PostId).ToList();
        var posts = await _context.Posts
            .Where(p => postIds.Contains(p.Id))
            .Include(p => p.Profile)
            .ToListAsync();
        
        return posts.Select(p => MapToDto(p, results)).ToList();
    }
    
    public virtual string ToPostGISPoint(double latitude, double longitude)
    {
        if (!IsValidCoordinates(latitude, longitude))
            throw new ArgumentException("Invalid coordinates");
        
        // PostGIS format: POINT(longitude latitude)
        // Note: Longitude first, then latitude!
        return $"POINT({longitude} {latitude})";
    }
    
    public virtual (double Latitude, double Longitude)? ParsePostGISPoint(
        string? geoLocation)
    {
        if (string.IsNullOrWhiteSpace(geoLocation))
            return null;
        
        try
        {
            // Format: "POINT(-74.0060 40.7128)"
            var coords = geoLocation
                .Replace("POINT(", "")
                .Replace(")", "")
                .Split(' ');
            
            if (coords.Length == 2 &&
                double.TryParse(coords[0], out var lng) &&
                double.TryParse(coords[1], out var lat))
            {
                return (lat, lng);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse PostGIS point: {GeoLocation}", 
                geoLocation);
        }
        
        return null;
    }
    
    public virtual bool IsValidCoordinates(double latitude, double longitude)
    {
        return latitude >= -90 && latitude <= 90 &&
               longitude >= -180 && longitude <= 180;
    }
    
    // ==================== HELPER METHODS ====================
    
    /// <summary>
    /// Haversine formula for distance calculation (fallback).
    /// </summary>
    protected double CalculateHaversineDistance(
        double lat1, double lng1, double lat2, double lng2)
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
    
    protected double ToRadians(double degrees) => degrees * Math.PI / 180.0;
    
    protected ProfileDto MapToDto(Profile profile, List<ProfileLocationResult> results)
    {
        var result = results.FirstOrDefault(r => r.ProfileId == profile.Id);
        
        return new ProfileDto
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            Handle = profile.Handle,
            Location = profile.Location,
            DistanceKm = result?.DistanceKm
        };
    }
    
    protected PostDto MapToDto(Post post, List<PostLocationResult> results)
    {
        var result = results.FirstOrDefault(r => r.PostId == post.Id);
        
        return new PostDto
        {
            Id = post.Id,
            Content = post.Content,
            Location = post.Location,
            DistanceKm = result?.DistanceKm,
            ProfileDisplayName = post.Profile.DisplayName
        };
    }
}

// Helper classes for raw SQL results
public class ProfileLocationResult
{
    public Guid ProfileId { get; set; }
    public double DistanceKm { get; set; }
}

public class PostLocationResult
{
    public Guid PostId { get; set; }
    public double DistanceKm { get; set; }
}
```

---

## 3. Provider Implementations

### 3.1 Nominatim Provider (FREE)

### Sivar.Os/Services/Providers/NominatimLocationService.cs

```csharp
namespace Sivar.Os.Services.Providers;

/// <summary>
/// Location service using Nominatim (OpenStreetMap).
/// FREE, no API key required, rate limited to 1 req/sec.
/// </summary>
public class NominatimLocationService : LocationServiceBase
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly NominatimOptions _options;
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static DateTime _lastRequest = DateTime.MinValue;
    
    public NominatimLocationService(
        SivarDbContext context,
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<NominatimOptions> options,
        ILogger<NominatimLocationService> logger)
        : base(context, logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        
        // Set required User-Agent header
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);
    }
    
    public override string ProviderName => "Nominatim (OpenStreetMap)";
    
    public override async Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string city, string? state = null, string? country = null)
    {
        var cacheKey = $"geocode:{city}:{state}:{country}";
        
        // Check cache first
        if (_cache.TryGetValue<(double, double)>(cacheKey, out var cached))
        {
            _logger.LogDebug("Geocode cache hit: {CacheKey}", cacheKey);
            return cached;
        }
        
        // Build query
        var query = city;
        if (!string.IsNullOrWhiteSpace(state)) query += $", {state}";
        if (!string.IsNullOrWhiteSpace(country)) query += $", {country}";
        
        // Rate limit: 1 request per second
        await EnforceRateLimit();
        
        try
        {
            var url = $"{_options.BaseUrl}/search?q={Uri.EscapeDataString(query)}&format=json&limit=1";
            
            _logger.LogInformation("Geocoding: {Query} via {Provider}", 
                query, ProviderName);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var results = await response.Content
                .ReadFromJsonAsync<List<NominatimSearchResult>>();
            
            if (results?.Any() == true)
            {
                var result = results[0];
                var coords = (result.Lat, result.Lon);
                
                // Cache for 30 days
                _cache.Set(cacheKey, coords, TimeSpan.FromDays(30));
                
                return coords;
            }
            
            _logger.LogWarning("No geocoding results for: {Query}", query);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Geocoding failed for: {Query}", query);
            return null;
        }
    }
    
    public override async Task<Location?> ReverseGeocodeAsync(
        double latitude, double longitude)
    {
        if (!IsValidCoordinates(latitude, longitude))
            return null;
        
        var cacheKey = $"reverse:{latitude:F6}:{longitude:F6}";
        
        // Check cache
        if (_cache.TryGetValue<Location>(cacheKey, out var cached))
        {
            _logger.LogDebug("Reverse geocode cache hit: {CacheKey}", cacheKey);
            return cached;
        }
        
        await EnforceRateLimit();
        
        try
        {
            var url = $"{_options.BaseUrl}/reverse?lat={latitude}&lon={longitude}&format=json";
            
            _logger.LogInformation("Reverse geocoding: ({Lat}, {Lng}) via {Provider}", 
                latitude, longitude, ProviderName);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content
                .ReadFromJsonAsync<NominatimReverseResult>();
            
            if (result?.Address != null)
            {
                var location = new Location(
                    result.Address.City ?? result.Address.Town ?? result.Address.Village ?? "",
                    result.Address.State ?? "",
                    result.Address.Country ?? "",
                    latitude,
                    longitude
                );
                
                // Cache for 30 days
                _cache.Set(cacheKey, location, TimeSpan.FromDays(30));
                
                return location;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reverse geocoding failed for: ({Lat}, {Lng})", 
                latitude, longitude);
            return null;
        }
    }
    
    private async Task EnforceRateLimit()
    {
        await _rateLimiter.WaitAsync();
        try
        {
            var elapsed = DateTime.UtcNow - _lastRequest;
            var minDelay = TimeSpan.FromSeconds(1.0 / _options.RateLimitPerSecond);
            
            if (elapsed < minDelay)
            {
                var delay = minDelay - elapsed;
                await Task.Delay(delay);
            }
            
            _lastRequest = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }
}

// Configuration
public class NominatimOptions
{
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";
    public string UserAgent { get; set; } = "Sivar.Os/1.0";
    public double RateLimitPerSecond { get; set; } = 1.0;
}

// Response models
public class NominatimSearchResult
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }
    
    [JsonPropertyName("lon")]
    public double Lon { get; set; }
    
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}

public class NominatimReverseResult
{
    [JsonPropertyName("address")]
    public NominatimAddress? Address { get; set; }
}

public class NominatimAddress
{
    [JsonPropertyName("city")]
    public string? City { get; set; }
    
    [JsonPropertyName("town")]
    public string? Town { get; set; }
    
    [JsonPropertyName("village")]
    public string? Village { get; set; }
    
    [JsonPropertyName("state")]
    public string? State { get; set; }
    
    [JsonPropertyName("country")]
    public string? Country { get; set; }
}
```

---

### 3.2 Azure Maps Provider (PAID - Optional)

### Sivar.Os/Services/Providers/AzureMapsLocationService.cs

```csharp
namespace Sivar.Os.Services.Providers;

/// <summary>
/// Location service using Azure Maps.
/// Requires subscription key, 1000 free requests/month.
/// </summary>
public class AzureMapsLocationService : LocationServiceBase
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly AzureMapsOptions _options;
    
    public AzureMapsLocationService(
        SivarDbContext context,
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<AzureMapsOptions> options,
        ILogger<AzureMapsLocationService> logger)
        : base(context, logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
    }
    
    public override string ProviderName => "Azure Maps";
    
    public override async Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string city, string? state = null, string? country = null)
    {
        var cacheKey = $"geocode:{city}:{state}:{country}";
        
        if (_cache.TryGetValue<(double, double)>(cacheKey, out var cached))
            return cached;
        
        var query = city;
        if (!string.IsNullOrWhiteSpace(state)) query += $", {state}";
        if (!string.IsNullOrWhiteSpace(country)) query += $", {country}";
        
        try
        {
            var url = $"https://atlas.microsoft.com/search/address/json" +
                      $"?api-version=1.0&subscription-key={_options.SubscriptionKey}" +
                      $"&query={Uri.EscapeDataString(query)}";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content
                .ReadFromJsonAsync<AzureMapsSearchResponse>();
            
            if (result?.Results?.Any() == true)
            {
                var first = result.Results[0];
                var coords = (first.Position.Lat, first.Position.Lon);
                
                _cache.Set(cacheKey, coords, TimeSpan.FromDays(30));
                return coords;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Maps geocoding failed");
            return null;
        }
    }
    
    public override async Task<Location?> ReverseGeocodeAsync(
        double latitude, double longitude)
    {
        // Similar implementation using Azure Maps reverse geocoding API
        // Left as exercise - follows same pattern as Nominatim
        throw new NotImplementedException();
    }
}

public class AzureMapsOptions
{
    public string SubscriptionKey { get; set; } = string.Empty;
}

// Response models omitted for brevity
```

---

## 4. Configuration-Based Provider Selection

### appsettings.json

```json
{
  "LocationServices": {
    "Provider": "Nominatim",  // ← Change here to switch providers
    
    "Nominatim": {
      "BaseUrl": "https://nominatim.openstreetmap.org",
      "UserAgent": "Sivar.Os/1.0 (contact@example.com)",
      "RateLimitPerSecond": 1.0
    },
    
    "AzureMaps": {
      "SubscriptionKey": ""  // Only needed if using Azure Maps
    }
  }
}
```

### Program.cs (Dependency Injection)

```csharp
// Read configuration
var locationConfig = builder.Configuration
    .GetSection("LocationServices")
    .Get<LocationServicesConfig>();

// Register HttpClient for location services
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

// Register the selected provider
switch (locationConfig?.Provider?.ToLower())
{
    case "azuremaps":
        builder.Services.AddScoped<ILocationService, AzureMapsLocationService>();
        builder.Services.Configure<AzureMapsOptions>(
            builder.Configuration.GetSection("LocationServices:AzureMaps"));
        break;
    
    case "nominatim":
    default:
        builder.Services.AddScoped<ILocationService, NominatimLocationService>();
        builder.Services.Configure<NominatimOptions>(
            builder.Configuration.GetSection("LocationServices:Nominatim"));
        break;
}

public class LocationServicesConfig
{
    public string Provider { get; set; } = "Nominatim";
}
```

---

## 5. Usage in Components (Provider-Agnostic)

### CreatePost.razor

```razor
@inject ILocationService LocationService  @* ← NO provider knowledge! *@

<LocationPicker SelectedLocation="selectedLocation"
                OnLocationSelected="OnLocationSelected" />

@code {
    private Location? selectedLocation;
    
    private async Task OnLocationSelected(Location location)
    {
        // Component doesn't know if this is Nominatim or Azure Maps!
        selectedLocation = location;
        
        // Optionally reverse geocode to get full address
        if (location.Latitude.HasValue && location.Longitude.HasValue)
        {
            var fullLocation = await LocationService.ReverseGeocodeAsync(
                location.Latitude.Value,
                location.Longitude.Value);
            
            if (fullLocation != null)
                selectedLocation = fullLocation;
        }
    }
}
```

---

## 6. Testing with Mock Provider

### Test/MockLocationService.cs

```csharp
public class MockLocationService : ILocationService
{
    public string ProviderName => "Mock (Testing)";
    
    public Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string city, string? state = null, string? country = null)
    {
        // Return predictable test data
        return Task.FromResult<(double, double)?>((40.7128, -74.0060));
    }
    
    public Task<Location?> ReverseGeocodeAsync(double latitude, double longitude)
    {
        return Task.FromResult<Location?>(
            new Location("Test City", "Test State", "Test Country", latitude, longitude)
        );
    }
    
    // ... implement other methods with test data
}

// In test
builder.Services.AddScoped<ILocationService, MockLocationService>();
```

---

## 7. Benefits Summary

| Benefit | Description |
|---------|-------------|
| **No Hardcoding** | Components use `ILocationService`, not specific providers |
| **Easy Switching** | Change provider in `appsettings.json`, no code changes |
| **Testing** | Mock interface in tests, no real API calls |
| **Future-Proof** | Add Google Maps, Mapbox, etc. without touching existing code |
| **Fallback** | Can implement fallback: try Azure Maps, fall back to Nominatim |
| **Logging** | Centralized logging via `ProviderName` property |

---

## 8. Future: Multi-Provider Fallback

```csharp
public class FallbackLocationService : ILocationService
{
    private readonly IEnumerable<ILocationService> _providers;
    
    public async Task<(double Latitude, double Longitude)?> GeocodeAsync(...)
    {
        foreach (var provider in _providers)
        {
            try
            {
                var result = await provider.GeocodeAsync(...);
                if (result.HasValue)
                {
                    _logger.LogInformation("Geocoding succeeded with {Provider}", 
                        provider.ProviderName);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Provider} failed, trying next", 
                    provider.ProviderName);
            }
        }
        
        return null; // All providers failed
    }
}
```

---

## Summary

✅ **YES** - You absolutely should (and the plan does) use `ILocationService` abstraction  
✅ **NO hardcoding** - Components only know about the interface  
✅ **Easy switching** - Change provider in config, not code  
✅ **Best practice** - Industry-standard dependency injection pattern  

**Your components will look like:**
```csharp
@inject ILocationService LocationService  // ← That's it!
```

**Never:**
```csharp
@inject NominatimService NominatimService  // ❌ Don't do this!
```

This is the **correct architecture** and it's already planned in the implementation guide! 🎯
