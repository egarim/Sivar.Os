using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for AI cost calculations and tracking
/// </summary>
public interface IAiCostService
{
    /// <summary>
    /// Calculate cost for a specific interaction
    /// </summary>
    /// <param name="modelId">Model identifier (e.g., "gpt-4o-mini")</param>
    /// <param name="inputTokens">Number of input tokens</param>
    /// <param name="outputTokens">Number of output tokens</param>
    /// <param name="useBatchPricing">Use batch pricing if available</param>
    /// <returns>Cost breakdown</returns>
    Task<AiCostResult> CalculateCostAsync(string modelId, int inputTokens, int outputTokens, bool useBatchPricing = false);

    /// <summary>
    /// Get all available model pricing
    /// </summary>
    Task<List<AiModelPricing>> GetAllPricingAsync();

    /// <summary>
    /// Get pricing for a specific model type
    /// </summary>
    Task<List<AiModelPricing>> GetPricingByTypeAsync(AiModelType modelType);

    /// <summary>
    /// Get the recommended cost-efficient model for a use case
    /// </summary>
    Task<AiModelPricing?> GetRecommendedModelAsync(AiModelType modelType);

    /// <summary>
    /// Get total costs for a profile within a date range
    /// </summary>
    Task<AiCostSummary> GetProfileCostSummaryAsync(Guid profileId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get system-wide cost summary
    /// </summary>
    Task<AiCostSummary> GetSystemCostSummaryAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Seed default pricing data
    /// </summary>
    Task SeedDefaultPricingAsync();
}

/// <summary>
/// Result of a cost calculation
/// </summary>
public class AiCostResult
{
    public string ModelId { get; set; } = string.Empty;
    public string ModelDisplayName { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
    public decimal InputCost { get; set; }
    public decimal OutputCost { get; set; }
    public decimal TotalCost => InputCost + OutputCost;
    public bool UsedBatchPricing { get; set; }
    public decimal InputRatePer1M { get; set; }
    public decimal OutputRatePer1M { get; set; }

    /// <summary>
    /// Format cost for display
    /// </summary>
    public string TotalCostDisplay => TotalCost < 0.01m 
        ? $"${TotalCost:F6}" 
        : $"${TotalCost:F4}";
}

/// <summary>
/// Summary of costs over a period
/// </summary>
public class AiCostSummary
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalInteractions { get; set; }
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public long TotalTokens => TotalInputTokens + TotalOutputTokens;
    public decimal TotalCost { get; set; }
    public decimal AverageCostPerInteraction => TotalInteractions > 0 
        ? TotalCost / TotalInteractions 
        : 0;
    public Dictionary<string, ModelUsageSummary> ByModel { get; set; } = new();
}

/// <summary>
/// Usage summary per model
/// </summary>
public class ModelUsageSummary
{
    public string ModelId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Interactions { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public decimal TotalCost { get; set; }
    public double PercentageOfTotal { get; set; }
}
