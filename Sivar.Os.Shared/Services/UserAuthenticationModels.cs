using Sivar.Os.Shared.DTOs;


namespace Sivar.Os.Shared.Services;

/// <summary>
/// Information needed for user authentication
/// </summary>
public class UserAuthenticationInfo
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "RegisteredUser";
}

/// <summary>
/// Result of user authentication flow
/// </summary>
public class UserAuthenticationResult
{
    public bool IsSuccess { get; set; }
    public UserDto? User { get; set; }
    public ProfileDto? ActiveProfile { get; set; }
    public bool IsNewUser { get; set; }
    public string? ErrorMessage { get; set; }
}