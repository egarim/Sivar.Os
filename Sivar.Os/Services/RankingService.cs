using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service implementation for computing multi-signal ranking with personalization.
/// Phase 11: Results Ranking & Personalization
/// </summary>
public class RankingService : IRankingService
{
    private readonly IRankingConfigurationRepository _configRepo;
    private readonly IUserSearchBehaviorRepository _behaviorRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RankingService> _logger;
    
    // Cache keys
    private const string ConfigCachePrefix = "ranking_config_";
    private static readonly TimeSpan ConfigCacheDuration = TimeSpan.FromMinutes(5);

    public RankingService(
        IRankingConfigurationRepository configRepo,
        IUserSearchBehaviorRepository behaviorRepo,
        IMemoryCache cache,
        ILogger<RankingService> logger)
    {
        _configRepo = configRepo;
        _behaviorRepo = behaviorRepo;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<RankedSearchResult>> RankResultsAsync(
        IEnumerable<SearchResultBaseDto> results,
        Guid? profileId,
        string query,
        string? category = null)
    {
        var resultList = results.ToList();
        if (!resultList.Any()) return new List<RankedSearchResult>();

        var config = await GetRankingConfigAsync(category);
        var userBehavior = profileId.HasValue 
            ? await GetUserBehaviorAsync(profileId.Value) 
            : null;

        // Store original ranks
        var originalRanks = resultList
            .Select((r, i) => (Result: r, Rank: i + 1))
            .ToDictionary(x => x.Result.Id, x => x.Rank);

        // Compute ranking factors for each result
        var rankedResults = resultList.Select(result =>
        {
            var factors = ComputeRankingFactors(result, userBehavior, config);
            return new RankedSearchResult
            {
                Result = result,
                Factors = factors,
                OriginalRank = originalRanks[result.Id]
            };
        })
        .OrderByDescending(r => r.FinalScore)
        .ToList();

        // Assign new ranks
        for (int i = 0; i < rankedResults.Count; i++)
        {
            rankedResults[i].NewRank = i + 1;
        }

        // Add ranking explanations
        foreach (var ranked in rankedResults)
        {
            ranked.Factors.TopReasons = ExplainRanking(ranked.Factors);
        }

        _logger.LogDebug("Ranked {Count} results for query '{Query}' with config '{Category}'", 
            rankedResults.Count, query, category ?? "default");

        return rankedResults;
    }

    public async Task<RankingConfiguration> GetRankingConfigAsync(string? category = null)
    {
        var cacheKey = $"{ConfigCachePrefix}{category ?? "default"}";
        
        if (_cache.TryGetValue<RankingConfiguration>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var config = await _configRepo.GetByCategoryAsync(category);
        
        if (config == null)
        {
            // Return a default configuration if none exists
            config = CreateDefaultConfig();
            _logger.LogWarning("No ranking configuration found for category '{Category}', using defaults", category);
        }

        _cache.Set(cacheKey, config, ConfigCacheDuration);
        return config;
    }

    public List<RankingReason> ExplainRanking(SearchRankingFactors factors)
    {
        var reasons = new List<RankingReason>();

        // Content relevance reasons
        if (factors.SemanticScore > 0.1)
            reasons.Add(new RankingReason("🔤", "Coincide con búsqueda", factors.SemanticScore, 
                $"Relevancia semántica: {factors.SemanticScore:P0}"));
        
        if (factors.FullTextScore > 0.1)
            reasons.Add(new RankingReason("📝", "Palabras exactas", factors.FullTextScore, 
                $"Coincidencia de texto: {factors.FullTextScore:P0}"));
        
        if (factors.GeoScore > 0.1)
            reasons.Add(new RankingReason("📍", "Cerca de ti", factors.GeoScore, 
                $"Proximidad geográfica: {factors.GeoScore:P0}"));

        // Quality reasons
        if (factors.RatingScore > 0.1)
            reasons.Add(new RankingReason("⭐", "Bien valorado", factors.RatingScore, 
                $"Calificación: {factors.RatingScore:P0}"));
        
        if (factors.ReviewCountScore > 0.1)
            reasons.Add(new RankingReason("💬", "Muchas reseñas", factors.ReviewCountScore, 
                $"Cantidad de reseñas: {factors.ReviewCountScore:P0}"));
        
        if (factors.VerificationBoost > 0.5)
            reasons.Add(new RankingReason("✅", "Verificado", factors.VerificationBoost, 
                "Negocio verificado"));
        
        if (factors.RecencyScore > 0.3)
            reasons.Add(new RankingReason("🕐", "Actualizado recientemente", factors.RecencyScore, 
                $"Reciente: {factors.RecencyScore:P0}"));

        // Content ranking reason
        if (factors.ContentRankScore > 0.1)
            reasons.Add(new RankingReason("📈", "Popular", factors.ContentRankScore, 
                $"Popularidad general: {factors.ContentRankScore:P0}"));

        // Personalization reasons
        if (factors.UserAffinityScore > 0.1)
            reasons.Add(new RankingReason("❤️", "Basado en tu historial", factors.UserAffinityScore, 
                $"Afinidad personal: {factors.UserAffinityScore:P0}"));
        
        if (factors.CategoryPreference > 0.1)
            reasons.Add(new RankingReason("🏷️", "Categoría favorita", factors.CategoryPreference, 
                $"Preferencia de categoría: {factors.CategoryPreference:P0}"));

        // Behavioral reasons
        if (factors.ClickPopularity > 0.1)
            reasons.Add(new RankingReason("👥", "Popular entre usuarios", factors.ClickPopularity, 
                $"Clics: {factors.ClickPopularity:P0}"));
        
        if (factors.ActionRate > 0.1)
            reasons.Add(new RankingReason("📞", "Usuarios toman acción", factors.ActionRate, 
                $"Tasa de acción: {factors.ActionRate:P0}"));

        // Sort by score and take top 4
        return reasons
            .OrderByDescending(r => r.Score)
            .Take(4)
            .ToList();
    }

    public async Task RecordInteractionAsync(Guid profileId, Guid targetId, InteractionType type, string? category = null)
    {
        try
        {
            var behavior = await _behaviorRepo.GetOrCreateAsync(profileId);
            
            switch (type)
            {
                case InteractionType.Click:
                    behavior.TotalClicks++;
                    break;
                case InteractionType.Call:
                case InteractionType.WhatsApp:
                case InteractionType.Email:
                case InteractionType.Directions:
                case InteractionType.Save:
                case InteractionType.Share:
                case InteractionType.Follow:
                case InteractionType.Review:
                    behavior.TotalActions++;
                    break;
            }
            
            // Update category affinity if provided
            if (!string.IsNullOrEmpty(category))
            {
                var interactionWeight = type switch
                {
                    InteractionType.Click => 0.5,
                    InteractionType.Call or InteractionType.WhatsApp => 1.0,
                    InteractionType.Save or InteractionType.Follow => 0.8,
                    InteractionType.Review => 1.0,
                    _ => 0.3
                };
                behavior.UpdateCategoryAffinity(category, interactionWeight);
            }
            
            await _behaviorRepo.UpdateAsync(behavior);
            
            _logger.LogDebug("Recorded interaction {Type} for profile {ProfileId} on target {TargetId}", 
                type, profileId, targetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record interaction {Type} for profile {ProfileId}", type, profileId);
        }
    }

    public async Task RecordImpressionsAsync(Guid? profileId, IEnumerable<Guid> resultIds)
    {
        if (!profileId.HasValue) return;
        
        try
        {
            var behavior = await _behaviorRepo.GetOrCreateAsync(profileId.Value);
            // Impressions are tracked but don't increment counters heavily
            // This data would typically go to analytics DB
            _logger.LogDebug("Recorded {Count} impressions for profile {ProfileId}", 
                resultIds.Count(), profileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record impressions for profile {ProfileId}", profileId);
        }
    }

    public async Task<UserSearchBehavior?> GetUserBehaviorAsync(Guid profileId)
    {
        return await _behaviorRepo.GetByProfileIdAsync(profileId);
    }

    public async Task UpdateUserBehaviorAsync(Guid profileId, string query, string? category = null)
    {
        try
        {
            var behavior = await _behaviorRepo.GetOrCreateAsync(profileId);
            behavior.RecordQuery(query);
            
            if (!string.IsNullOrEmpty(category))
            {
                behavior.UpdateCategoryAffinity(category, 0.3); // Light boost for search
            }
            
            await _behaviorRepo.UpdateAsync(behavior);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user behavior for profile {ProfileId}", profileId);
        }
    }

    #region Private Methods

    private SearchRankingFactors ComputeRankingFactors(
        SearchResultBaseDto result,
        UserSearchBehavior? userBehavior,
        RankingConfiguration config)
    {
        var factors = new SearchRankingFactors
        {
            // Content relevance - use existing score as base
            SemanticScore = NormalizeScore(result.RelevanceScore, 0, 1),
            FullTextScore = 0, // Would come from hybrid search if available
            GeoScore = ComputeGeoScore(result.DistanceKm),
            
            // Quality signals
            RatingScore = ComputeRatingScore(result),
            ReviewCountScore = ComputeReviewCountScore(result),
            VerificationBoost = 0, // Would need IsVerified field
            RecencyScore = 0.5, // Default, would need CreatedAt
            
            // Content ranking (from Elo system - not yet implemented)
            ContentRankScore = 0,
            
            // Personalization
            UserAffinityScore = ComputeAffinityScore(result, userBehavior),
            CategoryPreference = ComputeCategoryPreference(result.Category, userBehavior),
            
            // Behavioral (would need analytics data)
            ClickPopularity = 0,
            ActionRate = 0
        };

        // Compute final score
        factors.FinalScore = 
            factors.SemanticScore * config.SemanticWeight +
            factors.FullTextScore * config.FullTextWeight +
            factors.GeoScore * config.GeoWeight +
            factors.RatingScore * config.RatingWeight +
            factors.ReviewCountScore * config.ReviewCountWeight +
            factors.VerificationBoost * config.VerifiedWeight +
            factors.RecencyScore * config.RecencyWeight +
            factors.ContentRankScore * config.ContentRankWeight +
            factors.UserAffinityScore * config.PersonalizationWeight +
            factors.CategoryPreference * config.CategoryPreferenceWeight +
            factors.ClickPopularity * config.ClickPopularityWeight +
            factors.ActionRate * config.ActionRateWeight;

        return factors;
    }

    private static double NormalizeScore(double score, double min, double max)
    {
        if (max <= min) return 0;
        return Math.Clamp((score - min) / (max - min), 0, 1);
    }

    private static double ComputeGeoScore(double? distanceKm)
    {
        if (!distanceKm.HasValue) return 0.5; // Neutral if no distance
        
        // Exponential decay: closer = higher score
        // 0 km = 1.0, 1 km = 0.9, 5 km = 0.6, 10 km = 0.37, 20 km = 0.14
        return Math.Exp(-distanceKm.Value / 10.0);
    }

    private static double ComputeRatingScore(SearchResultBaseDto result)
    {
        // Try to get rating from business result
        if (result is BusinessSearchResultDto business && business.Rating.HasValue)
        {
            // Normalize 1-5 rating to 0-1
            return (business.Rating.Value - 1) / 4.0;
        }
        return 0.5; // Neutral default
    }

    private static double ComputeReviewCountScore(SearchResultBaseDto result)
    {
        // Try to get review count from business result
        if (result is BusinessSearchResultDto business && business.ReviewCount.HasValue)
        {
            // Logarithmic scale: more reviews = higher, but diminishing returns
            // 0 reviews = 0, 10 reviews = 0.5, 100 reviews = 0.8, 1000 reviews = 1.0
            return Math.Min(1.0, Math.Log10(business.ReviewCount.Value + 1) / 3.0);
        }
        return 0;
    }

    private static double ComputeAffinityScore(SearchResultBaseDto result, UserSearchBehavior? userBehavior)
    {
        if (userBehavior == null) return 0;
        
        // Check if user has interacted with this category before
        var category = result.Category?.ToLowerInvariant();
        if (string.IsNullOrEmpty(category)) return 0;
        
        var affinities = userBehavior.GetCategoryAffinities();
        return affinities.TryGetValue(category, out var affinity) ? affinity : 0;
    }

    private static double ComputeCategoryPreference(string? category, UserSearchBehavior? userBehavior)
    {
        if (userBehavior == null || string.IsNullOrEmpty(category)) return 0;
        
        var preferences = userBehavior.GetResultTypePreferences();
        var normalizedCategory = category.ToLowerInvariant();
        
        return preferences.TryGetValue(normalizedCategory, out var pref) ? pref : 0;
    }

    private static RankingConfiguration CreateDefaultConfig()
    {
        return new RankingConfiguration
        {
            Id = Guid.Empty,
            DisplayName = "Default (In-Memory)",
            Category = null,
            SemanticWeight = 0.25,
            FullTextWeight = 0.15,
            GeoWeight = 0.10,
            RatingWeight = 0.10,
            ReviewCountWeight = 0.05,
            VerifiedWeight = 0.05,
            RecencyWeight = 0.05,
            ContentRankWeight = 0.10,
            PersonalizationWeight = 0.05,
            CategoryPreferenceWeight = 0.05,
            ClickPopularityWeight = 0.025,
            ActionRateWeight = 0.025,
            IsActive = true
        };
    }

    #endregion
}
