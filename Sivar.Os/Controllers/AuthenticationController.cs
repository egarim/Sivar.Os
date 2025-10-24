using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace Sivar.Os.Controllers;

[ApiController]
[Route("authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(ILogger<AuthenticationController> logger)
    {
        _logger = logger;
    }
    [HttpGet("login")]
    public IActionResult Login(string returnUrl = "/")
    {
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl
        };
        
        return Challenge(authenticationProperties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("register")]
    public IActionResult Register(string returnUrl = "/")
    {
        // Keycloak registration URL - redirect to Keycloak's registration page
        // The user will be redirected to Keycloak, register, then return to the app
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl,
            // Add a parameter to tell Keycloak to show the registration page
            Items = { ["prompt"] = "create" }
        };
        
        return Challenge(authenticationProperties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = "/"
        };

        return SignOut(authenticationProperties, 
            "Cookies",
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> LogoutPost()
    {
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = "/"
        };

        return SignOut(authenticationProperties, 
            "Cookies",
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
    var cookieHeader = Request.Headers.ContainsKey("Cookie") ? string.Join("; ", Request.Headers["Cookie"].ToArray()) : "<none>";
    var isAuth = (User?.Identity != null && User.Identity.IsAuthenticated);
    _logger.LogInformation("GetProfile called. Cookie: {cookieHeader}. IsAuthenticated: {isAuth}", cookieHeader, isAuth);

    if (isAuth)
        {
            var identity = User?.Identity;
            var userName = identity?.Name ?? "<unknown>";
            var claims = User?.Claims?.Select(c => (object)new { c.Type, c.Value }).ToList() ?? new List<object>();
            var claimCount = claims.Count;
            _logger.LogInformation("User authenticated: {name}. Claims: {count}", userName, claimCount);
            return Ok(new
            {
                name = userName,
                isAuthenticated = true,
                claims = claims
            });
        }

            _logger.LogInformation("User NOT authenticated in GetProfile response.");
        return Ok(new { isAuthenticated = false });
    }
}
