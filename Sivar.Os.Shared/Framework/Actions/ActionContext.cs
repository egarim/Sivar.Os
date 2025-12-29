using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Framework.Actions;

/// <summary>
/// Context passed to action handlers with all necessary information.
/// </summary>
public class ActionContext
{
    /// <summary>
    /// The action ID being executed.
    /// Example: "post.edit", "post.delete", "profile.block"
    /// </summary>
    public string ActionId { get; set; } = string.Empty;
    
    /// <summary>
    /// The type of entity this action is for.
    /// Example: "Post", "Profile", "Comment", "Blog"
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the target entity.
    /// </summary>
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// The target entity object (for complex operations).
    /// </summary>
    public object? Entity { get; set; }
    
    /// <summary>
    /// Whether the current user is the owner of the entity.
    /// </summary>
    public bool IsOwner { get; set; }
    
    /// <summary>
    /// Whether the current user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; set; }
    
    /// <summary>
    /// The currently active profile.
    /// </summary>
    public ActiveProfileDto? ActiveProfile { get; set; }
    
    /// <summary>
    /// The owner profile ID of the entity.
    /// </summary>
    public Guid? OwnerProfileId { get; set; }
    
    /// <summary>
    /// The platform the app is running on.
    /// </summary>
    public PlatformType Platform { get; set; } = PlatformType.Web;
    
    /// <summary>
    /// Additional parameters for the action.
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();
    
    /// <summary>
    /// Gets a parameter value with type conversion.
    /// </summary>
    public T? GetParameter<T>(string key, T? defaultValue = default)
    {
        if (Parameters.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Sets a parameter value.
    /// </summary>
    public ActionContext WithParameter(string key, object? value)
    {
        Parameters[key] = value;
        return this;
    }
    
    /// <summary>
    /// Creates a context for a post action.
    /// </summary>
    public static ActionContext ForPost(string actionId, Guid postId, Guid ownerProfileId, ActiveProfileDto? activeProfile)
    {
        return new ActionContext
        {
            ActionId = actionId,
            EntityType = "Post",
            EntityId = postId,
            OwnerProfileId = ownerProfileId,
            ActiveProfile = activeProfile,
            IsAuthenticated = activeProfile != null,
            IsOwner = activeProfile?.Id == ownerProfileId
        };
    }
    
    /// <summary>
    /// Creates a context for a comment action.
    /// </summary>
    public static ActionContext ForComment(string actionId, Guid commentId, Guid ownerProfileId, ActiveProfileDto? activeProfile)
    {
        return new ActionContext
        {
            ActionId = actionId,
            EntityType = "Comment",
            EntityId = commentId,
            OwnerProfileId = ownerProfileId,
            ActiveProfile = activeProfile,
            IsAuthenticated = activeProfile != null,
            IsOwner = activeProfile?.Id == ownerProfileId
        };
    }
    
    /// <summary>
    /// Creates a context for a profile action.
    /// </summary>
    public static ActionContext ForProfile(string actionId, Guid profileId, ActiveProfileDto? activeProfile)
    {
        return new ActionContext
        {
            ActionId = actionId,
            EntityType = "Profile",
            EntityId = profileId,
            OwnerProfileId = profileId,
            ActiveProfile = activeProfile,
            IsAuthenticated = activeProfile != null,
            IsOwner = activeProfile?.Id == profileId
        };
    }
    
    /// <summary>
    /// Creates a context for a blog action.
    /// </summary>
    public static ActionContext ForBlog(string actionId, Guid blogId, Guid ownerProfileId, ActiveProfileDto? activeProfile)
    {
        return new ActionContext
        {
            ActionId = actionId,
            EntityType = "Blog",
            EntityId = blogId,
            OwnerProfileId = ownerProfileId,
            ActiveProfile = activeProfile,
            IsAuthenticated = activeProfile != null,
            IsOwner = activeProfile?.Id == ownerProfileId
        };
    }
}
