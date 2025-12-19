using System.Collections.ObjectModel;
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
    public virtual Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// Title/name of the conversation for easy identification
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public virtual string Title { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the last message in this conversation
    /// </summary>
    public virtual DateTime LastMessageAt { get; set; }

    /// <summary>
    /// Indicates if this conversation is currently active/selected
    /// </summary>
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Collection of messages in this conversation
    /// Uses ObservableCollection for XAF/EF Core change tracking compatibility
    /// </summary>
    public virtual ICollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();

    /// <summary>
    /// Collection of saved results from this conversation
    /// Uses ObservableCollection for XAF/EF Core change tracking compatibility
    /// </summary>
    public virtual ICollection<SavedResult> SavedResults { get; set; } = new ObservableCollection<SavedResult>();

    // ========================================
    // Token Usage & Cost Totals (denormalized for performance)
    // Updated incrementally when ChatTokenUsage is saved
    // ========================================

    /// <summary>
    /// Total input tokens consumed in this conversation
    /// </summary>
    public virtual int TotalInputTokens { get; set; }

    /// <summary>
    /// Total output tokens consumed in this conversation
    /// </summary>
    public virtual int TotalOutputTokens { get; set; }

    /// <summary>
    /// Total tokens (input + output) consumed in this conversation
    /// </summary>
    public virtual int TotalTokens { get; set; }

    /// <summary>
    /// Total estimated cost in USD for this conversation
    /// </summary>
    public virtual decimal TotalCost { get; set; }
}
