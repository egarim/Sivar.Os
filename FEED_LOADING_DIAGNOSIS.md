# Feed Posts Loading Flow Diagnosis

## Expected Flow

1. **Page Load (Server Pre-rendering)**
   - Server-side renders Home.razor with empty data
   - Server logs: `[PostsClient.GetFeedPostsAsync] Server-side called...`
   - Browser receives empty pre-rendered HTML

2. **Interactive Component Starts (Client-side)**
   - Blazor JavaScript loads and enhances component
   - Component becomes interactive
   - `OnInitializedAsync()` runs on client

3. **Client-Side API Calls**
   - Browser makes HTTP GET `/api/posts/feed?pageSize=10&pageNumber=1`
   - Expected log in server console: `[PostsController.GetActivityStreamFeed] API ENDPOINT CALLED`
   - API returns `PostFeedDto` with posts
   - Browser renders the feed

## Current Issue

The server console is showing:
```
info: Sivar.Os.Services.Clients.PostsClient[0]
      Feed posts retrieved: 0 items
[Home] No posts found in feed
```

This indicates:
- ✅ Server pre-rendering is working
- ❌ Either:
  - The API endpoint is NOT being called (no HTTP call from client)
  - OR The API endpoint returns 0 posts

## What to Check

1. **Open Browser Network Tab** (F12 → Network)
   - Look for request: `GET /api/posts/feed?pageSize=10&pageNumber=1`
   - Check if this request is being made
   - Check the response status (200? 401? 404?)
   - Check the response body (is it valid JSON?)

2. **Monitor Server Console**
   - After opening the page, look for:
     - `[PostsController.GetActivityStreamFeed] API ENDPOINT CALLED`
     - `[PostsController.GetActivityStreamFeed] Keycloak ID extracted: ...`
     - `[PostsController.GetActivityStreamFeed] Calling PostService.GetActivityFeedAsync...`

3. **Check Database**
   - Open PostgreSQL and verify there are posts:
     ```sql
     SELECT COUNT(*) FROM "Sivar_Posts";
     SELECT * FROM "Sivar_Posts" LIMIT 5;
     ```

## Next Steps

1. Refresh browser at https://localhost:5001
2. Open DevTools Network tab
3. Look for `/api/posts/feed` request
4. Report if you see the request and what the response is
5. Check server console for new logs
