# Sivar.Os URL Routing Standard

> **Version**: 1.0.0  
> **Created**: December 30, 2025  
> **Status**: 📋 PROPOSAL  
> **Author**: Development Team

---

## Table of Contents

1. [Overview](#overview)
2. [Current State Analysis](#current-state-analysis)
3. [Naming Conventions](#naming-conventions)
4. [Route Hierarchy](#route-hierarchy)
5. [Handle Routes](#1-handles-profile-usernames)
6. [Booking Routes](#2-bookings)
7. [Profile Routes](#3-profiles)
8. [User vs Profile](#4-users-vs-profiles)
9. [Blog Routes](#5-blogs)
10. [Public Content Routes](#6-public-content)
11. [Complete Route Table](#complete-route-table)
12. [Migration Checklist](#migration-checklist)

---

## Overview

This document establishes the URL routing standard for Sivar.Os across Web (Blazor) and Mobile (MAUI) platforms. The goal is to create consistent, predictable, and SEO-friendly URLs.

### Design Principles

| Principle | Description |
|-----------|-------------|
| **Consistency** | Same route patterns across all features |
| **Predictability** | Users can guess URLs based on patterns |
| **SEO-Friendly** | Clean, readable URLs for search engines |
| **Platform-Agnostic** | Works on Web and Mobile |
| **Localization-Ready** | English routes with localized UI content |
| **Clear Separation** | System routes use `/app/*`, profiles use `/{handle}` |

### The `/app` Prefix Strategy

Instead of reserving many words for system routes, we use a single prefix:

| Route Type | Pattern | Example |
|------------|---------|--------|
| System/App routes | `/app/*` | `/app/home`, `/app/bookings` |
| Profile handles | `/{handle}` | `/pizzahut`, `/maria_sv` |
| Public content | Direct paths | `/post/{id}`, `/blog/{slug}` |

**Benefits:**
- Only ONE reserved word: `app`
- Profiles get the cleanest URLs
- Clear developer organization
- No complex reserved word validation needed

---

## Current State Analysis

### Existing Routes (December 2025)

| Route | Page | Issues |
|-------|------|--------|
| `/` | Landing (anonymous) | ✅ Good |
| `/welcome` | Landing (alias) | ⚠️ Redundant alias |
| `/home` | Home (authenticated) | ⚠️ Should be `/app/home` |
| `/login` | Login | ⚠️ Should be `/app/login` |
| `/signup` | Sign Up | ⚠️ Should be `/app/signup` |
| `/search` | Search | ⚠️ Should be `/app/search` |
| `/explore` | Explore | ⚠️ Should be `/app/explore` |
| `/public` | Explore (alias) | ⚠️ Redundant alias |
| `/bookings` | Bookings | ⚠️ Should be `/app/bookings` |
| `/reservaciones` | Bookings (Spanish) | ⚠️ Localized route - inconsistent |
| `/my-schedule` | My Schedule | ⚠️ Should be `/schedule` |
| `/mi-agenda` | My Schedule (Spanish) | ⚠️ Localized route - inconsistent |
| `/profile/settings` | Profile Settings | ⚠️ Should be `/app/profile/settings` |
| `/post/{PostId:guid}` | Post Detail | ✅ Good |
| `/blog/edit/{PostId:guid}` | Blog Edit | ⚠️ Should be `/edit/blog/{id}` |
| `/{Identifier}` | Profile Page | ⚠️ Catch-all conflicts |
| `/Error` | Error | ⚠️ PascalCase - should be lowercase |
| `/counter`, `/weather` | Demo pages | ⚠️ Should be removed |

### Issues Identified

1. **Inconsistent casing**: `/Error` vs `/login`
2. **Redundant aliases**: `/welcome`, `/public`
3. **Localized routes**: `/reservaciones`, `/mi-agenda` - should use English-only
4. **Catch-all conflict**: `/{Identifier}` can match feature routes
5. **Inconsistent hierarchy**: `/blog/edit/{id}` vs `/edit/blog/{id}`

---

## Naming Conventions

### Route Naming Rules

| Convention | Rule | ✅ Correct | ❌ Incorrect |
|------------|------|-----------|-------------|
| **Case** | Always lowercase | `/profile/settings` | `/Profile/Settings` |
| **Word Separator** | Use hyphens (kebab-case) | `/my-schedule` | `/mySchedule`, `/my_schedule` |
| **Language** | English-only routes | `/bookings` | `/reservaciones` |
| **Hierarchy** | Use slashes for nesting | `/profile/settings` | `/profile-settings` |
| **Verbs** | Avoid in routes | `/posts` | `/view-posts` |
| **Pluralization** | Plural for collections | `/bookings` | `/booking` |
| **IDs** | Use path parameters | `/post/{id}` | `/post?id={id}` |

### Reserved Route Prefixes

With the `/app` prefix strategy, only these are reserved as profile handles:

```
/app      → All system/application routes
/api      → REST API endpoints  
/auth     → Authentication endpoints
/post     → Individual posts
/blog     → Individual blogs
```

**Note**: Words like `home`, `bookings`, `schedule` are NO LONGER reserved because they live under `/app/*`.

---

## Route Hierarchy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           SIVAR.OS URL STRUCTURE                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  🌐 PUBLIC (No Auth Required)                                               │
│  ├── /                          Landing page                                │
│  ├── /app/explore               Discover public content                     │
│  ├── /app/login                 Login                                       │
│  ├── /app/signup                Registration                                │
│  ├── /post/{id}                 View any public post                        │
│  ├── /blog/{slug}               View any public blog                        │
│  └── /{handle}                  View any public profile                     │
│                                                                              │
│  🔐 AUTHENTICATED (/app/*)                                                  │
│  ├── /app/home                  Personalized feed                           │
│  ├── /app/search                Search                                      │
│  ├── /app/notifications         Notifications                               │
│  └── /app/messages              Direct messages                             │
│                                                                              │
│  👤 PROFILE MANAGEMENT (/app/profile/*)                                      │
│  ├── /app/profile               → Redirects to /{myhandle}                  │
│  ├── /app/profile/settings      General settings                            │
│  ├── /app/profile/edit          Edit bio, avatar, etc.                      │
│  └── /app/profile/security      Password, 2FA                               │
│                                                                              │
│  📅 BOOKINGS (/app/bookings/*)                                               │
│  ├── /app/bookings              My bookings list                            │
│  ├── /app/bookings/{id}         Booking details                             │
│  └── /app/bookings/new          Create booking (via business profile)       │
│                                                                              │
│  🏢 BUSINESS SCHEDULE (/app/schedule/*)                                      │
│  ├── /app/schedule              My business schedule                        │
│  ├── /app/schedule/settings     Availability settings                       │
│  └── /app/schedule/bookings     Incoming booking requests                   │
│                                                                              │
│  📝 CONTENT CREATION (/app/create/*)                                         │
│  ├── /app/create/post           New post                                    │
│  ├── /app/create/blog           New blog                                    │
│  └── /app/create/story          New story (future)                          │
│                                                                              │
│  ✏️ CONTENT EDITING (/app/edit/*)                                            │
│  ├── /app/edit/post/{id}        Edit post                                   │
│  └── /app/edit/blog/{id}        Edit blog                                   │
│                                                                              │
│  � AI CHAT (/app/chat/*)                                                    │
│  ├── /app/chat                  Chat home (latest or new conversation)      │
│  └── /app/chat/{id}             Specific conversation by ID                 │
│                                                                              │
│  �👤 PROFILE HANDLES (CATCH-ALL - MUST BE LAST)                              │
│  └── /{handle}                  Public profile view                         │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 1. Handles (Profile Usernames)

Handles are unique identifiers for profiles (users, businesses, organizations).

### Route Pattern

```
/{handle}                  → Profile home (posts tab)
/{handle}/posts            → Posts tab
/{handle}/blogs            → Blogs tab (if applicable)
/{handle}/about            → About/Info tab
/{handle}/reviews          → Reviews tab (businesses)
/{handle}/book             → Booking page (businesses)
```

### Implementation

```razor
// ProfilePage.razor
@page "/{Identifier}"
@page "/{Identifier}/{Tab}"

@code {
    [Parameter] public string Identifier { get; set; }
    [Parameter] public string? Tab { get; set; }
}
```

### Why `/app` Prefix for System Routes?

| Approach | Pros | Cons |
|----------|------|------|
| `/{handle}` + reserved words | Clean profile URLs | Many reserved words, complex validation |
| `/u/{handle}` or `/p/{handle}` | Clear prefix | Extra characters for profiles |
| **`/app/*` for system** | Only 1 reserved word, clean profiles | System URLs slightly longer |

**Decision**: Use `/app/*` for all system routes. This means:
- Profiles get the cleanest URLs: `/pizzahut`, `/maria_sv`
- Only ONE word is reserved: `app`
- Clear separation for developers
- No complex reserved word validation needed

### Minimal Reserved Words

With `/app` prefix, only these prefixes are reserved:

```json
{
  "Handles": {
    "ReservedWords": ["app", "api", "auth", "post", "blog"],
    "MinLength": 3,
    "MaxLength": 30,
    "AllowedPattern": "^[a-zA-Z0-9_]+$"
  }
}
```

**Note**: This is a minimal list! Words like `home`, `bookings`, `profile` are NOT reserved because they live under `/app/*`.

### Handle Validation Service

```csharp
// Configuration/HandleSettings.cs
public class HandleSettings
{
    public List<string> ReservedWords { get; set; } = new();
    public int MinLength { get; set; } = 3;
    public int MaxLength { get; set; } = 30;
    public string AllowedPattern { get; set; } = "^[a-zA-Z0-9_]+$";
}

// Services/HandleValidationService.cs
public class HandleValidationService : IHandleValidationService
{
    private readonly HandleSettings _settings;
    private readonly HashSet<string> _reservedWords;
    private readonly Regex _patternRegex;

    public HandleValidationService(IOptions<HandleSettings> options)
    {
        _settings = options.Value;
        _reservedWords = new HashSet<string>(
            _settings.ReservedWords.Select(w => w.ToLowerInvariant())
        );
        _patternRegex = new Regex(_settings.AllowedPattern);
    }

    public bool IsReservedWord(string handle)
        => _reservedWords.Contains(handle.ToLowerInvariant());

    public HandleValidationResult Validate(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return HandleValidationResult.Error("Handle is required");

        if (handle.Length < _settings.MinLength)
            return HandleValidationResult.Error($"Handle must be at least {_settings.MinLength} characters");

        if (handle.Length > _settings.MaxLength)
            return HandleValidationResult.Error($"Handle must be at most {_settings.MaxLength} characters");

        if (!_patternRegex.IsMatch(handle))
            return HandleValidationResult.Error("Handle can only contain letters, numbers, and underscores");

        if (IsReservedWord(handle))
            return HandleValidationResult.Error("This handle is reserved and cannot be used");

        return HandleValidationResult.Success();
    }
}
```

---

## 2. Bookings

Two perspectives: **Consumer** (making bookings) and **Business** (receiving bookings).

### Consumer Routes (`/app/bookings/*`)

```
/app/bookings                  → List of my bookings
/app/bookings/{id}             → Booking detail
/app/bookings/{id}/cancel      → Cancel booking flow
/app/bookings/{id}/reschedule  → Reschedule flow
```

### Business Routes (`/app/schedule/*`)

```
/app/schedule                  → My business calendar/schedule
/app/schedule/settings         → Availability, hours, services
/app/schedule/bookings         → Incoming booking requests
/app/schedule/bookings/{id}    → Booking request detail
```

### Booking Flow

```
User discovers business:    /businesshandle
User views booking page:    /businesshandle/book
User selects service:       /businesshandle/book?service=haircut
User selects time:          /businesshandle/book?service=haircut&time=...
Booking confirmed:          /app/bookings/{newId}
```

### Route Table

| Route | Purpose | Auth | Controller |
|-------|---------|------|------------|
| `/app/bookings` | My bookings list | ✅ Required | BookingsPage.razor |
| `/app/bookings/{id}` | Booking detail | ✅ Required | BookingDetail.razor |
| `/app/schedule` | Business calendar | ✅ Required + Business | SchedulePage.razor |
| `/app/schedule/settings` | Business availability | ✅ Required + Business | ScheduleSettings.razor |
| `/{handle}/book` | Public booking form | Optional | ProfileBooking.razor |

---

## 3. Profiles

Distinction between **viewing** a profile and **managing** your own profile.

### Profile Viewing (`/{handle}/*`)

```
/{handle}                  → Profile home (default: posts)
/{handle}/posts            → Posts tab
/{handle}/blogs            → Blogs tab
/{handle}/about            → About/Bio/Info
/{handle}/reviews          → Reviews (businesses)
/{handle}/menu             → Menu (restaurants)
/{handle}/services         → Services list (businesses)
/{handle}/book             → Booking form
/{handle}/contact          → Contact form
```

### Profile Management (`/app/profile/*`)

```
/app/profile               → Redirects to /{myhandle}
/app/profile/settings      → Settings (language, notifications)
/app/profile/edit          → Edit profile (bio, avatar, cover)
/app/profile/security      → Security (password, 2FA, sessions)
/app/profile/handles       → Manage owned handles
/app/profile/switch        → Switch between profiles
```

### Access Control Matrix

| Route | Owner | Visitor | Anonymous |
|-------|-------|---------|-----------|
| `/{handle}` | ✅ Full | ✅ Public only | ✅ If public |
| `/{handle}/about` | ✅ Full | ✅ Public only | ✅ If public |
| `/app/profile/settings` | ✅ | ❌ | ❌ |
| `/app/profile/edit` | ✅ | ❌ | ❌ |

---

## 4. Users vs Profiles

**Important Distinction**: A User can have multiple Profiles.

### User Account Routes (`/app/account/*`)

User-level settings (one per person):

```
/app/account               → Account overview
/app/account/email         → Change email
/app/account/password      → Change password
/app/account/security      → 2FA, sessions, security keys
/app/account/data          → Download my data, GDPR
/app/account/delete        → Delete account
/app/account/billing       → Subscription, payment methods
```

### Profile Routes (`/app/profile/*`)

Profile-level settings (per handle/business):

```
/app/profile               → Current profile overview
/app/profile/settings      → Profile preferences
/app/profile/edit          → Edit bio, avatar
/app/profile/handles       → Manage handles
/app/profile/switch        → Switch between profiles
```

### Comparison Table

| Aspect | `/app/account/*` | `/app/profile/*` |
|--------|------------------|------------------|
| Scope | User (person) | Profile (handle) |
| Quantity | One per user | Multiple per user |
| Auth | Keycloak/SSO | Application DB |
| Examples | Email, Password, 2FA | Bio, Avatar, Handle |
| Deletion | Deletes everything | Deletes one profile |

---

## 5. Blogs

### Blog Viewing Routes

```
/blog/{slug}               → Read blog by slug (SEO-friendly)
/blog/preview/{id}         → Preview draft (auth required)
```

### Blog Management Routes

```
/app/create/blog           → Create new blog post
/app/edit/blog/{id}        → Edit existing blog
/app/drafts                → My drafts (posts + blogs)
/app/drafts/blogs          → My blog drafts only
```

### Blog Discovery Routes

```
/app/explore/blogs             → Discover blogs
/app/explore/blogs?category=tech → Filter by category
/{handle}/blogs            → Blogs by specific author
```

### Slug Generation

```csharp
// URL: /blog/how-to-build-blazor-apps
// Slug: how-to-build-blazor-apps

public static string GenerateSlug(string title)
{
    var slug = title.ToLowerInvariant()
        .Replace(" ", "-")
        .Replace("á", "a").Replace("é", "e").Replace("í", "i")
        .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");
    
    // Remove special characters
    slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
    
    // Remove duplicate hyphens
    slug = Regex.Replace(slug, @"-+", "-");
    
    return slug.Trim('-');
}
```

### Complete Blog Routes

| Route | Purpose | Auth |
|-------|---------|------|
| `/blog/{slug}` | Read published blog | Public |
| `/blog/preview/{id}` | Preview draft | ✅ Owner only |
| `/app/create/blog` | Create new | ✅ Required |
| `/app/edit/blog/{id}` | Edit existing | ✅ Owner only |
| `/app/drafts` | All drafts | ✅ Required |
| `/app/explore/blogs` | Discover | Public |
| `/{handle}/blogs` | Author's blogs | Public |

---

## 6. Public Content

### Explore Routes (`/app/explore/*`)

```
/app/explore               → Main discovery page
/app/explore/posts         → Trending posts
/app/explore/blogs         → Featured blogs
/app/explore/businesses    → Discover businesses
/app/explore/people        → Suggested people to follow
/app/explore/locations     → Browse by location
/app/explore/categories    → Browse by category
```

### Search Routes (`/app/search/*`)

```
/app/search                    → Universal search
/app/search?q={query}          → Search with query
/app/search?q={query}&type=posts    → Filter by type
/app/search?q={query}&type=people   → Filter to people
/app/search?q={query}&location=sv   → Filter by location
```

### Content Type Routes

```
/post/{id}                 → Single post (UUID)
/blog/{slug}               → Single blog (SEO slug)
```

### Query Parameter Standards

| Parameter | Purpose | Values |
|-----------|---------|--------|
| `q` | Search query | Any string |
| `type` | Content type filter | `posts`, `blogs`, `people`, `businesses` |
| `category` | Category filter | Category slug |
| `location` | Location filter | Location code |
| `sort` | Sort order | `recent`, `popular`, `relevant` |
| `page` | Pagination | Integer |
| `limit` | Results per page | Integer (max 50) |

---

## Complete Route Table

### Public Routes (No Auth)

| Route | Component | Description |
|-------|-----------|-------------|
| `/` | Landing.razor | Anonymous landing page |
| `/app/login` | Login.razor | Login form |
| `/app/signup` | Signup.razor | Registration form |
| `/app/explore` | Explore.razor | Public discovery |
| `/app/explore/posts` | Explore.razor | Trending posts |
| `/app/explore/blogs` | Explore.razor | Featured blogs |
| `/app/explore/businesses` | Explore.razor | Discover businesses |
| `/app/search` | Search.razor | Universal search |
| `/post/{id}` | PostDetail.razor | View post |
| `/blog/{slug}` | BlogDetail.razor | View blog |
| `/{handle}` | ProfilePage.razor | View profile |
| `/{handle}/posts` | ProfilePage.razor | Profile posts tab |
| `/{handle}/blogs` | ProfilePage.razor | Profile blogs tab |
| `/{handle}/about` | ProfilePage.razor | Profile about tab |
| `/{handle}/book` | ProfilePage.razor | Profile booking tab |
| `/app/error` | Error.razor | Error page |

### Authenticated Routes (Auth Required)

| Route | Component | Description |
|-------|-----------|-------------|
| `/app/home` | Home.razor | Personalized feed |
| `/app/notifications` | Notifications.razor | Notifications |
| `/app/messages` | Messages.razor | Direct messages |
| `/app/bookings` | Bookings.razor | My bookings |
| `/app/bookings/{id}` | BookingDetail.razor | Booking detail |
| `/app/schedule` | Schedule.razor | Business calendar |
| `/app/schedule/settings` | ScheduleSettings.razor | Availability |
| `/app/create/post` | CreatePost.razor | New post |
| `/app/create/blog` | CreateBlog.razor | New blog |
| `/app/edit/post/{id}` | EditPost.razor | Edit post |
| `/app/edit/blog/{id}` | EditBlog.razor | Edit blog |
| `/app/drafts` | Drafts.razor | My drafts |

### Profile Management Routes (Auth Required)

| Route | Component | Description |
|-------|-----------|-------------|
| `/app/profile` | Profile.razor | Redirect to /{handle} |
| `/app/profile/settings` | ProfileSettings.razor | Profile preferences |
| `/app/profile/edit` | ProfileEdit.razor | Edit bio, avatar |
| `/app/profile/security` | ProfileSecurity.razor | Password, 2FA |
| `/app/profile/handles` | ProfileHandles.razor | Manage handles |

### Account Routes (Auth Required)

| Route | Component | Description |
|-------|-----------|-------------|
| `/app/account` | Account.razor | Account overview |
| `/app/account/email` | AccountEmail.razor | Change email |
| `/app/account/password` | AccountPassword.razor | Change password |
| `/app/account/security` | AccountSecurity.razor | 2FA, sessions |
| `/app/account/data` | AccountData.razor | GDPR, export |
| `/app/account/billing` | AccountBilling.razor | Subscription |

---

## Migration Checklist

### Phase 1: Remove Redundant Routes

- [ ] Remove `/welcome` alias from Landing.razor
- [ ] Remove `/public` alias from Explore.razor
- [ ] Remove `/reservaciones` alias from Bookings.razor
- [ ] Remove `/mi-agenda` alias from MySchedule.razor
- [ ] Remove demo pages `/counter`, `/weather`

### Phase 2: Add `/app` Prefix to System Routes

- [ ] Update `/home` to `/app/home` in Home.razor
- [ ] Update `/login` to `/app/login` in Login.razor
- [ ] Update `/signup` to `/app/signup` in Signup.razor
- [ ] Update `/search` to `/app/search` in Search.razor
- [ ] Update `/explore` to `/app/explore` in Explore.razor
- [ ] Update `/bookings` to `/app/bookings` in Bookings.razor
- [ ] Update `/my-schedule` to `/app/schedule` in Schedule.razor
- [ ] Update `/profile/settings` to `/app/profile/settings`
- [ ] Update `/Error` to `/app/error` (lowercase)
- [ ] Update `CoreNavigationItems.cs` to use `/app/*` routes

### Phase 3: Implement Handle Pattern

- [ ] Keep ProfilePage.razor route as `/{Identifier}` (current)
- [ ] Add tab support: `/{Identifier}/{Tab}`
- [ ] Create `HandleSettings` configuration class (minimal reserved words)
- [ ] Create `IHandleValidationService` and implementation
- [ ] Add minimal reserved words to `appsettings.json`: `["app", "api", "auth", "post", "blog"]`
- [ ] Register services in `Program.cs`
- [ ] Add handle validation to profile creation/update
- [ ] Update all profile links throughout the app

### Phase 4: Standardize Content Routes

- [ ] Change `/blog/edit/{id}` to `/app/edit/blog/{id}`
- [ ] Create `/app/create/blog` route (if not exists)
- [ ] Create `/app/drafts` page
- [ ] Create `/blog/{slug}` for SEO-friendly blog URLs

### Phase 5: Update Navigation

Update `CoreNavigationItems.cs`:

```csharp
public static List<NavigationItem> GetItems() => new()
{
    new() { Title = "Home", Icon = Icons.Material.Filled.Home, Href = "/app/home" },
    new() { Title = "Search", Icon = Icons.Material.Filled.Search, Href = "/app/search" },
    new() { Title = "Schedule", Icon = Icons.Material.Filled.CalendarMonth, Href = "/app/schedule" },
    new() { Title = "Bookings", Icon = Icons.Material.Filled.BookOnline, Href = "/app/bookings" },
    new() { Title = "Explore", Icon = Icons.Material.Filled.Explore, Href = "/app/explore" },
    new() { Title = "Profile", Icon = Icons.Material.Filled.Person, Href = "/app/profile" }
};
```

### Phase 6: Testing

- [ ] Test all routes in development
- [ ] Verify navigation items work
- [ ] Test profile handle routing
- [ ] Test deep links
- [ ] Test mobile (MAUI) navigation

---

## Implementation Notes

### Route Order Matters

In Blazor, routes are matched in order. The catch-all `/{handle}` must be defined LAST to avoid conflicts:

```razor
// ✅ Correct order in _Imports.razor or Router
// 1. /app/* routes first (they have the /app prefix, so no conflicts)
// 2. Specific public routes: /post/{id}, /blog/{slug}
// 3. Catch-all last: /{Identifier}
```

### Handle Conflict Prevention

With the `/app` prefix strategy, conflict prevention is minimal:

```csharp
public async Task<bool> CanClaimHandle(string handle)
{
    // Only 5 reserved words!
    var reserved = new[] { "app", "api", "auth", "post", "blog" };
    if (reserved.Contains(handle.ToLower()))
        return false;
    
    // Check if already taken
    var existing = await _profileRepo.GetByHandleAsync(handle);
    return existing == null;
}
```

### Backward Compatibility

During migration, support old routes temporarily:

```razor
// Temporary - remove after migration period
@page "/home"           // Old route
@page "/app/home"       // New standard route

@page "/mi-agenda"      // Old Spanish route  
@page "/app/schedule"   // New standard route
```

---

## Related Documents

- [DEVELOPMENT_RULES.md](DEVELOPMENT_RULES.md) - Development standards
- [FRAMEWORK.md](Sivar.Os.Client/FRAMEWORK.md) - Client framework documentation
- [BOOKING_UI_PLAN.md](BOOKING_UI_PLAN.md) - Booking system design

---

**End of Document**

