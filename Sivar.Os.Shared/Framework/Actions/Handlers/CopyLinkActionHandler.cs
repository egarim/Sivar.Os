namespace Sivar.Os.Shared.Framework.Actions.Handlers;

/// <summary>
/// Handles copy link actions for various entity types.
/// Copies the entity URL to clipboard.
/// </summary>
public class CopyLinkActionHandler : ActionHandlerBase
{
    public override IEnumerable<string> HandledActions => new[]
    {
        "post.copylink",
        "profile.copylink",
        "blog.copylink"
    };
    
    public override Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken = default)
    {
        var baseUrl = context.GetParameter<string>("baseUrl") ?? "";
        var url = BuildFullUrl(baseUrl, context);
        
        return Task.FromResult(ActionResult.Ok(
            message: "Link copied to clipboard",
            data: new CopyLinkData { Url = url }
        ));
    }
    
    private static string BuildFullUrl(string baseUrl, ActionContext context)
    {
        var path = context.EntityType.ToLower() switch
        {
            "post" => $"/post/{context.EntityId}",
            "profile" => $"/profile/{context.EntityId}",
            "blog" => $"/blog/{context.EntityId}",
            _ => $"/{context.EntityType.ToLower()}/{context.EntityId}"
        };
        
        return $"{baseUrl.TrimEnd('/')}{path}";
    }
}

/// <summary>
/// Data for copy link actions.
/// </summary>
public class CopyLinkData
{
    public string Url { get; set; } = string.Empty;
}
