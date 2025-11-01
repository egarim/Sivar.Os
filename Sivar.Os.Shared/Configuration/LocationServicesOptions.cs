namespace Sivar.Os.Shared.Configuration;

/// <summary>
/// Configuration options for location services
/// </summary>
public class LocationServicesOptions
{
    /// <summary>
    /// Selected location provider: "Nominatim", "AzureMaps", "GoogleMaps"
    /// </summary>
    public string Provider { get; set; } = "Nominatim";

    /// <summary>
    /// Nominatim-specific configuration
    /// </summary>
    public NominatimOptions Nominatim { get; set; } = new();

    /// <summary>
    /// Azure Maps-specific configuration (for future implementation)
    /// </summary>
    public AzureMapsOptions? AzureMaps { get; set; }

    /// <summary>
    /// Google Maps-specific configuration (for future implementation)
    /// </summary>
    public GoogleMapsOptions? GoogleMaps { get; set; }
}

/// <summary>
/// Nominatim (OpenStreetMap) geocoding service options
/// FREE service with 1 request/second rate limit
/// </summary>
public class NominatimOptions
{
    /// <summary>
    /// Base URL for Nominatim API
    /// </summary>
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>
    /// User-Agent header (REQUIRED by Nominatim usage policy)
    /// Should identify your application
    /// </summary>
    public string UserAgent { get; set; } = "Sivar.Os/1.0 (https://sivar.os)";

    /// <summary>
    /// Maximum requests per second (default 1 for free tier)
    /// </summary>
    public double RateLimitPerSecond { get; set; } = 1.0;

    /// <summary>
    /// Cache duration for geocoding results (default 30 days)
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromDays(30);
}

/// <summary>
/// Azure Maps geocoding service options (for future implementation)
/// </summary>
public class AzureMapsOptions
{
    /// <summary>
    /// Azure Maps subscription key
    /// </summary>
    public string? SubscriptionKey { get; set; }

    /// <summary>
    /// Base URL for Azure Maps API
    /// </summary>
    public string BaseUrl { get; set; } = "https://atlas.microsoft.com";

    /// <summary>
    /// Maximum requests per second
    /// </summary>
    public double RateLimitPerSecond { get; set; } = 50.0;

    /// <summary>
    /// Cache duration for geocoding results
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromDays(30);
}

/// <summary>
/// Google Maps geocoding service options (for future implementation)
/// </summary>
public class GoogleMapsOptions
{
    /// <summary>
    /// Google Maps API key
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL for Google Maps API
    /// </summary>
    public string BaseUrl { get; set; } = "https://maps.googleapis.com";

    /// <summary>
    /// Maximum requests per second
    /// </summary>
    public double RateLimitPerSecond { get; set; } = 50.0;

    /// <summary>
    /// Cache duration for geocoding results
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromDays(30);
}
