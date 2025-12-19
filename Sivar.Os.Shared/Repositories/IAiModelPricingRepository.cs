using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for AI model pricing operations
/// </summary>
public interface IAiModelPricingRepository : IBaseRepository<AiModelPricing>
{
    /// <summary>
    /// Get pricing by model ID
    /// </summary>
    Task<AiModelPricing?> GetByModelIdAsync(string modelId);

    /// <summary>
    /// Get all active models
    /// </summary>
    Task<List<AiModelPricing>> GetActiveModelsAsync();

    /// <summary>
    /// Get models by type
    /// </summary>
    Task<List<AiModelPricing>> GetByModelTypeAsync(AiModelType modelType);

    /// <summary>
    /// Get the default model for a type
    /// </summary>
    Task<AiModelPricing?> GetDefaultModelAsync(AiModelType modelType);

    /// <summary>
    /// Get models by provider
    /// </summary>
    Task<List<AiModelPricing>> GetByProviderAsync(string provider);
}
