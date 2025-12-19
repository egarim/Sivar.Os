using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service for AI cost calculations and tracking
/// </summary>
public class AiCostService : IAiCostService
{
    private readonly IAiModelPricingRepository _pricingRepository;
    private readonly IChatTokenUsageRepository _tokenUsageRepository;
    private readonly SivarDbContext _dbContext;
    private readonly ILogger<AiCostService> _logger;

    // Cache for model pricing to reduce database calls
    private Dictionary<string, AiModelPricing>? _pricingCache;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private const int CacheMinutes = 30;

    public AiCostService(
        IAiModelPricingRepository pricingRepository,
        IChatTokenUsageRepository tokenUsageRepository,
        SivarDbContext dbContext,
        ILogger<AiCostService> logger)
    {
        _pricingRepository = pricingRepository;
        _tokenUsageRepository = tokenUsageRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AiCostResult> CalculateCostAsync(
        string modelId, 
        int inputTokens, 
        int outputTokens, 
        bool useBatchPricing = false)
    {
        var pricing = await GetPricingFromCacheAsync(modelId);
        
        if (pricing == null)
        {
            _logger.LogWarning("[AiCostService] Model pricing not found for {ModelId}, using default rates", modelId);
            
            // Return with zero cost if model not found
            return new AiCostResult
            {
                ModelId = modelId,
                ModelDisplayName = modelId,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                InputCost = 0,
                OutputCost = 0,
                UsedBatchPricing = false,
                InputRatePer1M = 0,
                OutputRatePer1M = 0
            };
        }

        var inputCost = pricing.CalculateInputCost(inputTokens, useBatchPricing);
        var outputCost = pricing.CalculateOutputCost(outputTokens, useBatchPricing);

        var inputRate = useBatchPricing && pricing.BatchInputCostPer1M.HasValue
            ? pricing.BatchInputCostPer1M.Value
            : pricing.InputCostPer1M;
        var outputRate = useBatchPricing && pricing.BatchOutputCostPer1M.HasValue
            ? pricing.BatchOutputCostPer1M.Value
            : pricing.OutputCostPer1M;

        return new AiCostResult
        {
            ModelId = modelId,
            ModelDisplayName = pricing.DisplayName,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            InputCost = inputCost,
            OutputCost = outputCost,
            UsedBatchPricing = useBatchPricing,
            InputRatePer1M = inputRate,
            OutputRatePer1M = outputRate
        };
    }

    /// <inheritdoc />
    public async Task<List<AiModelPricing>> GetAllPricingAsync()
    {
        return await _pricingRepository.GetActiveModelsAsync();
    }

    /// <inheritdoc />
    public async Task<List<AiModelPricing>> GetPricingByTypeAsync(AiModelType modelType)
    {
        return await _pricingRepository.GetByModelTypeAsync(modelType);
    }

    /// <inheritdoc />
    public async Task<AiModelPricing?> GetRecommendedModelAsync(AiModelType modelType)
    {
        // First try to get the default model
        var defaultModel = await _pricingRepository.GetDefaultModelAsync(modelType);
        if (defaultModel != null)
            return defaultModel;

        // Otherwise, get the cheapest active model of that type
        var models = await _pricingRepository.GetByModelTypeAsync(modelType);
        return models
            .OrderBy(m => m.InputCostPer1M + m.OutputCostPer1M)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<AiCostSummary> GetProfileCostSummaryAsync(
        Guid profileId, 
        DateTime startDate, 
        DateTime endDate)
    {
        var usageRecords = await _dbContext.ChatTokenUsages
            .Where(u => u.ProfileId == profileId && 
                       u.CreatedAt >= startDate && 
                       u.CreatedAt <= endDate)
            .ToListAsync();

        return await BuildCostSummaryAsync(usageRecords, startDate, endDate);
    }

    /// <inheritdoc />
    public async Task<AiCostSummary> GetSystemCostSummaryAsync(
        DateTime startDate, 
        DateTime endDate)
    {
        var usageRecords = await _dbContext.ChatTokenUsages
            .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
            .ToListAsync();

        return await BuildCostSummaryAsync(usageRecords, startDate, endDate);
    }

    /// <inheritdoc />
    public async Task SeedDefaultPricingAsync()
    {
        _logger.LogInformation("[AiCostService] Seeding default AI model pricing data...");

        var existingModels = await _pricingRepository.GetActiveModelsAsync();
        if (existingModels.Any())
        {
            _logger.LogInformation("[AiCostService] Pricing data already exists, skipping seed");
            return;
        }

        var defaultPricing = GetDefaultPricingData();

        foreach (var pricing in defaultPricing)
        {
            await _pricingRepository.AddAsync(pricing);
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("[AiCostService] Seeded {Count} AI model pricing records", defaultPricing.Count);
        
        // Invalidate cache
        _cacheExpiry = DateTime.MinValue;
    }

    // ========================================
    // PRIVATE METHODS
    // ========================================

    private async Task<AiModelPricing?> GetPricingFromCacheAsync(string modelId)
    {
        if (_pricingCache == null || DateTime.UtcNow > _cacheExpiry)
        {
            var allPricing = await _pricingRepository.GetActiveModelsAsync();
            _pricingCache = allPricing.ToDictionary(p => p.ModelId, p => p);
            _cacheExpiry = DateTime.UtcNow.AddMinutes(CacheMinutes);
        }

        return _pricingCache.TryGetValue(modelId, out var pricing) ? pricing : null;
    }

    private async Task<AiCostSummary> BuildCostSummaryAsync(
        List<ChatTokenUsage> usageRecords, 
        DateTime startDate, 
        DateTime endDate)
    {
        var summary = new AiCostSummary
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalInteractions = usageRecords.Count,
            TotalInputTokens = usageRecords.Sum(u => u.InputTokens),
            TotalOutputTokens = usageRecords.Sum(u => u.OutputTokens)
        };

        // Group by model and calculate costs
        var byModel = usageRecords
            .GroupBy(u => u.ModelName ?? "unknown")
            .ToList();

        decimal totalCost = 0;

        foreach (var group in byModel)
        {
            var modelId = group.Key;
            var pricing = await GetPricingFromCacheAsync(modelId);
            
            var modelSummary = new ModelUsageSummary
            {
                ModelId = modelId,
                DisplayName = pricing?.DisplayName ?? modelId,
                Interactions = group.Count(),
                InputTokens = group.Sum(u => u.InputTokens),
                OutputTokens = group.Sum(u => u.OutputTokens)
            };

            if (pricing != null)
            {
                modelSummary.TotalCost = pricing.CalculateTotalCost(
                    (int)modelSummary.InputTokens, 
                    (int)modelSummary.OutputTokens);
            }
            else
            {
                // Use stored estimated cost if pricing not found
                modelSummary.TotalCost = group.Sum(u => u.EstimatedCost ?? 0);
            }

            totalCost += modelSummary.TotalCost;
            summary.ByModel[modelId] = modelSummary;
        }

        summary.TotalCost = totalCost;

        // Calculate percentages
        if (totalCost > 0)
        {
            foreach (var model in summary.ByModel.Values)
            {
                model.PercentageOfTotal = (double)(model.TotalCost / totalCost * 100);
            }
        }

        return summary;
    }

    private List<AiModelPricing> GetDefaultPricingData()
    {
        var now = DateTime.UtcNow;

        return new List<AiModelPricing>
        {
            // ========================================
            // OPENAI CHAT MODELS
            // ========================================
            
            // 💚 LOW TIER - Budget-friendly, < $1/1M tokens
            new AiModelPricing
            {
                ModelId = "gpt-4o-mini",
                DisplayName = "GPT-4o Mini",
                Provider = "OpenAI",
                ModelType = AiModelType.Chat,
                Tier = AiModelTier.Low,
                InputCostPer1M = 0.15m,
                OutputCostPer1M = 0.60m,
                BatchInputCostPer1M = 0.075m,
                BatchOutputCostPer1M = 0.30m,
                IsActive = true,
                IsDefault = true, // ⭐ RECOMMENDED DEFAULT
                ContextWindowSize = 128000,
                MaxOutputTokens = 16384,
                SortOrder = 1,
                Notes = "⭐ Best cost/performance ratio - RECOMMENDED for most use cases",
                PricingUpdatedAt = now
            },
            new AiModelPricing
            {
                ModelId = "gpt-4.1-nano",
                DisplayName = "GPT-4.1 Nano",
                Provider = "OpenAI",
                ModelType = AiModelType.Chat,
                Tier = AiModelTier.Low,
                InputCostPer1M = 0.10m,
                OutputCostPer1M = 0.40m,
                BatchInputCostPer1M = 0.025m,
                BatchOutputCostPer1M = 0.10m,
                IsActive = true,
                IsDefault = false,
                ContextWindowSize = 1000000,
                MaxOutputTokens = 32768,
                SortOrder = 2,
                Notes = "Ultra-cheap for simple tasks, largest context window",
                PricingUpdatedAt = now
            },
            new AiModelPricing
            {
                ModelId = "gpt-3.5-turbo",
                DisplayName = "GPT-3.5 Turbo",
                Provider = "OpenAI",
                ModelType = AiModelType.Chat,
                Tier = AiModelTier.Low,
                InputCostPer1M = 0.50m,
                OutputCostPer1M = 1.50m,
                BatchInputCostPer1M = null,
                BatchOutputCostPer1M = null,
                IsActive = true,
                IsDefault = false,
                ContextWindowSize = 16385,
                MaxOutputTokens = 4096,
                SortOrder = 10,
                Notes = "Legacy model, still reliable for simple tasks",
                PricingUpdatedAt = now
            },

            // 💛 MEDIUM TIER - Balanced, $1-5/1M tokens
            new AiModelPricing
            {
                ModelId = "gpt-4.1-mini",
                DisplayName = "GPT-4.1 Mini",
                Provider = "OpenAI",
                ModelType = AiModelType.Chat,
                Tier = AiModelTier.Medium,
                InputCostPer1M = 0.40m,
                OutputCostPer1M = 1.60m,
                BatchInputCostPer1M = 0.10m,
                BatchOutputCostPer1M = 0.40m,
                IsActive = true,
                IsDefault = false,
                ContextWindowSize = 1000000,
                MaxOutputTokens = 32768,
                SortOrder = 3,
                Notes = "Newer model with 1M token context window",
                PricingUpdatedAt = now
            },
            new AiModelPricing
            {
                ModelId = "o4-mini",
                DisplayName = "O4 Mini Reasoning",
                Provider = "OpenAI",
                ModelType = AiModelType.Reasoning,
                Tier = AiModelTier.Medium,
                InputCostPer1M = 1.10m,
                OutputCostPer1M = 4.40m,
                BatchInputCostPer1M = 0.275m,
                BatchOutputCostPer1M = 1.10m,
                IsActive = true,
                IsDefault = false,
                SortOrder = 4,
                Notes = "Cost-efficient reasoning model for complex tasks",
                PricingUpdatedAt = now
            },

            // 🔶 HIGH TIER - Premium quality, $5-20/1M tokens
            new AiModelPricing
            {
                ModelId = "gpt-4o",
                DisplayName = "GPT-4o",
                Provider = "OpenAI",
                ModelType = AiModelType.Chat,
                Tier = AiModelTier.High,
                InputCostPer1M = 2.50m,
                OutputCostPer1M = 10.00m,
                BatchInputCostPer1M = 1.25m,
                BatchOutputCostPer1M = 5.00m,
                IsActive = true,
                IsDefault = false,
                ContextWindowSize = 128000,
                MaxOutputTokens = 16384,
                SortOrder = 5,
                Notes = "High quality reasoning and complex tasks",
                PricingUpdatedAt = now
            },
            new AiModelPricing
            {
                ModelId = "gpt-4.1",
                DisplayName = "GPT-4.1",
                Provider = "OpenAI",
                ModelType = AiModelType.Chat,
                Tier = AiModelTier.High,
                InputCostPer1M = 2.00m,
                OutputCostPer1M = 8.00m,
                BatchInputCostPer1M = 0.50m,
                BatchOutputCostPer1M = 2.00m,
                IsActive = true,
                IsDefault = false,
                ContextWindowSize = 1000000,
                MaxOutputTokens = 32768,
                SortOrder = 6,
                Notes = "Latest flagship with 1M context window",
                PricingUpdatedAt = now
            },
            new AiModelPricing
            {
                ModelId = "o3",
                DisplayName = "O3 Reasoning",
                Provider = "OpenAI",
                ModelType = AiModelType.Reasoning,
                Tier = AiModelTier.High,
                InputCostPer1M = 2.00m,
                OutputCostPer1M = 8.00m,
                BatchInputCostPer1M = 0.50m,
                BatchOutputCostPer1M = 2.00m,
                IsActive = true,
                IsDefault = true,
                SortOrder = 7,
                Notes = "Advanced reasoning for complex problem-solving",
                PricingUpdatedAt = now
            },

            // 💎 PREMIUM TIER - Enterprise, > $20/1M tokens
            new AiModelPricing
            {
                ModelId = "o1",
                DisplayName = "O1 Reasoning",
                Provider = "OpenAI",
                ModelType = AiModelType.Reasoning,
                Tier = AiModelTier.Premium,
                InputCostPer1M = 15.00m,
                OutputCostPer1M = 60.00m,
                BatchInputCostPer1M = 7.50m,
                BatchOutputCostPer1M = 30.00m,
                IsActive = true,
                IsDefault = false,
                SortOrder = 8,
                Notes = "Premium reasoning for most complex tasks",
                PricingUpdatedAt = now
            },
            new AiModelPricing
            {
                ModelId = "o1-pro",
                DisplayName = "O1 Pro Reasoning",
                Provider = "OpenAI",
                ModelType = AiModelType.Reasoning,
                Tier = AiModelTier.Premium,
                InputCostPer1M = 150.00m,
                OutputCostPer1M = 600.00m,
                BatchInputCostPer1M = null,
                BatchOutputCostPer1M = null,
                IsActive = true,
                IsDefault = false,
                SortOrder = 9,
                Notes = "Enterprise-grade reasoning, highest quality",
                PricingUpdatedAt = now
            },

            // ========================================
            // OPENAI EMBEDDING MODELS
            // ========================================
            new AiModelPricing
            {
                ModelId = "text-embedding-3-small",
                DisplayName = "Text Embedding 3 Small",
                Provider = "OpenAI",
                ModelType = AiModelType.Embedding,
                Tier = AiModelTier.Low,
                InputCostPer1M = 0.02m,
                OutputCostPer1M = 0m,
                BatchInputCostPer1M = 0.01m,
                BatchOutputCostPer1M = null,
                IsActive = true,
                IsDefault = true, // ⭐ RECOMMENDED for embeddings
                EmbeddingDimensions = 1536,
                SortOrder = 1,
                Notes = "⭐ Best cost/performance for semantic search - RECOMMENDED",
                PricingUpdatedAt = now
            },
            new AiModelPricing
            {
                ModelId = "text-embedding-3-large",
                DisplayName = "Text Embedding 3 Large",
                Provider = "OpenAI",
                ModelType = AiModelType.Embedding,
                Tier = AiModelTier.Medium,
                InputCostPer1M = 0.13m,
                OutputCostPer1M = 0m,
                BatchInputCostPer1M = 0.065m,
                BatchOutputCostPer1M = null,
                IsActive = true,
                IsDefault = false,
                EmbeddingDimensions = 3072,
                SortOrder = 2,
                Notes = "Higher quality embeddings, 3072 dimensions",
                PricingUpdatedAt = now
            },
            new AiModelPricing
            {
                ModelId = "text-embedding-ada-002",
                DisplayName = "Text Embedding Ada 002",
                Provider = "OpenAI",
                ModelType = AiModelType.Embedding,
                Tier = AiModelTier.Low,
                InputCostPer1M = 0.10m,
                OutputCostPer1M = 0m,
                BatchInputCostPer1M = 0.05m,
                BatchOutputCostPer1M = null,
                IsActive = true,
                IsDefault = false,
                EmbeddingDimensions = 1536,
                SortOrder = 3,
                Notes = "Legacy embedding model, still supported",
                PricingUpdatedAt = now
            },

            // ========================================
            // LOCAL MODELS (FREE)
            // ========================================
            new AiModelPricing
            {
                ModelId = "llama3.2",
                DisplayName = "Llama 3.2 (Ollama)",
                Provider = "Ollama",
                ModelType = AiModelType.Local,
                Tier = AiModelTier.Free,
                InputCostPer1M = 0m,
                OutputCostPer1M = 0m,
                BatchInputCostPer1M = null,
                BatchOutputCostPer1M = null,
                IsActive = true,
                IsDefault = true,
                ContextWindowSize = 128000,
                SortOrder = 1,
                Notes = "🆓 Free local model - only infrastructure/compute costs",
                PricingUpdatedAt = now
            },
            new AiModelPricing
            {
                ModelId = "llama3.1",
                DisplayName = "Llama 3.1 (Ollama)",
                Provider = "Ollama",
                ModelType = AiModelType.Local,
                Tier = AiModelTier.Free,
                InputCostPer1M = 0m,
                OutputCostPer1M = 0m,
                BatchInputCostPer1M = null,
                BatchOutputCostPer1M = null,
                IsActive = true,
                IsDefault = false,
                ContextWindowSize = 128000,
                SortOrder = 2,
                Notes = "🆓 Free local model - previous version",
                PricingUpdatedAt = now
            },
            new AiModelPricing
            {
                ModelId = "mistral",
                DisplayName = "Mistral (Ollama)",
                Provider = "Ollama",
                ModelType = AiModelType.Local,
                Tier = AiModelTier.Free,
                InputCostPer1M = 0m,
                OutputCostPer1M = 0m,
                BatchInputCostPer1M = null,
                BatchOutputCostPer1M = null,
                IsActive = true,
                IsDefault = false,
                ContextWindowSize = 32000,
                SortOrder = 3,
                Notes = "🆓 Free local model - efficient for its size",
                PricingUpdatedAt = now
            },
            new AiModelPricing
            {
                ModelId = "nomic-embed-text",
                DisplayName = "Nomic Embed Text (Ollama)",
                Provider = "Ollama",
                ModelType = AiModelType.Local,
                Tier = AiModelTier.Free,
                InputCostPer1M = 0m,
                OutputCostPer1M = 0m,
                BatchInputCostPer1M = null,
                BatchOutputCostPer1M = null,
                IsActive = true,
                IsDefault = false,
                EmbeddingDimensions = 768,
                SortOrder = 4,
                Notes = "🆓 Free local embedding model",
                PricingUpdatedAt = now
            },
            new AiModelPricing
            {
                ModelId = "mxbai-embed-large",
                DisplayName = "MXBai Embed Large (Ollama)",
                Provider = "Ollama",
                ModelType = AiModelType.Local,
                Tier = AiModelTier.Free,
                InputCostPer1M = 0m,
                OutputCostPer1M = 0m,
                BatchInputCostPer1M = null,
                BatchOutputCostPer1M = null,
                IsActive = true,
                IsDefault = false,
                EmbeddingDimensions = 1024,
                SortOrder = 5,
                Notes = "🆓 Free local embedding model - higher quality",
                PricingUpdatedAt = now
            }
        };
    }
}
