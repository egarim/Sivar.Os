using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Framework.Navigation;

/// <summary>
/// Context passed to navigation items for visibility and enablement checks.
/// Contains information about the current user, profile, and platform.
/// </summary>
public class NavContext
{
    /// <summary>
    /// Whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; set; }
    
    /// <summary>
    /// The currently active profile (null if not authenticated or no profile selected).
    /// Uses ActiveProfileDto which is the lightweight DTO used in layouts.
    /// </summary>
    public ActiveProfileDto? ActiveProfile { get; set; }
    
    /// <summary>
    /// The current route/URL path.
    /// Example: "/home", "/search"
    /// </summary>
    public string? CurrentRoute { get; set; }
    
    /// <summary>
    /// The platform the app is running on.
    /// </summary>
    public PlatformType Platform { get; set; } = PlatformType.Web;
    
    /// <summary>
    /// User's roles (from authentication claims).
    /// </summary>
    public string[] Roles { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Current culture/locale.
    /// Example: "en-US", "es-ES"
    /// </summary>
    public string Culture { get; set; } = "en-US";
    
    /// <summary>
    /// Whether dark mode is enabled.
    /// </summary>
    public bool IsDarkMode { get; set; }
    
    /// <summary>
    /// Additional custom data for extensibility.
    /// </summary>
    public Dictionary<string, object?> CustomData { get; set; } = new();
    
    /// <summary>
    /// Creates a context for an authenticated user.
    /// </summary>
    public static NavContext ForUser(ActiveProfileDto? profile, PlatformType platform = PlatformType.Web)
    {
        return new NavContext
        {
            IsAuthenticated = true,
            ActiveProfile = profile,
            Platform = platform
        };
    }
    
    /// <summary>
    /// Creates a context for an anonymous user.
    /// </summary>
    public static NavContext Anonymous(PlatformType platform = PlatformType.Web)
    {
        return new NavContext
        {
            IsAuthenticated = false,
            Platform = platform
        };
    }
}
