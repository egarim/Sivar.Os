namespace Sivar.Os.Shared.Framework.Menu;

/// <summary>
/// Represents a single menu item that can appear in context menus,
/// action menus, or dropdown menus throughout the application.
/// </summary>
public interface IMenuItem
{
    /// <summary>
    /// Unique identifier for this menu item.
    /// Example: "post.edit", "post.delete", "profile.block"
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Display title (fallback if localization key not found).
    /// </summary>
    string Title { get; }
    
    /// <summary>
    /// Localization resource key for the title.
    /// Example: "MenuItem_Edit", "MenuItem_Delete"
    /// </summary>
    string? TitleKey { get; }
    
    /// <summary>
    /// Icon identifier (Material Icons name).
    /// Example: "Edit", "Delete", "Share"
    /// </summary>
    string? Icon { get; }
    
    /// <summary>
    /// Display order within the menu group (lower = first).
    /// </summary>
    int Order { get; }
    
    /// <summary>
    /// Optional group name for visual separation.
    /// Example: "primary", "danger", "share"
    /// </summary>
    string? Group { get; }
    
    /// <summary>
    /// Color theme for the menu item.
    /// Example: "Default", "Primary", "Error" (for delete actions)
    /// </summary>
    string? Color { get; }
    
    /// <summary>
    /// The action ID to dispatch when this menu item is clicked.
    /// </summary>
    string ActionId { get; }
    
    /// <summary>
    /// Whether this menu item requires confirmation before executing.
    /// </summary>
    bool RequiresConfirmation { get; }
    
    /// <summary>
    /// Confirmation message key (if RequiresConfirmation is true).
    /// </summary>
    string? ConfirmationMessageKey { get; }
    
    /// <summary>
    /// Function to determine if this item is visible for a given context.
    /// </summary>
    Func<MenuContext, bool>? IsVisible { get; }
    
    /// <summary>
    /// Function to determine if this item is enabled for a given context.
    /// </summary>
    Func<MenuContext, bool>? IsEnabled { get; }
    
    /// <summary>
    /// Platform filter (which platforms show this item).
    /// </summary>
    PlatformType Platform { get; }
}
