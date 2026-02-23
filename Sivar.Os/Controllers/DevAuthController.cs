using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using System.Security.Claims;

namespace Sivar.Os.Controllers;

/// <summary>
/// Development-only authentication bypass
/// REMOVE THIS IN PRODUCTION!
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DevAuthController : ControllerBase
{
    private readonly SivarDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DevAuthController> _logger;

    public DevAuthController(
        SivarDbContext context,
        IWebHostEnvironment env,
        ILogger<DevAuthController> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Development login - creates user if doesn't exist and signs in
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> DevLogin([FromBody] DevLoginRequest request)
    {
        // SECURITY: Only allow in Development
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        try
        {
            // Find or create user (without loading profiles to avoid schema issues)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                // Create new user
                var keycloakId = Guid.NewGuid().ToString(); // Fake Keycloak ID for dev
                var emailName = request.Email.Split('@')[0];
                
                user = new User
                {
                    Id = Guid.NewGuid(),
                    KeycloakId = keycloakId,
                    Email = request.Email,
                    FirstName = emailName,
                    LastName = "DevUser",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create default personal profile
                var profileTypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
                var uniqueHandle = $"{emailName.ToLower()}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                
                var profile = new Profile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ProfileTypeId = profileTypeId,
                    DisplayName = emailName,
                    Handle = uniqueHandle,
                    Bio = "Development test user",
                    AllowedViewers = new List<Guid>(),
                    Tags = new List<string>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Profiles.Add(profile);
                await _context.SaveChangesAsync();

                _logger.LogWarning("[DEV AUTH] Created new user: {Email} with profile", request.Email);
            }

            var displayName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = user.Email;
            }

            var profileCount = await _context.Profiles.CountAsync(p => p.UserId == user.Id);

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.KeycloakId),
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("sub", user.KeycloakId),
                new Claim("email", user.Email),
                new Claim("name", displayName),
                new Claim("email_verified", "true")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            _logger.LogWarning("[DEV AUTH] User logged in: {Email}", request.Email);

            return Ok(new
            {
                success = true,
                email = user.Email,
                displayName = displayName,
                userId = user.Id,
                profileCount = profileCount,
                message = "Development login successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEV AUTH] Error during dev login");
            return StatusCode(500, new { error = "Login failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Quick login via GET for automated testing
    /// Usage: /api/devauth/quick-login?email=test@test.com&redirect=/chat
    /// Returns JSON with user and profile info, or redirects if redirect param provided
    /// </summary>
    [HttpGet("quick-login")]
    [AllowAnonymous]
    public async Task<IActionResult> QuickLogin([FromQuery] string email, [FromQuery] string? redirect = null)
    {
        // SECURITY: Only allow in Development
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required");
        }

        try
        {
            // Find or create user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            Profile? profile = null;

            if (user == null)
            {
                // Create new user
                var keycloakId = Guid.NewGuid().ToString();
                var emailName = email.Split('@')[0];
                
                user = new User
                {
                    Id = Guid.NewGuid(),
                    KeycloakId = keycloakId,
                    Email = email,
                    FirstName = emailName,
                    LastName = "DevUser",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create default personal profile
                var profileTypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
                
                // Ensure ProfileType exists
                var profileType = await _context.ProfileTypes.FindAsync(profileTypeId);
                if (profileType == null)
                {
                    // Create default ProfileType
                    profileType = new ProfileType
                    {
                        Id = profileTypeId,
                        Name = "Personal",
                        Description = "Personal profile type",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.ProfileTypes.Add(profileType);
                    await _context.SaveChangesAsync();
                }
                
                var uniqueHandle = $"{emailName.ToLower()}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                
                profile = new Profile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ProfileTypeId = profileTypeId,
                    DisplayName = emailName,
                    Handle = uniqueHandle,
                    Bio = "Development test user",
                    AllowedViewers = new List<Guid>(),
                    Tags = new List<string>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Profiles.Add(profile);
                await _context.SaveChangesAsync();

                _logger.LogWarning("[DEV AUTH] Quick-login created new user: {Email} with profile {ProfileId}", email, profile.Id);
            }
            else
            {
                // Find existing profile
                profile = await _context.Profiles
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);
                
                if (profile == null)
                {
                    // User exists but no profile - create one
                    var profileTypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
                    var emailName = email.Split('@')[0];
                    var uniqueHandle = $"{emailName.ToLower()}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                    
                    profile = new Profile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        ProfileTypeId = profileTypeId,
                        DisplayName = emailName,
                        Handle = uniqueHandle,
                        Bio = "Development test user",
                        AllowedViewers = new List<Guid>(),
                        Tags = new List<string>(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Profiles.Add(profile);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogWarning("[DEV AUTH] Created missing profile for user: {Email}", email);
                }
            }

            var displayName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = user.Email;
            }

            // Create claims and sign in
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.KeycloakId),
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("sub", user.KeycloakId),
                new Claim("email", user.Email),
                new Claim("name", displayName),
                new Claim("email_verified", "true")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            _logger.LogWarning("[DEV AUTH] Quick-login successful: {Email}, ProfileId={ProfileId}", email, profile?.Id);

            // If redirect specified, redirect there
            if (!string.IsNullOrEmpty(redirect))
            {
                return Redirect(redirect);
            }

            // Otherwise return JSON with user info
            return Ok(new
            {
                success = true,
                userId = user.Id,
                keycloakId = user.KeycloakId,
                email = user.Email,
                profileId = profile?.Id,
                profileHandle = profile?.Handle,
                displayName = displayName,
                message = "Quick login successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEV AUTH] Error during quick-login");
            return StatusCode(500, $"Quick-login failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get test user info (for automated testing)
    /// Usage: /api/devauth/test-info?email=test@test.com
    /// </summary>
    [HttpGet("test-info")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTestInfo([FromQuery] string email)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required");
        }

        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return NotFound(new { error = "User not found", email });
            }

            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            return Ok(new
            {
                userId = user.Id,
                keycloakId = user.KeycloakId,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                hasProfile = profile != null,
                profileId = profile?.Id,
                profileHandle = profile?.Handle,
                profileDisplayName = profile?.DisplayName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DEV AUTH] Error getting test info");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get current dev auth status
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult Status()
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var sub = User.FindFirst("sub")?.Value;

        return Ok(new
        {
            isDevelopment = true,
            isAuthenticated = isAuthenticated,
            email = email,
            keycloakId = sub,
            message = "Development authentication mode active"
        });
    }

    /// <summary>
    /// Logout
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { success = true, message = "Logged out" });
    }
}

public class DevLoginRequest
{
    public string Email { get; set; } = string.Empty;
}
