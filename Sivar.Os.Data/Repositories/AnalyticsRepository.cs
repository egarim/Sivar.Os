using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sivar.Os.Data.Repositories
{
    /// <summary>
    /// Repository for querying TimescaleDB continuous aggregates (materialized views)
    /// Provides fast, pre-computed analytics data
    /// </summary>
    public class AnalyticsRepository
    {
        private readonly SivarDbContext _context;

        public AnalyticsRepository(SivarDbContext context)
        {
            _context = context;
        }

        #region Post Metrics

        /// <summary>
        /// Get daily post metrics for a specific author
        /// </summary>
        public async Task<List<PostMetricsDailyDto>> GetPostMetricsByAuthorAsync(
            Guid authorKey, 
            DateTime startDate, 
            DateTime endDate)
        {
            var sql = $@"
                SELECT 
                    day,
                    author_key as ""AuthorKey"",
                    post_type as ""PostType"",
                    post_count as ""PostCount"",
                    total_views as ""TotalViews"",
                    total_shares as ""TotalShares"",
                    avg_views as ""AvgViews"",
                    last_post_at as ""LastPostAt""
                FROM post_metrics_daily
                WHERE author_key = '{authorKey}'
                  AND day >= '{startDate:yyyy-MM-dd}'
                  AND day <= '{endDate:yyyy-MM-dd}'
                ORDER BY day DESC";

            return await _context.Database
                .SqlQueryRaw<PostMetricsDailyDto>(sql)
                .ToListAsync();
        }

        /// <summary>
        /// Get aggregated post metrics for a specific post type
        /// </summary>
        public async Task<List<PostMetricsDailyDto>> GetPostMetricsByTypeAsync(
            string postType, 
            DateTime startDate, 
            DateTime endDate,
            int limit = 100)
        {
            var sql = $@"
                SELECT 
                    day,
                    author_key as ""AuthorKey"",
                    post_type as ""PostType"",
                    post_count as ""PostCount"",
                    total_views as ""TotalViews"",
                    total_shares as ""TotalShares"",
                    avg_views as ""AvgViews"",
                    last_post_at as ""LastPostAt""
                FROM post_metrics_daily
                WHERE post_type = '{postType}'
                  AND day >= '{startDate:yyyy-MM-dd}'
                  AND day <= '{endDate:yyyy-MM-dd}'
                ORDER BY day DESC, total_views DESC
                LIMIT {limit}";

            return await _context.Database
                .SqlQueryRaw<PostMetricsDailyDto>(sql)
                .ToListAsync();
        }

        #endregion

        #region Activity Metrics

        /// <summary>
        /// Get hourly activity metrics for a specific time range
        /// </summary>
        public async Task<List<ActivityMetricsHourlyDto>> GetActivityMetricsAsync(
            DateTime startDate, 
            DateTime endDate,
            string? verb = null,
            string? objectType = null)
        {
            var whereClause = new List<string>
            {
                $"hour >= '{startDate:yyyy-MM-dd HH:00:00}'",
                $"hour <= '{endDate:yyyy-MM-dd HH:00:00}'"
            };

            if (!string.IsNullOrEmpty(verb))
            {
                whereClause.Add($"verb = '{verb}'");
            }

            if (!string.IsNullOrEmpty(objectType))
            {
                whereClause.Add($"object_type = '{objectType}'");
            }

            var sql = $@"
                SELECT 
                    hour as ""Hour"",
                    verb as ""Verb"",
                    object_type as ""ObjectType"",
                    activity_count as ""ActivityCount"",
                    unique_users as ""UniqueUsers"",
                    last_activity_at as ""LastActivityAt""
                FROM activity_metrics_hourly
                WHERE {string.Join(" AND ", whereClause)}
                ORDER BY hour DESC";

            return await _context.Database
                .SqlQueryRaw<ActivityMetricsHourlyDto>(sql)
                .ToListAsync();
        }

        /// <summary>
        /// Get most active hours for a specific activity verb
        /// </summary>
        public async Task<List<ActivityMetricsHourlyDto>> GetMostActiveHoursAsync(
            string verb,
            DateTime startDate,
            DateTime endDate,
            int topN = 10)
        {
            var sql = $@"
                SELECT 
                    hour as ""Hour"",
                    verb as ""Verb"",
                    object_type as ""ObjectType"",
                    activity_count as ""ActivityCount"",
                    unique_users as ""UniqueUsers"",
                    last_activity_at as ""LastActivityAt""
                FROM activity_metrics_hourly
                WHERE verb = '{verb}'
                  AND hour >= '{startDate:yyyy-MM-dd HH:00:00}'
                  AND hour <= '{endDate:yyyy-MM-dd HH:00:00}'
                ORDER BY activity_count DESC
                LIMIT {topN}";

            return await _context.Database
                .SqlQueryRaw<ActivityMetricsHourlyDto>(sql)
                .ToListAsync();
        }

        #endregion

        #region User Engagement

        /// <summary>
        /// Get daily engagement metrics for a specific user
        /// </summary>
        public async Task<List<UserEngagementDailyDto>> GetUserEngagementAsync(
            Guid userKey,
            DateTime startDate,
            DateTime endDate)
        {
            var sql = $@"
                SELECT 
                    day as ""Day"",
                    user_key as ""UserKey"",
                    total_activities as ""TotalActivities"",
                    creates_count as ""CreatesCount"",
                    likes_count as ""LikesCount"",
                    comments_count as ""CommentsCount"",
                    shares_count as ""SharesCount"",
                    follows_count as ""FollowsCount"",
                    last_activity_at as ""LastActivityAt""
                FROM user_engagement_daily
                WHERE user_key = '{userKey}'
                  AND day >= '{startDate:yyyy-MM-dd}'
                  AND day <= '{endDate:yyyy-MM-dd}'
                ORDER BY day DESC";

            return await _context.Database
                .SqlQueryRaw<UserEngagementDailyDto>(sql)
                .ToListAsync();
        }

        /// <summary>
        /// Get most active users in a time period
        /// </summary>
        public async Task<List<UserEngagementDailyDto>> GetMostActiveUsersAsync(
            DateTime startDate,
            DateTime endDate,
            int topN = 10)
        {
            var sql = $@"
                SELECT 
                    day as ""Day"",
                    user_key as ""UserKey"",
                    total_activities as ""TotalActivities"",
                    creates_count as ""CreatesCount"",
                    likes_count as ""LikesCount"",
                    comments_count as ""CommentsCount"",
                    shares_count as ""SharesCount"",
                    follows_count as ""FollowsCount"",
                    last_activity_at as ""LastActivityAt""
                FROM user_engagement_daily
                WHERE day >= '{startDate:yyyy-MM-dd}'
                  AND day <= '{endDate:yyyy-MM-dd}'
                ORDER BY total_activities DESC
                LIMIT {topN}";

            return await _context.Database
                .SqlQueryRaw<UserEngagementDailyDto>(sql)
                .ToListAsync();
        }

        #endregion

        #region Post Engagement

        /// <summary>
        /// Get daily engagement metrics for a specific post
        /// </summary>
        public async Task<List<PostEngagementDailyDto>> GetPostEngagementAsync(
            Guid postId,
            DateTime startDate,
            DateTime endDate)
        {
            var sql = $@"
                SELECT 
                    day as ""Day"",
                    post_id as ""PostId"",
                    author_key as ""AuthorKey"",
                    post_type as ""PostType"",
                    unique_likes as ""UniqueLikes"",
                    unique_comments as ""UniqueComments"",
                    unique_shares as ""UniqueShares"",
                    total_engaged_users as ""TotalEngagedUsers"",
                    last_engagement_at as ""LastEngagementAt""
                FROM post_engagement_daily
                WHERE post_id = '{postId}'
                  AND day >= '{startDate:yyyy-MM-dd}'
                  AND day <= '{endDate:yyyy-MM-dd}'
                ORDER BY day DESC";

            return await _context.Database
                .SqlQueryRaw<PostEngagementDailyDto>(sql)
                .ToListAsync();
        }

        /// <summary>
        /// Get most engaged posts in a time period
        /// </summary>
        public async Task<List<PostEngagementDailyDto>> GetMostEngagedPostsAsync(
            DateTime startDate,
            DateTime endDate,
            string? postType = null,
            int topN = 10)
        {
            var whereClause = new List<string>
            {
                $"day >= '{startDate:yyyy-MM-dd}'",
                $"day <= '{endDate:yyyy-MM-dd}'"
            };

            if (!string.IsNullOrEmpty(postType))
            {
                whereClause.Add($"post_type = '{postType}'");
            }

            var sql = $@"
                SELECT 
                    day as ""Day"",
                    post_id as ""PostId"",
                    author_key as ""AuthorKey"",
                    post_type as ""PostType"",
                    unique_likes as ""UniqueLikes"",
                    unique_comments as ""UniqueComments"",
                    unique_shares as ""UniqueShares"",
                    total_engaged_users as ""TotalEngagedUsers"",
                    last_engagement_at as ""LastEngagementAt""
                FROM post_engagement_daily
                WHERE {string.Join(" AND ", whereClause)}
                ORDER BY total_engaged_users DESC
                LIMIT {topN}";

            return await _context.Database
                .SqlQueryRaw<PostEngagementDailyDto>(sql)
                .ToListAsync();
        }

        #endregion

        #region Dashboard Summary

        /// <summary>
        /// Get comprehensive analytics summary for dashboard
        /// </summary>
        public async Task<AnalyticsSummaryDto> GetAnalyticsSummaryAsync(
            DateTime startDate,
            DateTime endDate,
            Guid? authorKey = null)
        {
            var authorFilter = authorKey.HasValue ? $"AND author_key = '{authorKey.Value}'" : "";

            var sql = $@"
                WITH post_summary AS (
                    SELECT 
                        SUM(post_count) as total_posts,
                        SUM(total_views) as total_views,
                        SUM(total_shares) as total_shares,
                        post_type as most_popular_type,
                        ROW_NUMBER() OVER (ORDER BY SUM(post_count) DESC) as rn
                    FROM post_metrics_daily
                    WHERE day >= '{startDate:yyyy-MM-dd}'
                      AND day <= '{endDate:yyyy-MM-dd}'
                      {authorFilter}
                    GROUP BY post_type
                ),
                activity_summary AS (
                    SELECT 
                        SUM(activity_count) as total_activities,
                        SUM(unique_users) as unique_engaged_users,
                        EXTRACT(HOUR FROM hour) as hour_of_day,
                        SUM(activity_count) as hour_activity_count,
                        ROW_NUMBER() OVER (ORDER BY SUM(activity_count) DESC) as rn
                    FROM activity_metrics_hourly
                    WHERE hour >= '{startDate:yyyy-MM-dd HH:00:00}'
                      AND hour <= '{endDate:yyyy-MM-dd HH:00:00}'
                    GROUP BY EXTRACT(HOUR FROM hour)
                )
                SELECT 
                    '{startDate:yyyy-MM-dd}'::timestamp as ""PeriodStart"",
                    '{endDate:yyyy-MM-dd}'::timestamp as ""PeriodEnd"",
                    COALESCE(ps.total_posts, 0) as ""TotalPosts"",
                    COALESCE((SELECT SUM(total_activities) FROM activity_summary), 0) as ""TotalActivities"",
                    COALESCE(ps.total_views, 0) as ""TotalViews"",
                    COALESCE(ps.total_shares, 0) as ""TotalShares"",
                    COALESCE((SELECT SUM(unique_engaged_users) FROM activity_summary), 0) as ""UniqueEngagedUsers"",
                    CASE 
                        WHEN COALESCE(ps.total_posts, 0) > 0 
                        THEN COALESCE((SELECT SUM(unique_engaged_users) FROM activity_summary), 0)::float / ps.total_posts 
                        ELSE 0 
                    END as ""AvgEngagementRate"",
                    COALESCE((SELECT hour_of_day FROM activity_summary WHERE rn = 1 LIMIT 1), 0) as ""MostActiveHour"",
                    COALESCE((SELECT most_popular_type FROM post_summary WHERE rn = 1 LIMIT 1), '') as ""MostPopularPostType""
                FROM post_summary ps
                WHERE ps.rn = 1
                LIMIT 1";

            var results = await _context.Database
                .SqlQueryRaw<AnalyticsSummaryDto>(sql)
                .ToListAsync();

            return results.FirstOrDefault() ?? new AnalyticsSummaryDto
            {
                PeriodStart = startDate,
                PeriodEnd = endDate
            };
        }

        #endregion
    }
}
