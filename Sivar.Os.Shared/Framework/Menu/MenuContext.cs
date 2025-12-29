using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Shared.Framework.Menu;

/// <summary>
/// Context passed to menu items for visibility and enablement checks.
/// Contains information about the target entity, current user, and platform.
/// </summary>
public class MenuContext
{
    /// <summary>
    /// The type of entity this menu is for.
    /// Example: "Post", "Profile", "Comment", "Blog"
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the target entity.
    /// </summary>
    public Guid EntityId { get; set; }
    
    /// <summary>
    /// The target entity object (for complex checks).
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
    /// The owner profile of the entity (if applicable).
    /// </summary>
    public Guid? OwnerProfileId { get; set; }
    
    /// <summary>
    /// The platform the app is running on.
    /// </summary>
    public PlatformType Platform { get; set; } = PlatformType.Web;
    
    /// <summary>
    /// User's roles (from authentication claims).
    /// </summary>
    public string[] Roles { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Additional custom data for extensibility.
    /// </summary>
    public Dictionary<string, object?> CustomData { get; set; } = new();

    /// <summary>
    /// Creates a menu context for a post.
    /// </summary>
    public static MenuContext ForPost(Guid postId, Guid ownerProfileId, ActiveProfileDto? activeProfile)
    {
        return new MenuContext
        {
            EntityType = "Post",
            EntityId = postId,
            OwnerProfileId = ownerProfileId,
            ActiveProfile = activeProfile,
            IsAuthenticated = activeProfile != null,
            IsOwner = activeProfile?.Id == ownerProfileId
        };
    }

    /// <summary>
    /// Creates a menu context for a comment.
    /// </summary>
    public static MenuContext ForComment(Guid commentId, Guid ownerProfileId, ActiveProfileDto? activeProfile)
    {
        return new MenuContext
        {
            EntityType = "Comment",
            EntityId = commentId,
            OwnerProfileId = ownerProfileId,
            ActiveProfile = activeProfile,
            IsAuthenticated = activeProfile != null,
            IsOwner = activeProfile?.Id == ownerProfileId
        };
    }

    /// <summary>
    /// Creates a menu context for a profile.
    /// </summary>
    public static MenuContext ForProfile(Guid profileId, ActiveProfileDto? activeProfile)
    {
        return new MenuContext
        {
            EntityType = "Profile",
            EntityId = profileId,
            OwnerProfileId = profileId,
            ActiveProfile = activeProfile,
            IsAuthenticated = activeProfile != null,
            IsOwner = activeProfile?.Id == profileId
        };
    }

    /// <summary>
    /// Creates a menu context for a blog.
    /// </summary>
    public static MenuContext ForBlog(Guid blogId, Guid ownerProfileId, ActiveProfileDto? activeProfile)
    {
        return new MenuContext
        {
            EntityType = "Blog",
            EntityId = blogId,
            OwnerProfileId = ownerProfileId,
            ActiveProfile = activeProfile,
            IsAuthenticated = activeProfile != null,
            IsOwner = activeProfile?.Id == ownerProfileId
        };
    }
}
