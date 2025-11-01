using Microsoft.Extensions.Options;
using Sivar.Os.Configuration;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Services;

/// <summary>
/// Configurable sentiment analysis service with three modes:
/// - Adaptive: Smart routing (client if ready, server fallback) - DEFAULT
/// - ClientOnly: Always use browser ML (privacy-first, may wait for models)
/// - ServerOnly: Always use server (instant, keyword-based)
/// 
/// Mode configured in appsettings.json: AIServices.SentimentAnalysis.Mode
/// </summary>
public class SentimentAnalysisService : ISentimentAnalysisService
{
    private readonly IClientSentimentAnalysisService _clientService;
    private readonly IServerSentimentAnalysisService _serverService;
    private readonly ILogger<SentimentAnalysisService> _logger;
    private readonly AIServiceMode _mode;

    public SentimentAnalysisService(
        IClientSentimentAnalysisService clientService,
        IServerSentimentAnalysisService serverService,
        ILogger<SentimentAnalysisService> logger,
        IOptions<AIServiceOptions> options)
    {
        _clientService = clientService;
        _serverService = serverService;
        _logger = logger;
        _mode = options.Value.SentimentAnalysis.Mode;

        _logger.LogInformation(
            "[SentimentAnalysis] Service initialized with mode: {Mode}", 
            _mode);
    }

    /// <inheritdoc/>
    public async Task<SentimentAnalysisResultDto> AnalyzeAsync(string text, string language)
    {
        _logger.LogInformation(
            "[SentimentAnalysis] Starting analysis - Mode={Mode}, Language={Language}, Length={Length} chars", 
            _mode, language, text.Length);

        return _mode switch
        {
            AIServiceMode.ClientOnly => await AnalyzeClientOnlyAsync(text, language),
            AIServiceMode.ServerOnly => await AnalyzeServerOnlyAsync(text, language),
            AIServiceMode.Adaptive => await AnalyzeAdaptiveAsync(text, language),
            _ => await AnalyzeAdaptiveAsync(text, language) // Default to adaptive
        };
    }

    /// <summary>
    /// Client-Only Mode: Always use browser ML, throw if not available
    /// </summary>
    private async Task<SentimentAnalysisResultDto> AnalyzeClientOnlyAsync(
        string text, 
        string language)
    {
        _logger.LogInformation("[SentimentAnalysis.ClientOnly] Using client-side ML exclusively");

        try
        {
            var clientResult = await _clientService.TryAnalyzeAsync(text, language);
            if (clientResult != null)
            {
                _logger.LogInformation(
                    "[SentimentAnalysis.ClientOnly] ✅ Analysis successful: {Emotion}", 
                    clientResult.PrimaryEmotion);
                return clientResult;
            }

            // Client mode failed - throw exception (no server fallback)
            _logger.LogError("[SentimentAnalysis.ClientOnly] ❌ Client-side analysis failed and no fallback allowed");
            throw new InvalidOperationException(
                "Client-side sentiment analysis failed. Models may not be loaded yet. " +
                "Try again in a few seconds or switch to Adaptive mode in settings.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "[SentimentAnalysis.ClientOnly] ❌ Client-side error");
            throw new InvalidOperationException(
                "Client-side sentiment analysis error. Check browser console for details.", ex);
        }
    }

    /// <summary>
    /// Server-Only Mode: Always use server-side processing
    /// </summary>
    private async Task<SentimentAnalysisResultDto> AnalyzeServerOnlyAsync(
        string text, 
        string language)
    {
        _logger.LogInformation("[SentimentAnalysis.ServerOnly] Using server-side processing exclusively");

        var result = await _serverService.AnalyzeAsync(text, language);

        _logger.LogInformation(
            "[SentimentAnalysis.ServerOnly] ✅ Analysis complete: {Emotion} (source: {Source})", 
            result.PrimaryEmotion, result.AnalysisSource);

        return result;
    }

    /// <summary>
    /// Adaptive Mode: Smart routing with progressive enhancement
    /// 1. Check if client models are ready
    /// 2. If ready → use client (better quality, privacy-first)
    /// 3. If not ready → use server (instant processing)
    /// </summary>
    private async Task<SentimentAnalysisResultDto> AnalyzeAdaptiveAsync(
        string text, 
        string language)
    {
        _logger.LogInformation("[SentimentAnalysis.Adaptive] 🎯 Smart routing enabled");

        // Step 1: Check if client models are ready (non-blocking check)
        var clientReady = await _clientService.AreModelsReadyAsync();

        if (clientReady)
        {
            // Step 2a: Models ready → Use high-quality client-side ML
            _logger.LogInformation(
                "[SentimentAnalysis.Adaptive] ✅ Client models ready - using Transformers.js");

            try
            {
                var clientResult = await _clientService.TryAnalyzeAsync(text, language);
                if (clientResult != null)
                {
                    _logger.LogInformation(
                        "[SentimentAnalysis.Adaptive] ✅ Client-side success: {Emotion}", 
                        clientResult.PrimaryEmotion);
                    return clientResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[SentimentAnalysis.Adaptive] ⚠️ Client-side failed, falling back to server");
            }
        }
        else
        {
            // Step 2b: Models not ready → Use fast server-side
            _logger.LogInformation(
                "[SentimentAnalysis.Adaptive] 🔄 Client models not ready - using server-side (models loading in background)");
        }

        // Step 3: Server-side fallback (always works)
        var serverResult = await _serverService.AnalyzeAsync(text, language);

        _logger.LogInformation(
            "[SentimentAnalysis.Adaptive] ✅ Analysis complete via {Source}: {Emotion}",
            serverResult.AnalysisSource,
            serverResult.PrimaryEmotion);

        return serverResult;
    }
}
