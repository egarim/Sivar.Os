
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service implementation for Comment management in the activity stream
/// Provides business logic layer for comment operations with validation and threading support
/// </summary>
public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IPostRepository _postRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IReactionRepository _reactionRepository;

    public CommentService(
        ICommentRepository commentRepository,
        IPostRepository postRepository,
        IUserRepository userRepository,
        IProfileRepository profileRepository,
        IReactionRepository reactionRepository)
    {
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _reactionRepository = reactionRepository ?? throw new ArgumentNullException(nameof(reactionRepository));
    }

    /// <summary>
    /// Creates a new comment on a post for the authenticated user's active profile
    /// </summary>
    public async Task<CommentDto?> CreateCommentAsync(string keycloakId, CreateCommentDto createCommentDto)
    {
        if (string.IsNullOrWhiteSpace(keycloakId) || createCommentDto == null)
            return null;

        // Get user and their active profile
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user?.ActiveProfile == null)
            return null;

        // Validate content
        if (string.IsNullOrWhiteSpace(createCommentDto.Content))
            return null;

        // Check if post exists and user can comment on it
        var post = await _postRepository.GetByIdAsync(createCommentDto.PostId);
        if (post == null)
            return null;

        // TODO: Add permission check for commenting based on post visibility

        // Create comment entity
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            ProfileId = user.ActiveProfile.Id,
            PostId = createCommentDto.PostId,
            Content = createCommentDto.Content.Trim(),
            Language = createCommentDto.Language ?? "en",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _commentRepository.AddAsync(comment);
            return await MapToCommentDtoAsync(comment, keycloakId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a reply to an existing comment
    /// </summary>
    public async Task<CommentDto?> CreateReplyAsync(string keycloakId, Guid parentCommentId, CreateReplyDto createReplyDto)
    {
        if (string.IsNullOrWhiteSpace(keycloakId) || createReplyDto == null)
            return null;

        // Get user and their active profile
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user?.ActiveProfile == null)
            return null;

        // Validate content
        if (string.IsNullOrWhiteSpace(createReplyDto.Content))
            return null;

        // Check if parent comment exists
        var parentComment = await _commentRepository.GetByIdAsync(parentCommentId);
        if (parentComment == null)
            return null;

        // TODO: Add permission check for replying based on post visibility

        // Create reply entity
        var reply = new Comment
        {
            Id = Guid.NewGuid(),
            ProfileId = user.ActiveProfile.Id,
            PostId = parentComment.PostId,
            ParentCommentId = parentCommentId,
            Content = createReplyDto.Content.Trim(),
            Language = createReplyDto.Language ?? "en",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _commentRepository.AddAsync(reply);
            return await MapToCommentDtoAsync(reply, keycloakId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a comment by ID with permission validation
    /// </summary>
    public async Task<CommentDto?> GetCommentByIdAsync(Guid commentId, string? requestingKeycloakId = null, bool includeReplies = true, bool includeReactions = true)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
            return null;

        // Check if user can view this comment
        if (!await CanUserViewCommentAsync(commentId, requestingKeycloakId))
            return null;

        return await MapToCommentDtoAsync(comment, requestingKeycloakId, includeReplies, includeReactions);
    }

    /// <summary>
    /// Updates an existing comment (only by the author)
    /// </summary>
    public async Task<CommentDto?> UpdateCommentAsync(Guid commentId, string keycloakId, UpdateCommentDto updateCommentDto)
    {
        if (string.IsNullOrWhiteSpace(keycloakId) || updateCommentDto == null)
            return null;

        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
            return null;

        // Check if user is the author
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user?.ActiveProfile?.Id != comment.ProfileId)
            return null;

        // Update comment properties
        if (!string.IsNullOrWhiteSpace(updateCommentDto.Content))
        {
            comment.Content = updateCommentDto.Content.Trim();
            comment.IsEdited = true;
            comment.EditedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(updateCommentDto.Language))
            comment.Language = updateCommentDto.Language;

        comment.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _commentRepository.UpdateAsync(comment);
            return await MapToCommentDtoAsync(comment, keycloakId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deletes a comment (only by the author)
    /// </summary>
    public async Task<bool> DeleteCommentAsync(Guid commentId, string keycloakId)
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
            return false;

        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
            return false;

        // Check if user is the author
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user?.ActiveProfile?.Id != comment.ProfileId)
            return false;

        try
        {
            return await _commentRepository.DeleteAsync(commentId);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets comments for a specific post with pagination and threading
    /// </summary>
    public async Task<(IEnumerable<CommentDto> Comments, int TotalCount)> GetCommentsByPostAsync(Guid postId, string? requestingKeycloakId = null, int page = 1, int pageSize = 20, bool includeReplies = true)
    {
        // Check if user can view the post
        var post = await _postRepository.GetByIdAsync(postId);
        if (post == null)
            return (Enumerable.Empty<CommentDto>(), 0);

        // TODO: Add permission check based on post visibility

        var (comments, totalCount) = await _commentRepository.GetByPostAsync(postId, page, pageSize);
        
        var commentDtos = new List<CommentDto>();
        foreach (var comment in comments.Where(c => c.ParentCommentId == null)) // Only top-level comments
        {
            var dto = await MapToCommentDtoAsync(comment, requestingKeycloakId, includeReplies);
            if (dto != null)
                commentDtos.Add(dto);
        }

        return (commentDtos, totalCount);
    }

    /// <summary>
    /// Gets replies to a specific comment with pagination
    /// </summary>
    public async Task<(IEnumerable<CommentDto> Replies, int TotalCount)> GetRepliesByCommentAsync(Guid parentCommentId, string? requestingKeycloakId = null, int page = 1, int pageSize = 10)
    {
        // Check if parent comment exists and user can view it
        if (!await CanUserViewCommentAsync(parentCommentId, requestingKeycloakId))
            return (Enumerable.Empty<CommentDto>(), 0);

        var (replies, totalCount) = await _commentRepository.GetRepliesAsync(parentCommentId, page, pageSize);
        
        var replyDtos = new List<CommentDto>();
        foreach (var reply in replies)
        {
            var dto = await MapToCommentDtoAsync(reply, requestingKeycloakId, false); // Don't include nested replies
            if (dto != null)
                replyDtos.Add(dto);
        }

        return (replyDtos, totalCount);
    }

    /// <summary>
    /// Gets comments by a specific profile with pagination
    /// </summary>
    public async Task<(IEnumerable<CommentDto> Comments, int TotalCount)> GetCommentsByProfileAsync(Guid profileId, string? requestingKeycloakId = null, int page = 1, int pageSize = 20)
    {
        var (comments, totalCount) = await _commentRepository.GetByProfileAsync(profileId, page, pageSize);
        
        var commentDtos = new List<CommentDto>();
        foreach (var comment in comments)
        {
            if (await CanUserViewCommentAsync(comment.Id, requestingKeycloakId))
            {
                var dto = await MapToCommentDtoAsync(comment, requestingKeycloakId);
                if (dto != null)
                    commentDtos.Add(dto);
            }
        }

        return (commentDtos, totalCount);
    }

    /// <summary>
    /// Gets comment thread depth and reply count for a comment
    /// </summary>
    public async Task<CommentThreadStatsDto?> GetCommentThreadStatsAsync(Guid commentId)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
            return null;

        var replyCount = await _commentRepository.GetReplyCountAsync(commentId);
        var descendantCount = await _commentRepository.GetDescendantCountAsync(commentId);

        return new CommentThreadStatsDto
        {
            CommentId = commentId,
            MaxDepth = 0, // TODO: Implement thread depth calculation
            TotalReplies = descendantCount,
            DirectReplies = replyCount,
            LastActivityAt = comment.UpdatedAt
        };
    }

    /// <summary>
    /// Validates if a user can view a specific comment based on post visibility
    /// </summary>
    public async Task<bool> CanUserViewCommentAsync(Guid commentId, string? requestingKeycloakId)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment == null)
            return false;

        // Check if user can view the parent post
        var post = await _postRepository.GetByIdAsync(comment.PostId);
        if (post == null)
            return false;

        // TODO: Implement proper post visibility check
        // For now, allow public posts and authenticated users for other posts
        if (post.Visibility == VisibilityLevel.Public)
            return true;

        return !string.IsNullOrWhiteSpace(requestingKeycloakId);
    }

    /// <summary>
    /// Gets recent activity for comments (for notifications and activity feeds)
    /// </summary>
    public async Task<IEnumerable<CommentActivityDto>> GetRecentCommentActivityAsync(Guid profileId, int hoursBack = 24)
    {
        // TODO: Implement when GetRecentActivityAsync is available in repository
        var (comments, _) = await _commentRepository.GetByProfileAsync(profileId, 0, 50);
        var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);
        
        var recentComments = comments.Where(c => c.CreatedAt >= cutoffTime);
        
        var activityDtos = new List<CommentActivityDto>();
        foreach (var comment in recentComments)
        {
            activityDtos.Add(new CommentActivityDto
            {
                CommentId = comment.Id,
                PostId = comment.PostId,
                Commenter = MapToProfileDto(comment.Profile ?? await _profileRepository.GetByIdAsync(comment.ProfileId) ?? throw new ArgumentException("Profile not found")),
                ActivityType = CommentActivityType.NewComment, // TODO: Determine activity type
                ContentPreview = comment.Content.Length > 100 ? 
                    comment.Content.Substring(0, 100) + "..." : 
                    comment.Content,
                ActivityAt = comment.CreatedAt,
                IsReply = comment.ParentCommentId.HasValue,
                ParentCommentId = comment.ParentCommentId
            });
        }

        return activityDtos;
    }

    #region Private Helper Methods

    /// <summary>
    /// Maps a Comment entity to CommentDto
    /// </summary>
    private async Task<CommentDto?> MapToCommentDtoAsync(Comment comment, string? requestingKeycloakId = null, bool includeReplies = true, bool includeReactions = true)
    {
        if (comment.Profile == null)
        {
            // Load profile if not included
            var profile = await _profileRepository.GetByIdAsync(comment.ProfileId);
            if (profile == null)
                return null;
            comment.Profile = profile;
        }

        var commentDto = new CommentDto
        {
            Id = comment.Id,
            Profile = MapToProfileDto(comment.Profile),
            PostId = comment.PostId,
            ParentCommentId = comment.ParentCommentId,
            Content = comment.Content,
            Language = comment.Language,
            ReplyCount = includeReplies ? await _commentRepository.GetReplyCountAsync(comment.Id) : 0,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            IsEdited = comment.IsEdited,
            EditedAt = comment.EditedAt,
            ThreadDepth = await CalculateThreadDepthAsync(comment)
        };

        // Add replies if requested
        if (includeReplies)
        {
            var (replies, _) = await _commentRepository.GetRepliesAsync(comment.Id);
            var replyDtos = new List<CommentDto>();
            
            foreach (var reply in replies)
            {
                var replyDto = await MapToCommentDtoAsync(reply, requestingKeycloakId, false, includeReactions);
                if (replyDto != null)
                    replyDtos.Add(replyDto);
            }

            commentDto = commentDto with { Replies = replyDtos };
        }

        // Add reaction summary if requested
        if (includeReactions)
        {
            var reactionCounts = await _reactionRepository.GetReactionCountsByCommentAsync(comment.Id);
            ReactionType? userReaction = null;

            if (!string.IsNullOrWhiteSpace(requestingKeycloakId))
            {
                var user = await _userRepository.GetByKeycloakIdAsync(requestingKeycloakId);
                if (user?.ActiveProfile != null)
                {
                    var reaction = await _reactionRepository.GetUserReactionToCommentAsync(comment.Id, user.ActiveProfile.Id);
                    userReaction = reaction?.ReactionType;
                }
            }

            commentDto = commentDto with
            {
                ReactionSummary = new CommentReactionSummaryDto
                {
                    CommentId = comment.Id,
                    TotalReactions = reactionCounts.Sum(r => r.Value),
                    ReactionCounts = reactionCounts,
                    UserReaction = userReaction,
                    TopReactionType = reactionCounts.OrderByDescending(r => r.Value).FirstOrDefault().Key,
                    HasUserReacted = userReaction.HasValue
                }
            };
        }

        return commentDto;
    }

    /// <summary>
    /// Maps a Profile entity to ProfileDto (simplified version)
    /// </summary>
    private ProfileDto MapToProfileDto(Profile profile)
    {
        // TODO: Use proper ProfileService mapping when available
        return new ProfileDto
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            Avatar = profile.Avatar ?? "",
            Bio = profile.Bio ?? "",
            // Add other profile properties as needed
        };
    }

    /// <summary>
    /// Calculates the thread depth for a comment
    /// </summary>
    private async Task<int> CalculateThreadDepthAsync(Comment comment)
    {
        int depth = 0;
        var currentComment = comment;

        while (currentComment.ParentCommentId.HasValue)
        {
            depth++;
            currentComment = await _commentRepository.GetByIdAsync(currentComment.ParentCommentId.Value);
            if (currentComment == null)
                break;
        }

        return depth;
    }

    #endregion
}