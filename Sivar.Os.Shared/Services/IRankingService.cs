using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Interaction types for user behavior tracking
/// </summary>
public enum InteractionType
{
    Impression,      // Result shown in search
    Click,           // User clicked to view details
    Call,            // User called the business
    WhatsApp,        // User opened WhatsApp
    Email,           // User opened email
    Directions,      // User opened directions
    Save,            // User saved/bookmarked
    Share,           // User shared the result
    Follow,          // User followed the profile
    Review           // User left a review
}

/// <summary>
/// Service interface for computing multi-signal ranking with personalization.
/// Phase 11: Results Ranking & Personalization
/// </summary>
public interface IRankingService
{
    /// <summary>
    /// Applies full ranking with all signals to search results
    /// </summary>
    /// <param name="results">Raw search results from hybrid search</param>
    /// <param name="profileId">Current user's profile ID (for personalization)</param>
    /// <param name="query">Original search query</param>
    /// <param name="category">Optional category filter</param>
    /// <returns>Results re-ranked with full scoring</returns>
    Task<List<RankedSearchResult>> RankResultsAsync(
        IEnumerable<SearchResultBaseDto> results,
        Guid? profileId,
        string query,
        string? category = null);

    /// <summary>
    /// Gets the ranking configuration for a category (or global default)
    /// </summary>
    Task<RankingConfiguration> GetRankingConfigAsync(string? category = null);

    /// <summary>
    /// Gets ranking explanation for display to user
    /// </summary>
    List<RankingReason> ExplainRanking(SearchRankingFactors factors);

    /// <summary>
    /// Records a user interaction for future personalization
    /// </summary>
    Task RecordInteractionAsync(Guid profileId, Guid targetId, InteractionType type, string? category = null);

    /// <summary>
    /// Records that a result was shown (impression)
    /// </summary>
    Task RecordImpressionsAsync(Guid? profileId, IEnumerable<Guid> resultIds);

    /// <summary>
    /// Gets user search behavior for a profile
    /// </summary>
    Task<UserSearchBehavior?> GetUserBehaviorAsync(Guid profileId);

    /// <summary>
    /// Updates user search behavior after a search
    /// </summary>
    Task UpdateUserBehaviorAsync(Guid profileId, string query, string? category = null);
}

/// <summary>
/// A search result with computed ranking factors
/// </summary>
public class RankedSearchResult
{
    /// <summary>
    /// The original search result
    /// </summary>
    public SearchResultBaseDto Result { get; init; } = null!;
    
    /// <summary>
    /// All computed ranking factors
    /// </summary>
    public SearchRankingFactors Factors { get; init; } = new();
    
    /// <summary>
    /// Final computed score (same as Factors.FinalScore)
    /// </summary>
    public double FinalScore => Factors.FinalScore;
    
    /// <summary>
    /// Original rank before personalization (1-based)
    /// </summary>
    public int OriginalRank { get; set; }
    
    /// <summary>
    /// New rank after personalization (1-based)
    /// </summary>
    public int NewRank { get; set; }
    
    /// <summary>
    /// Whether rank changed due to personalization
    /// </summary>
    public bool RankChanged => OriginalRank != NewRank;
    
    /// <summary>
    /// Direction of rank change (+N = improved, -N = dropped)
    /// </summary>
    public int RankDelta => OriginalRank - NewRank;
}
