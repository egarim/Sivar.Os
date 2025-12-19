namespace Sivar.Os.Shared.Enums;

/// <summary>
/// Quality/cost tier for AI models.
/// Helps quickly identify premium vs budget model options.
/// </summary>
public enum AiModelTier
{
    /// <summary>
    /// Free models - local inference (Ollama, etc.)
    /// No API costs, only compute/infrastructure costs
    /// </summary>
    Free = 0,

    /// <summary>
    /// Low-cost models - budget-friendly options
    /// Examples: gpt-4o-mini, gpt-4.1-nano, text-embedding-3-small
    /// Typical cost: < $1/1M tokens
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium-cost models - balanced cost/performance
    /// Examples: gpt-4.1-mini, gpt-3.5-turbo
    /// Typical cost: $1-5/1M tokens
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High-cost models - premium quality
    /// Examples: gpt-4o, gpt-4.1, o3
    /// Typical cost: $5-20/1M tokens
    /// </summary>
    High = 3,

    /// <summary>
    /// Premium/Enterprise models - highest quality, specialized
    /// Examples: o1-pro, gpt-5-pro, fine-tuned models
    /// Typical cost: > $20/1M tokens
    /// </summary>
    Premium = 4
}
