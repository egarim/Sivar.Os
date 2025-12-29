using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Sivar.Os.Shared.Framework.Actions;

/// <summary>
/// Default implementation of IActionDispatcher.
/// Routes actions to registered handlers based on action ID patterns.
/// </summary>
public class ActionDispatcher : IActionDispatcher
{
    private readonly ConcurrentDictionary<string, List<IActionHandler>> _handlers = new();
    private readonly ConcurrentDictionary<string, Func<ActionContext, Task<ActionResult>>> _delegateHandlers = new();
    private readonly ILogger<ActionDispatcher>? _logger;

    public ActionDispatcher(ILogger<ActionDispatcher>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public event EventHandler<ActionContext>? BeforeAction;
    
    /// <inheritdoc/>
    public event EventHandler<ActionExecutedEventArgs>? AfterAction;

    /// <inheritdoc/>
    public async Task<ActionResult> DispatchAsync(ActionContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger?.LogDebug("Dispatching action: {ActionId} for {EntityType}:{EntityId}", 
                context.ActionId, context.EntityType, context.EntityId);
            
            // Raise before event
            BeforeAction?.Invoke(this, context);
            
            // Try delegate handlers first (simple registrations)
            if (_delegateHandlers.TryGetValue(context.ActionId, out var delegateHandler))
            {
                var result = await delegateHandler(context);
                RaiseAfterAction(context, result, stopwatch.Elapsed);
                return result;
            }
            
            // Try pattern-matched handlers
            var handler = FindHandler(context);
            if (handler != null)
            {
                var result = await handler.ExecuteAsync(context, cancellationToken);
                RaiseAfterAction(context, result, stopwatch.Elapsed);
                return result;
            }
            
            // No handler found
            _logger?.LogWarning("No handler found for action: {ActionId}", context.ActionId);
            var notFoundResult = ActionResult.Fail($"No handler registered for action: {context.ActionId}");
            RaiseAfterAction(context, notFoundResult, stopwatch.Elapsed);
            return notFoundResult;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing action: {ActionId}", context.ActionId);
            var errorResult = ActionResult.Fail($"Action failed: {ex.Message}");
            RaiseAfterAction(context, errorResult, stopwatch.Elapsed);
            return errorResult;
        }
    }

    /// <inheritdoc/>
    public Task<ActionResult> DispatchAsync(
        string actionId, 
        string entityType, 
        Guid entityId, 
        CancellationToken cancellationToken = default)
    {
        var context = new ActionContext
        {
            ActionId = actionId,
            EntityType = entityType,
            EntityId = entityId
        };
        
        return DispatchAsync(context, cancellationToken);
    }

    /// <inheritdoc/>
    public void RegisterHandler(IActionHandler handler)
    {
        foreach (var actionId in handler.HandledActions)
        {
            var key = actionId.ToLowerInvariant();
            _handlers.AddOrUpdate(
                key,
                _ => new List<IActionHandler> { handler },
                (_, list) =>
                {
                    list.Add(handler);
                    list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                    return list;
                });
        }
        
        _logger?.LogDebug("Registered handler for actions: {Actions}", 
            string.Join(", ", handler.HandledActions));
    }

    /// <inheritdoc/>
    public void RegisterHandler(string actionId, Func<ActionContext, Task<ActionResult>> handler)
    {
        _delegateHandlers[actionId.ToLowerInvariant()] = handler;
        _logger?.LogDebug("Registered delegate handler for action: {ActionId}", actionId);
    }

    /// <inheritdoc/>
    public bool HasHandler(string actionId)
    {
        var key = actionId.ToLowerInvariant();
        
        if (_delegateHandlers.ContainsKey(key))
            return true;
            
        if (_handlers.ContainsKey(key))
            return true;
            
        // Check for wildcard patterns
        var entityType = actionId.Split('.').FirstOrDefault() ?? "";
        var wildcardKey = $"{entityType}.*";
        
        return _handlers.ContainsKey(wildcardKey);
    }

    private IActionHandler? FindHandler(ActionContext context)
    {
        var actionId = context.ActionId.ToLowerInvariant();
        
        // Try exact match first
        if (_handlers.TryGetValue(actionId, out var handlers))
        {
            foreach (var handler in handlers)
            {
                if (handler.CanHandle(context))
                    return handler;
            }
        }
        
        // Try wildcard pattern (e.g., "post.*")
        var entityType = actionId.Split('.').FirstOrDefault() ?? "";
        var wildcardKey = $"{entityType}.*";
        
        if (_handlers.TryGetValue(wildcardKey, out var wildcardHandlers))
        {
            foreach (var handler in wildcardHandlers)
            {
                if (handler.CanHandle(context))
                    return handler;
            }
        }
        
        return null;
    }

    private void RaiseAfterAction(ActionContext context, ActionResult result, TimeSpan duration)
    {
        AfterAction?.Invoke(this, new ActionExecutedEventArgs
        {
            Context = context,
            Result = result,
            Duration = duration
        });
    }
}
