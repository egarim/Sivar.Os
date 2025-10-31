# Client-Side Vector Embeddings Implementation Plan

## 🎯 Objective

Implement a **hybrid embedding generation system** that:
1. **PRIMARY**: Generates embeddings client-side using Transformers.js (browser-based, free, private)
2. **FALLBACK**: Falls back to server-side generation if client-side fails (Azure OpenAI/Ollama)

This approach reduces server costs, improves performance, enhances privacy, and provides resilience.

---

## 📐 Architecture Overview

```
┌───────────────────────────────────────────────────────────┐
│  Client (Blazor WebAssembly)                              │
│                                                            │
│  User creates post with content                           │
│         ↓                                                  │
│  ┌──────────────────────────────────────────┐             │
│  │ Try Client-Side Embedding Generation     │             │
│  │ (Transformers.js + all-MiniLM-L6-v2)     │             │
│  └──────────────────────────────────────────┘             │
│         ↓                              ↓                   │
│    SUCCESS (float[])              FAILURE (null)          │
│         ↓                              ↓                   │
│         │                    ┌──────────────────┐         │
│         │                    │ Server Fallback  │         │
│         │                    │ (VectorEmbedding │         │
│         │                    │  Service)        │         │
│         │                    └──────────────────┘         │
│         ↓                              ↓                   │
│    Convert to PostgreSQL vector format                    │
│         ↓                                                  │
│    Send to Backend API (/api/posts)                       │
└───────────────────────────────────────────────────────────┘
                        ↓
┌───────────────────────────────────────────────────────────┐
│  Server (ASP.NET Core)                                    │
│                                                            │
│  Receive embedding vector as string                       │
│         ↓                                                  │
│  Save post to PostgreSQL                                  │
│         ↓                                                  │
│  Update ContentEmbedding via raw SQL                      │
│  (ExecuteSqlRawAsync with ::vector cast)                  │
│         ↓                                                  │
│  PostgreSQL pgvector (384-dimensional vector)             │
└───────────────────────────────────────────────────────────┘
```

---

## 🔧 Implementation Tasks

### Phase 1: JavaScript Infrastructure

#### Task 1.1: Add Transformers.js Library
**File**: `wwwroot/js/embeddings.js`

```javascript
// Import from CDN (or npm install @xenova/transformers)
import { pipeline } from 'https://cdn.jsdelivr.net/npm/@xenova/transformers@2.6.0';

let embeddingPipeline = null;

/**
 * Initialize the embedding model (lazy loading)
 * Model: Xenova/all-MiniLM-L6-v2 (384 dimensions, same as server-side)
 */
async function initializeModel() {
    if (!embeddingPipeline) {
        console.log('[ClientEmbeddings] Initializing model...');
        embeddingPipeline = await pipeline(
            'feature-extraction', 
            'Xenova/all-MiniLM-L6-v2'
        );
        console.log('[ClientEmbeddings] Model loaded successfully');
    }
}

/**
 * Generate embedding for text in the browser
 * @param {string} text - Text to embed
 * @returns {Promise<number[]|null>} - 384-dimensional embedding or null if failed
 */
export async function generateEmbedding(text) {
    try {
        if (!text || text.trim().length === 0) {
            console.warn('[ClientEmbeddings] Empty text provided');
            return null;
        }

        await initializeModel();
        
        console.log('[ClientEmbeddings] Generating embedding...');
        const output = await embeddingPipeline(text, {
            pooling: 'mean',
            normalize: true
        });

        // Convert to regular array for JSON serialization
        const embedding = Array.from(output.data);
        console.log('[ClientEmbeddings] Embedding generated:', embedding.length, 'dimensions');
        
        return embedding;
    } catch (error) {
        console.error('[ClientEmbeddings] Failed to generate embedding:', error);
        return null; // Trigger server-side fallback
    }
}

/**
 * Check if client-side embeddings are supported
 */
export async function isSupported() {
    try {
        // Check for WebAssembly support (required by transformers.js)
        if (typeof WebAssembly === 'undefined') {
            return false;
        }
        return true;
    } catch {
        return false;
    }
}
```

#### Task 1.2: Update Index.html
**File**: `wwwroot/index.html`

Add module support for ES6 imports:
```html
<!-- Before closing </body> -->
<script type="module" src="js/embeddings.js"></script>
```

---

### Phase 2: C# Services

#### Task 2.1: Create Client Embedding Service Interface
**File**: `Sivar.Os.Shared/Services/IClientEmbeddingService.cs`

```csharp
namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service for client-side embedding generation using Transformers.js
/// Returns null if client-side generation is unavailable or fails
/// </summary>
public interface IClientEmbeddingService
{
    /// <summary>
    /// Attempts to generate embedding on the client-side
    /// Returns null if unavailable, triggering server-side fallback
    /// </summary>
    /// <param name="text">Text to embed</param>
    /// <returns>384-dimensional embedding array or null if failed</returns>
    Task<float[]?> TryGenerateEmbeddingAsync(string text);

    /// <summary>
    /// Check if client-side embeddings are supported in current environment
    /// </summary>
    Task<bool> IsSupportedAsync();
}
```

#### Task 2.2: Implement Client Embedding Service
**File**: `Sivar.Os/Services/ClientEmbeddingService.cs`

```csharp
using Microsoft.JSInterop;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

public class ClientEmbeddingService : IClientEmbeddingService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ClientEmbeddingService> _logger;
    private IJSObjectReference? _module;
    private bool _isInitialized = false;
    private bool _isSupported = false;

    public ClientEmbeddingService(
        IJSRuntime jsRuntime, 
        ILogger<ClientEmbeddingService> logger)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            _logger.LogInformation("[ClientEmbedding] Initializing JS module...");
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/embeddings.js");
            
            _isSupported = await _module.InvokeAsync<bool>("isSupported");
            _isInitialized = true;
            
            _logger.LogInformation("[ClientEmbedding] Initialized. Supported: {IsSupported}", _isSupported);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ClientEmbedding] Failed to initialize");
            _isSupported = false;
            _isInitialized = true;
        }
    }

    public async Task<float[]?> TryGenerateEmbeddingAsync(string text)
    {
        try
        {
            await InitializeAsync();

            if (!_isSupported || _module == null)
            {
                _logger.LogWarning("[ClientEmbedding] Not supported, using server fallback");
                return null;
            }

            _logger.LogInformation("[ClientEmbedding] Generating client-side embedding...");
            var result = await _module.InvokeAsync<float[]?>("generateEmbedding", text);

            if (result != null && result.Length == 384)
            {
                _logger.LogInformation("[ClientEmbedding] Successfully generated {Dimensions}D embedding", result.Length);
                return result;
            }
            
            _logger.LogWarning("[ClientEmbedding] Invalid result, using server fallback");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ClientEmbedding] Failed to generate embedding, using server fallback");
            return null;
        }
    }

    public async Task<bool> IsSupportedAsync()
    {
        await InitializeAsync();
        return _isSupported;
    }
}
```

---

### Phase 3: Update Existing Services

#### Task 3.1: Update VectorEmbeddingService Interface
**File**: `Sivar.Os.Shared/Services/IVectorEmbeddingService.cs`

Add helper method for converting float array to PostgreSQL format:
```csharp
/// <summary>
/// Convert float array to PostgreSQL vector format
/// </summary>
string ToPostgresVector(float[] embedding);
```

#### Task 3.2: Implement Helper in VectorEmbeddingService
**File**: `Sivar.Os/Services/VectorEmbeddingService.cs`

```csharp
public string ToPostgresVector(float[] embedding)
{
    return "[" + string.Join(",", embedding) + "]";
}
```

#### Task 3.3: Update PostService with Hybrid Logic
**File**: `Sivar.Os/Services/PostService.cs`

Update constructor to inject `IClientEmbeddingService`:
```csharp
private readonly IClientEmbeddingService _clientEmbeddingService;

public PostService(
    // ... existing parameters
    IClientEmbeddingService clientEmbeddingService,
    IVectorEmbeddingService vectorEmbeddingService,
    // ... rest
)
{
    // ... existing assignments
    _clientEmbeddingService = clientEmbeddingService ?? throw new ArgumentNullException(nameof(clientEmbeddingService));
    _vectorEmbeddingService = vectorEmbeddingService ?? throw new ArgumentNullException(nameof(vectorEmbeddingService));
}
```

Update `CreatePostAsync` embedding generation:
```csharp
// Generate and save content embedding (HYBRID APPROACH)
try
{
    float[]? embedding = null;
    string vectorString;

    // 1. TRY CLIENT-SIDE FIRST
    _logger.LogInformation("[CreatePostAsync] Attempting client-side embedding generation for PostId={postId}", post.Id);
    embedding = await _clientEmbeddingService.TryGenerateEmbeddingAsync(post.Content);

    if (embedding != null)
    {
        // Client-side success
        _logger.LogInformation("[CreatePostAsync] Client-side embedding generated successfully");
        vectorString = _vectorEmbeddingService.ToPostgresVector(embedding);
    }
    else
    {
        // 2. FALLBACK TO SERVER-SIDE
        _logger.LogInformation("[CreatePostAsync] Client-side failed, using server-side embedding generation");
        var serverEmbedding = await _vectorEmbeddingService.GenerateEmbeddingAsync(post.Content);
        vectorString = _vectorEmbeddingService.ToPostgresVector(serverEmbedding);
    }

    // 3. SAVE TO DATABASE
    var embeddingUpdated = await _postRepository.UpdateContentEmbeddingAsync(post.Id, vectorString);
    if (embeddingUpdated)
    {
        _logger.LogInformation("[CreatePostAsync] Embedding saved successfully: PostId={postId}", post.Id);
    }
    else
    {
        _logger.LogWarning("[CreatePostAsync] Failed to save embedding for post: PostId={postId}", post.Id);
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, "[CreatePostAsync] Failed to generate/save embedding for post: PostId={postId}", post.Id);
    // Don't fail the post creation if embedding generation fails
}
```

---

### Phase 4: Configuration

#### Task 4.1: Add Configuration Settings
**File**: `appsettings.json`

```json
{
  "Embeddings": {
    "ClientSide": {
      "Enabled": true,
      "Model": "Xenova/all-MiniLM-L6-v2",
      "Timeout": 30000
    },
    "ServerSide": {
      "Provider": "AzureOpenAI",
      "Model": "text-embedding-3-small"
    }
  }
}
```

#### Task 4.2: Create Configuration Class
**File**: `Sivar.Os/Configuration/EmbeddingOptions.cs`

```csharp
namespace Sivar.Os.Configuration;

public class EmbeddingOptions
{
    public ClientSideOptions ClientSide { get; set; } = new();
    public ServerSideOptions ServerSide { get; set; } = new();
}

public class ClientSideOptions
{
    public bool Enabled { get; set; } = true;
    public string Model { get; set; } = "Xenova/all-MiniLM-L6-v2";
    public int Timeout { get; set; } = 30000;
}

public class ServerSideOptions
{
    public string Provider { get; set; } = "AzureOpenAI";
    public string Model { get; set; } = "text-embedding-3-small";
}
```

---

### Phase 5: Service Registration

#### Task 5.1: Register Services
**File**: `Program.cs`

```csharp
// Register embedding configuration
builder.Services.Configure<EmbeddingOptions>(
    builder.Configuration.GetSection("Embeddings"));

// Register client-side embedding service (Blazor WebAssembly only)
builder.Services.AddScoped<IClientEmbeddingService, ClientEmbeddingService>();

// Keep existing server-side service
builder.Services.AddScoped<IVectorEmbeddingService, VectorEmbeddingService>();
```

---

## 🧪 Testing Plan

### Test Case 1: Client-Side Success
1. Run application in browser with WebAssembly support
2. Create a post with content
3. Verify logs show "Client-side embedding generated successfully"
4. Verify embedding saved to database
5. Check browser DevTools console for Transformers.js logs

### Test Case 2: Client-Side Fallback
1. Simulate client-side failure (disable WebAssembly or throw error)
2. Create a post
3. Verify logs show "Client-side failed, using server-side"
4. Verify server-side embedding generated
5. Verify embedding saved to database

### Test Case 3: Configuration Toggle
1. Set `Embeddings:ClientSide:Enabled = false`
2. Create a post
3. Verify server-side is used directly

### Test Case 4: Semantic Search
1. Create multiple posts with embeddings
2. Use semantic search API
3. Verify results are ranked by similarity
4. Verify both client-side and server-side embeddings work in search

---

## 📊 Benefits Analysis

| Aspect | Client-Side | Server-Side | Hybrid |
|--------|-------------|-------------|--------|
| **Cost** | Free | $0.0001/1K tokens | Mostly free |
| **Speed** | ~1-2s | ~500ms + network | Best of both |
| **Privacy** | Full | Sent to API | Mostly private |
| **Reliability** | Browser-dependent | High | Very High |
| **Offline** | Yes (cached) | No | Partial |

---

## 🚀 Deployment Checklist

- [ ] JavaScript module created and tested
- [ ] C# services implemented
- [ ] Configuration added
- [ ] Service registration complete
- [ ] Unit tests for fallback logic
- [ ] Integration tests for both paths
- [ ] Performance monitoring added
- [ ] Documentation updated
- [ ] Browser compatibility tested (Chrome, Firefox, Edge, Safari)

---

## 📝 Notes

### Model Compatibility
- **Client-Side**: `all-MiniLM-L6-v2` (384 dimensions)
- **Server-Side**: `text-embedding-3-small` (384 dimensions configured)
- Both produce 384D vectors → compatible with existing pgvector schema

### Browser Support
- Requires WebAssembly support (available in all modern browsers)
- Model downloads to browser cache (~25MB first load)
- Subsequent loads are instant

### Fallback Triggers
- WebAssembly not supported
- Model loading fails
- Generation throws exception
- Timeout exceeded
- Client-side disabled in config

---

## 🔄 Future Enhancements

1. **Model Caching**: Pre-warm model on app load
2. **Progress Indicators**: Show "Generating embedding..." to user
3. **Batch Processing**: Generate embeddings for multiple posts
4. **Model Selection**: Allow users to choose model in settings
5. **Analytics**: Track client-side vs server-side usage
6. **A/B Testing**: Compare embedding quality between approaches

---

**Status**: Ready for implementation  
**Branch**: `feature/client-side-embeddings`  
**Estimated Effort**: 4-6 hours  
**Dependencies**: Transformers.js, JSRuntime, existing pgvector infrastructure
