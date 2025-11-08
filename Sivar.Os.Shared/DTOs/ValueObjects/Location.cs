using System.ComponentModel;

namespace Sivar.Os.Shared.DTOs.ValueObjects;

/// <summary>
/// Value object representing a geographic location
/// </summary>
public class Location : INotifyPropertyChanging, INotifyPropertyChanged
{
    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _city = string.Empty;
    private string _state = string.Empty;
    private string _country = string.Empty;
    private double? _latitude;
    private double? _longitude;

    /// <summary>
    /// City name
    /// </summary>
    public virtual string City 
    { 
        get => _city;
        set => SetProperty(ref _city, value);
    }

    /// <summary>
    /// State or province name
    /// </summary>
    public virtual string State 
    { 
        get => _state;
        set => SetProperty(ref _state, value);
    }

    /// <summary>
    /// Country name
    /// </summary>
    public virtual string Country 
    { 
        get => _country;
        set => SetProperty(ref _country, value);
    }

    /// <summary>
    /// Latitude coordinate (optional)
    /// </summary>
    public virtual double? Latitude 
    { 
        get => _latitude;
        set => SetProperty(ref _latitude, value);
    }

    /// <summary>
    /// Longitude coordinate (optional)
    /// </summary>
    public virtual double? Longitude 
    { 
        get => _longitude;
        set => SetProperty(ref _longitude, value);
    }

    /// <summary>
    /// Helper method to set property values with change notification
    /// </summary>
    private void SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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
        _city = city ?? string.Empty;
        _state = state ?? string.Empty;
        _country = country ?? string.Empty;
        _latitude = latitude;
        _longitude = longitude;
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

        return _city == other._city &&
               _state == other._state &&
               _country == other._country &&
               _latitude == other._latitude &&
               _longitude == other._longitude;
    }

    /// <summary>
    /// Serves as the default hash function
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(_city, _state, _country, _latitude, _longitude);
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
