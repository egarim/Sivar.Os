using Microsoft.Extensions.Logging;
using Moq;
using Sivar.Os.Services;
using Sivar.Os.Shared.DTOs.ValueObjects;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;
using Xunit;

namespace Sivar.Os.Tests.Services;

/// <summary>
/// Tests for ChatFunctionService search improvements
/// </summary>
public class ChatFunctionServiceSearchTests
{
    private readonly Mock<IPostRepository> _postRepoMock;
    private readonly Mock<IProfileRepository> _profileRepoMock;
    private readonly Mock<IProfileFollowerRepository> _followerRepoMock;
    private readonly Mock<ILocationService> _locationServiceMock;
    private readonly Mock<ICategoryNormalizer> _categoryNormalizerMock;
    private readonly Mock<ILogger<ChatFunctionService>> _loggerMock;
    private readonly ChatFunctionService _service;

    public ChatFunctionServiceSearchTests()
    {
        _postRepoMock = new Mock<IPostRepository>();
        _profileRepoMock = new Mock<IProfileRepository>();
        _followerRepoMock = new Mock<IProfileFollowerRepository>();
        _locationServiceMock = new Mock<ILocationService>();
        _categoryNormalizerMock = new Mock<ICategoryNormalizer>();
        _loggerMock = new Mock<ILogger<ChatFunctionService>>();

        // Default mock: normalizer returns empty list (fallback to content search)
        _categoryNormalizerMock.Setup(c => c.NormalizeQueryAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        _service = new ChatFunctionService(
            _profileRepoMock.Object,
            _postRepoMock.Object,
            _followerRepoMock.Object,
            _locationServiceMock.Object,
            _categoryNormalizerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SearchPosts_WithPizzeriaQuery_FindsPostInSanSalvador()
    {
        // Arrange - Create a post like the one in the database
        var pizzeriaPost = new Post
        {
            Id = Guid.NewGuid(),
            Content = "Pizzeria campestre",
            PostType = PostType.BusinessLocation,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            Location = new Location("San Salvador", "San Salvador", "El Salvador", 13.7052, -89.2453),
            Profile = new Profile { Id = Guid.NewGuid(), DisplayName = "Business" }
        };

        _postRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Post> { pizzeriaPost });

        // Act - Search for "pizzeria" in "San Salvador"
        var result = await _service.SearchPosts("pizzeria", city: "San Salvador");

        // Assert
        Assert.Contains("Pizzeria campestre", result);
        Assert.Contains("San Salvador", result);
        Assert.DoesNotContain("No posts found", result);
    }

    [Fact]
    public async Task FindBusinesses_WithPizzeriaInSanSalvador_FindsMatch()
    {
        // Arrange
        var pizzeriaPost = new Post
        {
            Id = Guid.NewGuid(),
            Content = "Pizzeria campestre",
            PostType = PostType.BusinessLocation,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            Location = new Location("San Salvador", "San Salvador", "El Salvador", 13.7052, -89.2453),
            Profile = new Profile { Id = Guid.NewGuid(), DisplayName = "Business" }
        };

        _postRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Post> { pizzeriaPost });

        // Act
        var result = await _service.FindBusinesses("pizzeria", city: "San Salvador");

        // Assert
        Assert.Contains("Pizzeria campestre", result);
        Assert.Contains("San Salvador", result);
        Assert.Contains("\"count\": 1", result);
    }

    [Fact]
    public async Task SearchPosts_WithLocationOnlyQuery_FindsPostByCity()
    {
        // Arrange - Post with location but search only by city in query
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Content = "Great food here!",
            PostType = PostType.General,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            Location = new Location("San Salvador", "San Salvador", "El Salvador"),
            Profile = new Profile { Id = Guid.NewGuid(), DisplayName = "Test User" }
        };

        _postRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Post> { post });

        // Act - Search for "San Salvador" as the query (should match location)
        var result = await _service.SearchPosts("San Salvador");

        // Assert
        Assert.Contains("Great food here!", result);
        Assert.DoesNotContain("No posts found", result);
    }
}
