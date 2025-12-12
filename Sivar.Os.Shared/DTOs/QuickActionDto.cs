namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for Quick Action response
/// </summary>
public record QuickActionDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Display label (e.g., "🍕 Buscar comida")
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Icon (emoji)
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// MudBlazor icon class
    /// </summary>
    public string? MudBlazorIcon { get; init; }

    /// <summary>
    /// Button color
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Default query sent when clicked
    /// </summary>
    public string? DefaultQuery { get; init; }

    /// <summary>
    /// Context hint for AI
    /// </summary>
    public string? ContextHint { get; init; }

    /// <summary>
    /// Sort order
    /// </summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// Whether this action requires user location
    /// </summary>
    public bool RequiresLocation { get; init; }

    /// <summary>
    /// Whether this action is active/enabled
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Linked capability key (if any)
    /// </summary>
    public string? CapabilityKey { get; init; }
}

/// <summary>
/// DTO for creating a Quick Action
/// </summary>
public record CreateQuickActionDto
{
    public string Label { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public string? MudBlazorIcon { get; init; }
    public string? Color { get; init; }
    public string? DefaultQuery { get; init; }
    public string? ContextHint { get; init; }
    public int SortOrder { get; init; }
    public bool RequiresLocation { get; init; }
    public Guid? CapabilityId { get; init; }
}

/// <summary>
/// DTO for updating a Quick Action
/// </summary>
public record UpdateQuickActionDto
{
    public string? Label { get; init; }
    public string? Icon { get; init; }
    public string? MudBlazorIcon { get; init; }
    public string? Color { get; init; }
    public string? DefaultQuery { get; init; }
    public string? ContextHint { get; init; }
    public int? SortOrder { get; init; }
    public bool? RequiresLocation { get; init; }
    public bool? IsActive { get; init; }
    public Guid? CapabilityId { get; init; }
}
