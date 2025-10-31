namespace Sivar.Os.Configuration
{
    /// <summary>
    /// Configuration settings for hybrid embedding generation system
    /// </summary>
    public class EmbeddingOptions
    {
        /// <summary>
        /// Whether to prefer client-side embedding generation when available
        /// Default: true (use browser-based Transformers.js when possible)
        /// </summary>
        public bool PreferClientSide { get; set; } = true;

        /// <summary>
        /// Whether to enable server-side fallback when client-side generation fails
        /// Default: true (ensures embeddings are always generated)
        /// </summary>
        public bool EnableServerFallback { get; set; } = true;

        /// <summary>
        /// The model name used for client-side embedding generation
        /// Must match the model used in wwwroot/js/embeddings.js
        /// Default: "Xenova/all-MiniLM-L6-v2"
        /// </summary>
        public string ModelName { get; set; } = "Xenova/all-MiniLM-L6-v2";

        /// <summary>
        /// Expected dimensions for embedding vectors
        /// Both client-side and server-side models must produce this dimension
        /// Default: 384 (for all-MiniLM-L6-v2)
        /// </summary>
        public int ExpectedDimensions { get; set; } = 384;

        /// <summary>
        /// Whether to warmup the client-side model on application startup
        /// This pre-loads the model in the browser for faster first embedding
        /// Default: false (lazy loading on first use)
        /// </summary>
        public bool WarmupOnStartup { get; set; } = false;

        /// <summary>
        /// Timeout in milliseconds for client-side embedding generation
        /// If exceeded, will fallback to server-side generation
        /// Default: 30000 (30 seconds)
        /// </summary>
        public int ClientTimeout { get; set; } = 30000;
    }
}
