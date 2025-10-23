using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using MyTestApp.Shared.Services;
using MyTestApp.Client.Services;
using MyTestApp.Client.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();

// Register UnauthorizedRedirectHandler so we can centrally handle 401 responses
builder.Services.AddTransient<UnauthorizedRedirectHandler>();

// Configure HttpClient for WASM to include browser credentials (cookies) on every request
// and use the UnauthorizedRedirectHandler to intercept 401s
builder.Services.AddScoped(sp =>
{
	var js = sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>();
	// For Blazor WASM the platform provides the underlying message handler; create a plain HttpClient.
	return new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});

// Register ApiClient wrapper that centralizes 401 handling
builder.Services.AddScoped<ApiClient>();

// Register client-side authentication service
builder.Services.AddScoped<IAuthenticationService, ClientAuthenticationService>();

// Register client-side weather service
builder.Services.AddScoped<IWeatherService, ClientWeatherService>();

// For hybrid Auto mode: authentication is handled server-side via cookies
// Register custom WASM authentication state provider to fetch auth state from server
builder.Services.AddScoped<AuthenticationStateProvider, WasmAuthenticationStateProvider>();

// Add authorization services required by AuthorizeView component
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();

await builder.Build().RunAsync();
