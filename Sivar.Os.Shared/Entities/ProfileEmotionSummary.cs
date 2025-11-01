using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Aggregated emotion statistics for a profile over a time period
/// Used for backend analytics and trend analysis
/// </summary>
public class ProfileEmotionSummary : BaseEntity
{
    /// <summary>
    /// The profile this summary belongs to
    /// </summary>
    public virtual Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// Time window type (hourly, daily, weekly, monthly)
    /// </summary>
    [Required]
    [StringLength(20)]
    public virtual string TimeWindow { get; set; } = "daily";

    /// <summary>
    /// Start date of the aggregation period
    /// </summary>
    public virtual DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the aggregation period
    /// </summary>
    public virtual DateTime EndDate { get; set; }

    /// <summary>
    /// Total number of posts analyzed in this period
    /// </summary>
    public virtual int TotalPosts { get; set; }

    /// <summary>
    /// Total number of comments analyzed in this period
    /// </summary>
    public virtual int TotalComments { get; set; }

    /// <summary>
    /// Average joy score across all content (0.0 to 1.0)
    /// </summary>
    public virtual decimal AvgJoyScore { get; set; }

    /// <summary>
    /// Average sadness score across all content (0.0 to 1.0)
    /// </summary>
    public virtual decimal AvgSadnessScore { get; set; }

    /// <summary>
    /// Average anger score across all content (0.0 to 1.0)
    /// </summary>
    public virtual decimal AvgAngerScore { get; set; }

    /// <summary>
    /// Average fear score across all content (0.0 to 1.0)
    /// </summary>
    public virtual decimal AvgFearScore { get; set; }

    /// <summary>
    /// Dominant emotion for this period (most frequent primary emotion)
    /// </summary>
    [StringLength(20)]
    public virtual string? DominantEmotion { get; set; }

    /// <summary>
    /// Number of posts/comments with Joy as primary emotion
    /// </summary>
    public virtual int JoyCount { get; set; }

    /// <summary>
    /// Number of posts/comments with Sadness as primary emotion
    /// </summary>
    public virtual int SadnessCount { get; set; }

    /// <summary>
    /// Number of posts/comments with Anger as primary emotion
    /// </summary>
    public virtual int AngerCount { get; set; }

    /// <summary>
    /// Number of posts/comments with Fear as primary emotion
    /// </summary>
    public virtual int FearCount { get; set; }

    /// <summary>
    /// Number of posts/comments with Neutral emotion
    /// </summary>
    public virtual int NeutralCount { get; set; }

    /// <summary>
    /// Number of posts/comments flagged for review in this period
    /// </summary>
    public virtual int FlaggedCount { get; set; }

    /// <summary>
    /// Overall sentiment polarity for the period (-1.0 to +1.0)
    /// </summary>
    public virtual decimal OverallPolarity { get; set; }

    /// <summary>
    /// Indicates if this summary was calculated by TimescaleDB continuous aggregate
    /// </summary>
    public virtual bool IsAutomated { get; set; } = true;
}
