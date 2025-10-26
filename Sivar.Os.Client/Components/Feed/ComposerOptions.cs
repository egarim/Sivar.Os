namespace Sivar.Os.Client.Components.Feed;

/// <summary>
/// Represents a post type option in the composer
/// </summary>
/// <param name="Id">Unique identifier for the option</param>
/// <param name="Label">Display label</param>
/// <param name="Icon">MudBlazor icon</param>
/// <param name="Tooltip">Tooltip text</param>
public record PostTypeOption(string Id, string Label, string Icon, string Tooltip);

/// <summary>
/// Represents an attachment option in the composer
/// </summary>
/// <param name="Id">Unique identifier for the option</param>
/// <param name="Label">Display label</param>
/// <param name="Icon">MudBlazor icon</param>
public record ComposerAttachmentOption(string Id, string Label, string Icon);
