using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sivar.Os.Shared.Configuration;
using Sivar.Os.Shared.DTOs.ValueObjects;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Location service implementation using OpenStreetMap Nominatim (FREE geocoding service)
/// Rate limit: 1 request per second (configurable)
/// Documentation: https://nominatim.org/release-docs/latest/api/Overview/
/// </summary>
public class NominatimLocationService : LocationServiceBase
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NominatimLocationService> _logger;
    private readonly NominatimOptions _options;
    private readonly SemaphoreSlim _rateLimiter;
    private DateTime _lastRequestTime = DateTime.MinValue;

    public NominatimLocationService(
        IProfileRepository profileRepository,
        IPostRepository postRepository,
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<NominatimLocationService> logger,
        IOptions<NominatimOptions> options)
        : base(profileRepository, postRepository)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _options = options.Value;
        _rateLimiter = new SemaphoreSlim(1, 1);

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);
    }

    public override string ProviderName => "Nominatim";

    /// <summary>
    /// Geocode address to coordinates using Nominatim
    /// </summary>
    public override async Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string? city, 
        string? state, 
        string? country, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city) && 
            string.IsNullOrWhiteSpace(state) && 
            string.IsNullOrWhiteSpace(country))
        {
            return null;
        }

        var cacheKey = $"geocode:{city}:{state}:{country}";
        
        // Check cache first
        if (_cache.TryGetValue<(double, double)>(cacheKey, out var cached))
        {
            _logger.LogDebug("Geocode cache hit for {City}, {State}, {Country}", city, state, country);
            return cached;
        }

        try
        {
            // Build query string
            var queryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(city)) queryParts.Add(city);
            if (!string.IsNullOrWhiteSpace(state)) queryParts.Add(state);
            if (!string.IsNullOrWhiteSpace(country)) queryParts.Add(country);
            var query = string.Join(", ", queryParts);

            // Rate limiting
            await EnforceRateLimitAsync(cancellationToken);

            // Call Nominatim API
            var url = $"/search?q={Uri.EscapeDataString(query)}&format=json&limit=1";
            _logger.LogInformation("Geocoding: {Query}", query);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var results = await response.Content.ReadFromJsonAsync<List<NominatimSearchResult>>(cancellationToken);

            if (results?.Any() != true)
            {
                _logger.LogWarning("No geocoding results for {City}, {State}, {Country}", city, state, country);
                return null;
            }

            var result = results.First();
            var coordinates = (result.Lat, result.Lon);

            // Cache for 30 days (addresses don't change often)
            _cache.Set(cacheKey, coordinates, TimeSpan.FromDays(30));

            _logger.LogInformation("Geocoded {Query} to ({Lat}, {Lon})", query, coordinates.Lat, coordinates.Lon);
            return coordinates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Geocoding failed for {City}, {State}, {Country}", city, state, country);
            return null;
        }
    }

    /// <summary>
    /// Reverse geocode coordinates to address using Nominatim
    /// </summary>
    public override async Task<Location?> ReverseGeocodeAsync(
        double latitude, 
        double longitude, 
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCoordinates(latitude, longitude))
        {
            throw new ArgumentException("Invalid coordinates");
        }

        var cacheKey = $"reverse:{latitude:F6}:{longitude:F6}";

        // Check cache first
        if (_cache.TryGetValue<Location>(cacheKey, out var cached))
        {
            _logger.LogDebug("Reverse geocode cache hit for ({Lat}, {Lon})", latitude, longitude);
            return cached;
        }

        try
        {
            // Rate limiting
            await EnforceRateLimitAsync(cancellationToken);

            // Call Nominatim API
            var url = $"/reverse?lat={latitude}&lon={longitude}&format=json";
            _logger.LogInformation("Reverse geocoding: ({Lat}, {Lon})", latitude, longitude);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Log raw JSON for debugging
            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[NOMINATIM DEBUG] Raw JSON Response:");
            Console.WriteLine(jsonContent);
            _logger.LogInformation("Nominatim raw response: {Json}", jsonContent);

            var result = System.Text.Json.JsonSerializer.Deserialize<NominatimReverseResult>(
                jsonContent, 
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Address == null)
            {
                Console.WriteLine($"[NOMINATIM DEBUG] No address in result!");
                _logger.LogWarning("No reverse geocoding results for ({Lat}, {Lon})", latitude, longitude);
                return null;
            }

            // Log the parsed properties
            Console.WriteLine($"[NOMINATIM DEBUG] Parsed Properties:");
            Console.WriteLine($"  City: {result.Address.City}");
            Console.WriteLine($"  Town: {result.Address.Town}");
            Console.WriteLine($"  Village: {result.Address.Village}");
            Console.WriteLine($"  State: {result.Address.State}");
            Console.WriteLine($"  Region: {result.Address.Region}");
            Console.WriteLine($"  StateDistrict: {result.Address.StateDistrict}");
            Console.WriteLine($"  County: {result.Address.County}");
            Console.WriteLine($"  Country: {result.Address.Country}");
            Console.WriteLine($"  CountryCode: {result.Address.CountryCode}");
            
            _logger.LogInformation("Nominatim reverse geocoding response - City: {City}, Town: {Town}, Village: {Village}, State: {State}, Region: {Region}, StateDistrict: {StateDistrict}, County: {County}, Country: {Country}, CountryCode: {Code}",
                result.Address.City, result.Address.Town, result.Address.Village, 
                result.Address.State, result.Address.Region, result.Address.StateDistrict, result.Address.County,
                result.Address.Country, result.Address.CountryCode);

            var location = new Location
            {
                City = result.Address.City ?? result.Address.Town ?? result.Address.Village ?? string.Empty,
                State = result.Address.State ?? result.Address.Region ?? result.Address.StateDistrict ?? result.Address.County ?? string.Empty,
                Country = result.Address.Country ?? string.Empty,
                Latitude = latitude,
                Longitude = longitude
            };

            Console.WriteLine($"[NOMINATIM DEBUG] Final Location Object:");
            Console.WriteLine($"  City: '{location.City}'");
            Console.WriteLine($"  State: '{location.State}'");
            Console.WriteLine($"  Country: '{location.Country}'");
            Console.WriteLine($"  Lat: {location.Latitude}");
            Console.WriteLine($"  Lon: {location.Longitude}");


            // Cache for 30 days
            _cache.Set(cacheKey, location, TimeSpan.FromDays(30));

            _logger.LogInformation("Reverse geocoded ({Lat}, {Lon}) to {City}, {State}, {Country}", 
                latitude, longitude, location.City, location.State, location.Country);
            
            return location;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reverse geocoding failed for ({Lat}, {Lon})", latitude, longitude);
            return null;
        }
    }

    /// <summary>
    /// Enforce Nominatim rate limit (default 1 request per second)
    /// </summary>
    private async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var minInterval = TimeSpan.FromSeconds(1.0 / _options.RateLimitPerSecond);

            if (timeSinceLastRequest < minInterval)
            {
                var delay = minInterval - timeSinceLastRequest;
                _logger.LogDebug("Rate limiting: waiting {Delay}ms", delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    #region Nominatim API Models

    private class NominatimSearchResult
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string? DisplayName { get; set; }
    }

    private class NominatimReverseResult
    {
        public NominatimAddress? Address { get; set; }
    }

    private class NominatimAddress
    {
        public string? City { get; set; }
        public string? Town { get; set; }
        public string? Village { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        
        // Additional common property names from Nominatim (using explicit names to avoid conflicts)
        [System.Text.Json.Serialization.JsonPropertyName("region")]
        public string? Region { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("county")]
        public string? County { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("state_district")]
        public string? StateDistrict { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("ISO3166-2-lvl4")]
        public string? StateCode { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }
    }

    #endregion
}
