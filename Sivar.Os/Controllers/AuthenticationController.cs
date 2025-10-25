using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

[ApiController]
[Route("authentication")]
public class AuthenticationController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IUserAuthenticationService _userAuthenticationService;

    public AuthenticationController(
        ILogger<AuthenticationController> logger,
        IUserAuthenticationService userAuthenticationService)
    {
        _logger = logger;
        _userAuthenticationService = userAuthenticationService;
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

    /// <summary>
    /// Authenticate user and auto-create user/profile if needed after Keycloak login
    /// This endpoint is called from the client-side Home page as a fallback approach
    /// </summary>
    [HttpPost("authenticate/{keycloakId}")]
    public async Task<IActionResult> AuthenticateUser(string keycloakId, [FromBody] UserAuthenticationInfo authInfo)
    {
        try
        {
            _logger.LogInformation(
                "Authenticating user: KeycloakId={KeycloakId}, Email={Email}, Name={FirstName} {LastName}",
                keycloakId, authInfo.Email, authInfo.FirstName, authInfo.LastName);

            var result = await _userAuthenticationService.AuthenticateUserAsync(keycloakId, authInfo);

            if (result.IsSuccess)
            {
                if (result.IsNewUser)
                {
                    _logger.LogInformation(
                        "New user created: UserId={UserId}, ProfileId={ProfileId}, Email={Email}",
                        result.User?.Id, result.ActiveProfile?.Id, authInfo.Email);
                }
                else
                {
                    _logger.LogInformation(
                        "Existing user authenticated: UserId={UserId}, Email={Email}",
                        result.User?.Id, authInfo.Email);
                }

                return Ok(new
                {
                    result.IsSuccess,
                    result.IsNewUser,
                    UserId = result.User?.Id,
                    ProfileId = result.ActiveProfile?.Id
                });
            }
            else
            {
                _logger.LogWarning(
                    "User authentication failed: KeycloakId={KeycloakId}, Error={Error}",
                    keycloakId, result.ErrorMessage);
                
                return BadRequest(new
                {
                    result.IsSuccess,
                    result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error authenticating user: KeycloakId={KeycloakId}, Email={Email}",
                keycloakId, authInfo.Email);
            
            return StatusCode(500, new
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while authenticating the user"
            });
        }
    }
}
