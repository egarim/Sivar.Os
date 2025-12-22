using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Sivar.Os.Services.AgentFunctions;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Services;

/// <summary>
/// Factory implementation for creating AI agents from database configurations.
/// Manages caching, intent routing, and dynamic tool loading.
/// Phase 10: Multi-Agent Configuration
/// </summary>
public class AgentFactory : IAgentFactory
{
    private readonly IAgentConfigurationRepository _configRepo;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AgentFactory> _logger;
    private readonly ChatFunctionService _functionService;
    private readonly BookingFunctions _bookingFunctions;
    private readonly IChatClient _chatClient;
    private readonly ILoggerFactory _loggerFactory;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string AgentCachePrefix = "agent:";
    private const string AllConfigsCacheKey = "agent:all-configs";

    // Map of function names to their AITool instances
    private readonly Dictionary<string, AITool> _availableTools;

    public AgentFactory(
        IAgentConfigurationRepository configRepo,
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        ILogger<AgentFactory> logger,
        ChatFunctionService functionService,
        BookingFunctions bookingFunctions,
        IChatClient chatClient,
        ILoggerFactory loggerFactory)
    {
        _configRepo = configRepo ?? throw new ArgumentNullException(nameof(configRepo));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _functionService = functionService ?? throw new ArgumentNullException(nameof(functionService));
        _bookingFunctions = bookingFunctions ?? throw new ArgumentNullException(nameof(bookingFunctions));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        // Initialize the available tools map
        _availableTools = InitializeAvailableTools();
    }

    /// <inheritdoc />
    public async Task<AIAgent> GetAgentAsync(string agentKey)
    {
        var cacheKey = $"{AgentCachePrefix}{agentKey}";

        if (_cache.TryGetValue(cacheKey, out AIAgent? cached) && cached != null)
        {
            _logger.LogDebug("Returning cached agent: {AgentKey}", agentKey);
            return cached;
        }

        _logger.LogInformation("Building agent from database: {AgentKey}", agentKey);
        
        var config = await _configRepo.GetByKeyAsync(agentKey);
        if (config == null)
        {
            _logger.LogWarning("Agent '{AgentKey}' not found, falling back to default", agentKey);
            return await GetDefaultAgentAsync();
        }

        var agent = BuildAgent(config);
        _cache.Set(cacheKey, agent, CacheDuration);

        return agent;
    }

    /// <inheritdoc />
    public async Task<AIAgent> GetAgentForIntentAsync(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return await GetDefaultAgentAsync();

        var configs = await GetCachedConfigsAsync();
        
        // Find matching agents by intent pattern
        AgentConfiguration? bestMatch = null;
        
        foreach (var config in configs.Where(c => c.IsActive))
        {
            var patterns = config.GetIntentPatterns();
            if (patterns.Count == 0)
                continue;

            foreach (var pattern in patterns)
            {
                try
                {
                    if (Regex.IsMatch(userMessage, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                    {
                        if (bestMatch == null || config.Priority > bestMatch.Priority)
                        {
                            bestMatch = config;
                            _logger.LogDebug("Intent match: pattern '{Pattern}' matched for agent '{AgentKey}'", 
                                pattern, config.AgentKey);
                        }
                        break; // One match is enough for this config
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    _logger.LogWarning("Regex timeout for pattern: {Pattern}", pattern);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning("Invalid regex pattern '{Pattern}': {Error}", pattern, ex.Message);
                }
            }
        }

        if (bestMatch != null)
        {
            _logger.LogInformation("Routing to agent '{AgentKey}' (priority: {Priority})", 
                bestMatch.AgentKey, bestMatch.Priority);
            return await GetAgentAsync(bestMatch.AgentKey);
        }

        _logger.LogDebug("No intent match, using default agent");
        return await GetDefaultAgentAsync();
    }

    /// <inheritdoc />
    public async Task<AIAgent> GetDefaultAgentAsync()
    {
        var defaultConfig = await _configRepo.GetDefaultAgentAsync();
        
        if (defaultConfig == null)
        {
            _logger.LogWarning("No default agent found in database, using fallback");
            return BuildFallbackAgent();
        }

        return await GetAgentAsync(defaultConfig.AgentKey);
    }

    /// <inheritdoc />
    public async Task RefreshCacheAsync()
    {
        _logger.LogInformation("Refreshing agent cache");
        
        // Remove all agent entries from cache
        _cache.Remove(AllConfigsCacheKey);
        
        var configs = await _configRepo.GetAllActiveAsync();
        foreach (var config in configs)
        {
            _cache.Remove($"{AgentCachePrefix}{config.AgentKey}");
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAvailableToolNames()
    {
        return _availableTools.Keys.OrderBy(k => k);
    }

    /// <summary>
    /// Build an AIAgent from a database configuration
    /// </summary>
    private AIAgent BuildAgent(AgentConfiguration config)
    {
        // Get enabled tools for this agent
        var enabledToolNames = config.GetEnabledToolNames();
        var tools = new List<AITool>();

        foreach (var toolName in enabledToolNames)
        {
            if (_availableTools.TryGetValue(toolName, out var tool))
            {
                tools.Add(tool);
            }
            else
            {
                _logger.LogWarning("Tool '{ToolName}' not found for agent '{AgentKey}'", toolName, config.AgentKey);
            }
        }

        _logger.LogInformation("Building agent '{AgentKey}' with {ToolCount} tools (v{Version})", 
            config.AgentKey, tools.Count, config.Version);

        // For now, we use the default chat client
        // TODO: Support different providers (OpenAI, Azure, etc.) based on config.Provider
        return new ChatClientAgent(
            _chatClient,
            instructions: config.SystemPrompt,
            name: config.AgentKey,
            description: config.Description ?? config.DisplayName,
            tools: tools,
            loggerFactory: _loggerFactory);
    }

    /// <summary>
    /// Build a fallback agent when no configuration exists
    /// </summary>
    private AIAgent BuildFallbackAgent()
    {
        var allTools = _availableTools.Values.ToList();
        
        return new ChatClientAgent(
            _chatClient,
            instructions: @"You are Sivar, a helpful AI assistant for the Sivar.Os social network platform in El Salvador.
You can help users find businesses, search profiles, and explore the network.
Always respond in Spanish when the user writes in Spanish.
Be friendly and helpful.",
            name: "sivar-fallback",
            description: "Fallback AI assistant",
            tools: allTools,
            loggerFactory: _loggerFactory);
    }

    /// <summary>
    /// Get cached agent configurations
    /// </summary>
    private async Task<IEnumerable<AgentConfiguration>> GetCachedConfigsAsync()
    {
        if (_cache.TryGetValue(AllConfigsCacheKey, out IEnumerable<AgentConfiguration>? cached) && cached != null)
        {
            return cached;
        }

        var configs = await _configRepo.GetAllActiveAsync();
        _cache.Set(AllConfigsCacheKey, configs.ToList(), CacheDuration);
        
        return configs;
    }

    /// <summary>
    /// Initialize the map of available tools from ChatFunctionService
    /// </summary>
    private Dictionary<string, AITool> InitializeAvailableTools()
    {
        return new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            // Core search functions
            ["SearchProfiles"] = AIFunctionFactory.Create(_functionService.SearchProfiles),
            ["SearchPosts"] = AIFunctionFactory.Create(_functionService.SearchPosts),
            ["GetPostDetails"] = AIFunctionFactory.Create(_functionService.GetPostDetails),
            ["FindBusinesses"] = AIFunctionFactory.Create(_functionService.FindBusinesses),
            
            // Profile management functions
            ["FollowProfile"] = AIFunctionFactory.Create(_functionService.FollowProfile),
            ["UnfollowProfile"] = AIFunctionFactory.Create(_functionService.UnfollowProfile),
            ["GetMyProfile"] = AIFunctionFactory.Create(_functionService.GetMyProfile),
            
            // Location-based functions (PostGIS)
            ["SearchNearbyProfiles"] = AIFunctionFactory.Create(_functionService.SearchNearbyProfiles),
            ["SearchNearbyPosts"] = AIFunctionFactory.Create(_functionService.SearchNearbyPosts),
            ["CalculateDistance"] = AIFunctionFactory.Create(_functionService.CalculateDistance),
            ["GetAddressFromCoordinates"] = AIFunctionFactory.Create(_functionService.GetAddressFromCoordinates),
            ["GetCoordinatesFromAddress"] = AIFunctionFactory.Create(_functionService.GetCoordinatesFromAddress),
            ["SearchNearMe"] = AIFunctionFactory.Create(_functionService.SearchNearMe),
            ["GetCurrentLocationStatus"] = AIFunctionFactory.Create(_functionService.GetCurrentLocationStatus),
            
            // Phase 6: Intent-specific functions
            ["GetContactInfo"] = AIFunctionFactory.Create(_functionService.GetContactInfo),
            ["GetBusinessHours"] = AIFunctionFactory.Create(_functionService.GetBusinessHours),
            ["GetDirections"] = AIFunctionFactory.Create(_functionService.GetDirections),
            ["GetProcedureInfo"] = AIFunctionFactory.Create(_functionService.GetProcedureInfo),
            
            // Booking functions - enables reservations and appointments via chat
            ["SearchBookableResources"] = AIFunctionFactory.Create(_bookingFunctions.SearchBookableResources),
            ["GetResourceDetails"] = AIFunctionFactory.Create(_bookingFunctions.GetResourceDetails),
            ["GetAvailableSlots"] = AIFunctionFactory.Create(_bookingFunctions.GetAvailableSlots),
            ["CreateBooking"] = AIFunctionFactory.Create(_bookingFunctions.CreateBooking),
            ["GetMyUpcomingBookings"] = AIFunctionFactory.Create(_bookingFunctions.GetMyUpcomingBookings),
            ["GetBookingByConfirmationCode"] = AIFunctionFactory.Create(_bookingFunctions.GetBookingByConfirmationCode),
            ["CancelBooking"] = AIFunctionFactory.Create(_bookingFunctions.CancelBooking),
            ["GetBookingCategories"] = AIFunctionFactory.Create(_bookingFunctions.GetBookingCategories)
        };
    }
}
