using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Services;

/// <summary>
/// Server-side sentiment analysis fallback service
/// Provides basic sentiment analysis when client-side analysis is unavailable
/// Future enhancement: Integrate with Azure Cognitive Services or ML.NET
/// </summary>
public class ServerSentimentAnalysisService : IServerSentimentAnalysisService
{
    private readonly ILogger<ServerSentimentAnalysisService> _logger;

    // Simple keyword-based sentiment detection (placeholder for real ML model)
    private static readonly Dictionary<string, string> EmotionKeywords = new()
    {
        // Joy keywords
        { "happy", "Joy" },
        { "joy", "Joy" },
        { "love", "Joy" },
        { "great", "Joy" },
        { "amazing", "Joy" },
        { "wonderful", "Joy" },
        { "excellent", "Joy" },
        { "excited", "Joy" },
        { "feliz", "Joy" }, // Spanish: happy
        { "alegre", "Joy" }, // Spanish: joyful
        { "amor", "Joy" }, // Spanish: love
        
        // Sadness keywords
        { "sad", "Sadness" },
        { "unhappy", "Sadness" },
        { "depressed", "Sadness" },
        { "disappointed", "Sadness" },
        { "grief", "Sadness" },
        { "triste", "Sadness" }, // Spanish: sad
        { "decepcionado", "Sadness" }, // Spanish: disappointed
        
        // Anger keywords
        { "angry", "Anger" },
        { "mad", "Anger" },
        { "furious", "Anger" },
        { "hate", "Anger" },
        { "annoyed", "Anger" },
        { "enojado", "Anger" }, // Spanish: angry
        { "furioso", "Anger" }, // Spanish: furious
        { "odio", "Anger" }, // Spanish: hate
        
        // Fear keywords
        { "afraid", "Fear" },
        { "scared", "Fear" },
        { "worried", "Fear" },
        { "anxious", "Fear" },
        { "nervous", "Fear" },
        { "miedo", "Fear" }, // Spanish: fear
        { "asustado", "Fear" }, // Spanish: scared
        { "preocupado", "Fear" } // Spanish: worried
    };

    public ServerSentimentAnalysisService(ILogger<ServerSentimentAnalysisService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SentimentAnalysisResultDto> AnalyzeAsync(string text, string language)
    {
        _logger.LogInformation("[ServerSentiment] Performing server-side fallback analysis ({Language}, {Length} chars)", 
            language, text.Length);

        try
        {
            // Basic keyword-based analysis (placeholder)
            var emotionCounts = new Dictionary<string, int>
            {
                { "Joy", 0 },
                { "Sadness", 0 },
                { "Anger", 0 },
                { "Fear", 0 },
                { "Neutral", 0 }
            };

            var lowerText = text.ToLowerInvariant();
            var words = lowerText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Count emotion keywords
            foreach (var word in words)
            {
                foreach (var (keyword, emotion) in EmotionKeywords)
                {
                    if (word.Contains(keyword))
                    {
                        emotionCounts[emotion]++;
                    }
                }
            }

            // If no emotions detected, mark as neutral
            var totalEmotions = emotionCounts.Values.Sum();
            if (totalEmotions == 0)
            {
                emotionCounts["Neutral"] = 1;
                totalEmotions = 1;
            }

            // Calculate emotion scores
            var joyScore = (decimal)emotionCounts["Joy"] / totalEmotions;
            var sadnessScore = (decimal)emotionCounts["Sadness"] / totalEmotions;
            var angerScore = (decimal)emotionCounts["Anger"] / totalEmotions;
            var fearScore = (decimal)emotionCounts["Fear"] / totalEmotions;
            var neutralScore = (decimal)emotionCounts["Neutral"] / totalEmotions;

            // Determine primary emotion
            var primaryEmotion = emotionCounts
                .OrderByDescending(x => x.Value)
                .First()
                .Key;

            var emotionScore = (decimal)emotionCounts[primaryEmotion] / totalEmotions;

            // Calculate polarity
            var polarity = joyScore - sadnessScore - angerScore - fearScore;

            // Check moderation flags
            var hasAnger = angerScore >= 0.6m;
            var needsReview = angerScore >= 0.75m;

            var result = new SentimentAnalysisResultDto
            {
                PrimaryEmotion = primaryEmotion,
                EmotionScore = emotionScore,
                SentimentPolarity = polarity,
                EmotionScores = new EmotionScoresDto
                {
                    Joy = joyScore,
                    Sadness = sadnessScore,
                    Anger = angerScore,
                    Fear = fearScore,
                    Neutral = neutralScore
                },
                HasAnger = hasAnger,
                NeedsReview = needsReview,
                Language = language,
                AnalysisSource = "server",
                AnalyzedAt = DateTime.UtcNow
            };

            _logger.LogInformation("[ServerSentiment] ✅ Fallback analysis complete: {Emotion} (score: {Score:F2})", 
                result.PrimaryEmotion, result.EmotionScore);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ServerSentiment] ❌ Error during server-side analysis");
            
            // Return neutral result on error
            return new SentimentAnalysisResultDto
            {
                PrimaryEmotion = "Neutral",
                EmotionScore = 0.5m,
                SentimentPolarity = 0m,
                EmotionScores = new EmotionScoresDto
                {
                    Joy = 0.2m,
                    Sadness = 0.2m,
                    Anger = 0.2m,
                    Fear = 0.2m,
                    Neutral = 0.2m
                },
                HasAnger = false,
                NeedsReview = false,
                Language = language,
                AnalysisSource = "server-error",
                AnalyzedAt = DateTime.UtcNow
            };
        }
    }
}
