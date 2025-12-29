using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sivar.Os.Shared.Framework.Actions;

/// <summary>
/// Extension methods for registering action framework services.
/// </summary>
public static class ActionServiceExtensions
{
    /// <summary>
    /// Adds the action framework services to the DI container.
    /// </summary>
    public static IServiceCollection AddActionFramework(this IServiceCollection services)
    {
        // Register the action dispatcher as a singleton
        services.AddSingleton<IActionDispatcher>(sp =>
        {
            var logger = sp.GetService<ILogger<ActionDispatcher>>();
            var dispatcher = new ActionDispatcher(logger);
            
            // Register all IActionHandler implementations from DI
            var handlers = sp.GetServices<IActionHandler>();
            foreach (var handler in handlers)
            {
                dispatcher.RegisterHandler(handler);
            }
            
            return dispatcher;
        });
        
        return services;
    }
    
    /// <summary>
    /// Registers an action handler implementation.
    /// </summary>
    public static IServiceCollection AddActionHandler<THandler>(this IServiceCollection services)
        where THandler : class, IActionHandler
    {
        services.AddSingleton<IActionHandler, THandler>();
        return services;
    }
}
