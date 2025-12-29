namespace Sivar.Os.Shared.Framework.Actions.Handlers;

/// <summary>
/// Handles share actions for various entity types.
/// Uses the Web Share API on supported platforms.
/// </summary>
public class ShareActionHandler : ActionHandlerBase
{
    public override IEnumerable<string> HandledActions => new[]
    {
        "post.share",
        "profile.share",
        "blog.share",
        "comment.share"
    };
    
    public override Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken = default)
    {
        // The actual share implementation will be done in the UI layer
        // This handler just validates and returns the share data
        
        var shareUrl = context.GetParameter<string>("url") ?? BuildShareUrl(context);
        var shareTitle = context.GetParameter<string>("title") ?? $"Check out this {context.EntityType.ToLower()}";
        var shareText = context.GetParameter<string>("text");
        
        return Task.FromResult(ActionResult.Ok(data: new ShareData
        {
            Url = shareUrl,
            Title = shareTitle,
            Text = shareText
        }));
    }
    
    private static string BuildShareUrl(ActionContext context)
    {
        // Build URL based on entity type
        return context.EntityType.ToLower() switch
        {
            "post" => $"/post/{context.EntityId}",
            "profile" => $"/profile/{context.EntityId}",
            "blog" => $"/blog/{context.EntityId}",
            _ => $"/{context.EntityType.ToLower()}/{context.EntityId}"
        };
    }
}

/// <summary>
/// Data for share actions.
/// </summary>
public class ShareData
{
    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? Text { get; set; }
}
