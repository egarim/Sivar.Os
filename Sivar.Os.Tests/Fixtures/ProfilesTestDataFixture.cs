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
}
