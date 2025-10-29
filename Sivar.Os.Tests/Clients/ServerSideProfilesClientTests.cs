using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Sivar.Os.Client.Services;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;
using Sivar.Os.Tests.Fixtures;

namespace Sivar.Os.Tests.Clients;

/// <summary>
/// Tests for the server-side ProfilesClient implementation
/// These tests verify that the server-side client correctly:
/// 1. Extracts keycloakId from HttpContext claims
/// 2. Calls the IProfileService methods
/// 3. Returns the expected data
/// </summary>
public class ServerSideProfilesClientTests : ProfilesClientContractTests
{
    private Mock<IProfileService> _profileServiceMock = null!;
    private Mock<IProfileRepository> _profileRepositoryMock = null!;
    private Mock<IProfileSwitcherService> _profileSwitcherServiceMock = null!;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
    private Mock<ILogger<Sivar.Os.Services.Clients.ProfilesClient>> _loggerMock = null!;

    protected override void SetupClient()
    {
        _profileServiceMock = new Mock<IProfileService>();
        _profileRepositoryMock = new Mock<IProfileRepository>();
        _profileSwitcherServiceMock = new Mock<IProfileSwitcherService>();
        _httpContextAccessorMock = AuthenticationTestFixture
            .CreateMockHttpContextAccessor(ProfilesTestDataFixture.TestKeycloakId);
        _loggerMock = new Mock<ILogger<Sivar.Os.Services.Clients.ProfilesClient>>();

        Client = new Sivar.Os.Services.Clients.ProfilesClient(
            _profileServiceMock.Object,
            _profileRepositoryMock.Object,
            _profileSwitcherServiceMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object
        );
    }

    #region Setup Mocks Implementation

    protected override void SetupCreateMyProfileMock(
        string keycloakId,
        CreateProfileDto request,
        ProfileDto? expectedProfile)
    {
        _profileServiceMock
            .Setup(s => s.CreateMyProfileAsync(keycloakId, request))
            .ReturnsAsync(expectedProfile);
    }

    protected override void SetupGetMyProfileMock(
        string keycloakId,
        ProfileDto? expectedProfile)
    {
        _profileServiceMock
            .Setup(s => s.GetMyProfileAsync(keycloakId))
            .ReturnsAsync(expectedProfile);
    }

    protected override void SetupUpdateMyProfileMock(
        string keycloakId,
        UpdateProfileDto request,
        ProfileDto? expectedProfile)
    {
        _profileServiceMock
            .Setup(s => s.UpdateMyProfileAsync(keycloakId, request))
            .ReturnsAsync(expectedProfile);
    }

    protected override void SetupDeleteMyProfileMock(string keycloakId)
    {
        _profileServiceMock
            .Setup(s => s.DeleteMyProfileAsync(keycloakId))
            .ReturnsAsync(true);
    }

    protected override void SetupGetAllMyProfilesMock(
        string keycloakId,
        IEnumerable<ProfileDto> expectedProfiles)
    {
        _profileServiceMock
            .Setup(s => s.GetMyProfilesAsync(keycloakId))
            .ReturnsAsync(expectedProfiles);
    }

    protected override void SetupGetMyActiveProfileMock(
        string keycloakId,
        ActiveProfileDto? expectedProfile)
    {
        // Note: GetMyActiveProfileAsync returns ProfileDto, so we need to adapt
        var profileDto = expectedProfile == null
            ? null
            : new ProfileDto { Id = expectedProfile.Id };

        _profileServiceMock
            .Setup(s => s.GetMyActiveProfileAsync(keycloakId))
            .ReturnsAsync(profileDto);
    }

    protected override void SetupSetMyActiveProfileMock(
        string keycloakId,
        Guid profileId,
        ActiveProfileDto? expectedProfile)
    {
        _profileServiceMock
            .Setup(s => s.SetActiveProfileAsync(keycloakId, profileId))
            .ReturnsAsync(expectedProfile != null);

        // Also setup GetMyActiveProfileAsync for after the set
        if (expectedProfile != null)
        {
            _profileServiceMock
                .Setup(s => s.GetMyActiveProfileAsync(keycloakId))
                .ReturnsAsync(new ProfileDto { Id = expectedProfile.Id });
        }
    }

    protected override void SetupUnauthenticatedContext()
    {
        _httpContextAccessorMock = AuthenticationTestFixture
            .CreateMockHttpContextAccessorUnauthenticated();

        // Recreate client with unauthenticated context
        Client = new Sivar.Os.Services.Clients.ProfilesClient(
            _profileServiceMock.Object,
            _profileRepositoryMock.Object,
            _profileSwitcherServiceMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object
        );
    }

    #endregion

    #region Verification Implementation

    protected override void VerifyCreateMyProfileWasCalledCorrectly(
        string keycloakId,
        CreateProfileDto request)
    {
        _profileServiceMock.Verify(
            s => s.CreateMyProfileAsync(keycloakId, request),
            Times.Once,
            $"CreateMyProfileAsync should be called exactly once with keycloakId={keycloakId}"
        );
    }

    protected override void VerifyGetMyProfileWasCalledCorrectly(string keycloakId)
    {
        _profileServiceMock.Verify(
            s => s.GetMyProfileAsync(keycloakId),
            Times.Once,
            $"GetMyProfileAsync should be called exactly once with keycloakId={keycloakId}"
        );
    }

    protected override void VerifyUpdateMyProfileWasCalledCorrectly(
        string keycloakId,
        UpdateProfileDto request)
    {
        _profileServiceMock.Verify(
            s => s.UpdateMyProfileAsync(keycloakId, request),
            Times.Once,
            $"UpdateMyProfileAsync should be called exactly once with keycloakId={keycloakId}"
        );
    }

    protected override void VerifyDeleteMyProfileWasCalledCorrectly(string keycloakId)
    {
        _profileServiceMock.Verify(
            s => s.DeleteMyProfileAsync(keycloakId),
            Times.Once,
            $"DeleteMyProfileAsync should be called exactly once with keycloakId={keycloakId}"
        );
    }

    protected override void VerifyDeleteMyProfileWasNotCalled()
    {
        _profileServiceMock.Verify(
            s => s.DeleteMyProfileAsync(It.IsAny<string>()),
            Times.Never,
            "DeleteMyProfileAsync should not be called for unauthenticated user"
        );
    }

    protected override void VerifyGetAllMyProfilesWasCalledCorrectly(string keycloakId)
    {
        _profileServiceMock.Verify(
            s => s.GetMyProfilesAsync(keycloakId),
            Times.Once,
            $"GetMyProfilesAsync should be called exactly once with keycloakId={keycloakId}"
        );
    }

    protected override void VerifyGetMyActiveProfileWasCalledCorrectly(string keycloakId)
    {
        _profileServiceMock.Verify(
            s => s.GetMyActiveProfileAsync(keycloakId),
            Times.AtLeastOnce,
            $"GetMyActiveProfileAsync should be called with keycloakId={keycloakId}"
        );
    }

    protected override void VerifySetMyActiveProfileWasCalledCorrectly(
        string keycloakId,
        Guid profileId)
    {
        _profileServiceMock.Verify(
            s => s.SetActiveProfileAsync(keycloakId, profileId),
            Times.Once,
            $"SetActiveProfileAsync should be called exactly once with keycloakId={keycloakId}, profileId={profileId}"
        );
    }

    #endregion
}
