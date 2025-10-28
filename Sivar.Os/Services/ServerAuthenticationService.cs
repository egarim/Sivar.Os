using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services
{
    public class ServerAuthenticationService : IAuthenticationService
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<ServerAuthenticationService> _logger;

        public ServerAuthenticationService(
            IHttpContextAccessor contextAccessor,
            ILogger<ServerAuthenticationService> logger)
        {
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        public Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var requestId = Guid.NewGuid();
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("[ServerAuthenticationService.GetAuthenticationStateAsync] START - RequestId={RequestId}, Timestamp={Timestamp}",
                requestId, startTime);

            try
            {
                // Validate context accessor
                if (_contextAccessor == null)
                {
                    _logger.LogError("[ServerAuthenticationService.GetAuthenticationStateAsync] CRITICAL - RequestId={RequestId}, ContextAccessorNull=true",
                        requestId);
                    throw new InvalidOperationException("IHttpContextAccessor is not configured properly");
                }

                var httpContext = _contextAccessor.HttpContext;

                // Log context availability
                _logger.LogDebug("[ServerAuthenticationService.GetAuthenticationStateAsync] HttpContext availability - RequestId={RequestId}, HttpContextNull={HttpContextNull}",
                    requestId, httpContext == null);

                // Handle null HttpContext - can happen in client-side renders
                if (httpContext == null)
                {
                    _logger.LogInformation("[ServerAuthenticationService.GetAuthenticationStateAsync] HttpContext is null (likely client-side render) - RequestId={RequestId}",
                        requestId);

                    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    _logger.LogInformation("[ServerAuthenticationService.GetAuthenticationStateAsync] UNAUTHENTICATED (no HttpContext) - RequestId={RequestId}, Duration={Duration}ms",
                        requestId, elapsed);

                    return Task.FromResult(new AuthenticationState
                    {
                        IsAuthenticated = false,
                        Name = null,
                        Principal = null
                    });
                }

                // Validate User and Identity
                var user = httpContext.User;
                if (user == null)
                {
                    _logger.LogWarning("[ServerAuthenticationService.GetAuthenticationStateAsync] User principal is null - RequestId={RequestId}",
                        requestId);

                    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    _logger.LogInformation("[ServerAuthenticationService.GetAuthenticationStateAsync] UNAUTHENTICATED (null User) - RequestId={RequestId}, Duration={Duration}ms",
                        requestId, elapsed);

                    return Task.FromResult(new AuthenticationState
                    {
                        IsAuthenticated = false,
                        Name = null,
                        Principal = null
                    });
                }

                var identity = user.Identity;
                if (identity == null)
                {
                    _logger.LogWarning("[ServerAuthenticationService.GetAuthenticationStateAsync] User identity is null - RequestId={RequestId}",
                        requestId);

                    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    _logger.LogInformation("[ServerAuthenticationService.GetAuthenticationStateAsync] UNAUTHENTICATED (null Identity) - RequestId={RequestId}, Duration={Duration}ms",
                        requestId, elapsed);

                    return Task.FromResult(new AuthenticationState
                    {
                        IsAuthenticated = false,
                        Name = null,
                        Principal = null
                    });
                }

                // Check authentication status
                if (!identity.IsAuthenticated)
                {
                    _logger.LogInformation("[ServerAuthenticationService.GetAuthenticationStateAsync] User not authenticated - RequestId={RequestId}, AuthenticationType={AuthType}",
                        requestId, identity.AuthenticationType ?? "null");

                    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    _logger.LogInformation("[ServerAuthenticationService.GetAuthenticationStateAsync] UNAUTHENTICATED - RequestId={RequestId}, Duration={Duration}ms",
                        requestId, elapsed);

                    return Task.FromResult(new AuthenticationState
                    {
                        IsAuthenticated = false,
                        Name = null,
                        Principal = null
                    });
                }

                // Extract claims for authenticated user
                var claimsList = user.Claims.ToList();
                var claimCount = claimsList.Count;

                _logger.LogInformation("[ServerAuthenticationService.GetAuthenticationStateAsync] User is authenticated - RequestId={RequestId}, ClaimCount={ClaimCount}, AuthType={AuthType}",
                    requestId, claimCount, identity.AuthenticationType);

                // Resolve user name from multiple claim types (fallback pattern)
                var name = ExtractUserName(user, requestId);

                if (string.IsNullOrEmpty(name))
                {
                    _logger.LogWarning("[ServerAuthenticationService.GetAuthenticationStateAsync] User name could not be resolved from claims - RequestId={RequestId}, ClaimCount={ClaimCount}",
                        requestId, claimCount);
                    name = "User"; // Fallback to default
                }

                _logger.LogDebug("[ServerAuthenticationService.GetAuthenticationStateAsync] User name resolved - RequestId={RequestId}, Name={Name}",
                    requestId, name);

                // Build authentication state
                var state = new AuthenticationState
                {
                    IsAuthenticated = true,
                    Name = name,
                    Principal = user
                };

                var elapsed2 = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("[ServerAuthenticationService.GetAuthenticationStateAsync] AUTHENTICATED - RequestId={RequestId}, UserName={Name}, ClaimCount={ClaimCount}, Duration={Duration}ms",
                    requestId, name, claimCount, elapsed2);

                return Task.FromResult(state);
            }
            catch (Exception ex)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogError(ex, "[ServerAuthenticationService.GetAuthenticationStateAsync] EXCEPTION - RequestId={RequestId}, ExceptionType={ExceptionType}, Duration={Duration}ms",
                    requestId, ex.GetType().Name, elapsed);
                throw;
            }
        }

        /// <summary>
        /// Extracts the user name from claims with fallback pattern.
        /// Tries multiple claim types to find the user name.
        /// </summary>
        private string? ExtractUserName(ClaimsPrincipal user, Guid requestId)
        {
            try
            {
                var claimTypes = new[] 
                { 
                    ClaimTypes.Name,
                    "name",
                    "preferred_username",
                    ClaimTypes.Email,
                    "email"
                };

                foreach (var claimType in claimTypes)
                {
                    var value = user.FindFirst(c => c.Type == claimType)?.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        _logger.LogDebug("[ServerAuthenticationService.ExtractUserName] Name found in claim - RequestId={RequestId}, ClaimType={ClaimType}",
                            requestId, claimType);
                        return value;
                    }
                }

                _logger.LogDebug("[ServerAuthenticationService.ExtractUserName] No name found in any claim type - RequestId={RequestId}",
                    requestId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ServerAuthenticationService.ExtractUserName] Exception extracting user name - RequestId={RequestId}",
                    requestId);
                return null;
            }
        }
    }
}
