using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a single message in a chat conversation (user or AI)
/// </summary>
public class ChatMessage : BaseEntity
{
    /// <summary>
    /// The conversation this message belongs to
    /// </summary>
    public virtual Guid ConversationId { get; set; }
    public virtual Conversation Conversation { get; set; } = null!;

    /// <summary>
    /// Role of the message sender: "user" or "assistant"
    /// </summary>
    [Required]
    [StringLength(20)]
    public virtual string Role { get; set; } = string.Empty;

    /// <summary>
    /// Text content of the message (always text for user, may be text or structured for assistant)
    /// </summary>
    [Required]
    public virtual string Content { get; set; } = string.Empty;

    /// <summary>
    /// Structured JSON response from AI (null for user messages)
    /// Stores the typed response object serialized as JSON
    /// </summary>
    public virtual string? StructuredResponse { get; set; }

    /// <summary>
    /// Order/sequence of this message in the conversation (0-based)
    /// </summary>
    public virtual int MessageOrder { get; set; }

    /// <summary>
    /// Structured search results associated with this message
    /// Enables rendering as graphical cards with call-to-actions
    /// </summary>
    public virtual ICollection<SearchResult> SearchResults { get; set; } = new ObservableCollection<SearchResult>();
}
