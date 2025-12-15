using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

/// <summary>
/// Repository implementation for AgentConfiguration data access
/// </summary>
public class AgentConfigurationRepository : BaseRepository<AgentConfiguration>, IAgentConfigurationRepository
{
    public AgentConfigurationRepository(SivarDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<AgentConfiguration?> GetByKeyAsync(string agentKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.AgentKey == agentKey && !a.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AgentConfiguration>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.IsActive && !a.IsDeleted)
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AgentConfiguration>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AgentConfiguration?> GetDefaultAgentAsync(CancellationToken cancellationToken = default)
    {
        // Default agent is the active one with lowest priority (catch-all)
        // or specifically "sivar-main" if it exists
        var mainAgent = await _dbSet
            .Where(a => a.AgentKey == "sivar-main" && a.IsActive && !a.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (mainAgent != null)
            return mainAgent;

        // Fall back to lowest priority active agent
        return await _dbSet
            .Where(a => a.IsActive && !a.IsDeleted)
            .OrderBy(a => a.Priority)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AgentConfiguration>> GetByIntentMatchAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var activeAgents = await GetAllActiveAsync(cancellationToken);
        var matchingAgents = new List<AgentConfiguration>();

        foreach (var agent in activeAgents)
        {
            var patterns = agent.GetIntentPatterns();
            if (patterns.Count == 0)
                continue;

            foreach (var pattern in patterns)
            {
                try
                {
                    if (Regex.IsMatch(userMessage, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                    {
                        matchingAgents.Add(agent);
                        break; // One match is enough
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    // Skip patterns that take too long
                    continue;
                }
            }
        }

        return matchingAgents.OrderByDescending(a => a.Priority);
    }

    /// <inheritdoc />
    public new async Task<AgentConfiguration> AddAsync(AgentConfiguration config, CancellationToken cancellationToken = default)
    {
        config.CreatedAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;
        config.Version = 1;

        await _dbSet.AddAsync(config, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    /// <inheritdoc />
    public async Task<AgentConfiguration> UpdateAsync(AgentConfiguration config, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(config.Id);
        if (existing == null)
            throw new InvalidOperationException($"AgentConfiguration with ID {config.Id} not found");

        // Increment version if SystemPrompt changed
        if (existing.SystemPrompt != config.SystemPrompt)
        {
            config.Version = existing.Version + 1;
        }

        config.UpdatedAt = DateTime.UtcNow;
        
        _context.Entry(existing).CurrentValues.SetValues(config);
        await _context.SaveChangesAsync(cancellationToken);
        
        return existing;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var config = await GetByIdAsync(id);
        if (config == null)
            return false;

        config.IsDeleted = true;
        config.DeletedAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string agentKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(a => a.AgentKey == agentKey && !a.IsDeleted, cancellationToken);
    }
}
