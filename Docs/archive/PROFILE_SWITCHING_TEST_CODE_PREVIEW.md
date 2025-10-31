# Test Implementation Preview

This shows the rough code structure we'll implement. Not final, just to give you a preview of what the test will look like.

## Test Class Skeleton

```csharp
namespace Sivar.Os.Tests.Integration.ProfileSwitching;

/// <summary>
/// Integration test verifying profile switching and post isolation
/// 
/// Scenario: A user creates multiple profiles and switches between them.
/// Posts should be isolated by profile - posts from Profile1 should NOT
/// appear when viewing Profile2, and vice versa.
/// </summary>
public class ProfileSwitchingIntegrationTests : IAsyncLifetime
{
    // Test user context
    private const string TestKeycloakId = "test-user-profile-switching-123";
    private const string TestUserEmail = "test@profile-switching.com";
    
    // Services
    private IProfileService _profileService;
    private IPostService _postService;
    private IProfileRepository _profileRepository;
    private IPostRepository _postRepository;
    private ProfilesClient _profilesClient;
    private PostsClient _postsClient;
    
    // Test data
    private ProfileDto _profile1;
    private ProfileDto _profile2;
    private List<PostDto> _profile1Posts = new();
    private List<PostDto> _profile2Posts = new();
    
    // Setup and teardown
    public async Task InitializeAsync()
    {
        // Create database
        // Configure services
        // Create mock HttpContextAccessor with TestKeycloakId
        // Initialize clients
    }
    
    public async Task DisposeAsync()
    {
        // Clean up database
    }
    
    // The main test
    [Fact]
    public async Task UserCanSwitchProfilesAndSeeProfileSpecificPosts()
    {
        // ========== SETUP ==========
        // Step 1: Setup authentication context
        var authenticated = await SetupAuthenticatedUser(TestKeycloakId);
        Assert.True(authenticated);
        
        // ========== PROFILE 1 ==========
        // Step 2: Get or create first profile
        _profile1 = await _profilesClient.GetMyProfileAsync();
        if (_profile1 == null)
        {
            _profile1 = await _profilesClient.CreateMyProfileAsync(
                new CreateProfileDto 
                { 
                    DisplayName = "Tech Enthusiast",
                    Bio = "I love technology",
                    Location = new Location("SF", "CA", "USA")
                }
            );
        }
        Assert.NotNull(_profile1);
        Assert.NotEmpty(_profile1.Id.ToString());
        
        // Step 3: Create posts in Profile 1
        var post1A = await CreateTestPost("Python tips and tricks", _profile1.Id);
        var post1B = await CreateTestPost("JavaScript best practices", _profile1.Id);
        var post1C = await CreateTestPost("Web development trends", _profile1.Id);
        _profile1Posts.AddRange(new[] { post1A, post1B, post1C });
        
        // Verify posts are in Profile 1
        var p1Posts = await _postsClient.GetProfilePostsAsync(_profile1.Id);
        Assert.NotNull(p1Posts.Posts);
        Assert.Equal(3, p1Posts.Posts.Count());
        
        // ========== PROFILE 2 ==========
        // Step 4: Create second profile
        _profile2 = await _profilesClient.CreateMyProfileAsync(
            new CreateProfileDto
            {
                DisplayName = "Travel Blogger",
                Bio = "Exploring the world",
                Location = new Location("Denver", "CO", "USA")
            }
        );
        Assert.NotNull(_profile2);
        Assert.NotEqual(_profile1.Id, _profile2.Id);
        
        // ========== SWITCH TO PROFILE 2 ==========
        // Step 5: Switch active profile to Profile 2
        var switchSuccess = await _profilesClient.SetMyActiveProfileAsync(_profile2.Id);
        Assert.True(switchSuccess);
        
        // Step 6: Verify Profile 2 is now active
        var activeProfile = await _profilesClient.GetMyActiveProfileAsync();
        Assert.NotNull(activeProfile);
        Assert.Equal(_profile2.Id, activeProfile.Id);
        
        // Step 7: Verify Profile 2 has no posts (newly created)
        var p2PostsEmpty = await _postsClient.GetProfilePostsAsync(_profile2.Id);
        Assert.NotNull(p2PostsEmpty.Posts);
        Assert.Empty(p2PostsEmpty.Posts);
        
        // ========== CREATE POSTS IN PROFILE 2 ==========
        // Step 8: Create posts in Profile 2
        var post2A = await CreateTestPost("Paris is beautiful", _profile2.Id);
        var post2B = await CreateTestPost("Tokyo adventures", _profile2.Id);
        _profile2Posts.AddRange(new[] { post2A, post2B });
        
        // Verify posts are in Profile 2
        var p2PostsFilled = await _postsClient.GetProfilePostsAsync(_profile2.Id);
        Assert.NotNull(p2PostsFilled.Posts);
        Assert.Equal(2, p2PostsFilled.Posts.Count());
        
        // ========== SWITCH BACK TO PROFILE 1 ==========
        // Step 9: Switch back to Profile 1
        switchSuccess = await _profilesClient.SetMyActiveProfileAsync(_profile1.Id);
        Assert.True(switchSuccess);
        
        // Step 10: Verify Profile 1 is now active
        activeProfile = await _profilesClient.GetMyActiveProfileAsync();
        Assert.NotNull(activeProfile);
        Assert.Equal(_profile1.Id, activeProfile.Id);
        
        // ========== FINAL VERIFICATION ==========
        // Step 11: Verify original posts still exist in Profile 1
        var p1PostsFinal = await _postsClient.GetProfilePostsAsync(_profile1.Id);
        Assert.NotNull(p1PostsFinal.Posts);
        Assert.Equal(3, p1PostsFinal.Posts.Count());
        
        // Verify posts from Profile2 are NOT visible in Profile1
        var p1PostIds = p1PostsFinal.Posts.Select(p => p.Id).ToList();
        foreach (var profile2Post in _profile2Posts)
        {
            Assert.DoesNotContain(profile2Post.Id, p1PostIds);
        }
        
        // Step 12: Verify Profile 2 posts still exist
        var p2PostsVerify = await _postsClient.GetProfilePostsAsync(_profile2.Id);
        Assert.NotNull(p2PostsVerify.Posts);
        Assert.Equal(2, p2PostsVerify.Posts.Count());
        
        // Verify posts from Profile1 are NOT visible in Profile2
        var p2PostIds = p2PostsVerify.Posts.Select(p => p.Id).ToList();
        foreach (var profile1Post in _profile1Posts)
        {
            Assert.DoesNotContain(profile1Post.Id, p2PostIds);
        }
        
        // ✅ TEST COMPLETE - All assertions passed!
    }
    
    // Helper methods
    private async Task<bool> SetupAuthenticatedUser(string keycloakId)
    {
        // Mock IHttpContextAccessor to return this keycloakId
        // Return true if successful
        return true;
    }
    
    private async Task<PostDto> CreateTestPost(string content, Guid profileId)
    {
        var request = new CreatePostDto
        {
            Content = content,
            Visibility = VisibilityLevel.Public,
            ProfileId = profileId
        };
        return await _postsClient.CreatePostAsync(request);
    }
}
```

## What This Shows

### ✅ Structure
- Clear test sections (SETUP, PROFILE 1, PROFILE 2, SWITCH, VERIFY)
- Helper methods for common operations
- Organized assertions

### ✅ The Scenario
- User creates Profile 1 → Creates posts
- User creates Profile 2 → No posts yet
- User switches to Profile 2 → Creates posts
- User switches back to Profile 1 → Original posts still there
- Verify no cross-profile post contamination

### ✅ Key Assertions
- Posts exist in correct profile
- Posts don't appear in other profiles
- Switching works bidirectionally
- Data persists across switches

### ✅ Services Used
- `IProfileService` / `ProfilesClient` - Profile management
- `IPostService` / `PostsClient` - Post management
- `IHttpContextAccessor` (mocked) - User authentication

## This Preview Shows

```
✅ Test class structure
✅ Test method flow (10+ steps)
✅ Helper method signatures
✅ Assertion patterns
✅ Service interactions

❌ NOT shown:
- Database setup code
- Mock creation code
- Fixture code
- Error handling
- Edge cases
```

## Ready for Full Implementation?

Once you approve:
1. I'll implement the complete test class (~400 lines)
2. I'll create helper fixture class (~150 lines)
3. I'll add setup/teardown methods
4. I'll run all tests to verify they pass
5. I'll show you the final working code

**Proceed?** ✅ YES / ❌ NO

If yes, any changes to the plan above?
