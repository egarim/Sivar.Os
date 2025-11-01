namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// Result from PostGIS proximity query for profiles.
/// Used internally by LocationService.
/// </summary>
public class ProfileLocationResult
{
    public Guid ProfileId { get; set; }
    public double DistanceKm { get; set; }
}

/// <summary>
/// Result from PostGIS proximity query for posts.
/// Used internally by LocationService.
/// </summary>
public class PostLocationResult
{
    public Guid PostId { get; set; }
    public double DistanceKm { get; set; }
}
