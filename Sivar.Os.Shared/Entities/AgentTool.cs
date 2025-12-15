using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Registry of available tools/functions that can be assigned to agents.
/// Used to manage which tools are available and their metadata.
/// </summary>
public class AgentTool : BaseEntity
{
    /// <summary>
    /// Unique function name (must match the actual code function name)
    /// Example: "SearchProfiles", "GetPostDetails", "SetCurrentLocation"
    /// </summary>
    [Required]
    [StringLength(100)]
    public virtual string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for admin UI
    /// </summary>
    [Required]
    [StringLength(100)]
    public virtual string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the tool does (shown to AI and in admin UI)
    /// </summary>
    [StringLength(500)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// Category for grouping in admin UI
    /// Examples: "Search", "Profile", "Post", "Location", "Content"
    /// </summary>
    [StringLength(50)]
    public virtual string Category { get; set; } = "General";

    /// <summary>
    /// JSON schema for parameters (for documentation and validation)
    /// Follows JSON Schema format
    /// </summary>
    [Column(TypeName = "jsonb")]
    public virtual JsonDocument? ParameterSchema { get; set; }

    /// <summary>
    /// Is this tool available for assignment to agents?
    /// </summary>
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Does this tool require specific permissions to use?
    /// Example: "admin", "moderator", null (no permission required)
    /// </summary>
    [StringLength(100)]
    public virtual string? RequiredPermission { get; set; }

    /// <summary>
    /// Sort order for display in admin UI
    /// </summary>
    public virtual int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether this tool makes external API calls
    /// Useful for rate limiting and monitoring
    /// </summary>
    public virtual bool IsExternalCall { get; set; } = false;

    /// <summary>
    /// Average execution time in milliseconds (for monitoring)
    /// Updated by analytics
    /// </summary>
    public virtual int? AvgExecutionTimeMs { get; set; }
}
