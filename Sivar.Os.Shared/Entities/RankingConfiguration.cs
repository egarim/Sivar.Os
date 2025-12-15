using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Database-stored ranking weights (per category or global).
/// Phase 11: Results Ranking & Personalization
/// </summary>
[Table("Sivar_RankingConfigurations")]
public class RankingConfiguration : BaseEntity
{
    /// <summary>
    /// Category this config applies to (null = global default)
    /// </summary>
    [StringLength(100)]
    public virtual string? Category { get; set; }
    
    /// <summary>
    /// Display name for admin UI
    /// </summary>
    [StringLength(100)]
    public virtual string DisplayName { get; set; } = "Default";
    
    /// <summary>
    /// Description of this configuration
    /// </summary>
    [StringLength(500)]
    public virtual string? Description { get; set; }
    
    #region Content Relevance Weights (should sum to ~0.50)
    
    /// <summary>
    /// Weight for semantic/vector similarity (default 0.25)
    /// </summary>
    public virtual double SemanticWeight { get; set; } = 0.25;
    
    /// <summary>
    /// Weight for PostgreSQL full-text search (default 0.15)
    /// </summary>
    public virtual double FullTextWeight { get; set; } = 0.15;
    
    /// <summary>
    /// Weight for geographic proximity (default 0.10)
    /// </summary>
    public virtual double GeoWeight { get; set; } = 0.10;
    
    #endregion
    
    #region Quality Signal Weights (should sum to ~0.25)
    
    /// <summary>
    /// Weight for average rating (default 0.10)
    /// </summary>
    public virtual double RatingWeight { get; set; } = 0.10;
    
    /// <summary>
    /// Weight for review count (default 0.05)
    /// </summary>
    public virtual double ReviewCountWeight { get; set; } = 0.05;
    
    /// <summary>
    /// Weight for verified business boost (default 0.05)
    /// </summary>
    public virtual double VerifiedWeight { get; set; } = 0.05;
    
    /// <summary>
    /// Weight for content recency (default 0.05)
    /// </summary>
    public virtual double RecencyWeight { get; set; } = 0.05;
    
    #endregion
    
    #region Content Ranking Weight (from Elo system)
    
    /// <summary>
    /// Weight for Elo-based content ranking composite score (default 0.10)
    /// </summary>
    public virtual double ContentRankWeight { get; set; } = 0.10;
    
    #endregion
    
    #region Personalization Weights (should sum to ~0.10)
    
    /// <summary>
    /// Weight for user affinity/interaction history (default 0.05)
    /// </summary>
    public virtual double PersonalizationWeight { get; set; } = 0.05;
    
    /// <summary>
    /// Weight for category preference (default 0.05)
    /// </summary>
    public virtual double CategoryPreferenceWeight { get; set; } = 0.05;
    
    #endregion
    
    #region Behavioral Weights (start at 0, increase as data grows)
    
    /// <summary>
    /// Weight for click popularity (default 0.00, enable when data sufficient)
    /// </summary>
    public virtual double ClickPopularityWeight { get; set; } = 0.00;
    
    /// <summary>
    /// Weight for action rate (default 0.00, enable when data sufficient)
    /// </summary>
    public virtual double ActionRateWeight { get; set; } = 0.00;
    
    #endregion
    
    #region Configuration Meta
    
    /// <summary>
    /// Whether this configuration is active
    /// </summary>
    public virtual bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Priority for matching (higher = preferred when multiple match)
    /// </summary>
    public virtual int Priority { get; set; } = 0;
    
    /// <summary>
    /// A/B test variant identifier (null = production)
    /// </summary>
    [StringLength(50)]
    public virtual string? AbTestVariant { get; set; }
    
    /// <summary>
    /// A/B test traffic percentage (1-100)
    /// </summary>
    public virtual int AbTestTrafficPercent { get; set; } = 100;
    
    #endregion
    
    /// <summary>
    /// Validates that weights sum to approximately 1.0
    /// </summary>
    public bool ValidateWeights(out double total)
    {
        total = SemanticWeight + FullTextWeight + GeoWeight +
                RatingWeight + ReviewCountWeight + VerifiedWeight + RecencyWeight +
                ContentRankWeight +
                PersonalizationWeight + CategoryPreferenceWeight +
                ClickPopularityWeight + ActionRateWeight;
        
        return total >= 0.95 && total <= 1.05;
    }
    
    /// <summary>
    /// Normalizes weights to sum to 1.0
    /// </summary>
    public void NormalizeWeights()
    {
        ValidateWeights(out var total);
        if (total == 0) return;
        
        SemanticWeight /= total;
        FullTextWeight /= total;
        GeoWeight /= total;
        RatingWeight /= total;
        ReviewCountWeight /= total;
        VerifiedWeight /= total;
        RecencyWeight /= total;
        ContentRankWeight /= total;
        PersonalizationWeight /= total;
        CategoryPreferenceWeight /= total;
        ClickPopularityWeight /= total;
        ActionRateWeight /= total;
    }
}
