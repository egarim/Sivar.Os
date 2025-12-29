using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Sivar.Os.Shared.Framework.Navigation;

namespace Sivar.Os.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        
        // Add MudBlazor services
        builder.Services.AddMudServices();
        
        // Add the navigation, menu, and action frameworks
        builder.Services.AddNavigationFramework();
        
        // Add HTTP client for API communication
        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri("https://api.sivaros.com/") // Configure your API URL
        });
        
        // Register shared services from Sivar.Os.Client
        // These will be configured to work with the MAUI platform
        
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
