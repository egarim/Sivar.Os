using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for Comment entity operations
/// Provides hierarchical comment management with threading support
/// </summary>
public class CommentRepository : BaseRepository<Comment>, ICommentRepository
{
    public CommentRepository(SivarDbContext context) : base(context)
    {
    }

    #region Basic CRUD Operations

    public async Task<Comment?> GetByIdAsync(Guid commentId, bool includeReplies = false, bool includeProfile = false)
    {
        var query = _context.Comments.Where(c => c.Id == commentId && !c.IsDeleted);

        if (includeProfile)
        {
            query = query.Include(c => c.Profile);
        }

        if (includeReplies)
        {
            query = query.Include(c => c.Replies.Where(r => !r.IsDeleted));
        }

        return await query.FirstOrDefaultAsync();
    }

    public new async Task<bool> DeleteAsync(Guid commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null || comment.IsDeleted)
            return false;

        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HardDeleteAsync(Guid commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
            return false;

        // Get all descendant comment IDs recursively
        var allDescendantIds = await GetAllDescendantIds(commentId);
        
        // Use ExecuteSqlAsync to truly hard delete (bypass soft delete mechanism)
        if (allDescendantIds.Any())
        {
            // For multiple IDs, we'll delete one by one to avoid SQL injection
            foreach (var descendantId in allDescendantIds)
            {
                await _context.Database.ExecuteSqlAsync($"DELETE FROM \"Comments\" WHERE \"Id\" = {descendantId}");
            }
        }
        
        // Delete the main comment
        await _context.Database.ExecuteSqlAsync($"DELETE FROM \"Comments\" WHERE \"Id\" = {commentId}");
        
        return true;
    }

    private async Task<List<Guid>> GetAllDescendantIds(Guid commentId)
    {
        var allIds = new List<Guid>();
        var currentLevelIds = new List<Guid> { commentId };

        while (currentLevelIds.Any())
        {
            var childIds = await _context.Comments
                .Where(c => currentLevelIds.Contains(c.ParentCommentId ?? Guid.Empty))
                .Select(c => c.Id)
                .ToListAsync();

            if (childIds.Any())
            {
                allIds.AddRange(childIds);
                currentLevelIds = childIds;
            }
            else
            {
                break;
            }
        }

        return allIds;
    }

    #endregion

    #region Post Comment Operations

    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetByPostAsync(
        Guid postId, 
        int page = 0, 
        int pageSize = 20, 
        bool includeReplies = false, 
        bool includeProfile = false)
    {
        var baseQuery = _context.Comments
            .Where(c => c.PostId == postId && !c.IsDeleted);

        var totalCount = await baseQuery.CountAsync();

        IQueryable<Comment> query = baseQuery.OrderByDescending(c => c.CreatedAt);

        if (includeProfile)
        {
            query = query.Include(c => c.Profile);
        }

        if (includeReplies)
        {
            query = query.Include(c => c.Replies.Where(r => !r.IsDeleted));
        }

        var comments = await query
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (comments, totalCount);
    }

    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetTopLevelByPostAsync(
        Guid postId, 
        int page = 0, 
        int pageSize = 20, 
        bool includeProfile = false)
    {
        var baseQuery = _context.Comments
            .Where(c => c.PostId == postId && c.ParentCommentId == null && !c.IsDeleted);

        var totalCount = await baseQuery.CountAsync();

        IQueryable<Comment> query = baseQuery.OrderByDescending(c => c.CreatedAt);

        if (includeProfile)
        {
            query = query.Include(c => c.Profile);
        }

        var comments = await query
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (comments, totalCount);
    }

    public async Task<int> GetCommentCountByPostAsync(Guid postId, bool includeDeleted = false)
    {
        var query = _context.Comments.Where(c => c.PostId == postId);
        
        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        return await query.CountAsync();
    }

    #endregion

    #region Reply Operations

    public async Task<(IEnumerable<Comment> Replies, int TotalCount)> GetRepliesAsync(
        Guid parentCommentId, 
        int page = 0, 
        int pageSize = 10, 
        bool includeProfile = false)
    {
        var baseQuery = _context.Comments
            .Where(c => c.ParentCommentId == parentCommentId && !c.IsDeleted);

        var totalCount = await baseQuery.CountAsync();

        IQueryable<Comment> query = baseQuery.OrderBy(c => c.CreatedAt); // Replies typically shown chronologically

        if (includeProfile)
        {
            query = query.Include(c => c.Profile);
        }

        var replies = await query
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (replies, totalCount);
    }

    public async Task<IEnumerable<Comment>> GetCommentThreadAsync(
        Guid parentCommentId, 
        int maxDepth = 0, 
        bool includeProfile = false)
    {
        var allReplies = new List<Comment>();
        var currentLevelIds = new List<Guid> { parentCommentId };
        var currentDepth = 0;

        while (currentLevelIds.Any() && (maxDepth == 0 || currentDepth < maxDepth))
        {
            var query = _context.Comments
                .Where(c => currentLevelIds.Contains(c.ParentCommentId ?? Guid.Empty) && !c.IsDeleted);

            if (includeProfile)
            {
                query = query.Include(c => c.Profile);
            }

            var levelReplies = await query.ToListAsync();
            allReplies.AddRange(levelReplies);

            currentLevelIds = levelReplies.Select(c => c.Id).ToList();
            currentDepth++;
        }

        return allReplies.OrderBy(c => c.CreatedAt);
    }

    public async Task<int> GetReplyCountAsync(Guid commentId, bool includeDeleted = false)
    {
        var query = _context.Comments.Where(c => c.ParentCommentId == commentId);
        
        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        return await query.CountAsync();
    }

    public async Task<int> GetDescendantCountAsync(Guid commentId, bool includeDeleted = false)
    {
        var totalCount = 0;
        var currentLevelIds = new List<Guid> { commentId };

        while (currentLevelIds.Any())
        {
            var query = _context.Comments
                .Where(c => currentLevelIds.Contains(c.ParentCommentId ?? Guid.Empty));

            if (!includeDeleted)
            {
                query = query.Where(c => !c.IsDeleted);
            }

            var levelReplies = await query.Select(c => c.Id).ToListAsync();
            totalCount += levelReplies.Count;
            currentLevelIds = levelReplies;
        }

        return totalCount;
    }

    #endregion

    #region Profile Comment Operations

    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetByProfileAsync(
        Guid profileId, 
        int page = 0, 
        int pageSize = 20, 
        bool includeReplies = true)
    {
        var query = _context.Comments
            .Where(c => c.ProfileId == profileId && !c.IsDeleted);

        if (!includeReplies)
        {
            query = query.Where(c => c.ParentCommentId == null);
        }

        query = query.OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();

        var comments = await query
            .Include(c => c.Post)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (comments, totalCount);
    }

    public async Task<IEnumerable<Comment>> GetRecentByProfileAsync(
        Guid profileId, 
        DateTime sinceDate, 
        int limit = 50)
    {
        return await _context.Comments
            .Where(c => c.ProfileId == profileId && c.CreatedAt >= sinceDate && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .Include(c => c.Post)
            .ToListAsync();
    }

    #endregion

    #region Search and Filter Operations

    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> SearchCommentsAsync(
        string searchTerm, 
        Guid? postId = null, 
        Guid? profileId = null, 
        int page = 0, 
        int pageSize = 20)
    {
        var query = _context.Comments
            .Where(c => !c.IsDeleted && c.Content.Contains(searchTerm));

        if (postId.HasValue)
        {
            query = query.Where(c => c.PostId == postId.Value);
        }

        if (profileId.HasValue)
        {
            query = query.Where(c => c.ProfileId == profileId.Value);
        }

        query = query.OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();

        var comments = await query
            .Include(c => c.Profile)
            .Include(c => c.Post)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (comments, totalCount);
    }

    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate, 
        Guid? postId = null, 
        int page = 0, 
        int pageSize = 20)
    {
        var query = _context.Comments
            .Where(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate && !c.IsDeleted);

        if (postId.HasValue)
        {
            query = query.Where(c => c.PostId == postId.Value);
        }

        query = query.OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();

        var comments = await query
            .Include(c => c.Profile)
            .Include(c => c.Post)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (comments, totalCount);
    }

    #endregion

    #region Moderation Operations

    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetCommentsForModerationAsync(
        int page = 0, 
        int pageSize = 20)
    {
        // For now, this gets comments that might need moderation
        // In a real system, you'd have flags/reports
        var query = _context.Comments
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();

        var comments = await query
            .Include(c => c.Profile)
            .Include(c => c.Post)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (comments, totalCount);
    }

    public async Task<bool> FlagCommentAsync(Guid commentId, string reason)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
            return false;

        // In a real system, you'd store flags in a separate table
        // For now, we'll just update a field (assuming it exists)
        // comment.IsFlagged = true;
        // comment.FlagReason = reason;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetDeletedCommentsAsync(
        int page = 0, 
        int pageSize = 20)
    {
        var query = _context.Comments
            .Where(c => c.IsDeleted)
            .OrderByDescending(c => c.DeletedAt);

        var totalCount = await query.CountAsync();

        var comments = await query
            .Include(c => c.Profile)
            .Include(c => c.Post)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (comments, totalCount);
    }

    public async Task<bool> RestoreCommentAsync(Guid commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null || !comment.IsDeleted)
            return false;

        comment.IsDeleted = false;
        comment.DeletedAt = null;
        
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Statistics Operations

    public async Task<CommentStatistics> GetCommentStatisticsAsync(Guid postId)
    {
        var comments = await _context.Comments
            .Where(c => c.PostId == postId && !c.IsDeleted)
            .ToListAsync();

        var topLevelComments = comments.Where(c => c.ParentCommentId == null).ToList();
        var replies = comments.Where(c => c.ParentCommentId != null).ToList();

        var uniqueCommenters = comments.Select(c => c.ProfileId).Distinct().Count();
        
        var averageReplies = topLevelComments.Any() 
            ? (double)replies.Count / topLevelComments.Count 
            : 0;

        // Calculate max thread depth
        var maxDepth = await CalculateMaxThreadDepth(postId);

        return new CommentStatistics
        {
            TotalComments = comments.Count,
            TopLevelComments = topLevelComments.Count,
            TotalReplies = replies.Count,
            UniqueCommenters = uniqueCommenters,
            AverageRepliesPerComment = averageReplies,
            MaxThreadDepth = maxDepth,
            LatestCommentDate = comments.Any() ? comments.Max(c => c.CreatedAt) : null,
            OldestCommentDate = comments.Any() ? comments.Min(c => c.CreatedAt) : null
        };
    }

    public async Task<ProfileCommentActivity> GetProfileCommentActivityAsync(Guid profileId, DateTime sinceDate)
    {
        var comments = await _context.Comments
            .Where(c => c.ProfileId == profileId && c.CreatedAt >= sinceDate && !c.IsDeleted)
            .ToListAsync();

        var topLevelComments = comments.Where(c => c.ParentCommentId == null).ToList();
        var replies = comments.Where(c => c.ParentCommentId != null).ToList();
        
        var postsCommentedOn = comments.Select(c => c.PostId).Distinct().Count();
        
        var averageCommentsPerPost = postsCommentedOn > 0 
            ? (double)comments.Count / postsCommentedOn 
            : 0;

        return new ProfileCommentActivity
        {
            TotalComments = comments.Count,
            TopLevelComments = topLevelComments.Count,
            Replies = replies.Count,
            PostsCommentedOn = postsCommentedOn,
            AverageCommentsPerPost = averageCommentsPerPost,
            LastCommentDate = comments.Any() ? comments.Max(c => c.CreatedAt) : null,
            FirstCommentDate = comments.Any() ? comments.Min(c => c.CreatedAt) : null
        };
    }

    private async Task<int> CalculateMaxThreadDepth(Guid postId)
    {
        var topLevelComments = await _context.Comments
            .Where(c => c.PostId == postId && c.ParentCommentId == null && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        var maxDepth = 0;

        foreach (var commentId in topLevelComments)
        {
            var depth = await CalculateCommentThreadDepth(commentId, 1);
            maxDepth = Math.Max(maxDepth, depth);
        }

        return maxDepth;
    }

    private async Task<int> CalculateCommentThreadDepth(Guid commentId, int currentDepth)
    {
        var childIds = await _context.Comments
            .Where(c => c.ParentCommentId == commentId && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        if (!childIds.Any())
            return currentDepth;

        var maxChildDepth = currentDepth;

        foreach (var childId in childIds)
        {
            var childDepth = await CalculateCommentThreadDepth(childId, currentDepth + 1);
            maxChildDepth = Math.Max(maxChildDepth, childDepth);
        }

        return maxChildDepth;
    }

    #endregion

    #region Utility Operations

    public override async Task<bool> ExistsAsync(Guid commentId)
    {
        return await _context.Comments.AnyAsync(c => c.Id == commentId);
    }

    public async Task<int> GetCommentDepthAsync(Guid commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
            return -1;

        var depth = 0;
        var currentComment = comment;

        while (currentComment.ParentCommentId.HasValue)
        {
            depth++;
            currentComment = await _context.Comments.FindAsync(currentComment.ParentCommentId.Value);
            if (currentComment == null)
                break;
        }

        return depth;
    }

    public async Task<Guid> GetRootCommentIdAsync(Guid commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if (comment == null)
            return Guid.Empty;

        var rootComment = comment;

        while (rootComment.ParentCommentId.HasValue)
        {
            var parentComment = await _context.Comments.FindAsync(rootComment.ParentCommentId.Value);
            if (parentComment == null)
                break;
            rootComment = parentComment;
        }

        return rootComment.Id;
    }

    #endregion
}