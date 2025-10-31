# Data Models and DTOs Reference

This document defines all the DTOs and models involved in the user/profile creation and post creation flow.

---

## User & Authentication DTOs

### `UserAuthenticationInfo`
**Shared DTO** - Used when client sends authentication info to server

```csharp
// Location: Sivar.Os.Shared/Services/UserAuthenticationInfo.cs
public class UserAuthenticationInfo
{
    /// <summary>
    /// User's email address (primary identifier with KeycloakId)
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// User's first name from Keycloak
    /// </summary>
    public string FirstName { get; set; }

    /// <summary>
    /// User's last name from Keycloak
    /// </summary>
    public string LastName { get; set; }

    /// <summary>
    /// User's role (e.g., "RegisteredUser", "Admin")
    /// </summary>
    public string Role { get; set; }
}
```

### `UserAuthenticationResult`
**Shared DTO** - Returned by authentication service

```csharp
// Location: Sivar.Os.Shared/Services/UserAuthenticationResult.cs
public class UserAuthenticationResult
{
    /// <summary>
    /// Whether authentication was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Whether this is a new user (profile was auto-created)
    /// </summary>
    public bool IsNewUser { get; set; }

    /// <summary>
    /// The authenticated user's DTO
    /// </summary>
    public UserDto User { get; set; }

    /// <summary>
    /// The user's active profile
    /// </summary>
    public ProfileDto ActiveProfile { get; set; }

    /// <summary>
    /// Error message if authentication failed
    /// </summary>
    public string ErrorMessage { get; set; }
}
```

### `UserDto`
**Shared DTO** - Represents user information returned from API

```csharp
// Location: Sivar.Os.Shared/DTOs/UserDto.cs
public class UserDto
{
    /// <summary>
    /// Internal database ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; set; }

    /// <summary>
    /// User's role
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// Whether user account is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When user account was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When user last logged in
    /// </summary>
    public DateTime? LastLogin { get; set; }
}
```

---

## Profile DTOs

### `ProfileDto`
**Shared DTO** - Represents a user profile

```csharp
// Location: Sivar.Os.Shared/DTOs/ProfileDto.cs
public class ProfileDto
{
    /// <summary>
    /// Profile's unique ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display name for the profile (e.g., "John's Business")
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Profile bio/description
    /// </summary>
    public string Bio { get; set; }

    /// <summary>
    /// Avatar image URL
    /// </summary>
    public string Avatar { get; set; }

    /// <summary>
    /// Profile's visibility level (Public, Private, etc)
    /// </summary>
    public VisibilityLevel VisibilityLevel { get; set; }

    /// <summary>
    /// Profile type (personal, business, service)
    /// </summary>
    public string ProfileType { get; set; }

    /// <summary>
    /// Whether this is the user's active profile
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Tags associated with profile
    /// </summary>
    public List<string> Tags { get; set; }

    /// <summary>
    /// Metadata as JSON string
    /// </summary>
    public string Metadata { get; set; }

    /// <summary>
    /// When profile was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When profile was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
```

### `ActiveProfileDto`
**Shared DTO** - Minimal profile info with active status

```csharp
// Location: Sivar.Os.Shared/DTOs/ActiveProfileDto.cs
public class ActiveProfileDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public bool IsActive { get; set; }
}
```

### `CreateProfileDto`
**Shared DTO** - Used to create a new profile

```csharp
// Location: Sivar.Os.Shared/DTOs/CreateProfileDto.cs
public class CreateProfileDto
{
    /// <summary>
    /// Display name for the profile
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Profile bio
    /// </summary>
    public string Bio { get; set; }

    /// <summary>
    /// Profile visibility
    /// </summary>
    public VisibilityLevel VisibilityLevel { get; set; }

    /// <summary>
    /// Tags for the profile
    /// </summary>
    public List<string> Tags { get; set; }

    /// <summary>
    /// Metadata as JSON
    /// </summary>
    public string Metadata { get; set; }
}
```

---

## Post DTOs

### `CreatePostDto`
**Shared DTO** - Client sends this to create a post

```csharp
// Location: Sivar.Os.Shared/DTOs/CreatePostDto.cs
public class CreatePostDto
{
    /// <summary>
    /// The post content/text
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Type of post (general, product, service, event, job)
    /// </summary>
    public PostType PostType { get; set; }

    /// <summary>
    /// Visibility level
    /// </summary>
    public VisibilityLevel Visibility { get; set; }

    /// <summary>
    /// Tags for searchability
    /// </summary>
    public List<string> Tags { get; set; }

    /// <summary>
    /// Post location if applicable
    /// </summary>
    public LocationDto Location { get; set; }

    /// <summary>
    /// Business metadata (JSON)
    /// </summary>
    public string BusinessMetadata { get; set; }

    /// <summary>
    /// Language of the post
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Schedule date if scheduling
    /// </summary>
    public DateTime? ScheduledDate { get; set; }
}
```

### `PostDto`
**Shared DTO** - Returned by API after post creation

```csharp
// Location: Sivar.Os.Shared/DTOs/PostDto.cs
public class PostDto
{
    /// <summary>
    /// Post's unique ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The profile that created this post
    /// </summary>
    public ProfileDto Profile { get; set; }

    /// <summary>
    /// The post content
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Type of post
    /// </summary>
    public PostType PostType { get; set; }

    /// <summary>
    /// Visibility level
    /// </summary>
    public VisibilityLevel Visibility { get; set; }

    /// <summary>
    /// Number of reactions
    /// </summary>
    public int ReactionCount { get; set; }

    /// <summary>
    /// Number of comments
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Number of shares
    /// </summary>
    public int ShareCount { get; set; }

    /// <summary>
    /// When post was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When post was updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Tags on the post
    /// </summary>
    public List<string> Tags { get; set; }

    /// <summary>
    /// Location if applicable
    /// </summary>
    public LocationDto Location { get; set; }

    /// <summary>
    /// Comments on this post
    /// </summary>
    public List<PostCommentDto> Comments { get; set; }

    /// <summary>
    /// Reactions on this post
    /// </summary>
    public List<ReactionDto> Reactions { get; set; }
}
```

### `PostCommentDto`
**Shared DTO** - Represents a comment on a post

```csharp
// Location: Sivar.Os.Shared/DTOs/PostCommentDto.cs
public class PostCommentDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public ProfileDto Profile { get; set; }
    public string Content { get; set; }
    public int ReactionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### `ReactionDto`
**Shared DTO** - Represents a reaction (like, emoji)

```csharp
// Location: Sivar.Os.Shared/DTOs/ReactionDto.cs
public class ReactionDto
{
    public Guid Id { get; set; }
    public ProfileDto Profile { get; set; }
    public string ReactionType { get; set; }  // "like", "love", "haha", etc
    public DateTime CreatedAt { get; set; }
}
```

---

## Enums

### `VisibilityLevel`
```csharp
// Location: Sivar.Os.Shared/Enums/VisibilityLevel.cs
public enum VisibilityLevel
{
    /// <summary>
    /// Visible to everyone
    /// </summary>
    Public = 0,

    /// <summary>
    /// Visible to followers only
    /// </summary>
    Followers = 1,

    /// <summary>
    /// Visible to specific people only
    /// </summary>
    Private = 2,

    /// <summary>
    /// Not visible (archived)
    /// </summary>
    Hidden = 3
}
```

### `PostType`
```csharp
// Location: Sivar.Os.Shared/Enums/PostType.cs
public enum PostType
{
    /// <summary>
    /// General post/update
    /// </summary>
    General = 0,

    /// <summary>
    /// Product listing or announcement
    /// </summary>
    Product = 1,

    /// <summary>
    /// Service offering
    /// </summary>
    Service = 2,

    /// <summary>
    /// Event announcement
    /// </summary>
    Event = 3,

    /// <summary>
    /// Job posting
    /// </summary>
    Job = 4
}
```

### `UserRole`
```csharp
// Location: Sivar.Os.Shared/Enums/UserRole.cs
public enum UserRole
{
    /// <summary>
    /// Regular registered user
    /// </summary>
    RegisteredUser = 0,

    /// <summary>
    /// Business owner/premium user
    /// </summary>
    BusinessOwner = 1,

    /// <summary>
    /// Platform administrator
    /// </summary>
    Administrator = 2,

    /// <summary>
    /// Moderator
    /// </summary>
    Moderator = 3
}
```

---

## Supporting DTOs

### `LocationDto`
```csharp
// Location: Sivar.Os.Shared/DTOs/LocationDto.cs
public class LocationDto
{
    public string City { get; set; }
    public string Country { get; set; }
    public string Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
```

### `UpdateUserPreferencesDto`
```csharp
// Location: Sivar.Os.Shared/DTOs/UpdateUserPreferencesDto.cs
public class UpdateUserPreferencesDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PreferredLanguage { get; set; }
    public bool NotificationsEnabled { get; set; }
}
```

### `UpdateProfileDto`
```csharp
// Location: Sivar.Os.Shared/DTOs/UpdateProfileDto.cs
public class UpdateProfileDto
{
    public string DisplayName { get; set; }
    public string Bio { get; set; }
    public string Avatar { get; set; }
    public VisibilityLevel VisibilityLevel { get; set; }
    public List<string> Tags { get; set; }
    public string Metadata { get; set; }
}
```

---

## Flow: How DTOs Are Used

### User Authentication Flow

```
┌─────────────────────────────────────────────┐
│ Client (Home.razor)                         │
│ Creates UserAuthenticationInfo:             │
│ {                                           │
│   Email: "john@example.com",                │
│   FirstName: "John",                        │
│   LastName: "Doe",                          │
│   Role: "RegisteredUser"                    │
│ }                                           │
└──────────────┬──────────────────────────────┘
               │ HTTP POST /authentication/authenticate/{keycloakId}
               ▼
┌─────────────────────────────────────────────┐
│ Server (AuthenticationController)           │
│ Calls UserAuthenticationService             │
│ Creates User entity (from UserDto)          │
│ Creates Profile entity (from ProfileDto)    │
└──────────────┬──────────────────────────────┘
               │ Returns UserAuthenticationResult:
               │ {
               │   IsSuccess: true,
               │   IsNewUser: true,
               │   User: UserDto { Id, Email, ... },
               │   ActiveProfile: ProfileDto { Id, DisplayName, ... }
               │ }
               ▼
┌─────────────────────────────────────────────┐
│ Client Receives Result                      │
│ Stores User ID and Profile ID               │
│ Calls LoadCurrentUserAsync()                │
└─────────────────────────────────────────────┘
```

### Post Creation Flow

```
┌─────────────────────────────────────────────┐
│ Client (Home.razor)                         │
│ Creates CreatePostDto:                      │
│ {                                           │
│   Content: "Check out my new product!",     │
│   PostType: PostType.Product,               │
│   Visibility: VisibilityLevel.Public,       │
│   Tags: ["ecommerce", "sale"]               │
│ }                                           │
└──────────────┬──────────────────────────────┘
               │ HTTP POST /api/posts
               ├─ Authorization header with JWT token
               ▼
┌─────────────────────────────────────────────┐
│ Server (PostsController)                    │
│ Extracts Keycloak ID from JWT               │
│ Calls PostService.CreatePostAsync()         │
│ Service:                                    │
│   1. Gets User from database (keycloakId)   │
│   2. Gets active Profile from database      │
│   3. Creates Post entity with ProfileId     │
│   4. Saves to database                      │
│   5. Maps to PostDto                        │
└──────────────┬──────────────────────────────┘
               │ Returns PostDto:
               │ {
               │   Id: guid,
               │   Profile: ProfileDto { DisplayName, Avatar, ... },
               │   Content: "Check out my new product!",
               │   CreatedAt: datetime,
               │   ReactionCount: 0,
               │   CommentCount: 0,
               │   ...
               │ }
               ▼
┌─────────────────────────────────────────────┐
│ Client Displays Post in Feed                │
│ Shows:                                      │
│ - Profile display name                      │
│ - Profile avatar                            │
│ - Post content                              │
│ - Creation timestamp                        │
└─────────────────────────────────────────────┘
```

---

## DTO Mapping Rules

### Controller to Service
```csharp
// Controller receives
CreatePostDto createPostDto

// Passes to service
PostService.CreatePostAsync(keycloakId, createPostDto)
// Note: keycloakId is separate, DTO is passed as-is
```

### Service to Controller (Return)
```csharp
// Service returns
PostDto postDto

// Controller passes back to client
return Ok(postDto);
```

### Service Database Layer
```csharp
// Service creates entity (NOT DTO)
var post = new Post
{
    Id = Guid.NewGuid(),
    ProfileId = profile.Id,
    Content = createPostDto.Content,
    ...
};

// Service maps entity to DTO before returning
var postDto = await MapToPostDtoAsync(post);
return postDto;
```

---

## Summary

**Key DTO Usage Pattern:**

1. **Client → Server:** Use specific Create/Update DTOs (e.g., `CreatePostDto`)
2. **Server (API Layer):** Accept DTOs, extract authentication, pass to service
3. **Server (Business Logic):** Work with entities internally, map DTOs at boundaries
4. **Server → Client:** Return read DTOs (e.g., `PostDto`, `ProfileDto`)
5. **Client:** Display DTO data in UI

**Never:**
- ❌ Return entities to clients (security risk, circular references)
- ❌ Use DTOs for internal service logic (use entities)
- ❌ Accept DTOs without validating authentication first
- ❌ Skip the mapping step between entities and DTOs
