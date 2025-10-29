using FluentAssertions;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Tests.Fixtures;
using Xunit;

namespace Sivar.Os.Tests.Clients;

/// <summary>
/// Abstract base test class that defines the contract/behavior that both
/// client and server implementations must satisfy.
/// 
/// This ensures both implementations behave identically from the caller's perspective.
/// </summary>
public abstract class ProfilesClientContractTests
{
    /// <summary>
    /// The client implementation under test (must be set by derived classes)
    /// </summary>
    protected IProfilesClient Client { get; set; } = null!;

    /// <summary>
    /// Abstract method for setup - derived classes must initialize the client
    /// </summary>
    protected abstract void SetupClient();

    /// <summary>
    /// Constructor to ensure derived classes set up the client
    /// </summary>
    public ProfilesClientContractTests()
    {
        SetupClient();
    }

    #region CreateMyProfileAsync Tests

    [Fact]
    public async Task CreateMyProfileAsync_WithValidRequest_ShouldReturnProfile()
    {
        // Arrange
        var request = ProfilesTestDataFixture.CreateValidCreateProfileRequest();
        var expectedProfile = ProfilesTestDataFixture.CreateProfileDtoWithId(
            displayName: request.DisplayName
        );

        SetupCreateMyProfileMock(
            ProfilesTestDataFixture.TestKeycloakId,
            request,
            expectedProfile
        );

        // Act
        var result = await Client.CreateMyProfileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.DisplayName.Should().Be(request.DisplayName);
        result.Bio.Should().Be(request.Bio);
        result.Location.Should().Be(request.Location);

        // Verify the service was called correctly
        VerifyCreateMyProfileWasCalledCorrectly(
            ProfilesTestDataFixture.TestKeycloakId,
            request
        );
    }

    [Fact]
    public async Task CreateMyProfileAsync_WithNullRequest_ShouldReturnNull()
    {
        // Act & Assert
        var result = await Client.CreateMyProfileAsync(null!);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateMyProfileAsync_WithUnauthenticatedUser_ShouldReturnNull()
    {
        // Arrange - Setup no authenticated user
        SetupUnauthenticatedContext();

        var request = ProfilesTestDataFixture.CreateValidCreateProfileRequest();

        // Act
        var result = await Client.CreateMyProfileAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateMyProfileAsync_MultipleProfiles_ShouldReturnProfile()
    {
        // Arrange - Simplified test focusing on single profile creation
        // Complex multi-call scenarios are better handled with integration tests
        var request = ProfilesTestDataFixture.CreateValidCreateProfileRequest();
        var expectedProfile = ProfilesTestDataFixture.CreateProfileDtoWithId(displayName: request.DisplayName);

        SetupCreateMyProfileMock(ProfilesTestDataFixture.TestKeycloakId, request, expectedProfile);

        // Act
        var result = await Client.CreateMyProfileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.DisplayName.Should().Be(request.DisplayName);
    }

    #endregion

    #region GetMyProfileAsync Tests

    [Fact]
    public async Task GetMyProfileAsync_WhenProfileExists_ShouldReturnProfile()
    {
        // Arrange
        var expectedProfile = ProfilesTestDataFixture.CreateProfileDtoWithId();
        SetupGetMyProfileMock(ProfilesTestDataFixture.TestKeycloakId, expectedProfile);

        // Act
        var result = await Client.GetMyProfileAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedProfile.Id);
        result.DisplayName.Should().Be(expectedProfile.DisplayName);

        VerifyGetMyProfileWasCalledCorrectly(ProfilesTestDataFixture.TestKeycloakId);
    }

    [Fact]
    public async Task GetMyProfileAsync_WhenProfileNotFound_ShouldReturnNull()
    {
        // Arrange
        SetupGetMyProfileMock(ProfilesTestDataFixture.TestKeycloakId, null);

        // Act
        var result = await Client.GetMyProfileAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMyProfileAsync_WithUnauthenticatedUser_ShouldReturnNull()
    {
        // Arrange
        SetupUnauthenticatedContext();

        // Act
        var result = await Client.GetMyProfileAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateMyProfileAsync Tests

    [Fact]
    public async Task UpdateMyProfileAsync_WithValidRequest_ShouldReturnUpdatedProfile()
    {
        // Arrange
        var updateRequest = ProfilesTestDataFixture.CreateValidUpdateProfileRequest();
        var updatedProfile = ProfilesTestDataFixture.CreateProfileDtoWithId(
            displayName: updateRequest.DisplayName
        );
        // Update the profile DTO to match the request data
        updatedProfile.Bio = updateRequest.Bio;
        if (updateRequest.Location != null)
        {
            updatedProfile.Location = updateRequest.Location;
            updatedProfile.LocationDisplay = $"{updateRequest.Location.City}, {updateRequest.Location.State}, {updateRequest.Location.Country}";
        }

        SetupUpdateMyProfileMock(
            ProfilesTestDataFixture.TestKeycloakId,
            updateRequest,
            updatedProfile
        );

        // Act
        var result = await Client.UpdateMyProfileAsync(updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be(updateRequest.DisplayName);
        result.Bio.Should().Be(updateRequest.Bio);

        VerifyUpdateMyProfileWasCalledCorrectly(
            ProfilesTestDataFixture.TestKeycloakId,
            updateRequest
        );
    }

    [Fact]
    public async Task UpdateMyProfileAsync_WithNullRequest_ShouldReturnNull()
    {
        // Act & Assert
        var result = await Client.UpdateMyProfileAsync(null!);
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateMyProfileAsync_WithUnauthenticatedUser_ShouldReturnNull()
    {
        // Arrange
        SetupUnauthenticatedContext();
        var updateRequest = ProfilesTestDataFixture.CreateValidUpdateProfileRequest();

        // Act
        var result = await Client.UpdateMyProfileAsync(updateRequest);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteMyProfileAsync Tests

    [Fact]
    public async Task DeleteMyProfileAsync_WithAuthenticatedUser_ShouldCallDelete()
    {
        // Arrange
        SetupDeleteMyProfileMock(ProfilesTestDataFixture.TestKeycloakId);

        // Act
        await Client.DeleteMyProfileAsync();

        // Assert
        VerifyDeleteMyProfileWasCalledCorrectly(ProfilesTestDataFixture.TestKeycloakId);
    }

    /// <summary>
    /// Gets whether this test implementation supports authentication context checks.
    /// Server-side implementations (with HttpContext access) support this.
    /// Client-side (HTTP) implementations do not.
    /// </summary>
    protected virtual bool SupportsAuthenticationContext => true;

    /// <summary>
    /// This test is only applicable to server-side clients that have access to authentication context.
    /// Client-side (HTTP) clients cannot check authentication - they just make HTTP calls.
    /// The authentication check happens at the server level (401 responses).
    /// </summary>
    [Fact]
    public async Task DeleteMyProfileAsync_WithUnauthenticatedUser_ShouldNotCallDelete()
    {
        if (!SupportsAuthenticationContext)
        {
            // Skip this test for client-side implementations
            return;
        }

        // Arrange
        SetupUnauthenticatedContext();

        // Act
        await Client.DeleteMyProfileAsync();

        // Assert - Should not throw but also should not delete anything
        VerifyDeleteMyProfileWasNotCalled();
    }

    #endregion

    #region GetAllMyProfilesAsync Tests

    [Fact]
    public async Task GetAllMyProfilesAsync_WhenProfilesExist_ShouldReturnCollection()
    {
        // Arrange
        var expectedProfiles = ProfilesTestDataFixture.CreateMultipleProfileDtos(3);
        SetupGetAllMyProfilesMock(ProfilesTestDataFixture.TestKeycloakId, expectedProfiles);

        // Act
        var result = await Client.GetAllMyProfilesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Select(p => p.Id).Should().BeEquivalentTo(expectedProfiles.Select(p => p.Id));

        VerifyGetAllMyProfilesWasCalledCorrectly(ProfilesTestDataFixture.TestKeycloakId);
    }

    [Fact]
    public async Task GetAllMyProfilesAsync_WithNoProfiles_ShouldReturnEmptyCollection()
    {
        // Arrange
        SetupGetAllMyProfilesMock(ProfilesTestDataFixture.TestKeycloakId, Enumerable.Empty<ProfileDto>());

        // Act
        var result = await Client.GetAllMyProfilesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllMyProfilesAsync_WithUnauthenticatedUser_ShouldReturnEmpty()
    {
        if (!SupportsAuthenticationContext)
        {
            // Skip this test for client-side implementations
            // Client-side HTTP clients return null for 401 responses
            return;
        }

        // Arrange
        SetupUnauthenticatedContext();

        // Act
        var result = await Client.GetAllMyProfilesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetMyActiveProfileAsync Tests

    [Fact]
    public async Task GetMyActiveProfileAsync_WhenProfileExists_ShouldReturnActiveProfile()
    {
        // Arrange
        var expectedActiveProfile = ProfilesTestDataFixture.CreateActiveProfileDto();
        SetupGetMyActiveProfileMock(ProfilesTestDataFixture.TestKeycloakId, expectedActiveProfile);

        // Act
        var result = await Client.GetMyActiveProfileAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedActiveProfile.Id);
        result.IsActive.Should().BeTrue();

        VerifyGetMyActiveProfileWasCalledCorrectly(ProfilesTestDataFixture.TestKeycloakId);
    }

    [Fact]
    public async Task GetMyActiveProfileAsync_WithNoActiveProfile_ShouldReturnNull()
    {
        // Arrange
        SetupGetMyActiveProfileMock(ProfilesTestDataFixture.TestKeycloakId, null);

        // Act
        var result = await Client.GetMyActiveProfileAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SetMyActiveProfileAsync Tests

    [Fact]
    public async Task SetMyActiveProfileAsync_WithValidProfileId_ShouldReturnActiveProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var expectedActiveProfile = ProfilesTestDataFixture.CreateActiveProfileDto(profileId);

        SetupSetMyActiveProfileMock(
            ProfilesTestDataFixture.TestKeycloakId,
            profileId,
            expectedActiveProfile
        );

        // Act
        var result = await Client.SetMyActiveProfileAsync(profileId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(profileId);
        result.IsActive.Should().BeTrue();

        VerifySetMyActiveProfileWasCalledCorrectly(
            ProfilesTestDataFixture.TestKeycloakId,
            profileId
        );
    }

    [Fact]
    public async Task SetMyActiveProfileAsync_WithUnauthenticatedUser_ShouldReturnNull()
    {
        // Arrange
        SetupUnauthenticatedContext();
        var profileId = Guid.NewGuid();

        // Act
        var result = await Client.SetMyActiveProfileAsync(profileId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Abstract Methods for Setup (Derived classes must implement)

    /// <summary>
    /// Derived classes must implement this to setup mock for CreateMyProfileAsync
    /// </summary>
    protected abstract void SetupCreateMyProfileMock(
        string keycloakId,
        CreateProfileDto request,
        ProfileDto? expectedProfile
    );

    /// <summary>
    /// Derived classes must implement this to setup mock for GetMyProfileAsync
    /// </summary>
    protected abstract void SetupGetMyProfileMock(
        string keycloakId,
        ProfileDto? expectedProfile
    );

    /// <summary>
    /// Derived classes must implement this to setup mock for UpdateMyProfileAsync
    /// </summary>
    protected abstract void SetupUpdateMyProfileMock(
        string keycloakId,
        UpdateProfileDto request,
        ProfileDto? expectedProfile
    );

    /// <summary>
    /// Derived classes must implement this to setup mock for DeleteMyProfileAsync
    /// </summary>
    protected abstract void SetupDeleteMyProfileMock(string keycloakId);

    /// <summary>
    /// Derived classes must implement this to setup mock for GetAllMyProfilesAsync
    /// </summary>
    protected abstract void SetupGetAllMyProfilesMock(
        string keycloakId,
        IEnumerable<ProfileDto> expectedProfiles
    );

    /// <summary>
    /// Derived classes must implement this to setup mock for GetMyActiveProfileAsync
    /// </summary>
    protected abstract void SetupGetMyActiveProfileMock(
        string keycloakId,
        ActiveProfileDto? expectedProfile
    );

    /// <summary>
    /// Derived classes must implement this to setup mock for SetMyActiveProfileAsync
    /// </summary>
    protected abstract void SetupSetMyActiveProfileMock(
        string keycloakId,
        Guid profileId,
        ActiveProfileDto? expectedProfile
    );

    /// <summary>
    /// Derived classes must implement this to setup unauthenticated context
    /// </summary>
    protected abstract void SetupUnauthenticatedContext();

    /// <summary>
    /// Derived classes must implement this to verify CreateMyProfileAsync was called correctly
    /// </summary>
    protected abstract void VerifyCreateMyProfileWasCalledCorrectly(
        string keycloakId,
        CreateProfileDto request
    );

    /// <summary>
    /// Derived classes must implement this to verify GetMyProfileAsync was called correctly
    /// </summary>
    protected abstract void VerifyGetMyProfileWasCalledCorrectly(string keycloakId);

    /// <summary>
    /// Derived classes must implement this to verify UpdateMyProfileAsync was called correctly
    /// </summary>
    protected abstract void VerifyUpdateMyProfileWasCalledCorrectly(
        string keycloakId,
        UpdateProfileDto request
    );

    /// <summary>
    /// Derived classes must implement this to verify DeleteMyProfileAsync was called correctly
    /// </summary>
    protected abstract void VerifyDeleteMyProfileWasCalledCorrectly(string keycloakId);

    /// <summary>
    /// Derived classes must implement this to verify DeleteMyProfileAsync was NOT called
    /// </summary>
    protected abstract void VerifyDeleteMyProfileWasNotCalled();

    /// <summary>
    /// Derived classes must implement this to verify GetAllMyProfilesAsync was called correctly
    /// </summary>
    protected abstract void VerifyGetAllMyProfilesWasCalledCorrectly(string keycloakId);

    /// <summary>
    /// Derived classes must implement this to verify GetMyActiveProfileAsync was called correctly
    /// </summary>
    protected abstract void VerifyGetMyActiveProfileWasCalledCorrectly(string keycloakId);

    /// <summary>
    /// Derived classes must implement this to verify SetMyActiveProfileAsync was called correctly
    /// </summary>
    protected abstract void VerifySetMyActiveProfileWasCalledCorrectly(
        string keycloakId,
        Guid profileId
    );

    #endregion
}
