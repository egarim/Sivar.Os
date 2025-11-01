using System;

namespace Sivar.Os.Shared.DTOs
{
    /// <summary>
    /// Daily post metrics from continuous aggregate
    /// Maps to: post_metrics_daily materialized view
    /// </summary>
    public class PostMetricsDailyDto
    {
        public DateTime Day { get; set; }
        public Guid AuthorKey { get; set; }
        public string PostType { get; set; } = string.Empty;
        public long PostCount { get; set; }
        public long TotalViews { get; set; }
        public long TotalShares { get; set; }
        public double AvgViews { get; set; }
        public DateTime LastPostAt { get; set; }
    }

    /// <summary>
    /// Hourly activity stream statistics from continuous aggregate
    /// Maps to: activity_metrics_hourly materialized view
    /// </summary>
    public class ActivityMetricsHourlyDto
    {
        public DateTime Hour { get; set; }
        public string Verb { get; set; } = string.Empty;
        public string ObjectType { get; set; } = string.Empty;
        public long ActivityCount { get; set; }
        public long UniqueUsers { get; set; }
        public DateTime LastActivityAt { get; set; }
    }

    /// <summary>
    /// Daily user engagement metrics from continuous aggregate
    /// Maps to: user_engagement_daily materialized view
    /// </summary>
    public class UserEngagementDailyDto
    {
        public DateTime Day { get; set; }
        public Guid UserKey { get; set; }
        public long TotalActivities { get; set; }
        public long CreatesCount { get; set; }
        public long LikesCount { get; set; }
        public long CommentsCount { get; set; }
        public long SharesCount { get; set; }
        public long FollowsCount { get; set; }
        public DateTime LastActivityAt { get; set; }
    }

    /// <summary>
    /// Daily post engagement metrics from continuous aggregate
    /// Maps to: post_engagement_daily materialized view
    /// </summary>
    public class PostEngagementDailyDto
    {
        public DateTime Day { get; set; }
        public Guid PostId { get; set; }
        public Guid AuthorKey { get; set; }
        public string PostType { get; set; } = string.Empty;
        public long UniqueLikes { get; set; }
        public long UniqueComments { get; set; }
        public long UniqueShares { get; set; }
        public long TotalEngagedUsers { get; set; }
        public DateTime? LastEngagementAt { get; set; }
    }

    /// <summary>
    /// Request DTO for querying analytics with date range
    /// </summary>
    public class AnalyticsQueryDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid? UserId { get; set; }
        public Guid? ProfileId { get; set; }
        public string? PostType { get; set; }
        public string? Verb { get; set; }
        public string? ObjectType { get; set; }
        public int? Limit { get; set; } = 100;
    }

    /// <summary>
    /// Aggregated analytics summary for dashboards
    /// </summary>
    public class AnalyticsSummaryDto
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public long TotalPosts { get; set; }
        public long TotalActivities { get; set; }
        public long TotalViews { get; set; }
        public long TotalShares { get; set; }
        public long UniqueEngagedUsers { get; set; }
        public double AvgEngagementRate { get; set; }
        public long MostActiveHour { get; set; }
        public string MostPopularPostType { get; set; } = string.Empty;
    }

    /// <summary>
    /// City-level sentiment metrics from continuous aggregate
    /// Maps to: sentiment_metrics_city_daily materialized view
    /// </summary>
    public class SentimentMetricsCityDto
    {
        public DateTime Day { get; set; }
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Emotion { get; set; } = string.Empty;
        public long PostCount { get; set; }
        public double AvgEmotionScore { get; set; }
        public double AvgPolarity { get; set; }
        public double AvgJoy { get; set; }
        public double AvgSadness { get; set; }
        public double AvgAnger { get; set; }
        public double AvgFear { get; set; }
        public long AngerFlaggedCount { get; set; }
        public long NeedsReviewCount { get; set; }
        public DateTime? FirstAnalyzed { get; set; }
        public DateTime? LastAnalyzed { get; set; }
    }

    /// <summary>
    /// Country-level sentiment metrics from continuous aggregate
    /// Maps to: sentiment_metrics_country_daily materialized view
    /// </summary>
    public class SentimentMetricsCountryDto
    {
        public DateTime Day { get; set; }
        public string Country { get; set; } = string.Empty;
        public string Emotion { get; set; } = string.Empty;
        public long PostCount { get; set; }
        public long UniqueCities { get; set; }
        public double AvgEmotionScore { get; set; }
        public double AvgPolarity { get; set; }
        public double AvgJoy { get; set; }
        public double AvgSadness { get; set; }
        public double AvgAnger { get; set; }
        public double AvgFear { get; set; }
        public long AngerFlaggedCount { get; set; }
        public long NeedsReviewCount { get; set; }
        public long TotalViews { get; set; }
        public long TotalShares { get; set; }
        public double AvgViews { get; set; }
        public double AvgShares { get; set; }
        public DateTime? FirstAnalyzed { get; set; }
        public DateTime? LastAnalyzed { get; set; }
    }
}
