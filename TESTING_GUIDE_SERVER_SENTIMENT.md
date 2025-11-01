# Server-Side Sentiment Analysis Testing Guide

## ✅ Test Status: Ready to Test

**Mode:** ServerOnly (keyword-based)  
**Build Status:** ✅ Compiled successfully  
**Date:** October 31, 2025

---

## Quick Test (5 minutes)

### Step 1: Start the Application

```powershell
cd C:\Users\joche\source\repos\SivarOs\Sivar.Os
dotnet run --project Sivar.Os/Sivar.Os.csproj
```

### Step 2: Watch for Startup Logs

Look for this in console:
```
[SentimentAnalysis] Service initialized with mode: ServerOnly
[SentimentAnalysis.ServerOnly] Using server-side processing exclusively
```

### Step 3: Create Test Posts

Open browser and create posts with these texts:

#### Test Case 1: Joy (Positive)
**Text:** `I am so happy and excited about this amazing project! I love it!`

**Expected Result:**
- Primary Emotion: **Joy**
- Sentiment Polarity: **Positive** (> 0)
- Emotion Score: High (> 0.5)

**Logs to check:**
```
[ServerSentiment] Performing server-side fallback analysis (en, XX chars)
[ServerSentiment] ✅ Fallback analysis complete: Joy (score: X.XX)
```

---

#### Test Case 2: Sadness (Negative)
**Text:** `I feel so sad and disappointed about this situation. Very unhappy.`

**Expected Result:**
- Primary Emotion: **Sadness**
- Sentiment Polarity: **Negative** (< 0)
- Emotion Score: High (> 0.5)

**Logs to check:**
```
[ServerSentiment] ✅ Fallback analysis complete: Sadness (score: X.XX)
```

---

#### Test Case 3: Anger (Negative)
**Text:** `I am so angry and furious! I hate this terrible situation!`

**Expected Result:**
- Primary Emotion: **Anger**
- Sentiment Polarity: **Negative** (< 0)
- HasAnger: **true**
- NeedsReview: **true** (if anger score > 0.75)

**Logs to check:**
```
[ServerSentiment] ✅ Fallback analysis complete: Anger (score: X.XX)
```

---

#### Test Case 4: Fear (Negative)
**Text:** `I am so scared and worried. Feeling afraid and anxious about this.`

**Expected Result:**
- Primary Emotion: **Fear**
- Sentiment Polarity: **Negative** (< 0)
- Emotion Score: High (> 0.5)

**Logs to check:**
```
[ServerSentiment] ✅ Fallback analysis complete: Fear (score: X.XX)
```

---

#### Test Case 5: Neutral
**Text:** `The meeting is scheduled for tomorrow at 3pm in the conference room.`

**Expected Result:**
- Primary Emotion: **Neutral**
- Sentiment Polarity: **~0** (close to zero)
- Emotion Score: Low to Medium

**Logs to check:**
```
[ServerSentiment] ✅ Fallback analysis complete: Neutral (score: X.XX)
```

---

#### Test Case 6: Spanish (Multilingual)
**Text:** `Estoy muy feliz y alegre hoy! Me encanta este proyecto!`

**Expected Result:**
- Primary Emotion: **Joy**
- Language: **es** (Spanish)
- Should detect "feliz", "alegre", "amor" keywords

**Logs to check:**
```
[ServerSentiment] Performing server-side fallback analysis (es, XX chars)
[ServerSentiment] ✅ Fallback analysis complete: Joy (score: X.XX)
```

---

## What to Verify

### ✅ Success Criteria

1. **Service Initialization:**
   - [ ] Console shows: `Service initialized with mode: ServerOnly`
   - [ ] No errors during startup

2. **Analysis Execution:**
   - [ ] Each post creation triggers analysis
   - [ ] Console shows: `Performing server-side fallback analysis`
   - [ ] Console shows: `✅ Fallback analysis complete: [Emotion]`

3. **Correct Results:**
   - [ ] Positive text → Joy emotion
   - [ ] Sad text → Sadness emotion
   - [ ] Angry text → Anger emotion + HasAnger flag
   - [ ] Fear text → Fear emotion
   - [ ] Neutral text → Neutral emotion
   - [ ] Spanish text → Detects Spanish keywords

4. **Data Persistence:**
   - [ ] Sentiment data saved to database
   - [ ] Can see sentiment in post details
   - [ ] AnalysisSource = "server"

5. **Performance:**
   - [ ] Analysis completes in < 100ms
   - [ ] No delays or timeouts
   - [ ] Instant user experience

---

## Database Verification

### Check Sentiment in Database

```sql
-- Connect to PostgreSQL
-- Database: XafSivarOs

-- View recent posts with sentiment
SELECT 
    p.id,
    p.content,
    p.primary_emotion,
    p.emotion_score,
    p.sentiment_polarity,
    p.has_anger,
    p.needs_review,
    p.analysis_source,
    p.analyzed_at
FROM posts p
ORDER BY p.created_at DESC
LIMIT 10;

-- Check for server-side analysis
SELECT COUNT(*) as server_count
FROM posts
WHERE analysis_source = 'server';

-- Check emotion distribution
SELECT 
    primary_emotion,
    COUNT(*) as count,
    AVG(emotion_score) as avg_score
FROM posts
WHERE analysis_source = 'server'
GROUP BY primary_emotion
ORDER BY count DESC;
```

---

## Troubleshooting

### Problem: No logs showing

**Solution:**
1. Check `appsettings.json` → Serilog MinimumLevel = "Information"
2. Restart application
3. Check logs folder: `Sivar.Os/logs/sivar-[date].txt`

### Problem: Always returning Neutral

**Cause:** Text doesn't contain keywords

**Solution:**
- Use stronger emotion words: "happy", "angry", "sad", "scared"
- Check keyword dictionary in `ServerSentimentAnalysisService.cs`
- This is expected for truly neutral text

### Problem: Spanish not detected

**Cause:** Limited Spanish keyword dictionary

**Solution:**
- Add more Spanish keywords to `EmotionKeywords` dictionary
- Or switch to ML model (ML.NET or Azure AI)

### Problem: Service not found error

**Cause:** DI registration missing

**Solution:**
Check `Program.cs` has:
```csharp
builder.Services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();
builder.Services.AddScoped<IServerSentimentAnalysisService, ServerSentimentAnalysisService>();
```

---

## Advanced Testing

### Test with Code

Create a test file: `Sivar.Os.Tests/Services/ServerSentimentAnalysisServiceTests.cs`

```csharp
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Sivar.Os.Services;

public class ServerSentimentAnalysisServiceTests
{
    private readonly ServerSentimentAnalysisService _service;

    public ServerSentimentAnalysisServiceTests()
    {
        _service = new ServerSentimentAnalysisService(
            NullLogger<ServerSentimentAnalysisService>.Instance
        );
    }

    [Fact]
    public async Task AnalyzeAsync_JoyfulText_ReturnsJoy()
    {
        // Arrange
        var text = "I am so happy and excited about this amazing project!";

        // Act
        var result = await _service.AnalyzeAsync(text, "en");

        // Assert
        Assert.Equal("Joy", result.PrimaryEmotion);
        Assert.True(result.SentimentPolarity > 0);
        Assert.Equal("server", result.AnalysisSource);
    }

    [Fact]
    public async Task AnalyzeAsync_AngryText_ReturnsAngerWithFlag()
    {
        // Arrange
        var text = "I am so angry and furious! I hate this!";

        // Act
        var result = await _service.AnalyzeAsync(text, "en");

        // Assert
        Assert.Equal("Anger", result.PrimaryEmotion);
        Assert.True(result.HasAnger);
        Assert.True(result.SentimentPolarity < 0);
    }

    [Fact]
    public async Task AnalyzeAsync_NeutralText_ReturnsNeutral()
    {
        // Arrange
        var text = "The meeting is at 3pm.";

        // Act
        var result = await _service.AnalyzeAsync(text, "en");

        // Assert
        Assert.Equal("Neutral", result.PrimaryEmotion);
    }
}
```

**Run tests:**
```powershell
dotnet test Sivar.Os.Tests/Sivar.Os.Tests.csproj --filter "ServerSentimentAnalysis"
```

---

## Success Checklist

Use this checklist to confirm everything works:

- [ ] Application starts without errors
- [ ] Console shows "Service initialized with mode: ServerOnly"
- [ ] Can create posts in the UI
- [ ] Console shows sentiment analysis logs
- [ ] Joy test case works correctly
- [ ] Sadness test case works correctly
- [ ] Anger test case works correctly (+ HasAnger flag)
- [ ] Fear test case works correctly
- [ ] Neutral test case works correctly
- [ ] Spanish test case detects keywords
- [ ] Database shows `analysis_source = 'server'`
- [ ] Sentiment displays in UI
- [ ] Analysis completes in < 100ms
- [ ] No JavaScript errors (client models NOT required)

---

## Next Steps After Testing

### If All Tests Pass ✅

1. **Document results** in GitHub issue #5
2. **Consider upgrading** to ML.NET for higher accuracy (optional)
3. **Monitor production** logs for sentiment distribution
4. **Add more keywords** to improve coverage

### If Tests Fail ❌

1. **Check logs** for error messages
2. **Verify configuration** in appsettings.json
3. **Check DI registration** in Program.cs
4. **Debug** using breakpoints in `ServerSentimentAnalysisService.cs`
5. **Update GitHub issue #5** with findings

---

**Test Duration:** ~10 minutes  
**Expected Outcome:** All 6 test cases pass with correct emotions detected

Good luck testing! 🚀
