# Sivar.Os Framework Architecture

> **Version**: 1.0.0  
> **Last Updated**: December 30, 2025  
> **Status**: ✅ **ACTIVE** - Framework navigation enabled in production  
> **Platforms**: Web (Blazor Server) + Mobile (MAUI Blazor Hybrid)

---

## Quick Start

The framework navigation is **enabled by default**. To toggle:

```json
// appsettings.json
{
  "FrameworkFeatures": {
    "UseFrameworkNavigation": true  // Set to false to use legacy NavMenu
  }
}
```

---

## Recent Updates (December 30, 2025)

### Authentication State Fix
- **Issue**: Menu items not showing after login/logout
- **Solution**: `MainLayout` now uses `AuthenticationStateProvider.GetAuthenticationStateAsync()` to verify auth state before loading profiles
- **Method**: New `LoadNavigationProfileAsync()` method ensures proper state synchronization

### FrameworkNavMenu Enhancements
- ✅ ProfileSwitcher integrated at top of menu
- ✅ Theme toggle (dark/light mode) with MudSwitch
- ✅ Logout button at bottom for authenticated users
- ✅ Guest branding and Login/SignUp buttons for anonymous users
- ✅ Localization keys added (en-US, es-ES)

### Property Mapping Notes
| Source | Property | Notes |
|--------|----------|-------|
| `ProfileDto` | `Avatar` | Base avatar path |
| `ActiveProfileDto` | `AvatarUrl` | Full URL with SAS token |
| `IProfilesClient` | `GetAllMyProfilesAsync()` | Not `GetMyProfilesAsync()` |

---

## Table of Contents

1. [Overview](#overview)
2. [Project Structure](#project-structure)
3. [Platform Architecture](#platform-architecture)
4. [Existing Patterns](#existing-patterns)
5. [Framework Patterns (Proposed)](#framework-patterns-proposed)
   - [Navigation Pattern](#1-navigation-pattern)
   - [Menu Pattern](#2-menu-pattern)
   - [Action Pattern](#3-action-pattern)
   - [Component Pattern](#4-component-pattern)
6. [Implementation Roadmap](#implementation-roadmap)

---

## Overview

Sivar.Os is a social media platform for El Salvador with two deployment targets:

| Platform | Project | Technology | Status |
|----------|---------|------------|--------|
| **Web** | `Sivar.Os.Client` | Blazor WebAssembly | ✅ Active |
| **Mobile** | `Sivar.Os.Maui` | MAUI Blazor Hybrid | 🔧 In Development |
| **Server** | `Sivar.Os` | ASP.NET Core | ✅ Active |
| **Shared** | `Sivar.Os.Shared` | .NET Class Library | ✅ Active |

### Design Goals

1. **Code Reuse**: Maximum component sharing between Web and Mobile
2. **Consistency**: Unified patterns for navigation, menus, and actions
3. **Extensibility**: Easy to add new features without breaking existing code
4. **Maintainability**: Clear separation of concerns and single source of truth

---

## Project Structure

```
Sivar.Os/
├── Sivar.Os/                    # ASP.NET Core Server
│   ├── Controllers/             # REST API endpoints
│   ├── Services/                # Business logic (server-side)
│   ├── Agents/                  # AI agents (chat, search)
│   ├── Hubs/                    # SignalR hubs
│   └── Components/              # Server-side Blazor components
│
├── Sivar.Os.Client/             # Blazor WebAssembly (Web)
│   ├── Pages/                   # Route pages (@page)
│   ├── Layout/                  # MainLayout, NavMenu
│   ├── Components/              # Reusable UI components
│   │   ├── Feed/                # Post, Blog, Comments
│   │   ├── Profile/             # Profile cards, settings
│   │   ├── AIChat/              # AI chat interface
│   │   ├── Booking/             # Resource booking
│   │   ├── Shared/              # Common components
│   │   └── ...
│   ├── Services/                # Client-side services
│   └── Clients/                 # API client implementations
│
├── Sivar.Os.Maui/               # MAUI Blazor Hybrid (Mobile)
│   ├── Components/
│   │   ├── Pages/               # Mobile-specific pages
│   │   ├── MainLayout.razor     # Mobile layout
│   │   └── NavMenu.razor        # Mobile navigation
│   └── Platforms/               # Platform-specific code
│
├── Sivar.Os.Shared/             # Shared Library
│   ├── Entities/                # Database entities
│   ├── DTOs/                    # Data transfer objects
│   ├── Enums/                   # Shared enumerations
│   ├── Services/                # Service interfaces
│   ├── Repositories/            # Repository interfaces
│   └── Clients/                 # API client interfaces
│
└── Xaf.Sivar.Os/                # DevExpress XAF Admin Backend
```

---

## Platform Architecture

### Shared Code Strategy

```
┌─────────────────────────────────────────────────────────────────┐
│                    Sivar.Os.Shared                              │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │
│  │  Entities   │ │    DTOs     │ │  Interfaces │ │   Enums   │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│ Sivar.Os.Client│    │ Sivar.Os.Maui │    │   Sivar.Os    │
│   (Web WASM)  │    │   (Mobile)    │    │   (Server)    │
├───────────────┤    ├───────────────┤    ├───────────────┤
│ Components/   │◄───│ Components/   │    │ Services/     │
│ Services/     │    │ (Can reuse)   │    │ Controllers/  │
│ Clients/      │    │ Services/     │    │ Repositories/ │
└───────────────┘    └───────────────┘    └───────────────┘
```

### Component Sharing Between Platforms

| Component Category | Web | Mobile | Sharing Strategy |
|--------------------|-----|--------|------------------|
| **Feed Components** | ✅ | ✅ | Reference from Sivar.Os.Client |
| **Profile Components** | ✅ | ✅ | Reference from Sivar.Os.Client |
| **AI Chat** | ✅ | ✅ | Reference from Sivar.Os.Client |
| **Layout** | ❌ | ❌ | Platform-specific |
| **Navigation** | ❌ | ❌ | Platform-specific (needs framework) |

---

## Existing Patterns

### ✅ Already Implemented

#### 1. Repository Pattern
```
Sivar.Os.Shared/Repositories/    → Interfaces (IPostRepository)
Sivar.Os.Data/Repositories/      → Implementations (PostRepository)
```

#### 2. Service Layer Pattern
```
Sivar.Os.Shared/Services/        → Interfaces (IPostService)
Sivar.Os/Services/               → Server implementations
Sivar.Os.Client/Services/        → Client implementations
```

#### 3. Client Facade Pattern (ISivarClient)
```csharp
ISivarClient
├── Auth         (IAuthClient)
├── Chat         (ISivarChatClient)
├── Posts        (IPostsClient)
├── Profiles     (IProfilesClient)
├── Comments     (ICommentsClient)
├── Reactions    (IReactionsClient)
├── Followers    (IFollowersClient)
├── Notifications (INotificationsClient)
├── Files        (IFilesClient)
├── Users        (IUsersClient)
├── Activities   (IActivitiesClient)
└── Public       (IPublicClient)
```

#### 4. Entity Base Pattern
```csharp
public abstract class BaseEntity
{
    public virtual Guid Id { get; set; }
    public virtual DateTime CreatedAt { get; set; }
    public virtual DateTime UpdatedAt { get; set; }
    public virtual bool IsDeleted { get; set; }
    public virtual DateTime? DeletedAt { get; set; }
}
```

---

## Framework Patterns (Proposed)

### 1. Navigation Pattern

**Problem**: Navigation is hardcoded differently in Web and Mobile:

```razor
<!-- Web: Sivar.Os.Client/Layout/NavMenu.razor -->
<MudNavLink Href="home" Icon="@Icons.Material.Filled.Home">Home</MudNavLink>
<MudNavLink Href="search" Icon="@Icons.Material.Filled.Search">Search</MudNavLink>
@if (_activeProfile?.ProfileType?.Name == "Business")
{
    <MudNavLink Href="my-schedule" ...>My Schedule</MudNavLink>
}

<!-- Mobile: Sivar.Os.Maui/Components/NavMenu.razor -->
<MudNavLink Href="/" Icon="@Icons.Material.Filled.Home">Home</MudNavLink>
<MudNavLink Href="/explore" Icon="@Icons.Material.Filled.Explore">Explore</MudNavLink>
```

**Solution**: Navigation Registry Pattern

```csharp
// Sivar.Os.Shared/Framework/Navigation/INavigationItem.cs
public interface INavigationItem
{
    string Id { get; }
    string Title { get; }
    string TitleKey { get; }          // Localization key
    string Icon { get; }
    string Route { get; }
    int Order { get; }
    bool RequiresAuth { get; }
    string[]? RequiredRoles { get; }
    string[]? RequiredProfileTypes { get; }
    Func<NavigationContext, bool>? IsVisible { get; }
    Func<NavigationContext, bool>? IsEnabled { get; }
}

// Sivar.Os.Shared/Framework/Navigation/NavigationContext.cs
public class NavigationContext
{
    public bool IsAuthenticated { get; set; }
    public ProfileDto? ActiveProfile { get; set; }
    public string? CurrentRoute { get; set; }
    public PlatformType Platform { get; set; }  // Web, Mobile, Desktop
}

// Sivar.Os.Shared/Framework/Navigation/INavigationRegistry.cs
public interface INavigationRegistry
{
    IEnumerable<INavigationItem> GetItems(NavigationContext context);
    INavigationItem? GetItem(string id);
    void Register(INavigationItem item);
}
```

**Benefits**:
- Single source of truth for navigation
- Role/profile-based visibility built-in
- Platform-aware (Web vs Mobile)
- Localization-ready

---

### 2. Menu Pattern

**Problem**: Context menus (post actions, profile actions) are inline in components.

**Solution**: Contextual Menu Builder

```csharp
// Sivar.Os.Shared/Framework/Menu/IMenuItem.cs
public interface IMenuItem
{
    string Id { get; }
    string Title { get; }
    string TitleKey { get; }
    string? Icon { get; }
    MenuItemType Type { get; }        // Action, Separator, Submenu
    int Order { get; }
    bool IsDangerous { get; }         // Red color for destructive actions
    Func<MenuContext, bool>? IsVisible { get; }
    Func<MenuContext, bool>? IsEnabled { get; }
}

public enum MenuItemType { Action, Separator, Submenu, Toggle }

// Sivar.Os.Shared/Framework/Menu/MenuContext.cs
public class MenuContext
{
    public object? Target { get; set; }          // The entity (Post, Profile, etc.)
    public Guid? TargetId { get; set; }
    public ProfileDto? CurrentProfile { get; set; }
    public bool IsOwner { get; set; }
    public PlatformType Platform { get; set; }
}

// Sivar.Os.Shared/Framework/Menu/IMenuProvider.cs
public interface IMenuProvider
{
    string MenuId { get; }            // "post-actions", "profile-actions"
    IEnumerable<IMenuItem> GetItems(MenuContext context);
}
```

**Usage**:
```csharp
// PostActionsMenuProvider.cs
public class PostActionsMenuProvider : IMenuProvider
{
    public string MenuId => "post-actions";
    
    public IEnumerable<IMenuItem> GetItems(MenuContext context)
    {
        yield return new MenuItem("edit", "Edit", "edit_post", Icons.Edit, order: 1)
        {
            IsVisible = ctx => ctx.IsOwner
        };
        
        yield return new MenuItem("delete", "Delete", "delete_post", Icons.Delete, order: 100)
        {
            IsVisible = ctx => ctx.IsOwner,
            IsDangerous = true
        };
        
        yield return new MenuItem("report", "Report", "report_post", Icons.Flag, order: 90)
        {
            IsVisible = ctx => !ctx.IsOwner
        };
    }
}
```

---

### 3. Action Pattern

**Problem**: Actions are scattered across components with duplicated logic.

**Solution**: Command Pattern with Handlers

```csharp
// Sivar.Os.Shared/Framework/Actions/IAction.cs
public interface IAction
{
    string Id { get; }
    string Name { get; }
}

public interface IAction<TContext> : IAction
{
    Task<ActionResult> ExecuteAsync(TContext context);
}

// Sivar.Os.Shared/Framework/Actions/ActionResult.cs
public class ActionResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? MessageKey { get; set; }
    public object? Data { get; set; }
    public ActionResultType Type { get; set; }
    
    public static ActionResult Ok(string? message = null) => 
        new() { Success = true, Message = message, Type = ActionResultType.Success };
    public static ActionResult Error(string message) => 
        new() { Success = false, Message = message, Type = ActionResultType.Error };
    public static ActionResult Confirm(string message) => 
        new() { Success = true, Message = message, Type = ActionResultType.RequiresConfirmation };
}

public enum ActionResultType { Success, Error, Warning, RequiresConfirmation, Navigate }
```

**Post Actions Example**:
```csharp
// Actions/PostActions.cs
public class LikePostAction : IAction<PostActionContext>
{
    public string Id => "like-post";
    public string Name => "Like";
    
    private readonly ISivarClient _client;
    
    public async Task<ActionResult> ExecuteAsync(PostActionContext context)
    {
        var result = await _client.Reactions.ToggleLikeAsync(context.PostId);
        return result.Success 
            ? ActionResult.Ok() 
            : ActionResult.Error(result.Error ?? "Failed to like post");
    }
}

public class DeletePostAction : IAction<PostActionContext>
{
    public string Id => "delete-post";
    public string Name => "Delete";
    
    public async Task<ActionResult> ExecuteAsync(PostActionContext context)
    {
        // First, require confirmation
        if (!context.IsConfirmed)
            return ActionResult.Confirm("Are you sure you want to delete this post?");
            
        var result = await _client.Posts.DeleteAsync(context.PostId);
        return result ? ActionResult.Ok("Post deleted") : ActionResult.Error("Failed to delete");
    }
}
```

---

### 4. Component Pattern

**Problem**: Components duplicated or inconsistent between platforms.

**Solution**: Component Registry with Platform Variants

```csharp
// Sivar.Os.Shared/Framework/Components/IComponentFactory.cs
public interface IComponentFactory
{
    Type GetComponent(string componentId, PlatformType platform);
    Type GetComponent<TProps>(string componentId, PlatformType platform);
}

// Sivar.Os.Shared/Framework/Components/ComponentRegistry.cs
public class ComponentRegistry : IComponentFactory
{
    private readonly Dictionary<(string, PlatformType), Type> _components = new();
    
    public void Register(string id, Type componentType, PlatformType platform = PlatformType.All)
    {
        _components[(id, platform)] = componentType;
    }
    
    public Type GetComponent(string id, PlatformType platform)
    {
        // Try platform-specific first, then fall back to All
        if (_components.TryGetValue((id, platform), out var specific))
            return specific;
        if (_components.TryGetValue((id, PlatformType.All), out var universal))
            return universal;
        throw new ComponentNotFoundException(id, platform);
    }
}
```

---

## Refactor Plan (Non-Breaking)

> **Principle**: Add new patterns alongside existing code, then migrate incrementally. Never remove working code until the new pattern is proven.

### Strategy: Parallel Implementation

```
Phase 1: ADD new interfaces/classes (no changes to existing code)
Phase 2: CREATE adapters that wrap existing code
Phase 3: OPT-IN new components can use new patterns
Phase 4: MIGRATE existing components one-by-one
Phase 5: DEPRECATE old patterns (mark, don't delete)
Phase 6: CLEANUP (only after full validation)
```

---

## Phase 1: Foundation (No Breaking Changes)

**Goal**: Create framework interfaces without touching existing code.

**Status**: ✅ **COMPLETE** (December 29, 2025)

### 1.1 Created Directory Structure

```
Sivar.Os.Shared/
└── Framework/
    ├── PlatformType.cs                           ✅ Created
    └── Navigation/
        ├── INavigationItem.cs                    ✅ Created
        ├── INavigationRegistry.cs                ✅ Created
        ├── NavigationItem.cs                     ✅ Created
        ├── NavigationContext.cs                  ✅ Created
        ├── NavigationRegistry.cs                 ✅ Created
        ├── CoreNavigationItems.cs                ✅ Created
        └── NavigationServiceExtensions.cs        ✅ Created
```

### 1.2 Tasks (Week 1) - COMPLETED

| Task | File | Breaking? | Status |
|------|------|-----------|--------|
| Create `PlatformType` enum | `Framework/PlatformType.cs` | ❌ No | ✅ Done |
| Create `INavigationItem` | `Framework/Navigation/INavigationItem.cs` | ❌ No | ✅ Done |
| Create `NavigationItem` | `Framework/Navigation/NavigationItem.cs` | ❌ No | ✅ Done |
| Create `NavContext` | `Framework/Navigation/NavigationContext.cs` | ❌ No | ✅ Done |
| Create `INavigationRegistry` | `Framework/Navigation/INavigationRegistry.cs` | ❌ No | ✅ Done |
| Create `NavigationRegistry` | `Framework/Navigation/NavigationRegistry.cs` | ❌ No | ✅ Done |
| Create `CoreNavigationItems` | `Framework/Navigation/CoreNavigationItems.cs` | ❌ No | ✅ Done |
| Create DI extensions | `Framework/Navigation/NavigationServiceExtensions.cs` | ❌ No | ✅ Done |
| Register in DI (Client) | `Sivar.Os.Client/Program.cs` | ❌ No | ✅ Done |
| Register in DI (Server) | `Sivar.Os/Program.cs` | ❌ No | ✅ Done |
| Create `FrameworkNavMenu` | `Components/Navigation/FrameworkNavMenu.razor` | ❌ No | ✅ Done |
| Create localization (en) | `Resources/.../FrameworkNavMenu.resx` | ❌ No | ✅ Done |
| Create localization (es) | `Resources/.../FrameworkNavMenu.es-ES.resx` | ❌ No | ✅ Done |

### 1.3 Validation Criteria

- [ ] Solution builds without errors
- [ ] All existing tests pass
- [ ] No changes to existing `.razor` files
- [ ] No changes to existing services

---

## Phase 2: Navigation Items Definition (No Breaking Changes)

**Goal**: Define all navigation items in the registry without modifying NavMenu.

### 2.1 Create Navigation Item Definitions

```csharp
// Sivar.Os.Shared/Framework/Navigation/CoreNavigationItems.cs
public static class CoreNavigationItems
{
    public static readonly NavigationItem Home = new()
    {
        Id = "home",
        TitleKey = "Home",
        Icon = "Home",
        Route = "/home",
        Order = 10,
        RequiresAuth = true
    };
    
    public static readonly NavigationItem Search = new()
    {
        Id = "search",
        TitleKey = "Search",
        Icon = "Search",
        Route = "/search",
        Order = 20,
        RequiresAuth = true
    };
    
    public static readonly NavigationItem MySchedule = new()
    {
        Id = "my-schedule",
        TitleKey = "MySchedule",
        Icon = "EventNote",
        Route = "/my-schedule",
        Order = 30,
        RequiresAuth = true,
        RequiredProfileTypes = new[] { "Business" }
    };
    
    public static readonly NavigationItem Bookings = new()
    {
        Id = "bookings",
        TitleKey = "Bookings",
        Icon = "EventAvailable",
        Route = "/bookings",
        Order = 30,
        RequiresAuth = true,
        IsVisible = ctx => ctx.ActiveProfile?.ProfileType?.Name != "Business"
    };
    
    public static readonly NavigationItem Chat = new()
    {
        Id = "chat",
        TitleKey = "Chat",
        Icon = "SmartToy",
        Route = null,  // Special action, not a route
        Order = 40,
        RequiresAuth = true,
        IsAction = true
    };
    
    // All items for registration
    public static IEnumerable<NavigationItem> All => new[]
    {
        Home, Search, MySchedule, Bookings, Chat
    };
}
```

### 2.2 Tasks (Week 1, continued)

| Task | File | Breaking? | Notes |
|------|------|-----------|-------|
| Create `CoreNavigationItems` | `Framework/Navigation/CoreNavigationItems.cs` | ❌ No | Static definitions |
| Create `NavigationRegistryExtensions` | `Framework/Navigation/Extensions.cs` | ❌ No | Helper methods |
| Add unit tests | `Sivar.Os.Tests/Framework/` | ❌ No | Test registry |

---

## Phase 3: Adapter Pattern (Gradual Migration)

**Goal**: Create a new NavMenu component that uses the registry, without removing the old one.

### 3.1 Create New Component (Parallel)

```
Sivar.Os.Client/
└── Components/
    └── Navigation/           # NEW folder
        ├── FrameworkNavMenu.razor      # NEW - uses registry
        └── FrameworkNavMenu.razor.cs
```

### 3.2 Feature Flag Approach

```csharp
// appsettings.json
{
  "FeatureFlags": {
    "UseFrameworkNavigation": false  // Toggle to enable new nav
  }
}
```

```razor
@* MainLayout.razor - Non-breaking switch *@
@if (_useFrameworkNavigation)
{
    <FrameworkNavMenu />
}
else
{
    <NavMenu />  @* Existing component - unchanged *@
}
```

### 3.3 Tasks (Week 2)

| Task | File | Breaking? | Notes |
|------|------|-----------|-------|
| Create `FrameworkNavMenu.razor` | `Components/Navigation/` | ❌ No | New component |
| Add feature flag | `appsettings.json` | ❌ No | Default: false |
| Add toggle in MainLayout | `Layout/MainLayout.razor` | ⚠️ Minor | Adds conditional |
| Test both modes | Manual testing | ❌ No | Verify feature flag |

---

## Phase 4: Menu Framework (No Breaking Changes)

**Goal**: Create menu system for post/profile actions.

### 4.1 Create Menu Framework

```
Sivar.Os.Shared/
└── Framework/
    └── Menu/
        ├── IMenuItem.cs
        ├── IMenuProvider.cs
        ├── MenuItem.cs
        ├── MenuContext.cs
        └── Providers/
            ├── PostActionsProvider.cs
            └── ProfileActionsProvider.cs
```

### 4.2 Create Generic Menu Component

```razor
@* Sivar.Os.Client/Components/Shared/FrameworkContextMenu.razor *@
@typeparam TTarget

<MudMenu Icon="@Icons.Material.Filled.MoreVert" Dense="true">
    @foreach (var item in _visibleItems)
    {
        @if (item.Type == MenuItemType.Separator)
        {
            <MudDivider />
        }
        else
        {
            <MudMenuItem Icon="@item.Icon" 
                         OnClick="@(() => HandleClick(item))"
                         Style="@(item.IsDangerous ? "color: var(--mud-palette-error);" : "")">
                @Localizer[item.TitleKey]
            </MudMenuItem>
        }
    }
</MudMenu>
```

### 4.3 Tasks (Week 2-3)

| Task | File | Breaking? | Notes |
|------|------|-----------|-------|
| Create menu interfaces | `Framework/Menu/` | ❌ No | Interfaces only |
| Create `PostActionsProvider` | `Framework/Menu/Providers/` | ❌ No | Implementation |
| Create `FrameworkContextMenu` | `Components/Shared/` | ❌ No | New component |
| Add to one component for testing | `PostCard.razor` | ⚠️ Minor | Optional usage |

---

## Phase 5: Action Framework (No Breaking Changes)

**Goal**: Centralize action handling with command pattern.

### 5.1 Create Action Framework

```
Sivar.Os.Shared/
└── Framework/
    └── Actions/
        ├── IAction.cs
        ├── IActionDispatcher.cs
        ├── ActionResult.cs
        ├── ActionContext.cs
        └── Handlers/
            ├── Post/
            │   ├── LikePostAction.cs
            │   ├── SharePostAction.cs
            │   └── DeletePostAction.cs
            └── Profile/
                ├── FollowProfileAction.cs
                └── BlockProfileAction.cs
```

### 5.2 Action Dispatcher Service

```csharp
// IActionDispatcher.cs
public interface IActionDispatcher
{
    Task<ActionResult> DispatchAsync<TContext>(string actionId, TContext context);
    void RegisterAction<TContext>(IAction<TContext> action);
}

// Usage in components (opt-in)
await _dispatcher.DispatchAsync("like-post", new PostActionContext { PostId = post.Id });
```

### 5.3 Tasks (Week 3)

| Task | File | Breaking? | Notes |
|------|------|-----------|-------|
| Create action interfaces | `Framework/Actions/` | ❌ No | Interfaces only |
| Create `ActionDispatcher` | `Framework/Actions/` | ❌ No | Implementation |
| Create post action handlers | `Framework/Actions/Handlers/Post/` | ❌ No | Action classes |
| Register in DI | `Program.cs` | ❌ No | AddScoped |
| Wire to menu items | `PostActionsProvider.cs` | ❌ No | Connect menu → action |

---

## Phase 6: Mobile Alignment (MAUI)

**Goal**: Share navigation and components with MAUI project.

### 6.1 Reference Shared Framework

```xml
<!-- Sivar.Os.Maui/Sivar.Os.Maui.csproj -->
<ProjectReference Include="..\Sivar.Os.Shared\Sivar.Os.Shared.csproj" />
```

### 6.2 Create Mobile NavMenu Using Registry

```razor
@* Sivar.Os.Maui/Components/FrameworkNavMenu.razor *@
@inject INavigationRegistry NavRegistry

<MudNavMenu>
    @foreach (var item in _navItems)
    {
        <MudNavLink Href="@item.Route" 
                    Icon="@GetIcon(item.Icon)"
                    Match="NavLinkMatch.Prefix">
            @Localizer[item.TitleKey]
        </MudNavLink>
    }
</MudNavMenu>
```

### 6.3 Tasks (Week 4)

| Task | File | Breaking? | Notes |
|------|------|-----------|-------|
| Add project reference | `Sivar.Os.Maui.csproj` | ❌ No | Reference shared |
| Register services | `MauiProgram.cs` | ❌ No | Add DI |
| Create mobile nav | `Components/FrameworkNavMenu.razor` | ❌ No | New component |
| Test on mobile | Manual testing | ❌ No | Verify mobile nav |

---

## Migration Checklist

### Before Starting Each Phase

- [ ] Create a git branch: `feature/framework-phase-X`
- [ ] Ensure all tests pass on main
- [ ] Document any risks

### After Each Phase

- [ ] Run full test suite
- [ ] Test Web app manually
- [ ] Test Mobile app (if applicable)
- [ ] Code review
- [ ] Merge to main
- [ ] Tag release: `framework-v0.X.0`

---

## Rollback Plan

### If Issues Occur

1. **Feature Flag**: Set `UseFrameworkNavigation: false`
2. **Component Level**: Old components remain unchanged
3. **Git Revert**: `git revert <commit>` if needed
4. **DI Registration**: Comment out framework services

### What NOT to Do

- ❌ Don't delete old components until Phase 6 is complete
- ❌ Don't remove existing service methods
- ❌ Don't modify existing DTOs
- ❌ Don't change existing routes

---

## Timeline Summary

| Phase | Duration | Risk Level | Dependencies |
|-------|----------|------------|--------------|
| **1. Foundation** | 3-4 days | 🟢 Low | None |
| **2. Nav Items** | 2-3 days | 🟢 Low | Phase 1 |
| **3. Adapter** | 3-4 days | 🟡 Medium | Phase 2 |
| **4. Menu** | 4-5 days | 🟢 Low | Phase 1 |
| **5. Actions** | 4-5 days | 🟡 Medium | Phase 4 |
| **6. Mobile** | 3-4 days | 🟡 Medium | Phase 1-3 |

**Total Estimated Time**: 3-4 weeks

---

## Success Criteria

### Phase 1 Complete When: ✅ DONE
- [x] All interfaces exist in `Sivar.Os.Shared/Framework/`
- [x] Solution compiles
- [x] No runtime errors
- [ ] Unit tests for registry pass

### Phase 2 Complete When: ✅ DONE
- [x] Navigation items defined in `CoreNavigationItems.cs`
- [x] DI registration in Client and Server `Program.cs`
- [x] `FrameworkNavMenu.razor` component created
- [x] Localization files for en-US and es-ES

### Phase 3 Complete When: ✅ DONE
- [x] Feature flags added to `appsettings.json`
- [x] `FrameworkFeatureFlags.cs` class created
- [x] `MainLayout.razor` conditionally renders FrameworkNavMenu or NavMenu
- [x] Action handler for Chat toggle implemented

### Phase 4 Complete When: ✅ DONE
- [x] `IMenuItem`, `MenuItem` interfaces created
- [x] `MenuContext` for entity-specific context
- [x] `IMenuProvider`, `IMenuRegistry` interfaces
- [x] `MenuRegistry` thread-safe implementation
- [x] `CoreMenuItems` for Post, Comment, Profile, Blog actions
- [x] `FrameworkContextMenu.razor` component with confirmation dialogs
- [x] Localization files (en-US, es-ES) for menu items
- [x] Menu framework auto-registered with navigation framework

### Phase 5 Complete When: ✅ DONE
- [x] `ActionResult` with Ok/Fail/OkWithRefresh/OkWithNavigation factory methods
- [x] `ActionContext` with entity info and parameters
- [x] `IActionHandler` interface and `ActionHandlerBase` base class
- [x] `IActionDispatcher` interface with events
- [x] `ActionDispatcher` with pattern matching and delegate handlers
- [x] Core handlers: `ShareActionHandler`, `CopyLinkActionHandler`, `NavigationActionHandler`
- [x] `FrameworkContextMenu` integrated with ActionDispatcher
- [x] Action framework auto-registered with navigation framework

### Phase 6 Complete When: ✅ DONE
- [x] `MauiProgram.cs` registers `AddNavigationFramework()`
- [x] `_Imports.razor` includes framework namespaces
- [x] `MainLayout.razor` uses bottom navigation with registry
- [x] `NavMenu.razor` uses `INavigationRegistry` for drawer menu
- [x] `MobileContextMenu.razor` uses `IMenuRegistry` and `IActionDispatcher`
- [x] Mobile-specific items defined in `CoreNavigationItems` (Explore, Profile)

### Full Migration Complete When:
- [x] Web app uses framework navigation (behind feature flag)
- [x] Menu framework created (behind feature flag)
- [x] Action dispatcher implemented
- [x] Mobile app uses framework navigation
- [x] Feature flag enabled for testing (`UseFrameworkNavigation: true`)
- [x] Auth state integration fixed (LoadNavigationProfileAsync)
- [ ] Old NavMenu component deprecated (after stability period)
- [x] Documentation updated

---

## Troubleshooting

### Menu Items Not Showing After Login
**Cause**: Auth state not properly synchronized with profile loading.  
**Solution**: Ensure `MainLayout` checks `AuthenticationStateProvider` before loading profiles.

```csharp
// MainLayout.razor - LoadNavigationProfileAsync pattern
var authState = await AuthStateProvider.GetAuthenticationStateAsync();
var isUserAuthenticated = authState.User?.Identity?.IsAuthenticated ?? false;

if (!isUserAuthenticated)
{
    _isAuthenticated = false;
    _currentProfile = null;
    return;
}

_currentProfile = await SivarClient.Profiles.GetMyActiveProfileAsync();
_isAuthenticated = _currentProfile != null;
```

### ProfileSwitcher Type Mismatch
**Issue**: `ProfileSwitcher` expects `ProfileDto`, but `MainLayout` has `ActiveProfileDto`.  
**Solution**: Create a computed property to convert or find matching profile:

```csharp
private ProfileDto? _activeProfileForSwitcher => 
    ActiveProfile != null 
        ? UserProfiles.FirstOrDefault(p => p.Id == ActiveProfile.Id) 
          ?? new ProfileDto { Id = ActiveProfile.Id, DisplayName = ActiveProfile.DisplayName }
        : null;
```

---

## Related Documentation

| Document | Purpose |
|----------|---------|
| [DEVELOPMENT_RULES.md](DEVELOPMENT_RULES.md) | Coding standards and patterns |
| [TODO.md](TODO.md) | Pending work items |
| [MULTI_LANGUAGE_LOCALIZATION_PLAN.md](MULTI_LANGUAGE_LOCALIZATION_PLAN.md) | i18n integration |

---

## Next Steps

1. ✅ Review this refactor plan
2. ✅ Approve Phase 1 scope
3. ✅ Implement foundation interfaces (Phase 1)
4. ✅ Create navigation items and FrameworkNavMenu (Phase 2)
5. ✅ Add feature flag toggle in MainLayout (Phase 3)
6. ✅ Create Menu Framework with FrameworkContextMenu (Phase 4)
7. ✅ Implement Action Framework with dispatcher (Phase 5)
8. ✅ Align MAUI mobile with framework (Phase 6)
9. ⏳ Test by enabling `UseFrameworkNavigation: true` in appsettings.json
10. ⏳ Flip feature flags for production rollout

**Ready to test? Set `FrameworkFeatures:UseFrameworkNavigation` to `true` in `appsettings.json`**
