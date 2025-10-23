using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for creating a new conversation
/// </summary>
public record CreateConversationDto
{
    /// <summary>
    /// ID of the profile creating the conversation
    /// </summary>
    [Required]
    public Guid ProfileId { get; init; }

    /// <summary>
    /// Title for the conversation
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;
}

/// <summary>
/// DTO for conversation data
/// </summary>
public record ConversationDto
{
    public Guid Id { get; init; }
    public Guid ProfileId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime LastMessageAt { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public int MessageCount { get; init; }
}

/// <summary>
/// DTO for updating a conversation
/// </summary>
public record UpdateConversationDto
{
    /// <summary>
    /// New title for the conversation
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;
}

/// <summary>
/// DTO for sending a message to AI
/// </summary>
public record SendMessageDto
{
    /// <summary>
    /// ID of the conversation
    /// </summary>
    [Required]
    public Guid ConversationId { get; init; }

    /// <summary>
    /// User's message content (text only)
    /// </summary>
    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// DTO for a chat message
/// </summary>
public record ChatMessageDto
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? StructuredResponse { get; init; }
    public int MessageOrder { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for AI chat response with structured data
/// </summary>
public record ChatResponseDto
{
    /// <summary>
    /// The user's message that was sent
    /// </summary>
    public ChatMessageDto UserMessage { get; init; } = null!;

    /// <summary>
    /// The AI's response message
    /// </summary>
    public ChatMessageDto AssistantMessage { get; init; } = null!;

    /// <summary>
    /// Conversation ID
    /// </summary>
    public Guid ConversationId { get; init; }
}

/// <summary>
/// DTO for creating a saved result
/// </summary>
public record CreateSavedResultDto
{
    /// <summary>
    /// ID of the conversation this result came from
    /// </summary>
    [Required]
    public Guid ConversationId { get; init; }

    /// <summary>
    /// Type of result
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ResultType { get; init; } = string.Empty;

    /// <summary>
    /// JSON data of the result
    /// </summary>
    [Required]
    public string ResultData { get; init; } = string.Empty;

    /// <summary>
    /// Description of the saved result
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// DTO for a saved result
/// </summary>
public record SavedResultDto
{
    public Guid Id { get; init; }
    public Guid ProfileId { get; init; }
    public Guid ConversationId { get; init; }
    public string ResultType { get; init; } = string.Empty;
    public string ResultData { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
