using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Agents.AI;
using MudBlazor.Services;
using DevExpress.Blazor;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Sivar.Os.Client.Pages;
using Sivar.Os.Client.Services;
using Sivar.Os.Client.Components.ProfileSwitcher;
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
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId());

// Configure OpenTelemetry for AI Chat tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Microsoft.Extensions.AI") // Traces from AI chat client
            .AddSource("SivarChat") // Custom traces
            .AddConsoleExporter(); // Output to console for now
    });

// Add MudBlazor services
builder.Services.AddMudServices();

// Configure DevExpress Blazor services (required for DxAIChat component)
builder.Services.AddDevExpressBlazor(configure => 
{
    configure.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
    configure.SizeMode = DevExpress.Blazor.SizeMode.Medium;
});

// Register DevExpress AI services (required for DxAIChat IAIAssistantFactory)
builder.Services.AddDevExpressAI();
// Add memory cache for rate limiting
builder.Services.AddMemoryCache();

// Add localization services for server-side components
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Configure request localization
var supportedCultures = new[] { "en-US", "es-ES" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("en-US")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    
    // Use cookie-based culture provider for persistence
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

// --- JWT Claim Mapping Configuration ---
// MUST be set BEFORE AddAuthentication to prevent WS-Fed claim URI wrapping
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

// --- Database Context ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=sivaros;Username=postgres;Password=postgres";

// Register VectorTypeInterceptor as a singleton so it can be injected into DbContext
builder.Services.AddSingleton<Sivar.Os.Data.Interceptors.VectorTypeInterceptor>();

// Use AddPooledDbContextFactory which:
// 1. Registers IDbContextFactory<SivarDbContext> as singleton (for CategoryNormalizer)
// 2. Also registers SivarDbContext as scoped (for normal DI injection)
builder.Services.AddPooledDbContextFactory<SivarDbContext>((serviceProvider, options) =>
{
    var vectorInterceptor = serviceProvider.GetRequiredService<Sivar.Os.Data.Interceptors.VectorTypeInterceptor>();
    options.UseNpgsql(connectionString, o => o.UseVector())
        .AddInterceptors(vectorInterceptor);
});

// Also register scoped DbContext for existing code that injects SivarDbContext directly
builder.Services.AddScoped<SivarDbContext>(sp => 
    sp.GetRequiredService<IDbContextFactory<SivarDbContext>>().CreateDbContext());

// --- ChatService Configuration (Early binding for API key from environment) ---
// Bind ChatServiceOptions early so all AI services get the correct API key
var chatServiceOptions = builder.Configuration.GetSection(ChatServiceOptions.SectionName).Get<ChatServiceOptions>() ?? new ChatServiceOptions();

// Override OpenAI API key from OPENAI_API_KEY environment variable
var envApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (!string.IsNullOrEmpty(envApiKey))
{
    chatServiceOptions.OpenAI.ApiKey = envApiKey;
}

// Register the resolved options for IOptions<ChatServiceOptions> consumers
builder.Services.Configure<ChatServiceOptions>(options =>
{
    options.Provider = chatServiceOptions.Provider;
    options.MaxMessagesPerConversation = chatServiceOptions.MaxMessagesPerConversation;
    options.DefaultResponseType = chatServiceOptions.DefaultResponseType;
    options.MaxTokens = chatServiceOptions.MaxTokens;
    options.Temperature = chatServiceOptions.Temperature;
    options.RateLimitPerMinute = chatServiceOptions.RateLimitPerMinute;
    options.Ollama = chatServiceOptions.Ollama;
    options.OpenAI = chatServiceOptions.OpenAI;
});

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
builder.Services.AddScoped<IChatTokenUsageRepository, ChatTokenUsageRepository>();
builder.Services.AddScoped<IAiModelPricingRepository, AiModelPricingRepository>(); // AI Cost Tracking
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<AnalyticsRepository>(); // Phase 7: Continuous Aggregates
// Phase 1: Contact Actions - Repository registration
builder.Services.AddScoped<IContactTypeRepository, ContactTypeRepository>();
builder.Services.AddScoped<IBusinessContactInfoRepository, BusinessContactInfoRepository>();
// Phase 0.5: Chat Bot Settings - Repository registration
builder.Services.AddScoped<IChatBotSettingsRepository, ChatBotSettingsRepository>();
// Profile Bookmarks - Repository registration
builder.Services.AddScoped<IProfileBookmarkRepository, ProfileBookmarkRepository>();
// Phase 10: Multi-Agent Configuration - Repository registration
builder.Services.AddScoped<IAgentConfigurationRepository, AgentConfigurationRepository>();
// Phase 11: Results Ranking & Personalization - Repository registration
builder.Services.AddScoped<IUserSearchBehaviorRepository, UserSearchBehaviorRepository>();
builder.Services.AddScoped<IRankingConfigurationRepository, RankingConfigurationRepository>();
// Search Ads System - Repository registration
builder.Services.AddScoped<IAdTransactionRepository, AdTransactionRepository>();
// Event/Appointment Scheduling System - Repository registration
builder.Services.AddScoped<IScheduleEventRepository, ScheduleEventRepository>();
// Resource Booking System - Repository registration
builder.Services.AddScoped<IResourceBookingRepository, ResourceBookingRepository>();

// --- AI Client Registration (Configurable Provider) ---
// Register IChatClient for ChatService based on configuration
// Uses pre-resolved chatServiceOptions with environment variable override applied
builder.Services.AddScoped<IChatClient>(sp =>
{
    var provider = chatServiceOptions.Provider?.ToLowerInvariant() ?? "ollama";

    return provider switch
    {
        "openai" => GetChatClientOpenAiImp(
            chatServiceOptions.OpenAI.ApiKey, 
            chatServiceOptions.OpenAI.ModelId),
        "ollama" => GetChatClientOllamaImp(
            chatServiceOptions.Ollama.Endpoint, 
            chatServiceOptions.Ollama.ModelId),
        _ => throw new InvalidOperationException($"Unknown AI provider: {provider}. Supported providers: 'openai', 'ollama'")
    };
});

// Register IAgentFactory (Phase 10: Multi-Agent Configuration) for dynamic agent loading
builder.Services.AddScoped<IAgentFactory, AgentFactory>();

// Register IEmbeddingGenerator for VectorEmbeddingService (using OpenAI)
// Uses pre-resolved chatServiceOptions with environment variable override applied
builder.Services.AddScoped<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
{
    var apiKey = chatServiceOptions.OpenAI.ApiKey;
    if (string.IsNullOrEmpty(apiKey))
    {
        throw new InvalidOperationException("OpenAI API key not configured. Set the OPENAI_API_KEY environment variable.");
    }
    var openAiClient = new OpenAIClient(apiKey);
    return openAiClient.GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator();
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
builder.Services.AddScoped<INotificationService, Sivar.Os.Services.NotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ISavedResultService, SavedResultService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
// Phase 1: Contact Actions - Service registration
builder.Services.AddScoped<IContactUrlBuilder, ContactUrlBuilder>();
// Profile Bookmarks - Service registration
builder.Services.AddScoped<IProfileBookmarkService, ProfileBookmarkService>();
// Event/Appointment Scheduling System - Service registration
builder.Services.AddScoped<IScheduleEventService, ScheduleEventService>();
// Resource Booking System - Service registration
builder.Services.AddScoped<IResourceBookingService, ResourceBookingService>();

// --- Utility Services Registration ---
builder.Services.AddScoped<IRateLimitingService, RateLimitingService>();
builder.Services.AddScoped<IFileUploadValidator, FileUploadValidator>();
builder.Services.AddScoped<IProfileMetadataValidator, ProfileMetadataValidator>();

// --- AI & Vector Services Registration ---
builder.Services.AddScoped<ChatFunctionService>();
builder.Services.AddScoped<Sivar.Os.Services.AgentFunctions.BookingFunctions>(); // Booking integration for Chat AI
builder.Services.AddScoped<IIntentClassifier, IntentClassifier>(); // Phase 6: Intent-Based Routing
builder.Services.AddSingleton<ICategoryNormalizer, CategoryNormalizer>(); // Phase 6: Multilingual Search
builder.Services.AddScoped<IVectorEmbeddingService, VectorEmbeddingService>();
builder.Services.AddScoped<IClientEmbeddingService, ClientEmbeddingService>();
builder.Services.AddScoped<ISearchResultService, SearchResultService>();
builder.Services.AddScoped<IContentExtractionService, ContentExtractionService>();
builder.Services.AddScoped<IRankingService, RankingService>(); // Phase 11: Results Ranking & Personalization
builder.Services.AddScoped<IProfileAdSelector, ProfileAdSelector>(); // Search Ads System
builder.Services.AddScoped<IProfileAdBudgetService, ProfileAdBudgetService>(); // Search Ads System
builder.Services.AddScoped<IAiCostService, AiCostService>(); // AI Cost Tracking

// --- Sentiment Analysis Services Registration ---
builder.Services.AddScoped<IClientSentimentAnalysisService, ClientSentimentAnalysisService>();
builder.Services.AddScoped<IServerSentimentAnalysisService, ServerSentimentAnalysisService>();
builder.Services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();

// Configure AI Services (Sentiment Analysis, Embeddings)
// Supports three modes: Adaptive (default), ClientOnly, ServerOnly
builder.Services.Configure<Sivar.Os.Configuration.AIServiceOptions>(
    builder.Configuration.GetSection("AIServices"));

// Configure EmbeddingOptions for hybrid client/server embedding system
builder.Services.Configure<Sivar.Os.Configuration.EmbeddingOptions>(
    builder.Configuration.GetSection("Embeddings"));

// --- Location Services Registration ---
// Configure LocationServices options from appsettings.json
builder.Services.Configure<Sivar.Os.Shared.Configuration.LocationServicesOptions>(
    builder.Configuration.GetSection("LocationServices"));

// Register the selected location service provider
var locationProvider = builder.Configuration.GetValue<string>("LocationServices:Provider") ?? "Nominatim";

switch (locationProvider.ToLowerInvariant())
{
    case "nominatim":
        // Register Nominatim-specific options
        builder.Services.Configure<Sivar.Os.Shared.Configuration.NominatimOptions>(
            builder.Configuration.GetSection("LocationServices:Nominatim"));
        
        // Register HttpClient for Nominatim
        builder.Services.AddHttpClient<ILocationService, NominatimLocationService>();
        
        // Register NominatimLocationService
        builder.Services.AddScoped<ILocationService, NominatimLocationService>();
        break;
    
    case "azuremaps":
        // TODO: Implement Azure Maps provider
        throw new NotSupportedException("Azure Maps provider not yet implemented");
    
    case "googlemaps":
        // TODO: Implement Google Maps provider
        throw new NotSupportedException("Google Maps provider not yet implemented");
    
    default:
        throw new InvalidOperationException($"Unknown location provider: {locationProvider}");
}

// Configure VectorEmbeddingOptions
builder.Services.Configure<VectorEmbeddingOptions>(options =>
{
    options.Provider = "OpenAI";
    options.MaxTextLength = 8000;
    options.BatchSize = 10;
    options.MinimumSimilarityThreshold = 0.1f;
    // Use 384 dimensions for Matryoshka embeddings - compatible with existing all-minilm vectors
    options.Dimensions = 384;
});

builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();
builder.Services.AddScoped<IImageCompressionService, ImageCompressionService>();

// Configure Azure Blob Storage options
builder.Services.Configure<Sivar.Os.Shared.Configuration.AzureBlobStorageConfiguration>(
    builder.Configuration.GetSection("AzureBlobStorage"));

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
builder.Services.AddScoped<IActivitiesClient, ActivitiesClient>();
builder.Services.AddScoped<IContactsClient, ContactsClient>();
builder.Services.AddScoped<IResourceBookingsClient, ResourceBookingsClient>();

// Register the aggregate SivarClient
builder.Services.AddScoped<ISivarClient, Sivar.Os.Services.Clients.SivarClient>();

// Register profile switcher service for hybrid Blazor (interactive components)
// Uses server-side implementation with repositories
builder.Services.AddScoped<IProfileSwitcherService, ProfileSwitcherClient>();

// Register browser permissions service for GPS and other browser APIs
builder.Services.AddScoped<BrowserPermissionsService>();

// Register chat location service for Phase 0: Location-Aware Chat
builder.Services.AddScoped<ChatLocationService>();

// Register profile context service for unified location/device/timezone context
builder.Services.AddScoped<IProfileContextService, ProfileContextService>();

// Register chat settings service for Phase 0.5: Configurable welcome messages
builder.Services.AddScoped<ChatSettingsService>();

// Register culture service for localization
builder.Services.AddScoped<ICultureService, CultureService>();

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
            // Log for debugging
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("[Auth] OnRedirectToLogin triggered for path: {Path}", context.Request.Path);

            // For API/XHR requests, return 401 instead of redirecting to the identity provider.
            var req = context.Request;
            var isApiRequest = req.Path.StartsWithSegments("/api")
                               || req.Headers.TryGetValue("X-Requested-With", out StringValues header) && header == "XMLHttpRequest"
                               || req.Headers.TryGetValue("Accept", out StringValues accept) && accept.ToString().Contains("application/json");

            if (isApiRequest)
            {
                logger.LogWarning("[Auth] API request - returning 401");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            // Allow access to welcome page and authentication pages without redirect
            if (context.Request.Path.StartsWithSegments("/welcome") || 
                context.Request.Path.StartsWithSegments("/authentication") ||
                context.Request.Path == "/" || 
                context.Request.Path == "")
            {
                logger.LogWarning("[Auth] Public page (/, /welcome, /auth) - allowing through without redirect");
                // Don't redirect, let the request through to Blazor
                return Task.CompletedTask;
            }

            // For other protected requests, redirect to authentication/login with returnUrl
            logger.LogWarning("[Auth] Protected path - redirecting to Keycloak: {RedirectUri}", context.RedirectUri);
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
            // Redirect to root (landing page) after successful logout
            context.Response.Redirect("/");
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    // Allow anonymous access to specific paths
    options.AddPolicy("AnonymousPolicy", policy =>
        policy.RequireAssertion(context =>
        {
            var httpContext = context.Resource as HttpContext;
            if (httpContext != null)
            {
                // Allow welcome and authentication pages without authentication
                if (httpContext.Request.Path.StartsWithSegments("/welcome") ||
                    httpContext.Request.Path.StartsWithSegments("/authentication"))
                {
                    return true;
                }
            }
            return false;
        }));
});

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
    .AddInteractiveServerComponents();
    // ✅ Blazor Server ONLY - No WebAssembly
    // Removed: .AddInteractiveWebAssemblyComponents();

// Configure circuit options for detailed error reporting
builder.Services.Configure<CircuitOptions>(options =>
{
    options.DetailedErrors = true;
});

// ⚡ Configure SignalR for large file uploads (up to 10MB)
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB - matches our file size limit
});

var app = builder.Build();

// Configure CORS for Azure Blob Storage (Azurite) in development
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var corsConfigurator = new BlobStorageCorsConfigurator(
            scope.ServiceProvider.GetRequiredService<IConfiguration>(),
            scope.ServiceProvider.GetRequiredService<ILogger<BlobStorageCorsConfigurator>>());
        
        await corsConfigurator.ConfigureCorsAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Failed to configure Azurite CORS - images may not load correctly");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // ✅ Blazor Server ONLY - removed: app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Use request localization
app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Add blob storage proxy to avoid CORS issues with Azurite
app.MapGet("/api/blob-proxy/{container}/{*blobPath}", async (
    string container,
    string blobPath,
    HttpContext context,
    ILogger<Program> logger) =>
{
    try
    {
        var azuriteUrl = $"http://127.0.0.1:10000/devstoreaccount1/{container}/{blobPath}";
        logger.LogInformation("[BlobProxy] Proxying request: {BlobPath} -> {AzuriteUrl}", blobPath, azuriteUrl);

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(azuriteUrl);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("[BlobProxy] Failed to fetch blob: {StatusCode}", response.StatusCode);
            return Results.NotFound();
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var stream = await response.Content.ReadAsStreamAsync();

        // Set cache headers for better performance
        context.Response.Headers.CacheControl = "public, max-age=31536000"; // 1 year
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");

        return Results.Stream(stream, contentType);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[BlobProxy] Error proxying blob: {BlobPath}", blobPath);
        return Results.Problem("Failed to load image");
    }
}).AllowAnonymous();

app.MapControllers();
app.MapStaticAssets();

// ✅ Blazor Server ONLY - render mode configuration
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Sivar.Os.Client._Imports).Assembly);
    // Removed for Server-only: .AddInteractiveWebAssemblyRenderMode()

app.Run();

// --- Helper Methods for AI Client Creation ---
static IChatClient GetChatClientOpenAiImp(string ApiKey, string ModelId)
{
    OpenAIClient openAIClient = new OpenAIClient(ApiKey);

    return openAIClient
        .GetChatClient(ModelId)
        .AsIChatClient()
        .AsBuilder()
        .UseOpenTelemetry(sourceName: "SivarChat", configure: c => c.EnableSensitiveData = true)
        .Build();
}

static IChatClient GetChatClientOllamaImp(string endpoint, string modelId)
{
    return new OllamaChatClient(endpoint, modelId: modelId)
     .AsBuilder()
        .UseOpenTelemetry(sourceName: "SivarChat", configure: c => c.EnableSensitiveData = true)
        .Build();
}
