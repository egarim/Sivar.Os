namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Tracks AI chat token usage per interaction for auditing and billing purposes.
/// Each record represents a single chat message/response pair and its token consumption.
/// </summary>
public class ChatTokenUsage : BaseEntity
{
    /// <summary>
    /// The profile that consumed the tokens
    /// </summary>
    public virtual Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// Optional conversation/session ID to group related messages
    /// </summary>
    public virtual Guid? ConversationId { get; set; }

    /// <summary>
    /// Number of tokens in the input/prompt
    /// </summary>
    public virtual int InputTokens { get; set; }

    /// <summary>
    /// Number of tokens in the output/response
    /// </summary>
    public virtual int OutputTokens { get; set; }

    /// <summary>
    /// Total tokens used (input + output)
    /// </summary>
    public virtual int TotalTokens { get; set; }

    /// <summary>
    /// The AI model used for this interaction (e.g., "gpt-4o-mini", "llama3")
    /// </summary>
    public virtual string? ModelName { get; set; }

    /// <summary>
    /// The detected intent/agent that handled this request
    /// </summary>
    public virtual string? Intent { get; set; }

    /// <summary>
    /// Brief summary of the user's message (first ~100 chars for reference)
    /// </summary>
    public virtual string? MessagePreview { get; set; }

    /// <summary>
    /// Estimated cost in USD based on model pricing (optional)
    /// </summary>
    public virtual decimal? EstimatedCost { get; set; }
}
