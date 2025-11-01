using Microsoft.JSInterop;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Services;

/// <summary>
/// Client-side sentiment analysis using Transformers.js via JavaScript Interop
/// </summary>
public class ClientSentimentAnalysisService : IClientSentimentAnalysisService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ClientSentimentAnalysisService> _logger;
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);
    private bool _isInitialized = false;

    public ClientSentimentAnalysisService(
        IJSRuntime jsRuntime,
        ILogger<ClientSentimentAnalysisService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SentimentAnalysisResultDto?> TryAnalyzeAsync(string text, string language)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("[ClientSentiment] Empty text provided for analysis");
            return null;
        }

        try
        {
            _logger.LogInformation("[ClientSentiment] Analyzing text ({Language}, {Length} chars)", 
                language, text.Length);

            // Check if browser supports sentiment analysis
            var isSupported = await IsSupportedAsync();
            if (!isSupported)
            {
                _logger.LogWarning("[ClientSentiment] Browser does not support sentiment analysis");
                return null;
            }

            // Call JavaScript sentiment analyzer
            var result = await _jsRuntime.InvokeAsync<SentimentAnalysisResult>(
                "SentimentAnalyzer.analyze", 
                text, 
                language);

            if (result == null)
            {
                _logger.LogWarning("[ClientSentiment] JS returned null result");
                return null;
            }

            // Convert JS result to DTO
            var dto = new SentimentAnalysisResultDto
            {
                PrimaryEmotion = result.PrimaryEmotion,
                EmotionScore = (decimal)result.EmotionScore,
                SentimentPolarity = (decimal)result.SentimentPolarity,
                EmotionScores = new EmotionScoresDto
                {
                    Joy = (decimal)result.EmotionScores.Joy,
                    Sadness = (decimal)result.EmotionScores.Sadness,
                    Anger = (decimal)result.EmotionScores.Anger,
                    Fear = (decimal)result.EmotionScores.Fear,
                    Neutral = (decimal)result.EmotionScores.Neutral
                },
                HasAnger = result.HasAnger,
                NeedsReview = result.NeedsReview,
                Language = result.Language,
                AnalysisSource = "client",
                AnalyzedAt = result.AnalyzedAt
            };

            _logger.LogInformation("[ClientSentiment] ✅ Analysis complete: {Emotion} (score: {Score:F2})", 
                dto.PrimaryEmotion, dto.EmotionScore);

            return dto;
        }
        catch (JSException jsEx)
        {
            _logger.LogError(jsEx, "[ClientSentiment] ❌ JavaScript error during analysis");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ClientSentiment] ❌ Error during sentiment analysis");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsSupportedAsync()
    {
        try
        {
            var supported = await _jsRuntime.InvokeAsync<bool>("SentimentAnalyzer.isSupported");
            return supported;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AreModelsReadyAsync()
    {
        try
        {
            // Call JavaScript to check if models are loaded and ready
            var isReady = await _jsRuntime.InvokeAsync<bool>("SentimentAnalyzer.isReady");

            _logger.LogDebug(
                "[ClientSentiment] Models ready check: {IsReady}",
                isReady);

            return isReady;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[ClientSentiment] Failed to check model readiness - assuming not ready");
            return false;
        }
    }

    /// <summary>
    /// JavaScript interop data structure for sentiment analysis result
    /// </summary>
    private class SentimentAnalysisResult
    {
        public string PrimaryEmotion { get; set; } = "Neutral";
        public double EmotionScore { get; set; }
        public double SentimentPolarity { get; set; }
        public EmotionScoresJs EmotionScores { get; set; } = new();
        public bool HasAnger { get; set; }
        public bool NeedsReview { get; set; }
        public string Language { get; set; } = "en";
        public string AnalysisSource { get; set; } = "client";
        public DateTime AnalyzedAt { get; set; }
    }

    private class EmotionScoresJs
    {
        public double Joy { get; set; }
        public double Sadness { get; set; }
        public double Anger { get; set; }
        public double Fear { get; set; }
        public double Neutral { get; set; }
    }
}
