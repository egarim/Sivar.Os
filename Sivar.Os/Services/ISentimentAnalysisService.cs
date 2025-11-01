using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Services;

/// <summary>
/// Client-side sentiment analysis service using Transformers.js
/// </summary>
public interface IClientSentimentAnalysisService
{
    /// <summary>
    /// Attempts to analyze text sentiment using client-side AI models
    /// </summary>
    /// <param name="text">Text to analyze</param>
    /// <param name="language">Language code (en or es)</param>
    /// <returns>Sentiment analysis result or null if client-side analysis fails</returns>
    Task<SentimentAnalysisResultDto?> TryAnalyzeAsync(string text, string language);
    
    /// <summary>
    /// Checks if client-side sentiment analysis is supported in the browser
    /// </summary>
    /// <returns>True if supported, false otherwise</returns>
    Task<bool> IsSupportedAsync();

    /// <summary>
    /// Checks if client-side AI models are fully loaded and ready to use
    /// Used for adaptive loading pattern to determine routing
    /// </summary>
    /// <returns>True if models are loaded and ready, false otherwise</returns>
    Task<bool> AreModelsReadyAsync();
}

/// <summary>
/// Server-side sentiment analysis service (fallback)
/// </summary>
public interface IServerSentimentAnalysisService
{
    /// <summary>
    /// Analyzes text sentiment using server-side logic or external API
    /// </summary>
    /// <param name="text">Text to analyze</param>
    /// <param name="language">Language code (en or es)</param>
    /// <returns>Sentiment analysis result</returns>
    Task<SentimentAnalysisResultDto> AnalyzeAsync(string text, string language);
}

/// <summary>
/// Hybrid sentiment analysis service (tries client first, falls back to server)
/// </summary>
public interface ISentimentAnalysisService
{
    /// <summary>
    /// Analyzes text sentiment using hybrid approach:
    /// 1. Try client-side analysis first (privacy-first, no network calls)
    /// 2. Fall back to server-side analysis if client fails
    /// </summary>
    /// <param name="text">Text to analyze</param>
    /// <param name="language">Language code (en or es)</param>
    /// <returns>Sentiment analysis result</returns>
    Task<SentimentAnalysisResultDto> AnalyzeAsync(string text, string language);
}
