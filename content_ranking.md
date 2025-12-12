# Content Ranking System - Sivar.Os

## Overview

A comprehensive content ranking system inspired by the **Elo rating system** (used in chess and gaming) adapted for social content. This system determines what content is worthy to show, supports organic vs sponsored content, and handles ranking decay over time.

### Goals
1. **Surface quality content** - High-quality, engaging content ranks higher
2. **Time-sensitive freshness** - Recent engagement matters more than old
3. **Fair competition** - New content can compete with established content
4. **Monetization support** - Sponsored content has separate but integrated ranking
5. **Abuse resistance** - Gaming the system should be difficult
6. **Integration** - Consumed by chat search and feeds

---

## The Elo-Inspired Ranking Model

### Why Elo?

Traditional metrics (likes, views) have problems:
- **Snowball effect**: Popular content gets more popular (unfair to new content)
- **No decay**: Old viral content forever dominates
- **Easy to game**: Buy likes/views
- **No context**: 100 likes on niche content vs 100 likes on viral content

**Elo advantages:**
- **Relative scoring**: Content is rated based on "matchups" (impressions)
- **Expectation-based**: Beating expectations increases rating more
- **Zero-sum thinking**: When users choose one result over another, ratings update
- **Built-in decay**: Can reset/decay ratings over time windows

### Elo Adaptation for Content

In chess, two players compete. In content ranking:
- **The "match"** = An impression (content shown to user)
- **"Win"** = User engages (click, call, save, etc.)
- **"Loss"** = User ignores or scrolls past
- **"Draw"** = User hovers/views briefly but no action

```
Standard Elo Formula:
R_new = R_old + K × (S - E)

Where:
- R_new = New rating
- R_old = Current rating  
- K = K-factor (sensitivity to change)
- S = Actual score (1 = win, 0.5 = draw, 0 = loss)
- E = Expected score based on rating difference
```

---

## Content Rating Entity

```csharp
/// <summary>
/// Elo-inspired rating for a piece of content (Post, Profile, etc.)
/// </summary>
public class ContentRating : BaseEntity
{
    /// <summary>
    /// The content being rated (Post, Profile, etc.)
    /// </summary>
    public Guid ContentId { get; set; }
    
    /// <summary>
    /// Type of content: Post, Profile, Product, Event, etc.
    /// </summary>
    public ContentType ContentType { get; set; }
    
    /// <summary>
    /// Category for category-relative rankings
    /// </summary>
    [StringLength(50)]
    public string? Category { get; set; }
    
    // ========================================
    // LIFETIME RATINGS (all-time)
    // ========================================
    
    /// <summary>
    /// Lifetime Elo rating (starts at 1200, like chess)
    /// </summary>
    public double LifetimeRating { get; set; } = 1200.0;
    
    /// <summary>
    /// Total "matches" (impressions) all-time
    /// </summary>
    public long LifetimeImpressions { get; set; }
    
    /// <summary>
    /// Total "wins" (engagements) all-time
    /// </summary>
    public long LifetimeEngagements { get; set; }
    
    /// <summary>
    /// Lifetime win rate = engagements / impressions
    /// </summary>
    public double LifetimeWinRate => LifetimeImpressions > 0 
        ? (double)LifetimeEngagements / LifetimeImpressions 
        : 0;
    
    // ========================================
    // ROLLING WINDOW RATINGS (recent performance)
    // ========================================
    
    /// <summary>
    /// 7-day rolling Elo rating
    /// </summary>
    public double WeeklyRating { get; set; } = 1200.0;
    
    /// <summary>
    /// 30-day rolling Elo rating
    /// </summary>
    public double MonthlyRating { get; set; } = 1200.0;
    
    /// <summary>
    /// Impressions in last 7 days
    /// </summary>
    public int WeeklyImpressions { get; set; }
    
    /// <summary>
    /// Impressions in last 30 days
    /// </summary>
    public int MonthlyImpressions { get; set; }
    
    /// <summary>
    /// Engagements in last 7 days
    /// </summary>
    public int WeeklyEngagements { get; set; }
    
    /// <summary>
    /// Engagements in last 30 days
    /// </summary>
    public int MonthlyEngagements { get; set; }
    
    // ========================================
    // COMPOSITE SCORE (for final ranking)
    // ========================================
    
    /// <summary>
    /// Blended score combining lifetime + recent + freshness
    /// This is the score used for search/feed ranking
    /// </summary>
    public double CompositeScore { get; set; }
    
    /// <summary>
    /// Ranking tier for display
    /// </summary>
    public RankingTier Tier { get; set; } = RankingTier.Standard;
    
    // ========================================
    // CONTENT CLASSIFICATION
    // ========================================
    
    /// <summary>
    /// Is this sponsored/paid content?
    /// </summary>
    public bool IsSponsored { get; set; }
    
    /// <summary>
    /// Sponsored bid amount (for sponsored content auction)
    /// </summary>
    public decimal? SponsoredBidAmount { get; set; }
    
    /// <summary>
    /// Is this "suggested" (editor's pick, verified, etc.)?
    /// </summary>
    public bool IsSuggested { get; set; }
    
    /// <summary>
    /// Manual boost factor (for promotions, -1 to +1)
    /// </summary>
    public double ManualBoost { get; set; } = 0;
    
    // ========================================
    // METADATA
    // ========================================
    
    /// <summary>
    /// When was this content created?
    /// </summary>
    public DateTimeOffset ContentCreatedAt { get; set; }
    
    /// <summary>
    /// When was rating last updated?
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }
    
    /// <summary>
    /// K-factor for this content (higher = more volatile ratings)
    /// New content starts with high K, decreases over time
    /// </summary>
    public double KFactor { get; set; } = 40.0;
    
    /// <summary>
    /// Confidence level in the rating (0-1)
    /// Low confidence = few impressions, high variance
    /// </summary>
    public double RatingConfidence { get; set; } = 0;
    
    /// <summary>
    /// Is this content currently active/visible?
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Has been flagged for review (suspicious activity)?
    /// </summary>
    public bool IsFlagged { get; set; }
}

public enum ContentType
{
    Post = 1,
    Profile = 2,
    Product = 3,
    Event = 4,
    Service = 5,
    Procedure = 6
}

public enum RankingTier
{
    /// <summary>Standard organic content</summary>
    Standard = 0,
    
    /// <summary>High-quality verified content</summary>
    Verified = 1,
    
    /// <summary>Editor's picks / curated</summary>
    Suggested = 2,
    
    /// <summary>Paid promotional content</summary>
    Sponsored = 3,
    
    /// <summary>New content in evaluation period</summary>
    New = 4,
    
    /// <summary>Content flagged for low quality</summary>
    LowQuality = 5
}
```

---

## Rating Events (for calculation)

```csharp
/// <summary>
/// Individual rating event/match (stored in TimescaleDB for time-series)
/// </summary>
public class ContentRatingEvent
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The content that was shown
    /// </summary>
    public Guid ContentId { get; set; }
    
    /// <summary>
    /// When the impression occurred
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Type of engagement (or none)
    /// </summary>
    public EngagementType EngagementType { get; set; }
    
    /// <summary>
    /// Position in results (1-based, affects expectations)
    /// </summary>
    public int Position { get; set; }
    
    /// <summary>
    /// Context: search, feed, map, etc.
    /// </summary>
    public ImpressionContext Context { get; set; }
    
    /// <summary>
    /// User's category for personalization
    /// </summary>
    public Guid? ProfileId { get; set; }
    
    /// <summary>
    /// Rating before this event
    /// </summary>
    public double RatingBefore { get; set; }
    
    /// <summary>
    /// Rating change from this event
    /// </summary>
    public double RatingDelta { get; set; }
    
    /// <summary>
    /// Rating after this event
    /// </summary>
    public double RatingAfter { get; set; }
    
    /// <summary>
    /// Expected score (E in Elo formula)
    /// </summary>
    public double ExpectedScore { get; set; }
    
    /// <summary>
    /// Actual score (S in Elo formula)
    /// </summary>
    public double ActualScore { get; set; }
}

public enum EngagementType
{
    /// <summary>No engagement - user scrolled past</summary>
    None = 0,
    
    /// <summary>Brief view (< 2 seconds)</summary>
    Glimpse = 1,
    
    /// <summary>Viewed card details (2-5 seconds)</summary>
    View = 2,
    
    /// <summary>Clicked to expand/see more</summary>
    Click = 3,
    
    /// <summary>Took action: called, messaged, etc.</summary>
    Action = 4,
    
    /// <summary>Saved for later</summary>
    Save = 5,
    
    /// <summary>Shared with others</summary>
    Share = 6,
    
    /// <summary>Left a review/comment</summary>
    Review = 7,
    
    /// <summary>Followed the profile</summary>
    Follow = 8,
    
    /// <summary>Negative: reported or hid</summary>
    NegativeAction = -1
}

public enum ImpressionContext
{
    ChatSearch = 1,
    FeedOrganic = 2,
    FeedSponsored = 3,
    MapView = 4,
    ProfileVisit = 5,
    DirectSearch = 6,
    Recommendation = 7
}
```

---

## The Rating Algorithm

### Elo Calculation with Content Adaptations

```csharp
public class ContentRatingCalculator
{
    // Starting rating (like chess 1200)
    public const double StartingRating = 1200.0;
    
    // Rating floor (content can't go below this)
    public const double MinRating = 400.0;
    
    // Rating ceiling (prevents runaway ratings)
    public const double MaxRating = 2800.0;
    
    // K-factor tiers (how much rating changes per match)
    public const double KFactorNew = 40.0;      // New content (< 100 impressions)
    public const double KFactorProvisional = 25.0;  // Provisional (100-500 impressions)
    public const double KFactorEstablished = 15.0;  // Established (500+ impressions)
    
    /// <summary>
    /// Calculate expected score based on rating difference
    /// Same as chess Elo: E = 1 / (1 + 10^((R_opponent - R_player) / 400))
    /// 
    /// For content: "opponent" is the average rating of content at same position
    /// </summary>
    public double CalculateExpectedScore(double contentRating, double averageRatingAtPosition)
    {
        return 1.0 / (1.0 + Math.Pow(10, (averageRatingAtPosition - contentRating) / 400.0));
    }
    
    /// <summary>
    /// Calculate rating change after an impression/engagement
    /// </summary>
    public double CalculateRatingDelta(
        double currentRating,
        double expectedScore,
        EngagementType engagement,
        int position,
        double kFactor)
    {
        // Convert engagement to score (0 to 1+)
        double actualScore = GetEngagementScore(engagement, position);
        
        // Standard Elo: Δ = K × (S - E)
        double delta = kFactor * (actualScore - expectedScore);
        
        // Apply position bonus (beating expectations at lower positions = more impressive)
        delta *= GetPositionMultiplier(position);
        
        return delta;
    }
    
    /// <summary>
    /// Convert engagement type to Elo score
    /// Win = 1.0, Draw = 0.5, Loss = 0.0
    /// We extend this for stronger signals
    /// </summary>
    private double GetEngagementScore(EngagementType engagement, int position)
    {
        return engagement switch
        {
            // Negative actions are strong losses
            EngagementType.NegativeAction => 0.0,
            
            // No engagement = loss (but position matters)
            EngagementType.None => position <= 3 ? 0.0 : 0.2, // Top 3 positions expected to engage
            
            // Weak engagements = partial wins
            EngagementType.Glimpse => 0.3,
            EngagementType.View => 0.5,
            
            // Standard engagements = wins
            EngagementType.Click => 0.7,
            EngagementType.Action => 0.9,
            
            // Strong engagements = super wins (can exceed 1.0)
            EngagementType.Save => 1.0,
            EngagementType.Share => 1.1,
            EngagementType.Review => 1.2,
            EngagementType.Follow => 1.3,
            
            _ => 0.5
        };
    }
    
    /// <summary>
    /// Position multiplier - engaging from lower positions is more impressive
    /// </summary>
    private double GetPositionMultiplier(int position)
    {
        return position switch
        {
            1 => 0.8,   // Position 1 expected to engage - less impressive
            2 => 0.9,
            3 => 1.0,   // Baseline
            4 => 1.1,
            5 => 1.2,
            >= 6 and <= 10 => 1.3,
            > 10 => 1.5  // Engaging from deep in results is very impressive
        };
    }
    
    /// <summary>
    /// Get appropriate K-factor based on content maturity
    /// </summary>
    public double GetKFactor(long totalImpressions, DateTimeOffset createdAt)
    {
        var ageInDays = (DateTimeOffset.UtcNow - createdAt).TotalDays;
        
        // Brand new content: very volatile
        if (totalImpressions < 50 || ageInDays < 1)
            return KFactorNew;
        
        // New content: volatile
        if (totalImpressions < 100 || ageInDays < 7)
            return KFactorNew;
        
        // Provisional: moderately volatile
        if (totalImpressions < 500 || ageInDays < 30)
            return KFactorProvisional;
        
        // Established: stable
        return KFactorEstablished;
    }
}
```

---

## Composite Score Calculation

The final ranking score blends multiple factors:

```csharp
public class CompositeScoreCalculator
{
    /// <summary>
    /// Calculate the composite score used for final ranking
    /// </summary>
    public double CalculateCompositeScore(ContentRating rating, CompositeScoreWeights weights)
    {
        // 1. BASE RATING BLEND
        // Weight recent performance more than lifetime
        double ratingScore = 
            rating.LifetimeRating * weights.LifetimeWeight +
            rating.MonthlyRating * weights.MonthlyWeight +
            rating.WeeklyRating * weights.WeeklyWeight;
        
        // Normalize to 0-1 scale (assuming 400-2800 range)
        ratingScore = (ratingScore - 400) / 2400;
        
        // 2. FRESHNESS DECAY
        // Content loses freshness over time
        double freshnessScore = CalculateFreshnessScore(
            rating.ContentCreatedAt, 
            weights.FreshnessHalfLifeDays);
        
        // 3. VELOCITY BONUS
        // Content gaining rating quickly gets a boost
        double velocityScore = CalculateVelocityScore(rating);
        
        // 4. CONFIDENCE FACTOR
        // Low-confidence ratings get dampened
        double confidenceFactor = Math.Min(1.0, rating.RatingConfidence * 2);
        
        // 5. TIER MODIFIERS
        double tierBonus = GetTierBonus(rating.Tier);
        
        // 6. MANUAL BOOST (admin overrides)
        double manualBoost = rating.ManualBoost;
        
        // COMBINE
        double composite = 
            (ratingScore * weights.RatingWeight +
             freshnessScore * weights.FreshnessWeight +
             velocityScore * weights.VelocityWeight) *
            confidenceFactor +
            tierBonus +
            manualBoost;
        
        // Clamp to valid range
        return Math.Clamp(composite, 0.0, 1.0);
    }
    
    /// <summary>
    /// Exponential decay for freshness (like radioactive decay)
    /// halfLifeDays = 7 means content has 50% freshness after 1 week
    /// </summary>
    private double CalculateFreshnessScore(DateTimeOffset createdAt, double halfLifeDays)
    {
        double ageInDays = (DateTimeOffset.UtcNow - createdAt).TotalDays;
        double decayRate = Math.Log(2) / halfLifeDays;
        return Math.Exp(-decayRate * ageInDays);
    }
    
    /// <summary>
    /// Velocity = rate of rating change (momentum)
    /// </summary>
    private double CalculateVelocityScore(ContentRating rating)
    {
        if (rating.WeeklyImpressions < 10)
            return 0; // Not enough data
        
        // Compare weekly to monthly win rate
        double weeklyWinRate = rating.WeeklyImpressions > 0 
            ? (double)rating.WeeklyEngagements / rating.WeeklyImpressions 
            : 0;
        double monthlyWinRate = rating.MonthlyImpressions > 0 
            ? (double)rating.MonthlyEngagements / rating.MonthlyImpressions 
            : 0;
        
        // Positive velocity if recent performance is better
        double velocity = weeklyWinRate - monthlyWinRate;
        
        // Normalize to -1 to +1, but we only boost positive velocity
        return Math.Max(0, velocity * 2);
    }
    
    private double GetTierBonus(RankingTier tier)
    {
        return tier switch
        {
            RankingTier.Verified => 0.05,
            RankingTier.Suggested => 0.10,
            RankingTier.New => 0.02,  // Small boost for discoverability
            RankingTier.LowQuality => -0.20,
            _ => 0
        };
    }
}

public class CompositeScoreWeights
{
    // Rating blend weights (should sum to 1.0)
    public double LifetimeWeight { get; set; } = 0.20;  // 20% lifetime
    public double MonthlyWeight { get; set; } = 0.50;   // 50% monthly
    public double WeeklyWeight { get; set; } = 0.30;    // 30% weekly
    
    // Score component weights (should sum to 1.0)
    public double RatingWeight { get; set; } = 0.60;    // 60% rating
    public double FreshnessWeight { get; set; } = 0.25; // 25% freshness
    public double VelocityWeight { get; set; } = 0.15;  // 15% momentum
    
    // Freshness decay parameter
    public double FreshnessHalfLifeDays { get; set; } = 14;  // 50% after 2 weeks
}
```

---

## Sponsored Content Ranking

Sponsored content uses a **second-price auction** combined with quality score:

```csharp
/// <summary>
/// Sponsored content ranking (like Google Ads)
/// </summary>
public class SponsoredContentRanker
{
    /// <summary>
    /// Calculate sponsored content rank
    /// Rank = Bid × QualityScore
    /// </summary>
    public double CalculateSponsoredRank(ContentRating rating)
    {
        if (!rating.IsSponsored || !rating.SponsoredBidAmount.HasValue)
            return 0;
        
        double bid = (double)rating.SponsoredBidAmount.Value;
        
        // Quality score based on organic performance
        // Prevents low-quality content from buying top spots
        double qualityScore = CalculateQualityScore(rating);
        
        return bid * qualityScore;
    }
    
    /// <summary>
    /// Quality score (0-1) based on organic metrics
    /// </summary>
    private double CalculateQualityScore(ContentRating rating)
    {
        // Blend of:
        // - Historic engagement rate (40%)
        // - Rating relative to average (40%)
        // - Content freshness (20%)
        
        double engagementScore = rating.LifetimeWinRate;
        
        double ratingScore = (rating.LifetimeRating - 400) / 2400; // Normalize
        
        double freshnessScore = rating.ContentCreatedAt > DateTimeOffset.UtcNow.AddDays(-30) 
            ? 1.0 
            : 0.5;
        
        return engagementScore * 0.4 + ratingScore * 0.4 + freshnessScore * 0.2;
    }
}
```

---

## Rating Decay & Reset

### Rolling Window Updates (Background Job)

```csharp
public class RatingDecayService
{
    /// <summary>
    /// Run daily to update rolling windows and decay ratings
    /// </summary>
    public async Task ProcessDailyDecay(CancellationToken ct)
    {
        // 1. ROLL WINDOWS
        // Move events older than 7 days out of weekly
        // Move events older than 30 days out of monthly
        await RollTimeWindows(ct);
        
        // 2. RECALCULATE ROLLING RATINGS
        // Recompute weekly/monthly Elo from remaining events
        await RecalculateRollingRatings(ct);
        
        // 3. DECAY INACTIVE CONTENT
        // Content with no impressions in 30 days slowly decays toward average
        await DecayInactiveContent(ct);
        
        // 4. RECALCULATE COMPOSITE SCORES
        await RecalculateCompositeScores(ct);
        
        // 5. DETECT ANOMALIES
        // Flag content with suspicious rating patterns
        await DetectAnomalies(ct);
    }
    
    /// <summary>
    /// Decay content toward average if inactive
    /// </summary>
    private async Task DecayInactiveContent(CancellationToken ct)
    {
        const double DecayRate = 0.02; // 2% decay per day toward average
        const double AverageRating = 1200.0;
        
        var inactiveContent = await _db.ContentRatings
            .Where(r => r.LastUpdated < DateTimeOffset.UtcNow.AddDays(-7))
            .Where(r => r.IsActive)
            .ToListAsync(ct);
        
        foreach (var rating in inactiveContent)
        {
            // Decay toward average
            rating.WeeklyRating = rating.WeeklyRating + 
                (AverageRating - rating.WeeklyRating) * DecayRate;
            rating.MonthlyRating = rating.MonthlyRating + 
                (AverageRating - rating.MonthlyRating) * DecayRate;
            rating.LastUpdated = DateTimeOffset.UtcNow;
        }
        
        await _db.SaveChangesAsync(ct);
    }
}
```

### TimescaleDB Continuous Aggregates

```sql
-- Create extension if not exists
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Rating events table (high volume)
CREATE TABLE "ContentRatingEvents" (
    "Id" uuid NOT NULL,
    "ContentId" uuid NOT NULL,
    "Timestamp" timestamptz NOT NULL,
    "EngagementType" integer NOT NULL,
    "Position" integer NOT NULL,
    "Context" integer NOT NULL,
    "ProfileId" uuid,
    "RatingBefore" double precision NOT NULL,
    "RatingDelta" double precision NOT NULL,
    "RatingAfter" double precision NOT NULL,
    "ExpectedScore" double precision NOT NULL,
    "ActualScore" double precision NOT NULL,
    PRIMARY KEY ("Id", "Timestamp")
);

-- Convert to hypertable
SELECT create_hypertable('"ContentRatingEvents"', 'Timestamp', 
    chunk_time_interval => INTERVAL '1 day');

-- Index for content-specific queries
CREATE INDEX idx_rating_events_content 
    ON "ContentRatingEvents"("ContentId", "Timestamp" DESC);

-- ============================================
-- CONTINUOUS AGGREGATES FOR ROLLING WINDOWS
-- ============================================

-- Daily content stats (for recalculating ratings)
CREATE MATERIALIZED VIEW content_stats_daily
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 day', "Timestamp") AS bucket,
    "ContentId",
    COUNT(*) AS impressions,
    COUNT(*) FILTER (WHERE "EngagementType" > 0) AS engagements,
    AVG("ActualScore") AS avg_score,
    SUM("RatingDelta") AS total_delta,
    AVG("Position") AS avg_position
FROM "ContentRatingEvents"
GROUP BY bucket, "ContentId"
WITH NO DATA;

SELECT add_continuous_aggregate_policy('content_stats_daily',
    start_offset => INTERVAL '2 days',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour');

-- Weekly rollups (for weekly rating calculation)
CREATE MATERIALIZED VIEW content_stats_weekly
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 week', "Timestamp") AS bucket,
    "ContentId",
    SUM(impressions) AS impressions,
    SUM(engagements) AS engagements,
    AVG(avg_score) AS avg_score,
    SUM(total_delta) AS total_delta
FROM content_stats_daily
GROUP BY bucket, "ContentId"
WITH NO DATA;

SELECT add_continuous_aggregate_policy('content_stats_weekly',
    start_offset => INTERVAL '2 weeks',
    end_offset => INTERVAL '1 day',
    schedule_interval => INTERVAL '1 day');

-- ============================================
-- COMPRESSION POLICY
-- ============================================

ALTER TABLE "ContentRatingEvents" SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = '"ContentId"',
    timescaledb.compress_orderby = '"Timestamp" DESC'
);

-- Compress after 7 days
SELECT add_compression_policy('"ContentRatingEvents"', INTERVAL '7 days');

-- Keep raw events for 90 days, then drop
SELECT add_retention_policy('"ContentRatingEvents"', INTERVAL '90 days');
```

---

## Integration with Chat Search

### Updated Search Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                     Search with Content Ranking                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  User: "pizzerias cerca de mi"                                       │
│                    │                                                 │
│                    ▼                                                 │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ 1. HYBRID SEARCH (existing)                                   │   │
│  │    - Semantic: vector similarity                              │   │
│  │    - Full-text: PostgreSQL ts_rank                           │   │
│  │    - Geographic: PostGIS distance                            │   │
│  │    → Produces relevance_score                                 │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                    │                                                 │
│                    ▼                                                 │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ 2. CONTENT RANKING (new - from this plan)                     │   │
│  │    - Load ContentRating for each result                       │   │
│  │    - Get composite_score (Elo + freshness + velocity)         │   │
│  │    - Separate sponsored content                               │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                    │                                                 │
│                    ▼                                                 │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ 3. PERSONALIZATION (from chat3.md Phase 11)                   │   │
│  │    - User affinity boost                                      │   │
│  │    - Category preferences                                     │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                    │                                                 │
│                    ▼                                                 │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ 4. FINAL RANKING                                              │   │
│  │                                                                │   │
│  │    final_score = relevance_score × relevance_weight           │   │
│  │                + composite_score × ranking_weight              │   │
│  │                + personalization × personalization_weight      │   │
│  │                                                                │   │
│  │    Default weights:                                            │   │
│  │    - relevance: 0.50 (query match is still most important)    │   │
│  │    - ranking: 0.35 (content quality/popularity)               │   │
│  │    - personalization: 0.15 (user preferences)                 │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                    │                                                 │
│                    ▼                                                 │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ 5. RESULT INTERLEAVING                                        │   │
│  │    - Insert sponsored results at designated slots             │   │
│  │    - Mark as "Patrocinado" in UI                              │   │
│  │    - Limit: 1 sponsored per 5 organic                         │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                    │                                                 │
│                    ▼                                                 │
│         Return ranked results to user                               │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Updated SearchResultService

```csharp
public class SearchResultService : ISearchResultService
{
    private readonly IContentRatingService _ratingService;
    
    public async Task<SearchResultsCollectionDto> HybridSearchAsync(HybridSearchRequestDto request)
    {
        // 1. Execute existing hybrid search
        var hybridResults = await _postRepository.HybridSearchAsync(...);
        
        // 2. Load content ratings for results
        var contentIds = hybridResults.Select(r => r.Post.Id).ToList();
        var ratings = await _ratingService.GetRatingsAsync(contentIds);
        
        // 3. Calculate final scores
        var rankedResults = hybridResults.Select(result =>
        {
            var rating = ratings.GetValueOrDefault(result.Post.Id);
            
            double relevanceScore = result.CombinedScore;
            double rankingScore = rating?.CompositeScore ?? 0.5;
            double personalizationScore = await GetPersonalizationScore(result.Post, request.ProfileId);
            
            double finalScore = 
                relevanceScore * 0.50 +
                rankingScore * 0.35 +
                personalizationScore * 0.15;
            
            return new
            {
                Result = result,
                Rating = rating,
                FinalScore = finalScore,
                IsSponsored = rating?.IsSponsored ?? false
            };
        })
        .OrderByDescending(r => r.FinalScore)
        .ToList();
        
        // 4. Interleave sponsored content
        var finalResults = InterleaveSponsored(rankedResults);
        
        // 5. Record impressions for rating updates
        await RecordImpressions(finalResults, request);
        
        return MapToDto(finalResults);
    }
}
```

---

## Rating Badges & UI

### Tier Badges

```
┌─────────────────────────────────────────────────────────────────────┐
│  Rating Tier Badges (shown on cards)                                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  🏆 Top Rated     - Rating > 2200 (top 1%)                          │
│  ⭐ Highly Rated  - Rating > 1800 (top 10%)                         │
│  ✅ Verified      - Verified business account                       │
│  🔥 Trending      - High velocity score this week                   │
│  🆕 New           - Less than 7 days old                            │
│  📢 Sponsored     - Paid promotional content                         │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Card Example with Rating

```
┌─────────────────────────────────────────────────────────────────────┐
│  🍕 Pizza Hut Centro                    🏆 Top Rated  ✅ Verified   │
│  ⭐ 4.5 (234 reviews) • 📍 1.2 km                                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [Photo]   Deliciosa pizza con los mejores ingredientes...         │
│                                                                      │
│  📊 Engagement: ████████████████░░░░ 85%  🔥 Trending               │
│                                                                      │
│  📞 Call   💬 WhatsApp   📍 Map   💾 Save                           │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  📢 Sponsored                                                        │
│  🍔 Burger King                                      ⭐ Highly Rated │
│  ⭐ 4.2 (156 reviews) • 📍 2.5 km                                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [Photo]   ¡Nuevas hamburguesas Whopper! 2x1 este fin de semana    │
│                                                                      │
│  📞 Call   💬 WhatsApp   📍 Map   💾 Save                           │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Anti-Gaming Measures

### Fraud Detection

```csharp
public class RatingFraudDetector
{
    /// <summary>
    /// Detect suspicious rating patterns
    /// </summary>
    public async Task<List<FraudAlert>> DetectAnomalies(CancellationToken ct)
    {
        var alerts = new List<FraudAlert>();
        
        // 1. SUDDEN RATING SPIKES
        // Rating increased by more than 200 points in a day
        var spikes = await DetectRatingSpikes(ct);
        alerts.AddRange(spikes);
        
        // 2. UNUSUAL ENGAGEMENT PATTERNS
        // Very high engagement from few unique users
        var lowDiversity = await DetectLowUserDiversity(ct);
        alerts.AddRange(lowDiversity);
        
        // 3. TIME-BASED ANOMALIES
        // Engagements clustered at unusual hours or intervals
        var timeAnomalies = await DetectTimeAnomalies(ct);
        alerts.AddRange(timeAnomalies);
        
        // 4. ENGAGEMENT WITHOUT VIEW TIME
        // Actions taken impossibly fast
        var impossibleSpeed = await DetectImpossibleSpeed(ct);
        alerts.AddRange(impossibleSpeed);
        
        // 5. CROSS-CONTENT PATTERNS
        // Same users engaging with same content owner's content
        var crossPatterns = await DetectCrossContentPatterns(ct);
        alerts.AddRange(crossPatterns);
        
        return alerts;
    }
}
```

### Rate Limiting

```csharp
public class RatingRateLimiter
{
    // Max engagements per content per user per day
    public const int MaxEngagementsPerContentPerUserPerDay = 3;
    
    // Max engagements per user per hour across all content
    public const int MaxEngagementsPerUserPerHour = 50;
    
    // Max rating change per content per day
    public const double MaxRatingChangePerDay = 100.0;
    
    /// <summary>
    /// Check if engagement should be counted for rating
    /// </summary>
    public async Task<bool> ShouldCountEngagement(
        Guid contentId, 
        Guid? profileId, 
        EngagementType engagement)
    {
        if (!profileId.HasValue)
            return true; // Anonymous - harder to track, but limited impact
        
        // Check user-content limit
        var userContentCount = await GetUserContentEngagementsToday(contentId, profileId.Value);
        if (userContentCount >= MaxEngagementsPerContentPerUserPerDay)
            return false;
        
        // Check user hourly limit
        var userHourlyCount = await GetUserEngagementsLastHour(profileId.Value);
        if (userHourlyCount >= MaxEngagementsPerUserPerHour)
            return false;
        
        return true;
    }
}
```

---

## Files to Create

| File | Purpose |
|------|---------|
| `Sivar.Os.Shared/Entities/ContentRating.cs` | Main rating entity |
| `Sivar.Os.Shared/Entities/ContentRatingEvent.cs` | Individual rating events |
| `Sivar.Os.Shared/Entities/RankingConfiguration.cs` | Configurable weights |
| `Sivar.Os.Shared/Repositories/IContentRatingRepository.cs` | Repository interface |
| `Sivar.Os.Data/Repositories/ContentRatingRepository.cs` | Repository implementation |
| `Sivar.Os/Services/ContentRatingService.cs` | Main rating service |
| `Sivar.Os/Services/ContentRatingCalculator.cs` | Elo calculation logic |
| `Sivar.Os/Services/CompositeScoreCalculator.cs` | Composite score logic |
| `Sivar.Os/Services/SponsoredContentRanker.cs` | Sponsored content ranking |
| `Sivar.Os/Services/RatingDecayService.cs` | Background decay job |
| `Sivar.Os/Services/RatingFraudDetector.cs` | Anti-gaming measures |
| `Sivar.Os/Controllers/ContentRatingController.cs` | Admin API |
| `Sivar.Os.Client/Components/RatingBadge.razor` | Rating badge component |

## Files to Modify

| File | Changes |
|------|---------|
| `Sivar.Os.Data/Context/SivarDbContext.cs` | Add `DbSet<ContentRating>` |
| `Sivar.Os/Services/SearchResultService.cs` | Integrate content ranking |
| `Sivar.Os.Shared/DTOs/SearchResultDtos.cs` | Add rating fields to DTOs |
| `Sivar.Os.Client/Components/AIChat/ChatMessage.razor` | Display rating badges |
| `Sivar.Os.Analytics/Context/AnalyticsDbContext.cs` | Add rating events |
| `appsettings.json` | Add ranking configuration section |

---

## Database Migrations

```csharp
// ContentRating table
migrationBuilder.CreateTable(
    name: "ContentRatings",
    columns: table => new
    {
        Id = table.Column<Guid>(nullable: false),
        ContentId = table.Column<Guid>(nullable: false),
        ContentType = table.Column<int>(nullable: false),
        Category = table.Column<string>(maxLength: 50, nullable: true),
        LifetimeRating = table.Column<double>(nullable: false, defaultValue: 1200.0),
        LifetimeImpressions = table.Column<long>(nullable: false, defaultValue: 0),
        LifetimeEngagements = table.Column<long>(nullable: false, defaultValue: 0),
        WeeklyRating = table.Column<double>(nullable: false, defaultValue: 1200.0),
        MonthlyRating = table.Column<double>(nullable: false, defaultValue: 1200.0),
        // ... all other fields
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_ContentRatings", x => x.Id);
        table.UniqueConstraint("AK_ContentRatings_ContentId_ContentType", 
            x => new { x.ContentId, x.ContentType });
    });

migrationBuilder.CreateIndex(
    name: "IX_ContentRatings_CompositeScore",
    table: "ContentRatings",
    column: "CompositeScore",
    descending: new[] { true });

migrationBuilder.CreateIndex(
    name: "IX_ContentRatings_Category_CompositeScore",
    table: "ContentRatings",
    columns: new[] { "Category", "CompositeScore" },
    descending: new[] { false, true });
```

---

## Configuration

```json
// appsettings.json
{
  "ContentRanking": {
    "Elo": {
      "StartingRating": 1200,
      "MinRating": 400,
      "MaxRating": 2800,
      "KFactorNew": 40,
      "KFactorProvisional": 25,
      "KFactorEstablished": 15
    },
    "CompositeScore": {
      "LifetimeWeight": 0.20,
      "MonthlyWeight": 0.50,
      "WeeklyWeight": 0.30,
      "RatingWeight": 0.60,
      "FreshnessWeight": 0.25,
      "VelocityWeight": 0.15,
      "FreshnessHalfLifeDays": 14
    },
    "SearchIntegration": {
      "RelevanceWeight": 0.50,
      "RankingWeight": 0.35,
      "PersonalizationWeight": 0.15
    },
    "Sponsored": {
      "MaxSponsoredPerPage": 2,
      "SponsoredSlotPositions": [3, 8],
      "MinQualityScore": 0.3
    },
    "Decay": {
      "InactivityThresholdDays": 7,
      "DailyDecayRate": 0.02
    }
  }
}
```

---

## Acceptance Criteria

### Core Rating System
- [ ] ContentRating entity created for all content types
- [ ] Elo-based rating calculation working
- [ ] Rating updates on every impression/engagement
- [ ] K-factor decreases as content matures
- [ ] Ratings bounded between 400-2800

### Time Windows
- [ ] Weekly and monthly rolling ratings calculated
- [ ] Daily background job updates rolling windows
- [ ] Inactive content decays toward average
- [ ] TimescaleDB hypertables for rating events

### Composite Score
- [ ] Composite score blends lifetime + recent + freshness
- [ ] Velocity bonus for trending content
- [ ] Confidence factor for low-data content
- [ ] Tier bonuses applied correctly

### Search Integration
- [ ] Chat search uses content ranking
- [ ] Final score combines relevance + ranking + personalization
- [ ] Sponsored content interleaved at designated slots
- [ ] Impressions recorded for rating updates

### UI
- [ ] Rating badges displayed on cards
- [ ] Trending indicator for high-velocity content
- [ ] Sponsored content clearly marked
- [ ] Rating tier visible (if applicable)

### Anti-Gaming
- [ ] Rate limiting per user per content
- [ ] Fraud detection for suspicious patterns
- [ ] Flagging system for manual review
- [ ] Max rating change per day enforced

---

## Relationship to chat3.md

This plan **complements** the phases in `chat3.md`:

| chat3.md Phase | Integration |
|----------------|-------------|
| Phase 2: Unified Search | Content ranking integrated into search pipeline |
| Phase 9: Analytics | Rating events stored in analytics DB |
| Phase 9.5: Tracing | Rating calculations traced for debugging |
| Phase 11: Ranking & Personalization | Content ranking provides the `composite_score` signal |

**Recommended Implementation Order:**
1. Implement this content ranking system
2. Then integrate with Phase 11 (Results Ranking & Personalization)
3. Phase 11 adds personalization ON TOP of content ranking

---

## Success Metrics

| Metric | Target |
|--------|--------|
| Rating correlation with user satisfaction | > 0.7 |
| Top-rated content CTR vs average | > 2x |
| New content discovery rate | > 10% of impressions |
| Sponsored content CTR | > 50% of organic CTR |
| Rating volatility (std dev) | Decreasing over time |
| Fraud detection accuracy | > 95% |

---

## Timeline

| Phase | Duration | Description |
|-------|----------|-------------|
| **1** | 3-4 days | Core entities, Elo calculation, database |
| **2** | 2-3 days | Composite score, decay service |
| **3** | 2-3 days | Search integration, impression tracking |
| **4** | 2-3 days | Sponsored content, UI badges |
| **5** | 2-3 days | Anti-gaming, fraud detection |
| **6** | 1-2 days | Admin dashboard, configuration UI |

**Total: ~14-18 days**

---

## Summary

This content ranking system provides:

1. **Elo-inspired ratings** - Fair, relative scoring that adapts to engagement
2. **Time-based decay** - Recent performance matters more than historic
3. **Rolling windows** - Weekly/monthly ratings for time-sensitive relevance
4. **Sponsored support** - Quality-weighted auction for paid content
5. **Anti-gaming** - Rate limiting and fraud detection
6. **Chat integration** - Consumed by search to surface quality content

The system ensures that high-quality, engaging content rises to the top while new content has a fair chance to compete.
