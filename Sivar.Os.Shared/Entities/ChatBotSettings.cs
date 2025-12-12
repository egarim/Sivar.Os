using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Configurable chat bot settings for welcome messages, quick actions, and system prompts.
/// Phase 0.5: Replaces hardcoded welcome messages with database-driven configuration.
/// </summary>
public class ChatBotSettings : BaseEntity
{
    /// <summary>
    /// Unique key for this setting (e.g., "default", "es-SV", "en-US")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Key { get; set; } = "default";

    /// <summary>
    /// Language/culture code (e.g., "es", "en")
    /// </summary>
    [StringLength(10)]
    public string? Culture { get; set; }

    /// <summary>
    /// The welcome message shown when chat opens.
    /// Supports markdown formatting.
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string WelcomeMessage { get; set; } = string.Empty;

    /// <summary>
    /// Short tagline shown in chat header (e.g., "Always here to help you explore")
    /// </summary>
    [StringLength(100)]
    public string? HeaderTagline { get; set; }

    /// <summary>
    /// Bot name displayed in header
    /// </summary>
    [StringLength(50)]
    public string BotName { get; set; } = "Sivar AI Assistant";

    /// <summary>
    /// Quick action buttons as JSON array.
    /// e.g., ["🍕 Buscar comida", "🏛️ Trámites", "📍 Cerca de mí", "🎉 Eventos"]
    /// </summary>
    public string? QuickActionsJson { get; set; }

    /// <summary>
    /// System prompt for the AI agent.
    /// Used to customize the AI's behavior and knowledge.
    /// </summary>
    [StringLength(5000)]
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Whether this setting is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority for selection (higher = preferred when multiple match)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Optional region code (e.g., "SV", "GT") for region-specific settings
    /// </summary>
    [StringLength(10)]
    public string? RegionCode { get; set; }

    /// <summary>
    /// Error fallback message when something goes wrong
    /// </summary>
    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Message shown while AI is "thinking"
    /// </summary>
    [StringLength(200)]
    public string? ThinkingMessage { get; set; }
}
