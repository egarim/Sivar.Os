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
        try
        {
            var config = GetRateLimitConfig(action);
            var cacheKey = GetCacheKey(userId, action);
            var windowStart = GetCurrentWindowStart(config.WindowMinutes);
            var fullCacheKey = $"{cacheKey}:{windowStart.Ticks}";

            if (_cache.TryGetValue(fullCacheKey, out int currentCount))
            {
                var isAllowed = currentCount < config.MaxRequests;
                
                if (!isAllowed)
                {
                    _logger.LogWarning("Rate limit exceeded for user {UserId} on action {Action}. Count: {CurrentCount}, Limit: {MaxRequests}", 
                        userId, action, currentCount, config.MaxRequests);
                }
                
                return Task.FromResult(isAllowed);
            }

            // No existing count means first request in this window
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for user {UserId} action {Action}", userId, action);
            // Fail open - allow request if there's an error
            return Task.FromResult(true);
        }
    }

    public Task IncrementAsync(string userId, string action)
    {
        try
        {
            var config = GetRateLimitConfig(action);
            var cacheKey = GetCacheKey(userId, action);
            var windowStart = GetCurrentWindowStart(config.WindowMinutes);
            var fullCacheKey = $"{cacheKey}:{windowStart.Ticks}";
            var windowEnd = windowStart.AddMinutes(config.WindowMinutes);

            var currentCount = 0;
            if (_cache.TryGetValue(fullCacheKey, out int existingCount))
            {
                currentCount = existingCount;
            }

            currentCount++;
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = windowEnd,
                Priority = CacheItemPriority.Normal
            };
            
            _cache.Set(fullCacheKey, currentCount, cacheOptions);
            
            _logger.LogDebug("Incremented rate limit counter for user {UserId} action {Action}: {CurrentCount}/{MaxRequests}", 
                userId, action, currentCount, config.MaxRequests);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing rate limit for user {UserId} action {Action}", userId, action);
            return Task.CompletedTask;
        }
    }

    public Task<bool> CheckAndIncrementAsync(string userId, string action)
    {
        try
        {
            var config = GetRateLimitConfig(action);
            var cacheKey = GetCacheKey(userId, action);
            var windowStart = GetCurrentWindowStart(config.WindowMinutes);
            var fullCacheKey = $"{cacheKey}:{windowStart.Ticks}";
            var windowEnd = windowStart.AddMinutes(config.WindowMinutes);

            var currentCount = 0;
            if (_cache.TryGetValue(fullCacheKey, out int existingCount))
            {
                currentCount = existingCount;
            }

            // Check if adding one more would exceed the limit
            if (currentCount >= config.MaxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for user {UserId} on action {Action}. Count: {CurrentCount}, Limit: {MaxRequests}", 
                    userId, action, currentCount, config.MaxRequests);
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
            
            _logger.LogDebug("Rate limit check passed and incremented for user {UserId} action {Action}: {CurrentCount}/{MaxRequests}", 
                userId, action, currentCount, config.MaxRequests);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckAndIncrementAsync for user {UserId} action {Action}", userId, action);
            // Fail open - allow request if there's an error
            return Task.FromResult(true);
        }
    }

    public Task<int> GetRemainingRequestsAsync(string userId, string action)
    {
        try
        {
            var config = GetRateLimitConfig(action);
            var cacheKey = GetCacheKey(userId, action);
            var windowStart = GetCurrentWindowStart(config.WindowMinutes);
            var fullCacheKey = $"{cacheKey}:{windowStart.Ticks}";

            if (_cache.TryGetValue(fullCacheKey, out int currentCount))
            {
                return Task.FromResult(Math.Max(0, config.MaxRequests - currentCount));
            }

            return Task.FromResult(config.MaxRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting remaining requests for user {UserId} action {Action}", userId, action);
            return Task.FromResult(0);
        }
    }

    public Task ResetRateLimitAsync(string userId, string action)
    {
        try
        {
            var config = GetRateLimitConfig(action);
            var cacheKey = GetCacheKey(userId, action);
            var windowStart = GetCurrentWindowStart(config.WindowMinutes);
            var fullCacheKey = $"{cacheKey}:{windowStart.Ticks}";
            
            _cache.Remove(fullCacheKey);
            
            _logger.LogInformation("Reset rate limit for user {UserId} action {Action}", userId, action);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit for user {UserId} action {Action}", userId, action);
            return Task.CompletedTask;
        }
    }

    private void InitializeRateLimitConfigs()
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

        _logger.LogInformation("Initialized rate limiting configurations: {Configs}", 
            string.Join(", ", _rateLimitConfigs.Select(kvp => $"{kvp.Key}:{kvp.Value.MaxRequests}/{kvp.Value.WindowMinutes}min")));
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