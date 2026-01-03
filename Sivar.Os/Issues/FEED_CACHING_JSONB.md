# Feed Performance Optimization Plan

## Overview
Implement two-tier caching strategy for feed performance:
1. **JSONB Snapshot** - Store PostDto as JSON in Activity table (primary optimization)
2. **Redis Cache** - Optional distributed cache for hot data (configurable)

---

## Phase 1: JSONB Snapshot on Activity

### 1.1 Database Schema Change
Add `PostSnapshot` column to Activity table:

```sql
ALTER TABLE "Activities" ADD COLUMN "PostSnapshot" JSONB;
CREATE INDEX idx_activities_post_snapshot ON "Activities" USING GIN ("PostSnapshot");
```

### 1.2 Entity Changes
**File:** `Sivar.Os.Shared/Entities/Activity.cs`

```csharp
/// <summary>
/// Denormalized PostDto snapshot for fast feed loading (no joins needed)
/// </summary>
public string? PostSnapshotJson { get; set; }
```

### 1.3 EF Configuration
**File:** `Sivar.Os.Data/Configurations/ActivityConfiguration.cs`

```csharp
builder.Property(a => a.PostSnapshotJson)
    .HasColumnName("PostSnapshot")
    .HasColumnType("jsonb");
```

### 1.4 Snapshot Population
**When to create/update snapshot:**
- `ActivityService.CreateActivityAsync()` - When creating "Create Post" activity
- `PostService.UpdatePostAsync()` - Update snapshot after post edit
- `PostService.DeletePostAsync()` - Clear snapshot when post deleted

### 1.5 Feed Loading Optimization
**File:** `Sivar.Os/Services/Clients/ActivitiesClient.cs`

```csharp
// Fast path: Use snapshot if available
if (!string.IsNullOrEmpty(activity.PostSnapshotJson))
{
    dto.Post = JsonSerializer.Deserialize<PostDto>(activity.PostSnapshotJson);
}
else
{
    // Fallback: Load from database (for old activities without snapshot)
    dto.Post = await _postService.GetPostByIdAsync(activity.ObjectId);
}
```

---

## Phase 2: Redis Cache (Optional)

### 2.1 Configuration
**File:** `appsettings.json`

```json
{
  "Caching": {
    "UseRedis": false,
    "Redis": {
      "ConnectionString": "localhost:6379",
      "InstanceName": "SivarOs:",
      "DefaultTTLMinutes": 5
    },
    "FeedCacheTTLMinutes": 5,
    "PostCacheTTLMinutes": 10,
    "ProfileCacheTTLMinutes": 15
  }
}
```

### 2.2 Configuration Class
**File:** `Sivar.Os.Shared/Configuration/CachingConfiguration.cs`

```csharp
public class CachingConfiguration
{
    public bool UseRedis { get; set; } = false;
    public RedisConfiguration Redis { get; set; } = new();
    public int FeedCacheTTLMinutes { get; set; } = 5;
    public int PostCacheTTLMinutes { get; set; } = 10;
    public int ProfileCacheTTLMinutes { get; set; } = 15;
}

public class RedisConfiguration
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "SivarOs:";
    public int DefaultTTLMinutes { get; set; } = 5;
}
```

### 2.3 Cache Service Interface
**File:** `Sivar.Os.Shared/Services/ICacheService.cs`

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;
}
```

### 2.4 Redis Implementation
**File:** `Sivar.Os/Services/RedisCacheService.cs`

```csharp
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly CachingConfiguration _config;
    private readonly ILogger<RedisCacheService> _logger;
    
    // Implementation using StackExchange.Redis via IDistributedCache
}
```

### 2.5 Memory Cache Fallback
**File:** `Sivar.Os/Services/MemoryCacheService.cs`

```csharp
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    // In-memory implementation for when Redis is disabled
}
```

### 2.6 DI Registration
**File:** `Sivar.Os/Program.cs`

```csharp
// Add caching configuration
var cachingConfig = builder.Configuration.GetSection("Caching").Get<CachingConfiguration>() 
    ?? new CachingConfiguration();
builder.Services.Configure<CachingConfiguration>(builder.Configuration.GetSection("Caching"));

if (cachingConfig.UseRedis)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = cachingConfig.Redis.ConnectionString;
        options.InstanceName = cachingConfig.Redis.InstanceName;
    });
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}
else
{
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
}
```

### 2.7 NuGet Package
```xml
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="9.0.0" />
```

---

## Phase 3: Integration

### 3.1 Cache Keys Strategy
```
feed:activities:{profileId}:{page}     → List<ActivityDto>
feed:post:{postId}                     → PostDto
feed:profile:{profileId}               → ProfileDto
feed:user:{keycloakId}                 → UserDto
```

### 3.2 Cache Invalidation
**On Post Create:**
- Invalidate `feed:activities:{authorProfileId}:*`

**On Post Update:**
- Invalidate `feed:post:{postId}`
- Update Activity.PostSnapshotJson

**On Post Delete:**
- Invalidate `feed:post:{postId}`
- Invalidate `feed:activities:{authorProfileId}:*`

**On Follow/Unfollow:**
- Invalidate `feed:activities:{followerProfileId}:*`

### 3.3 ActivitiesClient Caching
```csharp
public async Task<ActivityFeedDto> GetFeedActivitiesAsync(...)
{
    var cacheKey = $"feed:activities:{profileId}:{pageNumber}";
    
    return await _cacheService.GetOrSetAsync(cacheKey, async () =>
    {
        // Load from DB with JSONB snapshots
        var activities = await _activityService.GetFeedActivitiesAsync(...);
        return MapToFeedDto(activities);
    }, TimeSpan.FromMinutes(_config.FeedCacheTTLMinutes));
}
```

---

## Execution Order

### Step 1: Configuration (5 min)
- [ ] Create `CachingConfiguration.cs`
- [ ] Add config to `appsettings.json`

### Step 2: Cache Service (15 min)
- [ ] Create `ICacheService.cs` interface
- [ ] Create `MemoryCacheService.cs` implementation
- [ ] Create `RedisCacheService.cs` implementation
- [ ] Register in DI

### Step 3: JSONB Snapshot (20 min)
- [ ] Add `PostSnapshotJson` to Activity entity
- [ ] Add EF configuration
- [ ] Create migration
- [ ] Apply migration

### Step 4: Snapshot Population (15 min)
- [ ] Update `ActivityService.CreateActivityAsync` to store snapshot
- [ ] Update `PostService.UpdatePostAsync` to refresh snapshot
- [ ] Add helper method to serialize PostDto to JSON

### Step 5: Feed Loading Optimization (10 min)
- [ ] Update `ActivitiesClient.GetFeedActivitiesAsync` to use snapshot
- [ ] Add cache layer with `ICacheService`
- [ ] Add cache invalidation on write operations

### Step 6: Testing (10 min)
- [ ] Test with Redis disabled (memory cache)
- [ ] Test with Redis enabled (if available)
- [ ] Verify feed loading time < 500ms

---

## Expected Results

| Metric | Before | After (JSONB) | After (JSONB + Redis) |
|--------|--------|---------------|----------------------|
| DB Queries | 40+ | 1-3 | 0 (cache hit) |
| Feed Load Time | 2-5s | 200-400ms | 50-100ms |
| First Load | 2-5s | 200-400ms | 200-400ms |
| Subsequent Loads | 2-5s | 200-400ms | 50-100ms |

---

## Rollback Plan

If issues occur:
1. Set `Caching:UseRedis = false` → Falls back to memory cache
2. If JSONB causes issues, the code has fallback to load from DB
3. Migration can be reverted with `dotnet ef migrations remove`

---

## Ready to Execute?

Reply "go" to start implementation from Step 1.
