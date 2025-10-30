# Sivar.Os Development Rules & Guidelines

> **Last Updated**: October 30, 2025  
> **Project Type**: Blazor Server (Interactive Server Only)  
> **Target Framework**: .NET 9.0

---

## Table of Contents
1. [Project Architecture Overview](#project-architecture-overview)
2. [Blazor Configuration](#blazor-configuration)
3. [Service Layer Rules](#service-layer-rules)
4. [Repository Layer Rules](#repository-layer-rules)
5. [Controller Usage](#controller-usage)
6. [File Upload & Blob Storage](#file-upload--blob-storage) ⭐ **UPDATED**
   - Storage Configuration & Hierarchical Namespace
   - CORS & Mixed Content Solutions
   - Proxy Endpoint Implementation
   - URL Generation Strategy (Dynamic vs Stored)
   - GetFileUrlAsync - The Critical Metadata Loading Fix
   - Troubleshooting Guide & Common Issues
7. [CSS Organization & Styling](#css-organization--styling)
8. [Logging Standards](#logging-standards)
9. [Authentication & Authorization](#authentication--authorization)
10. [Error Handling](#error-handling)
11. [Testing & Debugging](#testing--debugging)
12. [References](#references)

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

## File Upload & Blob Storage

### ⭐ File Upload is a CRITICAL feature - Follow these patterns consistently

File upload functionality is a core feature of Sivar.Os, enabling users to attach images (JPG, PNG, GIF, WebP) to posts. The system uses Azure Blob Storage for file persistence and implements a server-side proxy to handle CORS/Mixed Content issues during development.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Blazor Component                          │
│                   (PostComposer.razor)                       │
│                  - InputFile component                       │
│                  - Preview generation                        │
│                  - File validation                           │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                   Parent Component                           │
│                      (Home.razor)                            │
│              - Upload files via FilesClient                  │
│              - Create post attachments                       │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                    FilesClient                               │
│                (Client & Server Implementations)             │
│              - UploadFileAsync                               │
│              - UploadBulkAsync                               │
│              - DeleteBulkAsync                               │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│              AzureBlobStorageService                         │
│              - UploadFileAsync                               │
│              - GeneratePublicUrl (with proxy detection)      │
│              - DeleteFileAsync                               │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                  Azure Blob Storage                          │
│              (Production) or Azurite (Development)           │
│                                                              │
│              ⬅️ PROXY ENDPOINT (Development Only) ⬅️          │
│              /api/blob-proxy/{container}/{blobPath}          │
│              Bypasses CORS/Mixed Content issues              │
└─────────────────────────────────────────────────────────────┘
```

### Storage Configuration

**Production:** Azure Blob Storage  
**Development:** Azurite (Local Azure Storage Emulator)

**Configuration (`appsettings.json`):**
```json
{
  "AzureBlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "BaseUrl": "https://localhost:7165",
    "UseHierarchicalNamespace": true  // ⭐ CRITICAL for blob organization
  }
}
```

**⚠️ CRITICAL: Hierarchical Namespace Setting**

The `UseHierarchicalNamespace` setting determines how blobs are named and organized:

| Setting | Blob Name Format | Use Case |
|---------|------------------|----------|
| `true` | `posts/{fileId}_{filename}` | **RECOMMENDED** - Better organization, folder-like structure |
| `false` | `{fileId}_{filename}` | Flat structure, all files in root |

**Why This Matters:**
- ✅ With hierarchical namespace: Blobs are stored in logical folders (`posts/`, `avatars/`, etc.)
- ❌ Without: All blobs are in the container root (messy with many files)
- 🔍 URL generation and blob search logic depend on this setting
- 🚨 Changing this setting requires updating ALL uploaded blob paths

### ⚠️ CRITICAL: CORS & Mixed Content Issue

**Problem:** When using HTTPS in development, browsers block HTTP resources from Azurite due to Mixed Content policy. Additionally, CORS errors occur when loading blob images.

**❌ WRONG Solutions (Don't Use):**
- Configuring Azurite CORS manually (complex, fragile, doesn't solve Mixed Content)
- Using HTTP instead of HTTPS (insecure, doesn't match production)
- Client-side CORS workarounds (don't work for Mixed Content)
- Storing raw Azurite URLs in database (breaks when moving to production)

**✅ CORRECT Solution: Server-Side Proxy + Dynamic URL Generation**

The solution has TWO critical components:

1. **Server-Side Proxy Endpoint** - Fetches images from Azurite server-side and serves via HTTPS
2. **Dynamic URL Generation** - Never stores raw URLs in database, generates proxy URLs on-the-fly

### ⚠️ CRITICAL LESSON: The Proxy Blob Path Must Match Storage Structure

**THE ROOT CAUSE OF ALL 404 ERRORS:**

When hierarchical namespace is enabled, blobs are stored as `posts/{fileId}_{filename}`, but the proxy was initially generating URLs without the `posts/` prefix, causing 404 errors.

**How It Should Work:**

| Storage Mode | Blob Name in Azurite | Proxy URL | Azurite URL |
|--------------|---------------------|-----------|-------------|
| Hierarchical (`true`) | `posts/abc123_image.jpg` | `/api/blob-proxy/sivaros-posts/posts/abc123_image.jpg` | `http://127.0.0.1:10000/devstoreaccount1/sivaros-posts/posts/abc123_image.jpg` |
| Flat (`false`) | `abc123_image.jpg` | `/api/blob-proxy/sivaros-posts/abc123_image.jpg` | `http://127.0.0.1:10000/devstoreaccount1/sivaros-posts/abc123_image.jpg` |

**Key Insight:** The proxy URL path segment must EXACTLY match the blob name in storage, including any prefix folders.

### Proxy Endpoint Implementation

**Location:** `Program.cs`

**⚠️ IMPORTANT:** The proxy endpoint uses a catch-all route parameter `{*blobPath}` to capture the ENTIRE blob path, including any folder prefixes like `posts/`.

```csharp
// ⭐ Blob Proxy Endpoint (Development - bypasses CORS/Mixed Content)
// Note: {*blobPath} captures the full path including folders (e.g., "posts/abc123_image.jpg")
app.MapGet("/api/blob-proxy/{container}/{*blobPath}", async (
    string container,
    string blobPath,
    HttpContext context,
    ILogger<Program> logger) =>
{
    logger.LogInformation(
        "[BlobProxy] Proxying request - Container={Container}, Path={Path}",
        container,
        blobPath);

    try
    {
        // Construct full Azurite URL with the exact blob path
        var azuriteUrl = $"http://127.0.0.1:10000/devstoreaccount1/{container}/{blobPath}";
        logger.LogInformation("[BlobProxy] Fetching from Azurite - URL={URL}", azuriteUrl);

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(azuriteUrl);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("[BlobProxy] Blob not found - StatusCode={StatusCode}, Path={Path}", 
                response.StatusCode, blobPath);
            return Results.NotFound();
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var stream = await response.Content.ReadAsStreamAsync();

        // Set cache headers for performance
        context.Response.Headers.CacheControl = "public, max-age=31536000"; // 1 year
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");

        return Results.Stream(stream, contentType);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[BlobProxy] Error proxying blob - Path={Path}", blobPath);
        return Results.Problem("Failed to load image");
    }
}).AllowAnonymous(); // Public images, no auth required
```

**Critical Details:**
- ✅ `{*blobPath}` captures full path including folders (e.g., `posts/abc123_image.jpg`)
- ✅ Constructs exact Azurite URL: `http://127.0.0.1:10000/devstoreaccount1/{container}/{blobPath}`
- ✅ No auth required - public images
- ✅ Caching headers for performance
- ✅ CORS headers to allow all origins

### URL Generation with Proxy Detection

**Location:** `AzureBlobStorageService.cs → GeneratePublicUrl()`

**⚠️ CRITICAL:** This method must account for hierarchical namespace when generating proxy URLs.

```csharp
private string GeneratePublicUrl(Uri blobUri, string container, string fileId, string fileName)
{
    _logger.LogDebug(
        "[GeneratePublicUrl] START - BlobUri={BlobUri}, Container={Container}, FileId={FileId}, FileName={FileName}",
        blobUri, container, fileId, fileName);

    // In development, use the blob proxy to avoid CORS issues with Azurite
    // In production, use the configured BaseUrl or blob URI directly
    
    if (!string.IsNullOrEmpty(_config.BaseUrl))
    {
        var url = $"{_config.BaseUrl.TrimEnd('/')}/{container}/{fileId}_{fileName}";
        _logger.LogInformation("[GeneratePublicUrl] Using BaseUrl - URL={URL}", url);
        return url;
    }

    // ⭐ CRITICAL: Check if running against Azurite (development)
    if (blobUri.Host.Contains("127.0.0.1") || blobUri.Host.Contains("localhost"))
    {
        // ⚠️ CRITICAL FIX: Include prefix if using hierarchical namespace
        // This ensures the proxy URL matches the actual blob path in storage
        var blobName = _config.UseHierarchicalNamespace 
            ? $"posts/{fileId}_{fileName}"      // With folder prefix
            : $"{fileId}_{fileName}";            // Flat structure
            
        var proxyUrl = $"/api/blob-proxy/{container}/{blobName}";
        
        _logger.LogInformation(
            "[GeneratePublicUrl] Detected Azurite, using proxy - Host={Host}, ProxyUrl={ProxyUrl}",
            blobUri.Host, proxyUrl);
        
        return proxyUrl;
    }

    // Production Azure Blob Storage URL
    var productionUrl = blobUri.ToString();
    _logger.LogInformation(
        "[GeneratePublicUrl] Using production blob URL - Host={Host}, URL={URL}",
        blobUri.Host, productionUrl);
    
    return productionUrl;
}
```

**Why This Fix Was Critical:**

❌ **Before (Broken):**
- Blob stored as: `posts/abc123_image.jpg`
- Proxy URL generated: `/api/blob-proxy/sivaros-posts/abc123_image.jpg`
- Azurite request: `http://127.0.0.1:10000/devstoreaccount1/sivaros-posts/abc123_image.jpg`
- Result: **404 Not Found** (blob is at `posts/abc123_image.jpg`, not `abc123_image.jpg`)

✅ **After (Fixed):**
- Blob stored as: `posts/abc123_image.jpg`
- Proxy URL generated: `/api/blob-proxy/sivaros-posts/posts/abc123_image.jpg`
- Azurite request: `http://127.0.0.1:10000/devstoreaccount1/sivaros-posts/posts/abc123_image.jpg`
- Result: **200 OK** (exact match!)

### Blob Upload and Naming Convention

**Location:** `AzureBlobStorageService.cs → UploadFileAsync()`

```csharp
public async Task<UploadFileResponseDto> UploadFileAsync(UploadFileRequestDto request)
{
    var fileId = Guid.NewGuid().ToString("N");
    
    // ⭐ CRITICAL: Blob name depends on UseHierarchicalNamespace setting
    var blobName = _config.UseHierarchicalNamespace
        ? $"posts/{fileId}_{request.FileName}"    // Hierarchical: posts/abc123_image.jpg
        : $"{fileId}_{request.FileName}";         // Flat: abc123_image.jpg
    
    var blobClient = containerClient.GetBlobClient(blobName);
    
    // Upload with metadata
    var metadata = new Dictionary<string, string>
    {
        { "file_id", fileId },
        { "original_filename", request.FileName },
        { "uploaded_at", DateTime.UtcNow.ToString("O") }
    };
    
    await blobClient.UploadAsync(
        new BinaryData(request.FileData),
        new BlobUploadOptions
        {
            Metadata = metadata,
            HttpHeaders = new BlobHttpHeaders { ContentType = request.MimeType }
        });
    
    // ⚠️ CRITICAL: Don't return raw Azurite URL, return empty or placeholder
    // The actual URL will be generated dynamically when needed
    return new UploadFileResponseDto
    {
        FileId = fileId,
        FilePath = "",  // Empty - URL generated dynamically
        FileName = request.FileName,
        MimeType = request.MimeType
    };
}
```

**Key Points:**
- Blob name format depends on `UseHierarchicalNamespace` setting
- Metadata stored with blob (`file_id`, `original_filename`, `uploaded_at`)
- Returns empty `FilePath` - URLs generated dynamically, not stored

### ⚠️ CRITICAL: URL Storage Strategy - Never Store Raw URLs

**THE PROBLEM WITH STORING URLS:**

When we first implemented file uploads, we stored the raw Azurite URLs in the database:
```sql
-- ❌ WRONG - Raw Azurite URLs in database
FilePath: "http://127.0.0.1:10000/devstoreaccount1/sivaros-posts/posts/abc123_image.jpg"
```

**Why This Was Broken:**
1. ❌ Mixed Content errors (HTTP URLs on HTTPS pages)
2. ❌ Environment-specific URLs (Azurite URLs don't work in production)
3. ❌ Can't switch storage providers without database migration
4. ❌ No proxy support (URLs bypass the proxy endpoint)

**✅ CORRECT SOLUTION: Dynamic URL Generation**

**What We Store in Database:**
```sql
-- ✅ CORRECT - Store placeholder or empty
FilePath: ""  -- Empty, or "blob://{fileId}/{filename}" placeholder
FileId: "abc123def456"  -- Always stored
FileName: "image.jpg"   -- Always stored
```

**How URLs are Generated:**

1. **During Upload** (`UploadFileAsync`):
   - Upload blob to storage with metadata
   - Return `FileId`, `FileName`, but **empty or placeholder FilePath**
   - Store attachment in database with empty FilePath

2. **During Display** (`MapAttachmentsToDtosAsync`):
   - For each attachment, call `GetFileUrlAsync(fileId)`
   - Search blob storage by `file_id` metadata
   - Generate appropriate URL (proxy for dev, direct for prod)
   - Return DTO with dynamically generated URL

**Benefits:**
- ✅ Works in development (generates proxy URLs)
- ✅ Works in production (generates direct URLs or BaseUrl)
- ✅ No database migration needed when changing storage
- ✅ No Mixed Content issues
- ✅ Environment-agnostic URLs

### GetFileUrlAsync - The URL Generator

**Location:** `AzureBlobStorageService.cs`

This method is CRITICAL - it finds blobs by metadata and generates the correct URL for the current environment.

```csharp
public async Task<string> GetFileUrlAsync(string fileId)
{
    var requestId = Guid.NewGuid().ToString("N");
    _logger.LogInformation(
        "[GetFileUrlAsync] START - RequestId={RequestId}, FileId={FileId}",
        requestId, fileId);

    try
    {
        _logger.LogInformation(
            "[GetFileUrlAsync] Starting container search - RequestId={RequestId}, FileId={FileId}",
            requestId, fileId);

        // ⭐ Search all containers for the blob
        foreach (var containerName in _config.Containers)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            
            _logger.LogInformation(
                "[GetFileUrlAsync] Searching container - RequestId={RequestId}, FileId={FileId}, Container={Container}",
                requestId, fileId, containerName);

            // ⚠️ CRITICAL: Search prefix depends on hierarchical namespace setting
            var searchPrefix = _config.UseHierarchicalNamespace
                ? $"posts/{fileId}"      // Hierarchical: "posts/abc123"
                : fileId;                 // Flat: "abc123"

            _logger.LogInformation(
                "[GetFileUrlAsync] Using search prefix - RequestId={RequestId}, Prefix={Prefix}, UseHierarchicalNamespace={UseHierarchicalNamespace}",
                requestId, searchPrefix, _config.UseHierarchicalNamespace);

            // ⚠️ CRITICAL: Must request BlobTraits.Metadata to load metadata
            var blobs = containerClient.GetBlobsAsync(
                traits: BlobTraits.Metadata,  // ⭐ CRITICAL - loads metadata
                prefix: searchPrefix);

            await foreach (var blob in blobs)
            {
                _logger.LogInformation(
                    "[GetFileUrlAsync] Found blob with prefix - RequestId={RequestId}, FileId={FileId}, BlobName={BlobName}, HasMetadata={HasMetadata}",
                    requestId, fileId, blob.Name, blob.Metadata?.Count > 0);

                // ⚠️ CRITICAL: Verify file_id metadata matches
                if (blob.Metadata?.ContainsKey("file_id") == true &&
                    blob.Metadata["file_id"] == fileId)
                {
                    var metadataFileId = blob.Metadata["file_id"];
                    _logger.LogInformation(
                        "[GetFileUrlAsync] Blob has file_id metadata - RequestId={RequestId}, FileId={FileId}, MetadataFileId={MetadataFileId}, Match={Match}",
                        requestId, fileId, metadataFileId, metadataFileId == fileId);

                    if (metadataFileId == fileId)
                    {
                        // Extract original filename from metadata
                        var originalFileName = blob.Metadata.ContainsKey("original_filename")
                            ? blob.Metadata["original_filename"]
                            : blob.Name.Split('_', 2).LastOrDefault() ?? "unknown";

                        _logger.LogInformation(
                            "[GetFileUrlAsync] File found - RequestId={RequestId}, FileId={FileId}, BlobName={BlobName}, Container={Container}",
                            requestId, fileId, blob.Name, containerName);

                        var blobClient = containerClient.GetBlobClient(blob.Name);
                        
                        // ⭐ Generate URL with proxy detection
                        var url = GeneratePublicUrl(blobClient.Uri, containerName, fileId, originalFileName);
                        
                        var duration = DateTime.UtcNow.Subtract(DateTime.UtcNow);
                        _logger.LogInformation(
                            "[GetFileUrlAsync] SUCCESS - RequestId={RequestId}, FileId={FileId}, OriginalFileName={OriginalFileName}, Duration={Duration}ms",
                            requestId, fileId, originalFileName, duration.TotalMilliseconds);

                        return url;
                    }
                }
            }
        }

        _logger.LogWarning(
            "[GetFileUrlAsync] File not found - RequestId={RequestId}, FileId={FileId}",
            requestId, fileId);

        throw new FileNotFoundException($"File with ID {fileId} not found in blob storage");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "[GetFileUrlAsync] FAILED - RequestId={RequestId}, FileId={FileId}, Error={Error}",
            requestId, fileId, ex.Message);
        throw;
    }
}
```

**Critical Steps in GetFileUrlAsync:**

1. **Search by Prefix:**
   - Uses hierarchical namespace setting to determine prefix
   - `posts/{fileId}` if hierarchical, `{fileId}` if flat

2. **⚠️ LOAD METADATA:**
   - **MUST use `BlobTraits.Metadata`** - without this, `blob.Metadata` is null!
   - This was the final bug that caused all 404 errors

3. **Verify file_id:**
   - Checks if `blob.Metadata["file_id"]` matches the requested `fileId`
   - Ensures we get the exact file, not a partial match

4. **Extract Original Filename:**
   - Gets `original_filename` from metadata
   - Fallback: parse from blob name

5. **Generate URL:**
   - Calls `GeneratePublicUrl()` which detects environment
   - Returns proxy URL for Azurite, direct URL for production

**Why BlobTraits.Metadata is Critical:**

```csharp
// ❌ WRONG - Metadata is null
var blobs = containerClient.GetBlobsAsync(prefix: searchPrefix);
await foreach (var blob in blobs)
{
    // blob.Metadata is NULL here! Check will fail!
    if (blob.Metadata?.ContainsKey("file_id") == true) { ... }
}

// ✅ CORRECT - Metadata is loaded
var blobs = containerClient.GetBlobsAsync(
    traits: BlobTraits.Metadata,  // ⭐ CRITICAL
    prefix: searchPrefix);
await foreach (var blob in blobs)
{
    // blob.Metadata is populated! Check succeeds!
    if (blob.Metadata?.ContainsKey("file_id") == true) { ... }
}
```

**The Azure SDK Quirk:**
- By default, `GetBlobsAsync()` only returns basic blob info (name, size, modified date)
- Metadata is **NOT loaded** unless you explicitly request it with `traits: BlobTraits.Metadata`
- This is for performance (metadata requires extra requests)
- **Result:** Without the flag, `blob.Metadata` is always null, even if the blob has metadata!

### PostService Integration - MapAttachmentsToDtosAsync

**Location:** `PostService.cs`

This method calls `GetFileUrlAsync` for each attachment to generate URLs dynamically.

```csharp
private async Task<List<PostAttachmentDto>> MapAttachmentsToDtosAsync(
    List<PostAttachment> attachments)
{
    var requestId = Guid.NewGuid().ToString("N");
    _logger.LogInformation(
        "[MapAttachmentsToDtosAsync] START - RequestId={RequestId}, PostId={PostId}",
        requestId, attachments.FirstOrDefault()?.PostId.ToString() ?? "NULL");

    if (!attachments.Any())
    {
        _logger.LogInformation(
            "[MapAttachmentsToDtosAsync] No attachments to map - RequestId={RequestId}",
            requestId);
        return new List<PostAttachmentDto>();
    }

    _logger.LogInformation(
        "[MapAttachmentsToDtosAsync] Retrieved {Count} attachments - RequestId={RequestId}, PostId={PostId}",
        attachments.Count, requestId, attachments.First().PostId);

    var attachmentDtos = new List<PostAttachmentDto>();

    foreach (var attachment in attachments)
    {
        try
        {
            // ⭐ CRITICAL: Generate URL dynamically via GetFileUrlAsync
            var fileUrl = await _blobStorageService.GetFileUrlAsync(attachment.FileId);

            attachmentDtos.Add(new PostAttachmentDto
            {
                Id = attachment.Id,
                FileId = attachment.FileId,
                FilePath = fileUrl,  // ✅ Dynamically generated URL
                FileName = attachment.FileName,
                MimeType = attachment.MimeType,
                FileSize = attachment.FileSize
            });

            _logger.LogDebug(
                "[MapAttachmentsToDtosAsync] Mapped attachment - RequestId={RequestId}, AttachmentId={AttachmentId}, FileId={FileId}, URL={URL}",
                requestId, attachment.Id, attachment.FileId, fileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MapAttachmentsToDtosAsync] Failed to generate URL for attachment - RequestId={RequestId}, AttachmentId={AttachmentId}, FileId={FileId}",
                requestId, attachment.Id, attachment.FileId);

            // ⚠️ Use error placeholder if URL generation fails
            attachmentDtos.Add(new PostAttachmentDto
            {
                Id = attachment.Id,
                FileId = attachment.FileId,
                FilePath = $"/api/file-not-found/{attachment.FileId}",  // Error placeholder
                FileName = attachment.FileName,
                MimeType = attachment.MimeType,
                FileSize = attachment.FileSize
            });
        }
    }

    var duration = DateTime.UtcNow.Subtract(DateTime.UtcNow);
    _logger.LogInformation(
        "[MapAttachmentsToDtosAsync] SUCCESS - RequestId={RequestId}, PostId={PostId}, MappedCount={Count}, Duration={Duration}ms",
        requestId, attachments.First().PostId, attachmentDtos.Count, duration.TotalMilliseconds);

    return attachmentDtos;
}
```

**Key Points:**
- ✅ Calls `GetFileUrlAsync()` for each attachment
- ✅ Uses dynamically generated URL, not stored URL
- ✅ Error handling with placeholder URL on failure
- ✅ Comprehensive logging for debugging
        var publicUrl = $"{_config.BaseUrl}/blobs/{containerName}/{fileId}_{fileName}";
        
        _logger.LogDebug(
            "[GeneratePublicUrl] Using BaseUrl - PublicUrl={PublicUrl}",
            publicUrl);
        
        return publicUrl;
    }

    _logger.LogDebug(
        "[GeneratePublicUrl] Using direct blob URI - BlobUri={BlobUri}",
        blobUri.ToString());

    return blobUri.ToString();
}
```

### File Upload Component Pattern

**Component:** `PostComposer.razor`

```razor
@* File Upload UI *@
<div class="post-composer__file-upload">
    <label for="imageUpload" class="file-upload-label">
        <MudIconButton Icon="@Icons.Material.Filled.Image" 
                      Color="Color.Primary" 
                      Size="Size.Small"
                      aria-label="Upload images" />
    </label>
    <InputFile id="imageUpload" 
               OnChange="OnFilesSelected" 
               accept="image/*" 
               multiple 
               style="display: none;" />
</div>

@* Image Previews *@
@if (PreviewUrls.Any())
{
    <div class="image-preview-container">
        @foreach (var preview in PreviewUrls)
        {
            <div class="image-preview">
                <img src="@preview.Value" alt="Preview" />
                
                @* GIF Badge *@
                @if (preview.Key.ContentType == "image/gif")
                {
                    <div class="gif-badge">GIF</div>
                }
                
                <MudIconButton Icon="@Icons.Material.Filled.Close" 
                              Size="Size.Small" 
                              OnClick="@(() => RemoveImage(preview.Key))" 
                              Class="remove-image-btn" />
            </div>
        }
    </div>
}

@code {
    [Parameter]
    public List<IBrowserFile> SelectedFiles { get; set; } = new();
    
    private Dictionary<IBrowserFile, string> PreviewUrls { get; set; } = new();
    
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private const int MaxFiles = 4;

    private async Task OnFilesSelected(InputFileChangeEventArgs e)
    {
        var newFiles = e.GetMultipleFiles(MaxFiles);
        
        foreach (var file in newFiles)
        {
            // ✅ Validation
            if (!file.ContentType.StartsWith("image/"))
            {
                // Show error: Only images allowed
                continue;
            }
            
            if (file.Size > MaxFileSize)
            {
                // Show error: File too large
                continue;
            }
            
            if (SelectedFiles.Count >= MaxFiles)
            {
                // Show error: Max files reached
                break;
            }
            
            SelectedFiles.Add(file);
        }
        
        // ✅ Generate previews (chunked to avoid UI freezing)
        await GeneratePreviewUrlsAsync();
    }

    private async Task GeneratePreviewUrlsAsync()
    {
        PreviewUrls.Clear();
        
        foreach (var file in SelectedFiles)
        {
            try
            {
                // ⭐ CRITICAL: Read in chunks to avoid UI freezing
                const int bufferSize = 8192; // 8KB chunks
                const long maxPreviewSize = 200 * 1024; // 200KB max for preview
                
                using var stream = file.OpenReadStream(MaxFileSize);
                using var memoryStream = new MemoryStream();
                
                var buffer = new byte[bufferSize];
                int bytesRead;
                long totalRead = 0;
                
                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;
                    
                    // Yield to UI every 16KB
                    if (totalRead % (bufferSize * 2) == 0)
                    {
                        await Task.Delay(1);
                        StateHasChanged();
                    }
                    
                    // Limit preview size
                    if (totalRead >= maxPreviewSize)
                        break;
                }
                
                var base64 = Convert.ToBase64String(memoryStream.ToArray());
                var dataUrl = $"data:{file.ContentType};base64,{base64}";
                
                PreviewUrls[file] = dataUrl;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[GeneratePreviewUrlsAsync] Failed to generate preview");
            }
        }
        
        StateHasChanged();
    }

    private void RemoveImage(IBrowserFile file)
    {
        SelectedFiles.Remove(file);
        PreviewUrls.Remove(file);
        StateHasChanged();
    }
}
```

### File Upload Handler Pattern

**Component:** `Home.razor`

```razor
@code {
    private List<IBrowserFile> _selectedFiles = new();

    private async Task HandlePostSubmitAsync(string content)
    {
        Logger.LogInformation("[HandlePostSubmitAsync] Starting post submission");
        
        var attachments = new List<CreatePostAttachmentDto>();
        
        // ⭐ Upload files BEFORE creating post
        if (_selectedFiles.Any())
        {
            Logger.LogInformation(
                "[HandlePostSubmitAsync] Uploading {Count} files",
                _selectedFiles.Count);
            
            foreach (var file in _selectedFiles)
            {
                try
                {
                    // Read file stream
                    using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    
                    var fileBytes = memoryStream.ToArray();
                    
                    // Upload to blob storage
                    var uploadResult = await SivarClient.Files.UploadFileAsync(
                        fileBytes,
                        file.Name,
                        file.ContentType);
                    
                    if (uploadResult != null)
                    {
                        // ✅ Create attachment DTO
                        attachments.Add(new CreatePostAttachmentDto
                        {
                            FileId = uploadResult.FileId,
                            FilePath = uploadResult.FilePath,
                            FileName = uploadResult.FileName,
                            MimeType = uploadResult.MimeType,
                            FileSize = uploadResult.FileSize
                        });
                        
                        Logger.LogInformation(
                            "[HandlePostSubmitAsync] File uploaded - FileId={FileId}",
                            uploadResult.FileId);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, 
                        "[HandlePostSubmitAsync] Failed to upload file - FileName={FileName}",
                        file.Name);
                }
            }
        }
        
        // Create post with attachments
        var createPostDto = new CreatePostDto
        {
            Content = content,
            Visibility = PostVisibility.Public,
            Attachments = attachments
        };
        
        var result = await PostService.CreatePostAsync(keycloakId, createPostDto);
        
        if (result != null)
        {
            _selectedFiles.Clear();
            await LoadFeedAsync(); // Refresh feed
        }
    }
}
```

### File Upload Service Pattern

**Service:** `PostService.ProcessPostAttachmentsAsync`

```csharp
private async Task ProcessPostAttachmentsAsync(
    Post post, 
    List<CreatePostAttachmentDto> attachments)
{
    if (attachments == null || !attachments.Any())
        return;

    _logger.LogInformation(
        "[ProcessPostAttachmentsAsync] Processing {Count} attachments for PostId={PostId}",
        attachments.Count,
        post.Id);

    foreach (var attachmentDto in attachments)
    {
        var postAttachment = new PostAttachment
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            FileId = attachmentDto.FileId,
            FilePath = attachmentDto.FilePath,
            FileName = attachmentDto.FileName,
            MimeType = attachmentDto.MimeType,
            FileSize = attachmentDto.FileSize,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _postAttachmentRepository.AddAsync(postAttachment);
        
        _logger.LogInformation(
            "[ProcessPostAttachmentsAsync] Added attachment - AttachmentId={AttachmentId}, FileId={FileId}",
            postAttachment.Id,
            postAttachment.FileId);
    }

    // ⭐ CRITICAL: Must save changes to persist attachments
    await _postAttachmentRepository.SaveChangesAsync();
    
    _logger.LogInformation(
        "[ProcessPostAttachmentsAsync] Saved {Count} attachments to database",
        attachments.Count);
}
```

### Display Pattern with Carousel

**Component:** `PostCard.razor`

```razor
@if (Post.Attachments != null && Post.Attachments.Any())
{
    @if (Post.Attachments.Count == 1)
    {
        @* Single Image *@
        <div class="single-image">
            <img src="@Post.Attachments[0].FilePath" 
                 alt="Post image" 
                 loading="lazy" />
            
            @if (Post.Attachments[0].MimeType == "image/gif")
            {
                <div class="gif-badge">GIF</div>
            }
        </div>
    }
    else
    {
        @* Multiple Images - MudCarousel *@
        <MudCarousel Class="post-carousel" 
                     ShowArrows="true" 
                     ShowBullets="true" 
                     EnableSwipeGesture="true"
                     AutoCycle="false">
            @foreach (var attachment in Post.Attachments)
            {
                <MudCarouselItem>
                    <div class="carousel-image-container">
                        <img src="@attachment.FilePath" 
                             alt="Post image" 
                             class="carousel-image"
                             loading="lazy" />
                        
                        @if (attachment.MimeType == "image/gif")
                        {
                            <div class="gif-badge">GIF</div>
                        }
                    </div>
                </MudCarouselItem>
            }
        </MudCarousel>
    }
}
```

### File Upload Validation Rules

| Validation | Rule | Error Message |
|------------|------|---------------|
| **File Type** | `image/*` (JPG, PNG, GIF, WebP) | "Only image files are allowed" |
| **File Size** | Max 10MB per file | "File size exceeds 10MB limit" |
| **File Count** | Max 4 files per post | "Maximum 4 images per post" |
| **Total Size** | No explicit limit (but 4 × 10MB = 40MB max) | N/A |

### CSS Styling for File Upload

**File:** `wireframe-components.css`

```css
/* Image Preview (Composer) */
.image-preview-container {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(100px, 1fr));
    gap: var(--spacing-sm);
    margin-top: var(--spacing-sm);
}

.image-preview {
    position: relative;
    border-radius: var(--border-radius);
    overflow: hidden;
    aspect-ratio: 1 / 1;
}

.image-preview img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}

.remove-image-btn {
    position: absolute;
    top: 4px;
    right: 4px;
    background: rgba(0, 0, 0, 0.6);
    color: white;
}

/* Single Image Display */
.single-image {
    position: relative;
    width: 100%;
    border-radius: var(--border-radius);
    overflow: hidden;
}

.single-image img {
    width: 100%;
    height: auto;
    display: block;
}

/* Carousel Display */
.post-carousel {
    width: 100%;
    border-radius: var(--border-radius);
    overflow: hidden;
}

.carousel-image-container {
    position: relative;
    width: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
}

.carousel-image {
    width: 100%;
    height: auto;
    max-height: 500px;
    object-fit: contain;
}

/* GIF Badge */
.gif-badge {
    position: absolute;
    top: 8px;
    left: 8px;
    background: rgba(76, 175, 80, 0.9);
    color: white;
    padding: 4px 8px;
    border-radius: 4px;
    font-size: 12px;
    font-weight: bold;
    border: 2px solid #4caf50;
    animation: pulse 2s infinite;
}

@keyframes pulse {
    0%, 100% { opacity: 1; }
    50% { opacity: 0.7; }
}
```

### File Upload Checklist

Before implementing file upload in a new feature:

- [ ] **Configure hierarchical namespace** (`UseHierarchicalNamespace` in appsettings.json)
- [ ] **Use `InputFile` component** (native Blazor, works on Server & WebAssembly)
- [ ] **Validate file type, size, and count** (prevent invalid uploads)
- [ ] **Generate previews in chunks** (8KB buffer, yield to UI, max 200KB preview)
- [ ] **Upload files BEFORE creating entity** (parent component responsibility)
- [ ] **Use `FilesClient` for uploads** (abstraction over storage service)
- [ ] **Store metadata with blobs** (`file_id`, `original_filename`, `uploaded_at`)
- [ ] **DON'T store raw URLs** (store empty FilePath or placeholder)
- [ ] **Use GetFileUrlAsync for display** (generates URLs dynamically)
- [ ] **Include BlobTraits.Metadata in searches** (CRITICAL - loads metadata)
- [ ] **Match search prefix to namespace setting** (hierarchical vs flat)
- [ ] **Match proxy URL to blob path** (include folder prefix if hierarchical)
- [ ] **Save attachments to database** (call `SaveChangesAsync()` in repository)
- [ ] **Display with MudCarousel** (for multiple images) or single image div
- [ ] **Add GIF badges** (when `MimeType == "image/gif"`)
- [ ] **Add CSS styles** (in `wireframe-components.css`, not component-scoped)
- [ ] **Log all operations** (upload start, success, failures with file names)

### Debugging File Upload Issues - Complete Guide

#### Step 1: Check Blob Storage

**Verify files uploaded to Azurite:**

Use Azure Storage Explorer or command line:
```bash
# List all blobs in container
az storage blob list --container-name sivaros-posts --connection-string "UseDevelopmentStorage=true"

# Check if specific file exists
az storage blob show --container-name sivaros-posts --name "posts/abc123_image.jpg" --connection-string "UseDevelopmentStorage=true"
```

**What to look for:**
- ✅ Blob exists with correct name format
- ✅ Blob has metadata (`file_id`, `original_filename`, `uploaded_at`)
- ✅ Blob name matches hierarchical namespace setting

#### Step 2: Check Database

**Verify attachments in database:**

```sql
-- Check if attachment records exist
SELECT 
    "Id", 
    "PostId", 
    "FileId", 
    "FilePath",  -- Should be empty or placeholder
    "FileName", 
    "MimeType"
FROM "Sivar_PostAttachments"
WHERE "IsDeleted" = false
ORDER BY "CreatedAt" DESC;
```

**What to look for:**
- ✅ Attachment record exists with correct `FileId`
- ✅ `FilePath` is empty or placeholder (not raw HTTP URL)
- ✅ `FileName` and `MimeType` are correct

#### Step 3: Check Server Logs

**Enable detailed logging in `appsettings.Development.json`:**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Sivar.Os.Services.AzureBlobStorageService": "Debug",
        "Sivar.Os.Services.PostService": "Debug"
      }
    }
  }
}
```

**Key log patterns to search for:**

```
# Upload succeeded?
[UploadFileAsync] File uploaded successfully - FileId={fileId}

# GetFileUrlAsync called?
[GetFileUrlAsync] START - FileId={fileId}

# Search prefix correct?
[GetFileUrlAsync] Using search prefix - Prefix={prefix}, UseHierarchicalNamespace={bool}

# Blob found?
[GetFileUrlAsync] Found blob with prefix - BlobName={blobName}

# Metadata loaded? (CRITICAL)
[GetFileUrlAsync] Blob has file_id metadata - MetadataFileId={metadataFileId}

# URL generated?
[GeneratePublicUrl] Detected Azurite, using proxy - ProxyUrl={proxyUrl}

# File found?
[GetFileUrlAsync] SUCCESS - FileId={fileId}, OriginalFileName={fileName}
```

**Missing log = problem:**

| Missing Log | Problem | Fix |
|-------------|---------|-----|
| "Blob has file_id metadata" | Metadata not loaded | Add `traits: BlobTraits.Metadata` to `GetBlobsAsync()` |
| "Found blob with prefix" | Wrong search prefix | Check `UseHierarchicalNamespace` setting matches blob naming |
| "Using proxy" | URL generation failed | Check `GeneratePublicUrl()` logic |
| "File uploaded successfully" | Upload failed | Check `UploadFileAsync()` error handling |

#### Step 4: Check Browser Console

**Open browser DevTools (F12) → Console tab**

**Look for errors:**

```
❌ Mixed Content: The page at 'https://localhost:7165/' was loaded over HTTPS, but requested an insecure resource 'http://127.0.0.1:10000/...'. This request has been blocked.

→ Problem: Storing raw Azurite URLs instead of using proxy
→ Fix: Ensure FilePath is empty in database, GetFileUrlAsync generates proxy URLs

❌ Failed to load resource: net::ERR_FAILED http://127.0.0.1:10000/devstoreaccount1/sivaros-posts/abc123_image.jpg

→ Problem: Trying to load HTTP resource on HTTPS page
→ Fix: Use proxy endpoint (/api/blob-proxy/...)

❌ GET https://localhost:7165/api/blob-proxy/sivaros-posts/abc123_image.jpg 404 (Not Found)

→ Problem: Proxy URL doesn't match blob path (missing "posts/" prefix)
→ Fix: Update GeneratePublicUrl to include folder prefix when hierarchical namespace enabled

✅ GET https://localhost:7165/api/blob-proxy/sivaros-posts/posts/abc123_image.jpg 200 (OK)

→ Success! Proxy URL matches blob path exactly
```

#### Step 5: Check Network Tab

**Open browser DevTools (F12) → Network tab**

**Filter by "Img" to see image requests:**

**What to look for:**

| Request URL | Status | Diagnosis |
|------------|--------|-----------|
| `http://127.0.0.1:10000/...` | (blocked) | ❌ Raw Azurite URL - Mixed Content error |
| `/api/blob-proxy/.../abc123_image.jpg` | 404 | ❌ Missing folder prefix in proxy URL |
| `/api/blob-proxy/.../posts/abc123_image.jpg` | 200 | ✅ Correct! |
| `/api/blob-proxy/.../posts/abc123_image.jpg` | 500 | ❌ Server error - check logs |

#### Common Error Patterns

**Error 1: "Blob not found" but blob exists in storage**

**Symptoms:**
- Logs show: `[GetFileUrlAsync] File not found`
- Blob exists in Azure Storage Explorer
- No "Found blob with prefix" log

**Causes & Fixes:**

| Cause | Fix |
|-------|-----|
| Wrong search prefix | Check `UseHierarchicalNamespace` matches blob naming |
| Metadata not loaded | Add `traits: BlobTraits.Metadata` to `GetBlobsAsync()` |
| file_id mismatch | Verify `file_id` metadata matches `FileId` in database |

**Error 2: "404 Not Found" from proxy endpoint**

**Symptoms:**
- Browser console shows 404 for `/api/blob-proxy/...`
- Logs show: `[BlobProxy] Blob not found`

**Causes & Fixes:**

| Cause | Fix |
|-------|-----|
| Proxy URL missing folder prefix | Update `GeneratePublicUrl()` to include `posts/` when hierarchical |
| Blob name doesn't match URL | Check blob name in storage vs URL path |
| Wrong container name | Verify container name in proxy URL |

**Error 3: Mixed Content errors**

**Symptoms:**
- Browser blocks HTTP requests
- Console shows "Mixed Content" warning

**Causes & Fixes:**

| Cause | Fix |
|-------|-----|
| Raw Azurite URLs in database | Clear `FilePath` in database, use dynamic URL generation |
| `GeneratePublicUrl()` returning HTTP URL | Check Azurite detection logic, ensure proxy URL returned |

### Troubleshooting Flowchart

```
File not displaying?
    ↓
Check browser console
    ↓
Mixed Content error? → Use proxy URLs (check GeneratePublicUrl)
404 Not Found? → Check proxy URL path matches blob name
    ↓
Check server logs
    ↓
"File not found"? → Check search prefix + metadata loading
"Blob not found"? → Check blob exists in storage
    ↓
Check database
    ↓
Attachment exists? → Check FileId matches blob metadata
FilePath not empty? → Clear it, use dynamic URLs
    ↓
Check blob storage
    ↓
Blob exists? → Check metadata (file_id, original_filename)
Blob name correct? → Match hierarchical namespace setting
```

### Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| **CORS errors** | HTTPS app loading HTTP Azurite resources | ✅ Proxy endpoint automatically handles this |
| **Mixed Content warnings** | Browser blocking HTTP on HTTPS page | ✅ Proxy serves via HTTPS |
| **404 from proxy** | Proxy URL doesn't match blob path | ✅ Include folder prefix when hierarchical namespace enabled |
| **Metadata is null** | Missing `BlobTraits.Metadata` flag | ✅ Add `traits: BlobTraits.Metadata` to `GetBlobsAsync()` |
| **File not found** | Wrong search prefix | ✅ Match prefix to `UseHierarchicalNamespace` setting |
| **Attachments not in database** | Missing `SaveChangesAsync()` | ✅ Add `await _repository.SaveChangesAsync()` after loop |
| **UI freezing during preview** | Loading entire 10MB file at once | ✅ Read in 8KB chunks with `await Task.Delay(1)` |
| **Old posts have HTTP URLs** | Migration from old storage pattern | ✅ Delete old test data or migrate FilePath to empty |

### Key Lessons Learned - File Upload Implementation

**🎯 Critical Insights from Implementation:**

1. **Never Store Environment-Specific URLs**
   - ❌ Don't store raw Azurite URLs (`http://127.0.0.1:10000/...`)
   - ❌ Don't store production URLs (breaks when switching environments)
   - ✅ Store empty `FilePath` or placeholder (`blob://{fileId}/{filename}`)
   - ✅ Generate URLs dynamically with `GetFileUrlAsync()`

2. **Hierarchical Namespace is Not Optional**
   - 🔧 Setting: `UseHierarchicalNamespace` in configuration
   - 📁 Affects: Blob naming, search prefixes, proxy URLs
   - ⚠️ Must be consistent across upload, search, and URL generation
   - 🎯 **Every blob-related method must check this setting**

3. **Azure SDK Metadata Loading is NOT Automatic**
   - 🐛 By default, `GetBlobsAsync()` does NOT load metadata
   - ❌ Without flag: `blob.Metadata` is always null
   - ✅ With flag: `traits: BlobTraits.Metadata` loads metadata
   - 🔍 This was the final bug causing all 404 errors

4. **Proxy URL Path Must Exactly Match Blob Name**
   - 🎯 Blob name: `posts/abc123_image.jpg`
   - ✅ Proxy URL: `/api/blob-proxy/sivaros-posts/posts/abc123_image.jpg`
   - ❌ Wrong: `/api/blob-proxy/sivaros-posts/abc123_image.jpg` (missing `posts/`)
   - 🔍 Use `{*blobPath}` in route to capture full path

5. **The Full Chain Must Be Consistent**
   ```
   UploadFileAsync → Blob name with folder prefix
          ↓
   Store in database → Empty FilePath, store FileId
          ↓
   GetFileUrlAsync → Search with correct prefix + load metadata
          ↓
   GeneratePublicUrl → Include folder prefix in proxy URL
          ↓
   Proxy endpoint → Request exact blob path from Azurite
          ↓
   Browser displays → No CORS, no Mixed Content, no 404
   ```

6. **Comprehensive Logging Saved the Day**
   - 📝 Every method logs START, key decisions, SUCCESS/FAILED
   - 🔍 Logs revealed: "Found blob" but "File not found" → metadata null
   - 🎯 Pattern: Missing log = missing functionality
   - ✅ Add log before/after critical operations

7. **Development vs Production Environments**
   - 🏗️ Development: Azurite (HTTP) + Proxy (HTTPS)
   - ☁️ Production: Azure Blob Storage (HTTPS) + Direct URLs
   - 🎯 Code must work in both without changes
   - ✅ `GeneratePublicUrl()` automatically detects environment

**📚 Documentation is Critical:**
- Every complex feature needs comprehensive documentation
- Include WHY, not just HOW
- Document common errors and solutions
- Include troubleshooting flowcharts
- Update immediately when patterns change

**🚀 Future Improvements:**
- Automated tests for blob upload/retrieval
- Migration tool for old URL formats
- Blob cleanup job for soft-deleted files
- Image optimization/resizing pipeline
- CDN integration for production

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

### ⭐ Logging is MANDATORY for ALL code (Services, Repositories, Components)

### ⚠️ CRITICAL: NEVER use Console.WriteLine - ALWAYS use ILogger

**Console.WriteLine is BANNED** in production code. Use `ILogger<T>` injection for all logging.

### Why ILogger Instead of Console?

| Feature | Console.WriteLine | ILogger |
|---------|------------------|---------|
| **Log Levels** | ❌ No levels | ✅ Info, Warning, Error, Debug |
| **Structured Logging** | ❌ String only | ✅ Named parameters, searchable |
| **Production Ready** | ❌ Not configurable | ✅ File, database, cloud sinks |
| **Filtering** | ❌ Can't filter | ✅ Filter by level, category |
| **Performance** | ❌ Synchronous only | ✅ Async, buffered |
| **Context** | ❌ No context | ✅ Timestamps, thread, machine |
| **Searchability** | ❌ Hard to search | ✅ Structured, queryable |

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

#### ❌ NEVER Use Console.WriteLine

```csharp
// ❌ BANNED - Do NOT use Console.WriteLine
Console.WriteLine("User created");
Console.WriteLine($"User ID: {userId}");

// ❌ BANNED - Even in components
@code {
    private void DoSomething()
    {
        Console.WriteLine("Doing something");  // ❌ WRONG!
    }
}
```

#### ✅ ALWAYS Use ILogger

**In Services:**
```csharp
public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task CreateUserAsync(string email)
    {
        _logger.LogInformation("Creating user: {Email}", email);  // ✅ CORRECT
    }
}
```

**In Repositories:**
```csharp
public class UserRepository : IUserRepository
{
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(SivarDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Getting user by ID: {UserId}", id);  // ✅ CORRECT
        return await _context.Users.FindAsync(id);
    }
}
```

**In Blazor Components:**
```razor
@inject ILogger<MyComponent> Logger

@code {
    private async Task LoadData()
    {
        Logger.LogInformation("Loading data for component");  // ✅ CORRECT
        
        try
        {
            var data = await Service.GetDataAsync();
            Logger.LogInformation("Data loaded: {Count} items", data.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load data");
        }
    }
}
```

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
- **ALWAYS inject `ILogger<T>` in services, repositories, and components**
- Use structured logging with named parameters: `{keycloakId}`, `{userId}`
- Include method name in brackets: `[CreatePostAsync]`
- Log START, key decisions, and SUCCESS/FAILED
- Use NULL-safe logging: `userId?.ToString() ?? "NULL"`
- Log parameters that help with debugging
- Log all exceptions with `LogError(ex, ...)`
- Use appropriate log levels (Information, Warning, Error, Debug)

❌ **DON'T:**
- **NEVER use `Console.WriteLine` or `Console.Write`** - Use `ILogger` instead
- Don't use string interpolation: ~~`$"User {userId}"`~~ - Use structured logging
- Don't log sensitive data (passwords, tokens, credit cards)
- Don't log excessively in tight loops
- Don't skip exception logging
- Don't use generic error messages

### Logging in Different Layers

#### Services (Business Logic Layer)

```csharp
using Microsoft.Extensions.Logging;

public class PostService : IPostService
{
    private readonly IPostRepository _repository;
    private readonly ILogger<PostService> _logger;  // ✅ REQUIRED

    public PostService(
        IPostRepository repository,
        ILogger<PostService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));  // ✅ Validate
    }

    public async Task<PostDto?> CreatePostAsync(string keycloakId, CreatePostDto dto)
    {
        _logger.LogInformation(
            "[CreatePostAsync] START - KeycloakId={KeycloakId}, Content={ContentLength}",
            keycloakId,
            dto.Content?.Length ?? 0);

        try
        {
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
            
            if (user == null)
            {
                _logger.LogWarning(
                    "[CreatePostAsync] User not found - KeycloakId={KeycloakId}",
                    keycloakId);
                return null;
            }

            _logger.LogInformation(
                "[CreatePostAsync] User found - UserId={UserId}",
                user.Id);

            // Business logic...
            
            _logger.LogInformation(
                "[CreatePostAsync] SUCCESS - PostId={PostId}",
                newPost.Id);

            return postDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[CreatePostAsync] FAILED - KeycloakId={KeycloakId}",
                keycloakId);
            throw;
        }
    }
}
```

#### Repositories (Data Access Layer)

```csharp
using Microsoft.Extensions.Logging;

public class PostRepository : IPostRepository
{
    private readonly SivarDbContext _context;
    private readonly ILogger<PostRepository> _logger;  // ✅ REQUIRED

    public PostRepository(
        SivarDbContext context,
        ILogger<PostRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));  // ✅ Validate
    }

    public async Task<Post?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug(
            "[GetByIdAsync] Retrieving post - PostId={PostId}",
            id);

        var post = await _context.Posts
            .Where(p => p.Id == id && !p.IsDeleted)
            .Include(p => p.Profile)
            .FirstOrDefaultAsync();

        _logger.LogDebug(
            "[GetByIdAsync] Post retrieved - PostId={PostId}, Found={Found}",
            id,
            post != null);

        return post;
    }

    public async Task<Post> CreateAsync(Post post)
    {
        _logger.LogDebug(
            "[CreateAsync] Creating post - ProfileId={ProfileId}",
            post.ProfileId);

        post.CreatedAt = DateTime.UtcNow;
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[CreateAsync] Post created - PostId={PostId}",
            post.Id);

        return post;
    }
}
```

#### Blazor Components

```razor
@using Microsoft.Extensions.Logging
@inject ILogger<PostCard> Logger  @* ✅ REQUIRED - Inject logger *@
@inject IPostService PostService

<article class="post-card">
    <!-- Component markup -->
</article>

@code {
    [Parameter, EditorRequired]
    public PostDto Post { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation(
            "[OnInitializedAsync] Initializing PostCard - PostId={PostId}",
            Post.Id);

        try
        {
            await LoadCommentsAsync();
            
            Logger.LogInformation(
                "[OnInitializedAsync] PostCard initialized successfully - PostId={PostId}",
                Post.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "[OnInitializedAsync] Failed to initialize PostCard - PostId={PostId}",
                Post.Id);
        }
    }

    private async Task HandleAuthorClick()
    {
        var handle = Post.Profile?.Handle ?? string.Empty;
        
        Logger.LogInformation(
            "[HandleAuthorClick] Author clicked - Handle={Handle}, DisplayName={DisplayName}",
            handle,
            Post.Profile?.DisplayName);

        if (string.IsNullOrEmpty(handle))
        {
            Logger.LogWarning(
                "[HandleAuthorClick] Handle is empty - PostId={PostId}",
                Post.Id);
            return;
        }

        await OnAuthorClick.InvokeAsync(handle);
        
        Logger.LogInformation(
            "[HandleAuthorClick] Navigation invoked - Handle={Handle}",
            handle);
    }
}
```

### Logging Configuration by Layer

**Services:**
- Use `LogInformation` for main operations
- Use `LogWarning` for business rule violations
- Use `LogError` for exceptions

**Repositories:**
- Use `LogDebug` for data access operations (can be disabled in production)
- Use `LogInformation` for significant database operations (create, update, delete)
- Let exceptions bubble up to services (don't catch and log here)

**Components:**
- Use `LogInformation` for user interactions and navigation
- Use `LogWarning` for validation issues
- Use `LogError` for exceptions that prevent rendering

### Structured Logging Rules

### Log File Location

```
Sivar.Os/logs/
├── sivar-20251028.txt
├── sivar-20251029.txt
└── ...
```

Configured in `appsettings.json`.

### Where Logs Appear

| Code Location | Log Output |
|--------------|------------|
| Services (PostService, UserService, etc.) | Server terminal/console + log files |
| Repositories (PostRepository, etc.) | Server terminal/console + log files |
| Blazor Components (PostCard, Home, etc.) | Server terminal/console + log files |
| Client-side JavaScript (if needed) | Browser console only |

**Note:** Since we use Blazor Server (not WebAssembly), all `ILogger` logs appear in the **server terminal/console**, NOT the browser console.

### Enabling Debug Logs

To see detailed `LogDebug` messages in development, update `appsettings.Development.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  }
}
```

For production (`appsettings.json`):

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    }
  }
}
```

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

- [ ] **Services use `ILogger<T>`, NOT `Console.WriteLine`**
- [ ] **Repositories use `ILogger<T>`, NOT `Console.WriteLine`**
- [ ] **Components use `ILogger<T>`, NOT `Console.WriteLine`**
- [ ] Services have comprehensive logging (START, key decisions, SUCCESS/FAILED)
- [ ] All repository calls are wrapped in services
- [ ] No direct DbContext access from services
- [ ] No entities exposed to components (only DTOs)
- [ ] **All UI components use MudBlazor** (no raw HTML inputs/buttons)
- [ ] CSS is in centralized files, not component-scoped
- [ ] No inline styles
- [ ] Error handling is implemented with proper logging
- [ ] NULL checks are in place
- [ ] Using `InteractiveServer` render mode
- [ ] **Code is compatible with both Blazor Server and WebAssembly**
- [ ] No direct HttpContext access in components
- [ ] No server-only dependencies in shared code
- [ ] No new controller dependencies added

**File Upload Specific (if applicable):**
- [ ] **UseHierarchicalNamespace** setting configured in appsettings.json
- [ ] **File uploads use InputFile** component
- [ ] **Blob metadata includes** `file_id`, `original_filename`, `uploaded_at`
- [ ] **FilePath stored as empty or placeholder** (NOT raw URLs)
- [ ] **GetFileUrlAsync uses** `traits: BlobTraits.Metadata` flag
- [ ] **Search prefix matches** hierarchical namespace setting
- [ ] **Proxy URL includes folder prefix** when hierarchical namespace enabled
- [ ] **GeneratePublicUrl detects environment** (Azurite vs production)
- [ ] **File attachments saved to database** (with SaveChangesAsync)
- [ ] **Comprehensive logging** at all blob operations
- [ ] **Error handling** with placeholder URLs on failures

---

**Need Help?** Check [TROUBLESHOOTING.md](TROUBLESHOOTING.md) first!

**Last Updated**: October 30, 2025  
**Maintainer**: Jose Ojeda  
**Project**: Sivar.Os

---

## Appendix: File Upload Journey - A Case Study

This section documents the complete debugging journey for file upload implementation, preserving the learning process for future developers.

### Timeline of Issues & Fixes

**Phase 1: Initial Implementation (Working)**
- ✅ InputFile component with previews
- ✅ File upload to Azurite
- ✅ Database persistence
- ✅ Display in PostCard

**Phase 2: CORS Issue Discovery**
- 🐛 Problem: Mixed Content errors blocking HTTP Azurite URLs
- ✅ Solution: Server-side proxy endpoint

**Phase 3: URL Storage Problem**
- 🐛 Problem: Storing raw HTTP URLs in database
- ✅ Solution: Empty FilePath, dynamic URL generation

**Phase 4: Blob Search Prefix Mismatch**
- 🐛 Problem: Search prefix not matching blob naming (hierarchical namespace)
- ✅ Solution: Conditional prefix based on `UseHierarchicalNamespace`

**Phase 5: Metadata Not Loaded (Final Bug)**
- 🐛 Problem: Blobs found but metadata null, file_id check failing
- 🔍 Discovery: Azure SDK doesn't load metadata by default
- ✅ Solution: Add `traits: BlobTraits.Metadata` to `GetBlobsAsync()`

**Phase 6: Proxy URL Mismatch**
- 🐛 Problem: Proxy requesting wrong blob path (missing `posts/` prefix)
- ✅ Solution: Include folder prefix in `GeneratePublicUrl()` when hierarchical

### What Worked

1. **Comprehensive Logging** - Every critical operation logged with request IDs
2. **Structured Approach** - Repository → Service → Component pattern
3. **Environment Detection** - Automatic proxy vs direct URL selection
4. **Dynamic URL Generation** - Never storing environment-specific URLs
5. **Incremental Debugging** - Fixed one issue at a time with validation

### What Didn't Work

1. ❌ Storing raw URLs in database (breaks across environments)
2. ❌ Assuming metadata loads automatically (Azure SDK quirk)
3. ❌ Hardcoded blob paths (not flexible for different namespaces)
4. ❌ Skipping comprehensive logging (made debugging harder)

### Key Takeaways for Future Features

1. **Never assume SDK behavior** - Always verify with documentation
2. **Log everything** - Comprehensive logging reveals hidden issues
3. **Test across environments** - Dev (Azurite) and Prod (Azure) behave differently
4. **Dynamic > Static** - Generate URLs dynamically, don't store them
5. **Configuration-driven** - Use settings for structural decisions (hierarchical namespace)
6. **Document as you go** - Update documentation immediately when patterns change

---
