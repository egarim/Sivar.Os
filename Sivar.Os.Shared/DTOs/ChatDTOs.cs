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
    
    /// <summary>
    /// Messages in this conversation (populated when loading a specific conversation)
    /// </summary>
    public List<ChatMessageDto>? Messages { get; init; }
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

    /// <summary>
    /// Optional location context for proximity-aware searches.
    /// When provided, search results will prioritize nearby results and include distances.
    /// </summary>
    public ChatLocationContext? Location { get; init; }
}

/// <summary>
/// Location context for chat sessions.
/// Used for proximity-aware searches and "cerca de mí" queries.
/// </summary>
public record ChatLocationContext
{
    /// <summary>
    /// Latitude of user's location
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// Longitude of user's location
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// City name (detected or selected)
    /// </summary>
    [StringLength(100)]
    public string? City { get; init; }

    /// <summary>
    /// State/Department name
    /// </summary>
    [StringLength(100)]
    public string? State { get; init; }

    /// <summary>
    /// Country name
    /// </summary>
    [StringLength(100)]
    public string? Country { get; init; }

    /// <summary>
    /// Display name for UI (e.g., "San Salvador, El Salvador")
    /// </summary>
    [StringLength(200)]
    public string? DisplayName { get; init; }

    /// <summary>
    /// How the location was obtained: "gps", "selected", "detected", "default"
    /// </summary>
    [StringLength(20)]
    public string Source { get; init; } = "unknown";

    /// <summary>
    /// Accuracy of GPS coordinates in meters (if from GPS)
    /// </summary>
    public double? AccuracyMeters { get; init; }

    /// <summary>
    /// Whether this location context is valid and can be used for searches
    /// </summary>
    public bool IsValid => Latitude.HasValue && Longitude.HasValue || !string.IsNullOrEmpty(City);
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

    /// <summary>
    /// Structured search results from the AI response
    /// When present, the UI should render these as graphical cards
    /// </summary>
    public SearchResultsCollectionDto? SearchResults { get; init; }

    /// <summary>
    /// Whether this response contains structured search results
    /// </summary>
    public bool HasStructuredResults => SearchResults?.HasResults == true;

    // ========================================
    // TOKEN USAGE INFORMATION
    // ========================================

    /// <summary>
    /// Number of tokens used in the input/prompt
    /// </summary>
    public int? InputTokens { get; init; }

    /// <summary>
    /// Number of tokens used in the output/response
    /// </summary>
    public int? OutputTokens { get; init; }

    /// <summary>
    /// Total tokens used for this interaction
    /// </summary>
    public int? TotalTokens { get; init; }

    /// <summary>
    /// Remaining tokens in the user's current allowance period
    /// </summary>
    public int? TokensRemaining { get; init; }

    /// <summary>
    /// When the token allowance period will reset
    /// </summary>
    public DateTime? AllowanceResetsAt { get; init; }
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
