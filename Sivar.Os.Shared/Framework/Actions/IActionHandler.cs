namespace Sivar.Os.Shared.Framework.Actions;

/// <summary>
/// Interface for action handlers that process specific action types.
/// Implement this interface to handle one or more action IDs.
/// </summary>
public interface IActionHandler
{
    /// <summary>
    /// The action IDs this handler can process.
    /// Example: ["post.edit", "post.delete"] or ["post.*"] for wildcards.
    /// </summary>
    IEnumerable<string> HandledActions { get; }
    
    /// <summary>
    /// Priority for ordering when multiple handlers exist for the same action.
    /// Lower values = higher priority (runs first).
    /// </summary>
    int Priority => 0;
    
    /// <summary>
    /// Whether this handler can handle the given action.
    /// </summary>
    bool CanHandle(ActionContext context);
    
    /// <summary>
    /// Executes the action and returns the result.
    /// </summary>
    Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken = default);
}
