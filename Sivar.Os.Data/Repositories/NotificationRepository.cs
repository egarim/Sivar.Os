using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for notification data access
/// </summary>
public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
{
    public NotificationRepository(SivarDbContext context) : base(context)
    {
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false, int page = 1, int pageSize = 20)
    {
        var query = _context.Notifications
            .Where(n => !n.IsDeleted && n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .Include(n => n.TriggeredByUser)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetUserNotificationsByTypeAsync(Guid userId, string notificationType, int page = 1, int pageSize = 20)
    {
        return await _context.Notifications
            .Where(n => !n.IsDeleted && n.UserId == userId && n.Type == notificationType)
            .Include(n => n.TriggeredByUser)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && !n.IsDeleted);

        if (notification == null || notification.IsRead)
        {
            return false;
        }

        notification.MarkAsRead();
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => !n.IsDeleted && n.UserId == userId && !n.IsRead)
            .ToListAsync();

        var count = unreadNotifications.Count;
        var now = DateTime.UtcNow;

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        if (count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return count;
    }

    public async Task<int> MarkTypeAsReadAsync(Guid userId, string notificationType)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => !n.IsDeleted && n.UserId == userId && !n.IsRead && n.Type == notificationType)
            .ToListAsync();

        var count = unreadNotifications.Count;
        var now = DateTime.UtcNow;

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        if (count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return count;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Notifications
            .CountAsync(n => !n.IsDeleted && n.UserId == userId && !n.IsRead);
    }

    public async Task<(int total, int unread, int recent, Dictionary<string, int> typeBreakdown)> GetNotificationSummaryAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .Where(n => !n.IsDeleted && n.UserId == userId)
            .ToListAsync();

        var total = notifications.Count;
        var unread = notifications.Count(n => !n.IsRead);
        var recent = notifications.Count(n => (DateTime.UtcNow - n.CreatedAt).TotalHours <= 24);

        var typeBreakdown = notifications
            .GroupBy(n => n.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        return (total, unread, recent, typeBreakdown);
    }

    public async Task<int> CleanupOldNotificationsAsync(int olderThanDays = 30, bool keepUnread = true)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

        var query = _context.Notifications
            .Where(n => !n.IsDeleted && n.CreatedAt < cutoffDate);

        if (keepUnread)
        {
            query = query.Where(n => n.IsRead);
        }

        var oldNotifications = await query.ToListAsync();
        var count = oldNotifications.Count;

        foreach (var notification in oldNotifications)
        {
            notification.IsDeleted = true;
            notification.DeletedAt = DateTime.UtcNow;
        }

        if (count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return count;
    }

    public async Task<bool> SimilarNotificationExistsAsync(Guid userId, string type, Guid? relatedEntityId, Guid? triggeredByUserId, int withinHours = 1)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-withinHours);

        return await _context.Notifications
            .AnyAsync(n => !n.IsDeleted 
                && n.UserId == userId 
                && n.Type == type 
                && n.RelatedEntityId == relatedEntityId 
                && n.TriggeredByUserId == triggeredByUserId 
                && n.CreatedAt > cutoffTime);
    }

    public async Task<List<Notification>> GetMultiUserNotificationsAsync(List<Guid> userIds, bool unreadOnly = false, int page = 1, int pageSize = 20)
    {
        var query = _context.Notifications
            .Where(n => !n.IsDeleted && userIds.Contains(n.UserId));

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .Include(n => n.User)
            .Include(n => n.TriggeredByUser)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Get recent notifications for real-time updates
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="since">Get notifications newer than this timestamp</param>
    /// <returns>List of recent notifications</returns>
    public async Task<List<Notification>> GetRecentNotificationsAsync(Guid userId, DateTime since)
    {
        return await _context.Notifications
            .Where(n => !n.IsDeleted && n.UserId == userId && n.CreatedAt > since)
            .Include(n => n.TriggeredByUser)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get notifications by priority level
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="priority">Priority level to filter by</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>List of notifications with specified priority</returns>
    public async Task<List<Notification>> GetNotificationsByPriorityAsync(Guid userId, NotificationPriority priority, int page = 1, int pageSize = 20)
    {
        return await _context.Notifications
            .Where(n => !n.IsDeleted && n.UserId == userId && n.Priority == priority)
            .Include(n => n.TriggeredByUser)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}