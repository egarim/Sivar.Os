using Microsoft.Extensions.DependencyInjection;

namespace Sivar.Os.Shared.Framework.Menu;

/// <summary>
/// Extension methods for registering menu framework services.
/// </summary>
public static class MenuServiceExtensions
{
    /// <summary>
    /// Adds the menu framework services to the DI container.
    /// Registers the menu registry and core menu items.
    /// </summary>
    public static IServiceCollection AddMenuFramework(this IServiceCollection services)
    {
        // Register the menu registry as a singleton
        services.AddSingleton<IMenuRegistry>(sp =>
        {
            var registry = new MenuRegistry();
            
            // Register core menu items
            foreach (var item in CoreMenuItems.Post.All)
            {
                registry.RegisterItem("Post", item);
            }
            
            foreach (var item in CoreMenuItems.Comment.All)
            {
                registry.RegisterItem("Comment", item);
            }
            
            foreach (var item in CoreMenuItems.Profile.All)
            {
                registry.RegisterItem("Profile", item);
            }
            
            foreach (var item in CoreMenuItems.Blog.All)
            {
                registry.RegisterItem("Blog", item);
            }
            
            return registry;
        });
        
        return services;
    }
    
    /// <summary>
    /// Registers a custom menu provider.
    /// </summary>
    public static IServiceCollection AddMenuProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IMenuProvider
    {
        services.AddSingleton<IMenuProvider, TProvider>();
        return services;
    }
}
