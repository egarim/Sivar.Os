using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for Reaction entity operations
/// Provides reaction management with aggregation and analytics support
/// </summary>
public class ReactionRepository : BaseRepository<Reaction>, IReactionRepository
{
    public ReactionRepository(SivarDbContext context) : base(context)
    {
    }

    #region Basic CRUD Operations

    public async Task<Reaction?> GetByIdAsync(Guid reactionId, bool includeProfile = false, bool includePost = false, bool includeComment = false)
    {
        IQueryable<Reaction> query = _context.Reactions.Where(r => r.Id == reactionId);

        if (includeProfile)
        {
            query = query.Include(r => r.Profile);
        }

        if (includePost)
        {
            query = query.Include(r => r.Post);
        }

        if (includeComment)
        {
            query = query.Include(r => r.Comment);
        }

        return await query.FirstOrDefaultAsync();
    }

    #endregion

    #region Post Reaction Operations

    public async Task<(IEnumerable<Reaction> Reactions, int TotalCount)> GetByPostAsync(
        Guid postId, 
        int page = 0, 
        int pageSize = 50, 
        ReactionType? reactionType = null, 
        bool includeProfile = false)
    {
        var baseQuery = _context.Reactions.Where(r => r.PostId == postId);

        if (reactionType.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.ReactionType == reactionType.Value);
        }

        var totalCount = await baseQuery.CountAsync();

        IQueryable<Reaction> query = baseQuery.OrderByDescending(r => r.CreatedAt);

        if (includeProfile)
        {
            query = query.Include(r => r.Profile);
        }

        var reactions = await query
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reactions, totalCount);
    }

    public async Task<Dictionary<ReactionType, int>> GetReactionCountsByPostAsync(Guid postId)
    {
        var counts = await _context.Reactions
            .Where(r => r.PostId == postId)
            .GroupBy(r => r.ReactionType)
            .Select(g => new { ReactionType = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(x => x.ReactionType, x => x.Count);
    }

    public async Task<int> GetTotalReactionCountByPostAsync(Guid postId)
    {
        return await _context.Reactions.CountAsync(r => r.PostId == postId);
    }

    public async Task<Reaction?> GetUserReactionToPostAsync(Guid postId, Guid profileId)
    {
        return await _context.Reactions
            .FirstOrDefaultAsync(r => r.PostId == postId && r.ProfileId == profileId);
    }

    public async Task<(IEnumerable<PostReactionSummary> Posts, int TotalCount)> GetMostReactedPostsAsync(
        DateTime startDate, 
        DateTime endDate, 
        ReactionType? reactionType = null, 
        int page = 0, 
        int pageSize = 20)
    {
        var query = _context.Reactions
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate && r.PostId != null);

        if (reactionType.HasValue)
        {
            query = query.Where(r => r.ReactionType == reactionType.Value);
        }

        var reactionSummaries = await query
            .GroupBy(r => r.PostId)
            .Select(g => new PostReactionSummary
            {
                PostId = g.Key!.Value,
                TotalReactions = g.Count(),
                LatestReactionDate = g.Max(r => r.CreatedAt)
            })
            .OrderByDescending(ps => ps.TotalReactions)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Get detailed reaction counts for each post
        var postIds = reactionSummaries.Select(ps => ps.PostId).ToList();
        var reactionCounts = await GetReactionCountsByPostsAsync(postIds);

        foreach (var summary in reactionSummaries)
        {
            if (reactionCounts.TryGetValue(summary.PostId, out var counts))
            {
                summary.ReactionCounts = counts;
            }
        }

        var totalCount = await query
            .GroupBy(r => r.PostId)
            .CountAsync();

        return (reactionSummaries, totalCount);
    }

    #endregion

    #region Comment Reaction Operations

    public async Task<(IEnumerable<Reaction> Reactions, int TotalCount)> GetByCommentAsync(
        Guid commentId, 
        int page = 0, 
        int pageSize = 50, 
        ReactionType? reactionType = null, 
        bool includeProfile = false)
    {
        var baseQuery = _context.Reactions.Where(r => r.CommentId == commentId);

        if (reactionType.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.ReactionType == reactionType.Value);
        }

        var totalCount = await baseQuery.CountAsync();

        IQueryable<Reaction> query = baseQuery.OrderByDescending(r => r.CreatedAt);

        if (includeProfile)
        {
            query = query.Include(r => r.Profile);
        }

        var reactions = await query
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reactions, totalCount);
    }

    public async Task<Dictionary<ReactionType, int>> GetReactionCountsByCommentAsync(Guid commentId)
    {
        var counts = await _context.Reactions
            .Where(r => r.CommentId == commentId)
            .GroupBy(r => r.ReactionType)
            .Select(g => new { ReactionType = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(x => x.ReactionType, x => x.Count);
    }

    public async Task<int> GetTotalReactionCountByCommentAsync(Guid commentId)
    {
        return await _context.Reactions.CountAsync(r => r.CommentId == commentId);
    }

    public async Task<Reaction?> GetUserReactionToCommentAsync(Guid commentId, Guid profileId)
    {
        return await _context.Reactions
            .FirstOrDefaultAsync(r => r.CommentId == commentId && r.ProfileId == profileId);
    }

    #endregion

    #region Profile Reaction Operations

    public async Task<(IEnumerable<Reaction> Reactions, int TotalCount)> GetByProfileAsync(
        Guid profileId, 
        int page = 0, 
        int pageSize = 50, 
        ReactionType? reactionType = null, 
        bool includePost = false, 
        bool includeComment = false)
    {
        var baseQuery = _context.Reactions.Where(r => r.ProfileId == profileId);

        if (reactionType.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.ReactionType == reactionType.Value);
        }

        var totalCount = await baseQuery.CountAsync();

        IQueryable<Reaction> query = baseQuery.OrderByDescending(r => r.CreatedAt);

        if (includePost)
        {
            query = query.Include(r => r.Post);
        }

        if (includeComment)
        {
            query = query.Include(r => r.Comment);
        }

        var reactions = await query
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reactions, totalCount);
    }

    public async Task<IEnumerable<Reaction>> GetRecentByProfileAsync(
        Guid profileId, 
        DateTime sinceDate, 
        int limit = 100)
    {
        return await _context.Reactions
            .Where(r => r.ProfileId == profileId && r.CreatedAt >= sinceDate)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .Include(r => r.Post)
            .Include(r => r.Comment)
            .ToListAsync();
    }

    public async Task<ProfileReactionActivity> GetProfileReactionActivityAsync(Guid profileId, DateTime sinceDate)
    {
        // Reactions given by the profile
        var reactionsGiven = await _context.Reactions
            .Where(r => r.ProfileId == profileId && r.CreatedAt >= sinceDate)
            .ToListAsync();

        // Reactions received on the profile's posts and comments
        var reactionsReceivedOnPosts = await _context.Reactions
            .Where(r => r.Post!.ProfileId == profileId && r.CreatedAt >= sinceDate)
            .ToListAsync();

        var reactionsReceivedOnComments = await _context.Reactions
            .Where(r => r.Comment!.ProfileId == profileId && r.CreatedAt >= sinceDate)
            .ToListAsync();

        var allReactionsReceived = reactionsReceivedOnPosts.Concat(reactionsReceivedOnComments).ToList();

        var reactionsGivenByType = reactionsGiven
            .GroupBy(r => r.ReactionType)
            .ToDictionary(g => g.Key, g => g.Count());

        var reactionsReceivedByType = allReactionsReceived
            .GroupBy(r => r.ReactionType)
            .ToDictionary(g => g.Key, g => g.Count());

        var postsReactedTo = reactionsGiven.Count(r => r.PostId != null);
        var commentsReactedTo = reactionsGiven.Count(r => r.CommentId != null);

        var daysSince = (DateTime.UtcNow - sinceDate).TotalDays;
        var avgReactionsPerDay = daysSince > 0 ? reactionsGiven.Count / daysSince : 0;

        return new ProfileReactionActivity
        {
            TotalReactionsGiven = reactionsGiven.Count,
            TotalReactionsReceived = allReactionsReceived.Count,
            ReactionsGivenByType = reactionsGivenByType,
            ReactionsReceivedByType = reactionsReceivedByType,
            PostsReactedTo = postsReactedTo,
            CommentsReactedTo = commentsReactedTo,
            AverageReactionsPerDay = avgReactionsPerDay,
            LastReactionDate = reactionsGiven.Any() ? reactionsGiven.Max(r => r.CreatedAt) : null,
            FirstReactionDate = reactionsGiven.Any() ? reactionsGiven.Min(r => r.CreatedAt) : null
        };
    }

    #endregion

    #region Batch Operations

    public async Task<ReactionResult> ToggleReactionAsync(
        Guid profileId, 
        ReactionType reactionType, 
        Guid? postId = null, 
        Guid? commentId = null)
    {
        // Validate input
        if (!postId.HasValue && !commentId.HasValue)
        {
            return new ReactionResult 
            { 
                Action = Shared.Repositories.ReactionAction.NoChange, 
                Message = "Either postId or commentId must be provided" 
            };
        }

        if (postId.HasValue && commentId.HasValue)
        {
            return new ReactionResult 
            { 
                Action = Shared.Repositories.ReactionAction.NoChange, 
                Message = "Cannot react to both post and comment simultaneously" 
            };
        }

        // Find existing reaction
        Reaction? existingReaction = null;
        if (postId.HasValue)
        {
            existingReaction = await GetUserReactionToPostAsync(postId.Value, profileId);
        }
        else if (commentId.HasValue)
        {
            existingReaction = await GetUserReactionToCommentAsync(commentId.Value, profileId);
        }

        if (existingReaction == null)
        {
            // Add new reaction
            var newReaction = new Reaction
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                PostId = postId,
                CommentId = commentId,
                ReactionType = reactionType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await AddAsync(newReaction);
            await SaveChangesAsync();

            return new ReactionResult 
            { 
                Action = Shared.Repositories.ReactionAction.Added, 
                Reaction = newReaction, 
                Message = "Reaction added successfully" 
            };
        }
        else if (existingReaction.ReactionType == reactionType)
        {
            // Same reaction type - remove it
            _context.Reactions.Remove(existingReaction);
            await SaveChangesAsync();

            return new ReactionResult 
            { 
                Action = Shared.Repositories.ReactionAction.Removed, 
                Message = "Reaction removed successfully" 
            };
        }
        else
        {
            // Different reaction type - update it
            existingReaction.ReactionType = reactionType;
            existingReaction.UpdatedAt = DateTime.UtcNow;

            await UpdateAsync(existingReaction);
            await SaveChangesAsync();

            return new ReactionResult 
            { 
                Action = Shared.Repositories.ReactionAction.Updated, 
                Reaction = existingReaction, 
                Message = "Reaction updated successfully" 
            };
        }
    }

    public async Task<Dictionary<Guid, IEnumerable<Reaction>>> GetReactionsByPostsAsync(
        IEnumerable<Guid> postIds, 
        Guid? profileId = null)
    {
        var query = _context.Reactions.Where(r => postIds.Contains(r.PostId!.Value));

        if (profileId.HasValue)
        {
            query = query.Where(r => r.ProfileId == profileId.Value);
        }

        var reactions = await query
            .Include(r => r.Profile)
            .ToListAsync();

        return reactions
            .GroupBy(r => r.PostId!.Value)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }

    public async Task<Dictionary<Guid, Dictionary<ReactionType, int>>> GetReactionCountsByPostsAsync(
        IEnumerable<Guid> postIds)
    {
        var reactionCounts = await _context.Reactions
            .Where(r => postIds.Contains(r.PostId!.Value))
            .GroupBy(r => new { PostId = r.PostId!.Value, r.ReactionType })
            .Select(g => new { g.Key.PostId, g.Key.ReactionType, Count = g.Count() })
            .ToListAsync();

        return reactionCounts
            .GroupBy(rc => rc.PostId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.ReactionType, x => x.Count)
            );
    }

    public async Task<Dictionary<Guid, IEnumerable<Reaction>>> GetReactionsByCommentsAsync(
        IEnumerable<Guid> commentIds, 
        Guid? profileId = null)
    {
        var query = _context.Reactions.Where(r => commentIds.Contains(r.CommentId!.Value));

        if (profileId.HasValue)
        {
            query = query.Where(r => r.ProfileId == profileId.Value);
        }

        var reactions = await query
            .Include(r => r.Profile)
            .ToListAsync();

        return reactions
            .GroupBy(r => r.CommentId!.Value)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }

    #endregion

    #region Analytics and Statistics

    public async Task<IEnumerable<ReactionTypeCount>> GetTrendingReactionTypesAsync(
        DateTime startDate, 
        DateTime endDate)
    {
        var reactionCounts = await _context.Reactions
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
            .GroupBy(r => r.ReactionType)
            .Select(g => new { ReactionType = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalReactions = reactionCounts.Sum(rc => rc.Count);

        return reactionCounts
            .OrderByDescending(rc => rc.Count)
            .Select(rc => new ReactionTypeCount
            {
                ReactionType = rc.ReactionType,
                Count = rc.Count,
                Percentage = totalReactions > 0 ? (double)rc.Count / totalReactions * 100 : 0
            })
            .ToList();
    }

    public async Task<IEnumerable<ReactionTimelineItem>> GetReactionTimelineAsync(
        Guid postId, 
        int intervalHours = 1)
    {
        var reactions = await _context.Reactions
            .Where(r => r.PostId == postId)
            .ToListAsync();

        if (!reactions.Any())
            return Enumerable.Empty<ReactionTimelineItem>();

        var startDate = reactions.Min(r => r.CreatedAt);
        var endDate = reactions.Max(r => r.CreatedAt);

        var timelineItems = new List<ReactionTimelineItem>();
        var currentTime = startDate;

        while (currentTime <= endDate)
        {
            var nextTime = currentTime.AddHours(intervalHours);
            
            var reactionsInSlot = reactions
                .Where(r => r.CreatedAt >= currentTime && r.CreatedAt < nextTime)
                .ToList();

            if (reactionsInSlot.Any())
            {
                var reactionsByType = reactionsInSlot
                    .GroupBy(r => r.ReactionType)
                    .ToDictionary(g => g.Key, g => g.Count());

                timelineItems.Add(new ReactionTimelineItem
                {
                    TimeSlot = currentTime,
                    ReactionCount = reactionsInSlot.Count,
                    ReactionsByType = reactionsByType
                });
            }

            currentTime = nextTime;
        }

        return timelineItems;
    }

    public async Task<IEnumerable<TopReactor>> GetTopReactorsToProfileAsync(
        Guid profileId, 
        DateTime sinceDate, 
        int limit = 10)
    {
        // Get reactions to profile's posts and comments
        var reactionsToContent = await _context.Reactions
            .Where(r => (r.Post!.ProfileId == profileId || r.Comment!.ProfileId == profileId) 
                       && r.CreatedAt >= sinceDate 
                       && r.ProfileId != profileId) // Exclude self-reactions
            .Include(r => r.Profile)
            .ToListAsync();

        var topReactors = reactionsToContent
            .GroupBy(r => r.ProfileId)
            .Select(g => new TopReactor
            {
                ProfileId = g.Key,
                ProfileName = g.First().Profile?.DisplayName ?? "Unknown",
                ReactionCount = g.Count(),
                ReactionsByType = g.GroupBy(r => r.ReactionType).ToDictionary(rt => rt.Key, rt => rt.Count()),
                LastReactionDate = g.Max(r => r.CreatedAt)
            })
            .OrderByDescending(tr => tr.ReactionCount)
            .Take(limit)
            .ToList();

        return topReactors;
    }

    public async Task<EngagementStatistics> GetEngagementStatisticsAsync(Guid profileId, DateTime sinceDate)
    {
        // Get profile's posts and comments
        var posts = await _context.Posts
            .Where(p => p.ProfileId == profileId && p.CreatedAt >= sinceDate)
            .ToListAsync();

        var comments = await _context.Comments
            .Where(c => c.ProfileId == profileId && c.CreatedAt >= sinceDate)
            .ToListAsync();

        // Get reactions to posts and comments
        var postIds = posts.Select(p => p.Id).ToList();
        var commentIds = comments.Select(c => c.Id).ToList();

        var reactionsToContent = await _context.Reactions
            .Where(r => (postIds.Contains(r.PostId!.Value) || commentIds.Contains(r.CommentId!.Value))
                       && r.CreatedAt >= sinceDate)
            .ToListAsync();

        var totalReactions = reactionsToContent.Count;
        var uniqueReactors = reactionsToContent.Select(r => r.ProfileId).Distinct().Count();

        var reactionsPerPost = posts.Count > 0 ? (double)reactionsToContent.Count(r => r.PostId.HasValue) / posts.Count : 0;
        var reactionsPerComment = comments.Count > 0 ? (double)reactionsToContent.Count(r => r.CommentId.HasValue) / comments.Count : 0;

        // Calculate engagement rate (reactions per total content items)
        var totalContentItems = posts.Count + comments.Count;
        var engagementRate = totalContentItems > 0 ? (double)totalReactions / totalContentItems : 0;

        // Most used reaction type
        var mostUsedReactionType = reactionsToContent
            .GroupBy(r => r.ReactionType)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? ReactionType.Like;

        return new EngagementStatistics
        {
            TotalPosts = posts.Count,
            TotalComments = comments.Count,
            TotalReactions = totalReactions,
            ReactionsPerPost = reactionsPerPost,
            ReactionsPerComment = reactionsPerComment,
            EngagementRate = engagementRate,
            MostUsedReactionType = mostUsedReactionType,
            UniqueReactors = uniqueReactors
        };
    }

    #endregion

    #region Utility Operations

    public override async Task<bool> ExistsAsync(Guid reactionId)
    {
        return await _context.Reactions.AnyAsync(r => r.Id == reactionId);
    }

    public async Task<ReactionValidationResult> ValidateReactionAsync(
        Guid profileId, 
        ReactionType reactionType, 
        Guid? postId = null, 
        Guid? commentId = null)
    {
        var result = new ReactionValidationResult { IsValid = true };

        // Basic validation
        if (!postId.HasValue && !commentId.HasValue)
        {
            result.IsValid = false;
            result.Errors.Add("Either postId or commentId must be provided");
        }

        if (postId.HasValue && commentId.HasValue)
        {
            result.IsValid = false;
            result.Errors.Add("Cannot react to both post and comment simultaneously");
        }

        // Check if profile exists
        var profileExists = await _context.Profiles.AnyAsync(p => p.Id == profileId);
        if (!profileExists)
        {
            result.IsValid = false;
            result.Errors.Add("Profile not found");
        }

        // Check if post exists (if postId provided)
        if (postId.HasValue)
        {
            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId.Value);
            if (!postExists)
            {
                result.IsValid = false;
                result.Errors.Add("Post not found");
            }
        }

        // Check if comment exists (if commentId provided)
        if (commentId.HasValue)
        {
            var commentExists = await _context.Comments.AnyAsync(c => c.Id == commentId.Value);
            if (!commentExists)
            {
                result.IsValid = false;
                result.Errors.Add("Comment not found");
            }
        }

        // Business rule: Check if user is trying to react to their own content
        if (postId.HasValue)
        {
            var isOwnPost = await _context.Posts.AnyAsync(p => p.Id == postId.Value && p.ProfileId == profileId);
            if (isOwnPost)
            {
                result.Warnings.Add("Reacting to own post");
            }
        }

        if (commentId.HasValue)
        {
            var isOwnComment = await _context.Comments.AnyAsync(c => c.Id == commentId.Value && c.ProfileId == profileId);
            if (isOwnComment)
            {
                result.Warnings.Add("Reacting to own comment");
            }
        }

        return result;
    }

    #endregion
}