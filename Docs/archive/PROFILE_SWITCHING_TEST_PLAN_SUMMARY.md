# Test Plan Summary - Profile Switching Integration Test

## What We're Building

A comprehensive integration test that verifies:
- Users can switch between multiple profiles
- Posts are isolated by profile (posts from Profile1 don't show in Profile2)
- The active profile can be switched back and forth without data loss
- Post visibility correctly reflects the active profile

## Test Flow (10 Steps)

```
STEP 1: Setup → User authenticated (keycloak ID available)
STEP 2: Get/Create → First profile
STEP 3: Create Posts → 3-5 posts in Profile 1
STEP 4: Create → Second profile  
STEP 5: Switch Active → To Profile 2
STEP 6: Verify Posts → Profile 2 shows empty list
STEP 7: Create Posts → 2-3 posts in Profile 2
STEP 8: Switch Active → Back to Profile 1
STEP 9: Verify Posts → Profile 1 still has original posts
STEP 10: Verify Posts → Profile 2 still has its posts
```

## Test Architecture

**Type**: Integration Test
- Real services (ProfilesClient, PostsClient)
- Real repositories (ProfileRepository, PostRepository)
- In-memory test database
- Mocked authentication (IHttpContextAccessor)

**Location**: `Sivar.Os.Tests/Integration/ProfileSwitching/ProfileSwitchingIntegrationTests.cs`

**Components Tested**:
```
ProfilesClient
  └─ SetMyActiveProfileAsync()      ← Switch active profile
  └─ GetMyActiveProfileAsync()      ← Get current active profile
  └─ GetMyProfileAsync()             ← Get first profile
  └─ CreateMyProfileAsync()          ← Create new profile

PostsClient
  └─ GetProfilePostsAsync()          ← Get posts for specific profile
  └─ CreatePostAsync()               ← Create post (optional)
```

## Key Assertions

### Post Isolation (The Core Requirement)
```
✓ Posts created in Profile1 have ProfileId = Profile1.Id
✓ Posts created in Profile2 have ProfileId = Profile2.Id
✓ GetProfilePostsAsync(Profile1.Id) returns Profile1's posts only
✓ GetProfilePostsAsync(Profile2.Id) returns Profile2's posts only
✓ No cross-profile post contamination at any point
```

### Profile Switching
```
✓ SetMyActiveProfileAsync(Profile2.Id) succeeds
✓ GetMyActiveProfileAsync() immediately returns Profile2
✓ Can switch back: SetMyActiveProfileAsync(Profile1.Id) succeeds
✓ GetMyActiveProfileAsync() now returns Profile1
✓ Switching doesn't lose profile data
```

### Data Persistence
```
✓ Posts exist after switching away from profile
✓ Posts exist after switching back to profile
✓ Multiple switch cycles don't affect data
```

## Test Data

### User Context
- Keycloak ID: `test-user-profile-switching-123`

### Profile 1: "Tech Enthusiast"
- Bio: "I love technology"
- Posts: 3-5 test posts

### Profile 2: "Travel Blogger"  
- Bio: "Exploring the world"
- Posts: 2-3 test posts

## What Gets Mocked vs Real

| Component | Approach | Why |
|-----------|----------|-----|
| IHttpContextAccessor | Mock | Control authenticated user |
| IProfileService | Real | Test actual business logic |
| IPostService | Real | Test actual business logic |
| Repositories | Real | Test database persistence |
| Database | In-Memory | Fast test execution |
| Logger | Can be Null | Not critical for test |

## Success Criteria

✅ All 10 steps execute without errors
✅ Posts are 100% isolated by profile
✅ Active profile switches work bidirectionally
✅ Data persists across switches
✅ Test completes in < 2 seconds

## Questions for You

Before implementation, please clarify:

1. **Should we use real post creation or mock it?**
   - Option A: Real (calls through PostsClient)
   - Option B: Mock (directly insert test posts)
   
2. **How many profiles should we test?**
   - Option A: 2 profiles (plan's default)
   - Option B: 3+ profiles

3. **Should we test rapid switching?**
   - Option A: Sequential switches (current plan)
   - Option B: Add rapid switching scenario

4. **Post visibility complexity?**
   - Option A: Just check posts exist in correct profile
   - Option B: Also verify posts content matches

5. **Should we test with different users?**
   - Option A: One user, multiple profiles (current plan)
   - Option B: Multiple users, verify isolation

---

## Files Created

1. `PROFILE_SWITCHING_TEST_PLAN.md` - Detailed 10-step plan
2. `PROFILE_SWITCHING_TEST_PLAN_VISUAL.md` - Visual diagrams and summaries
3. `PROFILE_SWITCHING_TEST_PLAN_SUMMARY.md` - This file

---

## Next Steps

### When Ready:
1. Review and approve the plan
2. Answer clarification questions above
3. Implement test class with:
   - SetUp: Create database, services, mocks
   - Test method: Execute 10 steps
   - TearDown: Clean up
4. Run tests and verify all pass
5. Iterate if needed

### Expected Output:
- 1 comprehensive integration test
- ~300-400 lines of code
- Tests complete real-world workflow
- Demonstrates post isolation by profile
- All assertions passing ✅

---

**Ready to proceed? Approve the plan above or let me know what adjustments you'd like!**
