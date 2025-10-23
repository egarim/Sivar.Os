using System;
using System.Threading.Tasks;

namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service for managing rate limits to prevent spam and abuse
/// </summary>
public interface IRateLimitingService
{
    /// <summary>
    /// Check if a user can perform an action based on rate limits
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="action">Action being performed (e.g., "post_creation", "comment_creation")</param>
    /// <returns>True if action is allowed, false if rate limit exceeded</returns>
    Task<bool> CheckRateLimitAsync(string userId, string action);
    
    /// <summary>
    /// Increment the counter for a user's action
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="action">Action being performed</param>
    Task IncrementAsync(string userId, string action);
    
    /// <summary>
    /// Check rate limit and increment counter in one operation
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="action">Action being performed</param>
    /// <returns>True if action is allowed and counter incremented, false if rate limit exceeded</returns>
    Task<bool> CheckAndIncrementAsync(string userId, string action);
    
    /// <summary>
    /// Get remaining requests for a user's action within the current time window
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="action">Action being performed</param>
    /// <returns>Number of requests remaining in current time window</returns>
    Task<int> GetRemainingRequestsAsync(string userId, string action);
    
    /// <summary>
    /// Reset rate limit for a specific user and action (admin function)
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="action">Action to reset</param>
    Task ResetRateLimitAsync(string userId, string action);
}