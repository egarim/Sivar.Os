# Profile Switching Test - Quick Visual Summary

## The Story
```
┌─────────────────────────────────────────────────────────────┐
│ Alice logs in                                               │
├─────────────────────────────────────────────────────────────┤
│ ↓ (authenticated, keycloak ID = alice-123)                 │
│                                                              │
│ Alice's First Profile: "Tech Enthusiast"                    │
│   ├─ Post 1: "I love Python"                                │
│   ├─ Post 2: "JavaScript tips"                              │
│   └─ Post 3: "Web development trends"                       │
│                                                              │
│ ↓ (Create second profile)                                   │
│                                                              │
│ Alice's Second Profile: "Travel Blogger"                    │
│   └─ (EMPTY - no posts yet)                                 │
│                                                              │
│ ↓ (SWITCH ACTIVE PROFILE to "Travel Blogger")              │
│                                                              │
│ ❌ VERIFY: Tech posts are NOT visible                        │
│ ✅ VERIFY: Travel profile is now active                      │
│                                                              │
│ ↓ (Create posts in Travel profile)                          │
│                                                              │
│ Alice's Second Profile: "Travel Blogger"                    │
│   ├─ Post 1: "Paris is beautiful"                           │
│   └─ Post 2: "Tokyo adventures"                             │
│                                                              │
│ ↓ (SWITCH BACK to "Tech Enthusiast")                        │
│                                                              │
│ ✅ VERIFY: Original tech posts still there                   │
│ ❌ VERIFY: Travel posts are NOT visible                      │
│                                                              │
│ ✅ TEST PASSED!                                              │
└─────────────────────────────────────────────────────────────┘
```

## What We're Testing

### Component Interactions
```
┌──────────────┐
│   User Auth  │
│ (Keycloak)   │
└──────┬───────┘
       │ Keycloak ID
       ↓
┌──────────────────────┐      ┌─────────────────┐
│  ProfilesClient      │◄────►│  IProfileService│
│ (Set Active, etc)    │      │                 │
└──────┬───────────────┘      └─────────────────┘
       │                               │
       │ ActiveProfileId               │ ProfileId
       ↓                               ↓
┌──────────────────────┐      ┌─────────────────┐
│    PostsClient       │◄────►│  IPostService   │
│ (GetProfilePosts)    │      │ (GetByProfile)  │
└──────────────────────┘      └─────────────────┘
       │
       │ ProfileId
       ↓
┌──────────────────────┐
│   Post Repository    │
│ (Query by ProfileId) │
└──────────────────────┘
```

## Critical Assertions

### 1️⃣ Profile Isolation
```
Profile1.Posts = [A, B, C]
Profile2.Posts = []  ← Different data
↓ (even though same user)
GetProfilePostsAsync(Profile1.Id) → [A, B, C]  ✅
GetProfilePostsAsync(Profile2.Id) → []         ✅
```

### 2️⃣ Active Profile Switching
```
SetMyActiveProfileAsync(Profile2.Id)
    ↓
GetMyActiveProfileAsync() == Profile2  ✅

SetMyActiveProfileAsync(Profile1.Id)
    ↓
GetMyActiveProfileAsync() == Profile1  ✅
```

### 3️⃣ Data Persistence
```
Create Profile1 → Create Posts (P, Q, R)
        ↓
Create Profile2 → Posts (empty)
        ↓
Switch → Profile2
        ↓
Switch → Profile1
        ↓
GetProfilePostsAsync(Profile1) → [P, Q, R]  ✅ (still there!)
```

## Test Layers

### Mocked
```
┌─────────────────────────────────┐
│ IHttpContextAccessor            │ ◄─ Mock (provide fake user)
│ ├─ HttpContext.User             │
│ └─ Claims["sub"] = testUserId    │
└─────────────────────────────────┘
```

### Real (or In-Memory)
```
┌─────────────────────────────────┐
│ ProfilesClient (Real)           │
│ ├─ Uses IProfileService         │
│ └─ Uses IProfileRepository      │
├─────────────────────────────────┤
│ PostsClient (Real)              │
│ ├─ Uses IPostService            │
│ └─ Uses IPostRepository         │
├─────────────────────────────────┤
│ Test Database                   │
│ ├─ Profiles table               │
│ ├─ Posts table                  │
│ └─ UserProfiles table           │
└─────────────────────────────────┘
```

## Expected Outcomes by Step

| Step | Action | Expected Result | Status |
|------|--------|-----------------|--------|
| 1 | Auth user alice-123 | User context created | ✅ |
| 2 | Get/Create Profile1 | Profile1 ID stored | ✅ |
| 3 | Create 3 posts in P1 | Posts have P1.Id | ✅ |
| 4 | Create Profile2 | Profile2 ID stored | ✅ |
| 5 | Set active = P2 | P2 is now active | ✅ |
| 6 | Query P2 posts | Result is empty list [] | ✅ |
| 7 | Create 2 posts in P2 | Posts have P2.Id | ✅ |
| 8 | Set active = P1 | P1 is now active | ✅ |
| 9 | Query P1 posts | Result has 3 items | ✅ |
| 10 | Query P2 posts | Result has 2 items | ✅ |

## Data Flow

```
User (alice-123)
  │
  ├─ Profile 1: "Tech"
  │   │
  │   ├─ Post A: "Python blog"
  │   ├─ Post B: "JS tips"
  │   └─ Post C: "Web trends"
  │
  └─ Profile 2: "Travel"
      │
      ├─ Post X: "Paris trip"
      └─ Post Y: "Tokyo guide"

Active Profile: switches between Profile1 ↔ Profile2

Posts visible to User:
  • When active = Profile1: [A, B, C]
  • When active = Profile2: [X, Y]
```

## Key Files Involved

- ✅ `IProfilesClient` - Switch active profile
- ✅ `IPostsClient` - Get posts by profile
- ✅ `ProfilesClient` (server-side) - Implementation
- ✅ `PostsClient` (server-side) - Implementation
- ✅ `IProfileService` - Profile business logic
- ✅ `IPostService` - Post business logic
- ✅ `IProfileRepository` - Profile persistence
- ✅ `IPostRepository` - Post persistence

## Questions for Clarification

Before we code, confirm:

1. ✅ **Integration vs Unit?** 
   - Plan uses Integration (real services, mocked auth)
   - Agree? Yes/No

2. ✅ **Database Type?**
   - Plan uses In-Memory SQLite for speed
   - Prefer: In-Memory / Test Container / Test DB?

3. ✅ **Test Isolation?**
   - Each test creates fresh profiles
   - Use same keycloak ID or different?

4. ✅ **Post Creation?**
   - Should we use real post creation flow?
   - Or mock post creation?

5. ✅ **Number of Profiles?**
   - Plan tests 2 profiles
   - Want to test 3+?

---

## Ready to Code?

If you approve this plan, I'll:
1. Create the test class structure
2. Implement the SetUp/TearDown
3. Write each test step
4. Add assertions
5. Run and verify all pass

**Approve? (Y/N)**
