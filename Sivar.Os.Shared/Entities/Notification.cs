using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Entity for user notifications in the social network
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>
    /// ID of the user who should receive this notification
    /// </summary>
    [Required]
    public virtual Guid UserId { get; set; }

    /// <summary>
    /// User who should receive this notification
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Type of notification (Follow, Comment, Reaction, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public virtual string Type { get; set; } = string.Empty;

    /// <summary>
    /// Content/message of the notification
    /// </summary>
    [Required]
    [StringLength(500)]
    public virtual string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether the notification has been read
    /// </summary>
    public virtual bool IsRead { get; set; } = false;

    /// <summary>
    /// ID of the related entity (Post, Comment, Profile, etc.)
    /// </summary>
    public virtual Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// Type of the related entity (for polymorphic relationships)
    /// </summary>
    [StringLength(50)]
    public virtual string? RelatedEntityType { get; set; }

    /// <summary>
    /// ID of the user who triggered this notification (e.g., who followed, commented, etc.)
    /// </summary>
    public virtual Guid? TriggeredByUserId { get; set; }

    /// <summary>
    /// User who triggered this notification
    /// </summary>
    public virtual User? TriggeredByUser { get; set; }

    /// <summary>
    /// Additional metadata as JSON (optional)
    /// </summary>
    [Column(TypeName = "text")]
    public virtual string? Metadata { get; set; }

    /// <summary>
    /// When the notification was read (if applicable)
    /// </summary>
    public virtual DateTime? ReadAt { get; set; }

    /// <summary>
    /// Priority level of the notification
    /// </summary>
    public virtual NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Check if notification is recent (within last 24 hours)
    /// </summary>
    public bool IsRecent => (DateTime.UtcNow - CreatedAt).TotalHours <= 24;
}

/// <summary>
/// Priority levels for notifications
/// </summary>
public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

/// <summary>
/// Common notification types
/// </summary>
public static class NotificationTypes
{
    public const string Follow = "Follow";
    public const string Unfollow = "Unfollow";
    public const string Comment = "Comment";
    public const string Reply = "Reply";
    public const string Reaction = "Reaction";
    public const string PostMention = "PostMention";
    public const string CommentMention = "CommentMention";
    public const string Message = "Message";
    public const string System = "System";
}

/// <summary>
/// Related entity types for notifications
/// </summary>
public static class NotificationEntityTypes
{
    public const string Post = "Post";
    public const string Comment = "Comment";
    public const string Profile = "Profile";
    public const string User = "User";
}