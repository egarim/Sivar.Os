using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.DTOs.ValueObjects;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Tests.Fixtures;

/// <summary>
/// Provides common test data for profiles client tests
/// </summary>
public class ProfilesTestDataFixture
{
    public const string TestKeycloakId = "test-user-12345";
    public const string TestKeycloakId2 = "test-user-67890";

    public static CreateProfileDto CreateValidCreateProfileRequest()
    {
        return new CreateProfileDto
        {
            DisplayName = "Test Profile",
            Bio = "This is a test bio",
            Location = new Location("Test City", "Test State", "Test Country")
        };
    }

    public static CreateProfileDto CreateCreateProfileRequestWithDifferentName(string displayName)
    {
        return new CreateProfileDto
        {
            DisplayName = displayName,
            Bio = "Updated bio",
            Location = new Location("Updated City", "Updated State", "Updated Country")
        };
    }

    public static UpdateProfileDto CreateValidUpdateProfileRequest()
    {
        return new UpdateProfileDto
        {
            DisplayName = "Updated Profile",
            Bio = "Updated bio text",
            Location = new Location("Updated City", "Updated State", "Updated Country")
        };
    }

    public static ProfileDto CreateProfileDtoWithId(Guid? id = null, string? displayName = null, string? keycloakId = null)
    {
        return new ProfileDto
        {
            Id = id ?? Guid.NewGuid(),
            DisplayName = displayName ?? "Test Profile",
            Bio = "This is a test bio",
            Location = new Location("Test City", "Test State", "Test Country"),
            LocationDisplay = "Test City, Test State, Test Country",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            VisibilityLevel = VisibilityLevel.Public
        };
    }

    public static ActiveProfileDto CreateActiveProfileDto(Guid? profileId = null)
    {
        return new ActiveProfileDto
        {
            Id = profileId ?? Guid.NewGuid(),
            IsActive = true
        };
    }

    public static IEnumerable<ProfileDto> CreateMultipleProfileDtos(int count = 3)
    {
        return Enumerable.Range(1, count)
            .Select(i => new ProfileDto
            {
                Id = Guid.NewGuid(),
                DisplayName = $"Profile {i}",
                Bio = $"Bio for profile {i}",
                Location = new Location($"City {i}", "State", "Country"),
                LocationDisplay = $"City {i}, State, Country",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                VisibilityLevel = VisibilityLevel.Public
            })
            .ToList();
    }

    #region Post Test Data Helpers

    public static CreatePostDto CreatePostRequestForProfile(Guid profileId, string? content = null)
    {
        return new CreatePostDto
        {
            ProfileId = profileId,
            Content = content ?? $"Test post content - {Guid.NewGuid()}",
            PostType = PostType.General,
            Visibility = VisibilityLevel.Public,
            Language = "en",
            Tags = new List<string> { "test", "integration" }
        };
    }

    public static PostDto CreatePostDtoForProfile(Guid profileId, ProfileDto profile, string? content = null)
    {
        return new PostDto
        {
            Id = Guid.NewGuid(),
            Profile = profile,
            Content = content ?? $"Test post - {Guid.NewGuid()}",
            PostType = PostType.General,
            Visibility = VisibilityLevel.Public,
            Language = "en",
            Tags = new List<string> { "test" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsEdited = false,
            CommentCount = 0
        };
    }

    public static PostFeedDto CreatePostFeedWithPosts(List<PostDto> posts)
    {
        return new PostFeedDto
        {
            Posts = posts,
            Page = 0,
            PageSize = 20,
            TotalCount = posts.Count
        };
    }

    public static PostFeedDto CreateEmptyPostFeed()
    {
        return new PostFeedDto
        {
            Posts = new List<PostDto>(),
            Page = 0,
            PageSize = 20,
            TotalCount = 0
        };
    }

    #endregion
}
