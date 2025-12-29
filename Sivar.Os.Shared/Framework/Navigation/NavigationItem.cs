namespace Sivar.Os.Shared.Framework.Navigation;

/// <summary>
/// Default implementation of INavigationItem.
/// Use this class to define navigation items in the registry.
/// </summary>
public class NavigationItem : INavigationItem
{
    /// <inheritdoc />
    public string Id { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string Title { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string TitleKey { get; set; } = string.Empty;
    
    /// <inheritdoc />
    public string Icon { get; set; } = "Circle";
    
    /// <inheritdoc />
    public string? Route { get; set; }
    
    /// <inheritdoc />
    public int Order { get; set; } = 100;
    
    /// <inheritdoc />
    public bool RequiresAuth { get; set; }
    
    /// <inheritdoc />
    public string[]? RequiredRoles { get; set; }
    
    /// <inheritdoc />
    public string[]? RequiredProfileTypes { get; set; }
    
    /// <inheritdoc />
    public PlatformType Platform { get; set; } = PlatformType.All;
    
    /// <inheritdoc />
    public bool IsAction { get; set; }
    
    /// <inheritdoc />
    public string? ActionId { get; set; }
    
    /// <inheritdoc />
    public Func<NavContext, bool>? IsVisible { get; set; }
    
    /// <inheritdoc />
    public Func<NavContext, bool>? IsEnabled { get; set; }
    
    /// <summary>
    /// Creates an empty navigation item.
    /// </summary>
    public NavigationItem() { }
    
    /// <summary>
    /// Creates a navigation item with required properties.
    /// </summary>
    public NavigationItem(string id, string titleKey, string icon, string? route, int order = 100)
    {
        Id = id;
        Title = titleKey; // Fallback to key
        TitleKey = titleKey;
        Icon = icon;
        Route = route;
        Order = order;
    }
    
    /// <summary>
    /// Creates a route-based navigation item.
    /// </summary>
    public static NavigationItem ForRoute(string id, string titleKey, string icon, string route, int order = 100)
    {
        return new NavigationItem
        {
            Id = id,
            Title = titleKey,
            TitleKey = titleKey,
            Icon = icon,
            Route = route,
            Order = order,
            IsAction = false
        };
    }
    
    /// <summary>
    /// Creates an action-based navigation item (no route, triggers action).
    /// </summary>
    public static NavigationItem ForAction(string id, string titleKey, string icon, string actionId, int order = 100)
    {
        return new NavigationItem
        {
            Id = id,
            Title = titleKey,
            TitleKey = titleKey,
            Icon = icon,
            Route = null,
            Order = order,
            IsAction = true,
            ActionId = actionId
        };
    }
}
