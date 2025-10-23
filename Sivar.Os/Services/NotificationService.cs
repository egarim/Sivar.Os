using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Sivar.Os.Hubs;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sivar.Os.Services;

/// <summary>
/// Service for managing user notifications
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IPostRepository _postRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly ILogger<NotificationService> _logger;
    private readonly IHubContext<NotificationHub>? _hubContext;

    public NotificationService(
        INotificationRepository notificationRepository,
        IUserRepository userRepository,
        IProfileRepository profileRepository,
        IPostRepository postRepository,
        ICommentRepository commentRepository,
        ILogger<NotificationService> logger,
        IHubContext<NotificationHub>? hubContext = null)
    {
        _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hubContext = hubContext; // Optional - for real-time notifications
    }

    public async Task<NotificationDto?> CreateNotificationAsync(CreateNotificationDto createNotificationDto)
    {
        try
        {
            // Check if similar notification already exists to prevent spam
            if (await _notificationRepository.SimilarNotificationExistsAsync(
                createNotificationDto.UserId,
                createNotificationDto.Type,
                createNotificationDto.RelatedEntityId,
                createNotificationDto.TriggeredByUserId))
            {
                _logger.LogDebug("Similar notification already exists, skipping creation");
                return null;
            }

            var notification = new Notification
            {
                UserId = createNotificationDto.UserId,
                Type = createNotificationDto.Type,
                Content = createNotificationDto.Content,
                RelatedEntityId = createNotificationDto.RelatedEntityId,
                RelatedEntityType = createNotificationDto.RelatedEntityType,
                TriggeredByUserId = createNotificationDto.TriggeredByUserId,
                Priority = createNotificationDto.Priority,
                Metadata = createNotificationDto.Metadata,
                CreatedAt = DateTime.UtcNow
            };

            var createdNotification = await _notificationRepository.AddAsync(notification);
            
            _logger.LogInformation("Created notification {NotificationId} for user {UserId} of type {Type}",
                createdNotification.Id, createNotificationDto.UserId, createNotificationDto.Type);

            var notificationDto = await MapToNotificationDtoAsync(createdNotification);
            
            // Send real-time notification via SignalR if available
            if (_hubContext != null && notificationDto != null)
            {
                await SendRealTimeNotificationAsync(createNotificationDto.UserId, notificationDto);
            }

            return notificationDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification for user {UserId}", createNotificationDto.UserId);
            throw;
        }
    }

    public async Task<NotificationDto?> CreateFollowNotificationAsync(Guid followedProfileId, Guid followerUserId)
    {
        try
        {
            // Get the followed profile and its owner
            var followedProfile = await _profileRepository.GetByIdAsync(followedProfileId);
            if (followedProfile == null)
            {
                _logger.LogWarning("Profile {ProfileId} not found for follow notification", followedProfileId);
                return null;
            }

            // Get the follower user
            var followerUser = await _userRepository.GetByIdAsync(followerUserId);
            if (followerUser == null)
            {
                _logger.LogWarning("Follower user {UserId} not found", followerUserId);
                return null;
            }

            // Don't notify if user follows their own profile
            if (followedProfile.UserId == followerUserId)
            {
                return null;
            }

            var content = $"{followerUser.FirstName} {followerUser.LastName} started following your profile '{followedProfile.DisplayName}'";

            var createNotificationDto = new CreateNotificationDto
            {
                UserId = followedProfile.UserId,
                Type = NotificationTypes.Follow,
                Content = content,
                RelatedEntityId = followedProfileId,
                RelatedEntityType = NotificationEntityTypes.Profile,
                TriggeredByUserId = followerUserId,
                Priority = NotificationPriority.Normal
            };

            return await CreateNotificationAsync(createNotificationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating follow notification for profile {ProfileId} by user {UserId}",
                followedProfileId, followerUserId);
            throw;
        }
    }

    public async Task<NotificationDto?> CreateCommentNotificationAsync(Guid postId, Guid commenterId, string commentContent)
    {
        try
        {
            // Get the post and its author
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found for comment notification", postId);
                return null;
            }

            // Get the commenter user
            var commenterUser = await _userRepository.GetByIdAsync(commenterId);
            if (commenterUser == null)
            {
                _logger.LogWarning("Commenter user {UserId} not found", commenterId);
                return null;
            }

            // Get the post author via profile
            var postProfile = await _profileRepository.GetByIdAsync(post.ProfileId);
            if (postProfile == null)
            {
                _logger.LogWarning("Post profile {ProfileId} not found", post.ProfileId);
                return null;
            }

            // Don't notify if user comments on their own post
            if (postProfile.UserId == commenterId)
            {
                return null;
            }

            var truncatedComment = commentContent.Length > 100 
                ? $"{commentContent[..100]}..." 
                : commentContent;

            var content = $"{commenterUser.FirstName} {commenterUser.LastName} commented on your post: \"{truncatedComment}\"";

            var createNotificationDto = new CreateNotificationDto
            {
                UserId = postProfile.UserId,
                Type = NotificationTypes.Comment,
                Content = content,
                RelatedEntityId = postId,
                RelatedEntityType = NotificationEntityTypes.Post,
                TriggeredByUserId = commenterId,
                Priority = NotificationPriority.Normal
            };

            return await CreateNotificationAsync(createNotificationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment notification for post {PostId} by user {UserId}",
                postId, commenterId);
            throw;
        }
    }

    public async Task<NotificationDto?> CreateReactionNotificationAsync(Guid postId, Guid reactorUserId, string reactionType)
    {
        try
        {
            // Get the post and its author
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Post {PostId} not found for reaction notification", postId);
                return null;
            }

            // Get the reactor user
            var reactorUser = await _userRepository.GetByIdAsync(reactorUserId);
            if (reactorUser == null)
            {
                _logger.LogWarning("Reactor user {UserId} not found", reactorUserId);
                return null;
            }

            // Get the post author via profile
            var postProfile = await _profileRepository.GetByIdAsync(post.ProfileId);
            if (postProfile == null)
            {
                _logger.LogWarning("Post profile {ProfileId} not found", post.ProfileId);
                return null;
            }

            // Don't notify if user reacts to their own post
            if (postProfile.UserId == reactorUserId)
            {
                return null;
            }

            var content = $"{reactorUser.FirstName} {reactorUser.LastName} reacted to your post with {reactionType}";

            var createNotificationDto = new CreateNotificationDto
            {
                UserId = postProfile.UserId,
                Type = NotificationTypes.Reaction,
                Content = content,
                RelatedEntityId = postId,
                RelatedEntityType = NotificationEntityTypes.Post,
                TriggeredByUserId = reactorUserId,
                Priority = NotificationPriority.Low, // Reactions are lower priority
                Metadata = JsonSerializer.Serialize(new { ReactionType = reactionType })
            };

            return await CreateNotificationAsync(createNotificationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reaction notification for post {PostId} by user {UserId}",
                postId, reactorUserId);
            throw;
        }
    }

    public async Task<NotificationDto?> CreateReplyNotificationAsync(Guid originalCommentId, Guid replierUserId, string replyContent)
    {
        try
        {
            // Get the original comment and its author
            var originalComment = await _commentRepository.GetByIdAsync(originalCommentId);
            if (originalComment == null)
            {
                _logger.LogWarning("Original comment {CommentId} not found for reply notification", originalCommentId);
                return null;
            }

            // Get the replier user
            var replierUser = await _userRepository.GetByIdAsync(replierUserId);
            if (replierUser == null)
            {
                _logger.LogWarning("Replier user {UserId} not found", replierUserId);
                return null;
            }

            // Get the original comment author via profile
            var originalCommentProfile = await _profileRepository.GetByIdAsync(originalComment.ProfileId);
            if (originalCommentProfile == null)
            {
                _logger.LogWarning("Comment profile {ProfileId} not found", originalComment.ProfileId);
                return null;
            }

            // Don't notify if user replies to their own comment
            if (originalCommentProfile.UserId == replierUserId)
            {
                return null;
            }

            var truncatedReply = replyContent.Length > 100 
                ? $"{replyContent[..100]}..." 
                : replyContent;

            var content = $"{replierUser.FirstName} {replierUser.LastName} replied to your comment: \"{truncatedReply}\"";

            var createNotificationDto = new CreateNotificationDto
            {
                UserId = originalCommentProfile.UserId,
                Type = NotificationTypes.Reply,
                Content = content,
                RelatedEntityId = originalCommentId,
                RelatedEntityType = NotificationEntityTypes.Comment,
                TriggeredByUserId = replierUserId,
                Priority = NotificationPriority.Normal
            };

            return await CreateNotificationAsync(createNotificationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reply notification for comment {CommentId} by user {UserId}",
                originalCommentId, replierUserId);
            throw;
        }
    }

    public async Task<NotificationDto?> CreateMessageNotificationAsync(Guid recipientUserId, Guid senderUserId, string messagePreview)
    {
        try
        {
            // Get the sender user
            var senderUser = await _userRepository.GetByIdAsync(senderUserId);
            if (senderUser == null)
            {
                _logger.LogWarning("Sender user {UserId} not found", senderUserId);
                return null;
            }

            // Don't notify if user messages themselves
            if (recipientUserId == senderUserId)
            {
                return null;
            }

            var truncatedMessage = messagePreview.Length > 100 
                ? $"{messagePreview[..100]}..." 
                : messagePreview;

            var content = $"{senderUser.FirstName} {senderUser.LastName} sent you a message: \"{truncatedMessage}\"";

            var createNotificationDto = new CreateNotificationDto
            {
                UserId = recipientUserId,
                Type = NotificationTypes.Message,
                Content = content,
                RelatedEntityId = null, // Can be conversation ID if needed
                RelatedEntityType = "Message",
                TriggeredByUserId = senderUserId,
                Priority = NotificationPriority.High // Messages are high priority
            };

            return await CreateNotificationAsync(createNotificationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating message notification for user {UserId} from user {SenderId}",
                recipientUserId, senderUserId);
            throw;
        }
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string keycloakId, NotificationQueryDto? queryParams = null)
    {
        try
        {
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            if (user == null)
            {
                _logger.LogWarning("User with Keycloak ID {KeycloakId} not found", keycloakId);
                return new List<NotificationDto>();
            }

            queryParams ??= new NotificationQueryDto();

            List<Notification> notifications;

            if (!string.IsNullOrEmpty(queryParams.Type))
            {
                notifications = await _notificationRepository.GetUserNotificationsByTypeAsync(
                    user.Id, queryParams.Type, queryParams.Page, queryParams.PageSize);
            }
            else
            {
                notifications = await _notificationRepository.GetUserNotificationsAsync(
                    user.Id, queryParams.UnreadOnly ?? false, queryParams.Page, queryParams.PageSize);
            }

            // Apply additional filters
            if (queryParams.Since.HasValue)
            {
                notifications = notifications.Where(n => n.CreatedAt > queryParams.Since.Value).ToList();
            }

            if (queryParams.Priority.HasValue)
            {
                notifications = notifications.Where(n => n.Priority == queryParams.Priority.Value).ToList();
            }

            var notificationDtos = new List<NotificationDto>();
            foreach (var notification in notifications)
            {
                var dto = await MapToNotificationDtoAsync(notification);
                if (dto != null)
                {
                    notificationDtos.Add(dto);
                }
            }

            return notificationDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {KeycloakId}", keycloakId);
            throw;
        }
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, string keycloakId)
    {
        try
        {
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            if (user == null)
            {
                _logger.LogWarning("User with Keycloak ID {KeycloakId} not found", keycloakId);
                return false;
            }

            var result = await _notificationRepository.MarkAsReadAsync(notificationId, user.Id);
            
            if (result)
            {
                _logger.LogDebug("Marked notification {NotificationId} as read for user {UserId}", 
                    notificationId, user.Id);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {KeycloakId}",
                notificationId, keycloakId);
            throw;
        }
    }

    public async Task<int> MarkAllAsReadAsync(string keycloakId)
    {
        try
        {
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            if (user == null)
            {
                _logger.LogWarning("User with Keycloak ID {KeycloakId} not found", keycloakId);
                return 0;
            }

            var count = await _notificationRepository.MarkAllAsReadAsync(user.Id);
            
            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", count, user.Id);
            
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {KeycloakId}", keycloakId);
            throw;
        }
    }

    public async Task<int> MarkTypeAsReadAsync(string keycloakId, string notificationType)
    {
        try
        {
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            if (user == null)
            {
                _logger.LogWarning("User with Keycloak ID {KeycloakId} not found", keycloakId);
                return 0;
            }

            var count = await _notificationRepository.MarkTypeAsReadAsync(user.Id, notificationType);
            
            _logger.LogInformation("Marked {Count} notifications of type {Type} as read for user {UserId}", 
                count, notificationType, user.Id);
            
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notifications of type {Type} as read for user {KeycloakId}",
                notificationType, keycloakId);
            throw;
        }
    }

    public async Task<NotificationSummaryDto> GetNotificationSummaryAsync(string keycloakId)
    {
        try
        {
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            if (user == null)
            {
                _logger.LogWarning("User with Keycloak ID {KeycloakId} not found", keycloakId);
                return new NotificationSummaryDto();
            }

            var (total, unread, recent, typeBreakdown) = await _notificationRepository.GetNotificationSummaryAsync(user.Id);

            // Get the most recent notification timestamp
            var recentNotifications = await _notificationRepository.GetUserNotificationsAsync(user.Id, false, 1, 1);
            var lastNotificationAt = recentNotifications.FirstOrDefault()?.CreatedAt;

            return new NotificationSummaryDto
            {
                TotalCount = total,
                UnreadCount = unread,
                RecentCount = recent,
                TypeBreakdown = typeBreakdown,
                LastNotificationAt = lastNotificationAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification summary for user {KeycloakId}", keycloakId);
            throw;
        }
    }

    public async Task<bool> DeleteNotificationAsync(Guid notificationId, string keycloakId)
    {
        try
        {
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            if (user == null)
            {
                _logger.LogWarning("User with Keycloak ID {KeycloakId} not found", keycloakId);
                return false;
            }

            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null || notification.UserId != user.Id)
            {
                return false;
            }

            await _notificationRepository.DeleteAsync(notificationId);
            await _notificationRepository.SaveChangesAsync();
            
            _logger.LogInformation("Deleted notification {NotificationId} for user {UserId}", 
                notificationId, user.Id);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId} for user {KeycloakId}",
                notificationId, keycloakId);
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(string keycloakId)
    {
        try
        {
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            if (user == null)
            {
                return 0;
            }

            return await _notificationRepository.GetUnreadCountAsync(user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {KeycloakId}", keycloakId);
            return 0;
        }
    }

    public Task<bool> IsNotificationTypeEnabledAsync(string keycloakId, string notificationType)
    {
        // TODO: Implement user notification preferences
        // For now, all notification types are enabled
        return Task.FromResult(true);
    }

    public async Task<List<NotificationDto>> CreateBatchNotificationsAsync(List<CreateNotificationDto> notifications)
    {
        try
        {
            var createdNotifications = new List<NotificationDto>();

            foreach (var notificationDto in notifications)
            {
                var created = await CreateNotificationAsync(notificationDto);
                if (created != null)
                {
                    createdNotifications.Add(created);
                }
            }

            _logger.LogInformation("Created {Count} notifications in batch", createdNotifications.Count);

            return createdNotifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch notifications");
            throw;
        }
    }

    public async Task<int> CleanupOldNotificationsAsync(int olderThanDays = 30, bool keepUnread = true)
    {
        try
        {
            var count = await _notificationRepository.CleanupOldNotificationsAsync(olderThanDays, keepUnread);
            
            _logger.LogInformation("Cleaned up {Count} old notifications (older than {Days} days)", 
                count, olderThanDays);
            
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old notifications");
            throw;
        }
    }

    private async Task<NotificationDto?> MapToNotificationDtoAsync(Notification notification)
    {
        try
        {
            NotificationUserDto? triggeredByUser = null;
            if (notification.TriggeredByUserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(notification.TriggeredByUserId.Value);
                if (user != null)
                {
                    // Get active profile for the triggered by user
                    NotificationProfileDto? activeProfile = null;
                    if (user.ActiveProfileId.HasValue)
                    {
                        var profile = await _profileRepository.GetByIdAsync(user.ActiveProfileId.Value);
                        if (profile != null)
                        {
                            activeProfile = new NotificationProfileDto
                            {
                                Id = profile.Id,
                                DisplayName = profile.DisplayName,
                                ProfileType = profile.ProfileType?.Name ?? "",
                                AvatarUrl = profile.Avatar
                            };
                        }
                    }

                    triggeredByUser = new NotificationUserDto
                    {
                        Id = user.Id,
                        DisplayName = $"{user.FirstName} {user.LastName}",
                        Email = user.Email,
                        ActiveProfile = activeProfile
                    };
                }
            }

            return new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Type = notification.Type,
                Content = notification.Content,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt,
                RelatedEntityId = notification.RelatedEntityId,
                RelatedEntityType = notification.RelatedEntityType,
                TriggeredByUserId = notification.TriggeredByUserId,
                TriggeredByUser = triggeredByUser,
                Priority = notification.Priority,
                Metadata = notification.Metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping notification {NotificationId} to DTO", notification.Id);
            return null;
        }
    }

    /// <summary>
    /// Send real-time notification via SignalR
    /// </summary>
    private async Task SendRealTimeNotificationAsync(Guid userId, NotificationDto notificationDto)
    {
        try
        {
            if (_hubContext == null)
            {
                return;
            }

            // Get the user to find their Keycloak ID for the SignalR group
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.KeycloakId))
            {
                _logger.LogWarning("Cannot send real-time notification - user {UserId} not found or has no Keycloak ID", userId);
                return;
            }

            // Send to the user's SignalR group
            var groupName = $"user_{user.KeycloakId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", notificationDto);
            
            _logger.LogDebug("Sent real-time notification {NotificationId} to user {KeycloakId}", 
                notificationDto.Id, user.KeycloakId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending real-time notification {NotificationId}", notificationDto.Id);
            // Don't throw - real-time notification failure shouldn't break the notification creation
        }
    }
}