using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;

namespace Sivar.Os.Controllers;

/// <summary>
/// Health check and system status endpoint
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly SivarDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(SivarDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Sivar.Os",
            version = "1.0.0-prototype"
        });
    }

    /// <summary>
    /// Detailed health check with database connectivity
    /// </summary>
    [HttpGet("detailed")]
    [AllowAnonymous]
    public async Task<IActionResult> Detailed()
    {
        try
        {
            // Test database connection
            var canConnect = await _context.Database.CanConnectAsync();
            
            // Get basic stats
            var userCount = await _context.Users.CountAsync();
            var profileCount = await _context.Profiles.CountAsync();
            var postCount = await _context.Posts.CountAsync();

            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "Sivar.Os",
                version = "1.0.0-prototype",
                database = new
                {
                    connected = canConnect,
                    users = userCount,
                    profiles = profileCount,
                    posts = postCount
                },
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new
            {
                status = "unhealthy",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
