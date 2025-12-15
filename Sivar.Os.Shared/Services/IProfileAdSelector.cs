using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Context information for a search request
/// </summary>
public class SearchAdContext
{
    /// <summary>
    /// The search query text
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Category being searched (e.g., "restaurant", "tourism")
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// User's latitude for geo-targeting
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// User's longitude for geo-targeting
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// ID of the user performing the search (for fraud detection)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Profile IDs already in organic results (to avoid duplicates)
    /// </summary>
    public List<Guid> OrganicProfileIds { get; set; } = new();
}

/// <summary>
/// Result of sponsored profile selection
/// </summary>
public class SponsoredProfileResult
{
    /// <summary>
    /// The profile ID
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// The profile entity
    /// </summary>
    public Profile Profile { get; set; } = null!;

    /// <summary>
    /// Calculated ad rank (bid × quality score × relevance)
    /// </summary>
    public double AdRank { get; set; }

    /// <summary>
    /// Actual price to charge per click (second-price auction)
    /// </summary>
    public decimal ActualPricePerClick { get; set; }

    /// <summary>
    /// Position in final results (set during interleaving)
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Relevance score to the search query
    /// </summary>
    public double RelevanceScore { get; set; }
}

/// <summary>
/// Service to select profiles for sponsored placement in search results
/// </summary>
public interface IProfileAdSelector
{
    /// <summary>
    /// Select profiles eligible for sponsored placement based on budget, targeting, and auction
    /// </summary>
    /// <param name="context">Search context for targeting</param>
    /// <param name="maxSponsored">Maximum number of sponsored results</param>
    /// <returns>List of sponsored profile results ordered by ad rank</returns>
    Task<List<SponsoredProfileResult>> SelectSponsoredProfilesAsync(
        SearchAdContext context,
        int maxSponsored = 2);
}
