# Sivar.Os Troubleshooting Guide

## Table of Contents
1. [Common Issues](#common-issues)
2. [Database Access](#database-access)
3. [ActiveProfile NULL Issue](#activeprofile-null-issue)
4. [Authentication & Profile Issues](#authentication--profile-issues)
5. [Debugging Tools](#debugging-tools)

---

## Common Issues

### Posts Not Loading in Feed

**Symptoms:**
- Home feed shows 0 posts even though posts were created
- Browser console shows posts being created successfully
- API returns empty feed

**Root Causes:**
1. **NULL ActiveProfileId in Users table** (Most Common)
2. Profile ID mismatch between post creation and feed loading
3. Visibility settings filtering out posts
4. User has no profiles

**Solution Applied:**
Modified `PostService.GetActivityFeedAsync()` to handle NULL ActiveProfile by fetching user's first available profile.

---

## Database Access

### Connection Details
- **Database Type**: PostgreSQL
- **Host**: localhost
- **Port**: 5432
- **Database Name**: XafSivarOs
- **Username**: postgres
- **Password**: 1234567890

### Connecting via psql

```powershell
# Set password environment variable
$env:PGPASSWORD='1234567890'

# Connect to database
psql -h localhost -U postgres -d XafSivarOs
```

### Useful Database Queries

#### Check User and ActiveProfile
```sql
SELECT "Id", "KeycloakId", "Email", "ActiveProfileId" 
FROM "Sivar_Users" 
WHERE "IsDeleted" = false;
```

#### Check All Profiles for a User
```sql
SELECT "Id", "UserId", "DisplayName", "IsDeleted" 
FROM "Sivar_Profiles" 
WHERE "UserId" = 'YOUR-USER-ID-HERE';
```

#### Count Posts by Profile
```sql
SELECT COUNT(*) as total_posts, "ProfileId", "Visibility" 
FROM "Sivar_Posts" 
WHERE "IsDeleted" = false 
GROUP BY "ProfileId", "Visibility";
```

#### Check Post Details
```sql
SELECT "Id", "ProfileId", "Content", "Visibility", "CreatedAt", "IsDeleted"
FROM "Sivar_Posts" 
WHERE "IsDeleted" = false 
ORDER BY "CreatedAt" DESC 
LIMIT 10;
```

#### Check Follows
```sql
SELECT "FollowerProfileId", "FollowedProfileId", "IsActive"
FROM "Sivar_ProfileFollowers"
WHERE "FollowerProfileId" = 'YOUR-PROFILE-ID-HERE';
```

---

## ActiveProfile NULL Issue

### Problem Description
When `ActiveProfileId` is NULL in the `Sivar_Users` table, many features fail because the system can't determine which profile to use for operations.

### Identifying the Issue

**Check if ActiveProfileId is NULL:**
```sql
SELECT "Id", "Email", "ActiveProfileId"
FROM "Sivar_Users"
WHERE "ActiveProfileId" IS NULL AND "IsDeleted" = false;
```

### Symptoms
- Posts not loading in feed (returns 0 items)
- Profile-related operations failing
- Logs showing: `ActiveProfile is NULL, fetching user's profiles`

### Solution Options

#### Option 1: Let the System Handle It (Current Implementation)
The `PostService.GetActivityFeedAsync()` method now automatically:
1. Checks if `ActiveProfile` is NULL
2. Fetches user's profiles using `GetProfilesByUserIdAsync`
3. Uses the first available profile

**Note**: This adds a database query overhead on every request.

#### Option 2: Fix the Database (Recommended)
Set the `ActiveProfileId` for users who have profiles:

```sql
-- For a specific user (replace the GUIDs with actual values)
UPDATE "Sivar_Users" 
SET "ActiveProfileId" = 'c3d381e6-07f1-4e82-92ff-a3f69ddb9391' 
WHERE "Id" = 'dde085dd-1750-4586-b9b4-a7f92c43041f';

-- For all users with NULL ActiveProfileId (sets to their first profile)
UPDATE "Sivar_Users" u
SET "ActiveProfileId" = (
    SELECT p."Id" 
    FROM "Sivar_Profiles" p 
    WHERE p."UserId" = u."Id" 
    AND p."IsDeleted" = false 
    ORDER BY p."CreatedAt" ASC 
    LIMIT 1
)
WHERE u."ActiveProfileId" IS NULL 
AND u."IsDeleted" = false
AND EXISTS (
    SELECT 1 
    FROM "Sivar_Profiles" p2 
    WHERE p2."UserId" = u."Id" 
    AND p2."IsDeleted" = false
);
```

---

## Authentication & Profile Issues

### Multiple Profile IDs Appearing

**Problem**: Different profile IDs appear in different contexts (e.g., one for creating posts, another for loading feed).

**Root Causes**:
1. User has multiple profiles
2. ActiveProfileId is NULL
3. Client-side and server-side using different profiles

**Debugging**:
```sql
-- Check how many profiles a user has
SELECT COUNT(*) as profile_count, "UserId"
FROM "Sivar_Profiles"
WHERE "IsDeleted" = false
GROUP BY "UserId"
HAVING COUNT(*) > 1;

-- List all profiles for a user
SELECT "Id", "DisplayName", "UserId", "IsActive", "CreatedAt"
FROM "Sivar_Profiles"
WHERE "UserId" = 'YOUR-USER-ID-HERE'
AND "IsDeleted" = false
ORDER BY "CreatedAt" ASC;
```

### Keycloak ID vs User ID vs Profile ID

**Important**: Understand the relationship:
- **Keycloak ID** (`sub` claim): External authentication ID
- **User ID** (`Sivar_Users.Id`): Internal user record
- **Profile ID** (`Sivar_Profiles.Id`): User's public-facing profile

**Mapping**:
```
Keycloak ID → User ID → ActiveProfileId (Profile ID)
```

**Query to see complete mapping**:
```sql
SELECT 
    u."KeycloakId",
    u."Id" as "UserId",
    u."Email",
    u."ActiveProfileId",
    p."Id" as "ProfileId",
    p."DisplayName"
FROM "Sivar_Users" u
LEFT JOIN "Sivar_Profiles" p ON u."ActiveProfileId" = p."Id"
WHERE u."IsDeleted" = false;
```

---

## Debugging Tools

### Server-Side Logging

The following services have comprehensive logging:

1. **PostService.GetActivityFeedAsync**
   - User lookup results
   - ActiveProfile status
   - Profile fetching (if ActiveProfile is NULL)
   - Repository call results
   - DTO mapping results

2. **PostsController**
   - API endpoint entry/exit
   - Parameter values
   - Service call results

3. **PostRepository.GetActivityFeedAsync**
   - Follow count
   - Feed type (discovery, own+public, followed profiles)
   - Query execution
   - Result count

### Log Patterns to Look For

**Posts Loading Successfully**:
```
[PostService.GetActivityFeedAsync] START - KeycloakId=...
[PostService.GetActivityFeedAsync] User found, UserId=...
[PostService.GetActivityFeedAsync] Using ActiveProfile: ...
[PostService.GetActivityFeedAsync] Repository returned 24 posts
```

**ActiveProfile NULL Scenario**:
```
[PostService.GetActivityFeedAsync] ActiveProfile is NULL, fetching user's profiles
[PostService.GetActivityFeedAsync] Using first available profile: ...
```

**No Posts Scenario**:
```
[PostRepository.GetActivityFeedAsync] Found 0 followed profiles
[PostRepository.GetActivityFeedAsync] Using own posts + public posts feed
[PostRepository.GetActivityFeedAsync] Total count: 0
```

### Browser Console

Check the browser console for:
- Post creation logs
- Feed loading logs
- API response data

### Database Query Analysis

Enable EF Core query logging in `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

---

## Potential System-Wide Issues

### Issue 1: Services Assuming ActiveProfile Exists

**Affected Areas**:
- `PostService.CreatePostAsync()`
- `PostService.GetActivityFeedAsync()` ✅ FIXED
- `ProfileService` methods
- `CommentService` methods
- `ReactionService` methods

**Pattern to Look For**:
```csharp
if (user?.ActiveProfile == null)
{
    return ...; // Early return, might be hiding NULL ActiveProfile issue
}
```

**Recommended Fix Pattern**:
```csharp
// Check if user exists
if (user == null)
{
    return ...;
}

// Handle NULL ActiveProfile
Guid profileId;
if (user.ActiveProfile != null)
{
    profileId = user.ActiveProfile.Id;
}
else
{
    var userProfiles = await _profileRepository.GetProfilesByUserIdAsync(user.Id);
    var firstProfile = userProfiles.FirstOrDefault();
    if (firstProfile == null)
    {
        return ...; // Truly no profile
    }
    profileId = firstProfile.Id;
}
```

### Issue 2: Client-Side vs Server-Side Profile ID Mismatch

**Scenario**: Blazor InteractiveAuto mode can cause different code paths.

**Detection**:
- Server logs show different profile IDs than client logs
- Operations work in browser but fail on page load

**Solution**: Ensure both server-side `PostsClient` and client-side HTTP `PostsClient` use the same logic.

### Issue 3: Feed Discovery Logic

**Scenario**: New users with 0 follows see no posts.

**Current Behavior**:
- If user has 0 follows AND `includeOwnPosts=true`: Shows own posts + all public posts
- If user has 0 follows AND `includeOwnPosts=false`: Shows all public posts (discovery feed)

**Query Check**:
```sql
-- Check if user is following anyone
SELECT COUNT(*) as follow_count
FROM "Sivar_ProfileFollowers"
WHERE "FollowerProfileId" = 'YOUR-PROFILE-ID-HERE'
AND "IsActive" = true;
```

---

## Quick Reference Commands

### Restart Application
```powershell
cd c:\Users\joche\source\repos\SivarOs\Sivar.Os\Sivar.Os
dotnet run --project Sivar.Os.csproj
```

### View Database Tables
```sql
-- List all tables
\dt

-- Describe table structure
\d "Sivar_Users"
\d "Sivar_Profiles"
\d "Sivar_Posts"
```

### Check Application Health
```powershell
# Check if server is running
curl https://localhost:5001

# Check database connection
$env:PGPASSWORD='1234567890'; psql -h localhost -U postgres -d XafSivarOs -c "SELECT 1;"
```

---

## Contact & Support

**Developer**: Jose Ojeda  
**Last Updated**: October 27, 2025  
**Branch**: postloading  
**Fix Applied**: NULL ActiveProfile handling in PostService.GetActivityFeedAsync
