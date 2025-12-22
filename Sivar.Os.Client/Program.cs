using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using MudBlazor.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Sivar.Os.Shared.Services;
using Sivar.Os.Client.Services;
using Sivar.Os.Client.Auth;
using Sivar.Os.Shared;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Client.Clients;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure MudBlazor services with Snackbar settings
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

// Add MudBlazor localization support with custom localizer
builder.Services.AddMudLocalization();
builder.Services.AddScoped<MudLocalizer, MudLocalizerService>();

// Configure DevExpress Blazor services
builder.Services.AddDevExpressBlazor(configure => 
{
    configure.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
    configure.SizeMode = DevExpress.Blazor.SizeMode.Medium;
});

// Register UnauthorizedRedirectHandler so we can centrally handle 401 responses
builder.Services.AddTransient<UnauthorizedRedirectHandler>();

// Configure JSON serialization options for matching server-side enum serialization
var jsonOptions = new System.Text.Json.JsonSerializerOptions
{
	PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
	DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());

// Configure HttpClient for WASM to include browser credentials (cookies) on every request
// The BaseAddress being set to the same origin means cookies will be included by default in Blazor
builder.Services.AddScoped(sp =>
{
	var httpClient = new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
	// Add header to indicate this is an XMLHttpRequest (helps with CORS and auth detection)
	httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
	return httpClient;
});

// Register ApiClient wrapper that centralizes 401 handling
builder.Services.AddScoped<ApiClient>();

// Register client-side authentication service
builder.Services.AddScoped<IAuthenticationService, ClientAuthenticationService>();

// Register client-side weather service
builder.Services.AddScoped<IWeatherService, ClientWeatherService>();

// Register profile switcher service
builder.Services.AddScoped<IProfileSwitcherService, ProfileSwitcherService>();

// Register browser permissions service for GPS and other browser APIs
builder.Services.AddScoped<BrowserPermissionsService>();

// Register chat location service for Phase 0: Location-Aware Chat
builder.Services.AddScoped<ChatLocationService>();

// Register profile context service for unified location/device/timezone context
builder.Services.AddScoped<IProfileContextService, ProfileContextService>();

// Register chat settings service for Phase 0.5: Configurable welcome messages
builder.Services.AddScoped<ChatSettingsService>();

// Register localization services
builder.Services.AddLocalization();

// Register culture service for multi-language support
builder.Services.AddScoped<ICultureService, CultureService>();

// Configure SivarClient options
builder.Services.Configure<SivarClientOptions>(options =>
{
    options.BaseUrl = builder.HostEnvironment.BaseAddress;
});

// Register individual API clients
builder.Services.AddScoped<IAuthClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new Sivar.Os.Client.Clients.AuthClient(httpClient, options.Value);
});

builder.Services.AddScoped<IUsersClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new UsersClient(httpClient, options.Value);
});

builder.Services.AddScoped<IProfilesClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new ProfilesClient(httpClient, options.Value);
});

builder.Services.AddScoped<IProfileTypesClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new ProfileTypesClient(httpClient, options.Value);
});

builder.Services.AddScoped<IPostsClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new PostsClient(httpClient, options.Value);
});

builder.Services.AddScoped<ICommentsClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new CommentsClient(httpClient, options.Value);
});

builder.Services.AddScoped<IReactionsClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new ReactionsClient(httpClient, options.Value);
});

builder.Services.AddScoped<IFollowersClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new FollowersClient(httpClient, options.Value);
});

builder.Services.AddScoped<INotificationsClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new NotificationsClient(httpClient, options.Value);
});

builder.Services.AddScoped<ISivarChatClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new ChatClient(httpClient, options.Value);
});

builder.Services.AddScoped<IFilesClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new FilesClient(httpClient, options.Value);
});

builder.Services.AddScoped<IContactsClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new ContactsClient(httpClient, options.Value);
});

// Register image compression service for optimizing uploads
builder.Services.AddScoped<IImageCompressionService, ImageCompressionService>();

// Register the aggregate SivarClient
builder.Services.AddScoped<ISivarClient>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var options = sp.GetRequiredService<IOptions<SivarClientOptions>>();
    return new Sivar.Os.Client.Clients.SivarClient(httpClient, options);
});

// For hybrid Auto mode: authentication is handled server-side via cookies
// Register custom WASM authentication state provider to fetch auth state from server
builder.Services.AddScoped<AuthenticationStateProvider, WasmAuthenticationStateProvider>();

// Add authorization services required by AuthorizeView component
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();

// Build the host
var host = builder.Build();

// Initialize culture from user preferences/browser before running the app
try
{
    var cultureService = host.Services.GetRequiredService<ICultureService>();
    var culture = await cultureService.GetEffectiveCultureAsync();
    
    // Set the culture for the current thread
    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
}
catch (Exception ex)
{
    // Log error but don't prevent app from starting
    Console.WriteLine($"Error initializing culture: {ex.Message}");
    // Fallback to default culture
    var defaultCulture = new CultureInfo("en-US");
    CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
    CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;
}

await host.RunAsync();
