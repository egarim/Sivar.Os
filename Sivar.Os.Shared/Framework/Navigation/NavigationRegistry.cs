using System.Collections.Concurrent;

namespace Sivar.Os.Shared.Framework.Navigation;

/// <summary>
/// Default implementation of INavigationRegistry.
/// Thread-safe registry for navigation items.
/// </summary>
public class NavigationRegistry : INavigationRegistry
{
    private readonly ConcurrentDictionary<string, INavigationItem> _items = new();
    
    /// <inheritdoc />
    public IEnumerable<INavigationItem> GetVisibleItems(NavContext context)
    {
        return _items.Values
            .Where(item => IsItemVisible(item, context))
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Title);
    }
    
    /// <inheritdoc />
    public IEnumerable<INavigationItem> GetAllItems()
    {
        return _items.Values
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Title);
    }
    
    /// <inheritdoc />
    public INavigationItem? GetItem(string id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }
    
    /// <inheritdoc />
    public void Register(INavigationItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Id))
            throw new ArgumentException("Navigation item must have an ID.", nameof(item));
            
        _items[item.Id] = item;
    }
    
    /// <inheritdoc />
    public void RegisterRange(IEnumerable<INavigationItem> items)
    {
        foreach (var item in items)
        {
            Register(item);
        }
    }
    
    /// <inheritdoc />
    public bool Unregister(string id)
    {
        return _items.TryRemove(id, out _);
    }
    
    /// <inheritdoc />
    public bool Contains(string id)
    {
        return _items.ContainsKey(id);
    }
    
    /// <summary>
    /// Determines if a navigation item should be visible given the context.
    /// </summary>
    private bool IsItemVisible(INavigationItem item, NavContext context)
    {
        // Check authentication requirement
        if (item.RequiresAuth && !context.IsAuthenticated)
            return false;
        
        // Check platform
        if (item.Platform != PlatformType.All && item.Platform != context.Platform)
            return false;
        
        // Check required roles
        if (item.RequiredRoles?.Length > 0)
        {
            if (!item.RequiredRoles.Any(role => context.Roles.Contains(role, StringComparer.OrdinalIgnoreCase)))
                return false;
        }
        
        // Check required profile types
        if (item.RequiredProfileTypes?.Length > 0)
        {
            var profileTypeName = context.ActiveProfile?.ProfileType?.Name;
            if (string.IsNullOrEmpty(profileTypeName) || 
                !item.RequiredProfileTypes.Contains(profileTypeName, StringComparer.OrdinalIgnoreCase))
                return false;
        }
        
        // Check custom visibility predicate
        if (item.IsVisible != null)
        {
            try
            {
                if (!item.IsVisible(context))
                    return false;
            }
            catch
            {
                // If predicate throws, hide the item
                return false;
            }
        }
        
        return true;
    }
}
