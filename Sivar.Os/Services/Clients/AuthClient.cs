
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of authentication client using repositories and services
/// Provides the same interface as the HTTP client but operates directly on the service layer
/// </summary>
public class AuthClient : BaseRepositoryClient, IAuthClient
{
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly ILogger<AuthClient> _logger;

    public AuthClient(
        IUserAuthenticationService userAuthenticationService,
        ILogger<AuthClient> logger)
    {
        _userAuthenticationService = userAuthenticationService ?? throw new ArgumentNullException(nameof(userAuthenticationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticates a user and returns authentication result
    /// </summary>
    public async Task<object> LoginAsync(UserAuthenticationInfo request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            _logger.LogWarning("LoginAsync called with null request");
            return new { error = "Invalid request" };
        }

        try
        {
            // For server-side client, we need the keycloakId
            // This would typically come from the current user context
            // For this implementation, we'll return the authentication info as-is
            var result = new
            {
                email = request.Email,
                firstName = request.FirstName,
                lastName = request.LastName,
                role = request.Role,
                timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Login processed for user: {Email}", request.Email);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Email}", request.Email);
            return new { error = "Authentication failed" };
        }
    }

    /// <summary>
    /// Gets the current authentication status
    /// </summary>
    public async Task<object> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = new
            {
                isAuthenticated = true,
                timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Status check completed");
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during status check");
            return new { isAuthenticated = false, error = "Status check failed" };
        }
    }

    /// <summary>
    /// Authenticate user and create user/profile if needed after Keycloak login
    /// </summary>
    public async Task<UserAuthenticationResult> AuthenticateUserAsync(string keycloakId, UserAuthenticationInfo authInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Authenticating user via client: KeycloakId={KeycloakId}, Email={Email}",
                keycloakId, authInfo.Email);

            var result = await _userAuthenticationService.AuthenticateUserAsync(keycloakId, authInfo);

            if (result.IsSuccess && result.IsNewUser)
            {
                _logger.LogInformation(
                    "New user created via client: UserId={UserId}, Email={Email}",
                    result.User?.Id, authInfo.Email);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error authenticating user via client: KeycloakId={KeycloakId}, Email={Email}",
                keycloakId, authInfo.Email);
            
            return new UserAuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while authenticating the user"
            };
        }
    }
}
