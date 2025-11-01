# AI Service Modes Configuration Guide

## Quick Reference

### Change Mode in appsettings.json

```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "Adaptive"  // Options: "Adaptive", "ClientOnly", "ServerOnly"
    },
    "Embeddings": {
      "Mode": "Adaptive"  // Options: "Adaptive", "ClientOnly", "ServerOnly"
    }
  }
}
```

## Three Modes Explained

### 1. Adaptive (Default - Recommended) ✅

**Best for**: Production use, best UX

**How it works:**
1. Check if browser models are loaded
2. IF loaded → use client-side ML (high quality)
3. IF not loaded → use server-side (instant processing)

**User Experience:**
- First visit: Server-side (instant) → Models download in background → Future posts use client-side
- Return visit: Client-side immediately (models cached)

**Pros:**
- ✅ Users never wait
- ✅ Quality improves automatically
- ✅ Works for everyone
- ✅ Privacy-first when possible

**Cons:**
- ⚠️ First post uses server (keyword-based, lower quality)

---

### 2. ClientOnly (Privacy-First) 🔒

**Best for**: Privacy-critical applications, healthcare, sensitive data

**How it works:**
- ALWAYS use browser ML
- Throw error if models not loaded

**User Experience:**
- First visit: Wait 10-60 seconds for models → Then instant
- Return visit: Instant (models cached)

**Pros:**
- ✅ Maximum privacy (no server processing)
- ✅ Free (no API costs)
- ✅ Offline-capable after first load

**Cons:**
- ❌ Users must wait on first visit
- ❌ May fail if models don't load

**Error Handling:**
```csharp
try
{
    var result = await _sentimentService.AnalyzeAsync(text, language);
}
catch (InvalidOperationException ex)
{
    // Models not ready - show notification
    ShowNotification("AI models are loading. Please wait a moment.");
}
```

---

### 3. ServerOnly (Instant Processing) ⚡

**Best for**: Testing, debugging, when you have ML.NET configured

**How it works:**
- ALWAYS use server-side processing
- Never use browser ML

**User Experience:**
- All visits: Instant processing

**Pros:**
- ✅ Always instant
- ✅ No model downloads
- ✅ Consistent results

**Cons:**
- ⚠️ Lower quality (keyword-based unless you add ML.NET)
- ⚠️ No privacy (server processes data)
- ⚠️ Potential API costs

---

## Mode Comparison

| Feature | Adaptive | ClientOnly | ServerOnly |
|---------|----------|------------|------------|
| **First-time user** | Instant (server) | Wait 10-60s | Instant |
| **Return user** | Instant (client) | Instant (client) | Instant |
| **Quality** | Good → Excellent | Excellent | Good |
| **Privacy** | Medium → High | High | Low |
| **Reliability** | Excellent | Medium | Excellent |
| **Setup complexity** | Low | Medium (bundle models) | Low |

---

## Recommended Configurations

### Production (General Use)
```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "Adaptive"
    },
    "Embeddings": {
      "Mode": "Adaptive"
    }
  }
}
```

### Privacy-Critical (Healthcare, Finance)
```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "ClientOnly"
    },
    "Embeddings": {
      "Mode": "ClientOnly"
    }
  }
}
```

### Testing/Debugging
```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "ServerOnly"
    },
    "Embeddings": {
      "Mode": "ServerOnly"
    }
  }
}
```

### Mixed Mode (Privacy for Embeddings, Adaptive for Sentiment)
```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "Adaptive",
      "Description": "Keyword fallback is acceptable"
    },
    "Embeddings": {
      "Mode": "ClientOnly",
      "Description": "Never generate vectors on server"
    }
  }
}
```

---

## How to Change Modes

### Step 1: Edit appsettings.json

Change the `Mode` value:
```json
"Mode": "Adaptive"  // Change to "ClientOnly" or "ServerOnly"
```

### Step 2: Restart Application

```powershell
# Stop and restart
dotnet run --project Sivar.Os/Sivar.Os.csproj
```

### Step 3: Verify in Logs

Check console output:
```
[SentimentAnalysis] Service initialized with mode: Adaptive
```

### Step 4: Test

Create a post and check logs for routing:
```
[SentimentAnalysis.Adaptive] 🎯 Smart routing enabled
[SentimentAnalysis.Adaptive] ✅ Client models ready - using Transformers.js
```

Or:
```
[SentimentAnalysis.ClientOnly] Using client-side ML exclusively
```

Or:
```
[SentimentAnalysis.ServerOnly] Using server-side processing exclusively
```

---

## Troubleshooting

### Issue: "Models not ready" in ClientOnly mode

**Cause:** Models haven't finished downloading

**Solution:**
1. Wait 30-60 seconds after page load
2. Check browser console for model loading logs
3. Verify models are in `wwwroot/models/` if using local models
4. Switch to Adaptive mode temporarily

### Issue: Low quality in ServerOnly mode

**Cause:** Using keyword-based server fallback

**Solution:**
1. Add ML.NET to ServerSentimentAnalysisService
2. Or switch to Adaptive mode (uses client ML when ready)

### Issue: Always using server in Adaptive mode

**Cause:** Client models not loading

**Solution:**
1. Check browser console for JavaScript errors
2. Verify sentiment-analyzer.js is loaded
3. Check IndexedDB for cached models (DevTools → Application → IndexedDB)
4. Try clearing cache and reloading

---

## Advanced: Custom Mode Logic

If you need custom routing logic, modify `SentimentAnalysisService.cs`:

```csharp
public async Task<SentimentAnalysisResultDto> AnalyzeAsync(string text, string language)
{
    // Custom logic example: Use client-only for short text
    if (text.Length < 100)
    {
        return await AnalyzeClientOnlyAsync(text, language);
    }
    
    // Use configured mode for longer text
    return _mode switch
    {
        AIServiceMode.ClientOnly => await AnalyzeClientOnlyAsync(text, language),
        AIServiceMode.ServerOnly => await AnalyzeServerOnlyAsync(text, language),
        AIServiceMode.Adaptive => await AnalyzeAdaptiveAsync(text, language),
        _ => await AnalyzeAdaptiveAsync(text, language)
    };
}
```

---

## Files Modified

1. **appsettings.json** - Added `AIServices` configuration section
2. **AIServiceOptions.cs** - Configuration class with enum
3. **SentimentAnalysisService.cs** - Supports three modes
4. **ISentimentAnalysisService.cs** - Added `AreModelsReadyAsync()` method
5. **ClientSentimentAnalysisService.cs** - Implemented `AreModelsReadyAsync()`
6. **Program.cs** - Registered AIServiceOptions configuration
7. **DEVELOPMENT_RULES.md** - Full documentation of modes

---

## Next Steps

1. ✅ Test all three modes with post creation
2. ✅ Monitor logs to understand routing
3. ✅ Choose mode based on your requirements
4. 📝 Update documentation if you add ML.NET server-side
5. 🚀 Consider bundling quantized models for faster ClientOnly mode

---

**Last Updated:** October 31, 2025  
**Related Documentation:** DEVELOPMENT_RULES.md → Adaptive Loading Pattern
