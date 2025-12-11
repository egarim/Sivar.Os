using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for managing structured search results from AI chat
/// </summary>
public interface ISearchResultService
{
    /// <summary>
    /// Performs a hybrid search combining semantic, full-text, and geographic search
    /// </summary>
    /// <param name="request">Search request with query, filters, and weights</param>
    /// <returns>Collection of typed search results grouped by type</returns>
    Task<SearchResultsCollectionDto> HybridSearchAsync(HybridSearchRequestDto request);

    /// <summary>
    /// Saves search results for a chat message
    /// </summary>
    /// <param name="chatMessageId">The chat message to associate results with</param>
    /// <param name="results">The search results to save</param>
    /// <returns>List of saved search result IDs</returns>
    Task<List<Guid>> SaveSearchResultsAsync(Guid chatMessageId, SearchResultsCollectionDto results);

    /// <summary>
    /// Gets search results for a specific chat message
    /// </summary>
    /// <param name="chatMessageId">Chat message ID</param>
    /// <returns>Collection of search results or null if none found</returns>
    Task<SearchResultsCollectionDto?> GetSearchResultsByMessageAsync(Guid chatMessageId);

    /// <summary>
    /// Gets search results for a conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>List of search result collections by message</returns>
    Task<List<(Guid MessageId, SearchResultsCollectionDto Results)>> GetSearchResultsByConversationAsync(Guid conversationId);

    /// <summary>
    /// Searches for businesses/profiles with structured results
    /// </summary>
    /// <param name="query">Natural language query</param>
    /// <param name="queryEmbedding">Optional pre-computed embedding</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="userLatitude">User's latitude for proximity</param>
    /// <param name="userLongitude">User's longitude for proximity</param>
    /// <param name="maxDistanceKm">Maximum distance filter</param>
    /// <param name="limit">Maximum results</param>
    /// <returns>List of business search results</returns>
    Task<List<BusinessSearchResultDto>> SearchBusinessesAsync(
        string query,
        float[]? queryEmbedding = null,
        string? category = null,
        double? userLatitude = null,
        double? userLongitude = null,
        double? maxDistanceKm = null,
        int limit = 10);

    /// <summary>
    /// Searches for events with structured results
    /// </summary>
    /// <param name="query">Natural language query</param>
    /// <param name="queryEmbedding">Optional pre-computed embedding</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="userLatitude">User's latitude for proximity</param>
    /// <param name="userLongitude">User's longitude for proximity</param>
    /// <param name="limit">Maximum results</param>
    /// <returns>List of event search results</returns>
    Task<List<EventSearchResultDto>> SearchEventsAsync(
        string query,
        float[]? queryEmbedding = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        double? userLatitude = null,
        double? userLongitude = null,
        int limit = 10);

    /// <summary>
    /// Searches for government procedures with structured results
    /// </summary>
    /// <param name="query">Natural language query</param>
    /// <param name="queryEmbedding">Optional pre-computed embedding</param>
    /// <param name="limit">Maximum results</param>
    /// <returns>List of procedure search results</returns>
    Task<List<ProcedureSearchResultDto>> SearchProceduresAsync(
        string query,
        float[]? queryEmbedding = null,
        int limit = 10);

    /// <summary>
    /// Searches for tourism attractions with structured results
    /// </summary>
    /// <param name="query">Natural language query</param>
    /// <param name="queryEmbedding">Optional pre-computed embedding</param>
    /// <param name="userLatitude">User's latitude for proximity</param>
    /// <param name="userLongitude">User's longitude for proximity</param>
    /// <param name="limit">Maximum results</param>
    /// <returns>List of tourism search results</returns>
    Task<List<TourismSearchResultDto>> SearchTourismAsync(
        string query,
        float[]? queryEmbedding = null,
        double? userLatitude = null,
        double? userLongitude = null,
        int limit = 10);

    /// <summary>
    /// Maps a Post entity to the appropriate search result DTO based on PostType
    /// </summary>
    /// <param name="post">The post to map</param>
    /// <param name="relevanceScore">Combined relevance score</param>
    /// <param name="semanticScore">Semantic similarity score</param>
    /// <param name="fullTextRank">Full-text search rank</param>
    /// <param name="distanceKm">Distance in kilometers</param>
    /// <param name="displayOrder">Display order position</param>
    /// <returns>The appropriate DTO for the post type</returns>
    SearchResultBaseDto MapPostToSearchResult(
        Post post,
        double relevanceScore,
        double? semanticScore = null,
        double? fullTextRank = null,
        double? distanceKm = null,
        int displayOrder = 0);
}
