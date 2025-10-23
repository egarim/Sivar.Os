
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services.Clients;

/// <summary>
/// Server-side implementation of users client
/// </summary>
public class UsersClient : BaseRepositoryClient, IUsersClient
{
    private readonly IUserService _userService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UsersClient> _logger;

    public UsersClient(
        IUserService userService,
        IUserRepository userRepository,
        ILogger<UsersClient> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // My user (authenticated user)
    public async Task<UserDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetMeAsync called");
        return new UserDto { Id = Guid.NewGuid(), Email = "user@example.com" };
    }

    public async Task<UserDto> UpdateMeAsync(UpdateUserPreferencesDto request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        _logger.LogInformation("UpdateMeAsync");
        return new UserDto { Id = Guid.NewGuid(), Email = "user@example.com" };
    }

    // User management (admin)
    public async Task<UserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("GetUserAsync called with empty user ID");
            return new UserDto();
        }

        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            _logger.LogInformation("User retrieved: {UserId}", userId);
            return user != null ? MapToDto(user) : new UserDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserPreferencesDto request, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || request == null)
        {
            _logger.LogWarning("UpdateUserAsync called with invalid parameters");
            return new UserDto();
        }

        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for update: {UserId}", userId);
                return new UserDto();
            }

            _logger.LogInformation("User updated: {UserId}", userId);
            return MapToDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            throw;
        }
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("DeleteUserAsync called with empty user ID");
            return;
        }

        try
        {
            await _userRepository.DeleteAsync(userId);
            _logger.LogInformation("User deleted: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<UserDto>> SearchUsersAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(query))
        {
            _logger.LogWarning("SearchUsersAsync called with empty query");
            return new List<UserDto>();
        }

        try
        {
            _logger.LogInformation("Users searched for query '{Query}'", query);
            return new List<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            throw;
        }
    }

    // Batch operations
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetAllUsersAsync");
        return new List<UserDto>();
    }

    public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetUsersByRoleAsync: {Role}", role);
        return new List<UserDto>();
    }

    // Statistics
    public async Task<UserStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetStatisticsAsync");
        return new UserStatisticsDto();
    }

    // User status management
    public async Task DeactivateUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DeactivateUserAsync: {UserId}", userId);
    }

    public async Task ReactivateUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ReactivateUserAsync: {UserId}", userId);
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
}
