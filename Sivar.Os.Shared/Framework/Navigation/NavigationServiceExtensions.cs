using Microsoft.Extensions.DependencyInjection;
using Sivar.Os.Shared.Framework.Actions;
using Sivar.Os.Shared.Framework.Actions.Handlers;
using Sivar.Os.Shared.Framework.Menu;

namespace Sivar.Os.Shared.Framework.Navigation;

/// <summary>
/// Extension methods for registering navigation services.
/// </summary>
public static class NavigationServiceExtensions
{
    /// <summary>
    /// Adds the complete framework services to the service collection.
    /// Registers navigation, menu, and action frameworks.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="registerCoreItems">Whether to register core items (default: true).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNavigationFramework(
        this IServiceCollection services, 
        bool registerCoreItems = true)
    {
        // Register the navigation registry as singleton
        services.AddSingleton<INavigationRegistry>(sp =>
        {
            var registry = new NavigationRegistry();
            
            if (registerCoreItems)
            {
                registry.RegisterRange(CoreNavigationItems.All);
            }
            
            return registry;
        });
        
        // Register the menu framework
        services.AddMenuFramework();
        
        // Register the action framework with core handlers
        services.AddActionFramework();
        services.AddActionHandler<ShareActionHandler>();
        services.AddActionHandler<CopyLinkActionHandler>();
        services.AddActionHandler<NavigationActionHandler>();
        
        return services;
    }
    
    /// <summary>
    /// Adds the navigation framework services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the registry after creation.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNavigationFramework(
        this IServiceCollection services,
        Action<INavigationRegistry> configure)
    {
        services.AddSingleton<INavigationRegistry>(sp =>
        {
            var registry = new NavigationRegistry();
            registry.RegisterRange(CoreNavigationItems.All);
            configure(registry);
            return registry;
        });
        
        // Register the menu framework
        services.AddMenuFramework();
        
        // Register the action framework with core handlers
        services.AddActionFramework();
        services.AddActionHandler<ShareActionHandler>();
        services.AddActionHandler<CopyLinkActionHandler>();
        services.AddActionHandler<NavigationActionHandler>();
        
        return services;
    }
}
