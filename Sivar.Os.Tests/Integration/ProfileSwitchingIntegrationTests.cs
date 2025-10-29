using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;
using Sivar.Os.Tests.Fixtures;
using Xunit;

namespace Sivar.Os.Tests.Integration;

/// <summary>
/// Integration test for profile switching functionality.
/// 
/// This test verifies the complete user workflow:
/// 1. User logs in (authenticated context)
/// 2. User creates multiple profiles
/// 3. User creates posts in each profile
/// 4. User switches between profiles
/// 5. Posts are properly isolated by profile
/// 6. Posts persist correctly across profile switches
/// 
/// This test ensures that profile isolation is working correctly and that
/// posts created in one profile don't appear in another profile.
/// </summary>
[Collection("ProfileSwitchingIntegration")]
public class ProfileSwitchingIntegrationTests
{
    #region Setup and Dependencies

    private readonly Mock<IProfileService> _profileServiceMock;
    private readonly Mock<IPostService> _postServiceMock;
    private readonly Mock<IProfileRepository> _profileRepositoryMock;
    private readonly Mock<IPostRepository> _postRepositoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<Sivar.Os.Services.Clients.ProfilesClient>> _profilesClientLoggerMock;
    private readonly Mock<ILogger<Sivar.Os.Services.Clients.PostsClient>> _postsClientLoggerMock;

    private readonly IProfilesClient _profilesClient;
    private readonly IPostsClient _postsClient;

    // Test data
    private readonly string _keycloakId = ProfilesTestDataFixture.TestKeycloakId;
    private Guid _profile1Id = Guid.NewGuid();
    private Guid _profile2Id = Guid.NewGuid();
    private ProfileDto _profile1 = null!;
    private ProfileDto _profile2 = null!;

    public ProfileSwitchingIntegrationTests()
    {
        // Initialize mocks
        _profileServiceMock = new Mock<IProfileService>();
        _postServiceMock = new Mock<IPostService>();
        _profileRepositoryMock = new Mock<IProfileRepository>();
        _postRepositoryMock = new Mock<IPostRepository>();
        _httpContextAccessorMock = AuthenticationTestFixture
            .CreateMockHttpContextAccessor(_keycloakId);
        _profilesClientLoggerMock = new Mock<ILogger<Sivar.Os.Services.Clients.ProfilesClient>>();
        _postsClientLoggerMock = new Mock<ILogger<Sivar.Os.Services.Clients.PostsClient>>();

        // Initialize clients with mocked dependencies
        _profilesClient = new Sivar.Os.Services.Clients.ProfilesClient(
            _profileServiceMock.Object,
            _profileRepositoryMock.Object,
            _httpContextAccessorMock.Object,
            _profilesClientLoggerMock.Object
        );

        _postsClient = new Sivar.Os.Services.Clients.PostsClient(
            _postServiceMock.Object,
            _postRepositoryMock.Object,
            _httpContextAccessorMock.Object,
            _postsClientLoggerMock.Object
        );

        // Initialize test profile data
        InitializeTestProfiles();
    }

    /// <summary>
    /// Initializes test profile DTOs with realistic data
    /// </summary>
    private void InitializeTestProfiles()
    {
        _profile1 = ProfilesTestDataFixture.CreateProfileDtoWithId(
            id: _profile1Id,
            displayName: "Tech Enthusiast"
        );

        _profile2 = ProfilesTestDataFixture.CreateProfileDtoWithId(
            id: _profile2Id,
            displayName: "Travel Blogger"
        );
    }

    #endregion

    #region Test: Profile Switching with Post Isolation

    /// <summary>
    /// STEP 1-10: Complete profile switching integration test
    /// 
    /// Scenario:
    /// - User creates 2 profiles
    /// - Creates 3 posts in Profile 1
    /// - Creates 2 posts in Profile 2
    /// - Switches between profiles multiple times
    /// - Verifies posts are properly isolated
    /// - Verifies posts persist across switches
    /// </summary>
    [Fact]
    public async Task UserCanSwitchProfilesAndSeeProfileSpecificPosts()
    {
        // ========== STEP 1: Setup authenticated context ==========
        var user = _httpContextAccessorMock.Object.HttpContext;
        user.Should().NotBeNull();
        user!.User.Should().NotBeNull();
        user.User.FindFirst("sub")?.Value.Should().Be(_keycloakId);

        // ========== STEP 2: Get/Create first profile (Tech Enthusiast) ==========
        SetupProfileServiceMock_CreateMyProfile(_keycloakId, _profile1);
        var createdProfile1 = await _profilesClient.CreateMyProfileAsync(
            ProfilesTestDataFixture.CreateCreateProfileRequestWithDifferentName("Tech Enthusiast")
        );

        createdProfile1.Should().NotBeNull();
        createdProfile1!.Id.Should().Be(_profile1Id);
        createdProfile1.DisplayName.Should().Be("Tech Enthusiast");

        // ========== STEP 3: Create 3-5 posts in Profile 1 ==========
        var profile1Posts = new List<PostDto>();
        
        // Post 1.1: "React best practices"
        var post1_1 = ProfilesTestDataFixture.CreatePostDtoForProfile(
            _profile1Id,
            _profile1,
            "Check out these React best practices for modern development #react #programming"
        );
        profile1Posts.Add(post1_1);

        // Post 1.2: ".NET Core performance tips"
        var post1_2 = ProfilesTestDataFixture.CreatePostDtoForProfile(
            _profile1Id,
            _profile1,
            "Performance optimization techniques in .NET Core #dotnet #csharp"
        );
        profile1Posts.Add(post1_2);

        // Post 1.3: "Cloud architecture discussion"
        var post1_3 = ProfilesTestDataFixture.CreatePostDtoForProfile(
            _profile1Id,
            _profile1,
            "Comparing Azure vs AWS for enterprise applications #cloud #architecture"
        );
        profile1Posts.Add(post1_3);

        SetupPostServiceMock_CreatePost(_keycloakId, profile1Posts[0]);
        var createdPost1_1 = await _postsClient.CreatePostAsync(
            ProfilesTestDataFixture.CreatePostRequestForProfile(_profile1Id, profile1Posts[0].Content)
        );
        createdPost1_1.Should().NotBeNull();

        SetupPostServiceMock_CreatePost(_keycloakId, profile1Posts[1]);
        var createdPost1_2 = await _postsClient.CreatePostAsync(
            ProfilesTestDataFixture.CreatePostRequestForProfile(_profile1Id, profile1Posts[1].Content)
        );
        createdPost1_2.Should().NotBeNull();

        SetupPostServiceMock_CreatePost(_keycloakId, profile1Posts[2]);
        var createdPost1_3 = await _postsClient.CreatePostAsync(
            ProfilesTestDataFixture.CreatePostRequestForProfile(_profile1Id, profile1Posts[2].Content)
        );
        createdPost1_3.Should().NotBeNull();

        // ========== STEP 4: Create second profile (Travel Blogger) ==========
        SetupProfileServiceMock_CreateMyProfile(_keycloakId, _profile2);
        var createdProfile2 = await _profilesClient.CreateMyProfileAsync(
            ProfilesTestDataFixture.CreateCreateProfileRequestWithDifferentName("Travel Blogger")
        );

        createdProfile2.Should().NotBeNull();
        createdProfile2!.Id.Should().Be(_profile2Id);
        createdProfile2.DisplayName.Should().Be("Travel Blogger");

        // ========== STEP 5: Switch active profile to Profile 2 ==========
        SetupProfileServiceMock_SetActiveProfile(_keycloakId, _profile2Id);
        SetupProfileServiceMock_GetActiveProfile(_keycloakId, _profile2);
        
        var setActiveResult = await _profilesClient.SetMyActiveProfileAsync(_profile2Id);
        setActiveResult.Should().NotBeNull();
        setActiveResult.Id.Should().Be(_profile2Id);

        var activeProfile = await _profilesClient.GetMyActiveProfileAsync();
        activeProfile.Should().NotBeNull();
        activeProfile!.Id.Should().Be(_profile2Id);

        // ========== STEP 6: Verify Profile 2 has no posts initially ==========
        var profile2InitialPosts = new List<PostDto>();
        SetupPostRepository_GetByProfile(_profile2Id, profile2InitialPosts, 0);
        
        var profile2PostsEmpty = await _postsClient.GetProfilePostsAsync(_profile2Id);
        profile2PostsEmpty.Posts.Should().BeEmpty();
        profile2PostsEmpty.TotalCount.Should().Be(0);

        // ========== STEP 7: Create 2-3 posts in Profile 2 ==========
        var profile2Posts = new List<PostDto>();

        // Post 2.1: "Paris travel guide"
        var post2_1 = ProfilesTestDataFixture.CreatePostDtoForProfile(
            _profile2Id,
            _profile2,
            "Just visited Paris! The Eiffel Tower at sunset was incredible #travel #paris #europe"
        );
        profile2Posts.Add(post2_1);

        // Post 2.2: "Tokyo recommendations"
        var post2_2 = ProfilesTestDataFixture.CreatePostDtoForProfile(
            _profile2Id,
            _profile2,
            "Tokyo has amazing street food and culture. Must visit Shibuya Crossing! #japan #travel"
        );
        profile2Posts.Add(post2_2);

        SetupPostServiceMock_CreatePost(_keycloakId, profile2Posts[0]);
        var createdPost2_1 = await _postsClient.CreatePostAsync(
            ProfilesTestDataFixture.CreatePostRequestForProfile(_profile2Id, profile2Posts[0].Content)
        );
        createdPost2_1.Should().NotBeNull();

        SetupPostServiceMock_CreatePost(_keycloakId, profile2Posts[1]);
        var createdPost2_2 = await _postsClient.CreatePostAsync(
            ProfilesTestDataFixture.CreatePostRequestForProfile(_profile2Id, profile2Posts[1].Content)
        );
        createdPost2_2.Should().NotBeNull();

        // ========== STEP 8: Switch active profile back to Profile 1 ==========
        SetupProfileServiceMock_SetActiveProfile(_keycloakId, _profile1Id);
        SetupProfileServiceMock_GetActiveProfile(_keycloakId, _profile1);

        var switchBack = await _profilesClient.SetMyActiveProfileAsync(_profile1Id);
        switchBack.Should().NotBeNull();
        switchBack.Id.Should().Be(_profile1Id);

        var activeProfile1Again = await _profilesClient.GetMyActiveProfileAsync();
        activeProfile1Again.Should().NotBeNull();
        activeProfile1Again!.Id.Should().Be(_profile1Id);

        // ========== STEP 9: Verify Profile 1 posts still exist ==========
        SetupPostRepository_GetByProfile(_profile1Id, profile1Posts, profile1Posts.Count);

        var profile1PostsAfterSwitch = await _postsClient.GetProfilePostsAsync(_profile1Id);
        profile1PostsAfterSwitch.Posts.Should().HaveCount(3);
        profile1PostsAfterSwitch.TotalCount.Should().Be(3);

        // Verify specific posts by their distinct content patterns
        profile1PostsAfterSwitch.Posts.Should().Contain(p => p.Content.Contains("React"));
        profile1PostsAfterSwitch.Posts.Should().Contain(p => p.Content.Contains("Performance optimization"));
        profile1PostsAfterSwitch.Posts.Should().Contain(p => p.Content.Contains("enterprise applications"));

        // ========== STEP 10: Verify no cross-profile contamination ==========
        // Profile 1 should NOT have Profile 2's posts
        profile1PostsAfterSwitch.Posts.Should().NotContain(p => p.Content.Contains("Paris"));
        profile1PostsAfterSwitch.Posts.Should().NotContain(p => p.Content.Contains("Tokyo"));

        // Profile 2 should NOT have Profile 1's posts
        SetupPostRepository_GetByProfile(_profile2Id, profile2Posts, profile2Posts.Count);

        var profile2PostsAfterSwitch = await _postsClient.GetProfilePostsAsync(_profile2Id);
        profile2PostsAfterSwitch.Posts.Should().HaveCount(2);
        profile2PostsAfterSwitch.TotalCount.Should().Be(2);

        profile2PostsAfterSwitch.Posts.Should().Contain(p => p.Content.Contains("Paris"));
        profile2PostsAfterSwitch.Posts.Should().Contain(p => p.Content.Contains("Tokyo"));

        profile2PostsAfterSwitch.Posts.Should().NotContain(p => p.Content.Contains("React"));
        profile2PostsAfterSwitch.Posts.Should().NotContain(p => p.Content.Contains(".NET Core"));
        profile2PostsAfterSwitch.Posts.Should().NotContain(p => p.Content.Contains("Cloud architecture"));
    }

    #endregion

    #region Test: Rapid Profile Switching

    /// <summary>
    /// Verifies that rapid switching between profiles maintains data integrity
    /// </summary>
    [Fact]
    public async Task RapidProfileSwitching_MaintainsDataIntegrity()
    {
        // Setup both profiles
        InitializeTestProfiles();
        SetupProfileServiceMock_CreateMyProfile(_keycloakId, _profile1);
        await _profilesClient.CreateMyProfileAsync(
            ProfilesTestDataFixture.CreateCreateProfileRequestWithDifferentName("Profile 1")
        );

        SetupProfileServiceMock_CreateMyProfile(_keycloakId, _profile2);
        await _profilesClient.CreateMyProfileAsync(
            ProfilesTestDataFixture.CreateCreateProfileRequestWithDifferentName("Profile 2")
        );

        // Create posts in both profiles
        var profile1Posts = new List<PostDto>
        {
            ProfilesTestDataFixture.CreatePostDtoForProfile(_profile1Id, _profile1, "Post A"),
            ProfilesTestDataFixture.CreatePostDtoForProfile(_profile1Id, _profile1, "Post B")
        };

        var profile2Posts = new List<PostDto>
        {
            ProfilesTestDataFixture.CreatePostDtoForProfile(_profile2Id, _profile2, "Post X"),
            ProfilesTestDataFixture.CreatePostDtoForProfile(_profile2Id, _profile2, "Post Y")
        };

        // Rapid switching: 1 -> 2 -> 1 -> 2 -> 1
        SetupProfileServiceMock_SetActiveProfile(_keycloakId, _profile2Id);
        SetupProfileServiceMock_GetActiveProfile(_keycloakId, _profile2);
        await _profilesClient.SetMyActiveProfileAsync(_profile2Id);

        SetupProfileServiceMock_SetActiveProfile(_keycloakId, _profile1Id);
        SetupProfileServiceMock_GetActiveProfile(_keycloakId, _profile1);
        await _profilesClient.SetMyActiveProfileAsync(_profile1Id);

        SetupProfileServiceMock_SetActiveProfile(_keycloakId, _profile2Id);
        SetupProfileServiceMock_GetActiveProfile(_keycloakId, _profile2);
        await _profilesClient.SetMyActiveProfileAsync(_profile2Id);

        SetupProfileServiceMock_SetActiveProfile(_keycloakId, _profile1Id);
        SetupProfileServiceMock_GetActiveProfile(_keycloakId, _profile1);
        await _profilesClient.SetMyActiveProfileAsync(_profile1Id);

        // Final verification: Posts are still properly isolated
        var finalProfile1Posts = profile1Posts;
        SetupPostRepository_GetByProfile(_profile1Id, finalProfile1Posts, finalProfile1Posts.Count);
        var result1 = await _postsClient.GetProfilePostsAsync(_profile1Id);
        result1.Posts.Should().HaveCount(2);
        result1.Posts.Should().Contain(p => p.Content == "Post A");
        result1.Posts.Should().Contain(p => p.Content == "Post B");
        result1.Posts.Should().NotContain(p => p.Content == "Post X");

        var finalProfile2Posts = profile2Posts;
        SetupPostRepository_GetByProfile(_profile2Id, finalProfile2Posts, finalProfile2Posts.Count);
        var result2 = await _postsClient.GetProfilePostsAsync(_profile2Id);
        result2.Posts.Should().HaveCount(2);
        result2.Posts.Should().Contain(p => p.Content == "Post X");
        result2.Posts.Should().Contain(p => p.Content == "Post Y");
        result2.Posts.Should().NotContain(p => p.Content == "Post A");
    }

    #endregion

    #region Mock Setup Helper Methods

    /// <summary>
    /// Sets up the profile service mock to return a created profile
    /// </summary>
    private void SetupProfileServiceMock_CreateMyProfile(string keycloakId, ProfileDto profile)
    {
        _profileServiceMock
            .Setup(s => s.CreateMyProfileAsync(
                It.Is<string>(k => k == keycloakId),
                It.IsAny<CreateProfileDto>()
            ))
            .ReturnsAsync(profile);
    }

    /// <summary>
    /// Sets up the profile service mock to set active profile
    /// </summary>
    private void SetupProfileServiceMock_SetActiveProfile(string keycloakId, Guid profileId)
    {
        _profileServiceMock
            .Setup(s => s.SetActiveProfileAsync(
                It.Is<string>(k => k == keycloakId),
                It.Is<Guid>(p => p == profileId)
            ))
            .ReturnsAsync(true);
    }

    /// <summary>
    /// Sets up the profile service mock to get active profile
    /// </summary>
    private void SetupProfileServiceMock_GetActiveProfile(string keycloakId, ProfileDto profile)
    {
        _profileServiceMock
            .Setup(s => s.GetMyActiveProfileAsync(
                It.Is<string>(k => k == keycloakId)
            ))
            .ReturnsAsync(profile);
    }

    /// <summary>
    /// Sets up the post service mock to create a post
    /// </summary>
    private void SetupPostServiceMock_CreatePost(string keycloakId, PostDto post)
    {
        _postServiceMock
            .Setup(s => s.CreatePostAsync(
                It.Is<string>(k => k == keycloakId),
                It.IsAny<CreatePostDto>()
            ))
            .ReturnsAsync(post);
    }

    /// <summary>
    /// Sets up the post repository mock to return posts for a profile
    /// </summary>
    private void SetupPostRepository_GetByProfile(Guid profileId, List<PostDto> postDtos, int totalCount)
    {
        // Convert DTOs to mock Post entities with minimal properties
        var posts = postDtos.Select(dto => new Post
        {
            Id = dto.Id,
            Content = dto.Content,
            PostType = dto.PostType,
            Visibility = dto.Visibility,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        }).ToList();

        _postRepositoryMock
            .Setup(r => r.GetByProfileAsync(
                It.Is<Guid>(p => p == profileId),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>()
            ))
            .ReturnsAsync(((IEnumerable<Post>)posts, totalCount));
    }

    #endregion
}
