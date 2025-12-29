namespace Sivar.Os.Shared.Framework.Menu;

/// <summary>
/// Default implementation of IMenuItem with fluent builder pattern.
/// </summary>
public class MenuItem : IMenuItem
{
    public string Id { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? TitleKey { get; private set; }
    public string? Icon { get; private set; }
    public int Order { get; private set; }
    public string? Group { get; private set; }
    public string? Color { get; private set; }
    public string ActionId { get; private set; } = string.Empty;
    public bool RequiresConfirmation { get; private set; }
    public string? ConfirmationMessageKey { get; private set; }
    public Func<MenuContext, bool>? IsVisible { get; private set; }
    public Func<MenuContext, bool>? IsEnabled { get; private set; }
    public PlatformType Platform { get; private set; } = PlatformType.All;

    private MenuItem() { }

    /// <summary>
    /// Creates a new menu item with the specified ID and action.
    /// </summary>
    public static MenuItem Create(string id, string actionId)
    {
        return new MenuItem
        {
            Id = id,
            ActionId = actionId
        };
    }

    /// <summary>
    /// Sets the display title and optional localization key.
    /// </summary>
    public MenuItem WithTitle(string title, string? titleKey = null)
    {
        Title = title;
        TitleKey = titleKey ?? $"MenuItem_{Id.Replace(".", "_")}";
        return this;
    }

    /// <summary>
    /// Sets the icon (Material Icons name).
    /// </summary>
    public MenuItem WithIcon(string icon)
    {
        Icon = icon;
        return this;
    }

    /// <summary>
    /// Sets the display order.
    /// </summary>
    public MenuItem WithOrder(int order)
    {
        Order = order;
        return this;
    }

    /// <summary>
    /// Sets the menu group for visual separation.
    /// </summary>
    public MenuItem InGroup(string group)
    {
        Group = group;
        return this;
    }

    /// <summary>
    /// Sets the color theme.
    /// </summary>
    public MenuItem WithColor(string color)
    {
        Color = color;
        return this;
    }

    /// <summary>
    /// Marks this item as requiring confirmation.
    /// </summary>
    public MenuItem WithConfirmation(string? messageKey = null)
    {
        RequiresConfirmation = true;
        ConfirmationMessageKey = messageKey;
        return this;
    }

    /// <summary>
    /// Sets the visibility predicate.
    /// </summary>
    public MenuItem VisibleWhen(Func<MenuContext, bool> predicate)
    {
        IsVisible = predicate;
        return this;
    }

    /// <summary>
    /// Sets the enabled predicate.
    /// </summary>
    public MenuItem EnabledWhen(Func<MenuContext, bool> predicate)
    {
        IsEnabled = predicate;
        return this;
    }

    /// <summary>
    /// Restricts this item to specific platforms.
    /// </summary>
    public MenuItem ForPlatform(PlatformType platform)
    {
        Platform = platform;
        return this;
    }

    /// <summary>
    /// Makes this item visible only to the owner of the target entity.
    /// </summary>
    public MenuItem OwnerOnly()
    {
        IsVisible = ctx => ctx.IsOwner;
        return this;
    }

    /// <summary>
    /// Makes this item visible only when NOT the owner (for report, block, etc).
    /// </summary>
    public MenuItem NonOwnerOnly()
    {
        IsVisible = ctx => !ctx.IsOwner;
        return this;
    }

    /// <summary>
    /// Makes this item visible only to authenticated users.
    /// </summary>
    public MenuItem AuthenticatedOnly()
    {
        IsVisible = ctx => ctx.IsAuthenticated;
        return this;
    }
}
