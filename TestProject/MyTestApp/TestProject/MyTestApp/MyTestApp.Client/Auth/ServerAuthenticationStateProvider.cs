using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MyTestApp.Client.Auth
{
    public class ServerAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private AuthenticationState? _cachedAuthenticationState;

        public ServerAuthenticationStateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // Return cached state if available to reduce server calls
            if (_cachedAuthenticationState != null)
            {
                return _cachedAuthenticationState;
            }

            try
            {
                var response = await _httpClient.GetFromJsonAsync<ProfileResponse>("authentication/profile");
                
                if (response?.IsAuthenticated == true)
                {
                    var claims = response.Claims?.Select(c => new Claim(c.Type, c.Value)).ToList() ?? [];
                    var identity = new ClaimsIdentity(claims, "Server authentication");
                    var principal = new ClaimsPrincipal(identity);
                    _cachedAuthenticationState = new AuthenticationState(principal);
                }
                else
                {
                    _cachedAuthenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }
            catch
            {
                _cachedAuthenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            return _cachedAuthenticationState;
        }

        public void ClearCache()
        {
            _cachedAuthenticationState = null;
        }
    }

    public class ProfileResponse
    {
        public bool IsAuthenticated { get; set; }
        public string? Name { get; set; }
        public List<ClaimResponse>? Claims { get; set; }
    }

    public class ClaimResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
