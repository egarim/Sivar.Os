using Sivar.Os.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service interface for notification management
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Create a notification for a user
    /// </summary>
    /// <param name="createNotificationDto">Notification creation data</param>
    /// <returns>Created notification DTO</returns>
    Task<NotificationDto?> CreateNotificationAsync(CreateNotificationDto createNotificationDto);

    /// <summary>
    /// Create a follow notification
    /// </summary>
    /// <param name="followedProfileId">ID of the profile being followed</param>
    /// <param name="followerUserId">ID of the user doing the following</param>
    /// <returns>Created notification DTO</returns>
    Task<NotificationDto?> CreateFollowNotificationAsync(Guid followedProfileId, Guid followerUserId);

    /// <summary>
    /// Create a comment notification
    /// </summary>
    /// <param name="postId">ID of the post being commented on</param>
    /// <param name="commenterId">ID of the user making the comment</param>
    /// <param name="commentContent">Content of the comment (truncated for notification)</param>
    /// <returns>Created notification DTO</returns>
    Task<NotificationDto?> CreateCommentNotificationAsync(Guid postId, Guid commenterId, string commentContent);

    /// <summary>
    /// Create a reaction notification
    /// </summary>
    /// <param name="postId">ID of the post being reacted to</param>
    /// <param name="reactorUserId">ID of the user adding the reaction</param>
    /// <param name="reactionType">Type of reaction (like, love, etc.)</param>
    /// <returns>Created notification DTO</returns>
    Task<NotificationDto?> CreateReactionNotificationAsync(Guid postId, Guid reactorUserId, string reactionType);

    /// <summary>
    /// Create a reply notification (comment on comment)
    /// </summary>
    /// <param name="originalCommentId">ID of the comment being replied to</param>
    /// <param name="replierUserId">ID of the user making the reply</param>
    /// <param name="replyContent">Content of the reply</param>
    /// <returns>Created notification DTO</returns>
    Task<NotificationDto?> CreateReplyNotificationAsync(Guid originalCommentId, Guid replierUserId, string replyContent);

    /// <summary>
    /// Create a message notification
    /// </summary>
    /// <param name="recipientUserId">ID of the user receiving the message</param>
    /// <param name="senderUserId">ID of the user sending the message</param>
    /// <param name="messagePreview">Preview of the message content</param>
    /// <returns>Created notification DTO</returns>
    Task<NotificationDto?> CreateMessageNotificationAsync(Guid recipientUserId, Guid senderUserId, string messagePreview);

    /// <summary>
    /// Get notifications for a user based on their Keycloak ID
    /// </summary>
    /// <param name="keycloakId">Keycloak ID of the user</param>
    /// <param name="queryParams">Query parameters for filtering</param>
    /// <returns>List of notification DTOs</returns>
    Task<List<NotificationDto>> GetUserNotificationsAsync(string keycloakId, NotificationQueryDto? queryParams = null);

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="keycloakId">Keycloak ID of the user (for security)</param>
    /// <returns>True if marked as read, false if not found or not owned by user</returns>
    Task<bool> MarkAsReadAsync(Guid notificationId, string keycloakId);

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    /// <param name="keycloakId">Keycloak ID of the user</param>
    /// <returns>Number of notifications marked as read</returns>
    Task<int> MarkAllAsReadAsync(string keycloakId);

    /// <summary>
    /// Mark all notifications of a specific type as read
    /// </summary>
    /// <param name="keycloakId">Keycloak ID of the user</param>
    /// <param name="notificationType">Type of notifications to mark as read</param>
    /// <returns>Number of notifications marked as read</returns>
    Task<int> MarkTypeAsReadAsync(string keycloakId, string notificationType);

    /// <summary>
    /// Get notification summary for a user
    /// </summary>
    /// <param name="keycloakId">Keycloak ID of the user</param>
    /// <returns>Notification summary DTO</returns>
    Task<NotificationSummaryDto> GetNotificationSummaryAsync(string keycloakId);

    /// <summary>
    /// Delete a notification (soft delete)
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="keycloakId">Keycloak ID of the user (for security)</param>
    /// <returns>True if deleted, false if not found or not owned by user</returns>
    Task<bool> DeleteNotificationAsync(Guid notificationId, string keycloakId);

    /// <summary>
    /// Get unread notification count for a user
    /// </summary>
    /// <param name="keycloakId">Keycloak ID of the user</param>
    /// <returns>Count of unread notifications</returns>
    Task<int> GetUnreadCountAsync(string keycloakId);

    /// <summary>
    /// Check if user has notifications enabled for specific types
    /// </summary>
    /// <param name="keycloakId">Keycloak ID of the user</param>
    /// <param name="notificationType">Type of notification to check</param>
    /// <returns>True if notifications are enabled for this type</returns>
    Task<bool> IsNotificationTypeEnabledAsync(string keycloakId, string notificationType);

    /// <summary>
    /// Batch create notifications (for system events)
    /// </summary>
    /// <param name="notifications">List of notifications to create</param>
    /// <returns>List of created notification DTOs</returns>
    Task<List<NotificationDto>> CreateBatchNotificationsAsync(List<CreateNotificationDto> notifications);

    /// <summary>
    /// Admin function: Clean up old notifications
    /// </summary>
    /// <param name="olderThanDays">Delete notifications older than this many days</param>
    /// <param name="keepUnread">If true, don't delete unread notifications</param>
    /// <returns>Number of notifications cleaned up</returns>
    Task<int> CleanupOldNotificationsAsync(int olderThanDays = 30, bool keepUnread = true);
}