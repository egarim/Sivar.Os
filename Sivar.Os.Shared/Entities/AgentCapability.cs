using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Defines what an AI agent can do - a capability/function that can be invoked.
/// Examples: SearchPosts, SearchProfiles, GetNearbyPlaces, GetDirections
/// </summary>
public class AgentCapability : BaseEntity
{
    /// <summary>
    /// Unique key identifier (e.g., "search_posts", "search_profiles", "get_nearby")
    /// </summary>
    [Required]
    [StringLength(50)]
    public virtual string Key { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the capability (e.g., "Search Posts", "Find Nearby Places")
    /// </summary>
    [Required]
    [StringLength(100)]
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of what this capability does.
    /// Used in system prompt generation.
    /// </summary>
    [Required]
    [StringLength(1000)]
    public virtual string Description { get; set; } = string.Empty;

    /// <summary>
    /// The actual function name to call (e.g., "SearchPosts", "SearchProfiles")
    /// Must match the registered tool/function in ChatFunctionService
    /// </summary>
    [Required]
    [StringLength(100)]
    public virtual string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// Category for grouping capabilities (e.g., "search", "navigation", "information")
    /// </summary>
    [StringLength(50)]
    public virtual string? Category { get; set; }

    /// <summary>
    /// Example queries that demonstrate this capability.
    /// Stored as JSON array: ["pizzerías cerca", "buscar restaurantes italianos"]
    /// </summary>
    [StringLength(2000)]
    public virtual string? ExampleQueriesJson { get; set; }

    /// <summary>
    /// Instructions for the AI on when/how to use this capability.
    /// Added to system prompt.
    /// </summary>
    [StringLength(2000)]
    public virtual string? UsageInstructions { get; set; }

    /// <summary>
    /// Whether this capability is currently enabled
    /// </summary>
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Sort order for display
    /// </summary>
    public virtual int SortOrder { get; set; } = 0;

    /// <summary>
    /// Icon for the capability (emoji or icon class)
    /// </summary>
    [StringLength(50)]
    public virtual string? Icon { get; set; }

    /// <summary>
    /// Parameters for this capability
    /// </summary>
    public virtual ObservableCollection<CapabilityParameter> Parameters { get; set; } = new ObservableCollection<CapabilityParameter>();
}
