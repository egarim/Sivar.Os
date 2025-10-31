# Profile Switching Integration Test Plan

## Test Overview
This integration test verifies the complete profile switching workflow, ensuring that posts are correctly isolated per profile and switching profiles updates the visible feed.

## Test Scenario Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ TEST: User Can Switch Profiles And See Different Posts          │
└─────────────────────────────────────────────────────────────────┘

Step 1: Setup & Authentication
├─ Mock authenticated user (Keycloak ID available)
└─ Create user context

Step 2: Get or Create First Profile
├─ Call GetMyProfileAsync() (server-side ProfilesClient)
├─ If null, create first profile via CreateMyProfileAsync()
└─ Store Profile1 reference

Step 3: Create Posts in Profile 1
├─ Create 3-5 test posts while Profile1 is active
├─ Verify posts are associated with Profile1.Id
├─ Verify posts are visible when querying Profile1
└─ Store post IDs for later verification

Step 4: Create Second Profile
├─ Call CreateMyProfileAsync() with different display name/bio
├─ Verify Profile2 is created successfully
├─ Verify Profile2 is different from Profile1
└─ Store Profile2 reference

Step 5: Switch to Profile 2 (Activate)
├─ Call SetMyActiveProfileAsync(Profile2.Id)
├─ Verify SetMyActiveProfileAsync returns success
├─ Call GetMyActiveProfileAsync() to confirm Profile2 is now active
└─ Verify returned active profile == Profile2

Step 6: Verify Posts Are Profile-Specific
├─ Call GetProfilePostsAsync(Profile2.Id)
├─ Verify returned posts == 0 (no posts created for Profile2)
├─ Verify posts from Profile1 are NOT visible in Profile2
└─ Confirm posts are isolated by profile

Step 7: Create Posts in Profile 2
├─ Create 2-3 test posts while Profile2 is active
├─ Verify new posts are associated with Profile2.Id
└─ Verify new posts are visible when querying Profile2

Step 8: Switch Back to Profile 1 (Activate)
├─ Call SetMyActiveProfileAsync(Profile1.Id)
├─ Verify SetMyActiveProfileAsync returns success
├─ Call GetMyActiveProfileAsync() to confirm Profile1 is now active
└─ Verify returned active profile == Profile1

Step 9: Verify Original Posts Still Exist in Profile 1
├─ Call GetProfilePostsAsync(Profile1.Id)
├─ Verify original posts from Step 3 are still there
├─ Verify posts from Profile2 are NOT visible in Profile1
└─ Confirm posts persist and stay isolated

Step 10: Final Verification
├─ Verify Profile1 posts count = 3-5 (from Step 3)
├─ Verify Profile2 posts count = 2-3 (from Step 7)
├─ Verify active profile can be switched back and forth
└─ Verify post isolation remains intact after multiple switches
```

## Test Structure

### Test Class Location
`Sivar.Os.Tests/Integration/ProfileSwitching/ProfileSwitchingIntegrationTests.cs`

### Test Type
**Integration Test** - Tests multiple components working together:
- Authentication context (mocked user)
- ProfilesClient (get, create, set active profile)
- PostsClient (create posts, retrieve by profile)
- Profile switching mechanism
- Database state persistence

### Test Isolation
- Each test method should:
  - Create fresh profiles (not reuse from previous tests)
  - Work with separate keycloak IDs (or same user, different profiles)
  - Clean up after itself (optional, depends on test strategy)

## Dependencies & Mocks

### Services Needed
1. **IProfileService** - Profile CRUD and activation
2. **IPostService** - Post CRUD and querying by profile
3. **IHttpContextAccessor** - Provide authenticated user context
4. **ILogger** - For logging (can be mocked)
5. **IProfileRepository** - For profile data access
6. **IPostRepository** - For post data access

### Mocking Strategy

#### Option A: Full Unit Test (All Mocks)
- Mock all repositories and services
- Mock IHttpContextAccessor with test keycloak ID
- Full control, but tests behavior, not integration

#### Option B: Semi-Integration (Real DB, Mocked Auth)
- Use real in-memory or test database
- Mock IHttpContextAccessor for authentication
- More realistic but slower tests

#### Option C: Full Integration (Recommended for this scenario)
- Use actual services and repositories
- Use test database (in-memory or test container)
- Mock only IHttpContextAccessor for authentication
- Tests actual data flow and isolation

### What to Mock
```csharp
// Always mock this - we control the authenticated user
IHttpContextAccessor httpContextAccessor
├─ HttpContext.User.Claims["sub"] = testKeycloakId

// Mock optionally (or use real implementation)
ILogger<T> logger (can be NullLogger)

// Consider: Real or Mock?
IProfileService       ← Real (uses repositories)
IPostService         ← Real (uses repositories)
IProfileRepository   ← Real (with test DB)
IPostRepository      ← Real (with test DB)
```

## Key Assertions

### Profile Creation
```
✓ Profile1 is created with unique DisplayName
✓ Profile1.Id is a valid GUID
✓ Profile1 is associated with keycloak user
```

### Post Creation & Isolation
```
✓ Post1, Post2, Post3 have Profile1.Id
✓ GetProfilePostsAsync(Profile1.Id) returns all 3 posts
✓ GetProfilePostsAsync(Profile2.Id) returns 0 posts
✓ Posts from Profile1 don't appear in Profile2 feed
```

### Profile Switching
```
✓ SetMyActiveProfileAsync(Profile2.Id) succeeds
✓ GetMyActiveProfileAsync() returns Profile2 after switch
✓ Active profile can switch back to Profile1
✓ Switching doesn't affect post data
```

### Data Persistence
```
✓ Posts created in Profile1 still exist after switching away
✓ New posts in Profile2 don't contaminate Profile1
✓ Switching back and forth preserves all post data
```

## Test Data

### User
- Keycloak ID: `test-user-profile-switching-123`
- Email: `test@profile-switching.com`

### Profile 1
- DisplayName: "Tech Enthusiast"
- Bio: "I love technology"
- Location: "San Francisco, CA, USA"
- Posts: 3-5 test posts with varied content

### Profile 2
- DisplayName: "Travel Blogger"
- Bio: "Exploring the world"
- Location: "Denver, CO, USA"
- Posts: 2-3 test posts with different content

### Post Structure
```csharp
new CreatePostDto
{
    Content = "Post content here",
    Visibility = VisibilityLevel.Public,
    ProfileId = activeProfileId
}
```

## Test Execution Order

1. ✅ SetUp: Create mock context and services
2. ✅ Authenticate user
3. ✅ Create/Get Profile 1
4. ✅ Create posts in Profile 1
5. ✅ Create Profile 2
6. ✅ Switch to Profile 2
7. ✅ Verify Profile 2 has no posts
8. ✅ Create posts in Profile 2
9. ✅ Switch back to Profile 1
10. ✅ Verify Profile 1 posts still exist
11. ✅ TearDown: Clean up

## Success Criteria

- ✅ All 10 steps execute without errors
- ✅ Posts are completely isolated by profile
- ✅ Active profile switches work correctly
- ✅ Data persists across profile switches
- ✅ No cross-profile data contamination
- ✅ Test runs in < 2 seconds (with in-memory DB)

## Edge Cases to Consider

### Future Enhancements
- [ ] Test with 3+ profiles
- [ ] Test switching in rapid succession
- [ ] Test with deleted posts and profile switching
- [ ] Test role-based post visibility with profile switching
- [ ] Test concurrent profile creation and switching
- [ ] Test post reactions isolated by profile

## Implementation Notes

### Services to Use
- `ProfilesClient` (server-side) for profile operations
- `PostsClient` (server-side) or `PostService` for post operations
- `ProfileSwitcherClient` for profile activation

### Database Setup
- Use in-memory database for speed: `UseSqlite(":memory:")`
- Or test container for full database testing

### Test Organization
```
Tests/
└─ Integration/
   └─ ProfileSwitching/
      ├─ ProfileSwitchingIntegrationTests.cs  (Main test)
      ├─ ProfileSwitchingTestFixture.cs       (Test data setup)
      └─ ProfileSwitchingTestHelpers.cs       (Helper methods)
```

---

## Next Steps
1. ✅ Approve this plan
2. ⏭️  Create test infrastructure
3. ⏭️  Implement test class with setUp/tearDown
4. ⏭️  Implement each test step
5. ⏭️  Run and verify all assertions pass
