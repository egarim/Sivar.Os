using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Shared.Repositories;

/// <summary>
/// Repository interface for AgentConfiguration operations
/// </summary>
public interface IAgentConfigurationRepository
{
    /// <summary>
    /// Get an agent configuration by its unique key
    /// </summary>
    Task<AgentConfiguration?> GetByKeyAsync(string agentKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active agent configurations
    /// </summary>
    Task<IEnumerable<AgentConfiguration>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all agent configurations (including inactive)
    /// </summary>
    Task<IEnumerable<AgentConfiguration>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the default/main agent configuration
    /// </summary>
    Task<AgentConfiguration?> GetDefaultAgentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Find agents that match the given intent based on their IntentPatterns
    /// Returns ordered by priority (highest first)
    /// </summary>
    Task<IEnumerable<AgentConfiguration>> GetByIntentMatchAsync(string userMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new agent configuration
    /// </summary>
    Task<AgentConfiguration> AddAsync(AgentConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing agent configuration
    /// Automatically increments version if SystemPrompt changes
    /// </summary>
    Task<AgentConfiguration> UpdateAsync(AgentConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete an agent configuration
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an agent key already exists
    /// </summary>
    Task<bool> ExistsAsync(string agentKey, CancellationToken cancellationToken = default);
}
