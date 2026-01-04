using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using System.Security.Claims;

namespace Sivar.Os.Middleware;

/// <summary>
/// Middleware to check if authenticated users are approved on the waiting list.
/// Unapproved users are redirected to a waiting page.
/// </summary>
public class WaitingListAccessMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WaitingListAccessMiddleware> _logger;

    // Paths that should bypass the waiting list check
    private static readonly HashSet<string> BypassPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/waitinglist",
        "/api/health",
        "/authentication",
        "/app/waiting",
        "/app/verify-phone",
        "/app/access-denied",
        "/app/explore",
        "/_blazor",
        "/_framework",
        "/_content",  // MudBlazor, DevExpress, and other library static files
        "/css",
        "/js",
        "/images",
        "/favicon.ico",
        "/api/blob-proxy",
        "/api/posts/public",
        "/api/profiles/public"
    };

    // Exact match paths (not prefix)
    private static readonly HashSet<string> ExactBypassPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/"
    };

    public WaitingListAccessMiddleware(RequestDelegate next, ILogger<WaitingListAccessMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        _logger.LogDebug("[WaitingListMiddleware] Checking path: {Path}", path);

        // Skip check for bypass paths
        if (ShouldBypassCheck(path))
        {
            _logger.LogDebug("[WaitingListMiddleware] Bypassing path: {Path}", path);
            await _next(context);
            return;
        }

        // Skip for unauthenticated users (they'll hit auth first)
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("[WaitingListMiddleware] User not authenticated, skipping");
            await _next(context);
            return;
        }

        _logger.LogInformation("[WaitingListMiddleware] Checking waiting list for authenticated user on path: {Path}", path);

        // Get user's Keycloak ID
        var keycloakId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(keycloakId))
        {
            await _next(context);
            return;
        }

        // Check waiting list status from Keycloak claims first (faster)
        var waitingListStatus = context.User.FindFirst("waiting_list_status")?.Value;
        
        if (waitingListStatus == "Approved")
        {
            await _next(context);
            return;
        }

        // If no claim or not approved, check the database
        using var scope = serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var waitingListRepository = scope.ServiceProvider.GetRequiredService<IWaitingListRepository>();

        var user = await userRepository.GetByKeycloakIdAsync(keycloakId);
        if (user == null)
        {
            // User not in our database yet - redirect to phone verification
            // This happens when a new user just registered via Keycloak
            _logger.LogInformation("[WaitingListMiddleware] User not in database yet (KeycloakId: {KeycloakId}), redirecting to verification", keycloakId);
            RedirectToVerification(context);
            return;
        }

        var entry = await waitingListRepository.GetByUserIdAsync(user.Id);
        
        if (entry == null)
        {
            // User exists in DB but has no waiting list entry
            // This means they're a "legacy" user from before the waiting list was implemented
            // Allow them through (they're already approved by existing in the system)
            _logger.LogInformation("[WaitingListMiddleware] Legacy user {UserId} has no waiting list entry - allowing access", user.Id);
            await _next(context);
            return;
        }

        switch (entry.Status)
        {
            case WaitingListStatus.Approved:
                // User is approved - proceed
                await _next(context);
                return;

            case WaitingListStatus.PendingVerification:
                // User needs to verify phone
                _logger.LogInformation("User {UserId} pending phone verification", user.Id);
                RedirectToVerification(context);
                return;

            case WaitingListStatus.Waiting:
                // User is in queue
                _logger.LogInformation("User {UserId} is in waiting queue at position {Position}", user.Id, entry.Position);
                RedirectToWaiting(context);
                return;

            case WaitingListStatus.Rejected:
            case WaitingListStatus.Expired:
                // User was rejected or expired
                _logger.LogInformation("User {UserId} was rejected/expired", user.Id);
                RedirectToRejected(context);
                return;

            default:
                await _next(context);
                return;
        }
    }

    private bool ShouldBypassCheck(string path)
    {
        // Check exact matches first
        if (ExactBypassPaths.Contains(path))
            return true;

        // Check prefix matches
        return BypassPaths.Any(bp => path.StartsWith(bp, StringComparison.OrdinalIgnoreCase));
    }

    private void RedirectToVerification(HttpContext context)
    {
        if (IsApiRequest(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.Headers["X-Waiting-List-Status"] = "PendingVerification";
        }
        else
        {
            context.Response.Redirect("/app/verify-phone");
        }
    }

    private void RedirectToWaiting(HttpContext context)
    {
        if (IsApiRequest(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.Headers["X-Waiting-List-Status"] = "Waiting";
        }
        else
        {
            context.Response.Redirect("/app/waiting");
        }
    }

    private void RedirectToRejected(HttpContext context)
    {
        if (IsApiRequest(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.Headers["X-Waiting-List-Status"] = "Rejected";
        }
        else
        {
            context.Response.Redirect("/app/access-denied");
        }
    }

    private bool IsApiRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api");
    }
}

/// <summary>
/// Extension methods for registering the waiting list middleware
/// </summary>
public static class WaitingListAccessMiddlewareExtensions
{
    /// <summary>
    /// Use the waiting list access middleware to restrict unapproved users
    /// </summary>
    public static IApplicationBuilder UseWaitingListAccess(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<WaitingListAccessMiddleware>();
    }
}
