using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Defines a parameter for an AgentCapability.
/// Used to describe what inputs a function accepts.
/// </summary>
public class CapabilityParameter : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent capability
    /// </summary>
    public virtual Guid CapabilityId { get; set; }

    /// <summary>
    /// Navigation property to parent capability
    /// </summary>
    public virtual AgentCapability? Capability { get; set; }

    /// <summary>
    /// Parameter name (e.g., "query", "latitude", "radius")
    /// </summary>
    [Required]
    [StringLength(50)]
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the parameter
    /// </summary>
    [StringLength(100)]
    public virtual string? DisplayName { get; set; }

    /// <summary>
    /// Description of what this parameter does
    /// </summary>
    [StringLength(500)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// Data type (string, number, boolean, array, object)
    /// </summary>
    [Required]
    [StringLength(20)]
    public virtual string DataType { get; set; } = "string";

    /// <summary>
    /// Whether this parameter is required
    /// </summary>
    public virtual bool IsRequired { get; set; } = false;

    /// <summary>
    /// Default value as JSON string
    /// </summary>
    [StringLength(500)]
    public virtual string? DefaultValue { get; set; }

    /// <summary>
    /// Allowed values as JSON array (for enum-like parameters)
    /// e.g., ["asc", "desc"] or ["date", "relevance", "distance"]
    /// </summary>
    [StringLength(1000)]
    public virtual string? AllowedValuesJson { get; set; }

    /// <summary>
    /// Sort order for display
    /// </summary>
    public virtual int SortOrder { get; set; } = 0;
}
