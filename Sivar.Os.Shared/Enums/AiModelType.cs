namespace Sivar.Os.Shared.Enums;

/// <summary>
/// Type of AI model for categorization and billing purposes
/// </summary>
public enum AiModelType
{
    /// <summary>
    /// Chat/completion models (e.g., gpt-4o-mini, gpt-4o)
    /// </summary>
    Chat = 0,

    /// <summary>
    /// Embedding models (e.g., text-embedding-3-small)
    /// </summary>
    Embedding = 1,

    /// <summary>
    /// Image generation models (e.g., dall-e-3)
    /// </summary>
    ImageGeneration = 2,

    /// <summary>
    /// Speech-to-text models (e.g., whisper)
    /// </summary>
    SpeechToText = 3,

    /// <summary>
    /// Text-to-speech models (e.g., tts-1)
    /// </summary>
    TextToSpeech = 4,

    /// <summary>
    /// Reasoning models (e.g., o1, o3)
    /// </summary>
    Reasoning = 5,

    /// <summary>
    /// Fine-tuned custom models
    /// </summary>
    FineTuned = 6,

    /// <summary>
    /// Local models (e.g., Ollama, Llama)
    /// </summary>
    Local = 7
}
