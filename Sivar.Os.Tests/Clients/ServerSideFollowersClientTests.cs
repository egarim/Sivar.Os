using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;
using Sivar.Os.Tests.Fixtures;
using Xunit;

namespace Sivar.Os.Tests.Clients;

/// <summary>
/// Tests for the server-side FollowersClient implementation
/// These tests verify that the server-side client correctly:
/// 1. Extracts keycloakId from HttpContext claims
/// 2. Calls the IProfileFollowerService methods
/// 3. Returns the expected data
/// </summary>
public class ServerSideFollowersClientTests : FollowersClientContractTests
{
    private Mock<IProfileFollowerService> _profileFollowerServiceMock = null!;
    private Mock<IProfileFollowerRepository> _profileFollowerRepositoryMock = null!;
    private Mock<IProfileService> _profileServiceMock = null!;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
    private Mock<ILogger<Sivar.Os.Services.Clients.FollowersClient>> _loggerMock = null!;
    private Mock<IServiceScopeFactory> _serviceScopeFactoryMock = null!;

    private const string TestKeycloakId = "test-keycloak-id";
    private readonly Guid _testProfileId = Guid.NewGuid();

    protected override void SetupClient()
    {
        _profileFollowerServiceMock = new Mock<IProfileFollowerService>();
        _profileFollowerRepositoryMock = new Mock<IProfileFollowerRepository>();
        _profileServiceMock = new Mock<IProfileService>();
        _httpContextAccessorMock = AuthenticationTestFixture.CreateMockHttpContextAccessor(TestKeycloakId);
        _loggerMock = new Mock<ILogger<Sivar.Os.Services.Clients.FollowersClient>>();

        // Setup IServiceScopeFactory mock
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IProfileService)))
            .Returns(_profileServiceMock.Object);
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IProfileFollowerService)))
            .Returns(_profileFollowerServiceMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(scopeMock.Object);

        // Setup default: GetMyActiveProfileAsync returns a test profile
        _profileServiceMock
            .Setup(s => s.GetMyActiveProfileAsync(TestKeycloakId))
            .ReturnsAsync(new ProfileDto { Id = _testProfileId });

        Client = new Sivar.Os.Services.Clients.FollowersClient(
            _profileFollowerServiceMock.Object,
            _profileFollowerRepositoryMock.Object,
            _profileServiceMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object
        );
    }

    #region Setup Mocks Implementation

    protected override void SetupFollowAsyncMock(FollowActionDto request, FollowResultDto expectedResult)
    {
        _profileFollowerServiceMock
            .Setup(s => s.FollowProfileAsync(_testProfileId, request.ProfileToFollowId))
            .ReturnsAsync(expectedResult);
    }

    protected override void SetupUnfollowAsyncMock(Guid profileId)
    {
        _profileFollowerServiceMock
            .Setup(s => s.UnfollowProfileAsync(_testProfileId, profileId))
            .ReturnsAsync(new FollowResultDto { Success = true });
    }

    protected override void SetupGetFollowersAsyncMock(IEnumerable<FollowerProfileDto> expectedFollowers)
    {
        _profileFollowerServiceMock
            .Setup(s => s.GetFollowersAsync(_testProfileId, _testProfileId))
            .ReturnsAsync(expectedFollowers);
    }

    protected override void SetupGetFollowingAsyncMock(IEnumerable<FollowingProfileDto> expectedFollowing)
    {
        _profileFollowerServiceMock
            .Setup(s => s.GetFollowingAsync(_testProfileId, _testProfileId))
            .ReturnsAsync(expectedFollowing);
    }

    protected override void SetupGetStatsAsyncMock(FollowerStatsDto expectedStats)
    {
        _profileFollowerServiceMock
            .Setup(s => s.GetFollowerStatsAsync(_testProfileId, _testProfileId))
            .ReturnsAsync(expectedStats);
    }

    protected override void SetupGetFollowingStatusAsyncMock(Guid targetProfileId, bool isFollowing)
    {
        _profileFollowerServiceMock
            .Setup(s => s.IsFollowingAsync(_testProfileId, targetProfileId))
            .ReturnsAsync(isFollowing);
    }

    protected override void SetupGetMutualFollowersAsyncMock(Guid otherProfileId, IEnumerable<ProfileFollowerDto> expectedMutual)
    {
        // Note: GetMutualFollowersAsync returns FollowerProfileDto, not ProfileFollowerDto
        // This is a known interface mismatch - for now, return empty list
        _profileFollowerServiceMock
            .Setup(s => s.GetMutualFollowersAsync(_testProfileId, otherProfileId))
            .ReturnsAsync(new List<FollowerProfileDto>());
    }

    protected override void SetupGetStatsForProfileAsyncMock(Guid profileId, FollowerStatsDto expectedStats)
    {
        _profileFollowerServiceMock
            .Setup(s => s.GetFollowerStatsAsync(profileId, null))
            .ReturnsAsync(expectedStats);
    }

    protected override void SetupGetFollowersForProfileAsyncMock(Guid profileId, IEnumerable<FollowerProfileDto> expectedFollowers)
    {
        _profileFollowerServiceMock
            .Setup(s => s.GetFollowersAsync(profileId, null))
            .ReturnsAsync(expectedFollowers);
    }

    protected override void SetupGetFollowingForProfileAsyncMock(Guid profileId, IEnumerable<FollowingProfileDto> expectedFollowing)
    {
        _profileFollowerServiceMock
            .Setup(s => s.GetFollowingAsync(profileId, null))
            .ReturnsAsync(expectedFollowing);
    }

    #endregion

    #region Additional Server-Side Specific Tests

    [Fact]
    public async Task FollowAsync_WhenNotAuthenticated_ReturnsFailureResult()
    {
        // Arrange - initialize mocks first
        SetupClient();
        
        // Create a client with unauthenticated context
        var unauthenticatedHttpContext = AuthenticationTestFixture.CreateMockHttpContextAccessorUnauthenticated();
        var client = new Sivar.Os.Services.Clients.FollowersClient(
            _profileFollowerServiceMock.Object,
            _profileFollowerRepositoryMock.Object,
            _profileServiceMock.Object,
            unauthenticatedHttpContext.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object
        );

        var request = new FollowActionDto { ProfileToFollowId = Guid.NewGuid() };

        // Act
        var result = await client.FollowAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("authenticated");
    }

    [Fact]
    public async Task FollowAsync_WhenNoActiveProfile_ReturnsFailureResult()
    {
        // Arrange
        SetupClient();
        
        // Override the default setup to return null
        _profileServiceMock
            .Setup(s => s.GetMyActiveProfileAsync(TestKeycloakId))
            .ReturnsAsync((ProfileDto?)null);

        var request = new FollowActionDto { ProfileToFollowId = Guid.NewGuid() };

        // Act
        var result = await Client.FollowAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("active profile");
    }

    [Fact]
    public async Task UnfollowAsync_WhenNotAuthenticated_CompletesWithoutError()
    {
        // Arrange - initialize mocks first
        SetupClient();
        
        // Create a client with unauthenticated context
        var unauthenticatedHttpContext = AuthenticationTestFixture.CreateMockHttpContextAccessorUnauthenticated();
        var client = new Sivar.Os.Services.Clients.FollowersClient(
            _profileFollowerServiceMock.Object,
            _profileFollowerRepositoryMock.Object,
            _profileServiceMock.Object,
            unauthenticatedHttpContext.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object
        );

        // Act
        await client.UnfollowAsync(Guid.NewGuid());

        // Assert
        // Should complete without throwing - service method should not be called
        _profileFollowerServiceMock.Verify(
            s => s.UnfollowProfileAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task GetFollowersAsync_WhenNotAuthenticated_ReturnsEmptyList()
    {
        // Arrange - initialize mocks first
        SetupClient();
        
        // Create a client with unauthenticated context
        var unauthenticatedHttpContext = AuthenticationTestFixture.CreateMockHttpContextAccessorUnauthenticated();
        var client = new Sivar.Os.Services.Clients.FollowersClient(
            _profileFollowerServiceMock.Object,
            _profileFollowerRepositoryMock.Object,
            _profileServiceMock.Object,
            unauthenticatedHttpContext.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object
        );

        // Act
        var result = await client.GetFollowersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatsAsync_WhenNotAuthenticated_ReturnsEmptyStats()
    {
        // Arrange - initialize mocks first
        SetupClient();
        
        // Create a client with unauthenticated context
        var unauthenticatedHttpContext = AuthenticationTestFixture.CreateMockHttpContextAccessorUnauthenticated();
        var client = new Sivar.Os.Services.Clients.FollowersClient(
            _profileFollowerServiceMock.Object,
            _profileFollowerRepositoryMock.Object,
            _profileServiceMock.Object,
            unauthenticatedHttpContext.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object
        );

        // Act
        var result = await client.GetStatsAsync();

        // Assert
        result.Should().NotBeNull();
        result.FollowersCount.Should().Be(0);
        result.FollowingCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFollowingStatusAsync_WhenNotAuthenticated_ReturnsFalse()
    {
        // Arrange - initialize mocks first
        SetupClient();
        
        // Create a client with unauthenticated context
        var unauthenticatedHttpContext = AuthenticationTestFixture.CreateMockHttpContextAccessorUnauthenticated();
        var client = new Sivar.Os.Services.Clients.FollowersClient(
            _profileFollowerServiceMock.Object,
            _profileFollowerRepositoryMock.Object,
            _profileServiceMock.Object,
            unauthenticatedHttpContext.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object
        );

        // Act
        var result = await client.GetFollowingStatusAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatsForProfileAsync_WithValidProfileId_CallsServiceWithNullCurrentUser()
    {
        // Arrange
        SetupClient();
        var profileId = Guid.NewGuid();
        var expectedStats = new FollowerStatsDto
        {
            ProfileId = profileId,
            FollowersCount = 50,
            FollowingCount = 25
        };

        SetupGetStatsForProfileAsyncMock(profileId, expectedStats);

        // Act
        var result = await Client.GetStatsForProfileAsync(profileId);

        // Assert
        result.Should().NotBeNull();
        result.ProfileId.Should().Be(profileId);
        
        // Verify service was called with null for current user (server-side doesn't have current user context)
        _profileFollowerServiceMock.Verify(
            s => s.GetFollowerStatsAsync(profileId, null),
            Times.Once);
    }

    #endregion
}
