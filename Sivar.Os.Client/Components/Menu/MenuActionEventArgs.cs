using Sivar.Os.Shared.Framework.Menu;

namespace Sivar.Os.Client.Components.Menu;

/// <summary>
/// Event args for menu action events.
/// </summary>
public class MenuActionEventArgs
{
    /// <summary>
    /// The action ID that was triggered.
    /// </summary>
    public string ActionId { get; set; } = string.Empty;
    
    /// <summary>
    /// The entity type the action is for.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// The entity ID the action is for.
    /// </summary>
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// The menu item that was clicked.
    /// </summary>
    public IMenuItem? MenuItem { get; set; }
    
    /// <summary>
    /// The context at the time of the action.
    /// </summary>
    public MenuContext? Context { get; set; }
}
