namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for chat bot settings response
/// </summary>
public record ChatBotSettingsDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Unique key for this setting
    /// </summary>
    public string Key { get; init; } = "default";

    /// <summary>
    /// Language/culture code
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The welcome message shown when chat opens
    /// </summary>
    public string WelcomeMessage { get; init; } = string.Empty;

    /// <summary>
    /// Short tagline shown in chat header
    /// </summary>
    public string? HeaderTagline { get; init; }

    /// <summary>
    /// Bot name displayed in header
    /// </summary>
    public string BotName { get; init; } = "Sivar AI Assistant";

    /// <summary>
    /// Quick action buttons with full relational data
    /// </summary>
    public List<QuickActionDto> QuickActionItems { get; init; } = new();

    /// <summary>
    /// System prompt for the AI agent
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Error fallback message
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Message shown while AI is thinking
    /// </summary>
    public string? ThinkingMessage { get; init; }
}

/// <summary>
/// DTO for creating/updating chat bot settings
/// </summary>
public record CreateChatBotSettingsDto
{
    public string Key { get; init; } = "default";
    public string? Culture { get; init; }
    public string WelcomeMessage { get; init; } = string.Empty;
    public string? HeaderTagline { get; init; }
    public string BotName { get; init; } = "Sivar AI Assistant";
    public List<string>? QuickActions { get; init; }
    public string? SystemPrompt { get; init; }
    public int Priority { get; init; } = 0;
    public string? RegionCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ThinkingMessage { get; init; }
}

/// <summary>
/// DTO for updating chat bot settings
/// </summary>
public record UpdateChatBotSettingsDto
{
    public string? WelcomeMessage { get; init; }
    public string? HeaderTagline { get; init; }
    public string? BotName { get; init; }
    public List<string>? QuickActions { get; init; }
    public string? SystemPrompt { get; init; }
    public int? Priority { get; init; }
    public bool? IsActive { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ThinkingMessage { get; init; }
}
