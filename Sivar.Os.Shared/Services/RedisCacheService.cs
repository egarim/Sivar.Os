using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sivar.Os.Shared.Configuration;
using StackExchange.Redis;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Redis cache implementation for distributed caching.
/// Used when CachingConfiguration.UseRedis is true.
/// </summary>
public class RedisCacheService : ICacheService, IDisposable
{
    private readonly CachingConfiguration _config;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly Lazy<ConnectionMultiplexer> _lazyConnection;
    private readonly JsonSerializerOptions _jsonOptions;

    private ConnectionMultiplexer Connection => _lazyConnection.Value;
    private IDatabase Database => Connection.GetDatabase();
    private string KeyPrefix => _config.Redis?.InstanceName ?? "SivarOs:";

    public RedisCacheService(
        IOptions<CachingConfiguration> config,
        ILogger<RedisCacheService> logger)
    {
        _config = config.Value;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var connectionString = _config.Redis?.ConnectionString ?? "localhost:6379";
        _lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            try
            {
                _logger.LogInformation("Connecting to Redis: {ConnectionString}", connectionString);
                return ConnectionMultiplexer.Connect(connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis. Falling back to no-op cache.");
                throw;
            }
        });
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var fullKey = $"{KeyPrefix}{key}";
            var value = await Database.StringGetAsync(fullKey);
            
            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Redis cache MISS for key: {Key}", fullKey);
                return null;
            }

            _logger.LogDebug("Redis cache HIT for key: {Key}", fullKey);
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var fullKey = $"{KeyPrefix}{key}";
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var expiry = ttl ?? TimeSpan.FromMinutes(_config.Redis?.DefaultTTLMinutes ?? 5);

            await Database.StringSetAsync(fullKey, json, expiry);
            _logger.LogDebug("Redis cache SET for key: {Key}, TTL: {TTL}", fullKey, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var fullKey = $"{KeyPrefix}{key}";
            await Database.KeyDeleteAsync(fullKey);
            _logger.LogDebug("Redis cache REMOVE for key: {Key}", fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE failed for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var fullPattern = $"{KeyPrefix}{pattern}";
            var server = Connection.GetServers().FirstOrDefault();
            
            if (server == null)
            {
                _logger.LogWarning("No Redis server available for pattern removal");
                return;
            }

            var keys = server.Keys(pattern: fullPattern).ToArray();
            
            if (keys.Length > 0)
            {
                await Database.KeyDeleteAsync(keys);
                _logger.LogDebug("Redis cache REMOVE BY PATTERN: {Pattern}, removed {Count} keys", fullPattern, keys.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE BY PATTERN failed for pattern: {Pattern}", pattern);
        }
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

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var fullKey = $"{KeyPrefix}{key}";
            return await Database.KeyExistsAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis EXISTS failed for key: {Key}", key);
            return false;
        }
    }

    public void Dispose()
    {
        if (_lazyConnection.IsValueCreated)
        {
            _lazyConnection.Value.Dispose();
        }
    }
}
