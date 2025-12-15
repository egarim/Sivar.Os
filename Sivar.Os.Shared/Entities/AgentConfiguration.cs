using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Stores AI agent configuration in database for dynamic management.
/// Allows editing system prompts, enabling/disabling tools, and creating
/// specialized agents without code changes.
/// </summary>
public class AgentConfiguration : BaseEntity
{
    /// <summary>
    /// Unique identifier for this agent (e.g., "sivar-main", "business-search", "government-help")
    /// Used as the agent name when building AI agents
    /// </summary>
    [Required]
    [StringLength(50)]
    public virtual string AgentKey { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for admin UI display
    /// </summary>
    [Required]
    [StringLength(100)]
    public virtual string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of agent's purpose and capabilities
    /// </summary>
    [StringLength(500)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// System prompt/instructions for the agent
    /// This is the main prompt that defines agent behavior
    /// </summary>
    [Required]
    public virtual string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Which AI provider to use: "ollama", "openai", "azure-openai"
    /// </summary>
    [StringLength(50)]
    public virtual string Provider { get; set; } = "ollama";

    /// <summary>
    /// Model ID for the provider (e.g., "llama3.2:latest", "gpt-4o", "gpt-4o-mini")
    /// </summary>
    [StringLength(100)]
    public virtual string ModelId { get; set; } = "llama3.2:latest";

    /// <summary>
    /// Temperature for AI responses (0.0 - 2.0)
    /// Lower = more focused/deterministic, Higher = more creative/varied
    /// </summary>
    public virtual double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens for response generation
    /// </summary>
    public virtual int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// JSON array of enabled tool/function names for this agent
    /// Example: ["SearchProfiles", "SearchPosts", "GetPostDetails"]
    /// </summary>
    [Column(TypeName = "jsonb")]
    public virtual JsonDocument? EnabledTools { get; set; }

    /// <summary>
    /// JSON object with additional provider-specific settings
    /// Example: {"topP": 0.9, "frequencyPenalty": 0.5}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public virtual JsonDocument? ProviderSettings { get; set; }

    /// <summary>
    /// JSON array of intent patterns (regex) that route messages to this agent
    /// Example: [".*pizza.*", ".*restaurante.*", ".*comida.*"]
    /// </summary>
    [Column(TypeName = "jsonb")]
    public virtual JsonDocument? IntentPatterns { get; set; }

    /// <summary>
    /// Priority when multiple agents match an intent (higher = preferred)
    /// The main catch-all agent should have priority 0
    /// </summary>
    public virtual int Priority { get; set; } = 0;

    /// <summary>
    /// Is this agent currently active and available for use?
    /// </summary>
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Version number for tracking prompt changes
    /// Incremented each time SystemPrompt is modified
    /// </summary>
    public virtual int Version { get; set; } = 1;

    /// <summary>
    /// Profile ID of who last updated this configuration
    /// </summary>
    public virtual Guid? UpdatedByProfileId { get; set; }

    /// <summary>
    /// Optional A/B test variant identifier (A, B, C, etc.)
    /// Null means this is the default configuration
    /// </summary>
    [StringLength(10)]
    public virtual string? AbTestVariant { get; set; }

    /// <summary>
    /// Percentage of traffic for this A/B test variant (0-100)
    /// Only used when AbTestVariant is set
    /// </summary>
    public virtual int AbTestWeight { get; set; } = 100;

    #region Helper Methods

    /// <summary>
    /// Get the list of enabled tool names from the EnabledTools JSON
    /// </summary>
    public List<string> GetEnabledToolNames()
    {
        if (EnabledTools == null)
            return new List<string>();

        try
        {
            var tools = new List<string>();
            foreach (var element in EnabledTools.RootElement.EnumerateArray())
            {
                var toolName = element.GetString();
                if (!string.IsNullOrEmpty(toolName))
                    tools.Add(toolName);
            }
            return tools;
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Get the list of intent patterns from the IntentPatterns JSON
    /// </summary>
    public List<string> GetIntentPatterns()
    {
        if (IntentPatterns == null)
            return new List<string>();

        try
        {
            var patterns = new List<string>();
            foreach (var element in IntentPatterns.RootElement.EnumerateArray())
            {
                var pattern = element.GetString();
                if (!string.IsNullOrEmpty(pattern))
                    patterns.Add(pattern);
            }
            return patterns;
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Set enabled tools from a list of tool names
    /// </summary>
    public void SetEnabledTools(IEnumerable<string> toolNames)
    {
        var json = JsonSerializer.Serialize(toolNames);
        EnabledTools = JsonDocument.Parse(json);
    }

    /// <summary>
    /// Set intent patterns from a list of regex patterns
    /// </summary>
    public void SetIntentPatterns(IEnumerable<string> patterns)
    {
        var json = JsonSerializer.Serialize(patterns);
        IntentPatterns = JsonDocument.Parse(json);
    }

    #endregion
}
