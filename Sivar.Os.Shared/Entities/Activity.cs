using System.ComponentModel.DataAnnotations;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents an activity in the activity stream following Activity Streams 2.0 pattern
/// Activities capture "Actor performed Action on Object/Target"
/// Example: "John Doe created a Post", "Jane Smith liked a Comment"
/// </summary>
public class Activity : BaseEntity
{
    /// <summary>
    /// The actor (profile) who performed the activity
    /// </summary>
    public virtual Guid ActorId { get; set; }
    public virtual Profile Actor { get; set; } = null!;

    /// <summary>
    /// The type of action performed
    /// </summary>
    public virtual ActivityVerb Verb { get; set; }

    /// <summary>
    /// Type of the target object (Post, Comment, Profile, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public virtual string ObjectType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the primary target object
    /// </summary>
    public virtual Guid ObjectId { get; set; }

    /// <summary>
    /// Optional secondary target (e.g., shared a post TO a group, added comment TO a post)
    /// </summary>
    [StringLength(50)]
    public virtual string? TargetType { get; set; }

    /// <summary>
    /// Optional ID of the secondary target
    /// </summary>
    public virtual Guid? TargetId { get; set; }

    /// <summary>
    /// Summary/description of the activity for display
    /// Auto-generated but can be customized
    /// Example: "John created a new post", "Jane liked your comment"
    /// </summary>
    [StringLength(500)]
    public virtual string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata about the activity stored as JSON
    /// Can include preview text, thumbnails, location, etc.
    /// </summary>
    public virtual string Metadata { get; set; } = "{}";

    /// <summary>
    /// Privacy/visibility level of this activity
    /// Determines who can see this activity in their feed
    /// </summary>
    public virtual VisibilityLevel Visibility { get; set; } = VisibilityLevel.Public;

    /// <summary>
    /// Indicates if this activity should appear in activity feeds
    /// Can be set to false for internal/system activities
    /// </summary>
    public virtual bool IsPublished { get; set; } = true;

    /// <summary>
    /// When the activity was published (may differ from CreatedAt for scheduled activities)
    /// </summary>
    public virtual DateTime PublishedAt { get; set; }

    /// <summary>
    /// Language/locale of the activity
    /// </summary>
    [StringLength(5)]
    public virtual string Language { get; set; } = "en";

    /// <summary>
    /// Priority/importance score for feed ranking algorithms
    /// Higher scores appear more prominently in feeds
    /// </summary>
    public virtual int Priority { get; set; } = 0;

    /// <summary>
    /// Number of times this activity has been viewed
    /// </summary>
    public virtual int ViewCount { get; set; } = 0;

    /// <summary>
    /// Engagement score (calculated from likes, comments, shares, etc.)
    /// Used for feed ranking
    /// </summary>
    public virtual int EngagementScore { get; set; } = 0;

    /// <summary>
    /// Generates a human-readable summary of the activity
    /// </summary>
    public string GenerateSummary(string actorDisplayName)
    {
        return Verb switch
        {
            ActivityVerb.Create => $"{actorDisplayName} created a {ObjectType.ToLower()}",
            ActivityVerb.Update => $"{actorDisplayName} updated a {ObjectType.ToLower()}",
            ActivityVerb.Delete => $"{actorDisplayName} deleted a {ObjectType.ToLower()}",
            ActivityVerb.Like => $"{actorDisplayName} liked a {ObjectType.ToLower()}",
            ActivityVerb.Comment => $"{actorDisplayName} commented on a {ObjectType.ToLower()}",
            ActivityVerb.Share => $"{actorDisplayName} shared a {ObjectType.ToLower()}",
            ActivityVerb.Follow => $"{actorDisplayName} followed a {ObjectType.ToLower()}",
            ActivityVerb.Unfollow => $"{actorDisplayName} unfollowed a {ObjectType.ToLower()}",
            ActivityVerb.Join => $"{actorDisplayName} joined a {ObjectType.ToLower()}",
            ActivityVerb.Leave => $"{actorDisplayName} left a {ObjectType.ToLower()}",
            ActivityVerb.Add => $"{actorDisplayName} added a {ObjectType.ToLower()}" + (TargetType != null ? $" to a {TargetType.ToLower()}" : ""),
            ActivityVerb.Remove => $"{actorDisplayName} removed a {ObjectType.ToLower()}" + (TargetType != null ? $" from a {TargetType.ToLower()}" : ""),
            ActivityVerb.Mention => $"{actorDisplayName} mentioned a {ObjectType.ToLower()}",
            ActivityVerb.Tag => $"{actorDisplayName} tagged a {ObjectType.ToLower()}",
            ActivityVerb.View => $"{actorDisplayName} viewed a {ObjectType.ToLower()}",
            ActivityVerb.Accept => $"{actorDisplayName} accepted a {ObjectType.ToLower()}",
            ActivityVerb.Reject => $"{actorDisplayName} rejected a {ObjectType.ToLower()}",
            _ => $"{actorDisplayName} performed an action on a {ObjectType.ToLower()}"
        };
    }

    /// <summary>
    /// Increments the view count
    /// </summary>
    public void IncrementViewCount()
    {
        ViewCount++;
    }

    /// <summary>
    /// Updates the engagement score based on interactions
    /// </summary>
    public void CalculateEngagementScore(int likes, int comments, int shares, int views)
    {
        // Weight different interactions differently
        EngagementScore = (likes * 3) + (comments * 5) + (shares * 10) + (views / 10);
    }
}
