using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Shared.Clients;

/// <summary>
/// Client for user operations
/// </summary>
public interface IUsersClient
{
    // User operations
    Task<UserDto> GetMeAsync(CancellationToken cancellationToken = default);
    Task<UserDto> UpdateMeAsync(UpdateUserPreferencesDto request, CancellationToken cancellationToken = default);

    // Admin operations
    Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<UserStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);

    // User management (admin)
    Task DeactivateUserAsync(string keycloakId, CancellationToken cancellationToken = default);
    Task ReactivateUserAsync(string keycloakId, CancellationToken cancellationToken = default);
}
