namespace Sivar.Os.Shared.DTOs.ValueObjects;

/// <summary>
/// Value object representing a geographic location
/// </summary>
public class Location
{
    /// <summary>
    /// City name
    /// </summary>
    public virtual string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province name
    /// </summary>
    public virtual string State { get; set; } = string.Empty;

    /// <summary>
    /// Country name
    /// </summary>
    public virtual string Country { get; set; } = string.Empty;

    /// <summary>
    /// Latitude coordinate (optional)
    /// </summary>
    public virtual double? Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate (optional)
    /// </summary>
    public virtual double? Longitude { get; set; }

    /// <summary>
    /// Creates a new Location instance
    /// </summary>
    public Location() { }

    /// <summary>
    /// Creates a new Location instance with specified values
    /// </summary>
    /// <param name="city">City name</param>
    /// <param name="state">State or province name</param>
    /// <param name="country">Country name</param>
    /// <param name="latitude">Optional latitude coordinate</param>
    /// <param name="longitude">Optional longitude coordinate</param>
    public Location(string city, string state, string country, double? latitude = null, double? longitude = null)
    {
        City = city ?? string.Empty;
        State = state ?? string.Empty;
        Country = country ?? string.Empty;
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Returns a formatted string representation of the location
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(City)) parts.Add(City);
        if (!string.IsNullOrWhiteSpace(State)) parts.Add(State);
        if (!string.IsNullOrWhiteSpace(Country)) parts.Add(Country);

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current Location
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not Location other)
            return false;

        return City == other.City &&
               State == other.State &&
               Country == other.Country &&
               Latitude == other.Latitude &&
               Longitude == other.Longitude;
    }

    /// <summary>
    /// Serves as the default hash function
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(City, State, Country, Latitude, Longitude);
    }

    /// <summary>
    /// Determines whether two Location objects are equal
    /// </summary>
    public static bool operator ==(Location? left, Location? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two Location objects are not equal
    /// </summary>
    public static bool operator !=(Location? left, Location? right)
    {
        return !(left == right);
    }
}
