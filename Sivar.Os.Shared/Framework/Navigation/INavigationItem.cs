namespace Sivar.Os.Shared.Framework.Navigation;

/// <summary>
/// Represents a navigation item in the application menu.
/// Supports role-based visibility, platform-specific behavior, and localization.
/// </summary>
public interface INavigationItem
{
    /// <summary>
    /// Unique identifier for the navigation item.
    /// Example: "home", "search", "my-schedule"
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Display title (fallback if localization key not found).
    /// </summary>
    string Title { get; }
    
    /// <summary>
    /// Localization resource key for the title.
    /// Example: "Home", "Search", "MySchedule"
    /// </summary>
    string TitleKey { get; }
    
    /// <summary>
    /// Icon identifier (MudBlazor icon name without prefix).
    /// Example: "Home", "Search", "EventNote"
    /// </summary>
    string Icon { get; }
    
    /// <summary>
    /// Route path for navigation. Null if this is an action item.
    /// Example: "/home", "/search", "/my-schedule"
    /// </summary>
    string? Route { get; }
    
    /// <summary>
    /// Sort order for display. Lower numbers appear first.
    /// </summary>
    int Order { get; }
    
    /// <summary>
    /// Whether authentication is required to see this item.
    /// </summary>
    bool RequiresAuth { get; }
    
    /// <summary>
    /// Roles required to see this item (null = any role).
    /// Example: ["Admin", "Moderator"]
    /// </summary>
    string[]? RequiredRoles { get; }
    
    /// <summary>
    /// Profile types that can see this item (null = any profile type).
    /// Example: ["Business", "Organization"]
    /// </summary>
    string[]? RequiredProfileTypes { get; }
    
    /// <summary>
    /// Platforms where this item is visible (default: All).
    /// </summary>
    PlatformType Platform { get; }
    
    /// <summary>
    /// Whether this is an action (like opening chat) rather than a route.
    /// </summary>
    bool IsAction { get; }
    
    /// <summary>
    /// Action identifier to execute when IsAction is true.
    /// Example: "toggle-ai-chat"
    /// </summary>
    string? ActionId { get; }
    
    /// <summary>
    /// Custom visibility predicate. Called with navigation context.
    /// Return true to show, false to hide.
    /// </summary>
    Func<NavContext, bool>? IsVisible { get; }
    
    /// <summary>
    /// Custom enabled predicate. Called with navigation context.
    /// Return true to enable, false to disable (grayed out).
    /// </summary>
    Func<NavContext, bool>? IsEnabled { get; }
}
