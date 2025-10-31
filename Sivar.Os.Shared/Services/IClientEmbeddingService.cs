namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service for client-side embedding generation using Transformers.js in the browser
/// Provides browser-based text embedding generation with automatic fallback to server-side
/// </summary>
/// <remarks>
/// This service uses WebAssembly and Transformers.js to generate embeddings directly in the user's browser.
/// Benefits:
/// - Zero API costs (no calls to OpenAI/Azure)
/// - Faster response (no network latency)
/// - Enhanced privacy (content never leaves browser)
/// - Offline capability (once model is cached)
/// 
/// Returns null if client-side generation is unavailable or fails, triggering automatic server-side fallback.
/// </remarks>
public interface IClientEmbeddingService
{
    /// <summary>
    /// Attempts to generate a 384-dimensional embedding on the client-side using Transformers.js
    /// </summary>
    /// <param name="text">Text to embed (recommended max ~512 tokens / ~2000 characters)</param>
    /// <returns>
    /// 384-dimensional embedding array if successful, or null if:
    /// - Client-side generation is not supported (no WebAssembly)
    /// - Model failed to load
    /// - Generation threw an exception
    /// - Result validation failed
    /// Null return triggers automatic server-side fallback in calling code.
    /// </returns>
    /// <example>
    /// <code>
    /// var embedding = await _clientEmbeddingService.TryGenerateEmbeddingAsync(postContent);
    /// if (embedding == null) {
    ///     // Fallback to server-side
    ///     embedding = await _serverEmbeddingService.GenerateEmbeddingAsync(postContent);
    /// }
    /// </code>
    /// </example>
    Task<float[]?> TryGenerateEmbeddingAsync(string text);

    /// <summary>
    /// Checks if client-side embeddings are supported in the current browser environment
    /// </summary>
    /// <returns>True if WebAssembly and required APIs are available, false otherwise</returns>
    /// <remarks>
    /// Performs a lightweight check for:
    /// - WebAssembly support
    /// - Fetch API availability
    /// - Module import capability
    /// This is called once during initialization and cached.
    /// </remarks>
    Task<bool> IsSupportedAsync();

    /// <summary>
    /// Pre-warms the embedding model by loading it into memory
    /// </summary>
    /// <returns>True if warmup succeeded, false otherwise</returns>
    /// <remarks>
    /// Optional optimization to reduce first-time latency.
    /// Call this on app startup or when user navigates to post creation page.
    /// Model is ~25MB and will be cached in browser for subsequent uses.
    /// </remarks>
    Task<bool> WarmupModelAsync();
}
