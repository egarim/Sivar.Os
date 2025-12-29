using System.Collections.Concurrent;

namespace Sivar.Os.Shared.Framework.Menu;

/// <summary>
/// Thread-safe implementation of IMenuRegistry.
/// Aggregates menu items from multiple providers and filters by context.
/// </summary>
public class MenuRegistry : IMenuRegistry
{
    private readonly ConcurrentDictionary<string, IMenuItem> _items = new();
    private readonly ConcurrentBag<IMenuProvider> _providers = new();
    private bool _providersLoaded = false;
    private readonly object _loadLock = new();

    /// <inheritdoc/>
    public IEnumerable<IMenuItem> GetMenuItems(MenuContext context)
    {
        EnsureProvidersLoaded();
        
        return _items.Values
            .Where(item => MatchesEntityType(item, context.EntityType))
            .Where(item => MatchesPlatform(item, context.Platform))
            .Where(item => IsVisible(item, context))
            .OrderBy(item => item.Group ?? "zzz") // Group null items last
            .ThenBy(item => item.Order);
    }

    /// <inheritdoc/>
    public IEnumerable<IMenuItem> GetAllItems(string entityType)
    {
        EnsureProvidersLoaded();
        
        return _items.Values
            .Where(item => MatchesEntityType(item, entityType))
            .OrderBy(item => item.Order);
    }

    /// <inheritdoc/>
    public IMenuItem? GetItem(string id)
    {
        EnsureProvidersLoaded();
        return _items.TryGetValue(id, out var item) ? item : null;
    }

    /// <inheritdoc/>
    public void RegisterProvider(IMenuProvider provider)
    {
        _providers.Add(provider);
        
        // Load items from this provider
        foreach (var item in provider.GetMenuItems())
        {
            _items.TryAdd(item.Id, item);
        }
    }

    /// <inheritdoc/>
    public void RegisterItem(string entityType, IMenuItem item)
    {
        // Prefix with entity type to ensure unique IDs
        var key = item.Id.StartsWith($"{entityType.ToLower()}.") 
            ? item.Id 
            : $"{entityType.ToLower()}.{item.Id}";
            
        _items.TryAdd(key, item);
    }

    /// <inheritdoc/>
    public bool UnregisterItem(string id)
    {
        return _items.TryRemove(id, out _);
    }

    private void EnsureProvidersLoaded()
    {
        if (_providersLoaded) return;
        
        lock (_loadLock)
        {
            if (_providersLoaded) return;
            
            foreach (var provider in _providers.OrderBy(p => p.Priority))
            {
                foreach (var item in provider.GetMenuItems())
                {
                    _items.TryAdd(item.Id, item);
                }
            }
            
            _providersLoaded = true;
        }
    }

    private static bool MatchesEntityType(IMenuItem item, string entityType)
    {
        // Item ID format: "entitytype.action" (e.g., "post.edit", "profile.block")
        var prefix = $"{entityType.ToLower()}.";
        return item.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesPlatform(IMenuItem item, PlatformType platform)
    {
        if (item.Platform == PlatformType.All)
            return true;
            
        return item.Platform == platform;
    }

    private static bool IsVisible(IMenuItem item, MenuContext context)
    {
        if (item.IsVisible == null)
            return true;
            
        try
        {
            return item.IsVisible(context);
        }
        catch
        {
            // If visibility check fails, hide the item
            return false;
        }
    }
}
