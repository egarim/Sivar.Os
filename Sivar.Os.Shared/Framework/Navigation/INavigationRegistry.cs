namespace Sivar.Os.Shared.Framework.Navigation;

/// <summary>
/// Registry for navigation items. Provides a single source of truth
/// for all navigation across Web and Mobile platforms.
/// </summary>
public interface INavigationRegistry
{
    /// <summary>
    /// Gets all navigation items that are visible for the given context.
    /// Items are filtered by authentication, roles, profile types, platform, and custom visibility.
    /// </summary>
    /// <param name="context">The navigation context with user/platform info.</param>
    /// <returns>Visible navigation items sorted by Order.</returns>
    IEnumerable<INavigationItem> GetVisibleItems(NavContext context);
    
    /// <summary>
    /// Gets all registered navigation items (unfiltered).
    /// </summary>
    /// <returns>All navigation items sorted by Order.</returns>
    IEnumerable<INavigationItem> GetAllItems();
    
    /// <summary>
    /// Gets a navigation item by its ID.
    /// </summary>
    /// <param name="id">The navigation item ID.</param>
    /// <returns>The navigation item or null if not found.</returns>
    INavigationItem? GetItem(string id);
    
    /// <summary>
    /// Registers a navigation item in the registry.
    /// </summary>
    /// <param name="item">The navigation item to register.</param>
    void Register(INavigationItem item);
    
    /// <summary>
    /// Registers multiple navigation items at once.
    /// </summary>
    /// <param name="items">The navigation items to register.</param>
    void RegisterRange(IEnumerable<INavigationItem> items);
    
    /// <summary>
    /// Removes a navigation item from the registry.
    /// </summary>
    /// <param name="id">The ID of the item to remove.</param>
    /// <returns>True if removed, false if not found.</returns>
    bool Unregister(string id);
    
    /// <summary>
    /// Checks if an item with the given ID exists.
    /// </summary>
    /// <param name="id">The navigation item ID.</param>
    /// <returns>True if exists, false otherwise.</returns>
    bool Contains(string id);
}
