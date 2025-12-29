namespace Sivar.Os.Shared.Framework;

/// <summary>
/// Feature flags for enabling/disabling framework features.
/// These allow gradual rollout of new patterns without breaking existing functionality.
/// </summary>
public class FrameworkFeatureFlags
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "FrameworkFeatures";
    
    /// <summary>
    /// Use the new framework-based navigation instead of hardcoded NavMenu.
    /// Default: false (use existing NavMenu)
    /// </summary>
    public bool UseFrameworkNavigation { get; set; } = false;
    
    /// <summary>
    /// Use the new framework-based context menus for posts.
    /// Default: false (use existing inline menus)
    /// </summary>
    public bool UseFrameworkMenus { get; set; } = false;
    
    /// <summary>
    /// Use the new action dispatcher for entity actions.
    /// Default: false (use existing inline handlers)
    /// </summary>
    public bool UseActionDispatcher { get; set; } = false;
}
