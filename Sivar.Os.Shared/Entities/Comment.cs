using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Comments on posts in the activity stream
/// </summary>
public class Comment : BaseEntity
{
    /// <summary>
    /// The post this comment belongs to
    /// </summary>
    public virtual Guid PostId { get; set; }
    public virtual Post Post { get; set; } = null!;

    /// <summary>
    /// The profile that made this comment
    /// </summary>
    public virtual Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// Parent comment for threaded discussions (null for top-level comments)
    /// </summary>
    public virtual Guid? ParentCommentId { get; set; }
    public virtual Comment? ParentComment { get; set; }

    /// <summary>
    /// Replies to this comment
    /// </summary>
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();

    /// <summary>
    /// The comment content
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public virtual string Content { get; set; } = string.Empty;

    /// <summary>
    /// Language of the comment
    /// </summary>
    [StringLength(5)]
    public virtual string Language { get; set; } = "en";

    /// <summary>
    /// Reactions on this comment
    /// </summary>
    public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

    /// <summary>
    /// Indicates if this comment has been edited
    /// </summary>
    public virtual bool IsEdited { get; set; } = false;

    /// <summary>
    /// Date when the comment was last edited
    /// </summary>
    public virtual DateTime? EditedAt { get; set; }

    // ==================== SENTIMENT ANALYSIS FIELDS ====================

    /// <summary>
    /// Primary emotion detected in the comment (Joy, Sadness, Anger, Fear, Neutral)
    /// </summary>
    [StringLength(20)]
    public virtual string? PrimaryEmotion { get; set; }

    /// <summary>
    /// Confidence score for the primary emotion (0.0 to 1.0)
    /// </summary>
    public virtual decimal? EmotionScore { get; set; }

    /// <summary>
    /// Sentiment polarity score (-1.0 = negative, 0 = neutral, +1.0 = positive)
    /// </summary>
    public virtual decimal? SentimentPolarity { get; set; }

    /// <summary>
    /// Joy emotion score (0.0 to 1.0)
    /// </summary>
    public virtual decimal? JoyScore { get; set; }

    /// <summary>
    /// Sadness emotion score (0.0 to 1.0)
    /// </summary>
    public virtual decimal? SadnessScore { get; set; }

    /// <summary>
    /// Anger emotion score (0.0 to 1.0)
    /// </summary>
    public virtual decimal? AngerScore { get; set; }

    /// <summary>
    /// Fear emotion score (0.0 to 1.0)
    /// </summary>
    public virtual decimal? FearScore { get; set; }

    /// <summary>
    /// Indicates if anger was detected above threshold (for moderation)
    /// </summary>
    public virtual bool HasAnger { get; set; } = false;

    /// <summary>
    /// Indicates if this comment needs manual review (high anger or toxic content)
    /// </summary>
    public virtual bool NeedsReview { get; set; } = false;

    /// <summary>
    /// Timestamp when sentiment analysis was performed
    /// </summary>
    public virtual DateTime? AnalyzedAt { get; set; }
}