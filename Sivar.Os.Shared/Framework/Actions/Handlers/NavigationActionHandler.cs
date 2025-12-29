namespace Sivar.Os.Shared.Framework.Actions.Handlers;

/// <summary>
/// Handles navigation actions that redirect to edit pages.
/// </summary>
public class NavigationActionHandler : ActionHandlerBase
{
    public override IEnumerable<string> HandledActions => new[]
    {
        "post.edit",
        "profile.edit",
        "profile.settings",
        "blog.edit"
    };
    
    public override Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken = default)
    {
        // Validate ownership for edit actions
        if (context.ActionId.EndsWith(".edit") || context.ActionId == "profile.settings")
        {
            var validation = ValidateAuthenticatedOwner(context);
            if (validation != null)
                return Task.FromResult(validation);
        }
        
        var navigateTo = GetNavigationUrl(context);
        
        return Task.FromResult(ActionResult.OkWithNavigation(navigateTo));
    }
    
    private static string GetNavigationUrl(ActionContext context)
    {
        return context.ActionId switch
        {
            "post.edit" => $"/post/{context.EntityId}/edit",
            "profile.edit" => $"/profile/{context.EntityId}/edit",
            "profile.settings" => "/settings",
            "blog.edit" => $"/blog/{context.EntityId}/edit",
            _ => "/"
        };
    }
}
