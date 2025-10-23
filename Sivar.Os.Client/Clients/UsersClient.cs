
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Client.Clients;

/// <summary>
/// Implementation of users client
/// </summary>
public class UsersClient : BaseClient, IUsersClient
{
    public UsersClient(HttpClient httpClient, SivarClientOptions options) 
        : base(httpClient, options) { }

    public async Task<UserDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<UserDto>("api/users/me", cancellationToken);
    }

    public async Task<UserDto> UpdateMeAsync(UpdateUserPreferencesDto request, CancellationToken cancellationToken = default)
    {
        return await PutAsync<UserDto>("api/users/me", request, cancellationToken);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<UserDto>>("api/users", cancellationToken);
    }

    public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        return await GetAsync<IEnumerable<UserDto>>($"api/users/by-role/{role}", cancellationToken);
    }

    public async Task<UserStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<UserStatisticsDto>("api/users/statistics", cancellationToken);
    }

    public async Task DeactivateUserAsync(string keycloakId, CancellationToken cancellationToken = default)
    {
        await PutAsync<object>($"api/users/{keycloakId}/deactivate", new { }, cancellationToken);
    }

    public async Task ReactivateUserAsync(string keycloakId, CancellationToken cancellationToken = default)
    {
        await PutAsync<object>($"api/users/{keycloakId}/reactivate", new { }, cancellationToken);
    }
}
