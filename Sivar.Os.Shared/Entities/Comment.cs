using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Comments on posts in the activity stream
/// </summary>
public class Comment : BaseEntity
{
    /// <summary>
    /// The post this comment belongs to
    /// </summary>
    public virtual Guid PostId { get; set; }
    public virtual Post Post { get; set; } = null!;

    /// <summary>
    /// The profile that made this comment
    /// </summary>
    public virtual Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// Parent comment for threaded discussions (null for top-level comments)
    /// </summary>
    public virtual Guid? ParentCommentId { get; set; }
    public virtual Comment? ParentComment { get; set; }

    /// <summary>
    /// Replies to this comment
    /// </summary>
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();

    /// <summary>
    /// The comment content
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public virtual string Content { get; set; } = string.Empty;

    /// <summary>
    /// Language of the comment
    /// </summary>
    [StringLength(5)]
    public virtual string Language { get; set; } = "en";

    /// <summary>
    /// Reactions on this comment
    /// </summary>
    public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

    /// <summary>
    /// Indicates if this comment has been edited
    /// </summary>
    public virtual bool IsEdited { get; set; } = false;

    /// <summary>
    /// Date when the comment was last edited
    /// </summary>
    public virtual DateTime? EditedAt { get; set; }
}