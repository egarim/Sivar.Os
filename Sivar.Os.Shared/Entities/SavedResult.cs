using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a saved AI chat result that a user wants to keep for later reference
/// </summary>
public class SavedResult : BaseEntity
{
    /// <summary>
    /// The profile that saved this result
    /// </summary>
    public virtual Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// The conversation this result came from
    /// </summary>
    public virtual Guid ConversationId { get; set; }
    public virtual Conversation Conversation { get; set; } = null!;

    /// <summary>
    /// Type of result (e.g., "ProfileSearch", "PostSearch", "FollowAction", "PostDetails")
    /// </summary>
    [Required]
    [StringLength(50)]
    public virtual string ResultType { get; set; } = string.Empty;

    /// <summary>
    /// JSON data of the saved result (profile, post, action confirmation, etc.)
    /// </summary>
    [Required]
    public virtual string ResultData { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly description of what was saved
    /// </summary>
    [Required]
    [StringLength(500)]
    public virtual string Description { get; set; } = string.Empty;
}
