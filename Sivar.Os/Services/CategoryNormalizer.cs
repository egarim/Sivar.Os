using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Sivar.Os.Data.Context;

namespace Sivar.Os.Services;

/// <summary>
/// Normalizes user queries to English category keys using CategoryDefinition synonyms
/// Implements the English-First Query Pattern for multilingual search
/// </summary>
public class CategoryNormalizer : ICategoryNormalizer
{
    private readonly IDbContextFactory<SivarDbContext> _dbContextFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CategoryNormalizer> _logger;
    
    private const string SynonymCacheKey = "CategoryNormalizer_Synonyms";
    private const string CategoryCacheKey = "CategoryNormalizer_Categories";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    // In-memory synonym lookup: lowercase synonym → category key
    private Dictionary<string, string> _synonymLookup = new(StringComparer.OrdinalIgnoreCase);
    
    // Category display names: key → (DisplayNameEn, DisplayNameEs)
    private Dictionary<string, (string En, string Es)> _displayNames = new(StringComparer.OrdinalIgnoreCase);
    
    private bool _isInitialized = false;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public CategoryNormalizer(
        IDbContextFactory<SivarDbContext> dbContextFactory,
        IMemoryCache cache,
        ILogger<CategoryNormalizer> logger)
    {
        _dbContextFactory = dbContextFactory;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<string>> NormalizeQueryAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<string>();

        await EnsureInitializedAsync();

        var normalizedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queryLower = query.ToLowerInvariant();

        // Tokenize query into words
        var words = TokenizeQuery(queryLower);

        // Check each word and phrase against synonyms
        foreach (var word in words)
        {
            if (_synonymLookup.TryGetValue(word, out var key))
            {
                normalizedKeys.Add(key);
            }
        }

        // Also check multi-word phrases (2-word and 3-word)
        for (int i = 0; i < words.Count - 1; i++)
        {
            var twoWord = $"{words[i]} {words[i + 1]}";
            if (_synonymLookup.TryGetValue(twoWord, out var key2))
            {
                normalizedKeys.Add(key2);
            }

            if (i < words.Count - 2)
            {
                var threeWord = $"{words[i]} {words[i + 1]} {words[i + 2]}";
                if (_synonymLookup.TryGetValue(threeWord, out var key3))
                {
                    normalizedKeys.Add(key3);
                }
            }
        }

        // Also try partial matching for compound words
        foreach (var word in words)
        {
            foreach (var synonym in _synonymLookup.Keys)
            {
                // Only match if word contains the synonym and synonym is substantial (>3 chars)
                if (synonym.Length > 3 && word.Contains(synonym, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedKeys.Add(_synonymLookup[synonym]);
                }
            }
        }

        _logger.LogDebug("Normalized query '{Query}' to keys: [{Keys}]", 
            query, string.Join(", ", normalizedKeys));

        return normalizedKeys.ToList();
    }

    /// <inheritdoc />
    public async Task<string?> GetPrimaryCategoryAsync(string query)
    {
        var keys = await NormalizeQueryAsync(query);
        return keys.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task RefreshCacheAsync()
    {
        _logger.LogInformation("Refreshing category normalizer cache");
        
        await _initLock.WaitAsync();
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            var categories = await dbContext.CategoryDefinitions
                .AsNoTracking()
                .Where(c => c.IsActive)
                .ToListAsync();

            var synonymLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var displayNames = new Dictionary<string, (string En, string Es)>(StringComparer.OrdinalIgnoreCase);

            foreach (var category in categories)
            {
                // Add the key itself as a synonym
                synonymLookup[category.Key.ToLowerInvariant()] = category.Key;
                
                // Add display names as synonyms
                synonymLookup[category.DisplayNameEn.ToLowerInvariant()] = category.Key;
                synonymLookup[category.DisplayNameEs.ToLowerInvariant()] = category.Key;
                
                // Add all explicit synonyms
                foreach (var synonym in category.Synonyms)
                {
                    var lowerSynonym = synonym.ToLowerInvariant();
                    if (!synonymLookup.ContainsKey(lowerSynonym))
                    {
                        synonymLookup[lowerSynonym] = category.Key;
                    }
                }

                // Store display names
                displayNames[category.Key] = (category.DisplayNameEn, category.DisplayNameEs);
            }

            _synonymLookup = synonymLookup;
            _displayNames = displayNames;
            _isInitialized = true;

            _logger.LogInformation("Category normalizer cache refreshed with {Count} categories and {SynonymCount} synonyms", 
                categories.Count, synonymLookup.Count);
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string> GetDisplayNameAsync(string key, string languageCode = "en")
    {
        await EnsureInitializedAsync();

        if (_displayNames.TryGetValue(key, out var names))
        {
            return languageCode.ToLowerInvariant() switch
            {
                "es" or "es-es" or "es-sv" => names.Es,
                _ => names.En
            };
        }

        return key;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized)
            return;

        await RefreshCacheAsync();
    }

    private static List<string> TokenizeQuery(string query)
    {
        // Remove common Spanish/English query patterns
        var cleanQuery = query
            .Replace("¿", "")
            .Replace("?", "")
            .Replace("!", "")
            .Replace("¡", "")
            .Replace(",", " ")
            .Replace(".", " ");

        // Remove common filler words in both languages
        var fillerWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Spanish
            "de", "la", "el", "los", "las", "un", "una", "unos", "unas",
            "que", "en", "para", "por", "con", "sin", "sobre", "entre",
            "donde", "dónde", "cual", "cuál", "como", "cómo", "cuando", "cuándo",
            "hay", "tiene", "tienen", "está", "están", "es", "son",
            "cerca", "mi", "mí", "me", "yo", "tu", "tú", "su", "sus",
            "puedo", "encontrar", "busco", "quiero", "necesito",
            // English
            "the", "a", "an", "of", "to", "for", "with", "without", "on", "in",
            "where", "what", "which", "how", "when", "who",
            "is", "are", "was", "were", "be", "been", "being",
            "there", "here", "near", "me", "my", "i", "you", "your",
            "can", "find", "looking", "want", "need", "get"
        };

        var words = cleanQuery
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 1 && !fillerWords.Contains(w))
            .ToList();

        return words;
    }
}
