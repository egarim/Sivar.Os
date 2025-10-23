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
    public Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// The conversation this result came from
    /// </summary>
    public Guid ConversationId { get; set; }
    public virtual Conversation Conversation { get; set; } = null!;

    /// <summary>
    /// Type of result (e.g., "ProfileSearch", "PostSearch", "FollowAction", "PostDetails")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ResultType { get; set; } = string.Empty;

    /// <summary>
    /// JSON data of the saved result (profile, post, action confirmation, etc.)
    /// </summary>
    [Required]
    public string ResultData { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly description of what was saved
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
}
