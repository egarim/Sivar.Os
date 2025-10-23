namespace Sivar.Os.Shared.Configuration
{
    /// <summary>
    /// Configuration for Blazor render modes.
    /// Defines which render mode strategy the application should use.
    /// </summary>
    public class RenderModeConfiguration
    {
        /// <summary>
        /// The default render mode for interactive components.
        /// 
        /// Possible values:
        /// - "InteractiveAuto" (recommended): Automatically selects between Server and WebAssembly based on capabilities
        /// - "InteractiveServer": All interactivity runs on the server (low bandwidth, real-time updates)
        /// - "InteractiveWebAssembly": All interactivity runs in the browser via WebAssembly (offline-capable, client-side)
        /// - "Static": Server-side rendering only, no interactivity
        /// </summary>
        public string Default { get; set; } = "InteractiveAuto";

        /// <summary>
        /// Optional: Override render mode for specific page types
        /// </summary>
        public string? WeatherPage { get; set; }

        /// <summary>
        /// Optional: Override render mode for counter pages
        /// </summary>
        public string? CounterPage { get; set; }

        /// <summary>
        /// Optional: Override render mode for home pages
        /// </summary>
        public string? HomePage { get; set; }
    }
}
