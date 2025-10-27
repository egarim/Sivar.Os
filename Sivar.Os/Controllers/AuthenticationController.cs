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
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[AuthenticationController.Login] START - RequestId={RequestId}, ReturnUrl={ReturnUrl}", 
            requestId, returnUrl);

        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl
        };
        
        _logger.LogInformation("[AuthenticationController.Login] Challenging with OpenIdConnect - RequestId={RequestId}, RedirectUri={RedirectUri}", 
            requestId, authenticationProperties.RedirectUri);
        
        return Challenge(authenticationProperties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("register")]
    public IActionResult Register(string returnUrl = "/")
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("[AuthenticationController.Register] START - RequestId={RequestId}, ReturnUrl={ReturnUrl}", 
            requestId, returnUrl);

        // Keycloak registration URL - redirect to Keycloak's registration page
        // The user will be redirected to Keycloak, register, then return to the app
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl,
            // Add a parameter to tell Keycloak to show the registration page
            Items = { ["prompt"] = "create" }
        };
        
        _logger.LogInformation("[AuthenticationController.Register] Challenging with OpenIdConnect registration - RequestId={RequestId}, RedirectUri={RedirectUri}", 
            requestId, authenticationProperties.RedirectUri);
        
        return Challenge(authenticationProperties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        var requestId = Guid.NewGuid();
        var userName = User?.Identity?.Name ?? "ANONYMOUS";
        _logger.LogInformation("[AuthenticationController.Logout] START - RequestId={RequestId}, User={UserName}", 
            requestId, userName);

        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = "/"
        };

        _logger.LogInformation("[AuthenticationController.Logout] Signing out - RequestId={RequestId}", requestId);

        return SignOut(authenticationProperties, 
            "Cookies",
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> LogoutPost()
    {
        var requestId = Guid.NewGuid();
        var userName = User?.Identity?.Name ?? "ANONYMOUS";
        _logger.LogInformation("[AuthenticationController.LogoutPost] START - RequestId={RequestId}, User={UserName}", 
            requestId, userName);

        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = "/"
        };

        _logger.LogInformation("[AuthenticationController.LogoutPost] Signing out - RequestId={RequestId}", requestId);

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
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation(
            "[AuthenticationController.AuthenticateUser] START - RequestId={RequestId}, KeycloakId={KeycloakId}, Email={Email}, Name={FirstName} {LastName}",
            requestId, keycloakId, authInfo.Email, authInfo.FirstName, authInfo.LastName);

        try
        {
            var result = await _userAuthenticationService.AuthenticateUserAsync(keycloakId, authInfo);

            if (result.IsSuccess)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                if (result.IsNewUser)
                {
                    _logger.LogInformation(
                        "[AuthenticationController.AuthenticateUser] NEW_USER_CREATED - RequestId={RequestId}, UserId={UserId}, ProfileId={ProfileId}, Email={Email}, Duration={Duration}ms",
                        requestId, result.User?.Id, result.ActiveProfile?.Id, authInfo.Email, elapsed);
                }
                else
                {
                    _logger.LogInformation(
                        "[AuthenticationController.AuthenticateUser] EXISTING_USER - RequestId={RequestId}, UserId={UserId}, Email={Email}, Duration={Duration}ms",
                        requestId, result.User?.Id, authInfo.Email, elapsed);
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
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning(
                    "[AuthenticationController.AuthenticateUser] FAILED - RequestId={RequestId}, KeycloakId={KeycloakId}, Error={Error}, Duration={Duration}ms",
                    requestId, keycloakId, result.ErrorMessage, elapsed);
                
                return BadRequest(new
                {
                    result.IsSuccess,
                    result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex,
                "[AuthenticationController.AuthenticateUser] ERROR - RequestId={RequestId}, KeycloakId={KeycloakId}, Email={Email}, Duration={Duration}ms",
                requestId, keycloakId, authInfo.Email, elapsed);
            
            return StatusCode(500, new
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while authenticating the user"
            });
        }
    }
}
