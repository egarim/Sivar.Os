using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;

using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;

namespace Sivar.Os.Controllers;

/// <summary>
/// Controller for semantic search functionality using vector embeddings
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[SwaggerTag("Semantic search operations using AI-powered vector embeddings")]
public class SearchController : ControllerBase
{
    private readonly IVectorEmbeddingService _vectorEmbeddingService;
    private readonly IPostService _postService;
    private readonly IProfileService _profileService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IVectorEmbeddingService vectorEmbeddingService,
        IPostService postService,
        IProfileService profileService,
        ILogger<SearchController> logger)
    {
        _vectorEmbeddingService = vectorEmbeddingService;
        _postService = postService;
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Unified global search across users, posts, and profiles
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="types">Comma-separated types to search: users,posts,profiles (default: all)</param>
    /// <param name="maxResults">Maximum results per type (default: 10, max: 50)</param>
    /// <param name="sortBy">Sort by: relevance, date, name (default: relevance)</param>
    /// <returns>Global search results grouped by type</returns>
    [HttpGet("global")]
    [SwaggerOperation(
        Summary = "Global search across all content",
        Description = "Search across users, posts, and profiles with filtering and sorting options"
    )]
    [SwaggerResponse(200, "Search completed successfully", typeof(GlobalSearchResponse))]
    [SwaggerResponse(400, "Invalid search parameters")]
    [SwaggerResponse(401, "User not authenticated")]
    public async Task<IActionResult> GlobalSearch(
        [FromQuery, SwaggerParameter("Search query")] string query,
        [FromQuery, SwaggerParameter("Types to search (comma-separated: users,posts,profiles)")] string? types = null,
        [FromQuery, SwaggerParameter("Maximum results per type (1-50)")] int maxResults = 10,
        [FromQuery, SwaggerParameter("Sort by: relevance, date, name")] string sortBy = "relevance")
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("[SearchController.GlobalSearch] START - RequestId={RequestId}, Query={Query}, Types={Types}, MaxResults={MaxResults}, SortBy={SortBy}", 
            requestId, query, types ?? "all", maxResults, sortBy);

        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("[SearchController.GlobalSearch] BAD_REQUEST - Empty query, RequestId={RequestId}", requestId);
                return BadRequest(new { error = "Query parameter is required" });
            }

            if (maxResults < 1 || maxResults > 50)
            {
                _logger.LogInformation("[SearchController.GlobalSearch] Capping maxResults from {Original} to 10, RequestId={RequestId}", 
                    maxResults, requestId);
                maxResults = 10;
            }

            var searchTypes = ParseSearchTypes(types);
            _logger.LogInformation("[SearchController.GlobalSearch] Parsed search types: {SearchTypes}, RequestId={RequestId}", 
                string.Join(",", searchTypes), requestId);
            
            var results = new GlobalSearchResponse { Query = query };

            // Search profiles
            if (searchTypes.Contains("profiles") || searchTypes.Contains("all"))
            {
                try
                {
                    _logger.LogInformation("[SearchController.GlobalSearch] Searching profiles, RequestId={RequestId}", requestId);
                    var profileResults = await _profileService.SearchProfilesAsync(query, 1, maxResults);
                    results.Profiles = profileResults.Items.ToList();
                    results.ProfileCount = profileResults.TotalItems;
                    _logger.LogInformation("[SearchController.GlobalSearch] Profile search completed - Count={Count}, RequestId={RequestId}", 
                        results.ProfileCount, requestId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SearchController.GlobalSearch] PROFILE_SEARCH_ERROR - RequestId={RequestId}", requestId);
                }
            }

            // Search posts
            if (searchTypes.Contains("posts") || searchTypes.Contains("all"))
            {
                try
                {
                    _logger.LogInformation("[SearchController.GlobalSearch] Searching posts, RequestId={RequestId}", requestId);
                    var keycloakId = GetKeycloakIdFromRequest();
                    var (posts, totalCount) = await _postService.SearchPostsAsync(query, keycloakId, 1, maxResults);
                    results.Posts = posts.ToList();
                    results.PostCount = totalCount;
                    _logger.LogInformation("[SearchController.GlobalSearch] Post search completed - Count={Count}, RequestId={RequestId}", 
                        results.PostCount, requestId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SearchController.GlobalSearch] POST_SEARCH_ERROR - RequestId={RequestId}", requestId);
                }
            }

            results.ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            results.TotalResults = results.ProfileCount + results.PostCount;

            _logger.LogInformation("[SearchController.GlobalSearch] SUCCESS - TotalResults={TotalResults}, RequestId={RequestId}, Duration={Duration}ms", 
                results.TotalResults, requestId, results.ProcessingTimeMs);

            return Ok(results);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[SearchController.GlobalSearch] ERROR - RequestId={RequestId}, Duration={Duration}ms", 
                requestId, elapsed);
            return StatusCode(500, new { error = "An error occurred while searching" });
        }
    }

    /// <summary>
    /// Get search suggestions/autocomplete for a query
    /// </summary>
    /// <param name="query">Partial search query</param>
    /// <param name="limit">Maximum number of suggestions (default: 5, max: 10)</param>
    /// <returns>List of search suggestions</returns>
    [HttpGet("suggestions")]
    [SwaggerOperation(
        Summary = "Get search suggestions",
        Description = "Returns autocomplete suggestions based on partial query"
    )]
    [SwaggerResponse(200, "Suggestions retrieved successfully", typeof(SearchSuggestionsResponse))]
    [SwaggerResponse(400, "Invalid parameters")]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery, SwaggerParameter("Partial search query")] string query,
        [FromQuery, SwaggerParameter("Maximum suggestions (1-10)")] int limit = 5)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Ok(new SearchSuggestionsResponse { Suggestions = new List<string>() });
            }

            if (limit < 1 || limit > 10)
            {
                limit = 5;
            }

            var suggestions = new List<string>();

            // Get profile name suggestions
            try
            {
                var profileResults = await _profileService.SearchProfilesAsync(query, 1, limit);
                suggestions.AddRange(profileResults.Items
                    .Select(p => p.DisplayName)
                    .Take(limit));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting profile suggestions");
            }

            return Ok(new SearchSuggestionsResponse
            {
                Query = query,
                Suggestions = suggestions.Distinct().Take(limit).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions");
            return StatusCode(500, new { error = "An error occurred while getting suggestions" });
        }
    }

    private string GetKeycloakIdFromRequest()
    {
        return User.FindFirst("sub")?.Value ?? string.Empty;
    }

    private static List<string> ParseSearchTypes(string? types)
    {
        if (string.IsNullOrWhiteSpace(types))
        {
            return new List<string> { "all" };
        }

        return types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Where(t => t == "users" || t == "posts" || t == "profiles" || t == "all")
            .ToList();
    }

    /// <summary>
    /// Perform semantic search across posts using natural language queries
    /// </summary>
    /// <param name="query">The search query in natural language</param>
    /// <param name="maxResults">Maximum number of results to return (default: 10, max: 50)</param>
    /// <param name="minSimilarity">Minimum similarity threshold (0.0 to 1.0, default: 0.1)</param>
    /// <param name="postType">Optional filter by post type</param>
    /// <returns>List of posts ordered by semantic similarity</returns>
    [HttpGet("posts")]
    [SwaggerOperation(
        Summary = "Semantic search for posts",
        Description = "Search posts using AI-powered semantic understanding. " +
                     "This goes beyond keyword matching to understand meaning and context."
    )]
    [SwaggerResponse(200, "Search results returned successfully", typeof(SemanticSearchResponse))]
    [SwaggerResponse(400, "Invalid search parameters")]
    [SwaggerResponse(401, "User not authenticated")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> ContentEmbedding(
        [FromQuery, SwaggerParameter("Search query in natural language")] string query,
        [FromQuery, SwaggerParameter("Maximum results to return (1-50)")] int maxResults = 10,
        [FromQuery, SwaggerParameter("Minimum similarity score (0.0-1.0)")] float minSimilarity = 0.1f,
        [FromQuery, SwaggerParameter("Filter by post type")] string? postType = null)
    {
        try
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Query parameter is required and cannot be empty" });
            }

            if (maxResults < 1 || maxResults > 50)
            {
                return BadRequest(new { error = "maxResults must be between 1 and 50" });
            }

            if (minSimilarity < 0.0f || minSimilarity > 1.0f)
            {
                return BadRequest(new { error = "minSimilarity must be between 0.0 and 1.0" });
            }

            _logger.LogInformation("Performing semantic search for query: '{Query}' with {MaxResults} max results", 
                query, maxResults);

            // Get all posts with embeddings
            var allPosts = await _postService.GetAllPostsWithEmbeddingsAsync();
            
            if (allPosts.Count == 0)
            {
                return Ok(new SemanticSearchResponse
                {
                    Query = query,
                    Results = new List<SemanticPostResult>(),
                    TotalResults = 0,
                    ProcessingTimeMs = 0
                });
            }

            var startTime = DateTime.UtcNow;

            // Convert posts to embedding candidates using the entity data (string) not DTO data (float[])
            var entityPosts = await _postService.GetAllPostEntitiesWithEmbeddingsAsync();
            var candidates = entityPosts
                .Where(p => !string.IsNullOrEmpty(p.ContentEmbedding))
                .Select(p => (
                    Text: p.Content,
                    Embedding: DeserializeEmbedding(p.ContentEmbedding!),
                    Post: allPosts.First(dto => dto.Id == p.Id) // Map back to DTO
                ))
                .Where(x => x.Embedding != null)
                .Select(x => (x.Text, x.Embedding!, x.Post))
                .ToArray();

            if (candidates.Length == 0)
            {
                return Ok(new SemanticSearchResponse
                {
                    Query = query,
                    Results = new List<SemanticPostResult>(),
                    TotalResults = 0,
                    ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
                });
            }

            // Perform semantic search
            var searchCandidates = candidates.Select(c => (c.Text, c.Item2)).ToArray();
            var searchResults = await _vectorEmbeddingService.PerformSemanticSearchAsync(
                query, searchCandidates, maxResults);

            // Map results back to posts
            var postResults = searchResults
                .Where(r => r.Similarity >= minSimilarity)
                .Select(result =>
                {
                    var matchingPost = candidates.First(c => c.Text == result.Text).Post;
                    return new SemanticPostResult
                    {
                        Post = matchingPost,
                        SimilarityScore = result.Similarity,
                        MatchingText = result.Text
                    };
                })
                .ToList();

            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation("Semantic search completed in {ProcessingTime}ms, found {ResultCount} results", 
                processingTime, postResults.Count);

            return Ok(new SemanticSearchResponse
            {
                Query = query,
                Results = postResults,
                TotalResults = postResults.Count,
                ProcessingTimeMs = processingTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing semantic search for query: '{Query}'", query);
            return StatusCode(500, new { error = "An error occurred while performing the search" });
        }
    }

    /// <summary>
    /// Generate vector embedding for a given text (for testing/debugging purposes)
    /// </summary>
    /// <param name="text">Text to generate embedding for</param>
    /// <returns>Vector embedding information</returns>
    [HttpPost("embed")]
    [SwaggerOperation(
        Summary = "Generate vector embedding",
        Description = "Generate a vector embedding for the provided text. Useful for testing and debugging."
    )]
    [SwaggerResponse(200, "Embedding generated successfully", typeof(EmbeddingResponse))]
    [SwaggerResponse(400, "Invalid input text")]
    [SwaggerResponse(401, "User not authenticated")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GenerateEmbedding([FromBody] EmbeddingRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new { error = "Text is required and cannot be empty" });
            }

            _logger.LogDebug("Generating embedding for text with length: {TextLength}", request.Text.Length);

            var startTime = DateTime.UtcNow;
            var embedding = await _vectorEmbeddingService.GenerateEmbeddingAsync(request.Text);
            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return Ok(new EmbeddingResponse
            {
                Text = request.Text,
                VectorLength = embedding.Vector.Length,
                ProcessingTimeMs = processingTime,
                // Only return first 10 values for brevity in API response
                SampleValues = embedding.Vector.Span.ToArray().Take(10).ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text");
            return StatusCode(500, new { error = "An error occurred while generating the embedding" });
        }
    }

    /// <summary>
    /// Compare similarity between two texts using vector embeddings
    /// </summary>
    /// <param name="request">Texts to compare</param>
    /// <returns>Similarity score and comparison details</returns>
    [HttpPost("similarity")]
    [SwaggerOperation(
        Summary = "Compare text similarity",
        Description = "Calculate semantic similarity between two texts using vector embeddings."
    )]
    [SwaggerResponse(200, "Similarity calculated successfully", typeof(SimilarityResponse))]
    [SwaggerResponse(400, "Invalid input texts")]
    [SwaggerResponse(401, "User not authenticated")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> CompareSimilarity([FromBody] SimilarityRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text1) || string.IsNullOrWhiteSpace(request.Text2))
            {
                return BadRequest(new { error = "Both texts are required and cannot be empty" });
            }

            var startTime = DateTime.UtcNow;

            // Generate embeddings for both texts
            var embedding1 = await _vectorEmbeddingService.GenerateEmbeddingAsync(request.Text1);
            var embedding2 = await _vectorEmbeddingService.GenerateEmbeddingAsync(request.Text2);

            // Calculate similarity
            var similarity = _vectorEmbeddingService.CalculateCosineSimilarity(embedding1, embedding2);

            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return Ok(new SimilarityResponse
            {
                Text1 = request.Text1,
                Text2 = request.Text2,
                SimilarityScore = similarity,
                ProcessingTimeMs = processingTime,
                SimilarityLevel = GetSimilarityLevel(similarity)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing text similarity");
            return StatusCode(500, new { error = "An error occurred while comparing similarity" });
        }
    }

    /// <summary>
    /// Deserialize embedding from JSON string stored in database
    /// </summary>
    private Embedding<float>? DeserializeEmbedding(string embeddingJson)
    {
        try
        {
            var values = JsonSerializer.Deserialize<float[]>(embeddingJson);
            return values != null ? new Embedding<float>(values) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize embedding from JSON");
            return null;
        }
    }

    /// <summary>
    /// Get human-readable similarity level description
    /// </summary>
    private static string GetSimilarityLevel(float similarity) =>
        similarity switch
        {
            >= 0.9f => "Very High",
            >= 0.7f => "High",
            >= 0.5f => "Moderate",
            >= 0.3f => "Low",
            >= 0.1f => "Very Low",
            _ => "Minimal"
        };
}

#region DTOs for Search API

/// <summary>
/// Response model for semantic search results
/// </summary>
public record SemanticSearchResponse
{
    public string Query { get; init; } = string.Empty;
    public List<SemanticPostResult> Results { get; init; } = new();
    public int TotalResults { get; init; }
    public double ProcessingTimeMs { get; init; }
}

/// <summary>
/// Post result with similarity score
/// </summary>
public record SemanticPostResult
{
    public PostDto Post { get; init; } = null!;
    public float SimilarityScore { get; init; }
    public string MatchingText { get; init; } = string.Empty;
}

/// <summary>
/// Request model for generating embeddings
/// </summary>
public record EmbeddingRequest
{
    public string Text { get; init; } = string.Empty;
}

/// <summary>
/// Response model for embedding generation
/// </summary>
public record EmbeddingResponse
{
    public string Text { get; init; } = string.Empty;
    public int VectorLength { get; init; }
    public double ProcessingTimeMs { get; init; }
    public float[] SampleValues { get; init; } = Array.Empty<float>();
}

/// <summary>
/// Request model for similarity comparison
/// </summary>
public record SimilarityRequest
{
    public string Text1 { get; init; } = string.Empty;
    public string Text2 { get; init; } = string.Empty;
}

/// <summary>
/// Response model for similarity comparison
/// </summary>
public record SimilarityResponse
{
    public string Text1 { get; init; } = string.Empty;
    public string Text2 { get; init; } = string.Empty;
    public float SimilarityScore { get; init; }
    public double ProcessingTimeMs { get; init; }
    public string SimilarityLevel { get; init; } = string.Empty;
}

/// <summary>
/// Response model for global search results
/// </summary>
public record GlobalSearchResponse
{
    public string Query { get; init; } = string.Empty;
    public List<ProfileSummaryDto> Profiles { get; set; } = new();
    public List<PostDto> Posts { get; set; } = new();
    public int ProfileCount { get; set; }
    public int PostCount { get; set; }
    public int TotalResults { get; set; }
    public double ProcessingTimeMs { get; set; }
}

/// <summary>
/// Response model for search suggestions
/// </summary>
public record SearchSuggestionsResponse
{
    public string Query { get; init; } = string.Empty;
    public List<string> Suggestions { get; init; } = new();
}

#endregion