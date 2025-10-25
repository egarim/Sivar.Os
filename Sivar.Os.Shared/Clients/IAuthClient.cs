using Sivar.Os.Shared.Services;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client for authentication operations
/// </summary>
public interface IAuthClient
{
    /// <summary>
    /// Login for development (generates JWT)
    /// </summary>
    Task<object> LoginAsync(UserAuthenticationInfo request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current authentication status
    /// </summary>
    Task<object> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticate user and create user/profile if needed after Keycloak login
    /// </summary>
    Task<UserAuthenticationResult> AuthenticateUserAsync(string keycloakId, UserAuthenticationInfo authInfo, CancellationToken cancellationToken = default);
}
