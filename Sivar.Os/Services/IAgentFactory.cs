using Microsoft.Agents.AI;

namespace Sivar.Os.Services;

/// <summary>
/// Factory interface for creating AI agents from database configurations.
/// Supports dynamic agent loading, caching, and intent-based routing.
/// Phase 10: Multi-Agent Configuration
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    /// Get an agent by its unique key, building from database config.
    /// Results are cached for performance.
    /// </summary>
    /// <param name="agentKey">The unique agent key (e.g., "sivar-main")</param>
    /// <returns>The configured AI agent</returns>
    Task<AIAgent> GetAgentAsync(string agentKey);

    /// <summary>
    /// Get the best agent for a given user message based on intent patterns.
    /// Evaluates all active agents' IntentPatterns and returns highest priority match.
    /// Falls back to default agent if no match.
    /// </summary>
    /// <param name="userMessage">The user's message to analyze</param>
    /// <returns>The best matching AI agent</returns>
    Task<AIAgent> GetAgentForIntentAsync(string userMessage);

    /// <summary>
    /// Get the default/main agent (sivar-main or lowest priority)
    /// </summary>
    /// <returns>The default AI agent</returns>
    Task<AIAgent> GetDefaultAgentAsync();

    /// <summary>
    /// Refresh all cached agent configurations.
    /// Call this after updating agent configs in admin UI.
    /// </summary>
    Task RefreshCacheAsync();

    /// <summary>
    /// Get list of all available tool names that can be assigned to agents
    /// </summary>
    IEnumerable<string> GetAvailableToolNames();
}
