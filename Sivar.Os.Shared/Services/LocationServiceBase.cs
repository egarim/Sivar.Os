using Microsoft.EntityFrameworkCore;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.DTOs.ValueObjects;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Base class for location service implementations.
/// Provides shared logic for geocoding, distance calculations, and PostGIS integration.
/// </summary>
public abstract class LocationServiceBase : ILocationService
{
    protected readonly IProfileRepository _profileRepository;
    protected readonly IPostRepository _postRepository;

    protected LocationServiceBase(
        IProfileRepository profileRepository,
        IPostRepository postRepository)
    {
        _profileRepository = profileRepository;
        _postRepository = postRepository;
    }

    /// <summary>
    /// Provider-specific geocoding implementation (e.g., Nominatim, Azure Maps, Google Maps)
    /// </summary>
    public abstract Task<(double Latitude, double Longitude)?> GeocodeAsync(
        string city, 
        string? state = null, 
        string? country = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Provider-specific reverse geocoding implementation
    /// </summary>
    public abstract Task<Location?> ReverseGeocodeAsync(
        double latitude, 
        double longitude, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Provider name for debugging/logging
    /// </summary>
    public abstract string ProviderName { get; }

    /// <summary>
    /// Calculate distance between two points using Haversine formula (fallback when PostGIS unavailable)
    /// </summary>
    public virtual Task<double> CalculateDistanceAsync(
        double lat1, 
        double lng1, 
        double lat2, 
        double lng2, 
        CancellationToken cancellationToken = default)
    {
        const double R = 6371; // Earth's radius in kilometers
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Task.FromResult(R * c);
    }

    /// <summary>
    /// Find nearby profiles using PostGIS find_nearby_profiles() function
    /// </summary>
    public virtual async Task<List<ProfileDto>> FindNearbyProfilesAsync(
        double latitude, 
        double longitude, 
        double radiusKm, 
        int limit = 50, 
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCoordinates(latitude, longitude))
            throw new ArgumentException("Invalid coordinates");

        if (radiusKm <= 0 || radiusKm > 10000)
            throw new ArgumentException("Radius must be between 0 and 10,000 km");

        if (limit <= 0 || limit > 500)
            throw new ArgumentException("Limit must be between 1 and 500");

        try
        {
            // Use PostGIS function for spatial query
            // Note: Using raw SQL since PostGIS functions aren't mapped in EF Core
            var sql = $"SELECT \"ProfileId\", \"DistanceKm\" FROM find_nearby_profiles({latitude}, {longitude}, {radiusKm}, {limit})";
            
            var dbContext = _profileRepository.GetDbContext();
            var connection = dbContext.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);
            
            var results = new List<ProfileLocationResult>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new ProfileLocationResult
                {
                    ProfileId = reader.GetGuid(0),
                    DistanceKm = reader.GetDouble(1)
                });
            }

            if (!results.Any())
                return new List<ProfileDto>();

            // Fetch full profile data with distance
            var profileIds = results.Select(r => r.ProfileId).ToList();
            var profiles = await _profileRepository.GetByIdsAsync(profileIds, cancellationToken);

            // Map to DTOs with distance
            return profiles.Select(p =>
            {
                var distance = results.First(r => r.ProfileId == p.Id).DistanceKm;
                return MapProfileToDto(p, distance);
            }).ToList();
        }
        catch (Exception ex)
        {
            // Log error and fall back to in-memory calculation if PostGIS unavailable
            Console.WriteLine($"PostGIS query failed: {ex.Message}. Falling back to Haversine.");
            // TODO: Implement fallback method when repository supports GetAllAsync with cancellation
            throw new InvalidOperationException("PostGIS is required for proximity searches. Fallback not yet implemented.", ex);
        }
    }

    /// <summary>
    /// Find nearby posts using PostGIS find_nearby_posts() function
    /// </summary>
    public virtual async Task<List<PostDto>> FindNearbyPostsAsync(
        double latitude, 
        double longitude, 
        double radiusKm, 
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCoordinates(latitude, longitude))
            throw new ArgumentException("Invalid coordinates");

        if (radiusKm <= 0 || radiusKm > 10000)
            throw new ArgumentException("Radius must be between 0 and 10,000 km");

        if (page <= 0)
            throw new ArgumentException("Page must be greater than 0");

        if (pageSize <= 0 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100");

        try
        {
            var offset = (page - 1) * pageSize;

            // Use PostGIS function for spatial query
            var sql = $"SELECT \"PostId\", \"DistanceKm\" FROM find_nearby_posts({latitude}, {longitude}, {radiusKm}, {pageSize}) OFFSET {offset}";
            
            var dbContext = _postRepository.GetDbContext();
            var connection = dbContext.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);
            
            var results = new List<PostLocationResult>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new PostLocationResult
                {
                    PostId = reader.GetGuid(0),
                    DistanceKm = reader.GetDouble(1)
                });
            }

            if (!results.Any())
                return new List<PostDto>();

            // Fetch full post data with distance
            var postIds = results.Select(r => r.PostId).ToList();
            var posts = await _postRepository.GetByIdsAsync(postIds, cancellationToken);

            // Map to DTOs with distance
            return posts.Select(p =>
            {
                var distance = results.First(r => r.PostId == p.Id).DistanceKm;
                return MapPostToDto(p, distance);
            }).ToList();
        }
        catch (Exception ex)
        {
            // Log error and fall back to in-memory calculation if PostGIS unavailable
            Console.WriteLine($"PostGIS query failed: {ex.Message}. Falling back to Haversine.");
            // TODO: Implement fallback method when repository supports GetAllAsync with cancellation
            throw new InvalidOperationException("PostGIS is required for proximity searches. Fallback not yet implemented.", ex);
        }
    }

    /// <summary>
    /// Update profile GeoLocation column using raw SQL (bypasses EF Core)
    /// </summary>
    public virtual async Task UpdateProfileGeoLocationAsync(
        Guid profileId, 
        double latitude, 
        double longitude, 
        string source, 
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCoordinates(latitude, longitude))
            throw new ArgumentException("Invalid coordinates");

        var validSources = new[] { "Manual", "Geocoded", "GPS", "IP", "Migrated", "Auto" };
        if (!validSources.Contains(source))
            throw new ArgumentException($"Invalid source. Must be one of: {string.Join(", ", validSources)}");

        var geoPoint = ToPostGISPoint(latitude, longitude);

        var sql = $@"UPDATE ""Sivar_Profiles""
                   SET ""GeoLocation"" = '{geoPoint}'::geography,
                       ""GeoLocationUpdatedAt"" = '{DateTime.UtcNow:O}',
                       ""GeoLocationSource"" = '{source}'
                   WHERE ""Id"" = '{profileId}'";

        await _profileRepository.GetDbContext()
            .Database
            .ExecuteSqlRawAsync(sql, cancellationToken);
    }

    /// <summary>
    /// Update post GeoLocation column using raw SQL (bypasses EF Core)
    /// </summary>
    public virtual async Task UpdatePostGeoLocationAsync(
        Guid postId, 
        double latitude, 
        double longitude, 
        string source, 
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCoordinates(latitude, longitude))
            throw new ArgumentException("Invalid coordinates");

        var validSources = new[] { "Manual", "Geocoded", "GPS", "IP", "Migrated", "Auto" };
        if (!validSources.Contains(source))
            throw new ArgumentException($"Invalid source. Must be one of: {string.Join(", ", validSources)}");

        var geoPoint = ToPostGISPoint(latitude, longitude);

        var sql = $@"UPDATE ""Sivar_Posts""
                   SET ""GeoLocation"" = '{geoPoint}'::geography,
                       ""GeoLocationUpdatedAt"" = '{DateTime.UtcNow:O}',
                       ""GeoLocationSource"" = '{source}'
                   WHERE ""Id"" = '{postId}'";

        await _postRepository.GetDbContext()
            .Database
            .ExecuteSqlRawAsync(sql, cancellationToken);
    }

    #region Helper Methods

    /// <summary>
    /// Convert latitude and longitude to PostGIS POINT format
    /// </summary>
    public virtual string ToPostGISPoint(double latitude, double longitude)
    {
        // PostGIS uses (longitude, latitude) order - SRID 4326 is WGS84
        return $"POINT({longitude} {latitude})";
    }

    /// <summary>
    /// Parse PostGIS POINT string to latitude/longitude
    /// </summary>
    public virtual (double Latitude, double Longitude)? ParsePostGISPoint(string? geoPoint)
    {
        if (string.IsNullOrWhiteSpace(geoPoint))
            return null;

        // Expected format: "POINT(longitude latitude)"
        var match = System.Text.RegularExpressions.Regex.Match(
            geoPoint, 
            @"POINT\((-?\d+\.?\d*)\s+(-?\d+\.?\d*)\)");

        if (!match.Success)
            return null;

        if (double.TryParse(match.Groups[1].Value, out var lng) &&
            double.TryParse(match.Groups[2].Value, out var lat))
        {
            return (lat, lng);
        }

        return null;
    }

    /// <summary>
    /// Validate coordinates
    /// </summary>
    public virtual bool IsValidCoordinates(double latitude, double longitude)
    {
        return latitude >= -90 && latitude <= 90 && 
               longitude >= -180 && longitude <= 180;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    /// <summary>
    /// Map Profile entity to ProfileDto with distance
    /// </summary>
    private ProfileDto MapProfileToDto(Profile profile, double distanceKm)
    {
        // Create basic ProfileDto with distance information
        // Note: Full DTO mapping should ideally use AutoMapper or a dedicated service
        return new ProfileDto
        {
            Id = profile.Id,
            Handle = profile.Handle,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            ProfileType = new ProfileTypeDto
            {
                Id = profile.ProfileType.Id,
                Name = profile.ProfileType.Name,
                Description = profile.ProfileType.Description
            },
            Avatar = profile.Avatar,
            LocationDisplay = profile.Location?.ToString() ?? string.Empty,
            IsActive = profile.IsActive,
            VisibilityLevel = profile.VisibilityLevel,
            ViewCount = profile.ViewCount,
            Tags = profile.Tags,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            DistanceKm = distanceKm
        };
    }

    /// <summary>
    /// Map Post entity to PostDto with distance
    /// </summary>
    private PostDto MapPostToDto(Post post, double distanceKm)
    {
        // Create basic PostDto with distance information
        // Note: Full DTO mapping should ideally use AutoMapper or a dedicated service
        return new PostDto
        {
            Id = post.Id,
            Content = post.Content,
            PostType = post.PostType,
            Visibility = post.Visibility,
            Language = post.Language,
            Tags = post.Tags?.ToList() ?? new List<string>(),
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            IsEdited = post.IsEdited,
            EditedAt = post.EditedAt,
            DistanceKm = distanceKm,
            Profile = new ProfileDto
            {
                Id = post.Profile.Id,
                Handle = post.Profile.Handle,
                DisplayName = post.Profile.DisplayName,
                Avatar = post.Profile.Avatar
            }
        };
    }

    #endregion
}
