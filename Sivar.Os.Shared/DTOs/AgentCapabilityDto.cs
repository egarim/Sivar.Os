namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for Agent Capability response
/// </summary>
public record AgentCapabilityDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Unique key (e.g., "search_posts")
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of what this capability does
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Function name to call
    /// </summary>
    public string FunctionName { get; init; } = string.Empty;

    /// <summary>
    /// Category (search, location, information, etc.)
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Icon
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Example queries
    /// </summary>
    public List<string> ExampleQueries { get; init; } = new();

    /// <summary>
    /// Usage instructions for AI
    /// </summary>
    public string? UsageInstructions { get; init; }

    /// <summary>
    /// Whether enabled
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Parameters for this capability
    /// </summary>
    public List<CapabilityParameterDto> Parameters { get; init; } = new();
}

/// <summary>
/// DTO for Capability Parameter
/// </summary>
public record CapabilityParameterDto
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Data type (string, number, boolean)
    /// </summary>
    public string DataType { get; init; } = "string";

    /// <summary>
    /// Whether required
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Default value
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Allowed values (for enum-like params)
    /// </summary>
    public List<string>? AllowedValues { get; init; }
}

/// <summary>
/// DTO for creating an Agent Capability
/// </summary>
public record CreateAgentCapabilityDto
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string FunctionName { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string? Icon { get; init; }
    public List<string>? ExampleQueries { get; init; }
    public string? UsageInstructions { get; init; }
    public List<CreateCapabilityParameterDto>? Parameters { get; init; }
}

/// <summary>
/// DTO for creating a Capability Parameter
/// </summary>
public record CreateCapabilityParameterDto
{
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string DataType { get; init; } = "string";
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public List<string>? AllowedValues { get; init; }
    public int SortOrder { get; init; }
}
