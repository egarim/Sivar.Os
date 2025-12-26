namespace Sivar.Os.Shared.Configuration;

/// <summary>
/// Configuration for caching layer (Redis or in-memory)
/// </summary>
public class CachingConfiguration
{
    /// <summary>
    /// Whether to use Redis for distributed caching.
    /// If false, uses in-memory cache (suitable for single-server deployments).
    /// </summary>
    public bool UseRedis { get; set; } = false;

    /// <summary>
    /// Redis connection configuration
    /// </summary>
    public RedisConfiguration Redis { get; set; } = new();

    /// <summary>
    /// Time-to-live for cached feed data (activity lists)
    /// </summary>
    public int FeedCacheTTLMinutes { get; set; } = 5;

    /// <summary>
    /// Time-to-live for cached post data
    /// </summary>
    public int PostCacheTTLMinutes { get; set; } = 10;

    /// <summary>
    /// Time-to-live for cached profile data
    /// </summary>
    public int ProfileCacheTTLMinutes { get; set; } = 15;

    /// <summary>
    /// Time-to-live for cached user data
    /// </summary>
    public int UserCacheTTLMinutes { get; set; } = 30;
}

/// <summary>
/// Redis-specific configuration
/// </summary>
public class RedisConfiguration
{
    /// <summary>
    /// Redis connection string (e.g., "localhost:6379" or "redis.example.com:6379,password=xxx")
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Instance name prefix for all cache keys (useful for multi-tenant or multi-app scenarios)
    /// </summary>
    public string InstanceName { get; set; } = "SivarOs:";

    /// <summary>
    /// Default TTL for cache entries when not specified
    /// </summary>
    public int DefaultTTLMinutes { get; set; } = 5;

    /// <summary>
    /// Enable SSL/TLS for Redis connection
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Sync timeout in milliseconds
    /// </summary>
    public int SyncTimeoutMs { get; set; } = 1000;
}
