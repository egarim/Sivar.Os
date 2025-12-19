using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for AI model pricing
/// </summary>
public class AiModelPricingRepository : BaseRepository<AiModelPricing>, IAiModelPricingRepository
{
    public AiModelPricingRepository(SivarDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<AiModelPricing?> GetByModelIdAsync(string modelId)
    {
        return await _context.AiModelPricings
            .FirstOrDefaultAsync(p => p.ModelId == modelId);
    }

    /// <inheritdoc />
    public async Task<List<AiModelPricing>> GetActiveModelsAsync()
    {
        return await _context.AiModelPricings
            .Where(p => p.IsActive)
            .OrderBy(p => p.ModelType)
            .ThenBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayName)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<AiModelPricing>> GetByModelTypeAsync(AiModelType modelType)
    {
        return await _context.AiModelPricings
            .Where(p => p.ModelType == modelType && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.InputCostPer1M)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<AiModelPricing?> GetDefaultModelAsync(AiModelType modelType)
    {
        return await _context.AiModelPricings
            .FirstOrDefaultAsync(p => p.ModelType == modelType && p.IsDefault && p.IsActive);
    }

    /// <inheritdoc />
    public async Task<List<AiModelPricing>> GetByProviderAsync(string provider)
    {
        return await _context.AiModelPricings
            .Where(p => p.Provider == provider && p.IsActive)
            .OrderBy(p => p.ModelType)
            .ThenBy(p => p.SortOrder)
            .ToListAsync();
    }
}
