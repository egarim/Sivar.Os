# Profile Switching Test - Implementation Checklist

## 📋 Test Plan Overview

A user logs in, creates multiple profiles, creates posts in each profile, switches between them, and verifies posts are isolated by profile.

## 🎯 The Scenario (TL;DR)

```
Alice (keycloak ID) 
  ├─ Profile 1: "Tech Guy" → Posts: [A, B, C]
  ├─ Profile 2: "Traveler" → Posts: [X, Y]
  
Switch: Profile 1 → [A, B, C] ✅ | Profile 2 → ❌ (not visible)
Switch: Profile 2 → [X, Y] ✅ | Profile 1 → ❌ (not visible)
```

## ✅ Plan Components

- [x] **10-Step Test Flow** - Detailed in `PROFILE_SWITCHING_TEST_PLAN.md`
- [x] **Visual Diagrams** - In `PROFILE_SWITCHING_TEST_PLAN_VISUAL.md`
- [x] **Architecture** - Integration test with mocked auth
- [x] **Assertions** - Profile isolation verified
- [x] **Data Model** - 2 profiles, multiple posts per profile

## 🏗️ Test Structure

```
Tests/Integration/ProfileSwitching/
├── ProfileSwitchingIntegrationTests.cs    ← Main test class
├── ProfileSwitchingTestFixture.cs         ← Test data setup
└── ProfileSwitchingTestHelpers.cs         ← Helper methods
```

## 🔧 Components to Test

| Component | Method | Purpose |
|-----------|--------|---------|
| ProfilesClient | GetMyProfileAsync() | Get first profile |
| ProfilesClient | CreateMyProfileAsync() | Create new profiles |
| ProfilesClient | SetMyActiveProfileAsync() | Switch active profile |
| ProfilesClient | GetMyActiveProfileAsync() | Check current active |
| PostsClient | GetProfilePostsAsync() | Get posts for profile |
| PostsClient | CreatePostAsync() | Create test posts |

## 🧪 Test Steps

```
[ ] Step 1:  Setup authentication context
[ ] Step 2:  Get or create first profile
[ ] Step 3:  Create 3-5 posts in Profile 1
[ ] Step 4:  Create second profile
[ ] Step 5:  Switch active profile to Profile 2
[ ] Step 6:  Verify Profile 2 has no posts
[ ] Step 7:  Create 2-3 posts in Profile 2
[ ] Step 8:  Switch active profile back to Profile 1
[ ] Step 9:  Verify Profile 1's original posts still exist
[ ] Step 10: Verify Profile 2's posts still exist
```

## ✔️ Key Assertions

```
[ ] ✓ Profile1.Posts count = 3-5
[ ] ✓ Profile2.Posts count = 0 (after creation, before post creation)
[ ] ✓ Profile2.Posts count = 2-3 (after creating posts)
[ ] ✓ Posts from Profile1 NOT visible in Profile2
[ ] ✓ Posts from Profile2 NOT visible in Profile1
[ ] ✓ Active profile switches correctly (bidirectional)
[ ] ✓ Data persists across profile switches
[ ] ✓ Each post has correct ProfileId
```

## 🎓 Questions to Clarify

Before we code, please answer:

**Q1: Post Creation Method?**
- [ ] A) Real post creation (through PostsClient)
- [ ] B) Mock/Direct insertion (bypass client)
- [ ] Preference: _______________

**Q2: Number of Profiles?**
- [ ] A) 2 profiles (current plan)
- [ ] B) 3+ profiles (more comprehensive)
- [ ] Preference: _______________

**Q3: Rapid Switching Test?**
- [ ] A) No, sequential only (current plan)
- [ ] B) Yes, add rapid switch scenario
- [ ] Preference: _______________

**Q4: Post Content Verification?**
- [ ] A) Check post exists in correct profile
- [ ] B) Also verify post content matches
- [ ] Preference: _______________

**Q5: User Isolation?**
- [ ] A) One user, multiple profiles (current plan)
- [ ] B) Multiple users, verify no cross-user data
- [ ] Preference: _______________

**Q6: Test Database?**
- [ ] A) In-Memory SQLite (fastest)
- [ ] B) Test container (more realistic)
- [ ] C) Dedicated test database
- [ ] Preference: _______________

## 📊 Expected Results

| Metric | Expected | Status |
|--------|----------|--------|
| Test execution time | < 2 seconds | TBD |
| All assertions pass | 100% | TBD |
| Code coverage | > 80% | TBD |
| Profile isolation | 100% | TBD |

## 🚀 Implementation Timeline

```
Phase 1: Setup (5 min)
  ├─ Create test project structure
  ├─ Create test class
  └─ Create fixture/helpers

Phase 2: Infrastructure (10 min)
  ├─ Setup database
  ├─ Configure services
  ├─ Mock IHttpContextAccessor
  └─ Create test helper methods

Phase 3: Test Implementation (20 min)
  ├─ Implement steps 1-5 (profile setup)
  ├─ Implement steps 6-7 (posts and switching)
  └─ Implement steps 8-10 (verification)

Phase 4: Verification (5 min)
  ├─ Run all tests
  ├─ Fix any issues
  └─ Add logging/debugging as needed

Total: ~40 minutes
```

## 📝 Deliverables

After implementation, we'll have:

1. **ProfileSwitchingIntegrationTests.cs** (Main test, ~300-400 lines)
   - Complete test flow
   - All assertions
   - Documentation comments

2. **ProfileSwitchingTestFixture.cs** (Test data, ~100-150 lines)
   - Test user setup
   - Profile creation helpers
   - Post creation helpers

3. **ProfileSwitchingTestHelpers.cs** (Helper methods, ~80-120 lines)
   - Service initialization
   - Database setup
   - Mock creation

4. **All tests passing** ✅
   - Profile isolation verified
   - Switching works correctly
   - Data persistence confirmed

## ✅ Approval Checklist

- [ ] Plan understood
- [ ] Architecture approved
- [ ] Questions answered above
- [ ] Ready to implement

---

## 📌 Files for Reference

1. `PROFILE_SWITCHING_TEST_PLAN.md` - Full detailed plan with 10 steps
2. `PROFILE_SWITCHING_TEST_PLAN_VISUAL.md` - Visual diagrams and flowcharts
3. `PROFILE_SWITCHING_TEST_PLAN_SUMMARY.md` - Executive summary

---

## 🎬 Ready?

Once you approve this plan and answer the clarification questions, I'll immediately start implementing the test code! 

**Let me know:**
- ✅ Plan looks good? 
- ✅ Any changes needed?
- ✅ Answer questions above?
- ✅ Ready to code?

Then we'll proceed to: **→ Implementation Phase**
