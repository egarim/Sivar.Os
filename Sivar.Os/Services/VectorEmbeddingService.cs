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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("[VectorEmbeddingService.GenerateEmbeddingAsync] Validation failed - RequestId={RequestId}, Reason=NullOrEmpty",
                requestId);
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        }

        _logger.LogInformation("[VectorEmbeddingService.GenerateEmbeddingAsync] START - RequestId={RequestId}, TextLength={TextLength}, Provider={Provider}",
            requestId, text.Length, _options.Provider);

        try
        {
            // Truncate text if it exceeds maximum length
            var processedText = text.Length > _options.MaxTextLength 
                ? text[.._options.MaxTextLength] 
                : text;

            if (processedText.Length != text.Length)
            {
                _logger.LogInformation("[VectorEmbeddingService.GenerateEmbeddingAsync] Text truncated - RequestId={RequestId}, OriginalLength={OriginalLength}, TruncatedLength={TruncatedLength}, MaxLength={MaxLength}",
                    requestId, text.Length, processedText.Length, _options.MaxTextLength);
            }

            _logger.LogInformation("[VectorEmbeddingService.GenerateEmbeddingAsync] Calling embedding generator - RequestId={RequestId}, ProcessedTextLength={ProcessedTextLength}",
                requestId, processedText.Length);

            var embedding = await _embeddingGenerator.GenerateAsync(processedText);
            
            _logger.LogInformation("[VectorEmbeddingService.GenerateEmbeddingAsync] Embedding generated - RequestId={RequestId}, VectorLength={VectorLength}",
                requestId, embedding.Vector.Length);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[VectorEmbeddingService.GenerateEmbeddingAsync] SUCCESS - RequestId={RequestId}, VectorLength={VectorLength}, Duration={Duration}ms",
                requestId, embedding.Vector.Length, elapsed);
            
            return embedding;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[VectorEmbeddingService.GenerateEmbeddingAsync] ERROR - RequestId={RequestId}, TextLength={TextLength}, Duration={Duration}ms, Provider={Provider}",
                requestId, text.Length, elapsed, _options.Provider);
            throw;
        }
    }

    /// <summary>
    /// Generate embeddings for multiple text strings using batch processing
    /// </summary>
    public async Task<(string Text, Embedding<float> Embedding)[]> GenerateBatchEmbeddingsAsync(List<string> texts)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        if (texts == null || texts.Count == 0)
        {
            _logger.LogWarning("[VectorEmbeddingService.GenerateBatchEmbeddingsAsync] Empty batch - RequestId={RequestId}, TextCount={TextCount}",
                requestId, texts?.Count ?? 0);
            return Array.Empty<(string, Embedding<float>)>();
        }

        _logger.LogInformation("[VectorEmbeddingService.GenerateBatchEmbeddingsAsync] START - RequestId={RequestId}, TextCount={TextCount}, BatchSize={BatchSize}, Provider={Provider}",
            requestId, texts.Count, _options.BatchSize, _options.Provider);

        try
        {
            // Process texts in batches to avoid overwhelming the service
            var batchSize = _options.BatchSize;
            var results = new List<(string Text, Embedding<float> Embedding)>();
            var totalBatches = (texts.Count + batchSize - 1) / batchSize;
            var currentBatch = 0;

            for (int i = 0; i < texts.Count; i += batchSize)
            {
                currentBatch++;
                var batch = texts.Skip(i).Take(batchSize).ToList();

                _logger.LogInformation("[VectorEmbeddingService.GenerateBatchEmbeddingsAsync] Processing batch - RequestId={RequestId}, Batch={CurrentBatch}/{TotalBatches}, ItemsInBatch={ItemsInBatch}",
                    requestId, currentBatch, totalBatches, batch.Count);

                var batchResults = await ProcessBatchAsync(batch, requestId);
                results.AddRange(batchResults);

                _logger.LogInformation("[VectorEmbeddingService.GenerateBatchEmbeddingsAsync] Batch processed - RequestId={RequestId}, Batch={CurrentBatch}/{TotalBatches}, ResultCount={ResultCount}",
                    requestId, currentBatch, totalBatches, batchResults.Count);
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[VectorEmbeddingService.GenerateBatchEmbeddingsAsync] SUCCESS - RequestId={RequestId}, ResultCount={ResultCount}, TotalBatches={TotalBatches}, Duration={Duration}ms",
                requestId, results.Count, totalBatches, elapsed);

            return results.ToArray();
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[VectorEmbeddingService.GenerateBatchEmbeddingsAsync] ERROR - RequestId={RequestId}, TextCount={TextCount}, Duration={Duration}ms, Provider={Provider}",
                requestId, texts.Count, elapsed, _options.Provider);
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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("[VectorEmbeddingService.PerformSemanticSearchAsync] Validation failed - RequestId={RequestId}, Reason=NullOrEmptyQuery",
                requestId);
            throw new ArgumentException("Query cannot be null or empty", nameof(query));
        }

        if (candidates == null || candidates.Length == 0)
        {
            _logger.LogWarning("[VectorEmbeddingService.PerformSemanticSearchAsync] No candidates provided - RequestId={RequestId}, CandidateCount={CandidateCount}",
                requestId, candidates?.Length ?? 0);
            return new List<SemanticSearchResult>();
        }

        _logger.LogInformation("[VectorEmbeddingService.PerformSemanticSearchAsync] START - RequestId={RequestId}, Query={Query}, CandidateCount={CandidateCount}, MaxResults={MaxResults}, Provider={Provider}",
            requestId, query, candidates.Length, maxResults, _options.Provider);

        try
        {
            // Generate embedding for the query
            _logger.LogInformation("[VectorEmbeddingService.PerformSemanticSearchAsync] Generating query embedding - RequestId={RequestId}, QueryLength={QueryLength}",
                requestId, query.Length);

            var queryEmbedding = await GenerateEmbeddingAsync(query);

            _logger.LogInformation("[VectorEmbeddingService.PerformSemanticSearchAsync] Query embedding generated - RequestId={RequestId}, QueryVectorLength={QueryVectorLength}",
                requestId, queryEmbedding.Vector.Length);

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

            _logger.LogInformation("[VectorEmbeddingService.PerformSemanticSearchAsync] Similarity filtering complete - RequestId={RequestId}, TotalCandidates={TotalCandidates}, AboveThreshold={AboveThreshold}, Threshold={Threshold}, FinalResults={FinalResults}",
                requestId, candidates.Length, results.Count, _options.MinimumSimilarityThreshold, Math.Min(results.Count, maxResults));

            var topResults = results.Take(maxResults).ToList();
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[VectorEmbeddingService.PerformSemanticSearchAsync] SUCCESS - RequestId={RequestId}, ResultCount={ResultCount}, Duration={Duration}ms",
                requestId, topResults.Count, elapsed);

            return topResults;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[VectorEmbeddingService.PerformSemanticSearchAsync] ERROR - RequestId={RequestId}, Query={Query}, CandidateCount={CandidateCount}, Duration={Duration}ms, Provider={Provider}",
                requestId, query, candidates.Length, elapsed, _options.Provider);
            throw;
        }
    }

    /// <summary>
    /// Calculate cosine similarity between two embeddings
    /// </summary>
    public float CalculateCosineSimilarity(Embedding<float> embedding1, Embedding<float> embedding2)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[VectorEmbeddingService.CalculateCosineSimilarity] START - RequestId={RequestId}, Vector1Length={Vector1Length}, Vector2Length={Vector2Length}",
            requestId, embedding1.Vector.Length, embedding2.Vector.Length);

        try
        {
            if (embedding1.Vector.Length != embedding2.Vector.Length)
            {
                _logger.LogWarning("[VectorEmbeddingService.CalculateCosineSimilarity] Vector length mismatch - RequestId={RequestId}, Vector1Length={Vector1Length}, Vector2Length={Vector2Length}",
                    requestId, embedding1.Vector.Length, embedding2.Vector.Length);
                throw new ArgumentException("Embedding vectors must have the same length");
            }

            var similarity = TensorPrimitives.CosineSimilarity(embedding1.Vector.Span, embedding2.Vector.Span);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[VectorEmbeddingService.CalculateCosineSimilarity] SUCCESS - RequestId={RequestId}, Similarity={Similarity}, VectorLength={VectorLength}, Duration={Duration}ms",
                requestId, similarity, embedding1.Vector.Length, elapsed);

            return similarity;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[VectorEmbeddingService.CalculateCosineSimilarity] ERROR - RequestId={RequestId}, Vector1Length={Vector1Length}, Vector2Length={Vector2Length}, Duration={Duration}ms",
                requestId, embedding1.Vector.Length, embedding2.Vector.Length, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Converts an Embedding<float> to a string in PostgreSQL vector format
    /// Returns format: "[0.1,0.2,0.3,...]" for storage in vector(384) column
    /// </summary>
    public string ToPostgresVector(Embedding<float> embedding)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[VectorEmbeddingService.ToPostgresVector] START - RequestId={RequestId}, EmbeddingLength={EmbeddingLength}",
            requestId, embedding.Vector.Length);

        try
        {
            // Convert to PostgreSQL vector format: "[val1,val2,val3,...]"
            // IMPORTANT: Use InvariantCulture to ensure periods are used as decimal separators
            var vectorString = "[" + string.Join(",", embedding.Vector.ToArray().Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[VectorEmbeddingService.ToPostgresVector] SUCCESS - RequestId={RequestId}, VectorDimensions={VectorDimensions}, Duration={Duration}ms",
                requestId, embedding.Vector.Length, elapsed);

            return vectorString;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[VectorEmbeddingService.ToPostgresVector] ERROR - RequestId={RequestId}, EmbeddingLength={EmbeddingLength}, Duration={Duration}ms",
                requestId, embedding.Vector.Length, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Converts a float array to a string in PostgreSQL vector format
    /// Used for client-side embeddings generated by Transformers.js
    /// Returns format: "[0.1,0.2,0.3,...]" for storage in vector(384) column
    /// </summary>
    /// <param name="embedding">Float array embedding (typically 384 dimensions from client-side)</param>
    /// <returns>String representation for PostgreSQL vector column</returns>
    public string ToPostgresVector(float[] embedding)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[VectorEmbeddingService.ToPostgresVector] START (float[]) - RequestId={RequestId}, EmbeddingLength={EmbeddingLength}, Source=ClientSide",
            requestId, embedding?.Length ?? 0);

        try
        {
            if (embedding == null || embedding.Length == 0)
            {
                _logger.LogWarning("[VectorEmbeddingService.ToPostgresVector] Invalid embedding array - RequestId={RequestId}",
                    requestId);
                throw new ArgumentException("Embedding array cannot be null or empty", nameof(embedding));
            }

            // Convert to PostgreSQL vector format: "[val1,val2,val3,...]"
            // IMPORTANT: Use InvariantCulture to ensure periods are used as decimal separators
            var vectorString = "[" + string.Join(",", embedding.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[VectorEmbeddingService.ToPostgresVector] SUCCESS (float[]) - RequestId={RequestId}, VectorDimensions={VectorDimensions}, Duration={Duration}ms, Source=ClientSide",
                requestId, embedding.Length, elapsed);

            return vectorString;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[VectorEmbeddingService.ToPostgresVector] ERROR (float[]) - RequestId={RequestId}, EmbeddingLength={EmbeddingLength}, Duration={Duration}ms, Source=ClientSide",
                requestId, embedding?.Length ?? 0, elapsed);
            throw;
        }
    }

    /// <summary>
    /// Process a batch of texts and generate embeddings
    /// </summary>
    private async Task<List<(string Text, Embedding<float> Embedding)>> ProcessBatchAsync(List<string> batch, Guid requestId)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[VectorEmbeddingService.ProcessBatchAsync] START - RequestId={RequestId}, ItemCount={ItemCount}",
            requestId, batch.Count);

        var results = new List<(string Text, Embedding<float> Embedding)>();
        var successCount = 0;
        var skipCount = 0;

        try
        {
            for (int i = 0; i < batch.Count; i++)
            {
                var text = batch[i];

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("[VectorEmbeddingService.ProcessBatchAsync] Skipping empty item - RequestId={RequestId}, Index={Index}/{Total}",
                        requestId, i + 1, batch.Count);
                    skipCount++;
                    continue;
                }

                _logger.LogInformation("[VectorEmbeddingService.ProcessBatchAsync] Processing item - RequestId={RequestId}, Index={Index}/{Total}, TextLength={TextLength}",
                    requestId, i + 1, batch.Count, text.Length);

                var embedding = await GenerateEmbeddingAsync(text);
                results.Add((text, embedding));
                successCount++;

                _logger.LogInformation("[VectorEmbeddingService.ProcessBatchAsync] Item processed - RequestId={RequestId}, Index={Index}/{Total}, VectorLength={VectorLength}",
                    requestId, i + 1, batch.Count, embedding.Vector.Length);
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[VectorEmbeddingService.ProcessBatchAsync] SUCCESS - RequestId={RequestId}, ProcessedCount={ProcessedCount}, SkippedCount={SkippedCount}, ResultCount={ResultCount}, Duration={Duration}ms",
                requestId, successCount, skipCount, results.Count, elapsed);

            return results;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[VectorEmbeddingService.ProcessBatchAsync] ERROR - RequestId={RequestId}, ItemCount={ItemCount}, ProcessedCount={ProcessedCount}, Duration={Duration}ms",
                requestId, batch.Count, successCount, elapsed);
            throw;
        }
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