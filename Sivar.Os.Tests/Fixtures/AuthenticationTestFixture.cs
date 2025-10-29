using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Sivar.Os.Tests.Fixtures;

/// <summary>
/// Fixture for mocking Keycloak authentication in both server and client contexts
/// </summary>
public class AuthenticationTestFixture
{
    /// <summary>
    /// Creates a mock HttpContext with Keycloak claims
    /// </summary>
    public static Mock<HttpContext> CreateMockHttpContextWithClaims(string keycloakId)
    {
        var claims = new List<Claim>
        {
            new Claim("sub", keycloakId),
            new Claim("preferred_username", $"user-{keycloakId}"),
            new Claim("email", $"{keycloakId}@test.local"),
            new Claim(ClaimTypes.NameIdentifier, keycloakId)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Mock HttpRequest with proper Headers collection
        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(x => x.Headers).Returns(new HeaderDictionary());

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.User).Returns(principal);
        httpContext.Setup(x => x.Request).Returns(httpRequest.Object);
        httpContext.Setup(x => x.Response).Returns(new Mock<HttpResponse>().Object);

        return httpContext;
    }

    /// <summary>
    /// Creates a mock IHttpContextAccessor with authenticated context
    /// </summary>
    public static Mock<IHttpContextAccessor> CreateMockHttpContextAccessor(string keycloakId)
    {
        var mockHttpContext = CreateMockHttpContextWithClaims(keycloakId);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
        return accessor;
    }

    /// <summary>
    /// Creates a mock IHttpContextAccessor that returns null (unauthenticated)
    /// </summary>
    public static Mock<IHttpContextAccessor> CreateMockHttpContextAccessorUnauthenticated()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        return accessor;
    }
}
