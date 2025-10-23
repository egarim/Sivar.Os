using System.Security.Claims;
using MyTestApp.Shared.Services;

namespace MyTestApp.Services
{
    public class ServerAuthenticationService : IAuthenticationService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public ServerAuthenticationService(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var httpContext = _contextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var user = httpContext.User;
                var name = user.FindFirst(c => c.Type == ClaimTypes.Name)?.Value
                        ?? user.FindFirst(c => c.Type == "name")?.Value
                        ?? user.FindFirst(c => c.Type == "preferred_username")?.Value
                        ?? "User";

                var state = new AuthenticationState
                {
                    IsAuthenticated = true,
                    Name = name,
                    Principal = user
                };
                return Task.FromResult(state);
            }

            return Task.FromResult(new AuthenticationState
            {
                IsAuthenticated = false,
                Name = null,
                Principal = null
            });
        }
    }
}
