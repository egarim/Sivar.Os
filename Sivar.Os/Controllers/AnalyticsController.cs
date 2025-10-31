using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Data.Repositories;
using Sivar.Os.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sivar.Os.Controllers
{
    /// <summary>
    /// API Controller for analytics and metrics
    /// Queries TimescaleDB continuous aggregates for real-time dashboard data
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AnalyticsRepository _analyticsRepository;

        public AnalyticsController(AnalyticsRepository analyticsRepository)
        {
            _analyticsRepository = analyticsRepository;
        }

        #region Post Metrics

        /// <summary>
        /// Get daily post metrics for a specific author
        /// </summary>
        /// <param name="authorKey">Author's GUID</param>
        /// <param name="startDate">Start date (default: 30 days ago)</param>
        /// <param name="endDate">End date (default: today)</param>
        [HttpGet("posts/author/{authorKey}")]
        public async Task<ActionResult<List<PostMetricsDailyDto>>> GetPostMetricsByAuthor(
            Guid authorKey,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var metrics = await _analyticsRepository.GetPostMetricsByAuthorAsync(authorKey, start, end);
            return Ok(metrics);
        }

        /// <summary>
        /// Get aggregated post metrics by post type
        /// </summary>
        /// <param name="postType">Post type (e.g., "Standard", "Photo", "Video")</param>
        /// <param name="startDate">Start date (default: 30 days ago)</param>
        /// <param name="endDate">End date (default: today)</param>
        /// <param name="limit">Maximum number of results (default: 100)</param>
        [HttpGet("posts/type/{postType}")]
        public async Task<ActionResult<List<PostMetricsDailyDto>>> GetPostMetricsByType(
            string postType,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int limit = 100)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var metrics = await _analyticsRepository.GetPostMetricsByTypeAsync(postType, start, end, limit);
            return Ok(metrics);
        }

        #endregion

        #region Activity Metrics

        /// <summary>
        /// Get hourly activity metrics
        /// </summary>
        /// <param name="startDate">Start date (default: 7 days ago)</param>
        /// <param name="endDate">End date (default: now)</param>
        /// <param name="verb">Optional: Filter by activity verb</param>
        /// <param name="objectType">Optional: Filter by object type</param>
        [HttpGet("activities/hourly")]
        public async Task<ActionResult<List<ActivityMetricsHourlyDto>>> GetActivityMetrics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? verb = null,
            [FromQuery] string? objectType = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            var metrics = await _analyticsRepository.GetActivityMetricsAsync(start, end, verb, objectType);
            return Ok(metrics);
        }

        /// <summary>
        /// Get most active hours for a specific activity verb
        /// </summary>
        /// <param name="verb">Activity verb (e.g., "Create", "Like", "Comment")</param>
        /// <param name="startDate">Start date (default: 7 days ago)</param>
        /// <param name="endDate">End date (default: now)</param>
        /// <param name="topN">Number of top hours to return (default: 10)</param>
        [HttpGet("activities/most-active-hours")]
        public async Task<ActionResult<List<ActivityMetricsHourlyDto>>> GetMostActiveHours(
            [FromQuery] string verb,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int topN = 10)
        {
            if (string.IsNullOrEmpty(verb))
            {
                return BadRequest("Verb parameter is required");
            }

            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            var metrics = await _analyticsRepository.GetMostActiveHoursAsync(verb, start, end, topN);
            return Ok(metrics);
        }

        #endregion

        #region User Engagement

        /// <summary>
        /// Get daily engagement metrics for a specific user
        /// </summary>
        /// <param name="userKey">User's GUID</param>
        /// <param name="startDate">Start date (default: 30 days ago)</param>
        /// <param name="endDate">End date (default: today)</param>
        [HttpGet("users/{userKey}/engagement")]
        public async Task<ActionResult<List<UserEngagementDailyDto>>> GetUserEngagement(
            Guid userKey,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var metrics = await _analyticsRepository.GetUserEngagementAsync(userKey, start, end);
            return Ok(metrics);
        }

        /// <summary>
        /// Get most active users in a time period
        /// </summary>
        /// <param name="startDate">Start date (default: 30 days ago)</param>
        /// <param name="endDate">End date (default: today)</param>
        /// <param name="topN">Number of top users to return (default: 10)</param>
        [HttpGet("users/most-active")]
        public async Task<ActionResult<List<UserEngagementDailyDto>>> GetMostActiveUsers(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int topN = 10)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var metrics = await _analyticsRepository.GetMostActiveUsersAsync(start, end, topN);
            return Ok(metrics);
        }

        #endregion

        #region Post Engagement

        /// <summary>
        /// Get daily engagement metrics for a specific post
        /// </summary>
        /// <param name="postId">Post's GUID</param>
        /// <param name="startDate">Start date (default: 30 days ago)</param>
        /// <param name="endDate">End date (default: today)</param>
        [HttpGet("posts/{postId}/engagement")]
        public async Task<ActionResult<List<PostEngagementDailyDto>>> GetPostEngagement(
            Guid postId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var metrics = await _analyticsRepository.GetPostEngagementAsync(postId, start, end);
            return Ok(metrics);
        }

        /// <summary>
        /// Get most engaged posts in a time period
        /// </summary>
        /// <param name="startDate">Start date (default: 30 days ago)</param>
        /// <param name="endDate">End date (default: today)</param>
        /// <param name="postType">Optional: Filter by post type</param>
        /// <param name="topN">Number of top posts to return (default: 10)</param>
        [HttpGet("posts/most-engaged")]
        public async Task<ActionResult<List<PostEngagementDailyDto>>> GetMostEngagedPosts(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? postType = null,
            [FromQuery] int topN = 10)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var metrics = await _analyticsRepository.GetMostEngagedPostsAsync(start, end, postType, topN);
            return Ok(metrics);
        }

        #endregion

        #region Dashboard Summary

        /// <summary>
        /// Get comprehensive analytics summary for dashboard
        /// </summary>
        /// <param name="startDate">Start date (default: 30 days ago)</param>
        /// <param name="endDate">End date (default: today)</param>
        /// <param name="authorKey">Optional: Filter by author</param>
        [HttpGet("summary")]
        public async Task<ActionResult<AnalyticsSummaryDto>> GetAnalyticsSummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] Guid? authorKey = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var summary = await _analyticsRepository.GetAnalyticsSummaryAsync(start, end, authorKey);
            return Ok(summary);
        }

        #endregion
    }
}
