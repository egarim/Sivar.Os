using Sivar.Os.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Reactions (likes, loves, etc.) on posts and comments
/// </summary>
public class Reaction : BaseEntity
{
    /// <summary>
    /// The profile that made this reaction
    /// </summary>
    public virtual Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// The post this reaction is on (null if reacting to a comment)
    /// </summary>
    public virtual Guid? PostId { get; set; }
    public virtual Post? Post { get; set; }

    /// <summary>
    /// The comment this reaction is on (null if reacting to a post)
    /// </summary>
    public virtual Guid? CommentId { get; set; }
    public virtual Comment? Comment { get; set; }

    /// <summary>
    /// Type of reaction
    /// </summary>
    public virtual ReactionType ReactionType { get; set; }

    /// <summary>
    /// Validates that reaction is on either post or comment, not both
    /// </summary>
    public bool IsValid()
    {
        return PostId.HasValue && !CommentId.HasValue || 
               !PostId.HasValue && CommentId.HasValue;
    }
}