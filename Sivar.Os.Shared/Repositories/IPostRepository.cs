
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for Post entity operations
/// Provides specialized methods for activity stream functionality
/// </summary>
public interface IPostRepository : IBaseRepository<Post>
{
    /// <summary>
    /// Gets posts by profile ID with pagination
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeRelated">Include related entities (Profile, Comments, Reactions, Attachments)</param>
    /// <param name="postType">Optional filter by post type</param>
    /// <returns>Paginated list of posts</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> GetByProfileAsync(
        Guid profileId, 
        int page = 1, 
        int pageSize = 10, 
        bool includeRelated = true,
        PostType? postType = null);

    /// <summary>
    /// Gets posts by post type with pagination
    /// </summary>
    /// <param name="postType">Type of posts to retrieve</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>Paginated list of posts</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> GetByPostTypeAsync(
        PostType postType, 
        int page = 1, 
        int pageSize = 10, 
        bool includeRelated = true);

    /// <summary>
    /// Gets posts within a geographic area with optional radius filtering
    /// </summary>
    /// <param name="latitude">Center point latitude</param>
    /// <param name="longitude">Center point longitude</param>
    /// <param name="radiusKm">Radius in kilometers (optional)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>Paginated list of posts with location</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> GetByLocationAsync(
        double latitude, 
        double longitude, 
        double? radiusKm = null,
        int page = 1, 
        int pageSize = 10, 
        bool includeRelated = true);

    /// <summary>
    /// Gets posts within a geographic radius with distance information using PostGIS
    /// </summary>
    /// <param name="latitude">Center point latitude</param>
    /// <param name="longitude">Center point longitude</param>
    /// <param name="radiusKm">Radius in kilometers</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>Paginated list of posts with their distances in km, ordered by proximity</returns>
    Task<(IEnumerable<(Post Post, double DistanceKm)> Posts, int TotalCount)> GetNearbyWithDistanceAsync(
        double latitude,
        double longitude,
        double radiusKm = 10,
        int page = 1,
        int pageSize = 10,
        bool includeRelated = true);

    /// <summary>
    /// Gets posts that have location information (non-null Location property)
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>Paginated list of posts with location</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> GetWithLocationAsync(
        int page = 1, 
        int pageSize = 10, 
        bool includeRelated = true);

    /// <summary>
    /// Gets activity feed for a profile with posts from followed profiles
    /// </summary>
    /// <param name="profileId">Profile ID requesting the feed</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeOwnPosts">Include posts from the requesting profile</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <param name="profileType">Filter by profile type name (e.g., "Business", "Personal")</param>
    /// <returns>Paginated activity feed</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> GetActivityFeedAsync(
        Guid profileId, 
        int page = 1, 
        int pageSize = 10, 
        bool includeOwnPosts = true,
        bool includeRelated = true,
        string? profileType = null);

    /// <summary>
    /// Gets posts with specific availability status (for business posts)
    /// </summary>
    /// <param name="availabilityStatus">Availability status to filter by</param>
    /// <param name="postTypes">Optional post types to filter by</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>Paginated list of posts</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> GetByAvailabilityStatusAsync(
        AvailabilityStatus availabilityStatus,
        PostType[]? postTypes = null,
        int page = 1, 
        int pageSize = 10, 
        bool includeRelated = true);

    /// <summary>
    /// Searches posts by content, title, or tags
    /// </summary>
    /// <param name="searchTerm">Search term to look for</param>
    /// <param name="postTypes">Optional post types to filter by</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>Paginated search results</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> SearchPostsAsync(
        string searchTerm,
        PostType[]? postTypes = null,
        int page = 1, 
        int pageSize = 10, 
        bool includeRelated = true);

    /// <summary>
    /// Gets featured posts (IsFeatured = true)
    /// </summary>
    /// <param name="postTypes">Optional post types to filter by</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>Paginated list of featured posts</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> GetFeaturedPostsAsync(
        PostType[]? postTypes = null,
        int page = 1, 
        int pageSize = 10, 
        bool includeRelated = true);

    /// <summary>
    /// Gets posts with pricing information (non-null PricingInfo)
    /// </summary>
    /// <param name="minPrice">Optional minimum price filter</param>
    /// <param name="maxPrice">Optional maximum price filter</param>
    /// <param name="currency">Optional currency filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>Paginated list of posts with pricing</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> GetWithPricingAsync(
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? currency = null,
        int page = 1, 
        int pageSize = 10, 
        bool includeRelated = true);

    /// <summary>
    /// Increments view count for a post
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <returns>Updated view count</returns>
    Task<int> IncrementViewCountAsync(Guid postId);

    /// <summary>
    /// Increments share count for a post
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <returns>Updated share count</returns>
    Task<int> IncrementShareCountAsync(Guid postId);

    /// <summary>
    /// Gets posts by multiple profile IDs (for batch operations)
    /// </summary>
    /// <param name="profileIds">List of profile IDs</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>Paginated list of posts</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> GetByProfilesAsync(
        IEnumerable<Guid> profileIds,
        int page = 1, 
        int pageSize = 10, 
        bool includeRelated = true);

    /// <summary>
    /// Gets a post by ID with all related entities
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <returns>Post with related entities or null if not found</returns>
    Task<Post?> GetWithRelatedAsync(Guid postId);

    /// <summary>
    /// Gets posts created within a date range
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>Paginated list of posts</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        int page = 1, 
        int pageSize = 10, 
        bool includeRelated = true);

    /// <summary>
    /// Gets all posts that have vector embeddings for semantic search
    /// </summary>
    /// <returns>List of posts with vector embeddings</returns>
    Task<List<Post>> GetAllWithEmbeddingsAsync();

    // ========== Phase 3.5: Multi-Language Full-Text Search Methods ==========

    /// <summary>
    /// Language-aware full-text search using PostgreSQL's native tsvector
    /// Uses language-specific search vector for accurate stemming and stop words
    /// </summary>
    /// <param name="searchQuery">Search query text</param>
    /// <param name="language">ISO 639-1 language code (e.g., "en", "es", "fr"). If null, defaults to "en"</param>
    /// <param name="postTypes">Optional post types to filter by</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>List of posts ranked by relevance</returns>
    Task<List<Post>> FullTextSearchAsync(
        string searchQuery,
        string? language = null,
        PostType[]? postTypes = null,
        int limit = 50,
        bool includeRelated = true);

    /// <summary>
    /// Cross-language full-text search
    /// Searches across ALL languages using the simple/universal search vector
    /// No stemming, but works for any language
    /// </summary>
    /// <param name="searchQuery">Search query text</param>
    /// <param name="postTypes">Optional post types to filter by</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>List of posts ranked by relevance across all languages</returns>
    Task<List<Post>> CrossLanguageSearchAsync(
        string searchQuery,
        PostType[]? postTypes = null,
        int limit = 50,
        bool includeRelated = true);

    /// <summary>
    /// Smart search: Tries language-specific first, falls back to cross-language
    /// Best of both worlds - prioritizes user's language but shows other results too
    /// </summary>
    /// <param name="searchQuery">Search query text</param>
    /// <param name="userLanguage">User's preferred language (ISO 639-1 code)</param>
    /// <param name="postTypes">Optional post types to filter by</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>List of posts with user's language prioritized</returns>
    Task<List<Post>> SmartSearchAsync(
        string searchQuery,
        string? userLanguage = null,
        PostType[]? postTypes = null,
        int limit = 50,
        bool includeRelated = true);

    /// <summary>
    /// Multi-language search with language detection
    /// Searches in multiple specific languages and combines results
    /// </summary>
    /// <param name="searchQuery">Search query text</param>
    /// <param name="targetLanguages">Array of ISO 639-1 language codes to search in</param>
    /// <param name="limitPerLanguage">Maximum results per language</param>
    /// <returns>List of tuples containing posts, their matched language, and relevance rank</returns>
    Task<List<(Post Post, string MatchLanguage, double Rank)>> MultiLanguageSearchAsync(
        string searchQuery,
        string[] targetLanguages,
        int limitPerLanguage = 20);

    /// <summary>
    /// Full-text search with relevance score and minimum similarity threshold
    /// Returns posts ranked by relevance with optional filtering
    /// </summary>
    /// <param name="searchQuery">Search query text</param>
    /// <param name="language">ISO 639-1 language code. If null, defaults to "en"</param>
    /// <param name="postTypes">Optional post types to filter by</param>
    /// <param name="minRelevance">Minimum relevance score (0.0 to 1.0)</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>List of tuples containing posts and their relevance ranks</returns>
    Task<List<(Post Post, double Rank)>> FullTextSearchWithRankAsync(
        string searchQuery,
        string? language = null,
        PostType[]? postTypes = null,
        double minRelevance = 0.1,
        int limit = 50,
        bool includeRelated = true);

    // ========== Phase 5: Native pgvector Semantic Search Methods ==========

    /// <summary>
    /// Native pgvector semantic search using database-native vector similarity
    /// Uses HNSW index for sub-millisecond similarity search (100-1000x faster than in-memory)
    /// </summary>
    /// <param name="queryVector">Query vector as string (PostgreSQL vector format: "[0.1,0.2,...]")</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>List of posts ordered by semantic similarity (most similar first)</returns>
    Task<List<Post>> SemanticSearchAsync(
        string queryVector,
        int limit = 50,
        bool includeRelated = true);

    /// <summary>
    /// Native pgvector semantic search with similarity scores
    /// Returns both posts and their cosine similarity scores
    /// </summary>
    /// <param name="queryVector">Query vector as string (PostgreSQL vector format: "[0.1,0.2,...]")</param>
    /// <param name="minSimilarity">Minimum similarity threshold (0.0 to 1.0, higher is more similar)</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>List of tuples containing posts and their similarity scores</returns>
    Task<List<(Post Post, double Similarity)>> SemanticSearchWithScoreAsync(
        string queryVector,
        double minSimilarity = 0.0,
        int limit = 50,
        bool includeRelated = true);

    /// <summary>
    /// Updates the ContentEmbedding column for a post using raw SQL with ::vector cast
    /// Required because ContentEmbedding is ignored by EF Core (see DEVELOPMENT_RULES.md section 12)
    /// </summary>
    /// <param name="postId">Post ID</param>
    /// <param name="embeddingVector">Embedding vector as string (PostgreSQL vector format: "[0.1,0.2,...]")</param>
    /// <returns>True if update succeeded, false otherwise</returns>
    Task<bool> UpdateContentEmbeddingAsync(Guid postId, string embeddingVector);

    // ========== Phase 6: Hybrid Search for Structured RAG ==========

    /// <summary>
    /// Hybrid search combining pgvector semantic similarity, PostgreSQL full-text search, and PostGIS geo proximity.
    /// Returns structured results with combined relevance scoring for AI chat card rendering.
    /// </summary>
    /// <param name="queryVector">Query embedding vector as string (PostgreSQL vector format: "[0.1,0.2,...]")</param>
    /// <param name="searchQuery">Natural language search query for full-text matching</param>
    /// <param name="userLatitude">Optional user latitude for geographic ranking</param>
    /// <param name="userLongitude">Optional user longitude for geographic ranking</param>
    /// <param name="maxDistanceKm">Maximum distance in kilometers for geo filtering</param>
    /// <param name="postTypes">Optional filter by post types</param>
    /// <param name="category">Optional filter by category tag</param>
    /// <param name="semanticWeight">Weight for semantic similarity (0.0-1.0)</param>
    /// <param name="fullTextWeight">Weight for full-text rank (0.0-1.0)</param>
    /// <param name="geoWeight">Weight for geographic proximity (0.0-1.0)</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of posts with combined relevance scores, similarity, rank, and distance</returns>
    Task<List<HybridSearchResult>> HybridSearchAsync(
        string queryVector,
        string searchQuery,
        double? userLatitude = null,
        double? userLongitude = null,
        double? maxDistanceKm = null,
        PostType[]? postTypes = null,
        string? category = null,
        double semanticWeight = 0.5,
        double fullTextWeight = 0.3,
        double geoWeight = 0.2,
        int limit = 10);
}

/// <summary>
/// Result from hybrid search combining semantic, full-text, and geo scoring
/// </summary>
public class HybridSearchResult
{
    /// <summary>
    /// The matched post
    /// </summary>
    public Post Post { get; set; } = null!;

    /// <summary>
    /// Combined relevance score (0.0 to 1.0, higher is better)
    /// </summary>
    public double CombinedScore { get; set; }

    /// <summary>
    /// Semantic similarity component (0.0 to 1.0)
    /// </summary>
    public double SemanticSimilarity { get; set; }

    /// <summary>
    /// Full-text search rank component
    /// </summary>
    public double FullTextRank { get; set; }

    /// <summary>
    /// Distance in kilometers from user location (null if no geo search)
    /// </summary>
    public double? DistanceKm { get; set; }

    /// <summary>
    /// Primary match source for this result
    /// </summary>
    public string MatchSource { get; set; } = "Hybrid";
}
