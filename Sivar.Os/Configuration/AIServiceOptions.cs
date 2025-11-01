namespace Sivar.Os.Configuration;

/// <summary>
/// Configuration for AI services (Sentiment Analysis, Embeddings, etc.)
/// </summary>
public class AIServiceOptions
{
    /// <summary>
    /// Sentiment analysis configuration
    /// </summary>
    public AIServiceModeOptions SentimentAnalysis { get; set; } = new();

    /// <summary>
    /// Embeddings generation configuration
    /// </summary>
    public AIServiceModeOptions Embeddings { get; set; } = new();
}

/// <summary>
/// Configuration for a specific AI service mode
/// </summary>
public class AIServiceModeOptions
{
    /// <summary>
    /// AI service mode: Adaptive, ClientOnly, or ServerOnly
    /// </summary>
    public AIServiceMode Mode { get; set; } = AIServiceMode.Adaptive;

    /// <summary>
    /// Description of the mode (for configuration UI)
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// AI service execution mode
/// </summary>
public enum AIServiceMode
{
    /// <summary>
    /// Adaptive Loading: Use client-side if models ready, fallback to server
    /// ✅ Best UX: Instant processing + progressive enhancement
    /// ✅ Best quality when ready
    /// ✅ Works for all users (cached or not)
    /// </summary>
    Adaptive = 0,

    /// <summary>
    /// Client-Only: Always use browser-based ML (Transformers.js)
    /// ✅ Privacy-first (no data sent to server)
    /// ✅ Free (no API costs)
    /// ⚠️ Users must wait for models to download on first visit
    /// ⚠️ May fail if models don't load
    /// </summary>
    ClientOnly = 1,

    /// <summary>
    /// Server-Only: Always use server-side processing
    /// ✅ Instant processing (no model downloads)
    /// ✅ Consistent results
    /// ⚠️ Lower quality (keyword-based unless ML.NET added)
    /// ⚠️ Potential API costs if using cloud AI
    /// </summary>
    ServerOnly = 2
}
