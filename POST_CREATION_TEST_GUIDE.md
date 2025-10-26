# Testing Post Creation - Step-by-Step Guide

## Current Status
- ✅ Application running on https://localhost:5001
- ✅ User authenticated as Jose Ojeda
- ✅ API endpoints working
- ⚠️ Post submission needs testing

## Step 1: Open the Application
1. Open browser: https://localhost:5001
2. You should be logged in already (authenticated via Keycloak)
3. You should see the home feed page

## Step 2: Locate the Post Composer
The post composer should be at the top of the feed with:
- Text input area (where you type your post)
- Post type dropdown (General, Product, Service, Event, Job)
- Visibility dropdown (Public/Private)
- Submit button

## Step 3: Create a Test Post

### Option A: Simple Text Post (RECOMMENDED FOR TESTING)
1. **Click in the text input area** of the post composer
2. **Type a simple test message:**
   ```
   This is my first test post! Testing the complete workflow.
   ```
3. **Verify the text appears** in the input field
4. **Click the Post/Submit button**

### Option B: Using Browser Console (ADVANCED)
If the UI doesn't work, you can use the browser console:

1. Open DevTools: **F12**
2. Go to **Console tab**
3. Look for existing console logs to understand the component state
4. Check if there are any JavaScript errors

## Step 4: Monitor the Workflow

### Watch Browser Console
Open F12 → Console tab and look for:
```
[Home] Submitting new post...
[Home] Post created successfully: <UUID>
```

### Watch Server Console
Look for:
```
[PostsController.CreatePost] POST request received at <timestamp>
[PostsController.CreatePost] Step 1: Extracting Keycloak ID from request
[PostsController.CreatePost] ✅ SUCCESS: Post created with ID = <UUID>
```

### Verify in Network Tab
1. F12 → Network tab
2. Create the post
3. Look for request: **POST /api/posts**
4. Check response status: **201 Created** (not 400, 401, or 500)
5. Check response body: Should contain the new post data

## Step 5: Verify the Feed
After creating a post:
1. The new post should appear at the **top of your feed**
2. Browser console should show:
   ```
   [Home] Post created successfully: <post-id>
   [Home] Loaded X posts (Page 1 of Y)
   ```
3. Server console should eventually show feed reload with post count increased

## Possible Issues & Solutions

### Issue: "Post text is empty, skipping submit"
**Cause**: Text input not binding to component variable

**Solution**:
1. Check that you actually typed text in the input field
2. Click outside the field first, then click back in
3. Try typing again
4. Check browser console for any JavaScript errors

### Issue: Button doesn't respond
**Cause**: Possible rendering issue

**Solution**:
1. Refresh the page (F5)
2. Try submitting again
3. Check F12 Console for JavaScript errors

### Issue: 401 Unauthorized error
**Cause**: Authentication token expired or not being sent

**Solution**:
1. Refresh the page to get new authentication
2. Check that cookies are being sent with requests (Network → Request Headers)

### Issue: 400 Bad Request
**Cause**: Invalid post data being sent

**Solution**:
1. Check browser console for validation errors
2. Check server console for specific error message
3. Verify CreatePostDto has required fields

## Complete Flow Summary

```
User Types Post Text
    ↓
User Clicks Submit Button
    ↓
Browser validates _postText is not empty
    ↓
Browser calls: SivarClient.Posts.CreatePostAsync(createPostDto)
    ↓
HTTP POST /api/posts with CreatePostDto
    ↓
Server receives request → PostsController.CreatePost()
    ↓
Server extracts Keycloak ID
    ↓
Server validates request
    ↓
Server checks rate limiting (5 posts/minute)
    ↓
Server calls PostService.CreatePostAsync()
    ↓
Database: INSERT post record
    ↓
Server returns 201 Created with PostDto
    ↓
Browser receives response with new PostDto
    ↓
Browser adds post to feed (insert at position 0)
    ↓
Feed refreshes and shows the new post
```

## Expected Logs After Creating a Post

### Browser Console
```
[Home] Submitting new post...
[Home] Post created successfully: a1b2c3d4-e5f6-7890-1234-567890abcdef
[Home] Loaded 1 posts (Page 1 of 1)
```

### Server Console
```
[PostsController.CreatePost] POST request received at 2025-10-25 19:30:45.123
[PostsController.CreatePost] Step 1: Extracting Keycloak ID from request
[GetKeycloakIdFromRequest] ✓ Found 'sub' claim: 28b46a88-d191-4c63-8812-1bb8f3332228
[PostsController.CreatePost] ✅ SUCCESS: Post created with ID = a1b2c3d4-e5f6-7890-1234-567890abcdef
```

### Network Tab
```
POST /api/posts
Status: 201 Created
Response: {
  "id": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "content": "This is my first test post!",
  "postType": 0,
  "visibility": 0,
  "createdAt": "2025-10-25T19:30:45.123Z",
  ...
}
```

## Next Steps

1. **Try creating the post** following the steps above
2. **Take note of what happens** - errors, console logs, etc.
3. **Share the results** - particularly:
   - Did the post appear on the feed?
   - What errors did you see?
   - What server console logs appeared?

This will help determine if the complete workflow is functioning correctly.
