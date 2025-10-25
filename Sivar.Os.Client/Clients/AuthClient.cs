
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of authentication client
/// </summary>
public class AuthClient : BaseClient, IAuthClient
{
    private const string BaseRoute = "api/auth";

    public AuthClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    public async Task<object> LoginAsync(UserAuthenticationInfo request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<object>($"{BaseRoute}/login", request, cancellationToken);
    }

    public async Task<object> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<object>($"{BaseRoute}/status", cancellationToken);
    }

    public async Task<UserAuthenticationResult> AuthenticateUserAsync(string keycloakId, UserAuthenticationInfo authInfo, CancellationToken cancellationToken = default)
    {
        return await PostAsync<UserAuthenticationResult>($"authentication/authenticate/{keycloakId}", authInfo, cancellationToken);
    }
}
