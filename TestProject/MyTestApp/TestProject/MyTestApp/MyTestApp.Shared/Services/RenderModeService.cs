using Microsoft.Extensions.Configuration;
using MyTestApp.Shared.Configuration;

namespace MyTestApp.Shared.Services
{
    /// <summary>
    /// Service for managing render mode configuration.
    /// Reads from IConfiguration and provides the configured render mode.
    /// </summary>
    public class RenderModeService
    {
        private readonly IConfiguration _configuration;
        private readonly string _defaultRenderMode;

        public RenderModeService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // Bind configuration to our object
            var renderModeConfig = new RenderModeConfiguration();
            _configuration.GetSection("RenderMode").Bind(renderModeConfig);
            
            _defaultRenderMode = renderModeConfig.Default ?? "InteractiveAuto";
        }

        /// <summary>
        /// Gets the configured default render mode.
        /// </summary>
        public string GetDefaultRenderMode()
        {
            return _defaultRenderMode;
        }

        /// <summary>
        /// Gets the render mode for a specific page, falls back to default if not configured.
        /// </summary>
        public string GetRenderModeForPage(string pageName)
        {
            var config = new RenderModeConfiguration();
            _configuration.GetSection("RenderMode").Bind(config);

            return pageName.ToLower() switch
            {
                "weather" => config.WeatherPage ?? _defaultRenderMode,
                "counter" => config.CounterPage ?? _defaultRenderMode,
                "home" => config.HomePage ?? _defaultRenderMode,
                _ => _defaultRenderMode
            };
        }

        /// <summary>
        /// Gets a human-readable description of the render mode.
        /// </summary>
        public string GetRenderModeDescription(string renderMode)
        {
            return renderMode.ToLower() switch
            {
                "interactiveauto" => "🔄 Auto (Server → WASM)",
                "interactiveserver" => "🖥️ Server (Connected)",
                "interactivewebassembly" => "🌐 WASM (Offline Capable)",
                "static" => "📄 Static (No Interactivity)",
                _ => "❓ Unknown"
            };
        }

        /// <summary>
        /// Validates that the render mode string is valid.
        /// </summary>
        public bool IsValidRenderMode(string renderMode)
        {
            return renderMode.ToLower() switch
            {
                "interactiveauto" or
                "interactiveserver" or
                "interactivewebassembly" or
                "static" => true,
                _ => false
            };
        }
    }
}
