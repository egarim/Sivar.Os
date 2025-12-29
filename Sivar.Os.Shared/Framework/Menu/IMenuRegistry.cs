namespace Sivar.Os.Shared.Framework.Menu;

/// <summary>
/// Central registry for menu providers.
/// Components request menus from here rather than building them inline.
/// </summary>
public interface IMenuRegistry
{
    /// <summary>
    /// Gets visible menu items for a given context.
    /// Filters based on entity type, ownership, platform, etc.
    /// </summary>
    IEnumerable<IMenuItem> GetMenuItems(MenuContext context);
    
    /// <summary>
    /// Gets all menu items for an entity type (unfiltered).
    /// </summary>
    IEnumerable<IMenuItem> GetAllItems(string entityType);
    
    /// <summary>
    /// Gets a specific menu item by ID.
    /// </summary>
    IMenuItem? GetItem(string id);
    
    /// <summary>
    /// Registers a menu provider.
    /// </summary>
    void RegisterProvider(IMenuProvider provider);
    
    /// <summary>
    /// Registers a single menu item directly.
    /// </summary>
    void RegisterItem(string entityType, IMenuItem item);
    
    /// <summary>
    /// Removes a menu item by ID.
    /// </summary>
    bool UnregisterItem(string id);
}
