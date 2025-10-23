
namespace Sivar.Os.Shared.Services;

/// <summary>
/// Interface for user authentication and automatic profile creation service
/// </summary>
public interface IUserAuthenticationService
{
    /// <summary>
    /// Handles user authentication flow - creates user and default profile if needed
    /// </summary>
    /// <param name="keycloakId">Keycloak user identifier</param>
    /// <param name="authInfo">User authentication information</param>
    /// <returns>Authentication result with user and active profile</returns>
    Task<UserAuthenticationResult> AuthenticateUserAsync(string keycloakId, UserAuthenticationInfo authInfo);
}