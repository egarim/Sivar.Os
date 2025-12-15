namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// Stores computed ranking factors for analytics and display.
/// Phase 11: Results Ranking & Personalization
/// </summary>
public class SearchRankingFactors
{
    #region Content Relevance (from hybrid search)
    
    /// <summary>
    /// Vector similarity score (0-1)
    /// </summary>
    public double SemanticScore { get; set; }
    
    /// <summary>
    /// PostgreSQL ts_rank full-text score (0-1 normalized)
    /// </summary>
    public double FullTextScore { get; set; }
    
    /// <summary>
    /// Geographic proximity score (0-1, closer = higher)
    /// </summary>
    public double GeoScore { get; set; }
    
    #endregion
    
    #region Quality Signals
    
    /// <summary>
    /// Rating score derived from average rating (0-1)
    /// </summary>
    public double RatingScore { get; set; }
    
    /// <summary>
    /// Review count score (logarithmic, 0-1)
    /// </summary>
    public double ReviewCountScore { get; set; }
    
    /// <summary>
    /// Boost for verified businesses (0 or 1)
    /// </summary>
    public double VerificationBoost { get; set; }
    
    /// <summary>
    /// Recency score based on last update (0-1, newer = higher)
    /// </summary>
    public double RecencyScore { get; set; }
    
    #endregion
    
    #region Personalization
    
    /// <summary>
    /// User affinity score based on past interactions (0-1)
    /// </summary>
    public double UserAffinityScore { get; set; }
    
    /// <summary>
    /// Category preference based on user behavior (0-1)
    /// </summary>
    public double CategoryPreference { get; set; }
    
    #endregion
    
    #region Behavioral Signals
    
    /// <summary>
    /// Click-through rate popularity (0-1)
    /// </summary>
    public double ClickPopularity { get; set; }
    
    /// <summary>
    /// Action rate (calls, saves, etc.) after clicks (0-1)
    /// </summary>
    public double ActionRate { get; set; }
    
    #endregion
    
    #region Content Ranking (from content_ranking.md)
    
    /// <summary>
    /// Elo-based composite score from content ranking system (0-1 normalized)
    /// </summary>
    public double ContentRankScore { get; set; }
    
    #endregion
    
    /// <summary>
    /// Final computed ranking score
    /// </summary>
    public double FinalScore { get; set; }
    
    /// <summary>
    /// Top reasons for this ranking (for display)
    /// </summary>
    public List<RankingReason> TopReasons { get; set; } = new();
}

/// <summary>
/// A single reason explaining why a result ranked where it did
/// </summary>
public record RankingReason(
    string Icon,
    string Label,
    double Score,
    string Description
)
{
    /// <summary>
    /// Score as percentage (0-100)
    /// </summary>
    public int ScorePercent => (int)(Score * 100);
}
