# 🐛 ISSUE: Adaptive Mode Not Working for Sentiment Analysis

**Status:** 🔴 OPEN  
**Priority:** HIGH  
**Created:** October 31, 2025  
**Assigned:** TBD  

---

## Problem Description

The **Adaptive mode** for Sentiment Analysis is not working correctly. As a temporary workaround, we've switched to **ServerOnly mode** in `appsettings.json`.

**Current Configuration (Temporary):**
```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "ServerOnly",  // ⚠️ TEMPORARY WORKAROUND
      "Description": "TEMPORARY: Using ServerOnly due to Adaptive mode issue"
    }
  }
}
```

**Expected Configuration (After Fix):**
```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "Adaptive",  // ✅ Should work
      "Description": "Smart routing (client if ready, server fallback)"
    }
  }
}
```

---

## Expected Behavior

When `Mode: "Adaptive"`:

1. **First-time user (models not loaded):**
   - System calls `AreModelsReadyAsync()` → returns `false`
   - Routes to server-side keyword analysis
   - Returns instant result
   - Models download in background

2. **Return user (models cached):**
   - System calls `AreModelsReadyAsync()` → returns `true`
   - Routes to client-side Transformers.js analysis
   - Returns high-quality ML result

3. **Logs show routing decisions:**
   ```
   [SentimentAnalysis.Adaptive] 🎯 Smart routing enabled
   [SentimentAnalysis.Adaptive] 🔄 Client models not ready - using server-side
   ```
   Or:
   ```
   [SentimentAnalysis.Adaptive] ✅ Client models ready - using Transformers.js
   ```

---

## Actual Behavior

⚠️ **Unknown - needs investigation**

Possible symptoms:
- Models never load
- `AreModelsReadyAsync()` always returns false
- JavaScript interop failing
- Client-side service not initializing
- Routing logic not executing correctly

---

## Investigation Checklist

### 1. Check Client Models Loading
- [ ] Browser console shows `sentiment-analyzer.js` loaded successfully
- [ ] No 404 errors for model files
- [ ] IndexedDB shows cached models (DevTools → Application → IndexedDB)
- [ ] `SentimentAnalyzer.isReady()` returns `true` after ~30 seconds

### 2. Check JavaScript Interop
- [ ] `ClientSentimentAnalysisService.AreModelsReadyAsync()` calls JS correctly
- [ ] `IJSRuntime.InvokeAsync<bool>` doesn't throw exceptions
- [ ] Browser console logs from `SentimentAnalyzer.isReady()` visible

### 3. Check Service Routing
- [ ] `SentimentAnalysisService` constructor logs show: `[SentimentAnalysis] Service initialized with mode: Adaptive`
- [ ] `AnalyzeAdaptiveAsync()` method is being called
- [ ] Logs show routing decision: `✅ Client models ready` or `🔄 Client models not ready`

### 4. Check Configuration
- [ ] `AIServiceOptions` is properly registered in `Program.cs`
- [ ] `appsettings.json` has correct `AIServices.SentimentAnalysis.Mode` value
- [ ] Configuration binding works: `_options.Value.SentimentAnalysis.Mode == AIServiceMode.Adaptive`

---

## Debugging Steps

### Step 1: Enable Detailed Logging

In `appsettings.json`:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",  // Change from "Information"
      "Override": {
        "Sivar.Os.Services": "Debug"  // Add this
      }
    }
  }
}
```

### Step 2: Test Client Models Manually

Open browser console and run:
```javascript
// Check if sentiment analyzer is loaded
window.SentimentAnalyzer

// Check if models are ready
await window.SentimentAnalyzer.isReady()

// Try manual analysis
await window.SentimentAnalyzer.analyzeSentiment("This is a test", "en")
```

### Step 3: Add Breakpoints

In Visual Studio, add breakpoints to:
1. `SentimentAnalysisService.AnalyzeAdaptiveAsync()` - Line where it checks `AreModelsReadyAsync()`
2. `ClientSentimentAnalysisService.AreModelsReadyAsync()` - Line with `IJSRuntime.InvokeAsync`
3. `SentimentAnalysisService` constructor - Verify mode is Adaptive

### Step 4: Check DI Registration

Verify in `Program.cs`:
```csharp
// Should exist:
builder.Services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();
builder.Services.AddScoped<IClientSentimentAnalysisService, ClientSentimentAnalysisService>();
builder.Services.AddScoped<IServerSentimentAnalysisService, ServerSentimentAnalysisService>();
builder.Services.Configure<AIServiceOptions>(builder.Configuration.GetSection("AIServices"));
```

---

## Potential Root Causes

### Hypothesis 1: Models Never Load
**Cause:** Transformers.js failing to download/initialize models  
**Evidence:** Browser console shows errors, IndexedDB empty  
**Fix:** Check `sentiment-analyzer.js` paths, verify CDN access, check CORS

### Hypothesis 2: JavaScript Interop Timing Issue
**Cause:** `AreModelsReadyAsync()` called before JS loaded  
**Evidence:** `IJSRuntime` throws exception  
**Fix:** Add null checks, retry logic, or wait for Blazor render complete

### Hypothesis 3: Configuration Not Binding
**Cause:** `AIServiceOptions` not reading from appsettings.json  
**Evidence:** Mode is always default (0 = Adaptive) regardless of config  
**Fix:** Verify `IOptions<AIServiceOptions>` injection, check GetSection("AIServices")

### Hypothesis 4: Service Not Using Adaptive Logic
**Cause:** Code path not reaching `AnalyzeAdaptiveAsync()`  
**Evidence:** Logs never show "Smart routing enabled"  
**Fix:** Check switch statement in `AnalyzeAsync()`, verify _mode field set correctly

---

## Files to Review

1. **SentimentAnalysisService.cs** (Lines 1-150)
   - Check constructor sets `_mode` from options
   - Verify `AnalyzeAsync()` switch routes to `AnalyzeAdaptiveAsync()`
   - Check `AnalyzeAdaptiveAsync()` logic

2. **ClientSentimentAnalysisService.cs** (Lines 80-100)
   - Check `AreModelsReadyAsync()` implementation
   - Verify error handling doesn't swallow exceptions

3. **sentiment-analyzer.js** (Lines 1-200)
   - Check `isReady()` function exists and works
   - Verify models load correctly
   - Check initialization logic

4. **Program.cs** (Lines 120-130)
   - Verify `Configure<AIServiceOptions>` registration
   - Check all three sentiment services registered

5. **appsettings.json** (Lines 64-75)
   - Verify `AIServices` section exists
   - Check Mode is spelled correctly ("Adaptive" not "adaptive")

---

## Success Criteria

✅ **Issue is resolved when:**

1. Setting `Mode: "Adaptive"` in appsettings.json works correctly
2. First-time users see instant server-side results
3. After 30 seconds, users see client-side results
4. Logs show:
   ```
   [SentimentAnalysis] Service initialized with mode: Adaptive
   [SentimentAnalysis.Adaptive] 🎯 Smart routing enabled
   [SentimentAnalysis.Adaptive] 🔄 Client models not ready - using server-side
   [SentimentAnalysis.Adaptive] ✅ Client models ready - using Transformers.js
   ```
5. Browser console shows models loaded in IndexedDB
6. No JavaScript errors in browser console

---

## Workaround (Current)

**Temporary Solution:**  
Use `ServerOnly` mode until Adaptive is fixed.

**Impact:**
- ✅ System works (keyword-based sentiment)
- ⚠️ Lower quality results (no ML model)
- ⚠️ No progressive enhancement
- ⚠️ Client-side ML never used

**Change in appsettings.json:**
```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "ServerOnly"  // Using this until Adaptive works
    }
  }
}
```

---

## Testing Plan (After Fix)

### Test Case 1: First Visit (Cold Start)
1. Clear browser cache and IndexedDB
2. Open app in incognito window
3. Create post with text: "I am so happy today!"
4. **Expected:** Instant result (server-side keyword analysis)
5. **Check logs:** Should show `🔄 Client models not ready - using server-side`

### Test Case 2: Wait for Models
1. Wait 30 seconds
2. Create another post: "I am excited about this project!"
3. **Expected:** High-quality result (client-side ML)
4. **Check logs:** Should show `✅ Client models ready - using Transformers.js`

### Test Case 3: Return Visit (Warm Start)
1. Refresh page (models cached)
2. Create post immediately: "This is amazing!"
3. **Expected:** Instant high-quality result (client-side ML)
4. **Check IndexedDB:** Models still cached
5. **Check logs:** Should show `✅ Client models ready`

### Test Case 4: Mode Switching
1. Change to `"Mode": "ClientOnly"` in appsettings.json
2. Restart app
3. Try creating post before models load
4. **Expected:** Error message: "Models are loading..."
5. Change back to `"Mode": "Adaptive"`
6. **Expected:** Works immediately (server fallback)

---

## Related Files

- `SentimentAnalysisService.cs` - Main orchestrator with Adaptive logic
- `ClientSentimentAnalysisService.cs` - Client-side ML service
- `ServerSentimentAnalysisService.cs` - Server-side keyword fallback
- `ISentimentAnalysisService.cs` - Interface with `AreModelsReadyAsync()`
- `AIServiceOptions.cs` - Configuration class
- `sentiment-analyzer.js` - JavaScript Transformers.js wrapper
- `Program.cs` - DI registration
- `appsettings.json` - Mode configuration
- `DEVELOPMENT_RULES.md` - Documentation (Section 7: Adaptive Loading)
- `AI_SERVICE_MODES_GUIDE.md` - User guide for modes

---

## Notes

- **Embeddings** are still using Adaptive mode successfully (if that's working, compare implementation)
- Consider adding telemetry to track mode usage in production
- May need retry logic for model loading failures
- Consider adding UI indicator for "Models loading..." state

---

## Update Log

| Date | Update | By |
|------|--------|-----|
| 2025-10-31 | Issue created, switched to ServerOnly workaround | System |
| | | |
| | | |

---

**Last Updated:** October 31, 2025  
**Next Review:** When investigating Adaptive mode fix
