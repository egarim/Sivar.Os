namespace Sivar.Os.Services;

/// <summary>
/// Interface for normalizing user queries to English category keys
/// Implements the English-First Query Pattern for multilingual search
/// </summary>
public interface ICategoryNormalizer
{
    /// <summary>
    /// Normalizes a user query to a list of English category keys
    /// Uses CategoryDefinition synonyms to translate multilingual terms
    /// </summary>
    /// <param name="query">User's search query in any supported language (Spanish/English)</param>
    /// <returns>List of normalized English category keys</returns>
    /// <example>
    /// Input: "pizzerías cerca de mí"
    /// Output: ["pizza", "restaurant"]
    /// </example>
    Task<List<string>> NormalizeQueryAsync(string query);

    /// <summary>
    /// Gets a single best-matching category key for the query
    /// Returns null if no matching category is found
    /// </summary>
    /// <param name="query">User's search query</param>
    /// <returns>Best matching category key or null</returns>
    Task<string?> GetPrimaryCategoryAsync(string query);

    /// <summary>
    /// Refreshes the in-memory synonym cache from the database
    /// Call this when CategoryDefinition data is updated
    /// </summary>
    Task RefreshCacheAsync();

    /// <summary>
    /// Gets display name for a category key in the specified language
    /// </summary>
    /// <param name="key">Normalized English category key</param>
    /// <param name="languageCode">Language code (en, es)</param>
    /// <returns>Localized display name or the key if not found</returns>
    Task<string> GetDisplayNameAsync(string key, string languageCode = "en");
}
