using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a bookmark/saved item relationship between a Profile and a Post.
/// Allows users to save posts for later viewing.
/// </summary>
public class ProfileBookmark : BaseEntity
{
    /// <summary>
    /// The profile that created this bookmark
    /// </summary>
    public virtual Guid ProfileId { get; set; }
    
    /// <summary>
    /// Navigation property to the profile
    /// </summary>
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// The post that was bookmarked
    /// </summary>
    public virtual Guid PostId { get; set; }
    
    /// <summary>
    /// Navigation property to the post
    /// </summary>
    public virtual Post Post { get; set; } = null!;

    /// <summary>
    /// Optional note or reason for bookmarking
    /// </summary>
    [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters")]
    public virtual string? Note { get; set; }
}
