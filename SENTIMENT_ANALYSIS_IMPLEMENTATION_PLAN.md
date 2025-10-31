# Sentiment Analysis Implementation Plan

**Status**: Planning Phase  
**Created**: October 31, 2025  
**Priority**: Medium  
**Estimated Total Time**: 12-16 hours

## Overview

Implement per-profile sentiment analysis system for activity streams using a hybrid client-side/server-side approach. This system will analyze user emotions (joy, sadness, anger, fear, neutral) for backend analytics only, supporting English and Spanish languages.

## Key Requirements

- ✅ Backend analytics only (not user-facing UI)
- ✅ Bilingual support (English + Spanish)
- ✅ Anger detection for content moderation
- ✅ Per-profile emotional tracking
- ✅ Leverage existing TimescaleDB continuous aggregates (Phase 7)
- ✅ Hybrid client-side/server-side approach (privacy-first)

## Architecture

### Models
- **Client**: `lxyuan/distilbert-base-multilingual-cased-sentiments-student` (EN/ES)
- **Client**: `SamLowe/roberta-base-go_emotions` (28 emotions → 5 categories)
- **Server**: Fallback using ML.NET or Azure Cognitive Services

### Emotion Categories
1. **Joy** - Happiness, excitement, love, gratitude
2. **Sadness** - Grief, disappointment, loneliness
3. **Anger** - Frustration, annoyance, rage (moderation flag)
4. **Fear** - Anxiety, worry, nervousness
5. **Neutral** - Informational, objective content

---

## Phase 1: Database Schema (2-3 hours)

### Task 1.1: Extend Posts and Comments Tables
**Priority**: CRITICAL  
**Estimated Time**: 1 hour

```sql
-- Add to Sivar_Posts
ALTER TABLE "Sivar_Posts" 
ADD COLUMN "PrimaryEmotion" VARCHAR(20),
ADD COLUMN "EmotionScore" DECIMAL(4,3),
ADD COLUMN "SentimentPolarity" DECIMAL(4,3),
ADD COLUMN "JoyScore" DECIMAL(4,3),
ADD COLUMN "SadnessScore" DECIMAL(4,3),
ADD COLUMN "AngerScore" DECIMAL(4,3),
ADD COLUMN "FearScore" DECIMAL(4,3),
ADD COLUMN "HasAnger" BOOLEAN DEFAULT FALSE,
ADD COLUMN "NeedsReview" BOOLEAN DEFAULT FALSE,
ADD COLUMN "AnalyzedAt" TIMESTAMPTZ,
ADD COLUMN "Language" VARCHAR(5);

-- Apply same to Sivar_Comments
ALTER TABLE "Sivar_Comments" 
ADD COLUMN "PrimaryEmotion" VARCHAR(20),
ADD COLUMN "EmotionScore" DECIMAL(4,3),
ADD COLUMN "SentimentPolarity" DECIMAL(4,3),
ADD COLUMN "JoyScore" DECIMAL(4,3),
ADD COLUMN "SadnessScore" DECIMAL(4,3),
ADD COLUMN "AngerScore" DECIMAL(4,3),
ADD COLUMN "FearScore" DECIMAL(4,3),
ADD COLUMN "HasAnger" BOOLEAN DEFAULT FALSE,
ADD COLUMN "NeedsReview" BOOLEAN DEFAULT FALSE,
ADD COLUMN "AnalyzedAt" TIMESTAMPTZ,
ADD COLUMN "Language" VARCHAR(5);

-- Add indexes for analytics queries
CREATE INDEX idx_posts_primary_emotion ON "Sivar_Posts"("PrimaryEmotion");
CREATE INDEX idx_posts_needs_review ON "Sivar_Posts"("NeedsReview") WHERE "NeedsReview" = TRUE;
CREATE INDEX idx_posts_has_anger ON "Sivar_Posts"("HasAnger") WHERE "HasAnger" = TRUE;
```

**Acceptance Criteria**:
- [ ] Migration created and tested
- [ ] All indexes created successfully
- [ ] No breaking changes to existing queries

### Task 1.2: Create ProfileEmotionSummaries Table
**Priority**: HIGH  
**Estimated Time**: 1 hour

```sql
CREATE TABLE "Sivar_ProfileEmotionSummaries" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ProfileId" UUID NOT NULL,
    "TimeWindow" VARCHAR(20) NOT NULL,  -- '7days', '30days', '90days'
    "StartDate" TIMESTAMPTZ NOT NULL,
    "EndDate" TIMESTAMPTZ NOT NULL,
    "DominantEmotion" VARCHAR(20),
    "AvgSentiment" DECIMAL(4,3),
    "TotalPosts" INTEGER DEFAULT 0,
    "TotalComments" INTEGER DEFAULT 0,
    "JoyCount" INTEGER DEFAULT 0,
    "SadnessCount" INTEGER DEFAULT 0,
    "AngerCount" INTEGER DEFAULT 0,
    "FearCount" INTEGER DEFAULT 0,
    "NeutralCount" INTEGER DEFAULT 0,
    "AvgJoyScore" DECIMAL(4,3),
    "AvgSadnessScore" DECIMAL(4,3),
    "AvgAngerScore" DECIMAL(4,3),
    "AvgFearScore" DECIMAL(4,3),
    "ContentFlagged" INTEGER DEFAULT 0,
    "LastUpdated" TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT fk_profile FOREIGN KEY ("ProfileId") REFERENCES "Sivar_Profiles"("Id") ON DELETE CASCADE,
    UNIQUE("ProfileId", "TimeWindow", "StartDate")
);

CREATE INDEX idx_profile_emotion_profile_id ON "Sivar_ProfileEmotionSummaries"("ProfileId");
CREATE INDEX idx_profile_emotion_time_window ON "Sivar_ProfileEmotionSummaries"("TimeWindow");
CREATE INDEX idx_profile_emotion_dates ON "Sivar_ProfileEmotionSummaries"("StartDate", "EndDate");
```

**Acceptance Criteria**:
- [ ] Table created with all constraints
- [ ] Foreign key relationship verified
- [ ] Indexes created for query optimization

### Task 1.3: Update DbContext
**Priority**: HIGH  
**Estimated Time**: 30 minutes

**File**: `Sivar.Os.Data/SivarDbContext.cs`

Add to DbContext:
```csharp
public DbSet<ProfileEmotionSummary> ProfileEmotionSummaries { get; set; }
```

Create entity class in `Sivar.Os.Shared/Models/`:
```csharp
public class ProfileEmotionSummary
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string TimeWindow { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DominantEmotion { get; set; }
    public decimal AvgSentiment { get; set; }
    public int TotalPosts { get; set; }
    public int TotalComments { get; set; }
    public int JoyCount { get; set; }
    public int SadnessCount { get; set; }
    public int AngerCount { get; set; }
    public int FearCount { get; set; }
    public int NeutralCount { get; set; }
    public decimal AvgJoyScore { get; set; }
    public decimal AvgSadnessScore { get; set; }
    public decimal AvgAngerScore { get; set; }
    public decimal AvgFearScore { get; set; }
    public int ContentFlagged { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Navigation
    public Profile Profile { get; set; }
}
```

**Acceptance Criteria**:
- [ ] Entity class created with all properties
- [ ] DbSet added to context
- [ ] Build successful

---

## Phase 2: TimescaleDB Continuous Aggregates (2-3 hours)

### Task 2.1: Community Sentiment Hourly Aggregate
**Priority**: HIGH  
**Estimated Time**: 1 hour

**File**: Create `Database/Scripts/AddSentimentContinuousAggregates.sql`

```sql
-- Community-wide sentiment tracking
CREATE MATERIALIZED VIEW community_sentiment_hourly
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 hour', "CreatedAt") AS hour,
    COUNT(*) as total_posts,
    AVG("SentimentPolarity") as avg_sentiment,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'joy') as joy_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'sadness') as sadness_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'anger') as anger_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'fear') as fear_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'neutral') as neutral_count,
    AVG("JoyScore") as avg_joy_score,
    AVG("SadnessScore") as avg_sadness_score,
    AVG("AngerScore") as avg_anger_score,
    AVG("FearScore") as avg_fear_score,
    COUNT(*) FILTER (WHERE "HasAnger" = TRUE) as anger_posts,
    COUNT(*) FILTER (WHERE "NeedsReview" = TRUE) as flagged_posts
FROM "Sivar_Posts"
WHERE "PrimaryEmotion" IS NOT NULL
GROUP BY hour;

-- Refresh policy
SELECT add_continuous_aggregate_policy('community_sentiment_hourly',
    start_offset => INTERVAL '3 hours',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour');
```

**Acceptance Criteria**:
- [ ] Materialized view created
- [ ] Refresh policy configured
- [ ] Test query returns valid data

### Task 2.2: Moderation Metrics Daily Aggregate
**Priority**: HIGH  
**Estimated Time**: 1 hour

```sql
-- Content moderation tracking
CREATE MATERIALIZED VIEW moderation_metrics_daily
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 day', "CreatedAt") AS day,
    COUNT(*) FILTER (WHERE "NeedsReview" = TRUE) as flagged_count,
    COUNT(*) FILTER (WHERE "HasAnger" = TRUE) as anger_count,
    AVG("AngerScore") FILTER (WHERE "HasAnger" = TRUE) as avg_anger_intensity,
    COUNT(DISTINCT "ProfileId") FILTER (WHERE "HasAnger" = TRUE) as unique_angry_users,
    COUNT(*) FILTER (WHERE "AngerScore" > 0.7) as high_anger_count
FROM "Sivar_Posts"
WHERE "PrimaryEmotion" IS NOT NULL
GROUP BY day;

SELECT add_continuous_aggregate_policy('moderation_metrics_daily',
    start_offset => INTERVAL '3 days',
    end_offset => INTERVAL '1 day',
    schedule_interval => INTERVAL '1 day');
```

**Acceptance Criteria**:
- [ ] Materialized view created
- [ ] Refresh policy configured
- [ ] Moderation dashboard queries work

### Task 2.3: Profile Sentiment Daily Aggregate
**Priority**: CRITICAL  
**Estimated Time**: 1 hour

```sql
-- Per-profile daily emotion tracking
CREATE MATERIALIZED VIEW profile_sentiment_daily
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 day', "CreatedAt") AS day,
    "ProfileId",
    COUNT(*) as post_count,
    AVG("SentimentPolarity") as avg_sentiment,
    mode() WITHIN GROUP (ORDER BY "PrimaryEmotion") as dominant_emotion,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'joy') as joy_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'sadness') as sadness_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'anger') as anger_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'fear') as fear_count,
    COUNT(*) FILTER (WHERE "PrimaryEmotion" = 'neutral') as neutral_count,
    AVG("JoyScore") as avg_joy,
    AVG("SadnessScore") as avg_sadness,
    AVG("AngerScore") as avg_anger,
    AVG("FearScore") as avg_fear,
    COUNT(*) FILTER (WHERE "NeedsReview" = TRUE) as flagged_count
FROM "Sivar_Posts"
WHERE "PrimaryEmotion" IS NOT NULL
GROUP BY day, "ProfileId";

SELECT add_continuous_aggregate_policy('profile_sentiment_daily',
    start_offset => INTERVAL '3 days',
    end_offset => INTERVAL '1 day',
    schedule_interval => INTERVAL '1 day');
```

**Acceptance Criteria**:
- [ ] Materialized view created
- [ ] Per-profile queries optimized
- [ ] Trend analysis queries work

---

## Phase 3: Client-Side Implementation (3-4 hours)

### Task 3.1: Create Sentiment Analyzer JavaScript Module
**Priority**: CRITICAL  
**Estimated Time**: 2-3 hours

**File**: `wwwroot/js/sentiment-analyzer.js`

```javascript
import { pipeline, env } from 'https://cdn.jsdelivr.net/npm/@xenova/transformers@2.6.0';

// Configure for browser
env.allowLocalModels = false;
env.useBrowserCache = true;

class SentimentAnalyzer {
    constructor() {
        this.sentimentPipeline = null;
        this.emotionPipeline = null;
        this.isInitialized = false;
    }

    async initialize() {
        if (this.isInitialized) return;
        
        try {
            // Multilingual sentiment (EN/ES)
            this.sentimentPipeline = await pipeline(
                'sentiment-analysis',
                'lxyuan/distilbert-base-multilingual-cased-sentiments-student'
            );
            
            // Emotion detection
            this.emotionPipeline = await pipeline(
                'text-classification',
                'SamLowe/roberta-base-go_emotions'
            );
            
            this.isInitialized = true;
            console.log('Sentiment analyzer initialized');
        } catch (error) {
            console.error('Failed to initialize sentiment analyzer:', error);
            throw error;
        }
    }

    mapGoEmotionsTo5Categories(emotions) {
        const categoryMap = {
            joy: ['admiration', 'amusement', 'approval', 'caring', 'desire', 'excitement', 'gratitude', 'joy', 'love', 'optimism', 'pride', 'relief'],
            sadness: ['disappointment', 'disapproval', 'embarrassment', 'grief', 'remorse', 'sadness'],
            anger: ['anger', 'annoyance', 'disgust'],
            fear: ['confusion', 'fear', 'nervousness', 'surprise'],
            neutral: ['curiosity', 'realization', 'neutral']
        };

        const scores = { joy: 0, sadness: 0, anger: 0, fear: 0, neutral: 0 };
        
        for (const emotion of emotions) {
            for (const [category, emotionList] of Object.entries(categoryMap)) {
                if (emotionList.includes(emotion.label.toLowerCase())) {
                    scores[category] += emotion.score;
                    break;
                }
            }
        }

        return scores;
    }

    async analyzeSentiment(text, language = 'en') {
        if (!this.isInitialized) {
            await this.initialize();
        }

        try {
            // Get sentiment polarity
            const sentimentResult = await this.sentimentPipeline(text);
            const sentimentPolarity = sentimentResult[0].label === 'positive' ? 
                sentimentResult[0].score : -sentimentResult[0].score;

            // Get detailed emotions
            const emotionResults = await this.emotionPipeline(text, { topk: 10 });
            const emotionScores = this.mapGoEmotionsTo5Categories(emotionResults);

            // Find primary emotion
            const primaryEmotion = Object.entries(emotionScores)
                .reduce((a, b) => a[1] > b[1] ? a : b)[0];
            const emotionScore = emotionScores[primaryEmotion];

            // Anger detection for moderation
            const hasAnger = emotionScores.anger > 0.3;
            const needsReview = emotionScores.anger > 0.5;

            return {
                primaryEmotion,
                emotionScore,
                sentimentPolarity,
                joyScore: emotionScores.joy,
                sadnessScore: emotionScores.sadness,
                angerScore: emotionScores.anger,
                fearScore: emotionScores.fear,
                hasAnger,
                needsReview,
                language,
                analyzedAt: new Date().toISOString()
            };
        } catch (error) {
            console.error('Sentiment analysis failed:', error);
            return null; // Trigger server fallback
        }
    }

    isSupported() {
        return 'Worker' in window && 'WebAssembly' in window;
    }
}

// Export singleton
const sentimentAnalyzer = new SentimentAnalyzer();
export default sentimentAnalyzer;

// Global namespace for Blazor interop
window.SentimentAnalyzer = {
    analyze: async (text, language) => {
        return await sentimentAnalyzer.analyzeSentiment(text, language);
    },
    isSupported: () => sentimentAnalyzer.isSupported()
};
```

**Acceptance Criteria**:
- [ ] Module loads in browser
- [ ] Both models download and initialize
- [ ] Analysis returns correct 5-category emotions
- [ ] Anger detection works with thresholds
- [ ] Blazor interop functions accessible

### Task 3.2: Test Client-Side Analysis
**Priority**: HIGH  
**Estimated Time**: 1 hour

Create test page to validate:
- English sentiment analysis
- Spanish sentiment analysis
- Anger detection accuracy
- Model loading time
- Error handling

**Acceptance Criteria**:
- [ ] Test samples analyzed correctly
- [ ] Both languages work
- [ ] Performance acceptable (<3 seconds)

---

## Phase 4: Server-Side Services (3-4 hours)

### Task 4.1: Create DTOs
**Priority**: HIGH  
**Estimated Time**: 30 minutes

**File**: `Sivar.Os.Shared/DTOs/SentimentAnalysisDtos.cs`

```csharp
namespace Sivar.Os.Shared.DTOs
{
    public class SentimentAnalysisResultDto
    {
        public string PrimaryEmotion { get; set; }
        public decimal EmotionScore { get; set; }
        public decimal SentimentPolarity { get; set; }
        public decimal JoyScore { get; set; }
        public decimal SadnessScore { get; set; }
        public decimal AngerScore { get; set; }
        public decimal FearScore { get; set; }
        public bool HasAnger { get; set; }
        public bool NeedsReview { get; set; }
        public string Language { get; set; }
        public DateTime AnalyzedAt { get; set; }
    }

    public class CommunitySentimentHourlyDto
    {
        public DateTime Hour { get; set; }
        public int TotalPosts { get; set; }
        public decimal AvgSentiment { get; set; }
        public int JoyCount { get; set; }
        public int SadnessCount { get; set; }
        public int AngerCount { get; set; }
        public int FearCount { get; set; }
        public int NeutralCount { get; set; }
        public int FlaggedPosts { get; set; }
    }

    public class ProfileSentimentDailyDto
    {
        public DateTime Day { get; set; }
        public Guid ProfileId { get; set; }
        public int PostCount { get; set; }
        public decimal AvgSentiment { get; set; }
        public string DominantEmotion { get; set; }
        public int JoyCount { get; set; }
        public int SadnessCount { get; set; }
        public int AngerCount { get; set; }
        public int FearCount { get; set; }
        public int NeutralCount { get; set; }
    }
}
```

**Acceptance Criteria**:
- [ ] All DTOs created
- [ ] Namespace correct
- [ ] Properties match database schema

### Task 4.2: Create Service Interfaces
**Priority**: HIGH  
**Estimated Time**: 30 minutes

**File**: `Sivar.Os/Services/ISentimentAnalysisService.cs`

```csharp
public interface IClientSentimentAnalysisService
{
    Task<SentimentAnalysisResultDto> TryAnalyzeAsync(string text, string language);
    bool IsSupported();
}

public interface IServerSentimentAnalysisService
{
    Task<SentimentAnalysisResultDto> AnalyzeAsync(string text, string language);
}
```

**Acceptance Criteria**:
- [ ] Interfaces defined
- [ ] Method signatures match requirements

### Task 4.3: Implement Client Service
**Priority**: CRITICAL  
**Estimated Time**: 1 hour

**File**: `Sivar.Os/Services/ClientSentimentAnalysisService.cs`

```csharp
public class ClientSentimentAnalysisService : IClientSentimentAnalysisService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ClientSentimentAnalysisService> _logger;

    public ClientSentimentAnalysisService(IJSRuntime jsRuntime, ILogger<ClientSentimentAnalysisService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<SentimentAnalysisResultDto> TryAnalyzeAsync(string text, string language)
    {
        if (!IsSupported())
        {
            _logger.LogWarning("Client-side sentiment analysis not supported");
            return null;
        }

        try
        {
            var result = await _jsRuntime.InvokeAsync<SentimentAnalysisResultDto>(
                "SentimentAnalyzer.analyze", text, language);
            
            _logger.LogInformation("Client-side sentiment analysis completed");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client-side sentiment analysis failed");
            return null; // Fallback to server
        }
    }

    public bool IsSupported()
    {
        try
        {
            return _jsRuntime.InvokeAsync<bool>("SentimentAnalyzer.isSupported").Result;
        }
        catch
        {
            return false;
        }
    }
}
```

**Acceptance Criteria**:
- [ ] Service implementation complete
- [ ] Error handling in place
- [ ] Logging configured

### Task 4.4: Implement Server Fallback Service
**Priority**: HIGH  
**Estimated Time**: 1-2 hours

**File**: `Sivar.Os/Services/ServerSentimentAnalysisService.cs`

```csharp
public class ServerSentimentAnalysisService : IServerSentimentAnalysisService
{
    private readonly ILogger<ServerSentimentAnalysisService> _logger;

    public ServerSentimentAnalysisService(ILogger<ServerSentimentAnalysisService> logger)
    {
        _logger = logger;
    }

    public async Task<SentimentAnalysisResultDto> AnalyzeAsync(string text, string language)
    {
        // TODO: Implement using ML.NET or Azure Cognitive Services
        // For now, return basic analysis
        _logger.LogWarning("Using basic server-side sentiment analysis - ML.NET integration pending");
        
        return new SentimentAnalysisResultDto
        {
            PrimaryEmotion = "neutral",
            EmotionScore = 0.5m,
            SentimentPolarity = 0.0m,
            JoyScore = 0.0m,
            SadnessScore = 0.0m,
            AngerScore = 0.0m,
            FearScore = 0.0m,
            HasAnger = false,
            NeedsReview = false,
            Language = language,
            AnalyzedAt = DateTime.UtcNow
        };
    }
}
```

**Note**: Server fallback is basic for now. Future enhancement with ML.NET or Azure AI.

**Acceptance Criteria**:
- [ ] Basic fallback works
- [ ] Returns valid DTO structure
- [ ] Logging in place

---

## Phase 5: Integration (2-3 hours)

### Task 5.1: Update PostService
**Priority**: CRITICAL  
**Estimated Time**: 1-2 hours

**File**: `Sivar.Os/Services/PostService.cs`

Modify `CreatePostAsync` method:

```csharp
public async Task<PostDto> CreatePostAsync(CreatePostDto createPostDto)
{
    // ... existing validation code ...

    // Hybrid sentiment analysis (client → server fallback)
    SentimentAnalysisResultDto sentiment = null;
    
    if (!string.IsNullOrWhiteSpace(createPostDto.Content))
    {
        var detectedLanguage = DetectLanguage(createPostDto.Content); // Implement simple detection
        
        sentiment = await _clientSentimentService.TryAnalyzeAsync(
            createPostDto.Content, 
            detectedLanguage
        );

        if (sentiment == null)
        {
            _logger.LogInformation("Falling back to server-side sentiment analysis");
            sentiment = await _serverSentimentService.AnalyzeAsync(
                createPostDto.Content,
                detectedLanguage
            );
        }
    }

    var post = new Post
    {
        Id = Guid.NewGuid(),
        ProfileId = createPostDto.ProfileId,
        Content = createPostDto.Content,
        CreatedAt = DateTime.UtcNow,
        
        // Add sentiment fields
        PrimaryEmotion = sentiment?.PrimaryEmotion,
        EmotionScore = sentiment?.EmotionScore,
        SentimentPolarity = sentiment?.SentimentPolarity,
        JoyScore = sentiment?.JoyScore,
        SadnessScore = sentiment?.SadnessScore,
        AngerScore = sentiment?.AngerScore,
        FearScore = sentiment?.FearScore,
        HasAnger = sentiment?.HasAnger ?? false,
        NeedsReview = sentiment?.NeedsReview ?? false,
        AnalyzedAt = sentiment?.AnalyzedAt,
        Language = sentiment?.Language
    };

    // ... rest of existing code ...
}

private string DetectLanguage(string content)
{
    // Simple detection based on common Spanish words
    var spanishWords = new[] { "el", "la", "de", "que", "y", "en", "es", "por", "para" };
    var words = content.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    
    var spanishCount = words.Count(w => spanishWords.Contains(w));
    return spanishCount > words.Length * 0.1 ? "es" : "en";
}
```

**Acceptance Criteria**:
- [ ] Hybrid analysis integrated
- [ ] Client-side attempted first
- [ ] Server fallback works
- [ ] Language detection functional
- [ ] All sentiment fields saved

### Task 5.2: Update CommentService (Similar Pattern)
**Priority**: HIGH  
**Estimated Time**: 1 hour

Apply same hybrid sentiment analysis to `CreateCommentAsync`.

**Acceptance Criteria**:
- [ ] Comments analyzed for sentiment
- [ ] Same hybrid pattern as posts

### Task 5.3: Register Services in DI
**Priority**: CRITICAL  
**Estimated Time**: 15 minutes

**File**: `Program.cs`

```csharp
// Add before builder.Build()
builder.Services.AddScoped<IClientSentimentAnalysisService, ClientSentimentAnalysisService>();
builder.Services.AddScoped<IServerSentimentAnalysisService, ServerSentimentAnalysisService>();
```

**Acceptance Criteria**:
- [ ] Services registered
- [ ] Application builds
- [ ] DI injection works

---

## Phase 6: Analytics APIs (2 hours)

### Task 6.1: Extend AnalyticsRepository
**Priority**: HIGH  
**Estimated Time**: 1 hour

**File**: `Sivar.Os.Data/Repositories/AnalyticsRepository.cs`

Add methods:

```csharp
public async Task<List<CommunitySentimentHourlyDto>> GetCommunitySentimentAsync(
    DateTime startDate, DateTime endDate)
{
    return await _context.Database
        .SqlQuery<CommunitySentimentHourlyDto>($@"
            SELECT hour, total_posts, avg_sentiment, 
                   joy_count, sadness_count, anger_count, fear_count, neutral_count,
                   flagged_posts
            FROM community_sentiment_hourly
            WHERE hour >= {startDate} AND hour < {endDate}
            ORDER BY hour")
        .ToListAsync();
}

public async Task<List<ProfileSentimentDailyDto>> GetProfileEmotionTrendAsync(
    Guid profileId, DateTime startDate, DateTime endDate)
{
    return await _context.Database
        .SqlQuery<ProfileSentimentDailyDto>($@"
            SELECT day, ""ProfileId"", post_count, avg_sentiment, dominant_emotion,
                   joy_count, sadness_count, anger_count, fear_count, neutral_count
            FROM profile_sentiment_daily
            WHERE ""ProfileId"" = {profileId} 
              AND day >= {startDate} AND day < {endDate}
            ORDER BY day")
        .ToListAsync();
}
```

**Acceptance Criteria**:
- [ ] Methods implemented
- [ ] Queries optimized
- [ ] Return correct DTOs

### Task 6.2: Add Analytics Endpoints
**Priority**: MEDIUM  
**Estimated Time**: 1 hour

**File**: `Sivar.Os/Controllers/AnalyticsController.cs`

```csharp
[HttpGet("sentiment/community")]
public async Task<ActionResult<List<CommunitySentimentHourlyDto>>> GetCommunitySentiment(
    [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
{
    var results = await _analyticsRepo.GetCommunitySentimentAsync(startDate, endDate);
    return Ok(results);
}

[HttpGet("profiles/{profileId}/emotion-trend")]
public async Task<ActionResult<List<ProfileSentimentDailyDto>>> GetProfileEmotionTrend(
    Guid profileId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
{
    var results = await _analyticsRepo.GetProfileEmotionTrendAsync(profileId, startDate, endDate);
    return Ok(results);
}
```

**Acceptance Criteria**:
- [ ] Endpoints created
- [ ] Swagger documentation updated
- [ ] API tested with Postman/curl

---

## Testing & Validation

### Integration Tests
- [ ] Post creation with sentiment analysis
- [ ] Comment creation with sentiment analysis
- [ ] Client-side analysis works in browser
- [ ] Server fallback triggers correctly
- [ ] Both English and Spanish analyzed

### Performance Tests
- [ ] Client-side analysis < 3 seconds
- [ ] Server fallback < 1 second
- [ ] Continuous aggregate refresh within SLA
- [ ] API endpoints respond < 500ms

### Data Validation
- [ ] Sentiment scores in valid range (0-1)
- [ ] Primary emotion matches highest score
- [ ] Anger detection threshold works
- [ ] Language detection accuracy > 80%

---

## Configuration

**File**: `appsettings.json`

```json
{
  "SentimentAnalysis": {
    "EnableClientSide": true,
    "EnableServerFallback": true,
    "AngerThreshold": 0.3,
    "ModerationThreshold": 0.5,
    "SupportedLanguages": ["en", "es"],
    "MinTextLength": 10
  }
}
```

---

## Deployment Checklist

- [ ] Database migrations applied to dev
- [ ] TimescaleDB aggregates created
- [ ] JavaScript module deployed
- [ ] Services registered in DI
- [ ] Configuration validated
- [ ] Integration tests pass
- [ ] Performance benchmarks met
- [ ] Database migrations applied to production
- [ ] Monitoring alerts configured
- [ ] Documentation updated

---

## Future Enhancements

1. **Phase 7**: Improve server fallback with ML.NET or Azure Cognitive Services
2. **Phase 8**: Add more languages (Portuguese, French)
3. **Phase 9**: Real-time sentiment monitoring dashboard
4. **Phase 10**: Automated content moderation actions
5. **Phase 11**: Sentiment-based content recommendations

---

## Dependencies

- ✅ PostgreSQL 14+ with pgvector
- ✅ TimescaleDB extension
- ✅ Phase 7 continuous aggregates infrastructure
- ✅ Transformers.js library (CDN)
- ✅ Blazor Server with IJSRuntime
- ⏳ ML.NET (future for server fallback)

---

## Notes

- This is backend analytics only - no UI components in this phase
- Client-side processing prioritizes privacy and cost reduction
- Anger detection designed for political/controversial content moderation
- Per-profile tracking enables granular user behavior insights
- Continuous aggregates provide pre-computed analytics at scale

---

**Last Updated**: October 31, 2025  
**Status**: Ready for Implementation
