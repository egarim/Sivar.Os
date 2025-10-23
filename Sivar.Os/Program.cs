using MudBlazor.Services;
using Sivar.Os.Client.Pages;
using Sivar.Os.Client.Services;
using Sivar.Os.Components;
using Sivar.Os.Services;
using Sivar.Os.Shared.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Data.Repositories;
using Sivar.Os.Shared.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// --- Database Context ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=sivaros;Username=postgres;Password=postgres";

builder.Services.AddDbContext<SivarDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- Repository Registration ---
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IProfileTypeRepository, ProfileTypeRepository>();
builder.Services.AddScoped<IProfileFollowerRepository, ProfileFollowerRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostAttachmentRepository, PostAttachmentRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IReactionRepository, ReactionRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<ISavedResultRepository, SavedResultRepository>();

// --- Auth (Keycloak OIDC) ---
var authority = builder.Configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/blazor-interactive";
var metadata = builder.Configuration["Keycloak:MetadataAddress"] ?? $"{authority}/.well-known/openid-configuration";
var clientId = builder.Configuration["Keycloak:ClientIdServer"] ?? "myhybridapp-server";
var clientSecret = builder.Configuration["Keycloak:ClientSecret"] ?? "CHANGE_ME";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            // For API/XHR requests, return 401 instead of redirecting to the identity provider.
            var req = context.Request;
            var isApiRequest = req.Path.StartsWithSegments("/api")
                               || req.Headers.TryGetValue("X-Requested-With", out StringValues header) && header == "XMLHttpRequest"
                               || req.Headers.TryGetValue("Accept", out StringValues accept) && accept.ToString().Contains("application/json");

            if (isApiRequest)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            var req = context.Request;
            var isApiRequest = req.Path.StartsWithSegments("/api")
                               || req.Headers.TryGetValue("X-Requested-With", out StringValues header) && header == "XMLHttpRequest"
                               || req.Headers.TryGetValue("Accept", out StringValues accept) && accept.ToString().Contains("application/json");

            if (isApiRequest)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }
    };
})
.AddOpenIdConnect(options =>
{
    //use this line when testing with http (dev only) keycloak is running without https
    options.RequireHttpsMetadata = false;
    options.Authority = authority;
    options.MetadataAddress = metadata;
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.TokenValidationParameters.ValidateIssuer = false; // For dev with http
    
    // Handle post-logout redirect
    options.Events = new OpenIdConnectEvents
    {
        OnSignedOutCallbackRedirect = context =>
        {
            context.Response.Redirect("/");
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// --- HTTP Context Accessor for Adaptive Services ---
builder.Services.AddHttpContextAccessor();

// --- Adaptive Authentication Service for Auto render mode ---
builder.Services.AddScoped<IAuthenticationService>(sp =>
{
    var contextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    // On server render: HttpContext is available, use ServerAuthenticationService
    // On client render: HttpContext is null, use ServerAuthenticationService which will return unauthenticated
    return new ServerAuthenticationService(contextAccessor);
});

// --- Adaptive Weather Service for Auto render mode ---
builder.Services.AddScoped<IWeatherService>(sp =>
{
    var contextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    // On server render: HttpContext is available, use ServerWeatherService (direct data access)
    // On client render: HttpContext is null, use ServerWeatherService which returns empty
    // Client will call the API endpoint instead
    return new ServerWeatherService();
});

// --- Auth state flow for Auto mode ---
builder.Services.AddCascadingAuthenticationState();

// Add controllers
builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Sivar.Os.Client._Imports).Assembly);

app.Run();
