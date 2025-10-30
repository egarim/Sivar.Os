# Sivar.Os Development Rules & Guidelines

> **Last Updated**: October 29, 2025  
> **Project Type**: Blazor Server (Interactive Server Only)  
> **Target Framework**: .NET 9.0

---

## Table of Contents
1. [Project Architecture Overview](#project-architecture-overview)
2. [Blazor Configuration](#blazor-configuration)
3. [Service Layer Rules](#service-layer-rules)
4. [Repository Layer Rules](#repository-layer-rules)
5. [Controller Usage](#controller-usage)
6. [CSS Organization & Styling](#css-organization--styling)
7. [Logging Standards](#logging-standards)
8. [Authentication & Authorization](#authentication--authorization)
9. [Error Handling](#error-handling)
10. [Testing & Debugging](#testing--debugging)
11. [References](#references)

---

## Project Architecture Overview

### Architecture Pattern: Repository-Service-Component

```
┌─────────────────────────────────────────────────────────────┐
│                    Blazor Components (.razor)                │
│                  (Client & Server Projects)                  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                   Service Layer (Services/)                  │
│        Business Logic, Validation, DTO Mapping               │
│   PostService, UserService, ProfileService, etc.             │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│               Repository Layer (Shared/Repositories)         │
│           Data Access, EF Core, Database Queries             │
│   PostRepository, UserRepository, ProfileRepository, etc.    │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│              Database Context (SivarDbContext)               │
│                     PostgreSQL Database                      │
└─────────────────────────────────────────────────────────────┘
```

### Project Structure

```
Sivar.Os/                         (Main Blazor Server Project)
├── Services/                     ⭐ PRIMARY - Business Logic
│   ├── PostService.cs
│   ├── UserService.cs
│   ├── ProfileService.cs
│   └── Clients/                  (Service Clients for hybrid mode)
├── Controllers/                  ⚠️ DEPRECATED - Not in active use
│   └── PostsController.cs        (Legacy - kept for reference)
├── Components/
│   ├── Pages/
│   └── Shared/
├── Program.cs                    (Dependency Injection & Configuration)
├── appsettings.json              (Production Settings)
└── appsettings.Development.json  (Development Settings)

Sivar.Os.Client/                  (Client-side Blazor Components)
├── Components/
├── Pages/
└── Services/                     (Client-side service interfaces)

Sivar.Os.Shared/                  (Shared Code)
├── Entities/                     (Database Models)
├── DTOs/                         (Data Transfer Objects)
├── Repositories/                 ⭐ PRIMARY - Data Access Interfaces
└── Services/                     (Service Interfaces)

Sivar.Os.Data/                    (Data Layer Implementation)
├── Context/
│   └── SivarDbContext.cs
└── Repositories/                 ⭐ PRIMARY - Repository Implementations
    ├── PostRepository.cs
    ├── UserRepository.cs
    └── ProfileRepository.cs
```

---

## Blazor Configuration

### ⚠️ CURRENT: Blazor Server ONLY - WebAssembly Ready

This project **currently uses** Interactive Server render mode exclusively. However, **all code must be written to support both Blazor Server AND WebAssembly** for future scalability.

### Render Mode Configuration

**Current Configuration in `Program.cs`:**
```csharp
// ✅ CURRENT - Server Only
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ⚠️ FUTURE - May add WebAssembly support
// .AddInteractiveWebAssemblyComponents();
```

**In Components (Current):**
```razor
<!-- ✅ CURRENT - Use InteractiveServer -->
@rendermode InteractiveServer

<!-- ⚠️ FUTURE - May use InteractiveAuto -->
@* @rendermode InteractiveAuto *@
```

### When to Use Render Modes

| Render Mode | Use Case | Example |
|-------------|----------|---------|
| `@rendermode InteractiveServer` | Interactive components (forms, real-time updates) | `CreatePost.razor`, `ProfileSwitcher.razor` |
| Static (no attribute) | Static content, layouts, error pages | `Error.razor`, static sections |

### ⚠️ CRITICAL: Write Code Compatible with Both Server and WebAssembly

Even though we're currently using Server-only mode, **all code MUST be compatible with both**.

---

## UI Component Standards

### ⭐ MANDATORY: Use MudBlazor Components

**ALL UI components MUST use MudBlazor**. Do NOT use raw HTML elements for interactive components.

### MudBlazor Component Usage

✅ **DO: Use MudBlazor Components**

```razor
<!-- ✅ CORRECT - Use MudBlazor -->
<MudTextField @bind-Value="email" 
              Label="Email" 
              Variant="Variant.Outlined" />

<MudButton Color="Color.Primary" 
           Variant="Variant.Filled" 
           OnClick="HandleSubmit">
    Submit
</MudButton>

<MudCard>
    <MudCardContent>
        <MudText Typo="Typo.h5">Title</MudText>
        <MudText Typo="Typo.body2">Content</MudText>
    </MudCardContent>
</MudCard>

<MudDialog @bind-IsVisible="showDialog">
    <DialogContent>
        <MudText>Dialog content</MudText>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="CloseDialog">Close</MudButton>
    </DialogActions>
</MudDialog>
```

❌ **DON'T: Use Raw HTML for Interactive Components**

```razor
<!-- ❌ INCORRECT - Don't use raw HTML -->
<input type="text" @bind="email" />
<button @onclick="HandleSubmit">Submit</button>
<div class="card">...</div>
```

### Common MudBlazor Components Reference

| Purpose | MudBlazor Component | Example |
|---------|-------------------|---------|
| Text Input | `<MudTextField>` | Email, username, search |
| Number Input | `<MudNumericField>` | Age, quantity |
| Select/Dropdown | `<MudSelect>` | Profile type selection |
| Checkbox | `<MudCheckBox>` | Terms acceptance |
| Radio | `<MudRadio>` | Option selection |
| Button | `<MudButton>` | Submit, cancel, action buttons |
| Icon Button | `<MudIconButton>` | Delete, edit icons |
| Card | `<MudCard>` | Post cards, profile cards |
| Dialog/Modal | `<MudDialog>` | Create profile modal |
| Table | `<MudTable>` | Data tables |
| List | `<MudList>` | Navigation, items |
| Avatar | `<MudAvatar>` | User profile pictures |
| Chip | `<MudChip>` | Tags, labels |
| Snackbar | `<MudSnackbar>` | Toast notifications |
| Progress | `<MudProgressCircular>`, `<MudProgressLinear>` | Loading indicators |

### MudBlazor Registration

Already configured in `Program.cs`:
```csharp
builder.Services.AddMudServices();
```

### MudBlazor Resources

- [MudBlazor Documentation](https://mudblazor.com/)
- [MudBlazor Component Gallery](https://mudblazor.com/components)
- [MudBlazor GitHub](https://github.com/MudBlazor/MudBlazor)

---

## Cross-Platform Compatibility (Server & WebAssembly)

### ⚠️ CRITICAL: All Code Must Work on Both Server and WebAssembly

Even though the project currently uses Blazor Server only, **all patterns and code must be compatible with both Blazor Server and WebAssembly**.

### Service Injection Pattern (Compatible with Both)

✅ **DO: Use Dependency Injection (Works on Both)**

```razor
@inject IPostService PostService
@inject IAuthenticationService AuthService

@code {
    private async Task LoadData()
    {
        var posts = await PostService.GetActivityFeedAsync(keycloakId);
    }
}
```

❌ **DON'T: Use HttpContext Directly (Server-Only)**

```razor
@inject IHttpContextAccessor HttpContextAccessor  ❌ WebAssembly doesn't have this

@code {
    private void GetUser()
    {
        var user = HttpContextAccessor.HttpContext?.User;  ❌ Breaks in WebAssembly
    }
}
```

### Authentication Pattern (Compatible with Both)

✅ **DO: Use Cascading Authentication State (Works on Both)**

```razor
<AuthorizeView>
    <Authorized>
        <p>Welcome, @context.User.Identity?.Name</p>
    </Authorized>
    <NotAuthorized>
        <p>Please log in</p>
    </NotAuthorized>
</AuthorizeView>

@code {
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }
    
    private async Task<string> GetKeycloakId()
    {
        if (AuthenticationStateTask == null)
            return string.Empty;
            
        var authState = await AuthenticationStateTask;
        return authState.User.FindFirst("sub")?.Value ?? string.Empty;
    }
}
```

### JavaScript Interop Pattern (Compatible with Both)

✅ **DO: Use IJSRuntime (Works on Both)**

```razor
@inject IJSRuntime JS

@code {
    private async Task ShowAlert(string message)
    {
        await JS.InvokeVoidAsync("alert", message);
    }
    
    private async Task<string> GetLocalStorage(string key)
    {
        return await JS.InvokeAsync<string>("localStorage.getItem", key);
    }
}
```

### State Management Pattern (Compatible with Both)

✅ **DO: Use Scoped Services for State (Works on Both)**

```csharp
// In Program.cs
builder.Services.AddScoped<AppState>();

// AppState.cs
public class AppState
{
    public event Action? OnChange;
    
    private string? _currentProfileId;
    public string? CurrentProfileId
    {
        get => _currentProfileId;
        set
        {
            _currentProfileId = value;
            NotifyStateChanged();
        }
    }
    
    private void NotifyStateChanged() => OnChange?.Invoke();
}

// In component
@inject AppState AppState
@implements IDisposable

@code {
    protected override void OnInitialized()
    {
        AppState.OnChange += StateHasChanged;
    }
    
    public void Dispose()
    {
        AppState.OnChange -= StateHasChanged;
    }
}
```

### File Upload Pattern (Compatible with Both)

✅ **DO: Use InputFile with Streams (Works on Both)**

```razor
<InputFile OnChange="HandleFileSelected" accept="image/*" />

@code {
    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        
        // Read file stream (works on both Server and WebAssembly)
        using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        
        var bytes = memoryStream.ToArray();
        
        // Upload using service
        await FileService.UploadAsync(bytes, file.Name);
    }
}
```

### Navigation Pattern (Compatible with Both)

✅ **DO: Use NavigationManager (Works on Both)**

```razor
@inject NavigationManager Navigation

@code {
    private void GoToHome()
    {
        Navigation.NavigateTo("/");
    }
    
    private void GoToProfile(string profileId)
    {
        Navigation.NavigateTo($"/profile/{profileId}");
    }
}
```

### Lifecycle Patterns (Compatible with Both)

✅ **DO: Use Blazor Component Lifecycle Methods**

```razor
@code {
    protected override async Task OnInitializedAsync()
    {
        // Called once when component initializes
        // Works on both Server and WebAssembly
        await LoadDataAsync();
    }
    
    protected override async Task OnParametersSetAsync()
    {
        // Called when parameters change
        // Works on both Server and WebAssembly
        await RefreshDataAsync();
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Called after component renders
        // Works on both Server and WebAssembly
        if (firstRender)
        {
            await JS.InvokeVoidAsync("initializeComponent");
        }
    }
}
```

### Cross-Platform Compatibility Checklist

Before creating any component or service, verify:

- [ ] **No direct HttpContext access** - Use `IAuthenticationService` or `AuthenticationState` instead
- [ ] **No server-side only APIs** - Check if API exists in WebAssembly
- [ ] **Use MudBlazor components** - All UI components use MudBlazor
- [ ] **Dependency injection for services** - Never instantiate services with `new`
- [ ] **Proper async/await patterns** - All async operations properly awaited
- [ ] **Cascading parameters for auth** - Use `CascadingParameter` for authentication state
- [ ] **IJSRuntime for JavaScript** - Never use raw JavaScript in HTML
- [ ] **NavigationManager for routing** - Don't use anchor tags for internal navigation
- [ ] **Scoped services for state** - Use DI for shared state, not static classes

### Testing Compatibility

When testing components:
1. Test in current Blazor Server mode
2. Mentally verify: "Would this work in WebAssembly?"
3. Check for server-only dependencies (HttpContext, etc.)
4. Ensure all services are injected, not hardcoded

---

## Service Layer Rules

### ⭐ Services are the PRIMARY business logic layer

### Service Responsibilities

1. **Business Logic**: All business rules and validation
2. **DTO Mapping**: Convert between Entities and DTOs
3. **Orchestration**: Coordinate multiple repository calls
4. **Validation**: Input validation and business rule enforcement
5. **Error Handling**: Catch and log repository errors
6. **Logging**: Comprehensive operation logging

### Service Structure Template

```csharp
using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Repositories;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Services;

/// <summary>
/// Service for [Domain] management
/// Provides business logic layer for [domain] operations
/// </summary>
public class ExampleService : IExampleService
{
    private readonly IExampleRepository _repository;
    private readonly ILogger<ExampleService> _logger;

    public ExampleService(
        IExampleRepository repository,
        ILogger<ExampleService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResultDto?> DoSomethingAsync(string keycloakId, InputDto input)
    {
        if (string.IsNullOrWhiteSpace(keycloakId) || input == null)
            return null;

        _logger.LogInformation(
            "[DoSomethingAsync] START - KeycloakId={keycloakId}", 
            keycloakId);

        try
        {
            // 1. Validation
            if (!ValidateInput(input))
            {
                _logger.LogWarning(
                    "[DoSomethingAsync] Validation failed for {keycloakId}", 
                    keycloakId);
                return null;
            }

            // 2. Repository calls
            var entity = await _repository.GetByIdAsync(input.Id);
            _logger.LogInformation(
                "[DoSomethingAsync] Retrieved entity: {entityId}", 
                entity?.Id.ToString() ?? "NULL");

            if (entity == null)
            {
                _logger.LogWarning(
                    "[DoSomethingAsync] Entity not found: {id}", 
                    input.Id);
                return null;
            }

            // 3. Business logic
            entity.Process();

            // 4. Save changes
            await _repository.UpdateAsync(entity);
            _logger.LogInformation(
                "[DoSomethingAsync] Updated entity: {entityId}", 
                entity.Id);

            // 5. Map to DTO
            var result = MapToDto(entity);

            _logger.LogInformation(
                "[DoSomethingAsync] SUCCESS - Result={resultId}", 
                result.Id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "[DoSomethingAsync] FAILED - KeycloakId={keycloakId}", 
                keycloakId);
            throw;
        }
    }

    private bool ValidateInput(InputDto input)
    {
        // Validation logic
        return true;
    }

    private ResultDto MapToDto(Entity entity)
    {
        // Mapping logic
        return new ResultDto();
    }
}
```

### Service Registration in `Program.cs`

```csharp
// Service Layer - Business Logic
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IReactionService, ReactionService>();
// ... add more as needed
```

### Service Best Practices

✅ **DO:**
- Always inject `ILogger<T>` for comprehensive logging
- Use structured logging with named parameters
- Return DTOs, never return Entities directly
- Handle NULL cases explicitly
- Log at method entry, key decision points, and exit
- Use dependency injection for all dependencies
- Keep methods focused and single-purpose
- Validate all inputs

❌ **DON'T:**
- Don't expose Entities to components
- Don't access DbContext directly from services
- Don't catch exceptions without logging
- Don't use magic strings or numbers
- Don't skip NULL checks
- Don't return repository entities directly

---

## Repository Layer Rules

### ⭐ Repositories are the ONLY data access layer

### Repository Responsibilities

1. **Data Access**: All EF Core database queries
2. **CRUD Operations**: Create, Read, Update, Delete
3. **Complex Queries**: Joins, filtering, pagination
4. **Soft Delete**: Handle `IsDeleted` flag
5. **Transaction Management**: Unit of work patterns

### Repository Structure Template

```csharp
using Microsoft.EntityFrameworkCore;
using Sivar.Os.Data.Context;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.Data.Repositories;

public class ExampleRepository : IExampleRepository
{
    private readonly SivarDbContext _context;

    public ExampleRepository(SivarDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Example?> GetByIdAsync(Guid id)
    {
        return await _context.Examples
            .Where(e => e.Id == id && !e.IsDeleted)
            .Include(e => e.RelatedEntity)
            .FirstOrDefaultAsync();
    }

    public async Task<Example> CreateAsync(Example entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.IsDeleted = false;
        
        _context.Examples.Add(entity);
        await _context.SaveChangesAsync();
        
        return entity;
    }

    public async Task<Example> UpdateAsync(Example entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        
        _context.Examples.Update(entity);
        await _context.SaveChangesAsync();
        
        return entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        // Soft delete
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        
        await UpdateAsync(entity);
        return true;
    }
}
```

### Repository Registration in `Program.cs`

```csharp
// Repository Layer - Data Access
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IReactionRepository, ReactionRepository>();
// ... add more as needed
```

### Repository Best Practices

✅ **DO:**
- Always filter by `!IsDeleted` unless specifically querying deleted items
- Use `Include()` for eager loading related entities
- Use `AsNoTracking()` for read-only queries
- Always set `CreatedAt` on creation
- Always set `UpdatedAt` on updates
- Use soft delete (set `IsDeleted = true`) instead of hard delete
- Keep queries efficient and indexed

❌ **DON'T:**
- Don't include business logic in repositories
- Don't log extensively (let services handle logging)
- Don't expose `IQueryable` outside the repository
- Don't hard delete records (use soft delete)
- Don't forget to include related entities when needed

---

## Controller Usage

### ⚠️ Controllers are DEPRECATED

**Status**: Controllers exist in the project but are **NOT actively used**.

**Why**: Blazor Server uses direct service calls from components instead of HTTP API calls.

### Current State

```
Controllers/
├── PostsController.cs      ❌ Not in use
├── UsersController.cs      ❌ Not in use
├── ProfilesController.cs   ❌ Not in use
└── ...                     ❌ Not in use
```

### What to Use Instead

**Instead of Controllers, use Services directly:**

```razor
@inject IPostService PostService

@code {
    private async Task LoadPosts()
    {
        // ✅ CORRECT - Direct service call
        var posts = await PostService.GetActivityFeedAsync(keycloakId);
        
        // ❌ INCORRECT - Don't use HTTP client to call controllers
        // var response = await Http.GetFromJsonAsync<List<PostDto>>("/api/posts");
    }
}
```

### When You Might Need Controllers

Controllers may be needed in the future for:
- External API integrations
- Mobile app backends
- Third-party webhook endpoints
- Public API exposure

**If you need to add controllers:**
1. Document the reason and use case
2. Update this guide
3. Ensure authentication is properly configured
4. Add API versioning

---

## CSS Organization & Styling

### CSS Architecture: Modular & Centralized

All CSS is organized into separate modular files imported by `app.css`.

### File Structure

```
wwwroot/css/
├── app.css                      ⭐ Master file (imports all others)
├── wireframe-theme.css          CSS variables & theme
├── wireframe-layout.css         Grid layouts & responsive
├── wireframe-components.css     UI component styles
└── wireframe-animations.css     Keyframes & transitions
```

### How CSS is Organized

**`app.css`** (Master Import File):
```css
/* Import core theme variables first */
@import url('wireframe-theme.css');

/* Import layout system */
@import url('wireframe-layout.css');

/* Import UI components */
@import url('wireframe-components.css');

/* Import animations */
@import url('wireframe-animations.css');
```

### CSS File Purposes

| File | Purpose | Examples |
|------|---------|----------|
| `wireframe-theme.css` | CSS variables, colors, spacing, fonts | `--primary-color`, `--spacing-md` |
| `wireframe-layout.css` | Grid systems, responsive breakpoints, layout utilities | `.container`, `.grid-2-col` |
| `wireframe-components.css` | UI component styles (buttons, cards, forms) | `.btn`, `.card`, `.form-group` |
| `wireframe-animations.css` | Keyframe animations, transitions, effects | `@keyframes fadeIn`, `.fade-in` |

### CSS Rules & Best Practices

#### ✅ DO: Use Centralized CSS Files

```css
/* ✅ Add to wireframe-components.css */
.post-card {
    background: var(--card-bg);
    border-radius: var(--border-radius);
    padding: var(--spacing-md);
}

.post-card__header {
    display: flex;
    justify-content: space-between;
}

.post-card__content {
    margin-top: var(--spacing-sm);
}
```

#### ✅ DO: Use CSS Variables (from `wireframe-theme.css`)

```css
/* ✅ Use theme variables for consistency */
.button-primary {
    background-color: var(--primary-color);
    color: var(--text-on-primary);
    padding: var(--spacing-sm) var(--spacing-md);
}
```

#### ✅ DO: Follow BEM Naming Convention

```css
/* Block__Element--Modifier pattern */
.profile-switcher { }                    /* Block */
.profile-switcher__dropdown { }          /* Element */
.profile-switcher__item { }              /* Element */
.profile-switcher__item--active { }      /* Modifier */
```

#### ❌ DON'T: Use Component-Scoped CSS Files

```razor
<!-- ❌ AVOID component-scoped CSS files -->
<!-- CreatePost.razor.css -->

<!-- ✅ INSTEAD add to wireframe-components.css -->
```

**When to use `.razor.css` files:**
- **ONLY** for styles that are truly unique to a single component instance
- **ONLY** when the style won't be reused anywhere else
- **RARE** cases only

#### ❌ DON'T: Use Inline Styles

```razor
<!-- ❌ AVOID inline styles -->
<div style="color: red; padding: 10px;">

<!-- ✅ USE CSS classes -->
<div class="error-message">
```

### MudBlazor Integration

This project uses **MudBlazor** for UI components. MudBlazor styles can be overridden:

```css
/* Override MudBlazor styles in app.css or wireframe-components.css */
.mud-main-content {
    padding: 0 !important;
}

.mud-scroll-to-top {
    display: none !important;
}
```

### Responsive Design

Use media queries in `wireframe-layout.css`:

```css
/* Mobile-first approach */
.container {
    padding: var(--spacing-sm);
}

@media (min-width: 768px) {
    .container {
        padding: var(--spacing-lg);
    }
}

@media (min-width: 1024px) {
    .container {
        max-width: 1200px;
        margin: 0 auto;
    }
}
```

### CSS Checklist

Before committing CSS changes:
- [ ] Added to appropriate modular file (`wireframe-*.css`)
- [ ] Used CSS variables from `wireframe-theme.css`
- [ ] Followed BEM naming convention
- [ ] Tested on mobile, tablet, and desktop
- [ ] No inline styles
- [ ] No component-scoped `.razor.css` files (unless absolutely necessary)

---

## Logging Standards

### ⭐ Logging is MANDATORY for all services

### Logging Framework: Serilog

Configured in `Program.cs`:
```csharp
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId());
```

### Log Levels

| Level | When to Use | Example |
|-------|-------------|---------|
| `LogInformation` | Normal flow, key operations | Method entry/exit, successful operations |
| `LogWarning` | Unexpected but handled situations | NULL values, validation failures |
| `LogError` | Exceptions and failures | Database errors, external API failures |
| `LogDebug` | Detailed debugging info | Development only |
| `LogTrace` | Very detailed diagnostic info | Rarely used |

### Logging Pattern

#### Method Entry Logging

```csharp
public async Task<PostDto?> CreatePostAsync(string keycloakId, CreatePostDto createPostDto)
{
    _logger.LogInformation(
        "[CreatePostAsync] START - KeycloakId={keycloakId}, ProfileId={profileId}", 
        keycloakId, 
        createPostDto.ProfileId);
    
    // ... method body
}
```

#### Key Decision Point Logging

```csharp
var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
_logger.LogInformation(
    "[CreatePostAsync] Retrieved user: UserId={userId}, ActiveProfileId={activeProfileId}", 
    user?.Id.ToString() ?? "NULL", 
    user?.ActiveProfileId.ToString() ?? "NULL");

if (user == null)
{
    _logger.LogWarning(
        "[CreatePostAsync] User not found for keycloakId={keycloakId}", 
        keycloakId);
    return null;
}
```

#### Success Logging

```csharp
_logger.LogInformation(
    "[CreatePostAsync] SUCCESS - PostId={postId}, ProfileId={profileId}", 
    newPost.Id, 
    activeProfile.Id);

return postDto;
```

#### Error Logging

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, 
        "[CreatePostAsync] FAILED - KeycloakId={keycloakId}, Error={error}", 
        keycloakId, 
        ex.Message);
    throw;
}
```

### Structured Logging Rules

✅ **DO:**
- Use structured logging with named parameters: `{keycloakId}`, `{userId}`
- Include method name in brackets: `[CreatePostAsync]`
- Log START, key decisions, and SUCCESS/FAILED
- Use NULL-safe logging: `userId?.ToString() ?? "NULL"`
- Log parameters that help with debugging
- Log all exceptions with `LogError(ex, ...)`

❌ **DON'T:**
- Don't use string interpolation: ~~`$"User {userId}"`~~
- Don't log sensitive data (passwords, tokens, credit cards)
- Don't log excessively in tight loops
- Don't skip exception logging
- Don't use generic error messages

### Log File Location

```
Sivar.Os/logs/
├── sivar-20251028.txt
├── sivar-20251029.txt
└── ...
```

Configured in `appsettings.json`.

### Viewing Logs

**Development:**
- Console output (Visual Studio / Terminal)
- Log files in `logs/` directory

**Production:**
- Serilog sinks (file, Azure Application Insights, etc.)

### Example: Complete Service Method with Logging

```csharp
public async Task<PostDto?> GetPostByIdAsync(string keycloakId, Guid postId)
{
    _logger.LogInformation(
        "[GetPostByIdAsync] START - KeycloakId={keycloakId}, PostId={postId}", 
        keycloakId, 
        postId);

    try
    {
        // Step 1: Get user
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        _logger.LogInformation(
            "[GetPostByIdAsync] User lookup: UserId={userId}", 
            user?.Id.ToString() ?? "NULL");

        if (user == null)
        {
            _logger.LogWarning(
                "[GetPostByIdAsync] User not found: {keycloakId}", 
                keycloakId);
            return null;
        }

        // Step 2: Get post
        var post = await _postRepository.GetByIdAsync(postId);
        _logger.LogInformation(
            "[GetPostByIdAsync] Post lookup: PostId={postId}, Found={found}", 
            postId, 
            post != null ? "YES" : "NO");

        if (post == null)
        {
            _logger.LogWarning(
                "[GetPostByIdAsync] Post not found: {postId}", 
                postId);
            return null;
        }

        // Step 3: Map to DTO
        var dto = MapToDto(post);
        
        _logger.LogInformation(
            "[GetPostByIdAsync] SUCCESS - PostId={postId}", 
            postId);

        return dto;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, 
            "[GetPostByIdAsync] FAILED - KeycloakId={keycloakId}, PostId={postId}", 
            keycloakId, 
            postId);
        throw;
    }
}
```

---

## Authentication & Authorization

### Authentication Provider: Keycloak (OpenID Connect)

### Key Claims

| Claim | Source | Description |
|-------|--------|-------------|
| `sub` | Keycloak | Keycloak User ID (external) |
| `email` | Keycloak | User email address |
| `preferred_username` | Keycloak | Username |
| `given_name` | Keycloak | First name |
| `family_name` | Keycloak | Last name |

### User Identity Flow

```
Keycloak Authentication
         ↓
OnTokenValidated (Program.cs)
         ↓
UserAuthenticationService.AuthenticateUserAsync()
         ↓
Create/Retrieve User in Database
         ↓
Create Default Profile (if new user)
         ↓
Set ActiveProfileId
```

### Important: Claim Mapping Configuration

**CRITICAL**: Must be set BEFORE `AddAuthentication()`:

```csharp
// Prevent WS-Fed claim URI wrapping
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthentication(...)
    .AddOpenIdConnect(options =>
    {
        options.MapInboundClaims = false; // Keep claims as-is
        // ...
    });
```

### Getting Current User in Services

```csharp
public async Task<PostDto?> CreatePostAsync(string keycloakId, CreatePostDto dto)
{
    // 1. Get user by Keycloak ID (from claims)
    var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
    
    if (user == null)
        return null;
    
    // 2. Use ActiveProfile or fetch profiles
    var profileId = user.ActiveProfileId ?? GetFirstProfileId(user.Id);
    
    // ... rest of logic
}
```

### Authorization in Components

```razor
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize]

<AuthorizeView>
    <Authorized>
        <p>Welcome, @context.User.Identity.Name!</p>
    </Authorized>
    <NotAuthorized>
        <p>Please log in.</p>
    </NotAuthorized>
</AuthorizeView>
```

---

## Error Handling

### Error Handling Strategy

1. **Services**: Catch, log, and re-throw or return `null`
2. **Repositories**: Let exceptions bubble up to services
3. **Components**: Display user-friendly error messages

### Service Error Handling Pattern

```csharp
public async Task<ResultDto?> DoSomethingAsync(string keycloakId)
{
    try
    {
        // Business logic
        var result = await _repository.GetDataAsync();
        return MapToDto(result);
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, 
            "[DoSomethingAsync] Database error - KeycloakId={keycloakId}", 
            keycloakId);
        throw; // Let global error handler catch it
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, 
            "[DoSomethingAsync] Unexpected error - KeycloakId={keycloakId}", 
            keycloakId);
        throw;
    }
}
```

### Component Error Handling

```razor
@code {
    private string? errorMessage;

    private async Task LoadData()
    {
        try
        {
            errorMessage = null;
            var data = await MyService.GetDataAsync(keycloakId);
            
            if (data == null)
            {
                errorMessage = "No data found.";
                return;
            }
            
            // Process data
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred. Please try again.";
            Logger.LogError(ex, "Failed to load data");
        }
    }
}

@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="error-message">@errorMessage</div>
}
```

### Global Error Handling

Configured in `Program.cs`:
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
```

---

## Testing & Debugging

### Database Access for Debugging

See **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** for:
- PostgreSQL connection details
- Common database queries
- Debugging tools and patterns

### Quick Commands

**Check User and Profile:**
```sql
SELECT "Id", "KeycloakId", "Email", "ActiveProfileId" 
FROM "Sivar_Users" 
WHERE "IsDeleted" = false;
```

**Check Posts:**
```sql
SELECT "Id", "ProfileId", "Content", "Visibility", "CreatedAt"
FROM "Sivar_Posts" 
WHERE "IsDeleted" = false 
ORDER BY "CreatedAt" DESC 
LIMIT 10;
```

### Logging-Based Debugging

Enable detailed logging in `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

This shows all SQL queries executed by EF Core.

### Browser Console Monitoring

Check browser console for:
- SignalR connection status
- JavaScript errors
- Blazor circuit errors

---

## References

### Internal Documentation

- **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** - Comprehensive troubleshooting guide
  - Database access and queries
  - Common issues and solutions
  - ActiveProfile NULL handling
  - Feed loading diagnosis
  - Authentication & profile issues

### External Resources

- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [MudBlazor Components](https://mudblazor.com/)
- [Serilog Documentation](https://serilog.net/)
- [Keycloak Documentation](https://www.keycloak.org/documentation)

---

## Quick Reference Checklist

### Before Creating a New Feature

- [ ] Identify which service(s) will handle business logic
- [ ] Identify which repository/repositories will handle data access
- [ ] Design DTOs for data transfer
- [ ] Plan logging points (START, key decisions, SUCCESS/FAILED)
- [ ] Plan error handling strategy
- [ ] Determine if new CSS classes are needed (add to `wireframe-*.css`)

### Before Committing Code

- [ ] Services have comprehensive logging
- [ ] All repository calls are wrapped in services
- [ ] No direct DbContext access from services
- [ ] No entities exposed to components (only DTOs)
- [ ] **All UI components use MudBlazor** (no raw HTML inputs/buttons)
- [ ] CSS is in centralized files, not component-scoped
- [ ] No inline styles
- [ ] Error handling is implemented
- [ ] NULL checks are in place
- [ ] Using `InteractiveServer` render mode
- [ ] **Code is compatible with both Blazor Server and WebAssembly**
- [ ] No direct HttpContext access in components
- [ ] No server-only dependencies in shared code
- [ ] No new controller dependencies added

---

**Need Help?** Check [TROUBLESHOOTING.md](TROUBLESHOOTING.md) first!

**Last Updated**: October 29, 2025  
**Maintainer**: Jose Ojeda  
**Project**: Sivar.Os
