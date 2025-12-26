using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sivar.Os.Shared.Configuration;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// In-memory cache implementation used when Redis is disabled.
/// Uses IMemoryCache for fast, single-server caching.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly CachingConfiguration _config;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly HashSet<string> _keys = new();
    private readonly object _keysLock = new();

    public MemoryCacheService(
        IMemoryCache cache,
        IOptions<CachingConfiguration> config,
        ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _config = config.Value;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache HIT for key: {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogDebug("Cache MISS for key: {Key}", key);
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(_config.FeedCacheTTLMinutes)
        };

        _cache.Set(key, value, options);
        
        // Track keys for pattern-based removal
        lock (_keysLock)
        {
            _keys.Add(key);
        }

        _logger.LogDebug("Cache SET for key: {Key}, TTL: {TTL}", key, options.AbsoluteExpirationRelativeToNow);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        _cache.Remove(key);
        
        lock (_keysLock)
        {
            _keys.Remove(key);
        }

        _logger.LogDebug("Cache REMOVE for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Convert simple pattern (e.g., "feed:*") to matching logic
        var prefix = pattern.TrimEnd('*');
        var keysToRemove = new List<string>();

        lock (_keysLock)
        {
            foreach (var key in _keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(key);
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            lock (_keysLock)
            {
                _keys.Remove(key);
            }
        }

        _logger.LogDebug("Cache REMOVE BY PATTERN: {Pattern}, removed {Count} keys", pattern, keysToRemove.Count);
        return Task.CompletedTask;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var value = await factory();
        if (value != null)
        {
            await SetAsync(key, value, ttl, cancellationToken);
        }

        return value;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_cache.TryGetValue(key, out _));
    }
}
