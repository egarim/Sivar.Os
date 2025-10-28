using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.Services;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Sivar.Os.Services;

/// <summary>
/// In-memory rate limiting service to prevent spam and abuse
/// </summary>
public class RateLimitingService : IRateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RateLimitingService> _logger;
    private readonly ConcurrentDictionary<string, RateLimitConfig> _rateLimitConfigs;
    
    public RateLimitingService(
        IMemoryCache cache, 
        IConfiguration configuration,
        ILogger<RateLimitingService> logger)
    {
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _rateLimitConfigs = new ConcurrentDictionary<string, RateLimitConfig>();
        
        InitializeRateLimitConfigs();
    }

    public Task<bool> CheckRateLimitAsync(string userId, string action)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[RateLimitingService.CheckRateLimitAsync] START - RequestId={RequestId}, Timestamp={Timestamp}, UserId={UserId}, Action={Action}",
            requestId, startTime, userId, action);

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("[RateLimitingService.CheckRateLimitAsync] VALIDATION ERROR - RequestId={RequestId}, UserIdNull=true",
                    requestId);
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(action))
            {
                _logger.LogError("[RateLimitingService.CheckRateLimitAsync] VALIDATION ERROR - RequestId={RequestId}, ActionNull=true",
                    requestId);
                throw new ArgumentException("Action cannot be null or empty", nameof(action));
            }

            var config = GetRateLimitConfig(action);
            var cacheKey = GetCacheKey(userId, action);
            var windowStart = GetCurrentWindowStart(config.WindowMinutes);
            var fullCacheKey = $"{cacheKey}:{windowStart.Ticks}";

            _logger.LogDebug("[RateLimitingService.CheckRateLimitAsync] Cache lookup - RequestId={RequestId}, CacheKey={CacheKey}, Action={Action}, MaxRequests={MaxRequests}, WindowMinutes={WindowMinutes}",
                requestId, cacheKey, action, config.MaxRequests, config.WindowMinutes);

            if (_cache.TryGetValue(fullCacheKey, out int currentCount))
            {
                var isAllowed = currentCount < config.MaxRequests;
                
                _logger.LogInformation("[RateLimitingService.CheckRateLimitAsync] Cache hit - RequestId={RequestId}, UserId={UserId}, Action={Action}, CurrentCount={CurrentCount}, MaxRequests={MaxRequests}, IsAllowed={IsAllowed}",
                    requestId, userId, action, currentCount, config.MaxRequests, isAllowed);

                if (!isAllowed)
                {
                    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    _logger.LogWarning("[RateLimitingService.CheckRateLimitAsync] RATE LIMIT EXCEEDED - RequestId={RequestId}, UserId={UserId}, Action={Action}, CurrentCount={CurrentCount}, Limit={Limit}, Duration={Duration}ms", 
                        requestId, userId, action, currentCount, config.MaxRequests, elapsed);
                }
                
                return Task.FromResult(isAllowed);
            }

            _logger.LogDebug("[RateLimitingService.CheckRateLimitAsync] Cache miss - first request in window - RequestId={RequestId}, UserId={UserId}, Action={Action}",
                requestId, userId, action);

            var elapsed2 = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[RateLimitingService.CheckRateLimitAsync] ALLOWED (first request) - RequestId={RequestId}, UserId={UserId}, Action={Action}, Duration={Duration}ms",
                requestId, userId, action, elapsed2);

            // No existing count means first request in this window
            return Task.FromResult(true);
        }
        catch (ArgumentException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[RateLimitingService.CheckRateLimitAsync] VALIDATION ERROR - RequestId={RequestId}, UserId={UserId}, Action={Action}, Duration={Duration}ms",
                requestId, userId, action, elapsed);
            // Fail open - allow request if there's a validation error
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[RateLimitingService.CheckRateLimitAsync] EXCEPTION - RequestId={RequestId}, UserId={UserId}, Action={Action}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, userId, action, ex.GetType().Name, elapsed);
            // Fail open - allow request if there's an error
            return Task.FromResult(true);
        }
    }

    public Task IncrementAsync(string userId, string action)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[RateLimitingService.IncrementAsync] START - RequestId={RequestId}, Timestamp={Timestamp}, UserId={UserId}, Action={Action}",
            requestId, startTime, userId, action);

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("[RateLimitingService.IncrementAsync] VALIDATION ERROR - RequestId={RequestId}, UserIdNull=true",
                    requestId);
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(action))
            {
                _logger.LogError("[RateLimitingService.IncrementAsync] VALIDATION ERROR - RequestId={RequestId}, ActionNull=true",
                    requestId);
                throw new ArgumentException("Action cannot be null or empty", nameof(action));
            }

            var config = GetRateLimitConfig(action);
            var cacheKey = GetCacheKey(userId, action);
            var windowStart = GetCurrentWindowStart(config.WindowMinutes);
            var fullCacheKey = $"{cacheKey}:{windowStart.Ticks}";
            var windowEnd = windowStart.AddMinutes(config.WindowMinutes);

            var currentCount = 0;
            if (_cache.TryGetValue(fullCacheKey, out int existingCount))
            {
                currentCount = existingCount;
                _logger.LogDebug("[RateLimitingService.IncrementAsync] Retrieved existing count - RequestId={RequestId}, Action={Action}, ExistingCount={ExistingCount}",
                    requestId, action, existingCount);
            }

            currentCount++;
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = windowEnd,
                Priority = CacheItemPriority.Normal
            };
            
            _cache.Set(fullCacheKey, currentCount, cacheOptions);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[RateLimitingService.IncrementAsync] SUCCESS - RequestId={RequestId}, UserId={UserId}, Action={Action}, NewCount={NewCount}, MaxRequests={MaxRequests}, Duration={Duration}ms",
                requestId, userId, action, currentCount, config.MaxRequests, elapsed);
            
            return Task.CompletedTask;
        }
        catch (ArgumentException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[RateLimitingService.IncrementAsync] VALIDATION ERROR - RequestId={RequestId}, UserId={UserId}, Action={Action}, Duration={Duration}ms",
                requestId, userId, action, elapsed);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[RateLimitingService.IncrementAsync] EXCEPTION - RequestId={RequestId}, UserId={UserId}, Action={Action}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, userId, action, ex.GetType().Name, elapsed);
            return Task.CompletedTask;
        }
    }

    public Task<bool> CheckAndIncrementAsync(string userId, string action)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[RateLimitingService.CheckAndIncrementAsync] START - RequestId={RequestId}, Timestamp={Timestamp}, UserId={UserId}, Action={Action}",
            requestId, startTime, userId, action);

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("[RateLimitingService.CheckAndIncrementAsync] VALIDATION ERROR - RequestId={RequestId}, UserIdNull=true",
                    requestId);
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(action))
            {
                _logger.LogError("[RateLimitingService.CheckAndIncrementAsync] VALIDATION ERROR - RequestId={RequestId}, ActionNull=true",
                    requestId);
                throw new ArgumentException("Action cannot be null or empty", nameof(action));
            }

            var config = GetRateLimitConfig(action);
            var cacheKey = GetCacheKey(userId, action);
            var windowStart = GetCurrentWindowStart(config.WindowMinutes);
            var fullCacheKey = $"{cacheKey}:{windowStart.Ticks}";
            var windowEnd = windowStart.AddMinutes(config.WindowMinutes);

            var currentCount = 0;
            if (_cache.TryGetValue(fullCacheKey, out int existingCount))
            {
                currentCount = existingCount;
                _logger.LogDebug("[RateLimitingService.CheckAndIncrementAsync] Retrieved existing count - RequestId={RequestId}, Action={Action}, Count={Count}",
                    requestId, action, existingCount);
            }

            // Check if adding one more would exceed the limit
            if (currentCount >= config.MaxRequests)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("[RateLimitingService.CheckAndIncrementAsync] RATE LIMIT EXCEEDED - RequestId={RequestId}, UserId={UserId}, Action={Action}, CurrentCount={CurrentCount}, Limit={Limit}, Duration={Duration}ms", 
                    requestId, userId, action, currentCount, config.MaxRequests, elapsed);
                return Task.FromResult(false);
            }

            // Increment the counter
            currentCount++;
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = windowEnd,
                Priority = CacheItemPriority.Normal
            };
            
            _cache.Set(fullCacheKey, currentCount, cacheOptions);

            var elapsed2 = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[RateLimitingService.CheckAndIncrementAsync] ALLOWED and INCREMENTED - RequestId={RequestId}, UserId={UserId}, Action={Action}, NewCount={NewCount}, MaxRequests={MaxRequests}, Duration={Duration}ms",
                requestId, userId, action, currentCount, config.MaxRequests, elapsed2);
            
            return Task.FromResult(true);
        }
        catch (ArgumentException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[RateLimitingService.CheckAndIncrementAsync] VALIDATION ERROR - RequestId={RequestId}, UserId={UserId}, Action={Action}, Duration={Duration}ms",
                requestId, userId, action, elapsed);
            // Fail open - allow request if there's a validation error
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[RateLimitingService.CheckAndIncrementAsync] EXCEPTION - RequestId={RequestId}, UserId={UserId}, Action={Action}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, userId, action, ex.GetType().Name, elapsed);
            // Fail open - allow request if there's an error
            return Task.FromResult(true);
        }
    }

    public Task<int> GetRemainingRequestsAsync(string userId, string action)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[RateLimitingService.GetRemainingRequestsAsync] START - RequestId={RequestId}, Timestamp={Timestamp}, UserId={UserId}, Action={Action}",
            requestId, startTime, userId, action);

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("[RateLimitingService.GetRemainingRequestsAsync] VALIDATION ERROR - RequestId={RequestId}, UserIdNull=true",
                    requestId);
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(action))
            {
                _logger.LogError("[RateLimitingService.GetRemainingRequestsAsync] VALIDATION ERROR - RequestId={RequestId}, ActionNull=true",
                    requestId);
                throw new ArgumentException("Action cannot be null or empty", nameof(action));
            }

            var config = GetRateLimitConfig(action);
            var cacheKey = GetCacheKey(userId, action);
            var windowStart = GetCurrentWindowStart(config.WindowMinutes);
            var fullCacheKey = $"{cacheKey}:{windowStart.Ticks}";

            var remaining = config.MaxRequests;
            if (_cache.TryGetValue(fullCacheKey, out int currentCount))
            {
                remaining = Math.Max(0, config.MaxRequests - currentCount);
                _logger.LogDebug("[RateLimitingService.GetRemainingRequestsAsync] Retrieved count - RequestId={RequestId}, Action={Action}, CurrentCount={CurrentCount}, Remaining={Remaining}",
                    requestId, action, currentCount, remaining);
            }

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[RateLimitingService.GetRemainingRequestsAsync] SUCCESS - RequestId={RequestId}, UserId={UserId}, Action={Action}, Remaining={Remaining}, Duration={Duration}ms",
                requestId, userId, action, remaining, elapsed);

            return Task.FromResult(remaining);
        }
        catch (ArgumentException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[RateLimitingService.GetRemainingRequestsAsync] VALIDATION ERROR - RequestId={RequestId}, UserId={UserId}, Action={Action}, Duration={Duration}ms",
                requestId, userId, action, elapsed);
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[RateLimitingService.GetRemainingRequestsAsync] EXCEPTION - RequestId={RequestId}, UserId={UserId}, Action={Action}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, userId, action, ex.GetType().Name, elapsed);
            return Task.FromResult(0);
        }
    }

    public Task ResetRateLimitAsync(string userId, string action)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[RateLimitingService.ResetRateLimitAsync] START - RequestId={RequestId}, Timestamp={Timestamp}, UserId={UserId}, Action={Action}",
            requestId, startTime, userId, action);

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("[RateLimitingService.ResetRateLimitAsync] VALIDATION ERROR - RequestId={RequestId}, UserIdNull=true",
                    requestId);
                throw new ArgumentException("UserId cannot be null or empty", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(action))
            {
                _logger.LogError("[RateLimitingService.ResetRateLimitAsync] VALIDATION ERROR - RequestId={RequestId}, ActionNull=true",
                    requestId);
                throw new ArgumentException("Action cannot be null or empty", nameof(action));
            }

            var config = GetRateLimitConfig(action);
            var cacheKey = GetCacheKey(userId, action);
            var windowStart = GetCurrentWindowStart(config.WindowMinutes);
            var fullCacheKey = $"{cacheKey}:{windowStart.Ticks}";
            
            _cache.Remove(fullCacheKey);

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[RateLimitingService.ResetRateLimitAsync] SUCCESS - RequestId={RequestId}, UserId={UserId}, Action={Action}, Duration={Duration}ms",
                requestId, userId, action, elapsed);

            return Task.CompletedTask;
        }
        catch (ArgumentException ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[RateLimitingService.ResetRateLimitAsync] VALIDATION ERROR - RequestId={RequestId}, UserId={UserId}, Action={Action}, Duration={Duration}ms",
                requestId, userId, action, elapsed);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[RateLimitingService.ResetRateLimitAsync] EXCEPTION - RequestId={RequestId}, UserId={UserId}, Action={Action}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, userId, action, ex.GetType().Name, elapsed);
            return Task.CompletedTask;
        }
    }

    private void InitializeRateLimitConfigs()
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("[RateLimitingService.InitializeRateLimitConfigs] START - RequestId={RequestId}, Timestamp={Timestamp}",
            requestId, startTime);

        try
        {
            // Default configurations
            _rateLimitConfigs["post_creation"] = new RateLimitConfig
            {
                MaxRequests = _configuration.GetValue<int>("RateLimiting:PostCreation:MaxRequests", 5),
                WindowMinutes = _configuration.GetValue<int>("RateLimiting:PostCreation:WindowMinutes", 1)
            };
            
            _rateLimitConfigs["comment_creation"] = new RateLimitConfig
            {
                MaxRequests = _configuration.GetValue<int>("RateLimiting:CommentCreation:MaxRequests", 10),
                WindowMinutes = _configuration.GetValue<int>("RateLimiting:CommentCreation:WindowMinutes", 1)
            };
            
            _rateLimitConfigs["reaction_creation"] = new RateLimitConfig
            {
                MaxRequests = _configuration.GetValue<int>("RateLimiting:ReactionCreation:MaxRequests", 20),
                WindowMinutes = _configuration.GetValue<int>("RateLimiting:ReactionCreation:WindowMinutes", 1)
            };
            
            _rateLimitConfigs["follow_action"] = new RateLimitConfig
            {
                MaxRequests = _configuration.GetValue<int>("RateLimiting:FollowAction:MaxRequests", 10),
                WindowMinutes = _configuration.GetValue<int>("RateLimiting:FollowAction:WindowMinutes", 5)
            };

            var configSummary = string.Join("; ", _rateLimitConfigs.Select(kvp => 
                $"{kvp.Key}: {kvp.Value.MaxRequests} requests/{kvp.Value.WindowMinutes}min"));

            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("[RateLimitingService.InitializeRateLimitConfigs] SUCCESS - RequestId={RequestId}, ConfigCount={ConfigCount}, Configs={Configs}, Duration={Duration}ms", 
                requestId, _rateLimitConfigs.Count, configSummary, elapsed);
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[RateLimitingService.InitializeRateLimitConfigs] EXCEPTION - RequestId={RequestId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                requestId, ex.GetType().Name, elapsed);
            throw;
        }
    }

    private RateLimitConfig GetRateLimitConfig(string action)
    {
        if (_rateLimitConfigs.TryGetValue(action, out var config))
        {
            return config;
        }

        // Default fallback config for unknown actions
        return new RateLimitConfig
        {
            MaxRequests = 5,
            WindowMinutes = 1
        };
    }

    private string GetCacheKey(string userId, string action)
    {
        return $"rate_limit:{userId}:{action}";
    }

    private DateTime GetCurrentWindowStart(int windowMinutes)
    {
        var now = DateTime.UtcNow;
        var windowStartMinute = (now.Minute / windowMinutes) * windowMinutes;
        return new DateTime(now.Year, now.Month, now.Day, now.Hour, windowStartMinute, 0, DateTimeKind.Utc);
    }

    private class RateLimitConfig
    {
        public int MaxRequests { get; set; }
        public int WindowMinutes { get; set; }
    }
}