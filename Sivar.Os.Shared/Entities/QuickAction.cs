using System.ComponentModel.DataAnnotations;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a quick action button shown in the chat interface.
/// Links to a ChatBotSettings and optionally to an AgentCapability.
/// </summary>
public class QuickAction : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent ChatBotSettings
    /// </summary>
    public virtual Guid ChatBotSettingsId { get; set; }

    /// <summary>
    /// Navigation property to parent settings
    /// </summary>
    public virtual ChatBotSettings? ChatBotSettings { get; set; }

    /// <summary>
    /// Foreign key to the capability this action triggers (optional)
    /// </summary>
    public virtual Guid? CapabilityId { get; set; }

    /// <summary>
    /// Navigation property to the capability
    /// </summary>
    public virtual AgentCapability? Capability { get; set; }

    /// <summary>
    /// Display label for the button (e.g., "🍕 Buscar comida")
    /// </summary>
    [Required]
    [StringLength(100)]
    public virtual string Label { get; set; } = string.Empty;

    /// <summary>
    /// Icon (emoji or MudBlazor icon class)
    /// </summary>
    [StringLength(50)]
    public virtual string? Icon { get; set; }

    /// <summary>
    /// MudBlazor icon class (e.g., "Icons.Material.Filled.Search")
    /// </summary>
    [StringLength(100)]
    public virtual string? MudBlazorIcon { get; set; }

    /// <summary>
    /// Background color for the button
    /// </summary>
    [StringLength(20)]
    public virtual string? Color { get; set; }

    /// <summary>
    /// The default query/message to send when clicked
    /// (e.g., "Buscar restaurantes cerca de mi ubicación")
    /// </summary>
    [StringLength(500)]
    public virtual string? DefaultQuery { get; set; }

    /// <summary>
    /// Additional context/hints for the AI when this action is triggered
    /// </summary>
    [StringLength(500)]
    public virtual string? ContextHint { get; set; }

    /// <summary>
    /// Sort order for display
    /// </summary>
    public virtual int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether this action is currently active
    /// </summary>
    public virtual bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this action requires user location
    /// </summary>
    public virtual bool RequiresLocation { get; set; } = false;
}
