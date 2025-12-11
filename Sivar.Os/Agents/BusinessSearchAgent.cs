using System.ComponentModel;
using System.Text.Json;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Agents;

/// <summary>
/// AI Agent specialized in searching for businesses, restaurants, services, and products.
/// Returns structured data for card-based UI rendering.
/// </summary>
public class BusinessSearchAgent
{
    private readonly ISearchResultService _searchResultService;
    private readonly ILogger<BusinessSearchAgent> _logger;
    private Guid _currentProfileId;
    private (double Latitude, double Longitude)? _userLocation;

    public BusinessSearchAgent(
        ISearchResultService searchResultService,
        ILogger<BusinessSearchAgent> logger)
    {
        _searchResultService = searchResultService ?? throw new ArgumentNullException(nameof(searchResultService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Set the current user's profile ID for context
    /// </summary>
    public void SetCurrentProfile(Guid profileId)
    {
        _currentProfileId = profileId;
    }

    /// <summary>
    /// Set the current user's location for proximity-based searches
    /// </summary>
    public void SetUserLocation(double? latitude, double? longitude)
    {
        if (latitude.HasValue && longitude.HasValue)
        {
            _userLocation = (latitude.Value, longitude.Value);
            _logger.LogInformation("[BusinessSearchAgent] Location set: {Lat}, {Lng}", latitude, longitude);
        }
        else
        {
            _userLocation = null;
        }
    }

    /// <summary>
    /// Search for businesses and return structured results for card rendering
    /// </summary>
    [Description("Search for businesses, restaurants, stores, or services. Returns structured data including location, contact info, and working hours.")]
    public async Task<SearchResultsCollectionDto> SearchBusinesses(
        [Description("The search query - what type of business or service the user is looking for")]
        string query,
        [Description("Optional category filter like 'Restaurante', 'Entretenimiento', 'Servicios'")]
        string? category = null,
        [Description("Maximum distance in kilometers (uses user's current location if available)")]
        double? maxDistanceKm = null,
        [Description("Maximum number of results (default 10)")]
        int limit = 10)
    {
        _logger.LogInformation("[BusinessSearchAgent.SearchBusinesses] Query='{Query}', Category='{Category}'", query, category);

        try
        {
            var request = new HybridSearchRequestDto
            {
                Query = query,
                Category = category,
                ResultTypes = [SearchResultType.Business],
                UserLatitude = _userLocation?.Latitude,
                UserLongitude = _userLocation?.Longitude,
                MaxDistanceKm = maxDistanceKm,
                Limit = limit,
                SemanticWeight = 0.5,
                FullTextWeight = 0.3,
                GeoWeight = _userLocation.HasValue ? 0.2 : 0.0
            };

            var results = await _searchResultService.HybridSearchAsync(request);

            _logger.LogInformation("[BusinessSearchAgent.SearchBusinesses] Found {Count} results", results.TotalCount);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BusinessSearchAgent.SearchBusinesses] Error searching businesses");
            return new SearchResultsCollectionDto { Query = query };
        }
    }

    /// <summary>
    /// Search for restaurants and food-related businesses
    /// </summary>
    [Description("Search for restaurants, cafes, pupuserías, or food establishments.")]
    public async Task<List<BusinessSearchResultDto>> SearchRestaurants(
        [Description("The type of food or restaurant name to search for")]
        string query,
        [Description("Price range filter: '$', '$$', or '$$$'")]
        string? priceRange = null,
        [Description("Maximum distance in kilometers")]
        double? maxDistanceKm = null,
        [Description("Maximum number of results")]
        int limit = 10)
    {
        _logger.LogInformation("[BusinessSearchAgent.SearchRestaurants] Query='{Query}'", query);

        try
        {
            var results = await _searchResultService.SearchBusinessesAsync(
                query: $"restaurante {query}",
                category: "Restaurante",
                userLatitude: _userLocation?.Latitude,
                userLongitude: _userLocation?.Longitude,
                maxDistanceKm: maxDistanceKm,
                limit: limit);

            // Filter by price range if specified
            if (!string.IsNullOrEmpty(priceRange))
            {
                results = results.Where(r => r.PriceRange == priceRange).ToList();
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BusinessSearchAgent.SearchRestaurants] Error");
            return [];
        }
    }

    /// <summary>
    /// Search for entertainment venues
    /// </summary>
    [Description("Search for entertainment venues like bars, clubs, theaters, cinemas, or recreation centers.")]
    public async Task<List<BusinessSearchResultDto>> SearchEntertainment(
        [Description("The type of entertainment to search for")]
        string query,
        [Description("Maximum distance in kilometers")]
        double? maxDistanceKm = null,
        [Description("Maximum number of results")]
        int limit = 10)
    {
        _logger.LogInformation("[BusinessSearchAgent.SearchEntertainment] Query='{Query}'", query);

        try
        {
            return await _searchResultService.SearchBusinessesAsync(
                query: query,
                category: "Entretenimiento",
                userLatitude: _userLocation?.Latitude,
                userLongitude: _userLocation?.Longitude,
                maxDistanceKm: maxDistanceKm,
                limit: limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BusinessSearchAgent.SearchEntertainment] Error");
            return [];
        }
    }

    /// <summary>
    /// Search for services (plumbers, electricians, lawyers, etc.)
    /// </summary>
    [Description("Search for professional services like plumbers, electricians, lawyers, doctors, or other service providers.")]
    public async Task<SearchResultsCollectionDto> SearchServices(
        [Description("The type of service needed")]
        string query,
        [Description("Maximum distance in kilometers")]
        double? maxDistanceKm = null,
        [Description("Maximum number of results")]
        int limit = 10)
    {
        _logger.LogInformation("[BusinessSearchAgent.SearchServices] Query='{Query}'", query);

        try
        {
            var request = new HybridSearchRequestDto
            {
                Query = query,
                Category = "Servicios",
                ResultTypes = [SearchResultType.Service, SearchResultType.Business],
                UserLatitude = _userLocation?.Latitude,
                UserLongitude = _userLocation?.Longitude,
                MaxDistanceKm = maxDistanceKm,
                Limit = limit
            };

            return await _searchResultService.HybridSearchAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BusinessSearchAgent.SearchServices] Error");
            return new SearchResultsCollectionDto { Query = query };
        }
    }

    /// <summary>
    /// Search for tourism attractions and destinations
    /// </summary>
    [Description("Search for tourist attractions, beaches, parks, historical sites, or other points of interest.")]
    public async Task<List<TourismSearchResultDto>> SearchTourism(
        [Description("The type of attraction or destination")]
        string query,
        [Description("Maximum distance in kilometers")]
        double? maxDistanceKm = null,
        [Description("Maximum number of results")]
        int limit = 10)
    {
        _logger.LogInformation("[BusinessSearchAgent.SearchTourism] Query='{Query}'", query);

        try
        {
            return await _searchResultService.SearchTourismAsync(
                query: query,
                userLatitude: _userLocation?.Latitude,
                userLongitude: _userLocation?.Longitude,
                limit: limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BusinessSearchAgent.SearchTourism] Error");
            return [];
        }
    }

    /// <summary>
    /// Get nearby businesses using the user's current location
    /// </summary>
    [Description("Find businesses near the user's current location. Requires location to be set.")]
    public async Task<SearchResultsCollectionDto> GetNearby(
        [Description("Optional category to filter by")]
        string? category = null,
        [Description("Maximum distance in kilometers (default 5km)")]
        double maxDistanceKm = 5,
        [Description("Maximum number of results")]
        int limit = 10)
    {
        if (!_userLocation.HasValue)
        {
            _logger.LogWarning("[BusinessSearchAgent.GetNearby] No location set");
            return new SearchResultsCollectionDto 
            { 
                Query = "nearby businesses",
                TotalCount = 0
            };
        }

        _logger.LogInformation("[BusinessSearchAgent.GetNearby] Category='{Category}', MaxDistance={Km}km", category, maxDistanceKm);

        try
        {
            var request = new HybridSearchRequestDto
            {
                Query = category ?? "negocios cercanos",
                Category = category,
                UserLatitude = _userLocation?.Latitude,
                UserLongitude = _userLocation?.Longitude,
                MaxDistanceKm = maxDistanceKm,
                Limit = limit,
                SemanticWeight = 0.2,
                FullTextWeight = 0.2,
                GeoWeight = 0.6 // Prioritize geographic proximity
            };

            return await _searchResultService.HybridSearchAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BusinessSearchAgent.GetNearby] Error");
            return new SearchResultsCollectionDto { Query = category ?? "nearby" };
        }
    }

    /// <summary>
    /// Convert search results to a format suitable for chat response
    /// </summary>
    public static string FormatResultsForChat(SearchResultsCollectionDto results)
    {
        if (!results.HasResults)
        {
            return "No encontré resultados para tu búsqueda.";
        }

        var response = new System.Text.StringBuilder();
        response.AppendLine($"Encontré {results.TotalCount} resultado(s):");
        response.AppendLine();

        int index = 1;

        foreach (var business in results.Businesses)
        {
            response.AppendLine($"**{index}. {business.Title}**");
            if (!string.IsNullOrEmpty(business.Category))
                response.AppendLine($"   📂 {business.Category}");
            if (!string.IsNullOrEmpty(business.City))
                response.AppendLine($"   📍 {business.City}{(business.DistanceKm.HasValue ? $" ({business.DistanceKm:F1} km)" : "")}");
            if (!string.IsNullOrEmpty(business.Description))
                response.AppendLine($"   {business.Description}");
            response.AppendLine();
            index++;
        }

        foreach (var evt in results.Events)
        {
            response.AppendLine($"**{index}. {evt.Title}**");
            response.AppendLine($"   📅 {evt.EventDateFormatted}");
            if (!string.IsNullOrEmpty(evt.Venue))
                response.AppendLine($"   🏛️ {evt.Venue}");
            if (!string.IsNullOrEmpty(evt.City))
                response.AppendLine($"   📍 {evt.City}");
            response.AppendLine();
            index++;
        }

        foreach (var svc in results.Services)
        {
            response.AppendLine($"**{index}. {svc.Title}**");
            if (!string.IsNullOrEmpty(svc.PriceFormatted))
                response.AppendLine($"   💰 {svc.PriceFormatted}");
            if (!string.IsNullOrEmpty(svc.City))
                response.AppendLine($"   📍 {svc.City}");
            response.AppendLine();
            index++;
        }

        return response.ToString();
    }
}
