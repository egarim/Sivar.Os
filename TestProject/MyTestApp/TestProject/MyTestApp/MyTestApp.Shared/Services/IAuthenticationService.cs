using System.Security.Claims;

namespace MyTestApp.Shared.Services
{
    public interface IAuthenticationService
    {
        Task<AuthenticationState> GetAuthenticationStateAsync();
    }

    public class AuthenticationState
    {
        public bool IsAuthenticated { get; set; }
        public string? Name { get; set; }
        public ClaimsPrincipal? Principal { get; set; }
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
