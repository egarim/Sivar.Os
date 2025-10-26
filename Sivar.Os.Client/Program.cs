using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Sivar.Os.Shared.Services;
using Sivar.Os.Client.Services;
using Sivar.Os.Client.Auth;
using Sivar.Os.Shared;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Client.Clients;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();

// Register UnauthorizedRedirectHandler so we can centrally handle 401 responses
builder.Services.AddTransient<UnauthorizedRedirectHandler>();

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

await builder.Build().RunAsync();
