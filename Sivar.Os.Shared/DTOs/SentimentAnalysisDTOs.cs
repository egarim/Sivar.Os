namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// Result of sentiment analysis for text content
/// </summary>
public record SentimentAnalysisResultDto
{
    /// <summary>
    /// Primary emotion detected (Joy, Sadness, Anger, Fear, Neutral)
    /// </summary>
    public string PrimaryEmotion { get; init; } = "Neutral";

    /// <summary>
    /// Confidence score for primary emotion (0.0 to 1.0)
    /// </summary>
    public decimal EmotionScore { get; init; }

    /// <summary>
    /// Sentiment polarity (-1.0 = negative, 0 = neutral, +1.0 = positive)
    /// </summary>
    public decimal SentimentPolarity { get; init; }

    /// <summary>
    /// Detailed emotion scores
    /// </summary>
    public EmotionScoresDto EmotionScores { get; init; } = new();

    /// <summary>
    /// Indicates if content has high anger level (>0.6)
    /// </summary>
    public bool HasAnger { get; init; }

    /// <summary>
    /// Indicates if content needs manual review (high toxicity/anger)
    /// </summary>
    public bool NeedsReview { get; init; }

    /// <summary>
    /// Language detected/used for analysis
    /// </summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Source of analysis (client or server)
    /// </summary>
    public string AnalysisSource { get; init; } = "client";

    /// <summary>
    /// Timestamp when analysis was performed
    /// </summary>
    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Detailed breakdown of emotion scores
/// </summary>
public record EmotionScoresDto
{
    /// <summary>
    /// Joy emotion score (0.0 to 1.0)
    /// </summary>
    public decimal Joy { get; init; }

    /// <summary>
    /// Sadness emotion score (0.0 to 1.0)
    /// </summary>
    public decimal Sadness { get; init; }

    /// <summary>
    /// Anger emotion score (0.0 to 1.0)
    /// </summary>
    public decimal Anger { get; init; }

    /// <summary>
    /// Fear emotion score (0.0 to 1.0)
    /// </summary>
    public decimal Fear { get; init; }

    /// <summary>
    /// Neutral/informational score (0.0 to 1.0)
    /// </summary>
    public decimal Neutral { get; init; }
}

/// <summary>
/// Profile emotion summary for analytics
/// </summary>
public record ProfileEmotionSummaryDto
{
    /// <summary>
    /// Summary ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Profile ID
    /// </summary>
    public Guid ProfileId { get; init; }

    /// <summary>
    /// Time window (hourly, daily, weekly, monthly)
    /// </summary>
    public string TimeWindow { get; init; } = "daily";

    /// <summary>
    /// Start date of aggregation
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// End date of aggregation
    /// </summary>
    public DateTime EndDate { get; init; }

    /// <summary>
    /// Total posts in period
    /// </summary>
    public int TotalPosts { get; init; }

    /// <summary>
    /// Total comments in period
    /// </summary>
    public int TotalComments { get; init; }

    /// <summary>
    /// Average emotion scores
    /// </summary>
    public EmotionScoresDto AverageEmotions { get; init; } = new();

    /// <summary>
    /// Dominant emotion for the period
    /// </summary>
    public string? DominantEmotion { get; init; }

    /// <summary>
    /// Emotion count breakdown
    /// </summary>
    public EmotionCountsDto EmotionCounts { get; init; } = new();

    /// <summary>
    /// Overall sentiment polarity
    /// </summary>
    public decimal OverallPolarity { get; init; }

    /// <summary>
    /// Number of flagged items
    /// </summary>
    public int FlaggedCount { get; init; }
}

/// <summary>
/// Count of items by emotion category
/// </summary>
public record EmotionCountsDto
{
    /// <summary>
    /// Joy count
    /// </summary>
    public int Joy { get; init; }

    /// <summary>
    /// Sadness count
    /// </summary>
    public int Sadness { get; init; }

    /// <summary>
    /// Anger count
    /// </summary>
    public int Anger { get; init; }

    /// <summary>
    /// Fear count
    /// </summary>
    public int Fear { get; init; }

    /// <summary>
    /// Neutral count
    /// </summary>
    public int Neutral { get; init; }
}

/// <summary>
/// Community-wide sentiment statistics (hourly aggregation)
/// </summary>
public record CommunitySentimentHourlyDto
{
    /// <summary>
    /// Hour timestamp (start of hour)
    /// </summary>
    public DateTime Hour { get; init; }

    /// <summary>
    /// Total posts in this hour
    /// </summary>
    public int TotalPosts { get; init; }

    /// <summary>
    /// Average emotion scores across all posts
    /// </summary>
    public EmotionScoresDto AverageEmotions { get; init; } = new();

    /// <summary>
    /// Dominant emotion for the hour
    /// </summary>
    public string? DominantEmotion { get; init; }

    /// <summary>
    /// Number of flagged posts in this hour
    /// </summary>
    public int FlaggedPosts { get; init; }

    /// <summary>
    /// Overall polarity for the hour
    /// </summary>
    public decimal OverallPolarity { get; init; }
}

/// <summary>
/// Daily moderation metrics
/// </summary>
public record ModerationMetricsDailyDto
{
    /// <summary>
    /// Day timestamp
    /// </summary>
    public DateTime Day { get; init; }

    /// <summary>
    /// Total posts analyzed
    /// </summary>
    public int TotalPosts { get; init; }

    /// <summary>
    /// Posts flagged for review
    /// </summary>
    public int FlaggedPosts { get; init; }

    /// <summary>
    /// Posts with detected anger
    /// </summary>
    public int PostsWithAnger { get; init; }

    /// <summary>
    /// Average anger score
    /// </summary>
    public decimal AverageAngerScore { get; init; }

    /// <summary>
    /// Posts with high anger (>0.7)
    /// </summary>
    public int HighAngerCount { get; init; }

    /// <summary>
    /// Flag rate (flagged / total)
    /// </summary>
    public decimal FlagRate { get; init; }
}

/// <summary>
/// Per-profile daily sentiment trend
/// </summary>
public record ProfileSentimentDailyDto
{
    /// <summary>
    /// Day timestamp
    /// </summary>
    public DateTime Day { get; init; }

    /// <summary>
    /// Profile ID
    /// </summary>
    public Guid ProfileId { get; init; }

    /// <summary>
    /// Total posts for this profile on this day
    /// </summary>
    public int PostCount { get; init; }

    /// <summary>
    /// Average emotions for the day
    /// </summary>
    public EmotionScoresDto AverageEmotions { get; init; } = new();

    /// <summary>
    /// Dominant emotion for the day
    /// </summary>
    public string? DominantEmotion { get; init; }

    /// <summary>
    /// Emotion counts
    /// </summary>
    public EmotionCountsDto EmotionCounts { get; init; } = new();

    /// <summary>
    /// Overall polarity for the day
    /// </summary>
    public decimal OverallPolarity { get; init; }

    /// <summary>
    /// Flagged post count
    /// </summary>
    public int FlaggedCount { get; init; }
}
