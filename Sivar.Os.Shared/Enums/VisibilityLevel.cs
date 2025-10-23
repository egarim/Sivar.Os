namespace Sivar.Os.Shared.Enums;

/// <summary>
/// Represents the visibility level of a profile
/// </summary>
public enum VisibilityLevel
{
    /// <summary>
    /// Profile is visible to everyone (public)
    /// </summary>
    Public = 1,

    /// <summary>
    /// Profile is only visible to the owner (private)
    /// </summary>
    Private = 2,

    /// <summary>
    /// Profile is visible to specific users only (restricted)
    /// </summary>
    Restricted = 3,

    /// <summary>
    /// Profile is visible to connections/friends only
    /// </summary>
    ConnectionsOnly = 4
}