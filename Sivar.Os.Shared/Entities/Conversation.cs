using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a persistent chat conversation between a profile and the AI assistant
/// </summary>
public class Conversation : BaseEntity
{
    /// <summary>
    /// The profile that owns this conversation
    /// </summary>
    public Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// Title/name of the conversation for easy identification
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the last message in this conversation
    /// </summary>
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if this conversation is currently active/selected
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Collection of messages in this conversation
    /// </summary>
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    /// <summary>
    /// Collection of saved results from this conversation
    /// </summary>
    public virtual ICollection<SavedResult> SavedResults { get; set; } = new List<SavedResult>();
}
