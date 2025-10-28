using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MudBlazor.Services;
using OpenAI;
using Sivar.Os.Client.Pages;
using Sivar.Os.Client.Services;
using Sivar.Os.Components;
using Sivar.Os.Data.Context;
using Sivar.Os.Data.Repositories;
using Sivar.Os.Services;
using Sivar.Os.Services.Clients;
using Sivar.Os.Shared;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;
using Sivar.Server.Library.Services;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add memory cache for rate limiting
builder.Services.AddMemoryCache();

// --- JWT Claim Mapping Configuration ---
// MUST be set BEFORE AddAuthentication to prevent WS-Fed claim URI wrapping
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

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

// --- AI Client Registration (Ollama) ---
// Register IChatClient for ChatService
builder.Services.AddScoped<IChatClient>(sp =>
{
    var endpoint = "http://127.0.0.1:11434/";
 var modelId = "phi3:latest";
  return GetChatClientOllamaImp(endpoint, modelId);
});

// Register IEmbeddingGenerator for VectorEmbeddingService
builder.Services.AddScoped<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
{
    var endpoint = "http://127.0.0.1:11434/";
    var modelId = "all-minilm:latest"; // Common embedding model for Ollama
    return new OllamaEmbeddingGenerator(endpoint, modelId: modelId);
});

// --- Service Registration ---
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IProfileTypeService, ProfileTypeService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IReactionService, ReactionService>();
builder.Services.AddScoped<IProfileFollowerService, ProfileFollowerService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ISavedResultService, SavedResultService>();

// --- Utility Services Registration ---
builder.Services.AddScoped<IRateLimitingService, RateLimitingService>();
builder.Services.AddScoped<IFileUploadValidator, FileUploadValidator>();
builder.Services.AddScoped<IProfileMetadataValidator, ProfileMetadataValidator>();

// --- AI & Vector Services Registration ---
builder.Services.AddScoped<ChatFunctionService>();
builder.Services.AddScoped<IVectorEmbeddingService, VectorEmbeddingService>();

// Configure ChatServiceOptions
builder.Services.Configure<ChatServiceOptions>(options =>
{
    options.Provider = "ollama";
    options.MaxTokens = 2000;
    options.Temperature = 0.7;
    options.MaxMessagesPerConversation = 1000;
    options.Ollama = new ChatServiceOptions.OllamaSettings
    {
        Endpoint = "http://127.0.0.1:11434",
        ModelId = "phi3:latest"
    };
});

// Configure VectorEmbeddingOptions
builder.Services.Configure<VectorEmbeddingOptions>(options =>
{
    options.Provider = "Ollama";
    options.MaxTextLength = 8000;
    options.BatchSize = 10;
    options.MinimumSimilarityThreshold = 0.1f;
    options.Ollama = new OllamaOptions
    {
   Endpoint = "http://127.0.0.1:11434",
        ModelId = "all-minilm:latest"
    };
});

builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();

// --- Client Registration (Sivar.Os.Services.Clients) ---
builder.Services.AddScoped<IAuthClient, AuthClient>();
builder.Services.AddScoped<IUsersClient, UsersClient>();
builder.Services.AddScoped<IProfilesClient, ProfilesClient>();
builder.Services.AddScoped<IProfileTypesClient, ProfileTypesClient>();
builder.Services.AddScoped<IPostsClient, PostsClient>();
builder.Services.AddScoped<ICommentsClient, CommentsClient>();
builder.Services.AddScoped<IReactionsClient, ReactionsClient>();
builder.Services.AddScoped<IFollowersClient, FollowersClient>();
builder.Services.AddScoped<INotificationsClient, NotificationsClient>();
builder.Services.AddScoped<ISivarChatClient, ChatClient>();
builder.Services.AddScoped<IFilesClient, FilesClient>();

// Register the aggregate SivarClient
builder.Services.AddScoped<ISivarClient, Sivar.Os.Services.Clients.SivarClient>();

// --- Auth (Keycloak OIDC) ---
var authority = builder.Configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/blazor-interactive";
var metadata = builder.Configuration["Keycloak:MetadataAddress"] ?? $"{authority}/.well-known/openid-configuration";
var clientId = builder.Configuration["Keycloak:ClientIdServer"] ?? "myhybridapp-server";
var clientSecret = builder.Configuration["Keycloak:ClientSecret"] ?? "CHANGE_ME";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;

    //TODO : Check if needed to disable claim mapping
    //JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
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

            // For regular requests, redirect to welcome page if trying to access root
            // or redirect to authentication/login with returnUrl
            if (context.Request.Path == "/" || context.Request.Path == "")
            {
                context.Response.Redirect("/welcome");
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);
            }
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
    
    // ⭐ CRITICAL: Prevent WS-Fed claim URI wrapping
    // This ensures claims like "email" stay as "email" instead of becoming
    // "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
    options.MapInboundClaims = false;
    
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    
    // Configure token validation parameters for proper claim handling
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "roles",
        ValidateIssuer = false // For dev with http
    };
    
    // Handle post-logout redirect
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            // Check if this is a registration request
            if (context.Properties.Items.TryGetValue("prompt", out var prompt) && prompt == "create")
            {
                // Add kc_action parameter to show Keycloak registration page
                context.ProtocolMessage.SetParameter("kc_action", "REGISTER");
            }
            
            // Handle logout redirect - use root URL which is already registered in Keycloak
            if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
            {
                // Override the post_logout_redirect_uri to use the root path
                var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
                context.ProtocolMessage.PostLogoutRedirectUri = baseUrl + "/";
            }
            
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            // After successful Keycloak authentication, create user and profile in our database
            var keycloakId = context.Principal?.FindFirst("sub")?.Value;
            var email = context.Principal?.FindFirst("email")?.Value 
                     ?? context.Principal?.FindFirst("preferred_username")?.Value;
            var firstName = context.Principal?.FindFirst("given_name")?.Value ?? "";
            var lastName = context.Principal?.FindFirst("family_name")?.Value ?? "";
            
            if (!string.IsNullOrEmpty(keycloakId))
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                
                try
                {
                    // Get the user authentication service
                    var authService = context.HttpContext.RequestServices
                        .GetRequiredService<IUserAuthenticationService>();
                    
                    var authInfo = new UserAuthenticationInfo
                    {
                        Email = email ?? "",
                        FirstName = firstName,
                        LastName = lastName,
                        Role = "RegisteredUser"
                    };
                    
                    // Create user and default profile if needed
                    var result = await authService.AuthenticateUserAsync(keycloakId, authInfo);
                    
                    if (result.IsSuccess)
                    {
                        if (result.IsNewUser)
                        {
                            logger.LogInformation(
                                "New user {Email} created with ID {UserId} and profile {ProfileId}",
                                email, result.User?.Id, result.ActiveProfile?.Id);
                        }
                        else
                        {
                            logger.LogInformation(
                                "Existing user {Email} authenticated with profile {ProfileId}",
                                email, result.ActiveProfile?.Id);
                        }
                    }
                    else
                    {
                        logger.LogError(
                            "Failed to authenticate user {Email}: {Error}", 
                            email, result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, 
                        "Exception during user authentication for {Email}", email);
                }
            }
        },
        OnSignedOutCallbackRedirect = context =>
        {
            // Redirect to welcome page after successful logout
            context.Response.Redirect("/welcome");
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
    var logger = sp.GetRequiredService<ILogger<ServerAuthenticationService>>();
    // On server render: HttpContext is available, use ServerAuthenticationService
    // On client render: HttpContext is null, use ServerAuthenticationService which will return unauthenticated
    return new ServerAuthenticationService(contextAccessor, logger);
});

// --- Adaptive Weather Service for Auto render mode ---
builder.Services.AddScoped<IWeatherService>(sp =>
{
    var contextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var logger = sp.GetRequiredService<ILogger<ServerWeatherService>>();
    // On server render: HttpContext is available, use ServerWeatherService (direct data access)
    // On client render: HttpContext is null, use ServerWeatherService which returns empty
    // Client will call the API endpoint instead
    return new ServerWeatherService(logger);
});

// --- Auth state flow for Auto mode ---
builder.Services.AddCascadingAuthenticationState();

// Add controllers with JSON serialization configuration for enum handling
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Configure circuit options for detailed error reporting
builder.Services.Configure<CircuitOptions>(options =>
{
    options.DetailedErrors = true;
});

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

// Handle post-logout redirect from root to welcome
app.Use(async (context, next) =>
{
    // If user is not authenticated and trying to access root, redirect to welcome
    if (context.Request.Path == "/" && 
        context.Request.Query.ContainsKey("logout") == false &&
        !context.User.Identity?.IsAuthenticated == true)
    {
        // Check if this looks like a post-logout scenario (no referer or coming from Keycloak)
        var referer = context.Request.Headers.Referer.ToString();
        if (string.IsNullOrEmpty(referer) || referer.Contains("keycloak") || referer.Contains("localhost:8080"))
        {
            context.Response.Redirect("/welcome");
            return;
        }
    }
    await next();
});

app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Sivar.Os.Client._Imports).Assembly);

app.Run();

// --- Helper Methods for AI Client Creation ---
static IChatClient GetChatClientOpenAiImp(string ApiKey, string ModelId)
{
    OpenAIClient openAIClient = new OpenAIClient(ApiKey);

    return new OpenAIChatClient(openAIClient, ModelId)
 .AsBuilder()
     .Build();
}

static IChatClient GetChatClientOllamaImp(string endpoint, string modelId)
{
    return new OllamaChatClient(endpoint, modelId: modelId)
     .AsBuilder()
        .Build();
}
