namespace Sivar.Os.Shared.Framework.Actions;

/// <summary>
/// Base class for action handlers that provides common functionality.
/// </summary>
public abstract class ActionHandlerBase : IActionHandler
{
    /// <inheritdoc/>
    public abstract IEnumerable<string> HandledActions { get; }
    
    /// <inheritdoc/>
    public virtual int Priority => 0;
    
    /// <inheritdoc/>
    public virtual bool CanHandle(ActionContext context)
    {
        var actionId = context.ActionId.ToLowerInvariant();
        
        foreach (var handled in HandledActions)
        {
            var pattern = handled.ToLowerInvariant();
            
            // Exact match
            if (pattern == actionId)
                return true;
                
            // Wildcard match (e.g., "post.*" matches "post.edit")
            if (pattern.EndsWith(".*"))
            {
                var prefix = pattern[..^2]; // Remove ".*"
                if (actionId.StartsWith(prefix + "."))
                    return true;
            }
        }
        
        return false;
    }
    
    /// <inheritdoc/>
    public abstract Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates that the user is authenticated.
    /// </summary>
    protected ActionResult? ValidateAuthenticated(ActionContext context)
    {
        if (!context.IsAuthenticated)
        {
            return ActionResult.Fail("You must be logged in to perform this action.", "Error_NotAuthenticated");
        }
        return null;
    }
    
    /// <summary>
    /// Validates that the user is the owner of the entity.
    /// </summary>
    protected ActionResult? ValidateOwnership(ActionContext context)
    {
        if (!context.IsOwner)
        {
            return ActionResult.Fail("You don't have permission to perform this action.", "Error_NotOwner");
        }
        return null;
    }
    
    /// <summary>
    /// Validates that the user is authenticated and is the owner.
    /// </summary>
    protected ActionResult? ValidateAuthenticatedOwner(ActionContext context)
    {
        return ValidateAuthenticated(context) ?? ValidateOwnership(context);
    }
}
