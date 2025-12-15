using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Tracks user search behavior for personalization.
/// Phase 11: Results Ranking & Personalization
/// </summary>
[Table("Sivar_UserSearchBehaviors")]
public class UserSearchBehavior : BaseEntity
{
    /// <summary>
    /// The profile this behavior belongs to
    /// </summary>
    public Guid ProfileId { get; set; }
    
    /// <summary>
    /// Category affinity scores (learned from interactions)
    /// Example: {"restaurant": 0.8, "tourism": 0.3, "government": 0.5}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? CategoryAffinities { get; set; }
    
    /// <summary>
    /// Recently interacted post/profile IDs with timestamps
    /// Example: [{"id": "guid", "type": "post", "at": "2024-01-01T12:00:00Z"}]
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? RecentInteractions { get; set; }
    
    /// <summary>
    /// Frequently searched terms with counts
    /// Example: {"pizza": 5, "banco": 3, "pasaporte": 2}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? FrequentQueries { get; set; }
    
    /// <summary>
    /// Preferred result types with affinities
    /// Example: {"business": 0.7, "procedure": 0.2, "event": 0.1}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? ResultTypePreferences { get; set; }
    
    /// <summary>
    /// Preferred price range for services/products
    /// Example: {"min": 5, "max": 50, "currency": "USD"}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? PricePreferences { get; set; }
    
    /// <summary>
    /// Average search radius (learned from behavior)
    /// </summary>
    public double? PreferredRadiusKm { get; set; }
    
    /// <summary>
    /// Preferred cities/departments
    /// </summary>
    [Column(TypeName = "text[]")]
    public string[]? PreferredLocations { get; set; }
    
    /// <summary>
    /// Total number of searches performed
    /// </summary>
    public int TotalSearches { get; set; }
    
    /// <summary>
    /// Total number of result clicks
    /// </summary>
    public int TotalClicks { get; set; }
    
    /// <summary>
    /// Total number of actions taken (calls, saves, etc.)
    /// </summary>
    public int TotalActions { get; set; }
    
    // Navigation property
    public virtual Profile? Profile { get; set; }
    
    #region Helper Methods
    
    /// <summary>
    /// Gets category affinities as dictionary
    /// </summary>
    public Dictionary<string, double> GetCategoryAffinities()
    {
        if (CategoryAffinities == null) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, double>>(
                CategoryAffinities.RootElement.GetRawText()) ?? new();
        }
        catch { return new(); }
    }
    
    /// <summary>
    /// Updates category affinity (incremental learning)
    /// </summary>
    public void UpdateCategoryAffinity(string category, double interaction)
    {
        var affinities = GetCategoryAffinities();
        const double learningRate = 0.1;
        
        if (affinities.TryGetValue(category, out var current))
        {
            // Exponential moving average
            affinities[category] = current + learningRate * (interaction - current);
        }
        else
        {
            affinities[category] = interaction * learningRate;
        }
        
        // Normalize to 0-1 range
        var max = affinities.Values.Max();
        if (max > 1)
        {
            foreach (var key in affinities.Keys.ToList())
            {
                affinities[key] /= max;
            }
        }
        
        CategoryAffinities = JsonDocument.Parse(JsonSerializer.Serialize(affinities));
    }
    
    /// <summary>
    /// Gets result type preferences as dictionary
    /// </summary>
    public Dictionary<string, double> GetResultTypePreferences()
    {
        if (ResultTypePreferences == null) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, double>>(
                ResultTypePreferences.RootElement.GetRawText()) ?? new();
        }
        catch { return new(); }
    }
    
    /// <summary>
    /// Gets frequent queries as dictionary
    /// </summary>
    public Dictionary<string, int> GetFrequentQueries()
    {
        if (FrequentQueries == null) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(
                FrequentQueries.RootElement.GetRawText()) ?? new();
        }
        catch { return new(); }
    }
    
    /// <summary>
    /// Records a search query
    /// </summary>
    public void RecordQuery(string query)
    {
        var queries = GetFrequentQueries();
        var normalizedQuery = query.ToLowerInvariant().Trim();
        
        if (queries.TryGetValue(normalizedQuery, out var count))
        {
            queries[normalizedQuery] = count + 1;
        }
        else
        {
            queries[normalizedQuery] = 1;
        }
        
        // Keep only top 50 queries
        if (queries.Count > 50)
        {
            var topQueries = queries.OrderByDescending(x => x.Value)
                .Take(50)
                .ToDictionary(x => x.Key, x => x.Value);
            queries = topQueries;
        }
        
        FrequentQueries = JsonDocument.Parse(JsonSerializer.Serialize(queries));
        TotalSearches++;
    }
    
    #endregion
}
