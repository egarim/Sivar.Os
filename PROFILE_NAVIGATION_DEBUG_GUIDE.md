# Profile Navigation Debug Guide

## Issue
Clicking on "Jose Ojeda" (author name) in posts does nothing - no navigation occurs.

## Debug Steps

### Step 1: Check Browser Console Logs

1. **Run the application** (F5 in Visual Studio)
2. **Open Browser DevTools** (F12)
3. **Go to Console tab**
4. **Click on "Jose Ojeda" name in a post**

### Step 2: Look for These Log Messages

You should see console output like this when you click:

```
=============================================================
[PostCard.HandleAuthorClick] Called
[PostCard.HandleAuthorClick] Post Profile DisplayName: 'Jose Ojeda'
[PostCard.HandleAuthorClick] Post Profile Handle: 'jose-ojeda'  <-- CRITICAL: Check this value
[PostCard.HandleAuthorClick] Handle IsNull: false
[PostCard.HandleAuthorClick] Handle IsEmpty: false
[PostCard.HandleAuthorClick] Invoking OnAuthorClick with: 'jose-ojeda'
[PostCard.HandleAuthorClick] OnAuthorClick.InvokeAsync completed
=============================================================
[Home.ViewProfile] Called with: 'jose-ojeda'
[Home.ViewProfile] Is null or empty: false
[Home.ViewProfile] Navigating to: /jose-ojeda
[Home.ViewProfile] Navigation.NavigateTo() completed
=============================================================
```

### Step 3: Identify the Problem

**Check each log line:**

#### If you see NO LOGS at all:
- The click event is not wired up correctly
- Check if `OnAuthorClick` parameter is being passed correctly

#### If Handle is NULL or EMPTY:
```
[PostCard.HandleAuthorClick] Post Profile Handle: ''
[PostCard.HandleAuthorClick] Handle IsNull: true
```
**Problem**: The Handle field is not being populated from the database
**Solution**: Check the ProfilesClient.MapToDto method

#### If Navigation logs appear but page doesn't change:
```
[Home.ViewProfile] Navigating to: /jose-ojeda
```
**Problem**: ProfilePage route might not be working
**Solution**: Check ProfilePage.razor route configuration

### Step 4: Check Database Handle Values

**Option A: Using pgAdmin or similar tool:**
```sql
SELECT "Id", "DisplayName", "Handle" 
FROM "Sivar_Profiles" 
WHERE "IsDeleted" = false 
ORDER BY "CreatedAt" DESC;
```

**Expected Result:**
| Id | DisplayName | Handle |
|----|-------------|--------|
| guid-123... | Jose Ojeda | jose-ojeda |

**If Handle column is empty:**
- Profiles were created before the Handle field was added
- Need to run migration or manually update handles

### Step 5: Check Feed Activity Logs

Look for these logs when the page loads:

```
[Home.LoadFeedActivitiesAsync] 📊 Processing X activities:
  Activity 1:
    ...
    Post Profile: Jose Ojeda
    Post Profile Handle: 'jose-ojeda'  <-- Should NOT be null or empty
```

## Quick Fixes

### Fix 1: If Handle is NULL/Empty in Database

Run this SQL to generate handles for existing profiles:

```sql
UPDATE "Sivar_Profiles"
SET "Handle" = lower(replace("DisplayName", ' ', '-'))
WHERE "Handle" IS NULL OR "Handle" = ''
  AND "IsDeleted" = false;
```

### Fix 2: If Event Not Firing

The PostCard component passes the handle to Home.ViewProfile:
```razor
OnAuthorClick="@HandleAuthorClick"
```

Which calls:
```csharp
private async Task HandleAuthorClick()
{
    var handle = Post.Profile?.Handle ?? string.Empty;
    await OnAuthorClick.InvokeAsync(handle);
}
```

Which triggers in Home.razor:
```razor
<PostCard ... OnAuthorClick="@((handle) => ViewProfile(handle))" />
```

### Fix 3: Verify ProfilePage Route

Check `ProfilePage.razor` has:
```razor
@page "/{Identifier}"
```

NOT:
```razor
@page "/profile/{Identifier}"  ❌ Wrong!
```

## Next Steps After Testing

1. **Copy the console output** from Step 2
2. **Report what you see** - share the exact console logs
3. **Check database** - verify Handle values exist
4. **We'll fix** the root cause based on what the logs show

## Expected Working Flow

1. User clicks "Jose Ojeda" → PostHeader.OnAuthorClick fires
2. PostCard.HandleAuthorClick receives event → logs profile info
3. Extracts Handle: "jose-ojeda"
4. Invokes Home.ViewProfile with "jose-ojeda"
5. Home.ViewProfile navigates to "/jose-ojeda"
6. ProfilePage loads with Identifier="jose-ojeda"
7. Profile displays successfully

---

**Ready to test? Run the app and click on a profile name, then check the console!**
