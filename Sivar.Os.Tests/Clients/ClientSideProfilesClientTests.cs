using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using Sivar.Os.Client.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Tests.Fixtures;

namespace Sivar.Os.Tests.Clients;

/// <summary>
/// Tests for the client-side (HTTP) ProfilesClient implementation
/// These tests verify that the HTTP client correctly:
/// 1. Calls the right API endpoints
/// 2. Sends the correct HTTP methods (GET, POST, PUT, DELETE)
/// 3. Serializes/deserializes data correctly
/// 4. Returns the expected data
/// </summary>
public class ClientSideProfilesClientTests : ProfilesClientContractTests
{
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
    private HttpClient _httpClient = null!;
    private const string BaseAddress = "https://localhost:7001";

    /// <summary>
    /// Client-side (HTTP) implementations don't have access to authentication context.
    /// Authentication is handled at the HTTP/server level (401 responses).
    /// </summary>
    protected override bool SupportsAuthenticationContext => false;

    protected override void SetupClient()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        Client = new Sivar.Os.Client.Clients.ProfilesClient(
            _httpClient,
            new SivarClientOptions { BaseUrl = BaseAddress }
        );
    }

    #region Setup Mocks Implementation

    protected override void SetupCreateMyProfileMock(
        string keycloakId,
        CreateProfileDto request,
        ProfileDto? expectedProfile)
    {
        if (expectedProfile == null)
        {
            MockHttpResponse(
                HttpMethod.Post,
                "/api/profiles/my",
                HttpStatusCode.BadRequest,
                new { error = "Invalid request" }
            );
        }
        else
        {
            MockHttpResponse(
                HttpMethod.Post,
                "/api/profiles/my",
                HttpStatusCode.OK,
                expectedProfile
            );
        }
    }

    protected override void SetupGetMyProfileMock(
        string keycloakId,
        ProfileDto? expectedProfile)
    {
        if (expectedProfile == null)
        {
            MockHttpResponse(
                HttpMethod.Get,
                "/api/profiles/my",
                HttpStatusCode.NotFound,
                new { error = "Not found" }
            );
        }
        else
        {
            MockHttpResponse(
                HttpMethod.Get,
                "/api/profiles/my",
                HttpStatusCode.OK,
                expectedProfile
            );
        }
    }

    protected override void SetupUpdateMyProfileMock(
        string keycloakId,
        UpdateProfileDto request,
        ProfileDto? expectedProfile)
    {
        if (expectedProfile == null)
        {
            MockHttpResponse(
                HttpMethod.Put,
                "/api/profiles/my",
                HttpStatusCode.BadRequest,
                new { error = "Invalid request" }
            );
        }
        else
        {
            MockHttpResponse(
                HttpMethod.Put,
                "/api/profiles/my",
                HttpStatusCode.OK,
                expectedProfile
            );
        }
    }

    protected override void SetupDeleteMyProfileMock(string keycloakId)
    {
        MockHttpResponse<object>(
            HttpMethod.Delete,
            "/api/profiles/my",
            HttpStatusCode.NoContent,
            null
        );
    }

    protected override void SetupGetAllMyProfilesMock(
        string keycloakId,
        IEnumerable<ProfileDto> expectedProfiles)
    {
        MockHttpResponse(
            HttpMethod.Get,
            "/api/profiles/my/all",
            HttpStatusCode.OK,
            expectedProfiles
        );
    }

    protected override void SetupGetMyActiveProfileMock(
        string keycloakId,
        ActiveProfileDto? expectedProfile)
    {
        if (expectedProfile == null)
        {
            MockHttpResponse(
                HttpMethod.Get,
                "/api/profiles/my/active",
                HttpStatusCode.NotFound,
                new { error = "Not found" }
            );
        }
        else
        {
            MockHttpResponse(
                HttpMethod.Get,
                "/api/profiles/my/active",
                HttpStatusCode.OK,
                expectedProfile
            );
        }
    }

    protected override void SetupSetMyActiveProfileMock(
        string keycloakId,
        Guid profileId,
        ActiveProfileDto? expectedProfile)
    {
        if (expectedProfile == null)
        {
            MockHttpResponse(
                HttpMethod.Put,
                $"/api/profiles/my/{profileId}/set-active",
                HttpStatusCode.BadRequest,
                new { error = "Invalid profile" }
            );
        }
        else
        {
            MockHttpResponse(
                HttpMethod.Put,
                $"/api/profiles/my/{profileId}/set-active",
                HttpStatusCode.OK,
                expectedProfile
            );
        }
    }

    protected override void SetupUnauthenticatedContext()
    {
        // For HTTP client, we mock 401 Unauthorized responses
        // This simulates what happens when no JWT is provided
        MockHttpResponse<object>(
            HttpMethod.Post,
            "/api/profiles/my",
            HttpStatusCode.Unauthorized,
            new { error = "Unauthorized" }
        );

        MockHttpResponse<object>(
            HttpMethod.Get,
            "/api/profiles/my",
            HttpStatusCode.Unauthorized,
            new { error = "Unauthorized" }
        );

        MockHttpResponse<object>(
            HttpMethod.Put,
            "/api/profiles/my",
            HttpStatusCode.Unauthorized,
            new { error = "Unauthorized" }
        );

        MockHttpResponse<object>(
            HttpMethod.Delete,
            "/api/profiles/my",
            HttpStatusCode.Unauthorized,
            new { error = "Unauthorized" }
        );
    }

    #endregion

    #region Verification Implementation

    protected override void VerifyCreateMyProfileWasCalledCorrectly(
        string keycloakId,
        CreateProfileDto request)
    {
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(msg =>
                msg.Method == HttpMethod.Post &&
                msg.RequestUri!.PathAndQuery.Contains("/api/profiles/my")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    protected override void VerifyGetMyProfileWasCalledCorrectly(string keycloakId)
    {
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(msg =>
                msg.Method == HttpMethod.Get &&
                msg.RequestUri!.PathAndQuery.Contains("/api/profiles/my") &&
                !msg.RequestUri.PathAndQuery.Contains("/set-active")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    protected override void VerifyUpdateMyProfileWasCalledCorrectly(
        string keycloakId,
        UpdateProfileDto request)
    {
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(msg =>
                msg.Method == HttpMethod.Put &&
                msg.RequestUri!.PathAndQuery.Contains("/api/profiles/my") &&
                !msg.RequestUri.PathAndQuery.Contains("/set-active")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    protected override void VerifyDeleteMyProfileWasCalledCorrectly(string keycloakId)
    {
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(msg =>
                msg.Method == HttpMethod.Delete &&
                msg.RequestUri!.PathAndQuery.Contains("/api/profiles/my")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    protected override void VerifyDeleteMyProfileWasNotCalled()
    {
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.Is<HttpRequestMessage>(msg =>
                msg.Method == HttpMethod.Delete &&
                msg.RequestUri!.PathAndQuery.Contains("/api/profiles/my")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    protected override void VerifyGetAllMyProfilesWasCalledCorrectly(string keycloakId)
    {
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(msg =>
                msg.Method == HttpMethod.Get &&
                msg.RequestUri!.PathAndQuery.Contains("/api/profiles/my/all")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    protected override void VerifyGetMyActiveProfileWasCalledCorrectly(string keycloakId)
    {
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.AtLeastOnce(),
            ItExpr.Is<HttpRequestMessage>(msg =>
                msg.Method == HttpMethod.Get &&
                msg.RequestUri!.PathAndQuery.Contains("/api/profiles/my/active")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    protected override void VerifySetMyActiveProfileWasCalledCorrectly(
        string keycloakId,
        Guid profileId)
    {
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(msg =>
                msg.Method == HttpMethod.Put &&
                msg.RequestUri!.PathAndQuery.Contains($"/api/profiles/my/{profileId}/set-active")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Mocks an HTTP response for a specific endpoint
    /// </summary>
    private void MockHttpResponse<T>(
        HttpMethod method,
        string endpoint,
        HttpStatusCode statusCode,
        T? responseBody)
    {
        var content = responseBody != null
            ? new StringContent(
                JsonSerializer.Serialize(responseBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                System.Text.Encoding.UTF8,
                "application/json"
            )
            : new StringContent("");

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(msg =>
                    msg.Method == method &&
                    msg.RequestUri!.PathAndQuery.Contains(endpoint)
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = content
            });
    }

    #endregion
}
