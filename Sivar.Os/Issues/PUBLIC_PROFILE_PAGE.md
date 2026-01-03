# Public Profile Page Implementation

> **Status**: � IN PROGRESS  
> **Priority**: Medium  
> **Created**: January 3, 2026  
> **Related**: URL_ROUTING_STANDARD.md, BLOG_SYSTEM_IMPLEMENTATION.md

---

## Overview

Create a public-facing profile page at `/app/explore/{handle}` for anonymous users. The authenticated profile at `/{handle}` remains unchanged.

---

## Design Decision

| Context | Route | Auth |
|---------|-------|------|
| Public/Anonymous | `/app/explore/{handle}` | `[AllowAnonymous]` |
| Authenticated | `/{handle}` | `[Authorize]` |

**Rationale**: Clear separation avoids conditional complexity and security edge cases.

---

## Tasks

### Phase 1: API Endpoint (Backend) ✅ ALREADY DONE

- [x] **1.1** Add `GetPublicProfileByHandle` endpoint to `PublicController`
  - Route: `GET /api/public/profiles/handle/{handle}`
  - Returns: `ProfileDto` (limited to public fields)
  - Attribute: `[AllowAnonymous]`

- [x] **1.2** `ProfileDto` already exists and is reused

- [x] **1.3** Add `GetPublicPostsByProfile` endpoint to `PublicController`
  - Route: `GET /api/public/profiles/{profileId}/posts`
  - Returns: Published posts only (no drafts)
  - Attribute: `[AllowAnonymous]`

### Phase 2: Client Interface (Shared) ✅ ALREADY DONE

- [x] **2.1** `GetPublicProfileByHandleAsync(string handle)` exists in `IPublicClient`
- [x] **2.2** `GetPublicPostsByProfileAsync(Guid profileId)` exists in `IPublicClient`
- [x] **2.3** Implemented in `PublicClient.cs` (both server and client versions)

### Phase 3: Public Profile Page (Client) ✅ DONE

- [x] **3.1** Create `PublicProfile.razor` at `/app/explore/{Handle}`
  - Route: `@page "/app/explore/{Handle}"`
  - Attribute: `[AllowAnonymous]`
  - Shows: Avatar, name, bio, post count, views, posts feed

- [x] **3.2** Add `JoinCta` banner for anonymous users
- [x] **3.3** Link author clicks in `Explore.razor` to `/app/explore/{handle}`

### Phase 4: Navigation & Links ✅ DONE

- [x] **4.1** Update `PublicPostCard` author links → `/app/explore/{handle}`
- [x] **4.2** Update `BlogCard` author links → `/app/explore/{handle}` (via handler)
- [x] **4.3** Add "Follow" and "Message" buttons that prompt login

---

## Out of Scope (Future)

- Tab navigation (`/app/explore/{handle}/blogs`)
- Public profile SEO meta tags
- Profile sharing/embed functionality

---

## Files to Create/Modify

| File | Action |
|------|--------|
| `Sivar.Os/Controllers/ProfilesController.cs` | Add public endpoint |
| `Sivar.Os.Shared/DTOs/PublicProfileDto.cs` | Create |
| `Sivar.Os.Shared/Clients/IProfilesClient.cs` | Add method |
| `Sivar.Os.Client/Clients/ProfilesClient.cs` | Implement |
| `Sivar.Os.Client/Pages/Explore/PublicProfile.razor` | Create |
| `Sivar.Os.Client/Components/Feed/PublicPostCard.razor` | Update links |

---

## Acceptance Criteria

1. Anonymous user can view `/app/explore/{handle}` without login
2. Public profile shows: avatar, name, bio, public posts
3. Public profile does NOT show: drafts, private posts, edit buttons
4. Clicking "Follow" or "Message" prompts login
5. Author links in Explore feed navigate to public profile
