using System.ComponentModel.DataAnnotations;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Stores AI model pricing information for cost tracking and billing.
/// Prices are stored per 1 million tokens for consistency with OpenAI pricing.
/// </summary>
public class AiModelPricing : BaseEntity
{
    /// <summary>
    /// Model identifier as used in API calls (e.g., "gpt-4o-mini", "text-embedding-3-small")
    /// </summary>
    [Required]
    [StringLength(100)]
    public virtual string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name (e.g., "GPT-4o Mini", "Text Embedding 3 Small")
    /// </summary>
    [Required]
    [StringLength(150)]
    public virtual string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Provider name (e.g., "OpenAI", "Anthropic", "Ollama", "Azure")
    /// </summary>
    [Required]
    [StringLength(50)]
    public virtual string Provider { get; set; } = "OpenAI";

    /// <summary>
    /// Type of AI model
    /// </summary>
    public virtual AiModelType ModelType { get; set; } = AiModelType.Chat;

    /// <summary>
    /// Quality/cost tier of the model (Free, Low, Medium, High, Premium)
    /// Helps quickly identify budget vs premium options
    /// </summary>
    public virtual AiModelTier Tier { get; set; } = AiModelTier.Low;

    /// <summary>
    /// Cost per 1 million INPUT tokens in USD (Standard tier)
    /// </summary>
    public virtual decimal InputCostPer1M { get; set; }

    /// <summary>
    /// Cost per 1 million OUTPUT tokens in USD (Standard tier)
    /// For embedding models, this is typically 0
    /// </summary>
    public virtual decimal OutputCostPer1M { get; set; }

    /// <summary>
    /// Cost per 1 million INPUT tokens in USD (Batch tier - typically 50% of standard)
    /// Null if batch processing is not available
    /// </summary>
    public virtual decimal? BatchInputCostPer1M { get; set; }

    /// <summary>
    /// Cost per 1 million OUTPUT tokens in USD (Batch tier)
    /// </summary>
    public virtual decimal? BatchOutputCostPer1M { get; set; }

    /// <summary>
    /// Whether this model is currently active and available for use
    /// </summary>
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is the default model for its type
    /// </summary>
    public virtual bool IsDefault { get; set; } = false;

    /// <summary>
    /// Context window size in tokens (e.g., 128000 for gpt-4o)
    /// </summary>
    public virtual int? ContextWindowSize { get; set; }

    /// <summary>
    /// Maximum output tokens supported
    /// </summary>
    public virtual int? MaxOutputTokens { get; set; }

    /// <summary>
    /// Embedding dimensions (for embedding models only)
    /// </summary>
    public virtual int? EmbeddingDimensions { get; set; }

    /// <summary>
    /// Sort order for display in UI
    /// </summary>
    public virtual int SortOrder { get; set; } = 0;

    /// <summary>
    /// Additional notes or description
    /// </summary>
    [StringLength(500)]
    public virtual string? Notes { get; set; }

    /// <summary>
    /// Date when pricing was last updated
    /// </summary>
    public virtual DateTime PricingUpdatedAt { get; set; } = DateTime.UtcNow;

    // ========================================
    // COMPUTED COST METHODS
    // ========================================

    /// <summary>
    /// Calculate the cost for a given number of input tokens
    /// </summary>
    /// <param name="inputTokens">Number of input tokens</param>
    /// <param name="useBatchPricing">Use batch pricing if available</param>
    /// <returns>Cost in USD</returns>
    public decimal CalculateInputCost(int inputTokens, bool useBatchPricing = false)
    {
        var rate = useBatchPricing && BatchInputCostPer1M.HasValue 
            ? BatchInputCostPer1M.Value 
            : InputCostPer1M;
        return (inputTokens / 1_000_000m) * rate;
    }

    /// <summary>
    /// Calculate the cost for a given number of output tokens
    /// </summary>
    /// <param name="outputTokens">Number of output tokens</param>
    /// <param name="useBatchPricing">Use batch pricing if available</param>
    /// <returns>Cost in USD</returns>
    public decimal CalculateOutputCost(int outputTokens, bool useBatchPricing = false)
    {
        var rate = useBatchPricing && BatchOutputCostPer1M.HasValue 
            ? BatchOutputCostPer1M.Value 
            : OutputCostPer1M;
        return (outputTokens / 1_000_000m) * rate;
    }

    /// <summary>
    /// Calculate total cost for an interaction
    /// </summary>
    /// <param name="inputTokens">Number of input tokens</param>
    /// <param name="outputTokens">Number of output tokens</param>
    /// <param name="useBatchPricing">Use batch pricing if available</param>
    /// <returns>Total cost in USD</returns>
    public decimal CalculateTotalCost(int inputTokens, int outputTokens, bool useBatchPricing = false)
    {
        return CalculateInputCost(inputTokens, useBatchPricing) + 
               CalculateOutputCost(outputTokens, useBatchPricing);
    }

    /// <summary>
    /// Get a formatted cost string for display
    /// </summary>
    /// <returns>Formatted pricing string</returns>
    public string GetPricingDisplay()
    {
        if (ModelType == AiModelType.Embedding)
        {
            return $"${InputCostPer1M:F2}/1M tokens";
        }
        return $"${InputCostPer1M:F2} in / ${OutputCostPer1M:F2} out per 1M";
    }

    /// <summary>
    /// Get tier display with emoji indicator
    /// </summary>
    public string GetTierDisplay()
    {
        return Tier switch
        {
            AiModelTier.Free => "🆓 Free",
            AiModelTier.Low => "💚 Low Cost",
            AiModelTier.Medium => "💛 Medium",
            AiModelTier.High => "🔶 High",
            AiModelTier.Premium => "💎 Premium",
            _ => Tier.ToString()
        };
    }

    /// <summary>
    /// Get a short summary for display in lists
    /// </summary>
    public string GetSummaryDisplay()
    {
        var tierEmoji = Tier switch
        {
            AiModelTier.Free => "🆓",
            AiModelTier.Low => "💚",
            AiModelTier.Medium => "💛",
            AiModelTier.High => "🔶",
            AiModelTier.Premium => "💎",
            _ => ""
        };
        return $"{tierEmoji} {DisplayName} - {GetPricingDisplay()}";
    }
}
