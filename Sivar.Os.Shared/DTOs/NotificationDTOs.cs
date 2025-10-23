
using Sivar.Os.Shared.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for creating a notification
/// </summary>
public record CreateNotificationDto
{
    /// <summary>
    /// ID of the user who should receive this notification
    /// </summary>
    [Required]
    public Guid UserId { get; init; }

    /// <summary>
    /// Type of notification
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Content/message of the notification
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// ID of the related entity (Post, Comment, Profile, etc.)
    /// </summary>
    public Guid? RelatedEntityId { get; init; }

    /// <summary>
    /// Type of the related entity
    /// </summary>
    [StringLength(50)]
    public string? RelatedEntityType { get; init; }

    /// <summary>
    /// ID of the user who triggered this notification
    /// </summary>
    public Guid? TriggeredByUserId { get; init; }

    /// <summary>
    /// Priority level of the notification
    /// </summary>
    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;

    /// <summary>
    /// Additional metadata as JSON (optional)
    /// </summary>
    public string? Metadata { get; init; }
}

/// <summary>
/// DTO for notification data
/// </summary>
public record NotificationDto
{
    /// <summary>
    /// Notification ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// ID of the user who should receive this notification
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Type of notification
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Content/message of the notification
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Whether the notification has been read
    /// </summary>
    public bool IsRead { get; init; }

    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the notification was read (if applicable)
    /// </summary>
    public DateTime? ReadAt { get; init; }

    /// <summary>
    /// ID of the related entity
    /// </summary>
    public Guid? RelatedEntityId { get; init; }

    /// <summary>
    /// Type of the related entity
    /// </summary>
    public string? RelatedEntityType { get; init; }

    /// <summary>
    /// ID of the user who triggered this notification
    /// </summary>
    public Guid? TriggeredByUserId { get; init; }

    /// <summary>
    /// Basic info about the user who triggered the notification
    /// </summary>
    public NotificationUserDto? TriggeredByUser { get; init; }

    /// <summary>
    /// Priority level of the notification
    /// </summary>
    public NotificationPriority Priority { get; init; }

    /// <summary>
    /// Additional metadata as JSON (optional)
    /// </summary>
    public string? Metadata { get; init; }

    /// <summary>
    /// Check if notification is recent (within last 24 hours)
    /// </summary>
    public bool IsRecent => (DateTime.UtcNow - CreatedAt).TotalHours <= 24;
}

/// <summary>
/// DTO for user information in notifications
/// </summary>
public record NotificationUserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// User's display name (first + last name)
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// User's email
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's active profile info (optional)
    /// </summary>
    public NotificationProfileDto? ActiveProfile { get; init; }
}

/// <summary>
/// DTO for profile information in notifications
/// </summary>
public record NotificationProfileDto
{
    /// <summary>
    /// Profile ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Profile display name
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Profile type name
    /// </summary>
    public string ProfileType { get; init; } = string.Empty;

    /// <summary>
    /// Profile avatar URL (optional)
    /// </summary>
    public string? AvatarUrl { get; init; }
}

/// <summary>
/// DTO for updating notification read status
/// </summary>
public record UpdateNotificationDto
{
    /// <summary>
    /// Whether to mark as read or unread
    /// </summary>
    public bool IsRead { get; init; }
}

/// <summary>
/// Query parameters for fetching notifications
/// </summary>
public record NotificationQueryDto
{
    /// <summary>
    /// Only return unread notifications
    /// </summary>
    public bool? UnreadOnly { get; init; }

    /// <summary>
    /// Filter by notification type
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Page number for pagination
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Only return notifications newer than this date
    /// </summary>
    public DateTime? Since { get; init; }

    /// <summary>
    /// Filter by priority level
    /// </summary>
    public NotificationPriority? Priority { get; init; }
}

/// <summary>
/// DTO for notification summary/stats
/// </summary>
public record NotificationSummaryDto
{
    /// <summary>
    /// Total number of notifications
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Number of unread notifications
    /// </summary>
    public int UnreadCount { get; init; }

    /// <summary>
    /// Number of recent notifications (last 24 hours)
    /// </summary>
    public int RecentCount { get; init; }

    /// <summary>
    /// Breakdown by notification type
    /// </summary>
    public Dictionary<string, int> TypeBreakdown { get; init; } = new();

    /// <summary>
    /// Most recent notification timestamp
    /// </summary>
    public DateTime? LastNotificationAt { get; init; }
}

/// <summary>
/// DTO for unread notification count response
/// </summary>
public record UnreadCountDto
{
    /// <summary>
    /// Number of unread notifications
    /// </summary>
    public int Count { get; init; }
}

/// <summary>
/// DTO for mark all as read response
/// </summary>
public record MarkAllReadResponseDto
{
    /// <summary>
    /// Number of notifications marked as read
    /// </summary>
    public int MarkedAsReadCount { get; init; }
}