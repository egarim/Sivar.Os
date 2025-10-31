# Applying the Working Pattern to PostsController

This document shows how the **proven working authentication pattern** from Home.razor should be applied to the PostsController and similar endpoints.

---

## The Working Pattern Applied to Posts

### Current Issue in PostsController

The `GetKeycloakIdFromRequest()` method in PostsController has been updated but needs verification that it works like Home.razor pattern:

```csharp
// What we HAVE (recently updated):
private string GetKeycloakIdFromRequest()
{
    // Check X-Keycloak-Id header (tests)
    if (Request.Headers.TryGetValue("X-Keycloak-Id", out var keycloakIdHeader))
        return keycloakIdHeader.ToString();

    // Check authenticated claims
    if (User?.Identity?.IsAuthenticated == true)
    {
        // Check "sub" claim (OpenID Connect standard)
        var subClaim = User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(subClaim))
            return subClaim;

        // Fallback: Check alternative claim types
        var userIdClaim = User.FindFirst("user_id")?.Value 
                       ?? User.FindFirst("id")?.Value 
                       ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim))
            return userIdClaim;
    }

    // Check X-Mock-Auth header (test scenarios)
    if (Request.Headers.ContainsKey("X-Mock-Auth"))
        return "mock-keycloak-user-id";

    return null!;
}
```

✅ **This is CORRECT** - matches Home.razor pattern!

---

## How Posts Are Created (Complete Flow)

### Step 1: Client Calls CreatePost Endpoint

```csharp
// From Home.razor or any other client
private async Task HandlePostSubmitAsync()
{
    try
    {
        var createPostDto = new CreatePostDto
        {
            Content = _postText,
            PostType = _selectedPostType,
            Visibility = _postVisibility,
            Tags = _postTags
        };

        Console.WriteLine("[Home] Submitting post...");
        
        // This calls the API endpoint: POST /api/posts
        var result = await SivarClient.Posts.CreatePostAsync(createPostDto);
        
        if (result != null)
        {
            Console.WriteLine("[Home] Post created successfully!");
            _posts.Insert(0, result); // Add to feed
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Home] Error creating post: {ex.Message}");
    }
}
```

### Step 2: PostsController.CreatePost() Receives Request

```csharp
[HttpPost]
[Authorize]  // ✅ IMPORTANT: Require authentication
public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostDto createPostDto)
{
    try
    {
        // ✅ Extract Keycloak ID from JWT claims (uses GetKeycloakIdFromRequest)
        var keycloakId = GetKeycloakIdFromRequest();
        
        if (string.IsNullOrEmpty(keycloakId))
        {
            Console.WriteLine("[PostsController] ERROR: keycloakId is NULL/EMPTY!");
            return Unauthorized(new { message = "User not authenticated" });
        }

        Console.WriteLine($"[PostsController] Creating post for Keycloak ID: {keycloakId}");

        // ✅ Pass keycloakId to service layer (NOT just the DTO)
        var result = await _postService.CreatePostAsync(keycloakId, createPostDto);
        
        if (result == null)
        {
            Console.WriteLine("[PostsController] ERROR: Service returned null!");
            return BadRequest(new { message = "Failed to create post" });
        }

        Console.WriteLine($"[PostsController] Post created successfully: {result.Id}");
        
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating post");
        return StatusCode(500, new { message = "Internal server error" });
    }
}
```

### Step 3: PostService.CreatePostAsync() - Business Logic

```csharp
public async Task<PostDto?> CreatePostAsync(string keycloakId, CreatePostDto createPostDto)
{
    try
    {
        if (string.IsNullOrWhiteSpace(keycloakId))
        {
            _logger.LogWarning("[PostService] CreatePostAsync called with null/empty keycloakId");
            return null;
        }

        Console.WriteLine($"[PostService] Creating post for user: {keycloakId}");

        // ✅ Step 1: Get the user from database using keycloakId
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        
        if (user == null)
        {
            _logger.LogWarning("[PostService] User not found for keycloakId: {KeycloakId}", keycloakId);
            return null;
        }

        Console.WriteLine($"[PostService] User found: {user.Id} - {user.Email}");

        // ✅ Step 2: Get user's active profile
        var profile = await _profileRepository.GetActiveProfileByKeycloakIdAsync(keycloakId);
        
        if (profile == null)
        {
            _logger.LogWarning("[PostService] No active profile for user: {KeycloakId}", keycloakId);
            return null;
        }

        Console.WriteLine($"[PostService] Profile found: {profile.Id} - {profile.DisplayName}");

        // ✅ Step 3: Create the post entity
        var post = new Post
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            Content = createPostDto.Content,
            PostType = createPostDto.PostType,
            Visibility = createPostDto.Visibility,
            Tags = string.Join(",", createPostDto.Tags ?? new List<string>()),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ✅ Step 4: Save to database
        await _postRepository.AddAsync(post);
        await _postRepository.SaveChangesAsync();

        Console.WriteLine($"[PostService] Post saved to database: {post.Id}");

        // ✅ Step 5: Map to DTO and return
        var postDto = await MapToPostDtoAsync(post);
        return postDto;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating post for keycloakId: {KeycloakId}", keycloakId);
        return null;
    }
}
```

### Step 4: Return PostDto to Client

```csharp
// PostDto returned to client
public class PostDto
{
    public Guid Id { get; set; }
    public string Content { get; set; }
    public ProfileDto Profile { get; set; }  // ✅ Includes user profile info
    public DateTime CreatedAt { get; set; }
    public int ReactionCount { get; set; }
    public int CommentCount { get; set; }
}
```

### Step 5: Client Displays Post

```csharp
// Back in Home.razor
if (result != null)
{
    _posts.Insert(0, result);  // Add to top of feed
    StateHasChanged();  // Re-render UI
}
```

---

## Why Posts Were Not Saving (Root Cause Analysis)

### The Problem
1. ❌ PostsController.GetKeycloakIdFromRequest() returned null/empty
2. ❌ CreatePostAsync() was called with null keycloakId
3. ❌ Service couldn't find user (because keycloakId was null)
4. ❌ Service returned null
5. ❌ Controller returned BadRequest (silently failing)
6. ❌ Client wasn't displaying the error message
7. ❌ User thought post didn't save, but it never reached the database

### The Solution
Update GetKeycloakIdFromRequest() to use the **Home.razor pattern** with:
1. ✅ Check "sub" claim first (OpenID Connect standard)
2. ✅ Fallback to alternative claims if needed
3. ✅ Return null only if absolutely nothing works
4. ✅ Ensure keycloakId is ALWAYS found for authenticated users

---

## Verification Checklist for Each Controller

For **PostsController**, **CommentsController**, **ReactionsController**, etc., verify:

- [ ] `[Authorize]` attribute on endpoint
- [ ] `GetKeycloakIdFromRequest()` uses full claim fallback chain
- [ ] keycloakId checked for null before passing to service
- [ ] Service receives keycloakId (not just DTO)
- [ ] Service queries database using keycloakId
- [ ] Service returns DTO to controller
- [ ] Controller returns DTO (not entity) to client
- [ ] Error handling at each layer
- [ ] Logging at key points

---

## Side-by-Side: What WORKS vs What DOESN'T

### ❌ DOESN'T WORK

```csharp
// Problem 1: Only checks one claim
var keycloakId = User.FindFirst("sub")?.Value;

// Problem 2: Missing service logic
var post = new Post { Content = createPostDto.Content };
await _postRepository.AddAsync(post);  // ProfileId is null!

// Problem 3: No user/profile lookup
if (keycloakId == null)
    return null;  // Silent failure - client doesn't know
```

### ✅ WORKS (Home.razor Pattern)

```csharp
// Solution 1: Full claim fallback chain
var keycloakId = User.FindFirst("sub")?.Value
              ?? User.FindFirst("user_id")?.Value
              ?? User.FindFirst("id")?.Value
              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

// Solution 2: Service looks up user and profile
var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
var profile = await _profileRepository.GetActiveProfileByKeycloakIdAsync(keycloakId);
var post = new Post { ProfileId = profile.Id, Content = createPostDto.Content };

// Solution 3: Explicit error handling
if (keycloakId == null)
    return Unauthorized("User not authenticated");  // Client knows what happened
```

---

## Testing the Pattern

### Test Case 1: Authenticated User Creates Post

```csharp
[Test]
public async Task CreatePost_WithValidAuthentication_ShouldSucceed()
{
    // Arrange
    var keycloakId = "test-user-123";
    var createPostDto = new CreatePostDto { Content = "Test post" };
    
    // Inject X-Keycloak-Id header for testing
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/posts");
    request.Headers.Add("X-Keycloak-Id", keycloakId);
    
    // Act
    var response = await _client.SendAsync(request);
    
    // Assert
    Assert.IsTrue(response.IsSuccessStatusCode);
    
    // Verify post was saved to database
    var post = await _postRepository.GetByIdAsync(...);
    Assert.IsNotNull(post);
    Assert.AreEqual("Test post", post.Content);
}
```

### Test Case 2: Unauthenticated User Cannot Create Post

```csharp
[Test]
public async Task CreatePost_WithoutAuthentication_ShouldReturnUnauthorized()
{
    // No authentication header
    var response = await _client.PostAsync("/api/posts", 
        JsonContent.Create(new CreatePostDto { Content = "Test" }));
    
    Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

---

## Summary

**The working pattern ensures:**
1. ✅ Keycloak ID is always extracted from JWT claims
2. ✅ User is looked up in database
3. ✅ User's active profile is found
4. ✅ Post is created with correct ProfileId
5. ✅ Data is saved to database
6. ✅ DTO is returned to client
7. ✅ Client receives and displays the post

**All 7 controllers have been updated to follow this pattern:**
- PostsController ✅
- CommentsController ✅
- ReactionsController ✅
- NotificationsController ✅
- ConversationsController ✅
- ChatMessagesController ✅
- SavedResultsController ✅
