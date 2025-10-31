# Feed Posts JSON Serialization Fix

## Issue
The browser console showed error: **"Error loading feed posts: The JSON value could not be converted to System.Collections.Generic.IEnumerable`1[Sivar.Os.Shared.DTOs.PostDto]"**

## Root Cause
**Type mismatch** between API response and client expectation:
- **API endpoint returns**: `PostFeedDto` (object with pagination metadata: `Posts`, `Page`, `PageSize`, `TotalCount`)
- **Client expected**: `IEnumerable<PostDto>` (just a list)

The JSON deserialization failed because the API response structure didn't match what the client was trying to deserialize.

## Solution
Updated all 4 feed-related methods across 3 files to return `PostFeedDto` instead of `IEnumerable<PostDto>`:

### Files Modified

#### 1. **Sivar.Os.Shared/Clients/IPostsClient.cs** (Interface)
```csharp
// OLD - Expected IEnumerable
Task<IEnumerable<PostDto>> GetFeedPostsAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
Task<IEnumerable<PostDto>> GetProfilePostsAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
Task<IEnumerable<PostDto>> SearchPostsAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
Task<IEnumerable<PostDto>> GetTrendingPostsAsync(int pageSize = 20, CancellationToken cancellationToken = default);

// NEW - Returns PostFeedDto with pagination
Task<PostFeedDto> GetFeedPostsAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
Task<PostFeedDto> GetProfilePostsAsync(Guid profileId, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
Task<PostFeedDto> SearchPostsAsync(string query, int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default);
Task<PostFeedDto> GetTrendingPostsAsync(int pageSize = 20, CancellationToken cancellationToken = default);
```

#### 2. **Sivar.Os.Client/Clients/PostsClient.cs** (HTTP Client Implementation)
Updated all 4 methods to deserialize to `PostFeedDto`:
```csharp
public async Task<PostFeedDto> GetFeedPostsAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
{
    return await GetAsync<PostFeedDto>($"api/posts/feed?pageSize={pageSize}&pageNumber={pageNumber}", cancellationToken);
}
// ... similar for GetProfilePostsAsync, SearchPostsAsync, GetTrendingPostsAsync
```

#### 3. **Sivar.Os/Services/Clients/PostsClient.cs** (Server-Side Client Implementation)
Updated all 4 methods to return `PostFeedDto`:
```csharp
public async Task<PostFeedDto> GetFeedPostsAsync(int pageSize = 20, int pageNumber = 1, CancellationToken cancellationToken = default)
{
    try
    {
        var feed = new PostFeedDto
        {
            Posts = new List<PostDto>(),
            Page = pageNumber - 1,
            PageSize = pageSize,
            TotalCount = 0
        };
        _logger.LogInformation("Feed posts retrieved: {Count} items", feed.Posts.Count);
        return feed;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving feed posts");
        throw;
    }
}
// ... similar for GetProfilePostsAsync, SearchPostsAsync, GetTrendingPostsAsync
```

#### 4. **Sivar.Os.Client/Pages/Home.razor** (Consumer Update)
Updated `LoadFeedPostsAsync()` to handle the new structure:
```csharp
private async Task LoadFeedPostsAsync()
{
    try
    {
        Console.WriteLine($"[Home] Loading feed posts (page {_currentPage})...");
        var feedDto = await SivarClient.Posts.GetFeedPostsAsync(pageSize: 10, pageNumber: _currentPage);
        
        if (feedDto?.Posts != null && feedDto.Posts.Any())
        {
            _posts = feedDto.Posts;
            _totalPages = feedDto.TotalPages;
            
            Console.WriteLine($"[Home] Loaded {_posts.Count} posts (Page {feedDto.Page + 1} of {feedDto.TotalPages})");
        }
        else
        {
            Console.WriteLine("[Home] No posts found in feed");
            _posts = new List<PostDto>();
            _totalPages = 0;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Home] Error loading feed posts: {ex.Message}");
    }
}
```

## Benefits of PostFeedDto
The `PostFeedDto` class provides:
- ✅ **Posts**: List of actual post objects
- ✅ **Page**: Current page number (0-based)
- ✅ **PageSize**: Number of posts per page
- ✅ **TotalCount**: Total number of posts available
- ✅ **TotalPages**: Calculated property for total pages
- ✅ **HasMorePages**: Helper property to check if more pages exist

## Verification
- ✅ **Build Status**: 0 errors, 20 non-critical warnings
- ✅ **Application**: Running on https://localhost:5001
- ✅ **JSON Serialization**: Now matches API response structure
- ✅ **Pagination**: Full pagination metadata now available to clients

## Expected Browser Console Output
After fix, you should see:
```
[Home] Loading feed posts (page 1)...
[Home] Loaded X posts (Page 1 of Y)
```

Instead of:
```
[Home] Error loading feed posts: The JSON value could not be converted to System.Collections.Generic.IEnumerable`1[Sivar.Os.Shared.DTOs.PostDto]
```

## Testing Instructions
1. Navigate to https://localhost:5001
2. Log in with Keycloak
3. Observe browser console (F12 → Console tab)
4. Feed posts should now load without JSON deserialization errors
5. Posts list should display on the home page
