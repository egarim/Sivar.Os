namespace Sivar.Os.Shared.Framework;

/// <summary>
/// Defines the platform types supported by the framework.
/// Used for platform-specific navigation, components, and behavior.
/// </summary>
public enum PlatformType
{
    /// <summary>
    /// Applies to all platforms (universal)
    /// </summary>
    All = 0,
    
    /// <summary>
    /// Web platform (Blazor WebAssembly)
    /// </summary>
    Web = 1,
    
    /// <summary>
    /// Mobile platform (MAUI Blazor Hybrid)
    /// </summary>
    Mobile = 2,
    
    /// <summary>
    /// Desktop platform (future support)
    /// </summary>
    Desktop = 3
}
