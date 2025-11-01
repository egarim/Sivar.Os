namespace Sivar.Os.Shared.Services;

using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.DTOs.ValueObjects;

/// <summary>
/// Provider-agnostic location service interface.
/// Implementations can use Nominatim, Azure Maps, Google Maps, etc.
/// Components should ONLY depend on this interface, never on specific providers.
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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Coordinates or null if not found</returns>
    Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string city, 
        string? state = null, 
        string? country = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Converts coordinates to an address.
    /// </summary>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Location with address information or null if not found</returns>
    Task<Location?> ReverseGeocodeAsync(
        double latitude, 
        double longitude,
        CancellationToken cancellationToken = default);
    
    // ==================== DISTANCE ====================
    
    /// <summary>
    /// Calculates distance between two points (in kilometers).
    /// Uses PostGIS if available, falls back to Haversine formula.
    /// </summary>
    /// <param name="lat1">First point latitude</param>
    /// <param name="lng1">First point longitude</param>
    /// <param name="lat2">Second point latitude</param>
    /// <param name="lng2">Second point longitude</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Distance in kilometers</returns>
    Task<double> CalculateDistanceAsync(
        double lat1, double lng1, 
        double lat2, double lng2,
        CancellationToken cancellationToken = default);
    
    // ==================== SEARCH ====================
    
    /// <summary>
    /// Finds nearby profiles within radius.
    /// </summary>
    /// <param name="latitude">Center point latitude</param>
    /// <param name="longitude">Center point longitude</param>
    /// <param name="radiusKm">Search radius in kilometers</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of profiles with distance information</returns>
    Task<List<ProfileDto>> FindNearbyProfilesAsync(
        double latitude, 
        double longitude, 
        double radiusKm, 
        int limit = 50,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds nearby posts within radius.
    /// </summary>
    /// <param name="latitude">Center point latitude</param>
    /// <param name="longitude">Center point longitude</param>
    /// <param name="radiusKm">Search radius in kilometers</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of results per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of posts with distance information</returns>
    Task<List<PostDto>> FindNearbyPostsAsync(
        double latitude, 
        double longitude, 
        double radiusKm, 
        int page = 1, 
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    
    // ==================== GEOLOCATION UPDATES ====================
    
    /// <summary>
    /// Updates GeoLocation column in database from latitude/longitude.
    /// Uses raw SQL to update PostGIS geography column.
    /// </summary>
    /// <param name="profileId">Profile ID to update</param>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="source">Source of location data (Manual, Geocoded, GPS, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateProfileGeoLocationAsync(
        Guid profileId, 
        double latitude, 
        double longitude, 
        string source = "Manual",
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates GeoLocation column in database for a post.
    /// </summary>
    /// <param name="postId">Post ID to update</param>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="source">Source of location data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdatePostGeoLocationAsync(
        Guid postId, 
        double latitude, 
        double longitude, 
        string source = "Manual",
        CancellationToken cancellationToken = default);
    
    // ==================== UTILITIES ====================
    
    /// <summary>
    /// Converts lat/long to PostGIS POINT format.
    /// </summary>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <returns>PostGIS POINT string, e.g., "POINT(-74.0060 40.7128)"</returns>
    string ToPostGISPoint(double latitude, double longitude);
    
    /// <summary>
    /// Parses PostGIS POINT string to coordinates.
    /// </summary>
    /// <param name="geoLocation">PostGIS POINT string</param>
    /// <returns>Tuple of (Latitude, Longitude) or null if invalid</returns>
    (double Latitude, double Longitude)? ParsePostGISPoint(string? geoLocation);
    
    /// <summary>
    /// Validates if coordinates are within valid ranges.
    /// Latitude: -90 to 90, Longitude: -180 to 180
    /// </summary>
    /// <param name="latitude">Latitude to validate</param>
    /// <param name="longitude">Longitude to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidCoordinates(double latitude, double longitude);
    
    /// <summary>
    /// Gets the current provider name (for debugging/logging).
    /// </summary>
    string ProviderName { get; }
}
