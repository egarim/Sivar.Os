# Feed System Implementation Plan
**Project**: Sivar.Os  
**Created**: October 31, 2025  
**Complexity**: ⭐⭐⭐⭐⭐ Very High  
**Estimated Timeline**: 6-8 weeks

---

## Executive Summary

Building a sophisticated **algorithmic feed system** with:
- **Algorithmic ranking** (relevance-based, not chronological)
- **Multiple content sources** (global trending, groups/communities, hashtags)
- **Native ads** (seamlessly integrated) + video ads (future)
- **Advanced targeting** (demographics, interests, behavioral)

This leverages existing PostgreSQL optimizations (pgvector, TimescaleDB, continuous aggregates) to deliver personalized, high-performance feeds at scale.

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    User Feed Request                         │
│                  GET /api/feed?page=1                        │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                    FeedService                               │
│              (Feed Composition & Ranking)                    │
└────────────────────────┬────────────────────────────────────┘
                         │
         ┌───────────────┼───────────────┬──────────────┐
         ↓               ↓               ↓              ↓
    ┌─────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ Organic │   │ Trending │   │  Groups  │   │   Ads    │
    │  Posts  │   │  Posts   │   │  Posts   │   │ Campaigns│
    └─────────┘   └──────────┘   └──────────┘   └──────────┘
         │               │               │              │
         └───────────────┴───────────────┴──────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                  Ranking Algorithm                           │
│   - User Interest Vector (pgvector)                         │
│   - Engagement Score (continuous aggregates)                │
│   - Recency Score (time decay)                              │
│   - Diversity Score (content mix)                           │
│   - Ad Injection (strategic placement)                      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                  Feed Cache (TimescaleDB)                    │
│              7-day retention, 1-hour expiry                  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                  Blazor Component                            │
│           Infinite scroll, lazy loading, ads                 │
└─────────────────────────────────────────────────────────────┘
```

---

## Phase 1: Database Schema (Week 1)

### New Tables

#### 1.1 User Follows & Social Graph

```sql
-- =====================================================
-- User Follows (Social Graph)
-- =====================================================
CREATE TABLE "Sivar_UserFollows" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "FollowerId" uuid NOT NULL,
    "FolloweeId" uuid NOT NULL,
    "CreatedAt" timestamp NOT NULL DEFAULT NOW(),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "UpdatedAt" timestamp NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_UserFollows_Follower" FOREIGN KEY ("FollowerId") 
        REFERENCES "Sivar_Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserFollows_Followee" FOREIGN KEY ("FolloweeId") 
        REFERENCES "Sivar_Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "UK_UserFollows_Follower_Followee" UNIQUE ("FollowerId", "FolloweeId")
);

CREATE INDEX "IX_UserFollows_Follower" ON "Sivar_UserFollows" ("FollowerId") WHERE NOT "IsDeleted";
CREATE INDEX "IX_UserFollows_Followee" ON "Sivar_UserFollows" ("FolloweeId") WHERE NOT "IsDeleted";
CREATE INDEX "IX_UserFollows_CreatedAt" ON "Sivar_UserFollows" ("CreatedAt" DESC);
```

#### 1.2 Groups/Communities

```sql
-- =====================================================
-- Groups (Communities)
-- =====================================================
CREATE TABLE "Sivar_Groups" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" text NOT NULL,
    "Description" text,
    "CreatorId" uuid NOT NULL,
    "CoverImageUrl" text,
    "Privacy" varchar(50) NOT NULL DEFAULT 'Public', -- Public, Private, Secret
    "MemberCount" int NOT NULL DEFAULT 0,
    "PostCount" int NOT NULL DEFAULT 0,
    "Tags" text[] NOT NULL DEFAULT '{}',
    "Metadata" jsonb,
    "CreatedAt" timestamp NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp NOT NULL DEFAULT NOW(),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    
    CONSTRAINT "FK_Groups_Creator" FOREIGN KEY ("CreatorId") 
        REFERENCES "Sivar_Users"("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Groups_Creator" ON "Sivar_Groups" ("CreatorId");
CREATE INDEX "IX_Groups_Privacy" ON "Sivar_Groups" ("Privacy") WHERE NOT "IsDeleted";
CREATE INDEX "IX_Groups_Tags_Gin" ON "Sivar_Groups" USING gin ("Tags");
CREATE INDEX "IX_Groups_MemberCount" ON "Sivar_Groups" ("MemberCount" DESC) WHERE NOT "IsDeleted";

-- Full-text search on group names/descriptions
ALTER TABLE "Sivar_Groups" ADD COLUMN "SearchVector" tsvector;
CREATE INDEX "IX_Groups_SearchVector" ON "Sivar_Groups" USING gin ("SearchVector");

-- =====================================================
-- Group Memberships
-- =====================================================
CREATE TABLE "Sivar_GroupMembers" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "GroupId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" varchar(50) NOT NULL DEFAULT 'Member', -- Admin, Moderator, Member
    "JoinedAt" timestamp NOT NULL DEFAULT NOW(),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    
    CONSTRAINT "FK_GroupMembers_Group" FOREIGN KEY ("GroupId") 
        REFERENCES "Sivar_Groups"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_GroupMembers_User" FOREIGN KEY ("UserId") 
        REFERENCES "Sivar_Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "UK_GroupMembers_Group_User" UNIQUE ("GroupId", "UserId")
);

CREATE INDEX "IX_GroupMembers_Group" ON "Sivar_GroupMembers" ("GroupId") WHERE NOT "IsDeleted";
CREATE INDEX "IX_GroupMembers_User" ON "Sivar_GroupMembers" ("UserId") WHERE NOT "IsDeleted";

-- =====================================================
-- Link Posts to Groups
-- =====================================================
ALTER TABLE "Sivar_Posts" ADD COLUMN "GroupId" uuid;
ALTER TABLE "Sivar_Posts" ADD CONSTRAINT "FK_Posts_Group" 
    FOREIGN KEY ("GroupId") REFERENCES "Sivar_Groups"("Id") ON DELETE SET NULL;

CREATE INDEX "IX_Posts_Group" ON "Sivar_Posts" ("GroupId") WHERE NOT "IsDeleted";
```

#### 1.3 Hashtags & Trending

```sql
-- =====================================================
-- Hashtags
-- =====================================================
CREATE TABLE "Sivar_Hashtags" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Tag" varchar(100) NOT NULL UNIQUE,
    "UsageCount" bigint NOT NULL DEFAULT 0,
    "TrendingScore" decimal NOT NULL DEFAULT 0,
    "CreatedAt" timestamp NOT NULL DEFAULT NOW(),
    "LastUsedAt" timestamp NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_Hashtags_Tag" ON "Sivar_Hashtags" ("Tag");
CREATE INDEX "IX_Hashtags_TrendingScore" ON "Sivar_Hashtags" ("TrendingScore" DESC);
CREATE INDEX "IX_Hashtags_LastUsedAt" ON "Sivar_Hashtags" ("LastUsedAt" DESC);

-- =====================================================
-- Post Hashtags (Many-to-Many)
-- =====================================================
CREATE TABLE "Sivar_PostHashtags" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "PostId" uuid NOT NULL,
    "HashtagId" uuid NOT NULL,
    "CreatedAt" timestamp NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_PostHashtags_Post" FOREIGN KEY ("PostId") 
        REFERENCES "Sivar_Posts"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PostHashtags_Hashtag" FOREIGN KEY ("HashtagId") 
        REFERENCES "Sivar_Hashtags"("Id") ON DELETE CASCADE,
    CONSTRAINT "UK_PostHashtags_Post_Hashtag" UNIQUE ("PostId", "HashtagId")
);

CREATE INDEX "IX_PostHashtags_Post" ON "Sivar_PostHashtags" ("PostId");
CREATE INDEX "IX_PostHashtags_Hashtag" ON "Sivar_PostHashtags" ("HashtagId");
```

#### 1.4 Feed Cache (TimescaleDB Hypertable)

```sql
-- =====================================================
-- Feed Cache (Pre-computed feeds)
-- =====================================================
CREATE TABLE "Sivar_FeedCache" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "PostId" uuid NOT NULL,
    "Score" decimal NOT NULL,
    "Rank" int NOT NULL,
    "FeedType" varchar(50) NOT NULL, -- 'organic', 'trending', 'group', 'suggested'
    "Metadata" jsonb,
    "GeneratedAt" timestamp NOT NULL DEFAULT NOW(),
    "ExpiresAt" timestamp NOT NULL,
    
    PRIMARY KEY ("Id", "GeneratedAt"),
    CONSTRAINT "FK_FeedCache_User" FOREIGN KEY ("UserId") 
        REFERENCES "Sivar_Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_FeedCache_Post" FOREIGN KEY ("PostId") 
        REFERENCES "Sivar_Posts"("Id") ON DELETE CASCADE
);

-- Convert to TimescaleDB hypertable
SELECT create_hypertable('Sivar_FeedCache', 'GeneratedAt', 
    chunk_time_interval => INTERVAL '1 day');

-- Auto-delete expired cache entries (7 days retention)
SELECT add_retention_policy('Sivar_FeedCache', INTERVAL '7 days');

-- Indexes for fast queries
CREATE INDEX "IX_FeedCache_User_GeneratedAt" ON "Sivar_FeedCache" ("UserId", "GeneratedAt" DESC);
CREATE INDEX "IX_FeedCache_User_Rank" ON "Sivar_FeedCache" ("UserId", "Rank");
CREATE INDEX "IX_FeedCache_ExpiresAt" ON "Sivar_FeedCache" ("ExpiresAt");
```

#### 1.5 Ad System

```sql
-- =====================================================
-- Ad Campaigns
-- =====================================================
CREATE TABLE "Sivar_AdCampaigns" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "AdvertiserId" uuid NOT NULL,
    "Title" text NOT NULL,
    "Content" text NOT NULL,
    "MediaUrl" text, -- Image or video URL
    "MediaType" varchar(50), -- 'image', 'video'
    "CallToActionUrl" text,
    "CallToActionText" varchar(100),
    
    -- Targeting
    "TargetDemographics" jsonb, -- { "ageMin": 18, "ageMax": 65, "gender": ["male", "female"], "locations": ["USA", "CA"] }
    "TargetInterests" text[], -- ["technology", "gaming", "sports"]
    "TargetBehaviors" jsonb, -- { "engagementLevel": "high", "activeHours": [9, 17] }
    
    -- Budget & Scheduling
    "Budget" decimal,
    "SpentBudget" decimal NOT NULL DEFAULT 0,
    "CostPerImpression" decimal,
    "CostPerClick" decimal,
    "StartDate" timestamp NOT NULL,
    "EndDate" timestamp NOT NULL,
    
    -- Status
    "Status" varchar(50) NOT NULL DEFAULT 'Draft', -- Draft, Active, Paused, Completed
    "IsActive" boolean NOT NULL DEFAULT false,
    
    "CreatedAt" timestamp NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp NOT NULL DEFAULT NOW(),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    
    CONSTRAINT "FK_AdCampaigns_Advertiser" FOREIGN KEY ("AdvertiserId") 
        REFERENCES "Sivar_Users"("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_AdCampaigns_Advertiser" ON "Sivar_AdCampaigns" ("AdvertiserId");
CREATE INDEX "IX_AdCampaigns_Status" ON "Sivar_AdCampaigns" ("Status") WHERE "IsActive";
CREATE INDEX "IX_AdCampaigns_StartEnd" ON "Sivar_AdCampaigns" ("StartDate", "EndDate");
CREATE INDEX "IX_AdCampaigns_TargetInterests_Gin" ON "Sivar_AdCampaigns" USING gin ("TargetInterests");

-- =====================================================
-- Ad Impressions (Analytics) - TimescaleDB
-- =====================================================
CREATE TABLE "Sivar_AdImpressions" (
    "Id" uuid NOT NULL,
    "AdId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ViewedAt" timestamp NOT NULL DEFAULT NOW(),
    "Clicked" boolean NOT NULL DEFAULT false,
    "ClickedAt" timestamp,
    "DurationMs" int, -- How long user viewed ad
    "Position" int, -- Position in feed
    
    PRIMARY KEY ("Id", "ViewedAt"),
    CONSTRAINT "FK_AdImpressions_Ad" FOREIGN KEY ("AdId") 
        REFERENCES "Sivar_AdCampaigns"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AdImpressions_User" FOREIGN KEY ("UserId") 
        REFERENCES "Sivar_Users"("Id") ON DELETE CASCADE
);

-- Convert to TimescaleDB hypertable
SELECT create_hypertable('Sivar_AdImpressions', 'ViewedAt', 
    chunk_time_interval => INTERVAL '7 days');

-- Auto-delete old impressions (90 days retention)
SELECT add_retention_policy('Sivar_AdImpressions', INTERVAL '90 days');

-- Compression (after 30 days)
ALTER TABLE "Sivar_AdImpressions" SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'AdId',
    timescaledb.compress_orderby = 'ViewedAt DESC'
);
SELECT add_compression_policy('Sivar_AdImpressions', INTERVAL '30 days');

CREATE INDEX "IX_AdImpressions_Ad_ViewedAt" ON "Sivar_AdImpressions" ("AdId", "ViewedAt" DESC);
CREATE INDEX "IX_AdImpressions_User" ON "Sivar_AdImpressions" ("UserId");
CREATE INDEX "IX_AdImpressions_Clicked" ON "Sivar_AdImpressions" ("Clicked") WHERE "Clicked";
```

#### 1.6 User Interests Profile

```sql
-- =====================================================
-- User Interest Profile (for targeting & recommendations)
-- =====================================================
CREATE TABLE "Sivar_UserInterests" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "InterestVector" vector(384), -- pgvector for semantic matching
    "TopicTags" text[] NOT NULL DEFAULT '{}',
    "InteractionHistory" jsonb, -- { "liked": [...], "commented": [...], "shared": [...] }
    "EngagementLevel" varchar(50), -- 'low', 'medium', 'high', 'power_user'
    "ActiveHours" int[], -- [9, 10, 11, ..., 17] (hours they're active)
    "UpdatedAt" timestamp NOT NULL DEFAULT NOW(),
    
    CONSTRAINT "FK_UserInterests_User" FOREIGN KEY ("UserId") 
        REFERENCES "Sivar_Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "UK_UserInterests_User" UNIQUE ("UserId")
);

CREATE INDEX "IX_UserInterests_User" ON "Sivar_UserInterests" ("UserId");
CREATE INDEX "IX_UserInterests_InterestVector_Hnsw" ON "Sivar_UserInterests" 
    USING hnsw ("InterestVector" vector_cosine_ops);
CREATE INDEX "IX_UserInterests_TopicTags_Gin" ON "Sivar_UserInterests" 
    USING gin ("TopicTags");
```

---

## Phase 2: Continuous Aggregates for Trending & Analytics (Week 2)

### 2.1 Trending Posts Aggregate

```sql
-- =====================================================
-- Trending Posts (Hourly aggregation)
-- =====================================================
CREATE MATERIALIZED VIEW trending_posts_hourly
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 hour', p."CreatedAt") AS hour,
    p."Id" as "PostId",
    p."AuthorKey",
    p."PostType",
    COUNT(DISTINCT r."Id") as reaction_count,
    COUNT(DISTINCT c."Id") as comment_count,
    COUNT(DISTINCT s."Id") as share_count,
    (
        COUNT(DISTINCT r."Id") * 1.0 +
        COUNT(DISTINCT c."Id") * 2.0 +
        COUNT(DISTINCT s."Id") * 3.0
    ) as engagement_score,
    (
        COUNT(DISTINCT r."Id") * 1.0 +
        COUNT(DISTINCT c."Id") * 2.0 +
        COUNT(DISTINCT s."Id") * 3.0
    ) / EXTRACT(EPOCH FROM (NOW() - p."CreatedAt")) * 3600 as trending_score
FROM "Sivar_Posts" p
LEFT JOIN "Sivar_Reactions" r ON p."Id" = r."PostId" AND NOT r."IsDeleted"
LEFT JOIN "Sivar_Comments" c ON p."Id" = c."PostId" AND NOT c."IsDeleted"
LEFT JOIN "Sivar_Activities" s ON p."Id"::text = s."ObjectId" 
    AND s."Verb" = 'Share' AND NOT s."IsDeleted"
WHERE NOT p."IsDeleted"
  AND p."CreatedAt" >= NOW() - INTERVAL '7 days'
GROUP BY hour, p."Id", p."AuthorKey", p."PostType";

-- Refresh every hour
SELECT add_continuous_aggregate_policy('trending_posts_hourly',
    start_offset => INTERVAL '7 days',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour'
);

-- Index for fast trending queries
CREATE INDEX idx_trending_posts_trending_score 
ON trending_posts_hourly (trending_score DESC);
CREATE INDEX idx_trending_posts_hour 
ON trending_posts_hourly (hour DESC);
```

### 2.2 Hashtag Trending Aggregate

```sql
-- =====================================================
-- Trending Hashtags (Hourly)
-- =====================================================
CREATE MATERIALIZED VIEW trending_hashtags_hourly
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 hour', ph."CreatedAt") AS hour,
    h."Id" as "HashtagId",
    h."Tag",
    COUNT(DISTINCT ph."PostId") as post_count,
    COUNT(DISTINCT p."AuthorKey") as unique_authors,
    SUM(p."ViewCount") as total_views,
    (
        COUNT(DISTINCT ph."PostId") * LOG(COUNT(DISTINCT p."AuthorKey") + 1)
    ) / EXTRACT(EPOCH FROM (NOW() - MAX(ph."CreatedAt"))) * 3600 as trending_score
FROM "Sivar_PostHashtags" ph
JOIN "Sivar_Hashtags" h ON ph."HashtagId" = h."Id"
JOIN "Sivar_Posts" p ON ph."PostId" = p."Id" AND NOT p."IsDeleted"
WHERE ph."CreatedAt" >= NOW() - INTERVAL '24 hours'
GROUP BY hour, h."Id", h."Tag";

SELECT add_continuous_aggregate_policy('trending_hashtags_hourly',
    start_offset => INTERVAL '24 hours',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour'
);

CREATE INDEX idx_trending_hashtags_score 
ON trending_hashtags_hourly (trending_score DESC);
```

### 2.3 Group Activity Aggregate

```sql
-- =====================================================
-- Group Activity Metrics (Daily)
-- =====================================================
CREATE MATERIALIZED VIEW group_activity_daily
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 day', p."CreatedAt") AS day,
    g."Id" as "GroupId",
    g."Name" as "GroupName",
    COUNT(DISTINCT p."Id") as post_count,
    COUNT(DISTINCT p."AuthorKey") as active_members,
    SUM(p."ViewCount") as total_views,
    AVG(p."ViewCount") as avg_views_per_post
FROM "Sivar_Groups" g
JOIN "Sivar_Posts" p ON g."Id" = p."GroupId" AND NOT p."IsDeleted"
WHERE NOT g."IsDeleted"
  AND p."CreatedAt" >= NOW() - INTERVAL '30 days'
GROUP BY day, g."Id", g."Name";

SELECT add_continuous_aggregate_policy('group_activity_daily',
    start_offset => INTERVAL '30 days',
    end_offset => INTERVAL '1 day',
    schedule_interval => INTERVAL '1 day'
);
```

---

## Phase 3: Feed Ranking Algorithm (Week 3-4)

### 3.1 Scoring Components

```csharp
public class FeedRankingService
{
    private readonly IPostRepository _postRepository;
    private readonly IUserInterestsRepository _userInterestsRepository;
    private readonly IVectorEmbeddingService _vectorService;
    private readonly ILogger<FeedRankingService> _logger;

    public async Task<List<ScoredFeedItem>> RankFeedItemsAsync(
        Guid userId,
        List<Post> candidatePosts)
    {
        var userProfile = await _userInterestsRepository.GetUserProfileAsync(userId);
        var scoredItems = new List<ScoredFeedItem>();

        foreach (var post in candidatePosts)
        {
            var score = await CalculateFeedScoreAsync(userId, post, userProfile);
            scoredItems.Add(new ScoredFeedItem
            {
                Post = post,
                TotalScore = score.TotalScore,
                RecencyScore = score.RecencyScore,
                EngagementScore = score.EngagementScore,
                RelevanceScore = score.RelevanceScore,
                DiversityScore = score.DiversityScore,
                SocialScore = score.SocialScore
            });
        }

        return scoredItems.OrderByDescending(x => x.TotalScore).ToList();
    }

    private async Task<FeedScore> CalculateFeedScoreAsync(
        Guid userId,
        Post post,
        UserInterestProfile userProfile)
    {
        // 1. Recency Score (time decay)
        var recencyScore = CalculateRecencyScore(post.CreatedAt);

        // 2. Engagement Score (likes, comments, shares)
        var engagementScore = CalculateEngagementScore(post);

        // 3. Relevance Score (semantic similarity using pgvector)
        var relevanceScore = await CalculateRelevanceScoreAsync(post, userProfile);

        // 4. Social Score (friends, followed users)
        var socialScore = await CalculateSocialScoreAsync(userId, post);

        // 5. Diversity Score (content variety)
        var diversityScore = CalculateDiversityScore(post);

        // Weighted combination
        var totalScore = 
            (recencyScore * 0.20) +
            (engagementScore * 0.25) +
            (relevanceScore * 0.30) +
            (socialScore * 0.15) +
            (diversityScore * 0.10);

        return new FeedScore
        {
            TotalScore = totalScore,
            RecencyScore = recencyScore,
            EngagementScore = engagementScore,
            RelevanceScore = relevanceScore,
            SocialScore = socialScore,
            DiversityScore = diversityScore
        };
    }

    /// <summary>
    /// Time decay function (exponential decay)
    /// Recent posts get higher scores
    /// </summary>
    private double CalculateRecencyScore(DateTime createdAt)
    {
        var hoursAgo = (DateTime.UtcNow - createdAt).TotalHours;
        
        // Exponential decay: score = e^(-λt)
        // Half-life of 12 hours (λ = ln(2)/12 ≈ 0.0578)
        var lambda = Math.Log(2) / 12;
        var score = Math.Exp(-lambda * hoursAgo);
        
        return Math.Max(0, Math.Min(1, score));
    }

    /// <summary>
    /// Engagement score based on likes, comments, shares
    /// Uses log scale to prevent viral posts from dominating
    /// </summary>
    private double CalculateEngagementScore(Post post)
    {
        var reactionWeight = 1.0;
        var commentWeight = 2.0;
        var shareWeight = 3.0;
        var viewWeight = 0.1;

        var engagementPoints = 
            (post.LikeCount * reactionWeight) +
            (post.CommentCount * commentWeight) +
            (post.ShareCount * shareWeight) +
            (post.ViewCount * viewWeight);

        // Log scale to prevent viral content from overwhelming feed
        var score = Math.Log10(engagementPoints + 1) / 4; // Normalize to 0-1 range

        return Math.Max(0, Math.Min(1, score));
    }

    /// <summary>
    /// Semantic relevance using pgvector cosine similarity
    /// Matches post content to user interests
    /// </summary>
    private async Task<double> CalculateRelevanceScoreAsync(
        Post post,
        UserInterestProfile userProfile)
    {
        if (string.IsNullOrEmpty(post.ContentEmbedding) || 
            userProfile?.InterestVector == null)
        {
            return 0.5; // Neutral score if no embeddings
        }

        // Calculate cosine similarity using PostgreSQL
        var similarity = await _postRepository.CalculateCosineSimilarityAsync(
            post.ContentEmbedding,
            userProfile.InterestVector
        );

        // Convert from [-1, 1] to [0, 1]
        var score = (similarity + 1) / 2;

        return Math.Max(0, Math.Min(1, score));
    }

    /// <summary>
    /// Social graph score (friends, followed users)
    /// Higher score for posts from users you follow
    /// </summary>
    private async Task<double> CalculateSocialScoreAsync(Guid userId, Post post)
    {
        // Check if post author is followed by user
        var isFollowing = await _userFollowsRepository.IsFollowingAsync(
            userId, 
            post.AuthorId
        );

        if (isFollowing)
        {
            return 1.0; // Max score for followed users
        }

        // Check mutual friends (second-degree connections)
        var mutualFriendsCount = await _userFollowsRepository.GetMutualFollowsCountAsync(
            userId,
            post.AuthorId
        );

        if (mutualFriendsCount > 0)
        {
            // Score based on mutual connections (log scale)
            var score = Math.Log10(mutualFriendsCount + 1) / 2;
            return Math.Max(0, Math.Min(0.7, score)); // Max 0.7 for indirect connections
        }

        return 0.3; // Low score for unknown users (still show some global content)
    }

    /// <summary>
    /// Diversity score to prevent filter bubbles
    /// Penalize content too similar to recently seen posts
    /// </summary>
    private double CalculateDiversityScore(Post post)
    {
        // This would compare against recently shown posts
        // For now, use post type diversity
        
        // TODO: Implement session-based diversity tracking
        // For now, slight randomization to ensure variety
        var random = new Random((int)(post.Id.GetHashCode() % 1000));
        var diversityBonus = random.NextDouble() * 0.2; // 0-0.2 random bonus

        return 0.5 + diversityBonus;
    }
}
```

### 3.2 Feed Composition Service

```csharp
public class FeedCompositionService
{
    private readonly IPostRepository _postRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IHashtagRepository _hashtagRepository;
    private readonly IAdCampaignRepository _adRepository;
    private readonly FeedRankingService _rankingService;
    private readonly ILogger<FeedCompositionService> _logger;

    public async Task<FeedResponse> GenerateFeedAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20)
    {
        var requestId = Guid.NewGuid().ToString("N");
        _logger.LogInformation(
            "[GenerateFeed] START - RequestId={RequestId}, UserId={UserId}, Page={Page}",
            requestId, userId, page);

        try
        {
            // 1. Gather candidate posts from multiple sources
            var candidates = await GatherCandidatePostsAsync(userId, page, pageSize);

            _logger.LogInformation(
                "[GenerateFeed] Gathered candidates - RequestId={RequestId}, Count={Count}",
                requestId, candidates.Count);

            // 2. Rank candidates using scoring algorithm
            var ranked = await _rankingService.RankFeedItemsAsync(userId, candidates);

            // 3. Take top N for this page
            var feedItems = ranked
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 4. Inject ads at strategic positions
            var feedWithAds = await InjectAdsAsync(userId, feedItems, requestId);

            // 5. Convert to DTOs
            var feedDtos = await MapToFeedDtosAsync(feedWithAds);

            _logger.LogInformation(
                "[GenerateFeed] SUCCESS - RequestId={RequestId}, Items={Count}, Ads={AdCount}",
                requestId, feedDtos.Count, feedDtos.Count(x => x.IsAd));

            return new FeedResponse
            {
                Items = feedDtos,
                Page = page,
                PageSize = pageSize,
                HasMore = ranked.Count > page * pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[GenerateFeed] FAILED - RequestId={RequestId}, UserId={UserId}",
                requestId, userId);
            throw;
        }
    }

    private async Task<List<Post>> GatherCandidatePostsAsync(
        Guid userId,
        int page,
        int pageSize)
    {
        // Gather 5x more candidates than needed for ranking
        var candidateCount = pageSize * 5;

        var tasks = new List<Task<List<Post>>>
        {
            // Source 1: Organic (followed users)
            GetOrganicPostsAsync(userId, candidateCount / 2),
            
            // Source 2: Trending (global)
            GetTrendingPostsAsync(candidateCount / 4),
            
            // Source 3: Groups (user's groups)
            GetGroupPostsAsync(userId, candidateCount / 4),
            
            // Source 4: Hashtags (user's interests)
            GetHashtagPostsAsync(userId, candidateCount / 4)
        };

        var results = await Task.WhenAll(tasks);
        
        // Combine and deduplicate
        var allPosts = results
            .SelectMany(x => x)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

        return allPosts;
    }

    /// <summary>
    /// Get posts from followed users (organic content)
    /// </summary>
    private async Task<List<Post>> GetOrganicPostsAsync(Guid userId, int limit)
    {
        var sql = @"
            SELECT p.*
            FROM ""Sivar_Posts"" p
            JOIN ""Sivar_UserFollows"" f ON p.""AuthorId"" = f.""FolloweeId""
            WHERE f.""FollowerId"" = {0}
              AND NOT p.""IsDeleted""
              AND NOT f.""IsDeleted""
              AND p.""CreatedAt"" >= NOW() - INTERVAL '7 days'
            ORDER BY p.""CreatedAt"" DESC
            LIMIT {1}";

        return await _postRepository.ExecuteRawSqlAsync(sql, userId, limit);
    }

    /// <summary>
    /// Get trending posts from continuous aggregate
    /// </summary>
    private async Task<List<Post>> GetTrendingPostsAsync(int limit)
    {
        var sql = @"
            SELECT p.*
            FROM ""Sivar_Posts"" p
            JOIN trending_posts_hourly t ON p.""Id"" = t.""PostId""
            WHERE NOT p.""IsDeleted""
              AND t.hour >= NOW() - INTERVAL '24 hours'
            ORDER BY t.trending_score DESC
            LIMIT {0}";

        return await _postRepository.ExecuteRawSqlAsync(sql, limit);
    }

    /// <summary>
    /// Get posts from groups user is member of
    /// </summary>
    private async Task<List<Post>> GetGroupPostsAsync(Guid userId, int limit)
    {
        var sql = @"
            SELECT p.*
            FROM ""Sivar_Posts"" p
            JOIN ""Sivar_GroupMembers"" gm ON p.""GroupId"" = gm.""GroupId""
            WHERE gm.""UserId"" = {0}
              AND NOT p.""IsDeleted""
              AND NOT gm.""IsDeleted""
              AND p.""CreatedAt"" >= NOW() - INTERVAL '7 days'
            ORDER BY p.""CreatedAt"" DESC
            LIMIT {1}";

        return await _postRepository.ExecuteRawSqlAsync(sql, userId, limit);
    }

    /// <summary>
    /// Get posts by hashtags user is interested in
    /// </summary>
    private async Task<List<Post>> GetHashtagPostsAsync(Guid userId, int limit)
    {
        // Get user's interest hashtags
        var userInterests = await _userInterestsRepository.GetUserInterestsAsync(userId);
        
        if (userInterests?.TopicTags == null || !userInterests.TopicTags.Any())
        {
            return new List<Post>();
        }

        var sql = @"
            SELECT p.*
            FROM ""Sivar_Posts"" p
            JOIN ""Sivar_PostHashtags"" ph ON p.""Id"" = ph.""PostId""
            JOIN ""Sivar_Hashtags"" h ON ph.""HashtagId"" = h.""Id""
            WHERE h.""Tag"" = ANY({0})
              AND NOT p.""IsDeleted""
              AND p.""CreatedAt"" >= NOW() - INTERVAL '3 days'
            ORDER BY h.""TrendingScore"" DESC, p.""CreatedAt"" DESC
            LIMIT {1}";

        return await _postRepository.ExecuteRawSqlAsync(sql, userInterests.TopicTags, limit);
    }

    /// <summary>
    /// Inject native ads at strategic positions (every 5 posts)
    /// </summary>
    private async Task<List<ScoredFeedItem>> InjectAdsAsync(
        Guid userId,
        List<ScoredFeedItem> feedItems,
        string requestId)
    {
        // Get user profile for targeting
        var userProfile = await _userInterestsRepository.GetUserProfileAsync(userId);

        // Get targeted ads
        var ads = await _adRepository.GetTargetedAdsAsync(
            userProfile,
            limit: (feedItems.Count / 5) + 1 // One ad per 5 posts
        );

        if (!ads.Any())
        {
            return feedItems;
        }

        var feedWithAds = new List<ScoredFeedItem>();
        var adIndex = 0;

        for (int i = 0; i < feedItems.Count; i++)
        {
            feedWithAds.Add(feedItems[i]);

            // Inject ad every 5 posts (positions 4, 9, 14, ...)
            if ((i + 1) % 5 == 4 && adIndex < ads.Count)
            {
                var ad = ads[adIndex++];
                
                // Track impression
                await _adRepository.RecordImpressionAsync(new AdImpression
                {
                    AdId = ad.Id,
                    UserId = userId,
                    ViewedAt = DateTime.UtcNow,
                    Position = feedWithAds.Count,
                    Clicked = false
                });

                // Convert ad to feed item
                feedWithAds.Add(new ScoredFeedItem
                {
                    Post = null,
                    Ad = ad,
                    TotalScore = 0, // Ads don't have scores
                    IsAd = true
                });

                _logger.LogDebug(
                    "[InjectAds] Injected ad - RequestId={RequestId}, AdId={AdId}, Position={Position}",
                    requestId, ad.Id, feedWithAds.Count - 1);
            }
        }

        return feedWithAds;
    }
}
```

---

## Phase 4: Ad Targeting System (Week 5)

### 4.1 Ad Targeting Repository

```csharp
public class AdCampaignRepository : IAdCampaignRepository
{
    private readonly SivarDbContext _context;
    private readonly ILogger<AdCampaignRepository> _logger;

    public async Task<List<AdCampaign>> GetTargetedAdsAsync(
        UserInterestProfile userProfile,
        int limit = 5)
    {
        var now = DateTime.UtcNow;

        // Build targeting query
        var query = _context.AdCampaigns
            .Where(a => a.IsActive && !a.IsDeleted)
            .Where(a => a.Status == "Active")
            .Where(a => a.StartDate <= now && a.EndDate >= now)
            .Where(a => a.SpentBudget < a.Budget)
            .AsQueryable();

        // Demographic targeting (JSONB query)
        if (userProfile.Demographics != null)
        {
            // Age targeting
            if (userProfile.Demographics.Age.HasValue)
            {
                query = query.Where(a => 
                    EF.Functions.JsonContains(a.TargetDemographics, 
                        new { ageMin = userProfile.Demographics.Age.Value }) ||
                    EF.Functions.JsonContains(a.TargetDemographics, 
                        new { ageMax = userProfile.Demographics.Age.Value })
                );
            }

            // Location targeting
            if (!string.IsNullOrEmpty(userProfile.Demographics.Location))
            {
                query = query.Where(a =>
                    EF.Functions.JsonContains(a.TargetDemographics,
                        new { locations = new[] { userProfile.Demographics.Location } })
                );
            }
        }

        // Interest targeting (array overlap)
        if (userProfile.TopicTags?.Any() == true)
        {
            query = query.Where(a => 
                a.TargetInterests.Any(interest => userProfile.TopicTags.Contains(interest))
            );
        }

        // Behavioral targeting
        if (userProfile.EngagementLevel != null)
        {
            query = query.Where(a =>
                EF.Functions.JsonContains(a.TargetBehaviors,
                    new { engagementLevel = userProfile.EngagementLevel })
            );
        }

        // Get ads with scoring
        var ads = await query
            .Take(limit * 2) // Get more than needed for scoring
            .ToListAsync();

        // Score and rank ads
        var scoredAds = ads.Select(ad => new
        {
            Ad = ad,
            Score = CalculateAdRelevanceScore(ad, userProfile)
        })
        .OrderByDescending(x => x.Score)
        .Take(limit)
        .Select(x => x.Ad)
        .ToList();

        return scoredAds;
    }

    private double CalculateAdRelevanceScore(AdCampaign ad, UserInterestProfile profile)
    {
        double score = 0;

        // Interest match score (30%)
        if (ad.TargetInterests?.Any() == true && profile.TopicTags?.Any() == true)
        {
            var matchCount = ad.TargetInterests.Intersect(profile.TopicTags).Count();
            var interestScore = (double)matchCount / ad.TargetInterests.Length;
            score += interestScore * 0.30;
        }

        // Demographic match score (20%)
        // (simplified - would need to parse JSONB in real implementation)
        score += 0.20;

        // Bid/Budget score (30%)
        var budgetRemaining = (ad.Budget - ad.SpentBudget) / ad.Budget;
        score += budgetRemaining * 0.30;

        // Freshness score (20%)
        var daysSinceStart = (DateTime.UtcNow - ad.StartDate).TotalDays;
        var campaignDuration = (ad.EndDate - ad.StartDate).TotalDays;
        var freshnessScore = 1.0 - (daysSinceStart / campaignDuration);
        score += Math.Max(0, freshnessScore) * 0.20;

        return score;
    }

    public async Task RecordImpressionAsync(AdImpression impression)
    {
        _context.AdImpressions.Add(impression);
        await _context.SaveChangesAsync();

        // Update campaign spent budget
        await UpdateCampaignSpentAsync(impression.AdId, incrementImpression: true);
    }

    public async Task RecordClickAsync(Guid impressionId)
    {
        var impression = await _context.AdImpressions.FindAsync(impressionId);
        if (impression != null)
        {
            impression.Clicked = true;
            impression.ClickedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Update campaign spent budget
            await UpdateCampaignSpentAsync(impression.AdId, incrementClick: true);
        }
    }

    private async Task UpdateCampaignSpentAsync(
        Guid adId, 
        bool incrementImpression = false, 
        bool incrementClick = false)
    {
        var campaign = await _context.AdCampaigns.FindAsync(adId);
        if (campaign != null)
        {
            if (incrementImpression && campaign.CostPerImpression.HasValue)
            {
                campaign.SpentBudget += campaign.CostPerImpression.Value;
            }

            if (incrementClick && campaign.CostPerClick.HasValue)
            {
                campaign.SpentBudget += campaign.CostPerClick.Value;
            }

            // Pause campaign if budget exhausted
            if (campaign.SpentBudget >= campaign.Budget)
            {
                campaign.IsActive = false;
                campaign.Status = "Completed";
            }

            await _context.SaveChangesAsync();
        }
    }
}
```

---

## Phase 5: Feed Caching Strategy (Week 6)

### 5.1 Feed Cache Service

```csharp
public class FeedCacheService
{
    private readonly SivarDbContext _context;
    private readonly FeedCompositionService _compositionService;
    private readonly ILogger<FeedCacheService> _logger;

    public async Task<FeedResponse> GetOrGenerateFeedAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        bool forceRefresh = false)
    {
        var requestId = Guid.NewGuid().ToString("N");

        if (!forceRefresh)
        {
            // Try to get from cache
            var cached = await GetCachedFeedAsync(userId, page, pageSize);
            
            if (cached != null && cached.Items.Any())
            {
                _logger.LogInformation(
                    "[FeedCache] Cache HIT - RequestId={RequestId}, UserId={UserId}, Items={Count}",
                    requestId, userId, cached.Items.Count);
                return cached;
            }
        }

        _logger.LogInformation(
            "[FeedCache] Cache MISS - RequestId={RequestId}, UserId={UserId}, Generating fresh feed",
            requestId, userId);

        // Generate fresh feed
        var fresh = await _compositionService.GenerateFeedAsync(userId, page, pageSize);

        // Cache for 1 hour
        await CacheFeedAsync(userId, fresh, TimeSpan.FromHours(1));

        return fresh;
    }

    private async Task<FeedResponse?> GetCachedFeedAsync(
        Guid userId,
        int page,
        int pageSize)
    {
        var now = DateTime.UtcNow;
        var skip = (page - 1) * pageSize;

        var cachedItems = await _context.FeedCache
            .Where(f => f.UserId == userId)
            .Where(f => f.ExpiresAt > now)
            .Where(f => f.Rank >= skip && f.Rank < skip + pageSize)
            .OrderBy(f => f.Rank)
            .ToListAsync();

        if (!cachedItems.Any())
        {
            return null;
        }

        // Load full posts
        var postIds = cachedItems.Select(x => x.PostId).ToList();
        var posts = await _context.Posts
            .Where(p => postIds.Contains(p.Id))
            .ToListAsync();

        // Map to DTOs (preserving cache order)
        var feedItems = cachedItems
            .Select(cache => new FeedItemDto
            {
                Post = posts.FirstOrDefault(p => p.Id == cache.PostId),
                Score = cache.Score,
                FeedType = cache.FeedType
            })
            .ToList();

        return new FeedResponse
        {
            Items = feedItems,
            Page = page,
            PageSize = pageSize,
            HasMore = true // Assume more cached items exist
        };
    }

    private async Task CacheFeedAsync(
        Guid userId,
        FeedResponse feed,
        TimeSpan expiration)
    {
        var expiresAt = DateTime.UtcNow.Add(expiration);
        var generatedAt = DateTime.UtcNow;

        // Delete old cache for this user
        await _context.Database.ExecuteSqlRawAsync(
            @"DELETE FROM ""Sivar_FeedCache"" 
              WHERE ""UserId"" = {0} 
              AND ""ExpiresAt"" < {1}",
            userId, DateTime.UtcNow.AddMinutes(5)
        );

        // Insert new cache entries
        var cacheEntries = feed.Items
            .Where(item => !item.IsAd) // Don't cache ads
            .Select((item, index) => new FeedCacheEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PostId = item.Post.Id,
                Score = item.Score,
                Rank = ((feed.Page - 1) * feed.PageSize) + index,
                FeedType = item.FeedType,
                GeneratedAt = generatedAt,
                ExpiresAt = expiresAt
            })
            .ToList();

        await _context.FeedCache.AddRangeAsync(cacheEntries);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[FeedCache] Cached feed - UserId={UserId}, Items={Count}, ExpiresAt={ExpiresAt}",
            userId, cacheEntries.Count, expiresAt);
    }
}
```

---

## Phase 6: User Interface (Week 7-8)

### 6.1 Feed Component (Blazor)

```razor
@page "/feed"
@inject IFeedService FeedService
@inject IJSRuntime JS
@using Sivar.Os.Shared.DTOs

<PageTitle>Feed - Sivar.Os</PageTitle>

<div class="feed-container">
    <div class="feed-header">
        <h1>Your Feed</h1>
        <MudButton OnClick="RefreshFeed" Color="Color.Primary" Variant="Variant.Text">
            <MudIcon Icon="@Icons.Material.Filled.Refresh" />
            Refresh
        </MudButton>
    </div>

    @if (_loading && !_feedItems.Any())
    {
        <div class="feed-loading">
            <MudProgressCircular Indeterminate="true" Size="Size.Large" />
            <p>Loading your personalized feed...</p>
        </div>
    }
    else
    {
        <div class="feed-items" @ref="_feedContainer">
            @foreach (var item in _feedItems)
            {
                @if (item.IsAd)
                {
                    <!-- Native Ad Component -->
                    <div class="feed-item feed-ad">
                        <div class="ad-label">Sponsored</div>
                        <AdCard Ad="item.Ad" OnClick="() => HandleAdClick(item.Ad.Id)" />
                    </div>
                }
                else
                {
                    <!-- Regular Post Component -->
                    <div class="feed-item">
                        <PostCard Post="item.Post" />
                    </div>
                }
            }

            @if (_loading)
            {
                <div class="feed-loading-more">
                    <MudProgressCircular Indeterminate="true" />
                </div>
            }

            @if (!_hasMore && _feedItems.Any())
            {
                <div class="feed-end">
                    <p>You're all caught up! 🎉</p>
                </div>
            }
        </div>
    }
</div>

@code {
    private List<FeedItemDto> _feedItems = new();
    private ElementReference _feedContainer;
    private int _currentPage = 1;
    private bool _loading = false;
    private bool _hasMore = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadFeedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Set up infinite scroll
            await JS.InvokeVoidAsync("setupInfiniteScroll", 
                DotNetObjectReference.Create(this));
        }
    }

    private async Task LoadFeedAsync()
    {
        if (_loading || !_hasMore) return;

        _loading = true;
        StateHasChanged();

        try
        {
            var response = await FeedService.GetFeedAsync(_currentPage);
            
            _feedItems.AddRange(response.Items);
            _hasMore = response.HasMore;
            _currentPage++;
        }
        catch (Exception ex)
        {
            // Handle error
            Console.WriteLine($"Error loading feed: {ex.Message}");
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public async Task LoadMoreItems()
    {
        await LoadFeedAsync();
    }

    private async Task RefreshFeed()
    {
        _feedItems.Clear();
        _currentPage = 1;
        _hasMore = true;
        await LoadFeedAsync();
    }

    private async Task HandleAdClick(Guid adId)
    {
        // Track ad click
        await FeedService.RecordAdClickAsync(adId);
        
        // Open ad link in new tab
        // (handled by AdCard component)
    }
}
```

### 6.2 Infinite Scroll JavaScript

```javascript
// wwwroot/js/feed.js

window.setupInfiniteScroll = (dotNetHelper) => {
    const observer = new IntersectionObserver(
        (entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    // Load more items when bottom is reached
                    dotNetHelper.invokeMethodAsync('LoadMoreItems');
                }
            });
        },
        {
            root: null,
            rootMargin: '200px', // Trigger 200px before bottom
            threshold: 0.1
        }
    );

    // Observe the last feed item
    const feedContainer = document.querySelector('.feed-items');
    if (feedContainer) {
        const lastItem = feedContainer.lastElementChild;
        if (lastItem) {
            observer.observe(lastItem);
        }
    }

    // Re-observe when new items are added
    const mutationObserver = new MutationObserver(() => {
        observer.disconnect();
        const lastItem = feedContainer.lastElementChild;
        if (lastItem) {
            observer.observe(lastItem);
        }
    });

    mutationObserver.observe(feedContainer, {
        childList: true
    });
};
```

---

## Performance Targets

| Metric | Target | Notes |
|--------|--------|-------|
| **Feed Load Time** | < 500ms | With cache hit |
| **Feed Generation (cache miss)** | < 2s | Without cache |
| **Ranking Algorithm** | < 1s | For 100 candidate posts |
| **Ad Targeting** | < 200ms | Query + scoring |
| **Cache Hit Rate** | > 70% | 1-hour expiration |
| **Feed Staleness** | < 1 hour | Auto-refresh policy |
| **Infinite Scroll** | Seamless | Preload next page |

---

## Success Criteria

### Functional Requirements
- ✅ Feed shows mix of organic, trending, group, and hashtag content
- ✅ Algorithmic ranking based on relevance, engagement, recency
- ✅ Native ads seamlessly integrated every 5 posts
- ✅ Ad targeting by demographics, interests, behavior
- ✅ Infinite scroll with lazy loading
- ✅ Cache reduces database load by 70%+

### Non-Functional Requirements
- ✅ Feed loads in < 500ms (cached)
- ✅ Supports 10,000+ concurrent users
- ✅ TimescaleDB handles time-series data efficiently
- ✅ pgvector enables semantic recommendations
- ✅ Continuous aggregates provide real-time trending

---

## Testing Strategy

### Unit Tests
- Feed ranking algorithm components
- Ad targeting logic
- Cache expiration logic

### Integration Tests
- End-to-end feed generation
- Database queries performance
- Ad impression tracking

### Load Tests
- 10,000 concurrent feed requests
- Cache hit/miss scenarios
- Database query performance under load

### A/B Testing
- Different ranking weights
- Ad placement strategies
- Feed refresh frequencies

---

## Future Enhancements (Post-MVP)

1. **Machine Learning Ranking** - Train model on user engagement data
2. **Video Ads** - Support for video ad campaigns
3. **Real-time Feed Updates** - WebSocket for live updates
4. **Feed Customization** - User preferences for content types
5. **Advanced Analytics** - Feed engagement metrics dashboard
6. **Ad Auction System** - Real-time bidding for ad placements

---

## Estimated Timeline

| Phase | Duration | Deliverable |
|-------|----------|-------------|
| **Phase 1: Database Schema** | 1 week | Tables, indexes, hypertables |
| **Phase 2: Continuous Aggregates** | 1 week | Trending, hashtags, groups |
| **Phase 3: Ranking Algorithm** | 2 weeks | Scoring system, feed composition |
| **Phase 4: Ad Targeting** | 1 week | Ad selection, impression tracking |
| **Phase 5: Feed Caching** | 1 week | Cache service, optimization |
| **Phase 6: User Interface** | 2 weeks | Blazor components, infinite scroll |

**Total**: 6-8 weeks (depending on team size and parallel work)

---

## Next Steps

1. **Review and approve this plan**
2. **Create feature branch: `feature/feed-system`**
3. **Start with Phase 1: Database Schema**
4. **Implement incrementally, test continuously**
5. **Deploy to staging for user testing**
6. **Iterate based on feedback**

**Ready to start? Let's begin with Phase 1 - Database Schema!** 🚀
