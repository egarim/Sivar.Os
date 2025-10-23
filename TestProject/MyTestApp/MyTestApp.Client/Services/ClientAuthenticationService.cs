using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using MyTestApp.Shared.Services;

namespace MyTestApp.Client.Services
{
    public class ClientAuthenticationService : IAuthenticationService
    {
        private readonly AuthenticationStateProvider _authStateProvider;
        private MyTestApp.Shared.Services.AuthenticationState? _cachedAuthenticationState;

        public ClientAuthenticationService(AuthenticationStateProvider authStateProvider)
        {
            _authStateProvider = authStateProvider;
        }

        public async Task<MyTestApp.Shared.Services.AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Get the current authentication state from the provider
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                var isAuthenticated = user.Identity?.IsAuthenticated ?? false;

                _cachedAuthenticationState = new MyTestApp.Shared.Services.AuthenticationState
                {
                    IsAuthenticated = isAuthenticated,
                    Name = user.Identity?.Name,
                    Principal = user
                };
            }
            catch
            {
                _cachedAuthenticationState = new MyTestApp.Shared.Services.AuthenticationState
                {
                    IsAuthenticated = false,
                    Name = null,
                    Principal = null
                };
            }

            return _cachedAuthenticationState;
        }

        public void ClearCache()
        {
            _cachedAuthenticationState = null;
        }
    }
}
