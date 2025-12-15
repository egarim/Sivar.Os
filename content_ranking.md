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

## 📢 Search Ads System

> **This section integrates sponsored content (ads) with the content ranking system for search results.**

### Overview: Ads vs Organic Content

```
┌─────────────────────────────────────────────────────────────────────┐
│                     Search Results Composition                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Position 1: 🏆 Organic (Highest ranked)                            │
│  Position 2: ⭐ Organic                                              │
│  Position 3: 📢 SPONSORED (Ad slot #1)    ← Clearly marked          │
│  Position 4: ⭐ Organic                                              │
│  Position 5: ⭐ Organic                                              │
│  Position 6: ⭐ Organic                                              │
│  Position 7: ⭐ Organic                                              │
│  Position 8: 📢 SPONSORED (Ad slot #2)    ← If more ads available   │
│  Position 9: ⭐ Organic                                              │
│  Position 10: ⭐ Organic                                             │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Key Principles

| Principle | Description |
|-----------|-------------|
| **Transparency** | Ads always marked as "Patrocinado" / "Sponsored" |
| **Quality Gate** | Low-quality ads can't buy top spots (quality score required) |
| **Relevance** | Ads must match search context (category, location, keywords) |
| **Fair Competition** | Second-price auction - pay $0.01 more than next bid |
| **Limited Density** | Max 2 ads per 10 results (20% max) |
| **User Experience** | Ads should feel native, not disruptive |

---

### Search Ad Entity

```csharp
/// <summary>
/// Advertisement that can appear in search results
/// </summary>
public class SearchAd : BaseEntity
{
    // ========================================
    // ADVERTISER INFO
    // ========================================
    
    /// <summary>
    /// The profile/business that owns this ad
    /// </summary>
    public virtual Guid AdvertiserProfileId { get; set; }
    public virtual Profile AdvertiserProfile { get; set; } = null!;
    
    /// <summary>
    /// Campaign this ad belongs to (for budget management)
    /// </summary>
    public virtual Guid? CampaignId { get; set; }
    public virtual AdCampaign? Campaign { get; set; }
    
    // ========================================
    // AD CONTENT
    // ========================================
    
    /// <summary>
    /// Ad headline (max 60 chars)
    /// </summary>
    [StringLength(60)]
    public virtual string Headline { get; set; } = string.Empty;
    
    /// <summary>
    /// Ad description (max 150 chars)
    /// </summary>
    [StringLength(150)]
    public virtual string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Display URL shown to users
    /// </summary>
    [StringLength(50)]
    public virtual string? DisplayUrl { get; set; }
    
    /// <summary>
    /// Actual destination URL when clicked
    /// </summary>
    [StringLength(500)]
    public virtual string DestinationUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional image/media
    /// </summary>
    [StringLength(500)]
    public virtual string? ImageUrl { get; set; }
    
    /// <summary>
    /// Call to action button text
    /// </summary>
    [StringLength(25)]
    public virtual string CallToAction { get; set; } = "Ver más";
    
    // ========================================
    // TARGETING
    // ========================================
    
    /// <summary>
    /// Target categories (e.g., "restaurant", "tourism")
    /// Empty = all categories
    /// </summary>
    public virtual ObservableCollection<string> TargetCategories { get; set; } = new();
    
    /// <summary>
    /// Target keywords (search terms that trigger this ad)
    /// </summary>
    public virtual ObservableCollection<string> TargetKeywords { get; set; } = new();
    
    /// <summary>
    /// Target locations (department codes or coordinates)
    /// </summary>
    public virtual ObservableCollection<string> TargetLocations { get; set; } = new();
    
    /// <summary>
    /// Target radius in km (for location-based targeting)
    /// </summary>
    public virtual double? TargetRadiusKm { get; set; }
    
    /// <summary>
    /// Target latitude (center point for radius targeting)
    /// </summary>
    public virtual double? TargetLatitude { get; set; }
    
    /// <summary>
    /// Target longitude (center point for radius targeting)
    /// </summary>
    public virtual double? TargetLongitude { get; set; }
    
    // ========================================
    // BIDDING
    // ========================================
    
    /// <summary>
    /// Maximum bid per impression (CPM model)
    /// </summary>
    public virtual decimal MaxBidPerImpression { get; set; }
    
    /// <summary>
    /// Maximum bid per click (CPC model)
    /// </summary>
    public virtual decimal MaxBidPerClick { get; set; }
    
    /// <summary>
    /// Pricing model: CPC (pay per click) or CPM (pay per 1000 impressions)
    /// </summary>
    public virtual AdPricingModel PricingModel { get; set; } = AdPricingModel.CPC;
    
    /// <summary>
    /// Daily budget limit
    /// </summary>
    public virtual decimal DailyBudget { get; set; }
    
    /// <summary>
    /// Amount spent today
    /// </summary>
    public virtual decimal SpentToday { get; set; }
    
    /// <summary>
    /// Total lifetime budget
    /// </summary>
    public virtual decimal? LifetimeBudget { get; set; }
    
    /// <summary>
    /// Total amount spent
    /// </summary>
    public virtual decimal TotalSpent { get; set; }
    
    // ========================================
    // QUALITY & PERFORMANCE
    // ========================================
    
    /// <summary>
    /// Quality score (0-1) based on CTR, relevance, landing page
    /// </summary>
    public virtual double QualityScore { get; set; } = 0.5;
    
    /// <summary>
    /// Click-through rate (clicks / impressions)
    /// </summary>
    public virtual double ClickThroughRate { get; set; }
    
    /// <summary>
    /// Total impressions
    /// </summary>
    public virtual long TotalImpressions { get; set; }
    
    /// <summary>
    /// Total clicks
    /// </summary>
    public virtual long TotalClicks { get; set; }
    
    // ========================================
    // STATUS
    // ========================================
    
    /// <summary>
    /// Ad status: Draft, Active, Paused, Completed, Rejected
    /// </summary>
    public virtual AdStatus Status { get; set; } = AdStatus.Draft;
    
    /// <summary>
    /// When the ad starts running
    /// </summary>
    public virtual DateTimeOffset? StartDate { get; set; }
    
    /// <summary>
    /// When the ad stops running
    /// </summary>
    public virtual DateTimeOffset? EndDate { get; set; }
    
    /// <summary>
    /// Review status for content moderation
    /// </summary>
    public virtual AdReviewStatus ReviewStatus { get; set; } = AdReviewStatus.Pending;
    
    /// <summary>
    /// Rejection reason if rejected
    /// </summary>
    [StringLength(500)]
    public virtual string? RejectionReason { get; set; }
}

public enum AdPricingModel
{
    CPC = 1,  // Cost Per Click
    CPM = 2   // Cost Per Mille (1000 impressions)
}

public enum AdStatus
{
    Draft = 0,
    Active = 1,
    Paused = 2,
    Completed = 3,
    Rejected = 4
}

public enum AdReviewStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
```

---

### Ad Campaign Entity (Budget Container)

```csharp
/// <summary>
/// Campaign container for multiple ads with shared budget
/// </summary>
public class AdCampaign : BaseEntity
{
    public virtual Guid AdvertiserProfileId { get; set; }
    public virtual Profile AdvertiserProfile { get; set; } = null!;
    
    [StringLength(100)]
    public virtual string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Total campaign budget
    /// </summary>
    public virtual decimal Budget { get; set; }
    
    /// <summary>
    /// Amount spent from budget
    /// </summary>
    public virtual decimal Spent { get; set; }
    
    /// <summary>
    /// Daily spend limit across all ads
    /// </summary>
    public virtual decimal DailyLimit { get; set; }
    
    /// <summary>
    /// Amount spent today
    /// </summary>
    public virtual decimal SpentToday { get; set; }
    
    public virtual DateTimeOffset StartDate { get; set; }
    public virtual DateTimeOffset? EndDate { get; set; }
    
    public virtual CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    
    /// <summary>
    /// Ads in this campaign
    /// </summary>
    public virtual ObservableCollection<SearchAd> Ads { get; set; } = new();
}

public enum CampaignStatus
{
    Draft = 0,
    Active = 1,
    Paused = 2,
    Completed = 3,
    OutOfBudget = 4
}
```

---

### Ad Selection Algorithm

```csharp
/// <summary>
/// Selects which ads to show in search results
/// </summary>
public class SearchAdSelector
{
    private readonly ISearchAdRepository _adRepository;
    private readonly ILogger<SearchAdSelector> _logger;
    
    /// <summary>
    /// Select ads for a search request using auction model
    /// </summary>
    public async Task<List<SearchAdResult>> SelectAdsForSearchAsync(
        SearchContext context,
        int maxAds = 2)
    {
        _logger.LogInformation(
            "[SelectAds] Query={Query}, Category={Category}, Location=({Lat},{Lng})",
            context.Query, context.Category, context.Latitude, context.Longitude);
        
        // 1. Get eligible ads (matching targeting criteria)
        var eligibleAds = await GetEligibleAdsAsync(context);
        
        if (!eligibleAds.Any())
        {
            _logger.LogInformation("[SelectAds] No eligible ads found");
            return new List<SearchAdResult>();
        }
        
        // 2. Calculate ad rank for each (Bid × QualityScore)
        var rankedAds = eligibleAds
            .Select(ad => new
            {
                Ad = ad,
                AdRank = CalculateAdRank(ad, context),
                RelevanceScore = CalculateRelevanceScore(ad, context)
            })
            .Where(x => x.AdRank > 0)
            .OrderByDescending(x => x.AdRank)
            .Take(maxAds)
            .ToList();
        
        // 3. Calculate actual price (second-price auction)
        var results = new List<SearchAdResult>();
        for (int i = 0; i < rankedAds.Count; i++)
        {
            var current = rankedAds[i];
            
            // Second-price: pay just enough to beat next bidder
            decimal actualPrice;
            if (i + 1 < rankedAds.Count)
            {
                var nextBidder = rankedAds[i + 1];
                // Price = NextAdRank / YourQualityScore + $0.01
                actualPrice = (decimal)(nextBidder.AdRank / current.Ad.QualityScore) + 0.01m;
            }
            else
            {
                // No competition - pay minimum
                actualPrice = 0.01m;
            }
            
            results.Add(new SearchAdResult
            {
                Ad = current.Ad,
                AdRank = current.AdRank,
                RelevanceScore = current.RelevanceScore,
                ActualPrice = Math.Min(actualPrice, current.Ad.MaxBidPerClick),
                Position = 0 // Assigned later during interleaving
            });
        }
        
        _logger.LogInformation(
            "[SelectAds] Selected {Count} ads from {Eligible} eligible",
            results.Count, eligibleAds.Count);
        
        return results;
    }
    
    /// <summary>
    /// Get ads matching targeting criteria
    /// </summary>
    private async Task<List<SearchAd>> GetEligibleAdsAsync(SearchContext context)
    {
        var now = DateTimeOffset.UtcNow;
        
        return await _adRepository.Query()
            .Where(ad => ad.Status == AdStatus.Active)
            .Where(ad => ad.ReviewStatus == AdReviewStatus.Approved)
            .Where(ad => ad.StartDate <= now && (ad.EndDate == null || ad.EndDate > now))
            .Where(ad => ad.SpentToday < ad.DailyBudget)
            .Where(ad => ad.Campaign == null || ad.Campaign.SpentToday < ad.Campaign.DailyLimit)
            // Category targeting
            .Where(ad => !ad.TargetCategories.Any() || 
                        ad.TargetCategories.Contains(context.Category))
            // Keyword targeting
            .Where(ad => !ad.TargetKeywords.Any() || 
                        ad.TargetKeywords.Any(k => context.Query.Contains(k, StringComparison.OrdinalIgnoreCase)))
            // Location targeting (simplified - full impl uses PostGIS)
            .Where(ad => ad.TargetLatitude == null || 
                        CalculateDistanceKm(ad.TargetLatitude.Value, ad.TargetLongitude!.Value,
                                           context.Latitude, context.Longitude) <= ad.TargetRadiusKm)
            .ToListAsync();
    }
    
    /// <summary>
    /// Ad Rank = MaxBid × QualityScore × RelevanceBoost
    /// Higher rank = better ad position
    /// </summary>
    private double CalculateAdRank(SearchAd ad, SearchContext context)
    {
        var bid = (double)ad.MaxBidPerClick;
        var qualityScore = ad.QualityScore;
        var relevanceBoost = CalculateRelevanceScore(ad, context);
        
        // Minimum quality threshold
        if (qualityScore < 0.3)
        {
            return 0; // Reject low-quality ads
        }
        
        return bid * qualityScore * relevanceBoost;
    }
    
    /// <summary>
    /// Calculate how relevant ad is to the search context
    /// </summary>
    private double CalculateRelevanceScore(SearchAd ad, SearchContext context)
    {
        double score = 1.0;
        
        // Keyword match bonus
        var keywordMatches = ad.TargetKeywords
            .Count(k => context.Query.Contains(k, StringComparison.OrdinalIgnoreCase));
        if (keywordMatches > 0)
        {
            score += 0.2 * Math.Min(keywordMatches, 3); // Max 0.6 bonus
        }
        
        // Category match bonus
        if (ad.TargetCategories.Contains(context.Category))
        {
            score += 0.3;
        }
        
        // Location proximity bonus
        if (ad.TargetLatitude.HasValue && context.Latitude != 0)
        {
            var distanceKm = CalculateDistanceKm(
                ad.TargetLatitude.Value, ad.TargetLongitude!.Value,
                context.Latitude, context.Longitude);
            
            if (distanceKm < 1) score += 0.3;
            else if (distanceKm < 5) score += 0.2;
            else if (distanceKm < 10) score += 0.1;
        }
        
        return score;
    }
}

public class SearchContext
{
    public string Query { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Guid? UserId { get; set; }
}

public class SearchAdResult
{
    public SearchAd Ad { get; set; } = null!;
    public double AdRank { get; set; }
    public double RelevanceScore { get; set; }
    public decimal ActualPrice { get; set; }
    public int Position { get; set; }
}
```

---

### Result Interleaving (Organic + Ads)

```csharp
/// <summary>
/// Combines organic results with ads at designated positions
/// </summary>
public class SearchResultInterleaver
{
    // Ad slot positions (0-indexed): positions 2 and 7
    private readonly int[] AdSlotPositions = { 2, 7 };
    
    /// <summary>
    /// Interleave organic results with ads
    /// </summary>
    public SearchResultsCollectionDto InterleaveResults(
        List<BusinessSearchResultDto> organicResults,
        List<SearchAdResult> ads)
    {
        var finalResults = new List<object>();
        var organicIndex = 0;
        var adIndex = 0;
        var position = 0;
        
        while (organicIndex < organicResults.Count || adIndex < ads.Count)
        {
            // Check if this position should be an ad slot
            if (adIndex < ads.Count && AdSlotPositions.Contains(position))
            {
                // Insert ad
                var adResult = ads[adIndex];
                adResult.Position = position + 1; // 1-indexed for display
                
                finalResults.Add(new SearchResultItemDto
                {
                    Type = SearchResultType.Ad,
                    Ad = MapAdToDto(adResult),
                    Position = position + 1
                });
                
                adIndex++;
            }
            else if (organicIndex < organicResults.Count)
            {
                // Insert organic result
                var organic = organicResults[organicIndex];
                organic.Position = position + 1;
                
                finalResults.Add(new SearchResultItemDto
                {
                    Type = SearchResultType.Organic,
                    Business = organic,
                    Position = position + 1
                });
                
                organicIndex++;
            }
            
            position++;
        }
        
        return new SearchResultsCollectionDto
        {
            Items = finalResults,
            TotalOrganic = organicResults.Count,
            TotalAds = ads.Count,
            Query = /* from context */
        };
    }
}

public enum SearchResultType
{
    Organic = 1,
    Ad = 2,
    Sponsored = 3 // Same as Ad but different label
}
```

---

### Ad Impression & Click Tracking

```csharp
/// <summary>
/// Track ad impressions and clicks for billing and quality score
/// </summary>
public class SearchAdTrackingService
{
    private readonly ISearchAdRepository _adRepository;
    private readonly IAdEventRepository _eventRepository;
    
    /// <summary>
    /// Record when ads are displayed in search results
    /// </summary>
    public async Task RecordImpressionsAsync(
        List<SearchAdResult> displayedAds,
        SearchContext context)
    {
        var events = displayedAds.Select(adResult => new AdImpressionEvent
        {
            Id = Guid.NewGuid(),
            AdId = adResult.Ad.Id,
            CampaignId = adResult.Ad.CampaignId,
            UserId = context.UserId,
            Timestamp = DateTimeOffset.UtcNow,
            Position = adResult.Position,
            SearchQuery = context.Query,
            SearchCategory = context.Category,
            ActualCost = adResult.Ad.PricingModel == AdPricingModel.CPM 
                ? adResult.ActualPrice / 1000 // CPM = per 1000 impressions
                : 0 // CPC = only pay on click
        }).ToList();
        
        await _eventRepository.BulkInsertAsync(events);
        
        // Update impression counts (batch update)
        await UpdateImpressionCountsAsync(displayedAds);
    }
    
    /// <summary>
    /// Record when user clicks an ad
    /// </summary>
    public async Task RecordClickAsync(
        Guid adId,
        Guid? userId,
        int position,
        decimal actualCost)
    {
        var clickEvent = new AdClickEvent
        {
            Id = Guid.NewGuid(),
            AdId = adId,
            UserId = userId,
            Timestamp = DateTimeOffset.UtcNow,
            Position = position,
            ActualCost = actualCost
        };
        
        await _eventRepository.AddAsync(clickEvent);
        
        // Update ad stats
        var ad = await _adRepository.GetByIdAsync(adId);
        if (ad != null)
        {
            ad.TotalClicks++;
            ad.SpentToday += actualCost;
            ad.TotalSpent += actualCost;
            ad.ClickThroughRate = (double)ad.TotalClicks / Math.Max(1, ad.TotalImpressions);
            
            // Update campaign if applicable
            if (ad.Campaign != null)
            {
                ad.Campaign.SpentToday += actualCost;
                ad.Campaign.Spent += actualCost;
                
                // Check if campaign out of budget
                if (ad.Campaign.Spent >= ad.Campaign.Budget)
                {
                    ad.Campaign.Status = CampaignStatus.OutOfBudget;
                }
            }
            
            await _adRepository.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Daily job: Update quality scores based on CTR
    /// </summary>
    public async Task UpdateQualityScoresAsync()
    {
        var ads = await _adRepository.GetActiveAdsAsync();
        
        foreach (var ad in ads)
        {
            if (ad.TotalImpressions < 100)
            {
                // Not enough data - use default
                ad.QualityScore = 0.5;
                continue;
            }
            
            // Quality score based on CTR relative to average
            // Average CTR is typically 2-3% for search ads
            var avgCtr = 0.025;
            var ctrRatio = ad.ClickThroughRate / avgCtr;
            
            // Clamp to 0.1 - 1.0 range
            ad.QualityScore = Math.Clamp(ctrRatio * 0.5 + 0.25, 0.1, 1.0);
        }
        
        await _adRepository.SaveChangesAsync();
    }
    
    /// <summary>
    /// Daily job: Reset daily spend counters
    /// </summary>
    public async Task ResetDailySpendAsync()
    {
        await _adRepository.ExecuteRawSqlAsync(
            @"UPDATE ""SearchAds"" SET ""SpentToday"" = 0");
        await _adRepository.ExecuteRawSqlAsync(
            @"UPDATE ""AdCampaigns"" SET ""SpentToday"" = 0, 
              ""Status"" = CASE WHEN ""Status"" = 4 AND ""Spent"" < ""Budget"" 
                               THEN 1 ELSE ""Status"" END");
    }
}
```

---

### UI Display for Ads

```razor
@* SearchAdCard.razor - Display ad in search results *@

<div class="search-ad-card" data-ad-id="@Ad.Id">
    @* Sponsored badge - ALWAYS visible *@
    <div class="ad-badge">
        <MudIcon Icon="@Icons.Material.Filled.Campaign" Size="Size.Small" />
        <span>@Localizer["Sponsored"]</span>
    </div>
    
    <div class="ad-content" @onclick="HandleAdClick">
        @if (!string.IsNullOrEmpty(Ad.ImageUrl))
        {
            <div class="ad-image">
                <img src="@Ad.ImageUrl" alt="@Ad.Headline" />
            </div>
        }
        
        <div class="ad-text">
            <h3 class="ad-headline">@Ad.Headline</h3>
            <p class="ad-description">@Ad.Description</p>
            
            @if (!string.IsNullOrEmpty(Ad.DisplayUrl))
            {
                <span class="ad-url">@Ad.DisplayUrl</span>
            }
        </div>
        
        <MudButton Color="Color.Primary" Variant="Variant.Filled" Size="Size.Small">
            @Ad.CallToAction
        </MudButton>
    </div>
</div>

@code {
    [Parameter] public SearchAdDto Ad { get; set; } = null!;
    [Parameter] public EventCallback<SearchAdDto> OnClick { get; set; }
    
    private async Task HandleAdClick()
    {
        await OnClick.InvokeAsync(Ad);
        // Navigation happens in parent after tracking click
    }
}
```

```css
/* SearchAdCard.razor.css */
.search-ad-card {
    position: relative;
    background: linear-gradient(135deg, 
        rgba(124, 77, 255, 0.05) 0%, 
        rgba(124, 77, 255, 0.02) 100%);
    border: 1px solid rgba(124, 77, 255, 0.2);
    border-radius: 12px;
    padding: 16px;
    transition: all 0.2s ease;
}

.search-ad-card:hover {
    border-color: rgba(124, 77, 255, 0.4);
    box-shadow: 0 4px 12px rgba(124, 77, 255, 0.15);
}

.ad-badge {
    position: absolute;
    top: 8px;
    right: 8px;
    display: flex;
    align-items: center;
    gap: 4px;
    padding: 4px 8px;
    background: rgba(124, 77, 255, 0.15);
    border-radius: 4px;
    font-size: 0.75rem;
    color: var(--mud-palette-primary);
}

.ad-content {
    cursor: pointer;
}

.ad-headline {
    font-size: 1.1rem;
    font-weight: 600;
    color: var(--mud-palette-primary);
    margin-bottom: 4px;
}

.ad-description {
    font-size: 0.9rem;
    color: var(--mud-palette-text-secondary);
    margin-bottom: 8px;
}

.ad-url {
    font-size: 0.8rem;
    color: var(--mud-palette-text-disabled);
}
```

---

### Configuration

```json
// appsettings.json - Add to existing ContentRanking section
{
  "ContentRanking": {
    // ... existing Elo config ...
    
    "SearchAds": {
      "Enabled": true,
      "MaxAdsPerPage": 2,
      "AdSlotPositions": [3, 8],
      "MinQualityScore": 0.3,
      "MinBidPerClick": 0.05,
      "MaxBidPerClick": 10.00,
      "DefaultQualityScore": 0.5,
      "QualityScoreUpdateHours": 24,
      "FraudDetection": {
        "MaxClicksPerUserPerAdPerDay": 3,
        "MaxImpressionsPerUserPerAdPerHour": 10,
        "SuspiciousClickRateThreshold": 0.15
      }
    }
  }
}
```

---

## 💰 Profile Ad Budget System (Simplified)

> **Simpler approach**: Every profile has ad credits. When shown as sponsored, credits are deducted. Later: add payment to top-up.

### Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                     Profile Ad Budget Flow                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1. Profile has AdBudget = $50.00 (credits)                         │
│                      │                                               │
│                      ▼                                               │
│  2. Profile enables "Appear in Sponsored Results"                   │
│     - Sets max bid per click: $0.25                                 │
│     - Sets daily limit: $5.00                                       │
│                      │                                               │
│                      ▼                                               │
│  3. User searches → Profile shown as sponsored                      │
│     - Click happens → Deduct $0.25 from AdBudget                    │
│     - AdBudget now = $49.75                                         │
│                      │                                               │
│                      ▼                                               │
│  4. Budget reaches $0 → Stop showing as sponsored                   │
│                      │                                               │
│                      ▼                                               │
│  5. (FUTURE) Profile owner tops up budget via payment              │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Profile Entity Changes

```csharp
// Add to existing Profile entity (Sivar.Os.Shared/Entities/Profile.cs)

public class Profile : BaseEntity
{
    // ... existing properties ...
    
    // ========================================
    // AD BUDGET & SPONSORED SETTINGS
    // ========================================
    
    /// <summary>
    /// Available ad credit balance (in local currency, e.g., USD)
    /// </summary>
    public virtual decimal AdBudget { get; set; } = 0;
    
    /// <summary>
    /// Enable appearing as sponsored in search results
    /// </summary>
    public virtual bool SponsoredEnabled { get; set; } = false;
    
    /// <summary>
    /// Maximum amount willing to pay per click (CPC)
    /// </summary>
    public virtual decimal MaxBidPerClick { get; set; } = 0.10m;
    
    /// <summary>
    /// Maximum daily spend limit
    /// </summary>
    public virtual decimal DailyAdLimit { get; set; } = 5.00m;
    
    /// <summary>
    /// Amount spent today on ads
    /// </summary>
    public virtual decimal AdSpentToday { get; set; } = 0;
    
    /// <summary>
    /// Total amount ever spent on ads
    /// </summary>
    public virtual decimal TotalAdSpent { get; set; } = 0;
    
    /// <summary>
    /// Target categories for ads (empty = all searches in their category)
    /// </summary>
    public virtual string? AdTargetCategories { get; set; } // JSON array
    
    /// <summary>
    /// Target keywords for ads
    /// </summary>
    public virtual string? AdTargetKeywords { get; set; } // JSON array
    
    /// <summary>
    /// Target radius in km (0 = no geo restriction)
    /// </summary>
    public virtual double AdTargetRadiusKm { get; set; } = 0;
    
    // ========================================
    // AD PERFORMANCE STATS
    // ========================================
    
    /// <summary>
    /// Total sponsored impressions
    /// </summary>
    public virtual long SponsoredImpressions { get; set; } = 0;
    
    /// <summary>
    /// Total sponsored clicks
    /// </summary>
    public virtual long SponsoredClicks { get; set; } = 0;
    
    /// <summary>
    /// Click-through rate (clicks / impressions)
    /// </summary>
    public virtual double SponsoredCtr => SponsoredImpressions > 0 
        ? (double)SponsoredClicks / SponsoredImpressions 
        : 0;
    
    /// <summary>
    /// Quality score based on CTR (affects ad position)
    /// </summary>
    public virtual double AdQualityScore { get; set; } = 0.5;
}
```

### Simplified Ad Selection (Profile-Based)

```csharp
/// <summary>
/// Select profiles to show as sponsored results
/// </summary>
public class ProfileAdSelector
{
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<ProfileAdSelector> _logger;
    
    /// <summary>
    /// Get profiles eligible for sponsored placement
    /// </summary>
    public async Task<List<SponsoredProfileResult>> SelectSponsoredProfilesAsync(
        SearchContext context,
        List<Guid> organicProfileIds, // Exclude already-shown profiles
        int maxSponsored = 2)
    {
        var now = DateTimeOffset.UtcNow;
        
        // Get eligible sponsored profiles
        var eligibleProfiles = await _profileRepository.Query()
            .Where(p => p.SponsoredEnabled)
            .Where(p => p.AdBudget > 0)
            .Where(p => p.AdSpentToday < p.DailyAdLimit)
            .Where(p => !p.IsDeleted)
            .Where(p => !organicProfileIds.Contains(p.Id)) // Not already in results
            // Category match (if profile set targeting)
            .Where(p => string.IsNullOrEmpty(p.AdTargetCategories) || 
                       p.AdTargetCategories.Contains(context.Category))
            .ToListAsync();
        
        if (!eligibleProfiles.Any())
        {
            return new List<SponsoredProfileResult>();
        }
        
        // Filter by location if applicable
        if (context.Latitude != 0 && context.Longitude != 0)
        {
            eligibleProfiles = eligibleProfiles
                .Where(p => p.AdTargetRadiusKm == 0 || // No geo restriction
                           (p.Latitude.HasValue && p.Longitude.HasValue &&
                            CalculateDistance(p.Latitude.Value, p.Longitude.Value,
                                            context.Latitude, context.Longitude) <= p.AdTargetRadiusKm))
                .ToList();
        }
        
        // Filter by keyword match
        if (!string.IsNullOrEmpty(context.Query))
        {
            eligibleProfiles = eligibleProfiles
                .Where(p => string.IsNullOrEmpty(p.AdTargetKeywords) || // No keyword restriction
                           MatchesKeywords(p.AdTargetKeywords, context.Query))
                .ToList();
        }
        
        // Rank by: Bid × QualityScore
        var rankedProfiles = eligibleProfiles
            .Select(p => new
            {
                Profile = p,
                AdRank = (double)p.MaxBidPerClick * p.AdQualityScore,
                RelevanceScore = CalculateRelevance(p, context)
            })
            .Where(x => x.AdRank > 0)
            .OrderByDescending(x => x.AdRank * x.RelevanceScore)
            .Take(maxSponsored)
            .ToList();
        
        // Calculate actual price (second-price auction)
        var results = new List<SponsoredProfileResult>();
        for (int i = 0; i < rankedProfiles.Count; i++)
        {
            var current = rankedProfiles[i];
            
            // Second-price: pay enough to beat next bidder + $0.01
            decimal actualPrice;
            if (i + 1 < rankedProfiles.Count)
            {
                var next = rankedProfiles[i + 1];
                actualPrice = (decimal)(next.AdRank / current.Profile.AdQualityScore) + 0.01m;
            }
            else
            {
                actualPrice = 0.01m; // Minimum price if no competition
            }
            
            // Cap at their max bid
            actualPrice = Math.Min(actualPrice, current.Profile.MaxBidPerClick);
            
            results.Add(new SponsoredProfileResult
            {
                ProfileId = current.Profile.Id,
                Profile = current.Profile,
                AdRank = current.AdRank,
                ActualPricePerClick = actualPrice,
                Position = 0 // Assigned during interleaving
            });
        }
        
        _logger.LogInformation(
            "[ProfileAdSelector] Selected {Count} sponsored from {Eligible} eligible",
            results.Count, eligibleProfiles.Count);
        
        return results;
    }
    
    private bool MatchesKeywords(string keywordsJson, string query)
    {
        try
        {
            var keywords = JsonSerializer.Deserialize<List<string>>(keywordsJson) ?? new();
            return keywords.Any(k => query.Contains(k, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return true; // On parse error, allow
        }
    }
}

public class SponsoredProfileResult
{
    public Guid ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;
    public double AdRank { get; set; }
    public decimal ActualPricePerClick { get; set; }
    public int Position { get; set; }
}
```

### Budget Deduction on Click

```csharp
/// <summary>
/// Service to handle ad budget transactions
/// </summary>
public class ProfileAdBudgetService
{
    private readonly IProfileRepository _profileRepository;
    private readonly IAdTransactionRepository _transactionRepository;
    private readonly ILogger<ProfileAdBudgetService> _logger;
    
    /// <summary>
    /// Record a sponsored impression (free, just for stats)
    /// </summary>
    public async Task RecordImpressionAsync(Guid profileId)
    {
        await _profileRepository.ExecuteRawSqlAsync(
            @"UPDATE ""Sivar_Profiles"" 
              SET ""SponsoredImpressions"" = ""SponsoredImpressions"" + 1
              WHERE ""Id"" = {0}",
            profileId);
    }
    
    /// <summary>
    /// Record a sponsored click and deduct budget
    /// </summary>
    public async Task<bool> RecordClickAsync(
        Guid profileId, 
        decimal amountToDeduct,
        Guid? clickerUserId = null)
    {
        // Use transaction for atomic update
        await using var transaction = await _profileRepository.BeginTransactionAsync();
        
        try
        {
            var profile = await _profileRepository.GetByIdAsync(profileId);
            if (profile == null)
            {
                return false;
            }
            
            // Check sufficient budget
            if (profile.AdBudget < amountToDeduct)
            {
                _logger.LogWarning(
                    "[AdBudget] Insufficient budget for profile {ProfileId}. Has {Budget}, needs {Amount}",
                    profileId, profile.AdBudget, amountToDeduct);
                return false;
            }
            
            // Check daily limit
            if (profile.AdSpentToday + amountToDeduct > profile.DailyAdLimit)
            {
                _logger.LogInformation(
                    "[AdBudget] Daily limit reached for profile {ProfileId}",
                    profileId);
                return false;
            }
            
            // Deduct budget
            profile.AdBudget -= amountToDeduct;
            profile.AdSpentToday += amountToDeduct;
            profile.TotalAdSpent += amountToDeduct;
            profile.SponsoredClicks++;
            
            // Recalculate CTR and quality score
            if (profile.SponsoredImpressions > 100)
            {
                var ctr = (double)profile.SponsoredClicks / profile.SponsoredImpressions;
                // Quality score: CTR relative to 2.5% average
                profile.AdQualityScore = Math.Clamp(ctr / 0.025 * 0.5 + 0.25, 0.1, 1.0);
            }
            
            await _profileRepository.SaveChangesAsync();
            
            // Record transaction for audit
            await _transactionRepository.AddAsync(new AdTransaction
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                TransactionType = AdTransactionType.Click,
                Amount = -amountToDeduct,
                BalanceAfter = profile.AdBudget,
                ClickerUserId = clickerUserId,
                Timestamp = DateTimeOffset.UtcNow,
                Description = "Sponsored click"
            });
            
            await transaction.CommitAsync();
            
            _logger.LogInformation(
                "[AdBudget] Click recorded. Profile={ProfileId}, Deducted={Amount}, NewBalance={Balance}",
                profileId, amountToDeduct, profile.AdBudget);
            
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[AdBudget] Failed to record click for profile {ProfileId}", profileId);
            throw;
        }
    }
    
    /// <summary>
    /// Add budget to profile (for future: called after payment)
    /// </summary>
    public async Task<decimal> AddBudgetAsync(
        Guid profileId, 
        decimal amount,
        string source = "manual")
    {
        var profile = await _profileRepository.GetByIdAsync(profileId);
        if (profile == null)
        {
            throw new InvalidOperationException($"Profile {profileId} not found");
        }
        
        profile.AdBudget += amount;
        await _profileRepository.SaveChangesAsync();
        
        // Record transaction
        await _transactionRepository.AddAsync(new AdTransaction
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            TransactionType = AdTransactionType.TopUp,
            Amount = amount,
            BalanceAfter = profile.AdBudget,
            Timestamp = DateTimeOffset.UtcNow,
            Description = $"Budget top-up via {source}"
        });
        
        _logger.LogInformation(
            "[AdBudget] Budget added. Profile={ProfileId}, Added={Amount}, NewBalance={Balance}",
            profileId, amount, profile.AdBudget);
        
        return profile.AdBudget;
    }
    
    /// <summary>
    /// Daily job: Reset daily spend counters
    /// </summary>
    public async Task ResetDailySpendAsync()
    {
        await _profileRepository.ExecuteRawSqlAsync(
            @"UPDATE ""Sivar_Profiles"" SET ""AdSpentToday"" = 0 WHERE ""SponsoredEnabled"" = true");
    }
}

/// <summary>
/// Transaction record for ad budget changes
/// </summary>
public class AdTransaction : BaseEntity
{
    public virtual Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;
    
    public virtual AdTransactionType TransactionType { get; set; }
    
    /// <summary>
    /// Positive = top-up, Negative = spend
    /// </summary>
    public virtual decimal Amount { get; set; }
    
    /// <summary>
    /// Balance after this transaction
    /// </summary>
    public virtual decimal BalanceAfter { get; set; }
    
    /// <summary>
    /// User who clicked (for click transactions)
    /// </summary>
    public virtual Guid? ClickerUserId { get; set; }
    
    public virtual DateTimeOffset Timestamp { get; set; }
    
    [StringLength(500)]
    public virtual string? Description { get; set; }
    
    /// <summary>
    /// External reference (payment ID, etc.)
    /// </summary>
    [StringLength(100)]
    public virtual string? ExternalReference { get; set; }
}

public enum AdTransactionType
{
    TopUp = 1,      // Budget added
    Click = 2,      // Click deduction
    Refund = 3,     // Refund (fraud, etc.)
    Adjustment = 4, // Manual adjustment
    Bonus = 5       // Promotional credit
}
```

### Sponsored Settings UI

```razor
@* SponsoredSettingsPanel.razor - In profile settings *@

<MudPaper Class="pa-4 mb-4">
    <MudText Typo="Typo.h6" Class="mb-3">
        <MudIcon Icon="@Icons.Material.Filled.Campaign" Class="mr-2" />
        @Localizer["SponsoredSettings"]
    </MudText>
    
    @* Current Balance *@
    <MudAlert Severity="@(Profile.AdBudget > 0 ? Severity.Success : Severity.Warning)" 
              Class="mb-4">
        <MudText Typo="Typo.h5">
            @Localizer["AdBalance"]: @Profile.AdBudget.ToString("C2")
        </MudText>
        @if (Profile.AdBudget <= 5)
        {
            <MudText Typo="Typo.caption">
                @Localizer["LowBalanceWarning"]
            </MudText>
        }
    </MudAlert>
    
    @* Enable/Disable Toggle *@
    <MudSwitch @bind-Value="Profile.SponsoredEnabled" 
               Label="@Localizer["EnableSponsored"]"
               Color="Color.Primary"
               Disabled="@(Profile.AdBudget <= 0)" />
    
    @if (Profile.SponsoredEnabled)
    {
        <MudDivider Class="my-4" />
        
        @* Bid Settings *@
        <MudGrid>
            <MudItem xs="12" sm="6">
                <MudNumericField @bind-Value="Profile.MaxBidPerClick"
                                 Label="@Localizer["MaxBidPerClick"]"
                                 Adornment="Adornment.Start"
                                 AdornmentText="$"
                                 Min="0.01m" Max="10.00m" Step="0.05m"
                                 Variant="Variant.Outlined" />
            </MudItem>
            <MudItem xs="12" sm="6">
                <MudNumericField @bind-Value="Profile.DailyAdLimit"
                                 Label="@Localizer["DailyLimit"]"
                                 Adornment="Adornment.Start"
                                 AdornmentText="$"
                                 Min="1.00m" Max="1000.00m" Step="5.00m"
                                 Variant="Variant.Outlined" />
            </MudItem>
        </MudGrid>
        
        @* Targeting (Optional) *@
        <MudExpansionPanels Class="mt-4">
            <MudExpansionPanel Text="@Localizer["AdvancedTargeting"]">
                <MudTextField @bind-Value="_targetKeywords"
                              Label="@Localizer["TargetKeywords"]"
                              Placeholder="pizza, restaurante, comida..."
                              HelperText="@Localizer["TargetKeywordsHelp"]"
                              Variant="Variant.Outlined" />
                              
                <MudNumericField @bind-Value="Profile.AdTargetRadiusKm"
                                 Label="@Localizer["TargetRadius"]"
                                 Adornment="Adornment.End"
                                 AdornmentText="km"
                                 Min="0" Max="100"
                                 HelperText="@Localizer["TargetRadiusHelp"]"
                                 Variant="Variant.Outlined" 
                                 Class="mt-3" />
            </MudExpansionPanel>
        </MudExpansionPanels>
        
        @* Performance Stats *@
        <MudDivider Class="my-4" />
        <MudText Typo="Typo.subtitle2" Class="mb-2">@Localizer["Performance"]</MudText>
        
        <MudGrid>
            <MudItem xs="6" sm="3">
                <MudPaper Class="pa-3 text-center" Elevation="0" Style="background: var(--mud-palette-surface)">
                    <MudText Typo="Typo.h6">@Profile.SponsoredImpressions</MudText>
                    <MudText Typo="Typo.caption">@Localizer["Impressions"]</MudText>
                </MudPaper>
            </MudItem>
            <MudItem xs="6" sm="3">
                <MudPaper Class="pa-3 text-center" Elevation="0" Style="background: var(--mud-palette-surface)">
                    <MudText Typo="Typo.h6">@Profile.SponsoredClicks</MudText>
                    <MudText Typo="Typo.caption">@Localizer["Clicks"]</MudText>
                </MudPaper>
            </MudItem>
            <MudItem xs="6" sm="3">
                <MudPaper Class="pa-3 text-center" Elevation="0" Style="background: var(--mud-palette-surface)">
                    <MudText Typo="Typo.h6">@((Profile.SponsoredCtr * 100).ToString("F2"))%</MudText>
                    <MudText Typo="Typo.caption">CTR</MudText>
                </MudPaper>
            </MudItem>
            <MudItem xs="6" sm="3">
                <MudPaper Class="pa-3 text-center" Elevation="0" Style="background: var(--mud-palette-surface)">
                    <MudText Typo="Typo.h6">@Profile.TotalAdSpent.ToString("C2")</MudText>
                    <MudText Typo="Typo.caption">@Localizer["TotalSpent"]</MudText>
                </MudPaper>
            </MudItem>
        </MudGrid>
    }
    
    @* Top-up Button (Future: Links to payment) *@
    <MudDivider Class="my-4" />
    <MudButton Variant="Variant.Filled" 
               Color="Color.Primary" 
               StartIcon="@Icons.Material.Filled.AddCard"
               OnClick="HandleTopUp"
               FullWidth="true">
        @Localizer["TopUpBalance"]
    </MudButton>
</MudPaper>

@code {
    [Parameter] public Profile Profile { get; set; } = null!;
    [Parameter] public EventCallback<Profile> OnSave { get; set; }
    
    private string _targetKeywords = "";
    
    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(Profile.AdTargetKeywords))
        {
            try {
                var keywords = JsonSerializer.Deserialize<List<string>>(Profile.AdTargetKeywords);
                _targetKeywords = string.Join(", ", keywords ?? new());
            } catch { }
        }
    }
    
    private async Task HandleTopUp()
    {
        // TODO: Navigate to payment page or show payment dialog
        // For now: Show coming soon message
        await DialogService.ShowMessageBox(
            Localizer["TopUpBalance"],
            Localizer["TopUpComingSoon"]);
    }
}
```

### Future: Payment Integration

```csharp
/// <summary>
/// FUTURE: Payment service for ad budget top-ups
/// Options: Stripe, PayPal, local payment methods
/// </summary>
public interface IAdPaymentService
{
    /// <summary>
    /// Create a checkout session for budget top-up
    /// </summary>
    Task<PaymentSession> CreateTopUpSessionAsync(
        Guid profileId,
        decimal amount,
        string currency = "USD");
    
    /// <summary>
    /// Handle webhook from payment provider
    /// </summary>
    Task HandlePaymentWebhookAsync(string payload, string signature);
    
    /// <summary>
    /// Process successful payment (called by webhook handler)
    /// </summary>
    Task ProcessSuccessfulPaymentAsync(
        Guid profileId,
        decimal amount,
        string paymentId);
}

// Pricing tiers example:
// $10 → Get $10 ad credit
// $25 → Get $27 ad credit (8% bonus)
// $50 → Get $57 ad credit (14% bonus)
// $100 → Get $120 ad credit (20% bonus)
```

### Summary: Profile Ad Budget vs Full Ad System

| Aspect | Profile Budget (Simpler) | Full Ad System |
|--------|-------------------------|----------------|
| **Who can advertise** | Any profile | Creates separate ad campaigns |
| **Ad content** | Profile itself | Custom ads (headline, image, etc.) |
| **Setup complexity** | Toggle + set bid | Full campaign creation |
| **Targeting** | Basic (keywords, radius) | Advanced (demographics, interests) |
| **Billing** | Per-click from balance | Invoicing, credit cards, etc. |
| **Best for** | Small businesses | Large advertisers |

### Implementation Priority

**Phase 1: Profile Budget (This Design)**
1. Add fields to Profile entity
2. Implement ProfileAdSelector
3. Implement ProfileAdBudgetService
4. Add SponsoredSettingsPanel UI
5. Integrate with search results

**Phase 2: Payment (Future)**
1. Choose payment provider (Stripe recommended)
2. Implement checkout flow
3. Handle webhooks
4. Add transaction history UI

---

### Files to Create for Ads

| File | Purpose |
|------|---------|
| `Sivar.Os.Shared/Entities/SearchAd.cs` | Ad entity |
| `Sivar.Os.Shared/Entities/AdCampaign.cs` | Campaign entity |
| `Sivar.Os.Shared/Entities/AdImpressionEvent.cs` | Impression tracking |
| `Sivar.Os.Shared/Entities/AdClickEvent.cs` | Click tracking |
| `Sivar.Os.Shared/DTOs/SearchAdDto.cs` | Ad DTO for frontend |
| `Sivar.Os.Shared/Repositories/ISearchAdRepository.cs` | Repository interface |
| `Sivar.Os.Data/Repositories/SearchAdRepository.cs` | Repository impl |
| `Sivar.Os/Services/SearchAdSelector.cs` | Ad selection logic |
| `Sivar.Os/Services/SearchAdTrackingService.cs` | Impression/click tracking |
| `Sivar.Os/Services/SearchResultInterleaver.cs` | Combine organic + ads |
| `Sivar.Os.Client/Components/AIChat/SearchAdCard.razor` | Ad display component |

---

### Integration with Content Ranking

The ad system works **alongside** content ranking:

```
User searches "pizzerias cerca de mi"
            │
            ▼
┌───────────────────────────────────────┐
│ 1. Hybrid Search (semantic + geo)     │
│    → Get 20 organic results           │
└───────────────────────────────────────┘
            │
            ▼
┌───────────────────────────────────────┐
│ 2. Content Ranking (Elo + Composite)  │
│    → Rank organic results by quality  │
└───────────────────────────────────────┘
            │
            ▼
┌───────────────────────────────────────┐
│ 3. Ad Selection (Auction)             │
│    → Select 0-2 ads matching context  │
└───────────────────────────────────────┘
            │
            ▼
┌───────────────────────────────────────┐
│ 4. Interleaving                       │
│    → Insert ads at positions 3 & 8    │
└───────────────────────────────────────┘
            │
            ▼
┌───────────────────────────────────────┐
│ 5. Tracking                           │
│    → Record impressions for:          │
│       - Organic (ContentRatingEvent)  │
│       - Ads (AdImpressionEvent)       │
└───────────────────────────────────────┘
            │
            ▼
       Return to user
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
