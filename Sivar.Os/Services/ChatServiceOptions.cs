namespace Sivar.Os.Services;

/// <summary>
/// Configuration options for AI chat service
/// </summary>
public class ChatServiceOptions
{
    public const string SectionName = "ChatService";

    /// <summary>
    /// AI provider: "ollama", "openai", or "openrouter"
    /// </summary>
    public string Provider { get; set; } = "ollama";

    /// <summary>
    /// Maximum number of messages allowed per conversation
    /// </summary>
    public int MaxMessagesPerConversation { get; set; } = 1000;

    /// <summary>
    /// Default response type if not specified
    /// </summary>
    public string DefaultResponseType { get; set; } = "SimpleText";

    /// <summary>
    /// Maximum tokens for AI response
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// Temperature for AI responses (0.0 - 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Rate limit: max messages per minute
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 20;

    /// <summary>
    /// Ollama-specific settings
    /// </summary>
    public OllamaSettings Ollama { get; set; } = new();

    /// <summary>
    /// OpenAI-specific settings
    /// </summary>
    public OpenAISettings OpenAI { get; set; } = new();

    /// <summary>
    /// OpenRouter-specific settings
    /// </summary>
    public OpenRouterSettings OpenRouter { get; set; } = new();

    public class OllamaSettings
    {
        public string Endpoint { get; set; } = "http://127.0.0.1:11434";
        public string ModelId { get; set; } = "llama3.2:latest";
    }

    public class OpenAISettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelId { get; set; } = "gpt-4o";
        public string? OrganizationId { get; set; }
    }

    public class OpenRouterSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public string ModelId { get; set; } = "meta-llama/llama-3.3-70b-instruct";
        public string? SiteName { get; set; } = "Sivar.Os";
        public string? SiteUrl { get; set; } = "https://sivar.lat";
    }
}
