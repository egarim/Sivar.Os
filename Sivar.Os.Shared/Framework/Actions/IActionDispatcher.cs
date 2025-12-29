namespace Sivar.Os.Shared.Framework.Actions;

/// <summary>
/// Central dispatcher for executing actions.
/// Components dispatch actions here instead of handling them inline.
/// </summary>
public interface IActionDispatcher
{
    /// <summary>
    /// Dispatches an action to the appropriate handler.
    /// </summary>
    Task<ActionResult> DispatchAsync(ActionContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dispatches an action by ID with entity info.
    /// </summary>
    Task<ActionResult> DispatchAsync(
        string actionId, 
        string entityType, 
        Guid entityId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers an action handler.
    /// </summary>
    void RegisterHandler(IActionHandler handler);
    
    /// <summary>
    /// Registers a simple action handler using a delegate.
    /// </summary>
    void RegisterHandler(string actionId, Func<ActionContext, Task<ActionResult>> handler);
    
    /// <summary>
    /// Checks if a handler exists for the given action.
    /// </summary>
    bool HasHandler(string actionId);
    
    /// <summary>
    /// Event raised before an action is executed.
    /// </summary>
    event EventHandler<ActionContext>? BeforeAction;
    
    /// <summary>
    /// Event raised after an action is executed.
    /// </summary>
    event EventHandler<ActionExecutedEventArgs>? AfterAction;
}

/// <summary>
/// Event args for action executed events.
/// </summary>
public class ActionExecutedEventArgs : EventArgs
{
    public ActionContext Context { get; set; } = new();
    public ActionResult Result { get; set; } = new();
    public TimeSpan Duration { get; set; }
}
