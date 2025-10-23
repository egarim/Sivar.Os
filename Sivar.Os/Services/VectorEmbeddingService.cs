using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Sivar.Os.Shared.Services;
using System.Numerics.Tensors;

namespace Sivar.Os.Services;

/// <summary>
/// Service for generating vector embeddings using Microsoft.Extensions.AI
/// Supports both Ollama and OpenAI embedding generators
/// </summary>
public class VectorEmbeddingService : IVectorEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly VectorEmbeddingOptions _options;
    private readonly ILogger<VectorEmbeddingService> _logger;

    public VectorEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IOptions<VectorEmbeddingOptions> options,
        ILogger<VectorEmbeddingService> logger)
    {
        _embeddingGenerator = embeddingGenerator;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Generate an embedding vector for a single text string
    /// </summary>
    public async Task<Embedding<float>> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        }

        try
        {
            _logger.LogDebug("Generating embedding for text with length: {TextLength}", text.Length);
            
            // Truncate text if it exceeds maximum length
            var processedText = text.Length > _options.MaxTextLength 
                ? text[.._options.MaxTextLength] 
                : text;

            var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(processedText);
            
            _logger.LogDebug("Generated embedding with vector length: {VectorLength}", embedding.Vector.Length);
            
            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text");
            throw;
        }
    }

    /// <summary>
    /// Generate embeddings for multiple text strings using batch processing
    /// </summary>
    public async Task<(string Text, Embedding<float> Embedding)[]> GenerateBatchEmbeddingsAsync(List<string> texts)
    {
        if (texts == null || texts.Count == 0)
        {
            return Array.Empty<(string, Embedding<float>)>();
        }

        try
        {
            _logger.LogDebug("Generating batch embeddings for {TextCount} texts", texts.Count);

            // Process texts in batches to avoid overwhelming the service
            var batchSize = _options.BatchSize;
            var results = new List<(string Text, Embedding<float> Embedding)>();

            for (int i = 0; i < texts.Count; i += batchSize)
            {
                var batch = texts.Skip(i).Take(batchSize).ToList();
                var batchResults = await ProcessBatchAsync(batch);
                results.AddRange(batchResults);
            }

            _logger.LogDebug("Generated {ResultCount} embeddings", results.Count);
            return results.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating batch embeddings");
            throw;
        }
    }

    /// <summary>
    /// Perform semantic search to find the most similar texts to a query
    /// </summary>
    public async Task<List<SemanticSearchResult>> PerformSemanticSearchAsync(
        string query, 
        (string Text, Embedding<float> Embedding)[] candidates, 
        int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty", nameof(query));
        }

        if (candidates == null || candidates.Length == 0)
        {
            return new List<SemanticSearchResult>();
        }

        try
        {
            _logger.LogDebug("Performing semantic search for query: '{Query}' against {CandidateCount} candidates", 
                query, candidates.Length);

            // Generate embedding for the query
            var queryEmbedding = await GenerateEmbeddingAsync(query);

            // Calculate similarities and sort by relevance
            var results = candidates
                .Select(candidate => new SemanticSearchResult
                {
                    Text = candidate.Text,
                    Similarity = CalculateCosineSimilarity(queryEmbedding, candidate.Embedding),
                    Metadata = null
                })
                .Where(r => r.Similarity >= _options.MinimumSimilarityThreshold)
                .OrderByDescending(r => r.Similarity)
                .Take(maxResults)
                .ToList();

            _logger.LogDebug("Found {ResultCount} results above similarity threshold {Threshold}", 
                results.Count, _options.MinimumSimilarityThreshold);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing semantic search");
            throw;
        }
    }

    /// <summary>
    /// Calculate cosine similarity between two embeddings
    /// </summary>
    public float CalculateCosineSimilarity(Embedding<float> embedding1, Embedding<float> embedding2)
    {
        if (embedding1.Vector.Length != embedding2.Vector.Length)
        {
            throw new ArgumentException("Embedding vectors must have the same length");
        }

        return TensorPrimitives.CosineSimilarity(embedding1.Vector.Span, embedding2.Vector.Span);
    }

    /// <summary>
    /// Process a batch of texts and generate embeddings
    /// </summary>
    private async Task<List<(string Text, Embedding<float> Embedding)>> ProcessBatchAsync(List<string> batch)
    {
        var results = new List<(string Text, Embedding<float> Embedding)>();

        foreach (var text in batch)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                var embedding = await GenerateEmbeddingAsync(text);
                results.Add((text, embedding));
            }
        }

        return results;
    }
}

/// <summary>
/// Configuration options for vector embedding service
/// </summary>
public class VectorEmbeddingOptions
{
    public const string SectionName = "VectorEmbedding";

    /// <summary>
    /// Maximum text length to process (longer texts will be truncated)
    /// </summary>
    public int MaxTextLength { get; set; } = 8000;

    /// <summary>
    /// Batch size for processing multiple texts
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Minimum similarity threshold for search results
    /// </summary>
    public float MinimumSimilarityThreshold { get; set; } = 0.1f;

    /// <summary>
    /// Ollama service configuration
    /// </summary>
    public OllamaOptions Ollama { get; set; } = new();

    /// <summary>
    /// OpenAI service configuration
    /// </summary>
    public OpenAIOptions OpenAI { get; set; } = new();

    /// <summary>
    /// Which embedding provider to use: "Ollama" or "OpenAI"
    /// </summary>
    public string Provider { get; set; } = "Ollama";
}

/// <summary>
/// Ollama-specific configuration options
/// </summary>
public class OllamaOptions
{
    /// <summary>
    /// Ollama service endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = "http://127.0.0.1:11434";

    /// <summary>
    /// Model to use for embeddings
    /// </summary>
    public string ModelId { get; set; } = "all-minilm:latest";
}

/// <summary>
/// OpenAI-specific configuration options
/// </summary>
public class OpenAIOptions
{
    /// <summary>
    /// OpenAI API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model to use for embeddings
    /// </summary>
    public string ModelId { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Optional organization ID
    /// </summary>
    public string? OrganizationId { get; set; }
}