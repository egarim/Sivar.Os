namespace Sivar.Os.Shared.Framework.Menu;

/// <summary>
/// Provides menu items for a specific entity type.
/// Implement this interface to define menu items for posts, profiles, etc.
/// </summary>
public interface IMenuProvider
{
    /// <summary>
    /// The entity type this provider handles.
    /// Example: "Post", "Profile", "Comment", "Blog"
    /// </summary>
    string EntityType { get; }
    
    /// <summary>
    /// Priority for ordering when multiple providers exist for the same entity type.
    /// Lower values = higher priority (items appear first).
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Gets all menu items this provider can supply.
    /// The registry will filter based on context.
    /// </summary>
    IEnumerable<IMenuItem> GetMenuItems();
}
