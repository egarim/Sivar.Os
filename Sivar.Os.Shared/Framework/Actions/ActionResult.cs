namespace Sivar.Os.Shared.Framework.Actions;

/// <summary>
/// Represents the result of an action execution.
/// </summary>
public class ActionResult
{
    /// <summary>
    /// Whether the action was successful.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Optional message (success or error).
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// Localization key for the message.
    /// </summary>
    public string? MessageKey { get; set; }
    
    /// <summary>
    /// Optional data returned by the action.
    /// </summary>
    public object? Data { get; set; }
    
    /// <summary>
    /// Whether the UI should refresh after this action.
    /// </summary>
    public bool RequiresRefresh { get; set; }
    
    /// <summary>
    /// Optional navigation URL after action completes.
    /// </summary>
    public string? NavigateTo { get; set; }
    
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ActionResult Ok(string? message = null, object? data = null)
    {
        return new ActionResult
        {
            Success = true,
            Message = message,
            Data = data
        };
    }
    
    /// <summary>
    /// Creates a successful result with refresh.
    /// </summary>
    public static ActionResult OkWithRefresh(string? message = null)
    {
        return new ActionResult
        {
            Success = true,
            Message = message,
            RequiresRefresh = true
        };
    }
    
    /// <summary>
    /// Creates a successful result with navigation.
    /// </summary>
    public static ActionResult OkWithNavigation(string navigateTo, string? message = null)
    {
        return new ActionResult
        {
            Success = true,
            Message = message,
            NavigateTo = navigateTo
        };
    }
    
    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ActionResult Fail(string message, string? messageKey = null)
    {
        return new ActionResult
        {
            Success = false,
            Message = message,
            MessageKey = messageKey
        };
    }
}
