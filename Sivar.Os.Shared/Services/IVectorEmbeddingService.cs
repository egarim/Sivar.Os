using Microsoft.Extensions.AI;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service for generating vector embeddings and performing semantic searches
/// </summary>
public interface IVectorEmbeddingService
{
    /// <summary>
    /// Generate an embedding vector for a single text string
    /// </summary>
    /// <param name="text">The text to generate an embedding for</param>
    /// <returns>An embedding containing the vector representation</returns>
    Task<Embedding<float>> GenerateEmbeddingAsync(string text);

    /// <summary>
    /// Generate embeddings for multiple text strings
    /// </summary>
    /// <param name="texts">The texts to generate embeddings for</param>
    /// <returns>Array of text-embedding pairs</returns>
    Task<(string Text, Embedding<float> Embedding)[]> GenerateBatchEmbeddingsAsync(List<string> texts);

    /// <summary>
    /// Perform semantic search to find the most similar texts to a query
    /// </summary>
    /// <param name="query">The search query</param>
    /// <param name="candidates">Text-embedding pairs to search within</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>Ordered list of results with similarity scores</returns>
    Task<List<SemanticSearchResult>> PerformSemanticSearchAsync(
        string query, 
        (string Text, Embedding<float> Embedding)[] candidates, 
        int maxResults = 10);

    /// <summary>
    /// Calculate cosine similarity between two embeddings
    /// </summary>
    /// <param name="embedding1">First embedding</param>
    /// <param name="embedding2">Second embedding</param>
    /// <returns>Similarity score between -1 and 1</returns>
    float CalculateCosineSimilarity(Embedding<float> embedding1, Embedding<float> embedding2);

    /// <summary>
    /// Convert Microsoft.Extensions.AI Embedding to PostgreSQL vector string format
    /// Returns string in PostgreSQL vector format: "[0.1,0.2,0.3,...]"
    /// </summary>
    /// <param name="embedding">The embedding to convert</param>
    /// <returns>String representation for PostgreSQL vector(384) column</returns>
    string ToPostgresVector(Embedding<float> embedding);

    /// <summary>
    /// Convert float array to PostgreSQL vector string format
    /// Used for client-side embeddings from Transformers.js
    /// Returns string in PostgreSQL vector format: "[0.1,0.2,0.3,...]"
    /// </summary>
    /// <param name="embedding">The embedding array to convert (typically 384 dimensions)</param>
    /// <returns>String representation for PostgreSQL vector(384) column</returns>
    string ToPostgresVector(float[] embedding);
}

/// <summary>
/// Represents a semantic search result with similarity score
/// </summary>
public class SemanticSearchResult
{
    public string Text { get; set; } = string.Empty;
    public float Similarity { get; set; }
    public object? Metadata { get; set; }
}