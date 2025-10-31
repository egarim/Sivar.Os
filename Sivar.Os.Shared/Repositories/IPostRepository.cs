
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
    /// <returns>Paginated list of posts</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> GetByProfileAsync(
        Guid profileId, 
        int page = 1, 
        int pageSize = 10, 
        bool includeRelated = true);

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

    /// <summary>
    /// Full-text search using PostgreSQL's native tsvector (Phase 3: Full-Text Search)
    /// This provides much faster and more accurate search than LIKE queries
    /// Supports language-aware stemming, ranking, and fuzzy matching
    /// </summary>
    /// <param name="searchQuery">Search query text</param>
    /// <param name="postTypes">Optional post types to filter by</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>List of posts ranked by relevance</returns>
    Task<List<Post>> FullTextSearchAsync(
        string searchQuery,
        PostType[]? postTypes = null,
        int limit = 50,
        bool includeRelated = true);

    /// <summary>
    /// Full-text search with relevance score and minimum similarity threshold
    /// Returns posts ranked by relevance with optional filtering
    /// </summary>
    /// <param name="searchQuery">Search query text</param>
    /// <param name="postTypes">Optional post types to filter by</param>
    /// <param name="minRelevance">Minimum relevance score (0.0 to 1.0)</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="includeRelated">Include related entities</param>
    /// <returns>List of tuples containing posts and their relevance ranks</returns>
    Task<List<(Post Post, double Rank)>> FullTextSearchWithRankAsync(
        string searchQuery,
        PostType[]? postTypes = null,
        double minRelevance = 0.1,
        int limit = 50,
        bool includeRelated = true);
}