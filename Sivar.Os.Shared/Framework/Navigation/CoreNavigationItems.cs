namespace Sivar.Os.Shared.Framework.Navigation;

/// <summary>
/// Core navigation items for the Sivar.Os application.
/// These are the default navigation items shared between Web and Mobile.
/// </summary>
public static class CoreNavigationItems
{
    /// <summary>
    /// Home feed - main landing page for authenticated users.
    /// </summary>
    public static readonly NavigationItem Home = new()
    {
        Id = "home",
        Title = "Home",
        TitleKey = "Home",
        Icon = "Home",
        Route = "/app/home",
        Order = 10,
        RequiresAuth = true,
        Platform = PlatformType.All
    };
    
    /// <summary>
    /// Search page for discovering content and profiles.
    /// </summary>
    public static readonly NavigationItem Search = new()
    {
        Id = "search",
        Title = "Search",
        TitleKey = "Search",
        Icon = "Search",
        Route = "/app/search",
        Order = 20,
        RequiresAuth = true,
        Platform = PlatformType.All
    };
    
    /// <summary>
    /// My Schedule - business profile schedule management.
    /// Only visible to Business profile types.
    /// </summary>
    public static readonly NavigationItem MySchedule = new()
    {
        Id = "schedule",
        Title = "Schedule",
        TitleKey = "Schedule",
        Icon = "EventNote",
        Route = "/app/schedule",
        Order = 30,
        RequiresAuth = true,
        RequiredProfileTypes = new[] { "Business" },
        Platform = PlatformType.All
    };
    
    /// <summary>
    /// Bookings - user's booked appointments.
    /// Hidden from Business profiles (they use My Schedule instead).
    /// </summary>
    public static readonly NavigationItem Bookings = new()
    {
        Id = "bookings",
        Title = "Bookings",
        TitleKey = "Bookings",
        Icon = "EventAvailable",
        Route = "/app/bookings",
        Order = 30,
        RequiresAuth = true,
        Platform = PlatformType.All,
        IsVisible = ctx => ctx.ActiveProfile?.ProfileType?.Name != "Business"
    };
    
    /// <summary>
    /// AI Chat - opens the AI assistant.
    /// This is an action, not a route navigation.
    /// </summary>
    public static readonly NavigationItem Chat = new()
    {
        Id = "chat",
        Title = "Chat",
        TitleKey = "Chat",
        Icon = "SmartToy",
        Route = null,
        Order = 40,
        RequiresAuth = true,
        IsAction = true,
        ActionId = "toggle-ai-chat",
        Platform = PlatformType.All
    };
    
    /// <summary>
    /// Explore - public discovery page (mobile-specific or anonymous).
    /// </summary>
    public static readonly NavigationItem Explore = new()
    {
        Id = "explore",
        Title = "Explore",
        TitleKey = "Explore",
        Icon = "Explore",
        Route = "/app/explore",
        Order = 15,
        RequiresAuth = false,
        Platform = PlatformType.Mobile
    };
    
    /// <summary>
    /// Profile - view own profile.
    /// </summary>
    public static readonly NavigationItem Profile = new()
    {
        Id = "profile",
        Title = "Profile",
        TitleKey = "Profile",
        Icon = "Person",
        Route = "/app/profile",
        Order = 50,
        RequiresAuth = true,
        Platform = PlatformType.Mobile // Only in mobile nav, web uses ProfileSwitcher
    };
    
    /// <summary>
    /// Gets all core navigation items for registration.
    /// </summary>
    public static IEnumerable<NavigationItem> All => new[]
    {
        Home,
        Search,
        MySchedule,
        Bookings,
        Chat,
        Explore,
        Profile
    };
    
    /// <summary>
    /// Gets navigation items for authenticated users on Web.
    /// </summary>
    public static IEnumerable<NavigationItem> WebAuthenticated => new[]
    {
        Home,
        Search,
        MySchedule,
        Bookings,
        Chat
    };
    
    /// <summary>
    /// Gets navigation items for authenticated users on Mobile.
    /// </summary>
    public static IEnumerable<NavigationItem> MobileAuthenticated => new[]
    {
        Home,
        Explore,
        Search,
        Bookings,
        Profile
    };
}
