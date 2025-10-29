using FluentAssertions;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Xunit;

namespace Sivar.Os.Tests.Clients;

/// <summary>
/// Contract tests for IFollowersClient
/// These tests define the expected behavior that all implementations must satisfy
/// </summary>
public abstract class FollowersClientContractTests
{
    protected IFollowersClient Client { get; set; } = null!;

    protected abstract void SetupClient();

    // Abstract methods for setting up mocks - each implementation provides its own
    protected abstract void SetupFollowAsyncMock(FollowActionDto request, FollowResultDto expectedResult);
    protected abstract void SetupUnfollowAsyncMock(Guid profileId);
    protected abstract void SetupGetFollowersAsyncMock(IEnumerable<FollowerProfileDto> expectedFollowers);
    protected abstract void SetupGetFollowingAsyncMock(IEnumerable<FollowingProfileDto> expectedFollowing);
    protected abstract void SetupGetStatsAsyncMock(FollowerStatsDto expectedStats);
    protected abstract void SetupGetFollowingStatusAsyncMock(Guid targetProfileId, bool isFollowing);
    protected abstract void SetupGetMutualFollowersAsyncMock(Guid otherProfileId, IEnumerable<ProfileFollowerDto> expectedMutual);
    protected abstract void SetupGetStatsForProfileAsyncMock(Guid profileId, FollowerStatsDto expectedStats);
    protected abstract void SetupGetFollowersForProfileAsyncMock(Guid profileId, IEnumerable<FollowerProfileDto> expectedFollowers);
    protected abstract void SetupGetFollowingForProfileAsyncMock(Guid profileId, IEnumerable<FollowingProfileDto> expectedFollowing);

    [Fact]
    public async Task FollowAsync_WithValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        SetupClient();
        var profileToFollowId = Guid.NewGuid();
        var request = new FollowActionDto { ProfileToFollowId = profileToFollowId };
        var expectedResult = new FollowResultDto 
        { 
            Success = true, 
            Message = "Successfully followed profile" 
        };

        SetupFollowAsyncMock(request, expectedResult);

        // Act
        var result = await Client.FollowAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Successfully followed profile");
    }

    [Fact]
    public async Task FollowAsync_WhenAlreadyFollowing_ReturnsFailureResult()
    {
        // Arrange
        SetupClient();
        var profileToFollowId = Guid.NewGuid();
        var request = new FollowActionDto { ProfileToFollowId = profileToFollowId };
        var expectedResult = new FollowResultDto 
        { 
            Success = false, 
            Message = "Already following this profile" 
        };

        SetupFollowAsyncMock(request, expectedResult);

        // Act
        var result = await Client.FollowAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Already following this profile");
    }

    [Fact]
    public async Task UnfollowAsync_WithValidProfileId_CompletesSuccessfully()
    {
        // Arrange
        SetupClient();
        var profileToUnfollowId = Guid.NewGuid();

        SetupUnfollowAsyncMock(profileToUnfollowId);

        // Act
        await Client.UnfollowAsync(profileToUnfollowId);

        // Assert
        // Method should complete without throwing
        true.Should().BeTrue();
    }

    [Fact]
    public async Task GetFollowersAsync_ReturnsFollowersList()
    {
        // Arrange
        SetupClient();
        var expectedFollowers = new List<FollowerProfileDto>
        {
            new() { Id = Guid.NewGuid(), DisplayName = "Follower 1", FollowedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), DisplayName = "Follower 2", FollowedAt = DateTime.UtcNow }
        };

        SetupGetFollowersAsyncMock(expectedFollowers);

        // Act
        var result = await Client.GetFollowersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFollowingAsync_ReturnsFollowingList()
    {
        // Arrange
        SetupClient();
        var expectedFollowing = new List<FollowingProfileDto>
        {
            new() { Id = Guid.NewGuid(), DisplayName = "Following 1", FollowedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), DisplayName = "Following 2", FollowedAt = DateTime.UtcNow }
        };

        SetupGetFollowingAsyncMock(expectedFollowing);

        // Act
        var result = await Client.GetFollowingAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsFollowerStats()
    {
        // Arrange
        SetupClient();
        var expectedStats = new FollowerStatsDto
        {
            ProfileId = Guid.NewGuid(),
            FollowersCount = 10,
            FollowingCount = 5,
            IsFollowedByCurrentUser = false
        };

        SetupGetStatsAsyncMock(expectedStats);

        // Act
        var result = await Client.GetStatsAsync();

        // Assert
        result.Should().NotBeNull();
        result.FollowersCount.Should().Be(10);
        result.FollowingCount.Should().Be(5);
        result.IsFollowedByCurrentUser.Should().BeFalse();
    }

    [Fact]
    public async Task GetFollowingStatusAsync_WhenFollowing_ReturnsTrue()
    {
        // Arrange
        SetupClient();
        var targetProfileId = Guid.NewGuid();

        SetupGetFollowingStatusAsyncMock(targetProfileId, true);

        // Act
        var result = await Client.GetFollowingStatusAsync(targetProfileId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetFollowingStatusAsync_WhenNotFollowing_ReturnsFalse()
    {
        // Arrange
        SetupClient();
        var targetProfileId = Guid.NewGuid();

        SetupGetFollowingStatusAsyncMock(targetProfileId, false);

        // Act
        var result = await Client.GetFollowingStatusAsync(targetProfileId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatsForProfileAsync_ReturnsStatsForSpecificProfile()
    {
        // Arrange
        SetupClient();
        var profileId = Guid.NewGuid();
        var expectedStats = new FollowerStatsDto
        {
            ProfileId = profileId,
            FollowersCount = 100,
            FollowingCount = 50,
            IsFollowedByCurrentUser = true
        };

        SetupGetStatsForProfileAsyncMock(profileId, expectedStats);

        // Act
        var result = await Client.GetStatsForProfileAsync(profileId);

        // Assert
        result.Should().NotBeNull();
        result.ProfileId.Should().Be(profileId);
        result.FollowersCount.Should().Be(100);
        result.FollowingCount.Should().Be(50);
        result.IsFollowedByCurrentUser.Should().BeTrue();
    }

    [Fact]
    public async Task GetFollowersForProfileAsync_ReturnsFollowersForSpecificProfile()
    {
        // Arrange
        SetupClient();
        var profileId = Guid.NewGuid();
        var expectedFollowers = new List<FollowerProfileDto>
        {
            new() { Id = Guid.NewGuid(), DisplayName = "Follower A", FollowedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), DisplayName = "Follower B", FollowedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), DisplayName = "Follower C", FollowedAt = DateTime.UtcNow }
        };

        SetupGetFollowersForProfileAsyncMock(profileId, expectedFollowers);

        // Act
        var result = await Client.GetFollowersForProfileAsync(profileId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetFollowingForProfileAsync_ReturnsFollowingForSpecificProfile()
    {
        // Arrange
        SetupClient();
        var profileId = Guid.NewGuid();
        var expectedFollowing = new List<FollowingProfileDto>
        {
            new() { Id = Guid.NewGuid(), DisplayName = "Following A", FollowedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), DisplayName = "Following B", FollowedAt = DateTime.UtcNow }
        };

        SetupGetFollowingForProfileAsyncMock(profileId, expectedFollowing);

        // Act
        var result = await Client.GetFollowingForProfileAsync(profileId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }
}
