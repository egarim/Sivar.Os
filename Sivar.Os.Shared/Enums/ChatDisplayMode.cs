namespace Sivar.Os.Shared.Enums;

/// <summary>
/// Defines how chat search results are displayed to the user
/// </summary>
public enum ChatDisplayMode
{
    /// <summary>
    /// Display results as visual cards in a carousel (one at a time with navigation)
    /// Best for: browsing with images, mobile users
    /// </summary>
    Cards = 0,
    
    /// <summary>
    /// Display results as a compact text list
    /// Best for: quick scanning, seeing more results at once
    /// </summary>
    List = 1,
    
    /// <summary>
    /// Display results in a grid layout (multiple cards visible)
    /// Best for: desktop users, visual comparison
    /// </summary>
    Grid = 2,
    
    /// <summary>
    /// Display results on an interactive map with location markers
    /// Best for: location-based searches, geographic exploration
    /// </summary>
    Map = 3
}
