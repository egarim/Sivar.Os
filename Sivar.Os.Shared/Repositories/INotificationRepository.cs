using Sivar.Os.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for notification data access
/// </summary>
public interface INotificationRepository : IBaseRepository<Notification>
{
    /// <summary>
    /// Get notifications for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="unreadOnly">If true, only return unread notifications</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>List of notifications</returns>
    Task<List<Notification>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get notifications for a user filtered by type
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="notificationType">Type of notification to filter by</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>List of notifications of specified type</returns>
    Task<List<Notification>> GetUserNotificationsByTypeAsync(Guid userId, string notificationType, int page = 1, int pageSize = 20);

    /// <summary>
    /// Mark a specific notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User ID (for security verification)</param>
    /// <returns>True if marked as read, false if not found or not owned by user</returns>
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);

    /// <summary>
    /// Mark all notifications as read for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Number of notifications marked as read</returns>
    Task<int> MarkAllAsReadAsync(Guid userId);

    /// <summary>
    /// Mark notifications of a specific type as read for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="notificationType">Type of notifications to mark as read</param>
    /// <returns>Number of notifications marked as read</returns>
    Task<int> MarkTypeAsReadAsync(Guid userId, string notificationType);

    /// <summary>
    /// Get count of unread notifications for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Count of unread notifications</returns>
    Task<int> GetUnreadCountAsync(Guid userId);

    /// <summary>
    /// Get notification summary/stats for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Notification summary</returns>
    Task<(int total, int unread, int recent, Dictionary<string, int> typeBreakdown)> GetNotificationSummaryAsync(Guid userId);

    /// <summary>
    /// Delete old read notifications (cleanup)
    /// </summary>
    /// <param name="olderThanDays">Delete notifications older than this many days</param>
    /// <param name="keepUnread">If true, don't delete unread notifications</param>
    /// <returns>Number of notifications deleted</returns>
    Task<int> CleanupOldNotificationsAsync(int olderThanDays = 30, bool keepUnread = true);

    /// <summary>
    /// Check if a similar notification already exists (to prevent duplicates)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="type">Notification type</param>
    /// <param name="relatedEntityId">Related entity ID</param>
    /// <param name="triggeredByUserId">User who triggered the notification</param>
    /// <param name="withinHours">Check for duplicates within this many hours (default 1)</param>
    /// <returns>True if similar notification exists</returns>
    Task<bool> SimilarNotificationExistsAsync(Guid userId, string type, Guid? relatedEntityId, Guid? triggeredByUserId, int withinHours = 1);

    /// <summary>
    /// Get notifications for multiple users (admin function)
    /// </summary>
    /// <param name="userIds">List of user IDs</param>
    /// <param name="unreadOnly">If true, only return unread notifications</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>List of notifications</returns>
    Task<List<Notification>> GetMultiUserNotificationsAsync(List<Guid> userIds, bool unreadOnly = false, int page = 1, int pageSize = 20);
}