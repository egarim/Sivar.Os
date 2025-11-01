# Sivar.Os Development Rules & Guidelines

> **Last Updated**: November 1, 2025 - Added Authentication & Authorization Routing Patterns  
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
7. [Adaptive Loading Pattern - Client/Server ML Hybrid](#adaptive-loading-pattern---clientserver-ml-hybrid) ⭐ **NEW**
   - Progressive Enhancement for AI Features
   - Background Model Preloading
   - Server-First with Client Switch Strategy
   - Transformers.js Integration Examples
   - Sentiment Analysis & Embeddings Implementation
8. [CSS Organization & Styling](#css-organization--styling)
9. [Logging Standards](#logging-standards)
10. [Authentication & Authorization Routing](#authentication--authorization-routing) ⭐ **NEW**
    - Route Configuration Pattern
    - AllowAnonymous Implementation
    - Cookie Authentication Middleware
    - Redirect Loop Prevention
    - Common Issues & Solutions
11. [Error Handling](#error-handling)
12. [Testing & Debugging](#testing--debugging)
13. [PostgreSQL pgvector & EF Core 9.0](#postgresql-pgvector--ef-core-90) ⚠️ **CRITICAL**
14. [Database Script System](#database-script-system) ⭐ **UPDATED**
    - Architecture Overview
    - Existing SQL Scripts (Phase 5-7)
    - How to Add More Continuous Aggregates ⭐ **NEW**
    - Script Execution Order
    - Best Practices & Troubleshooting
15. [References](#references)

---

## ⚠️ CRITICAL: PostgreSQL pgvector & EF Core 9.0 Compatibility

### 🚨 RECURRING ISSUE - READ THIS BEFORE USING PGVECTOR

**Problem:** Pgvector.EntityFrameworkCore's `Vector` type is **INCOMPATIBLE** with EF Core 9.0, leading to runtime database errors.

### The Issue Explained

| Component | Status | Details |
|-----------|--------|---------|
| **PostgreSQL pgvector extension** | ✅ WORKS | Database extension works perfectly with EF Core 9.0 |
| **Pgvector.EntityFrameworkCore package** | ✅ INSTALLED | NuGet package version 0.2.2 installed |
| **`Vector` C# type** | ❌ BROKEN | The `Vector?` type does NOT work with EF Core 9.0/Npgsql 9.0 |
| **Workaround** | ✅ REQUIRED | Use `string?` type with manual conversion (Phase 3 pattern) |

### Why This Happens

```csharp
// ❌ THIS LOOKS RIGHT BUT DOESN'T WORK
using Pgvector;

public class Post
{
    public Vector? ContentEmbedding { get; set; }  // ❌ FAILS AT RUNTIME
}

// Runtime Error:
// "column 'ContentEmbedding' is of type vector but expression is of type character varying"
// "You will need to rewrite or cast the expression"
```

**Root Cause:**
- Pgvector.EntityFrameworkCore 0.2.2 was built for **EF Core 8.0**
- EF Core 9.0 changed internal APIs and type handling
- The `Vector` type's value converter doesn't work with EF Core 9.0's new architecture
- EF Core sends vector data as `character varying` instead of `vector` type

### ✅ CORRECT Solution: Phase 3 Pattern

**Use `string?` type with `.Ignore()` to completely bypass EF Core:**

```csharp
// ✅ CORRECT - Entity uses string?
using Sivar.Os.Shared.Entities;

public class Post : BaseEntity
{
    // ... other properties ...
    
    /// <summary>
    /// Vector embedding for semantic search (384 dimensions)
    /// Stored as PostgreSQL vector type, represented as string in C#
    /// Format: "[0.1,0.2,0.3,...]"
    /// ⚠️ CRITICAL: This property is IGNORED by EF Core
    /// Updated manually via raw SQL to bypass type conversion errors
    /// </summary>
    public string? ContentEmbedding { get; set; }  // ✅ Use string, not Vector
}

// ✅ CORRECT - Configuration (IGNORE the column!)
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        // ... other configurations ...

        // ⭐ CRITICAL SOLUTION: Ignore ContentEmbedding completely
        // EF Core 9.0 cannot handle vector type conversion at all
        // We update this column manually via raw SQL instead
        builder.Ignore(p => p.ContentEmbedding);
        
        // HNSW index must be created manually in database:
        // CREATE INDEX IX_Posts_ContentEmbedding_Hnsw 
        //   ON "Sivar_Posts" USING hnsw ("ContentEmbedding" vector_cosine_ops);
    }
}
```

### How to Update ContentEmbedding

Since the column is ignored by EF Core, you must update it using raw SQL:

```csharp
// After creating a post, update ContentEmbedding via raw SQL
var embedding = await _vectorService.GenerateEmbeddingAsync(post.Content);
var vectorString = _vectorService.ToPostgresVector(embedding); // Returns "[0.1,0.2,...]"

await _context.Database.ExecuteSqlRawAsync(
    @"UPDATE ""Sivar_Posts"" 
      SET ""ContentEmbedding"" = {0}::vector, 
          ""UpdatedAt"" = {1}
      WHERE ""Id"" = {2}",
    vectorString,
    DateTime.UtcNow,
    post.Id);
```

### Conversion Pattern

**Service Layer - Convert to PostgreSQL Vector Format:**

```csharp
// ✅ CORRECT - VectorEmbeddingService.cs
public class VectorEmbeddingService
{
    public string ToPostgresVector(Embedding<float> embedding)
    {
        // Convert ReadOnlyMemory<float> to PostgreSQL vector format: "[val1,val2,...]"
        var values = embedding.Vector.ToArray();
        return $"[{string.Join(",", values)}]";
    }
    
    public async Task<Embedding<float>> GenerateEmbeddingAsync(string text)
    {
        var embeddings = await _embeddingGenerator.GenerateAsync([text]);
        return embeddings[0];
    }
}

// ✅ CORRECT - PostService.cs
public async Task<PostDto?> CreatePostAsync(string keycloakId, CreatePostDto createPostDto)
{
    // Generate embedding and convert to string
    var embedding = await _vectorService.GenerateEmbeddingAsync(post.Content);
    post.ContentEmbedding = _vectorService.ToPostgresVector(embedding);  // ✅ Store as string
    
    await _postRepository.CreateAsync(post);
    await _postRepository.SaveChangesAsync();
    
    return postDto;
}
```

**Repository Layer - Use Raw SQL for Vector Operations:**

```csharp
// ✅ CORRECT - PostRepository.cs
public async Task<List<Post>> SemanticSearchAsync(string queryEmbedding, int limit = 10)
{
    // Use raw SQL with <=> operator for cosine similarity
    var sql = @"
        SELECT * FROM ""Sivar_Posts""
        WHERE ""IsDeleted"" = false 
          AND ""ContentEmbedding"" IS NOT NULL
        ORDER BY ""ContentEmbedding"" <=> @queryEmbedding::vector
        LIMIT @limit";

    var posts = await _context.Posts
        .FromSqlRaw(sql, 
            new NpgsqlParameter("@queryEmbedding", queryEmbedding),  // ✅ Pass string
            new NpgsqlParameter("@limit", limit))
        .Include(p => p.Profile)
        .ToListAsync();

    return posts;
}
```

**Display Layer - Parse String to Float Array:**

```csharp
// ✅ CORRECT - PostService.cs (DTO mapping)
private PostDto MapToDto(Post post)
{
    return new PostDto
    {
        // ... other properties ...
        
        // Parse "[0.1,0.2,...]" to float[]
        ContentEmbedding = !string.IsNullOrEmpty(post.ContentEmbedding)
            ? post.ContentEmbedding
                .Trim('[', ']')
                .Split(',')
                .Select(float.Parse)
                .ToArray()
            : null
    };
}
```

### Why We Can't Downgrade to EF Core 8.0

**Constraint:** This project uses `Microsoft.Extensions.AI` preview packages that **REQUIRE .NET 9.0**.

```csharp
// DEPENDENCY CHAIN:
Microsoft.Extensions.AI 9.0.1-preview  (Requires .NET 9.0)
    ↓
IChatClient, IEmbeddingGenerator  (Used throughout project)
    ↓
System.Numerics.Tensors 9.0.x  (May not work on .NET 8.0)
```

**Downgrade Impact:**
- ❌ Would break AI features (chat, embeddings)
- ❌ Requires changing ALL 10+ projects from net9.0 to net8.0
- ❌ Authentication packages would need downgrade
- ❌ Massive migration effort for minimal gain

**Verdict:** **MUST stay on .NET 9.0 + EF Core 9.0**

### Configuration Checklist

Before using pgvector in any entity:

- [ ] ✅ **Use `string?` type** - NOT `Vector?` in entity properties
- [ ] ✅ **Use `.Ignore()`** - Add `builder.Ignore(p => p.ContentEmbedding)` in entity configuration
- [ ] ✅ **Create HNSW index manually** - Via raw SQL in database (not EF Core)
- [ ] ✅ **Use raw SQL for queries** - With `<=>` operator for cosine similarity
- [ ] ✅ **Use raw SQL for updates** - `ExecuteSqlRawAsync()` with `::vector` cast for INSERT/UPDATE
- [ ] ✅ **Convert embeddings** - Use `ToPostgresVector()` to convert to `"[val1,val2,...]"` format
- [ ] ✅ **Parse on read** - EF Core can read the column as string, parse to `float[]` in DTOs
- [ ] ✅ **Register .UseVector()** - In `Program.cs` for Npgsql pgvector support
- [ ] ❌ **DON'T use Vector type** - It's incompatible with EF Core 9.0
- [ ] ❌ **DON'T use .HasColumnType()** - Column is ignored, configured in database directly
- [ ] ❌ **DON'T use .HasIndex()** - Create HNSW index manually in PostgreSQL
- [ ] ❌ **DON'T let EF Core touch the column** - Use `.Ignore()` to bypass completely

### Program.cs Configuration

```csharp
// ✅ REQUIRED - Register pgvector extension
builder.Services.AddDbContext<SivarDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.UseVector());  // ✅ Enable pgvector
});
```

### Common Errors & Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| "column is of type vector but expression is of type character varying" | Using `Vector?` type | ✅ Change to `string?` |
| "Cannot write Vector type" | EF Core 9.0 incompatibility | ✅ Use Phase 3 pattern (string?) |
| "Operator <=> does not exist" | Missing pgvector extension | ✅ Add `.UseVector()` to Npgsql |
| "Invalid vector format" | Wrong string format | ✅ Use `"[val1,val2,...]"` format |
| "Index method hnsw does not exist" | pgvector not installed in DB | ✅ Install pgvector in PostgreSQL |

### Testing Checklist

When implementing semantic search:

1. **Verify Database Setup:**
   - [ ] pgvector extension installed in PostgreSQL
   - [ ] `.UseVector()` registered in Program.cs
   - [ ] Column type is `vector(384)` in database

2. **Verify Code Pattern:**
   - [ ] Entity uses `string?` type (not `Vector?`)
   - [ ] Configuration uses `.HasColumnType("vector(384)")`
   - [ ] HNSW index created with `vector_cosine_ops`

3. **Verify Conversion Logic:**
   - [ ] Embeddings converted to `"[val1,val2,...]"` format before insert
   - [ ] Raw SQL queries use `@param::vector` casting
   - [ ] DTOs parse string back to `float[]` for display

4. **Verify Queries:**
   - [ ] Search uses raw SQL with `<=>` operator
   - [ ] Query parameter format matches stored format
   - [ ] Results ordered by similarity score

### Key Lessons

1. **pgvector Extension vs Pgvector.EntityFrameworkCore Package:**
   - ✅ pgvector DATABASE extension works perfectly
   - ❌ Pgvector.EntityFrameworkCore C# types DON'T work with EF Core 9.0
   - ✅ Use `.Ignore()` to bypass EF Core completely

2. **Why .Ignore() Pattern Works:**
   - EF Core NEVER touches the ContentEmbedding column
   - All updates done via raw SQL with `::vector` cast
   - Reading works fine (EF Core can read vector as string)
   - Semantic search uses raw SQL queries
   - Completely bypasses EF Core's broken type handling

3. **This is NOT a Bug - It's an Architectural Incompatibility:**
   - Pgvector.EntityFrameworkCore 0.2.2 is for EF Core 8.0
   - No EF Core 9.0-compatible version exists yet
   - **Builder.Ignore() + raw SQL is the ONLY reliable solution**
   - Interceptors, converters, and SQL modifications CANNOT fix this

### Future Updates

Monitor these for EF Core 9.0 support:
- [Pgvector.EntityFrameworkCore GitHub](https://github.com/pgvector/pgvector-dotnet)
- [EF Core 9.0 Release Notes](https://docs.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/whatsnew)

**When EF Core 9.0-compatible version is released:**
1. Update package version
2. Test Vector type compatibility
3. Update this documentation
4. Consider migrating from string? back to Vector? if stable

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

## Adaptive Loading Pattern - Client/Server ML Hybrid

### ⭐ Industry-Standard Pattern for Progressive AI Enhancement

**Pattern Name**: Adaptive Loading / Progressive Enhancement / Hybrid Rendering  
**Purpose**: Start with fast server-side processing, seamlessly upgrade to high-quality client-side ML when models are ready  
**Status**: ✅ PROVEN - Used by Google Translate, Grammarly, Photoshop Web, GitHub Copilot

### The Problem

**Challenge**: Client-side ML models (Transformers.js) can be 100-300MB and take 10-60 seconds to download on first visit.

**User Impact:**
- ❌ Users must wait before using features (bad UX)
- ❌ Blank screens or loading spinners
- ❌ High bounce rates on slow connections

### The Solution: Adaptive Loading

**Strategy**: Use fast server-side processing immediately, then switch to better client-side ML when ready.

```
User arrives → Server-side (instant) → Models download (background) → Client-side (better quality)
```

### How It Works

#### Phase 1: First Visit (Cold Start)

```
Timeline:
0:00 ─ Page loads
0:00 ─ Models start downloading (background, non-blocking)
0:05 ─ User creates post #1 → ✅ Server-side analysis (instant)
0:30 ─ Models finish loading → ✅ Client ready!
0:45 ─ User creates post #2 → ✅ Client-side analysis (better quality)
```

#### Phase 2: Return Visit (Cached Models)

```
Timeline:
0:00 ─ Page loads
0:01 ─ Models load from IndexedDB cache → ✅ Client ready!
0:05 ─ User creates post → ✅ Client-side analysis (immediately)
```

### Architecture Components

```
┌─────────────────────────────────────────────────────────────┐
│               Blazor Component (UI Layer)                    │
│                  (CreatePost.razor)                          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│            Hybrid Service (Orchestrator)                     │
│         SentimentAnalysisService / EmbeddingService          │
│                                                              │
│   1. Check: Are client models ready?                         │
│   2. IF ready → Use ClientService (high quality)             │
│   3. IF not ready → Use ServerService (fast fallback)        │
└─────────┬────────────────────────────────────┬──────────────┘
          │                                    │
          ↓                                    ↓
┌──────────────────────────┐    ┌──────────────────────────┐
│   ClientService          │    │   ServerService          │
│   (Transformers.js)      │    │   (Keyword/ML.NET)       │
│                          │    │                          │
│   - IJSRuntime           │    │   - Fast algorithms      │
│   - Browser ML models    │    │   - Always available     │
│   - Best quality         │    │   - Good-enough quality  │
│   - Requires models      │    │   - No dependencies      │
└──────────────────────────┘    └──────────────────────────┘
```

### Implementation Pattern

#### Step 1: JavaScript Module with Background Loading

**File**: `sentiment-analyzer.js` (or `embeddings.js`)

```javascript
// ✅ CORRECT - Auto-initialize on load, non-blocking
class SentimentAnalyzer {
    constructor() {
        this.modelsReady = false;
        this.modelsLoading = false;
        this.initPromise = null;
        
        // 🎯 KEY: Start loading immediately in background
        this.initializeInBackground();
    }
    
    /**
     * Background initialization - doesn't block page load
     * Models download while user interacts with page
     */
    async initializeInBackground() {
        if (this.modelsLoading || this.modelsReady) return;
        
        this.modelsLoading = true;
        console.log('[SentimentAnalyzer] 🔄 Starting background model loading...');
        
        try {
            this.initPromise = this.loadModels();
            await this.initPromise;
            
            this.modelsReady = true;
            console.log('[SentimentAnalyzer] ✅ Models ready - switching to client-side!');
            
            // Optional: Notify .NET that models are ready
            if (window.DotNet) {
                window.DotNet.invokeMethodAsync('Sivar.Os', 'OnModelsReady');
            }
        } catch (error) {
            console.warn('[SentimentAnalyzer] ⚠️ Models failed - staying on server-side', error);
            this.modelsReady = false;
        } finally {
            this.modelsLoading = false;
        }
    }
    
    /**
     * Check if models are ready (called from .NET)
     * @returns {boolean} True if client-side analysis available
     */
    isReady() {
        return this.modelsReady;
    }
    
    /**
     * Actual model loading logic
     */
    async loadModels() {
        const { pipeline } = await import('https://cdn.jsdelivr.net/npm/@xenova/transformers@2.6.0');
        
        // Load sentiment model
        this.sentimentClassifier = await pipeline(
            'text-classification',
            '/models/sentiment/', // Local quantized models
            { quantized: true }
        );
        
        // Load emotion model
        this.emotionClassifier = await pipeline(
            'text-classification',
            '/models/emotion/',
            { topk: 5, quantized: true }
        );
    }
    
    /**
     * Analyze text (only works if models ready)
     */
    async analyze(text, language) {
        if (!this.modelsReady) {
            throw new Error('Models not ready - use server-side analysis');
        }
        
        const sentimentResult = await this.sentimentClassifier(text);
        const emotionResult = await this.emotionClassifier(text);
        
        return {
            primaryEmotion: emotionResult[0].label,
            emotionScore: emotionResult[0].score,
            sentimentPolarity: sentimentResult[0].score,
            // ... map results
        };
    }
}

// Global instance - starts loading immediately
window.SentimentAnalyzer = new SentimentAnalyzer();
console.log('[SentimentAnalyzer] Module loaded - background initialization started');
```

#### Step 2: Client Service (JavaScript Interop)

**File**: `ClientSentimentAnalysisService.cs`

```csharp
using Microsoft.JSInterop;
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Services;

/// <summary>
/// Client-side sentiment analysis using Transformers.js
/// Only works when browser models are loaded
/// </summary>
public class ClientSentimentAnalysisService : IClientSentimentAnalysisService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ClientSentimentAnalysisService> _logger;

    public ClientSentimentAnalysisService(
        IJSRuntime jsRuntime,
        ILogger<ClientSentimentAnalysisService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Check if client-side models are ready
    /// 🎯 KEY METHOD - Called before every analysis
    /// </summary>
    public async Task<bool> AreModelsReadyAsync()
    {
        try
        {
            var isReady = await _jsRuntime.InvokeAsync<bool>(
                "SentimentAnalyzer.isReady");
            
            _logger.LogDebug("[ClientSentiment] Models ready check: {IsReady}", isReady);
            return isReady;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ClientSentiment] Failed to check model readiness");
            return false;
        }
    }

    /// <summary>
    /// Analyze text using client-side ML
    /// Only call if AreModelsReadyAsync() returns true
    /// </summary>
    public async Task<SentimentAnalysisResultDto?> TryAnalyzeAsync(
        string text, 
        string language)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("[ClientSentiment] Empty text provided");
            return null;
        }

        try
        {
            _logger.LogInformation(
                "[ClientSentiment] Analyzing via Transformers.js - Length={Length}", 
                text.Length);

            var result = await _jsRuntime.InvokeAsync<SentimentAnalysisResult>(
                "SentimentAnalyzer.analyze", 
                text, 
                language);

            if (result == null)
            {
                _logger.LogWarning("[ClientSentiment] JS returned null result");
                return null;
            }

            // Convert JS result to DTO
            var dto = new SentimentAnalysisResultDto
            {
                PrimaryEmotion = result.PrimaryEmotion,
                EmotionScore = (decimal)result.EmotionScore,
                SentimentPolarity = (decimal)result.SentimentPolarity,
                EmotionScores = new EmotionScoresDto
                {
                    Joy = (decimal)result.EmotionScores.Joy,
                    Sadness = (decimal)result.EmotionScores.Sadness,
                    Anger = (decimal)result.EmotionScores.Anger,
                    Fear = (decimal)result.EmotionScores.Fear,
                    Neutral = (decimal)result.EmotionScores.Neutral
                },
                HasAnger = result.HasAnger,
                NeedsReview = result.NeedsReview,
                Language = result.Language,
                AnalysisSource = "client", // 🎯 Track source
                AnalyzedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "[ClientSentiment] ✅ Analysis complete: {Emotion} (score: {Score:F2})", 
                dto.PrimaryEmotion, dto.EmotionScore);

            return dto;
        }
        catch (JSException jsEx)
        {
            _logger.LogError(jsEx, "[ClientSentiment] ❌ JavaScript error during analysis");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ClientSentiment] ❌ Error during sentiment analysis");
            return null;
        }
    }

    // JS interop data structures
    private class SentimentAnalysisResult
    {
        public string PrimaryEmotion { get; set; } = "Neutral";
        public double EmotionScore { get; set; }
        public double SentimentPolarity { get; set; }
        public EmotionScoresJs EmotionScores { get; set; } = new();
        public bool HasAnger { get; set; }
        public bool NeedsReview { get; set; }
        public string Language { get; set; } = "en";
    }

    private class EmotionScoresJs
    {
        public double Joy { get; set; }
        public double Sadness { get; set; }
        public double Anger { get; set; }
        public double Fear { get; set; }
        public double Neutral { get; set; }
    }
}
```

#### Step 3: Server Service (Fast Fallback)

**File**: `ServerSentimentAnalysisService.cs`

```csharp
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Services;

/// <summary>
/// Server-side sentiment analysis fallback
/// Fast, always-available, keyword-based (or ML.NET in future)
/// </summary>
public class ServerSentimentAnalysisService : IServerSentimentAnalysisService
{
    private readonly ILogger<ServerSentimentAnalysisService> _logger;

    public ServerSentimentAnalysisService(ILogger<ServerSentimentAnalysisService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Server-side analysis using keyword detection
    /// 🎯 Always available, no dependencies
    /// 🎯 Good-enough quality for immediate processing
    /// </summary>
    public Task<SentimentAnalysisResultDto> AnalyzeAsync(string text, string language)
    {
        _logger.LogInformation(
            "[ServerSentiment] Analyzing via keyword detection - Length={Length}", 
            text.Length);

        // Simple keyword-based detection (replace with ML.NET for better quality)
        var emotionScores = DetectEmotionsViaKeywords(text, language);
        
        var primaryEmotion = emotionScores.OrderByDescending(e => e.Value).First().Key;
        var emotionScore = emotionScores[primaryEmotion];

        var result = new SentimentAnalysisResultDto
        {
            PrimaryEmotion = primaryEmotion,
            EmotionScore = (decimal)emotionScore,
            SentimentPolarity = (decimal)CalculatePolarity(emotionScores),
            EmotionScores = new EmotionScoresDto
            {
                Joy = (decimal)emotionScores["Joy"],
                Sadness = (decimal)emotionScores["Sadness"],
                Anger = (decimal)emotionScores["Anger"],
                Fear = (decimal)emotionScores["Fear"],
                Neutral = (decimal)emotionScores["Neutral"]
            },
            HasAnger = emotionScores["Anger"] >= 0.6m,
            NeedsReview = emotionScores["Anger"] >= 0.75m,
            Language = language,
            AnalysisSource = "server", // 🎯 Track source
            AnalyzedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "[ServerSentiment] ✅ Analysis complete: {Emotion} (score: {Score:F2})", 
            result.PrimaryEmotion, result.EmotionScore);

        return Task.FromResult(result);
    }

    private Dictionary<string, decimal> DetectEmotionsViaKeywords(string text, string language)
    {
        // Simple keyword detection - replace with ML.NET for production
        var textLower = text.ToLowerInvariant();
        
        var joyKeywords = language == "es" 
            ? new[] { "feliz", "alegre", "genial", "excelente", "amor" }
            : new[] { "happy", "joy", "love", "great", "excellent" };
        
        var sadnessKeywords = language == "es"
            ? new[] { "triste", "solo", "deprimido", "llorar" }
            : new[] { "sad", "lonely", "depressed", "cry" };
        
        var angerKeywords = language == "es"
            ? new[] { "odio", "enojado", "furioso", "molesto" }
            : new[] { "hate", "angry", "furious", "annoyed" };

        var joyScore = joyKeywords.Count(k => textLower.Contains(k)) * 0.25m;
        var sadnessScore = sadnessKeywords.Count(k => textLower.Contains(k)) * 0.25m;
        var angerScore = angerKeywords.Count(k => textLower.Contains(k)) * 0.25m;

        return new Dictionary<string, decimal>
        {
            ["Joy"] = Math.Min(joyScore, 1.0m),
            ["Sadness"] = Math.Min(sadnessScore, 1.0m),
            ["Anger"] = Math.Min(angerScore, 1.0m),
            ["Fear"] = 0.0m,
            ["Neutral"] = joyScore == 0 && sadnessScore == 0 && angerScore == 0 ? 1.0m : 0.2m
        };
    }

    private decimal CalculatePolarity(Dictionary<string, decimal> emotions)
    {
        return emotions["Joy"] - emotions["Sadness"] - emotions["Anger"];
    }
}
```

#### Step 4: Hybrid Orchestrator (The Smart Router)

**File**: `SentimentAnalysisService.cs`

```csharp
using Sivar.Os.Shared.DTOs;

namespace Sivar.Os.Services;

/// <summary>
/// 🎯 ADAPTIVE LOADING - Smart router between client and server
/// 
/// Strategy:
/// 1. Check if client models are ready
/// 2. IF ready → Use client (better quality, privacy-first)
/// 3. IF not ready → Use server (fast, always available)
/// 
/// User Experience:
/// - First visit: Server-side (instant) while models download
/// - After models load: Client-side (better quality)
/// - Return visits: Client-side immediately (cached models)
/// </summary>
public class SentimentAnalysisService : ISentimentAnalysisService
{
    private readonly IClientSentimentAnalysisService _clientService;
    private readonly IServerSentimentAnalysisService _serverService;
    private readonly ILogger<SentimentAnalysisService> _logger;

    public SentimentAnalysisService(
        IClientSentimentAnalysisService clientService,
        IServerSentimentAnalysisService serverService,
        ILogger<SentimentAnalysisService> logger)
    {
        _clientService = clientService;
        _serverService = serverService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SentimentAnalysisResultDto> AnalyzeAsync(string text, string language)
    {
        _logger.LogInformation(
            "[HybridSentiment] 🎯 Starting adaptive analysis - Length={Length}", 
            text.Length);

        // 🎯 STEP 1: Check if client models are ready (non-blocking check)
        var clientReady = await _clientService.AreModelsReadyAsync();
        
        if (clientReady)
        {
            // ✅ STEP 2a: Models ready → Use high-quality client-side ML
            _logger.LogInformation(
                "[HybridSentiment] ✅ Client models ready - using Transformers.js");
            
            try
            {
                var clientResult = await _clientService.TryAnalyzeAsync(text, language);
                if (clientResult != null)
                {
                    _logger.LogInformation(
                        "[HybridSentiment] ✅ Client-side analysis successful: {Emotion}", 
                        clientResult.PrimaryEmotion);
                    return clientResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "[HybridSentiment] ⚠️ Client-side failed, falling back to server");
            }
        }
        else
        {
            // 🔄 STEP 2b: Models not ready → Use fast server-side
            _logger.LogInformation(
                "[HybridSentiment] 🔄 Client models not ready - using server-side fallback");
        }

        // 🎯 STEP 3: Server-side fallback (always works)
        var serverResult = await _serverService.AnalyzeAsync(text, language);
        
        _logger.LogInformation(
            "[HybridSentiment] ✅ Analysis complete via {Source}: {Emotion}", 
            serverResult.AnalysisSource, 
            serverResult.PrimaryEmotion);
        
        return serverResult;
    }
}
```

### Configuration Modes

The AI services (Sentiment Analysis, Embeddings) support three execution modes configured in `appsettings.json`:

#### Mode 1: Adaptive (Default - Recommended)

**Configuration:**
```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "Adaptive"
    },
    "Embeddings": {
      "Mode": "Adaptive"
    }
  }
}
```

**Behavior:**
- ✅ **Smart routing**: Check if client models are ready
- ✅ **If ready**: Use client-side ML (high quality, privacy-first, free)
- ✅ **If not ready**: Use server-side (instant processing, keyword-based)
- ✅ **Best UX**: Users never wait, quality improves automatically when models load
- ✅ **Works for all users**: First-time visitors (server) and returning visitors (client)

**Use this when:**
- You want the best user experience
- You want progressive enhancement
- You don't want users to wait for model downloads
- You want automatic quality improvement

**Example Flow:**
```
First Visit:
  0:00 - Page loads, models start downloading (background)
  0:05 - User creates post → Server-side (instant)
  0:30 - Models finish loading
  0:45 - User creates 2nd post → Client-side (better quality)

Return Visit:
  0:00 - Page loads
  0:01 - Models load from cache
  0:05 - User creates post → Client-side (immediately)
```

#### Mode 2: ClientOnly (Privacy-First)

**Configuration:**
```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "ClientOnly"
    },
    "Embeddings": {
      "Mode": "ClientOnly"
    }
  }
}
```

**Behavior:**
- ✅ **Always client-side**: Forces browser-based ML exclusively
- ✅ **Maximum privacy**: No data sent to server
- ✅ **Free**: No API costs
- ⚠️ **Users must wait**: First-time visitors wait for model downloads (10-60 seconds)
- ⚠️ **May fail**: Throws exception if models fail to load
- ✅ **Cached**: Subsequent visits are instant (models cached in IndexedDB)

**Use this when:**
- Privacy is paramount (healthcare, sensitive data)
- You want zero server-side processing
- You're okay with first-visit delays
- You have quantized models bundled with app (faster download)

**Error Handling:**
```csharp
try
{
    var result = await _sentimentService.AnalyzeAsync(text, language);
}
catch (InvalidOperationException ex)
{
    // Models not loaded yet - show user-friendly message
    ShowNotification("AI models are loading. Please try again in a moment.");
}
```

#### Mode 3: ServerOnly (Instant Processing)

**Configuration:**
```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "ServerOnly"
    },
    "Embeddings": {
      "Mode": "ServerOnly"
    }
  }
}
```

**Behavior:**
- ✅ **Always server-side**: Forces server-based processing
- ✅ **Instant**: No model downloads, no waiting
- ✅ **Consistent**: Same quality for all users
- ⚠️ **Lower quality**: Keyword-based for sentiment (unless you add ML.NET)
- ⚠️ **Potential costs**: If using cloud AI APIs (Azure OpenAI, etc.)
- ⚠️ **No privacy**: Data processed on server

**Use this when:**
- You want consistent, instant processing
- You have ML.NET or cloud AI configured server-side
- You don't want to ship browser models
- Testing/debugging server-side logic

**Upgrade Path:**
```csharp
// Replace keyword detection with ML.NET
public class ServerSentimentAnalysisService : IServerSentimentAnalysisService
{
    private readonly MLContext _mlContext;
    private readonly ITransformer _model;

    public async Task<SentimentAnalysisResultDto> AnalyzeAsync(string text, string language)
    {
        // Use ML.NET for better server-side quality
        var prediction = _mlContext.Model.Predict<SentimentPrediction>(text);
        // ... map to DTO
    }
}
```

### Mode Comparison Table

| Feature | Adaptive | ClientOnly | ServerOnly |
|---------|----------|------------|------------|
| **First-visit UX** | ✅ Instant (server) | ⚠️ Wait for models (10-60s) | ✅ Instant |
| **Return-visit UX** | ✅ Instant (client, cached) | ✅ Instant (client, cached) | ✅ Instant |
| **Quality (first visit)** | ⭐⭐⭐ Good (keyword) | ⭐⭐⭐⭐⭐ Excellent (ML) | ⭐⭐⭐ Good (keyword) |
| **Quality (cached)** | ⭐⭐⭐⭐⭐ Excellent (ML) | ⭐⭐⭐⭐⭐ Excellent (ML) | ⭐⭐⭐ Good (keyword) |
| **Privacy** | ⚠️ Server first-visit | ✅ Always client | ❌ Always server |
| **Cost** | Free | Free | Free (unless cloud AI) |
| **Offline** | ⚠️ After cache | ✅ After cache | ❌ Requires server |
| **Reliability** | ✅✅ Best (fallback) | ⚠️ May fail | ✅ Always works |
| **Best for** | Production | Privacy-critical | Testing/debugging |

### Changing Modes at Runtime

You can change modes without recompiling by editing `appsettings.json`:

```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "Adaptive"  // Change to "ClientOnly" or "ServerOnly"
    },
    "Embeddings": {
      "Mode": "ClientOnly"  // Can mix modes (sentiment=Adaptive, embeddings=ClientOnly)
    }
  }
}
```

**After changing:**
1. Restart the application
2. Check logs for: `[SentimentAnalysis] Service initialized with mode: {Mode}`
3. Test with a post creation to verify routing

### Per-Feature Mode Configuration

You can configure different modes for different AI features:

```json
{
  "AIServices": {
    "SentimentAnalysis": {
      "Mode": "Adaptive",
      "Description": "Use adaptive for sentiment (keyword fallback is acceptable)"
    },
    "Embeddings": {
      "Mode": "ClientOnly",
      "Description": "Use client-only for embeddings (privacy-critical for semantic search)"
    }
  }
}
```

**Reasoning:**
- **Sentiment**: Adaptive works well (keyword fallback is "good enough" while models load)
- **Embeddings**: ClientOnly ensures vectors are never generated on server (privacy)

### Monitoring Mode Usage

Check logs to see which mode is being used:

```
# Adaptive mode logs
[SentimentAnalysis.Adaptive] 🎯 Smart routing enabled
[SentimentAnalysis.Adaptive] ✅ Client models ready - using Transformers.js
[SentimentAnalysis.Adaptive] ✅ Client-side success: Joy

# Or if models not ready:
[SentimentAnalysis.Adaptive] 🔄 Client models not ready - using server-side (models loading in background)
[SentimentAnalysis.Adaptive] ✅ Analysis complete via server: Neutral

# ClientOnly mode logs
[SentimentAnalysis.ClientOnly] Using client-side ML exclusively
[SentimentAnalysis.ClientOnly] ✅ Analysis successful: Joy

# Or if models failed:
[SentimentAnalysis.ClientOnly] ❌ Client-side analysis failed and no fallback allowed

# ServerOnly mode logs
[SentimentAnalysis.ServerOnly] Using server-side processing exclusively
[SentimentAnalysis.ServerOnly] ✅ Analysis complete: Neutral (source: server)
```

### Registration in Program.cs

```csharp
// 🎯 AI Services Registration
builder.Services.AddScoped<IClientSentimentAnalysisService, ClientSentimentAnalysisService>();
builder.Services.AddScoped<IServerSentimentAnalysisService, ServerSentimentAnalysisService>();
builder.Services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();

// Same pattern for embeddings
builder.Services.AddScoped<IClientEmbeddingService, ClientEmbeddingService>();
builder.Services.AddScoped<IServerEmbeddingService, ServerEmbeddingService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();

// 🎯 Configure AI Service Modes (Adaptive, ClientOnly, ServerOnly)
builder.Services.Configure<AIServiceOptions>(
    builder.Configuration.GetSection("AIServices"));
```

### Loading Models in App.razor (Optional Preload Trigger)

**File**: `App.razor`

```razor
@* Add script for sentiment analyzer *@
<script type="module" src="js/sentiment-analyzer.js"></script>
<script type="module" src="js/embeddings.js"></script>

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Optional: Trigger model preload explicitly
            // (Models auto-load on script init, but you can force it here)
            try
            {
                await JS.InvokeVoidAsync("SentimentAnalyzer.initializeInBackground");
                await JS.InvokeVoidAsync("ClientEmbeddings.initializeInBackground");
                
                Logger.LogInformation("[App] Model preloading triggered");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "[App] Model preload trigger failed (models will still load in background)");
            }
        }
    }
}
```

### User Experience Flow

#### Scenario 1: First-Time User (No Cache)

```
User Action                  Backend Processing               Models Status
───────────────────────────────────────────────────────────────────────────
Page loads                   -                                🔄 Downloading (0%)
User scrolls, reads          -                                🔄 Downloading (25%)
User clicks "Create Post"    Server-side analysis (instant)   🔄 Downloading (50%)
Post created ✅              Keyword-based sentiment          🔄 Downloading (75%)
User edits profile           -                                ✅ Ready! (100%)
User creates 2nd post        Client-side analysis (ML)        ✅ Cached
Post created ✅              Transformers.js high-quality     ✅ Cached
```

**User sees:**
- ✅ Instant post creation (no waiting!)
- ✅ Seamless experience (no difference in UI)
- ✅ Better quality on 2nd+ posts (but user doesn't know/care)

#### Scenario 2: Returning User (Cached Models)

```
User Action                  Backend Processing               Models Status
───────────────────────────────────────────────────────────────────────────
Page loads                   -                                🔄 Loading from cache
User scrolls, reads          -                                ✅ Ready! (< 1 second)
User clicks "Create Post"    Client-side analysis (ML)        ✅ Cached
Post created ✅              Transformers.js high-quality     ✅ Cached
```

**User sees:**
- ✅ Instant high-quality ML from first post
- ✅ Offline-capable (models in browser)

### Real-World Examples in Sivar.Os

#### Example 1: Sentiment Analysis (Current Implementation)

**Files:**
- `ClientSentimentAnalysisService.cs` - Transformers.js wrapper
- `ServerSentimentAnalysisService.cs` - Keyword fallback
- `SentimentAnalysisService.cs` - Adaptive orchestrator
- `sentiment-analyzer.js` - Background model loader

**Flow:**
1. User creates post with text "I love this community!"
2. `PostService.CreatePostAsync()` calls `_sentimentService.AnalyzeAsync()`
3. Hybrid service checks `_clientService.AreModelsReadyAsync()`
4. **If ready**: Transformers.js analyzes → "Joy" (0.95 score)
5. **If not ready**: Keyword detection → "Joy" (0.75 score)
6. Post saved with sentiment data

**Code (PostService.cs):**
```csharp
// Adaptive sentiment analysis - uses best available method
var sentimentResult = await _sentimentService.AnalyzeAsync(
    post.Content, 
    post.Language ?? "en");

if (sentimentResult != null)
{
    post.PrimaryEmotion = sentimentResult.PrimaryEmotion;
    post.EmotionScore = sentimentResult.EmotionScore;
    // Source is logged: "client" or "server"
}
```

#### Example 2: Content Embeddings (Same Pattern)

**Files:**
- `ClientEmbeddingService.cs` - Transformers.js embeddings
- `ServerEmbeddingService.cs` - Azure OpenAI fallback
- `EmbeddingService.cs` - Adaptive orchestrator
- `embeddings.js` - Background model loader

**Flow:**
1. User creates post
2. `PostService` calls `_embeddingService.GenerateEmbeddingAsync()`
3. **If client ready**: Browser generates 384-dim vector locally (free, private)
4. **If not ready**: Azure OpenAI generates vector (costs money, server-side)
5. Vector stored in database for semantic search

**Benefits:**
- ✅ Privacy: Embeddings generated in browser when possible
- ✅ Cost: Free client-side vs paid server-side
- ✅ Speed: No API latency when using client
- ✅ Reliability: Server fallback always available

### Monitoring & Analytics

#### Track Analysis Source

Add telemetry to understand client vs server usage:

```csharp
// In SentimentAnalysisService
_logger.LogInformation(
    "[Telemetry] Sentiment analysis - Source={Source}, Emotion={Emotion}, Duration={Duration}ms",
    result.AnalysisSource, // "client" or "server"
    result.PrimaryEmotion,
    stopwatch.ElapsedMilliseconds);
```

**Metrics to track:**
- % of analyses using client vs server
- Average time to models becoming ready
- Model cache hit rate (return visitors)
- Fallback usage rate

### Troubleshooting

#### Issue: Always Using Server-Side

**Symptoms:**
- Logs show "Client models not ready"
- All analyses show `AnalysisSource = "server"`

**Diagnosis:**
1. Check browser console for JavaScript errors
2. Verify `sentiment-analyzer.js` loaded (Network tab)
3. Check for "Models ready" log in console
4. Verify IndexedDB has cached models (Application tab)

**Solutions:**
- Clear browser cache and reload
- Check models are in `wwwroot/models/` directory
- Verify `<script type="module">` in App.razor
- Check CORS if loading from CDN

#### Issue: JavaScript Exceptions

**Symptoms:**
- Browser console shows errors
- `AreModelsReadyAsync()` throws exception

**Solutions:**
- Wrap JS interop in try-catch
- Check `window.SentimentAnalyzer` exists
- Verify `isReady()` method exists in JS
- Use `IJSInProcessRuntime` for synchronous calls (Server-only)

### Best Practices

✅ **DO:**
- Always check `AreModelsReadyAsync()` before using client service
- Log analysis source ("client" or "server") for monitoring
- Implement comprehensive fallback logic
- Use background initialization (non-blocking)
- Cache models in IndexedDB automatically
- Provide identical API for both client and server services
- Use try-catch around JS interop calls

❌ **DON'T:**
- Block page load waiting for models
- Throw exceptions when models not ready
- Skip server fallback
- Force users to wait for downloads
- Clear IndexedDB cache unnecessarily
- Assume client will always work

### Performance Characteristics

| Metric | Client-Side (Transformers.js) | Server-Side (Keyword) | Server-Side (ML.NET) |
|--------|-------------------------------|----------------------|---------------------|
| **First load** | 10-60 seconds download | < 10ms | 100-500ms |
| **Cached load** | < 1 second | < 10ms | 100-500ms |
| **Inference time** | 50-200ms | < 10ms | 50-150ms |
| **Quality** | ⭐⭐⭐⭐⭐ Excellent | ⭐⭐ Basic | ⭐⭐⭐⭐ Very Good |
| **Privacy** | ✅ Private (browser) | ✅ Private | ⚠️ Server logs |
| **Cost** | Free | Free | Free (if self-hosted) |
| **Offline** | ✅ Works offline | ✅ Works offline | ❌ Requires server |

### Future Enhancements

**1. Service Worker for Offline Support**
```javascript
// Register service worker to cache models for offline use
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/sw.js');
}
```

**2. ML.NET Server-Side**
Replace keyword detection with ML.NET for better server-side quality:
```csharp
// Use Microsoft.ML for sentiment analysis
var prediction = _mlContext.Model.Predict<SentimentPrediction>(text);
```

**3. Adaptive Model Selection**
Choose model size based on device capability:
```javascript
// Load smaller model on mobile, full model on desktop
const modelSize = isMobile ? 'small' : 'large';
const model = await pipeline('sentiment-analysis', `model-${modelSize}`);
```

**4. Progressive Model Updates**
Update models in background without affecting users:
```javascript
// Check for model updates weekly
if (lastUpdate > 7 * 24 * 60 * 60 * 1000) {
    updateModelsInBackground();
}
```

### Key Takeaways

🎯 **For Developers:**
- Adaptive loading is industry-standard, not experimental
- Always provide server fallback for reliability
- Track client vs server usage for optimization
- Models cached in browser persist across sessions
- User experience is seamless (no visible difference)

🎯 **For Product:**
- Users never wait for ML features
- Quality improves automatically when ready
- Privacy-first approach (browser ML when possible)
- Works offline after first model download
- Scales without infrastructure costs (client-side)

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

## Authentication & Authorization Routing

### ⚠️ CRITICAL: Multi-Layer Authorization System

Blazor Server has **THREE layers** of authorization that must work together:

1. **HTTP Middleware Layer** - Cookie authentication in `Program.cs`
2. **Component Layer** - `AuthorizeRouteView` in `Routes.razor`
3. **Page Level** - `[Authorize]` or `[AllowAnonymous]` attributes

**If ANY layer blocks access, users get redirected to authentication.**

---

### The Problem: Welcome Page Redirect Loop

**Symptom**: Accessing `https://localhost:5001/` immediately redirects to Keycloak, or creates infinite redirect loop after login.

**Root Cause**: Landing page is protected by `[Authorize]` or missing `[AllowAnonymous]` attribute, causing redirect chain.

---

### ✅ CORRECT Route Configuration

**Landing Page (Public) - Must be at root:**

```razor
@* Landing.razor - Public landing page *@
@page "/"
@page "/welcome"
@layout LandingLayout
@attribute [AllowAnonymous]  ⭐ CRITICAL
@using Microsoft.AspNetCore.Components.Authorization
@inject AuthenticationStateProvider AuthenticationStateProvider
@inject NavigationManager Navigation

<h1>Welcome to Sivar.Os</h1>
<!-- Landing page content -->

@code {
    protected override async Task OnInitializedAsync()
    {
        // ⭐ CRITICAL: Redirect authenticated users away from landing page
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            Navigation.NavigateTo("/home", forceLoad: true);
        }
    }

    private void HandleSignIn()
    {
        // ⭐ CRITICAL: returnUrl must be /home, NOT /
        Navigation.NavigateTo("/authentication/login?returnUrl=/home", forceLoad: true);
    }

    private void HandleSignUp()
    {
        Navigation.NavigateTo("/authentication/register?returnUrl=/home", forceLoad: true);
    }
}
```

**Home Page (Authenticated) - Must NOT be at root:**

```razor
@* Home.razor - Authenticated home page *@
@page "/home"  ⭐ NOT "/" - root is for landing page
@layout MainLayout
@attribute [Microsoft.AspNetCore.Authorization.Authorize]

<h1>Feed</h1>
<!-- Authenticated content -->
```

**Other Public Pages:**

```razor
@* Login.razor *@
@page "/login"
@layout LandingLayout
@attribute [AllowAnonymous]

@* SignUp.razor *@
@page "/signup"
@layout LandingLayout
@attribute [AllowAnonymous]

@* Authentication.razor - Handles OIDC callbacks *@
@page "/authentication/{action}"
@attribute [AllowAnonymous]
```

---

### ✅ CORRECT Routes.razor Configuration

**Location:** `Sivar.Os.Client/Routes.razor`

```razor
<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)">
            <NotAuthorized>
                @{
                    // ⭐ CRITICAL: Check if page allows anonymous access
                    var pageType = routeData.PageType;
                    var allowAnonymous = pageType
                        .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
                        .Any();

                    if (allowAnonymous)
                    {
                        // Page allows anonymous, render it
                        var routeValues = routeData.RouteValues.ToDictionary(
                            kv => kv.Key,
                            kv => (object?)kv.Value
                        );
                        
                        <DynamicComponent Type="@pageType" Parameters="@routeValues" />
                    }
                    else
                    {
                        // Redirect to login with return URL
                        var returnUrl = WebUtility.UrlEncode($"/{routeData.RouteValues["page"] ?? "home"}");
                        <RedirectToLogin ReturnUrl="@returnUrl" />
                    }
                }
            </NotAuthorized>
        </AuthorizeRouteView>
    </Found>
</Router>
```

**Why This Is Critical:**
- Without the `allowAnonymous` check, `AuthorizeRouteView` redirects ALL unauthenticated users
- Even if page has `[AllowAnonymous]`, it gets blocked
- Must use `DynamicComponent` to render allowed pages

---

### ✅ CORRECT Program.cs Cookie Configuration

**Location:** `Sivar.Os/Program.cs`

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Sivar.Auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // ⭐ CRITICAL: Allow public paths without redirect
        options.Events.OnRedirectToLogin = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            
            var requestPath = context.Request.Path.Value ?? "";
            
            logger.LogInformation(
                "[OnRedirectToLogin] Path={Path}, IsAuthenticated={IsAuth}",
                requestPath,
                context.HttpContext.User.Identity?.IsAuthenticated ?? false
            );

            // ⭐ CRITICAL: Don't redirect these public paths
            if (requestPath == "/" || 
                requestPath == "/welcome" || 
                requestPath.StartsWith("/authentication"))
            {
                logger.LogInformation(
                    "[OnRedirectToLogin] Allowing public path without redirect - Path={Path}",
                    requestPath
                );
                return Task.CompletedTask;
            }

            // All other paths redirect to Keycloak
            var returnUrl = WebUtility.UrlEncode(requestPath);
            context.Response.Redirect($"/authentication/login?returnUrl={returnUrl}");
            return Task.CompletedTask;
        };
    })
    .AddOpenIdConnect("oidc", options =>
    {
        // ... Keycloak configuration
    });
```

**OpenID Connect Logout Configuration:**

```csharp
.AddOpenIdConnect("oidc", options =>
{
    // ... other config

    options.Events.OnSignedOutCallbackRedirect = context =>
    {
        // ⭐ CRITICAL: Redirect to root after logout
        context.Response.Redirect("/");
        context.HandleResponse();
        return Task.CompletedTask;
    };
});
```

---

### Redirect Loop Prevention Checklist

**Problem**: After Keycloak login, endless redirects between `/` and authentication.

**Solution Checklist:**

- [ ] **Landing page at `/`** - Root route is public landing page
- [ ] **Home page at `/home`** - Authenticated home is NOT at root
- [ ] **Landing has `[AllowAnonymous]`** - Explicitly allows public access
- [ ] **Landing checks authentication state** - `OnInitializedAsync` redirects authenticated users to `/home`
- [ ] **Login returnUrl is `/home`** - Never return to `/` after login
- [ ] **Cookie middleware allows `/`** - `OnRedirectToLogin` doesn't redirect public paths
- [ ] **Routes.razor checks `AllowAnonymous`** - Renders pages with attribute instead of redirecting
- [ ] **Logout redirects to `/`** - `OnSignedOutCallbackRedirect` goes to landing page

---

### Common Issues & Solutions

#### Issue 1: "Can't access welcome page, redirected to Keycloak"

**Symptoms:**
- Accessing `https://localhost:5001/` immediately redirects
- Never see landing page

**Causes & Fixes:**

| Cause | Fix |
|-------|-----|
| Home.razor at `/` with `[Authorize]` | ✅ Move Home to `/home`, Landing to `/` |
| Landing missing `[AllowAnonymous]` | ✅ Add `@attribute [AllowAnonymous]` |
| Cookie middleware redirects `/` | ✅ Add path check in `OnRedirectToLogin` |
| Routes.razor always redirects | ✅ Check for `AllowAnonymous` attribute |

#### Issue 2: "Redirect loop after login"

**Symptoms:**
- Successfully log in to Keycloak
- Browser shows endless redirects
- Network tab shows repeated requests to `/` and `/authentication/login`

**Causes & Fixes:**

| Cause | Fix |
|-------|-----|
| returnUrl points to `/` | ✅ Change all login returnUrl to `/home` |
| Landing doesn't redirect authenticated users | ✅ Add `OnInitializedAsync` authentication check |
| Logout redirects to authenticated page | ✅ Set `OnSignedOutCallbackRedirect` to `/` |

#### Issue 3: "Login works but returns to landing page"

**Symptoms:**
- Login succeeds
- User sent back to landing page instead of home

**Cause & Fix:**

| Cause | Fix |
|-------|-----|
| returnUrl missing or wrong | ✅ Ensure `returnUrl=/home` in all login links |

---

### Authentication Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│  User accesses https://localhost:5001/                      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│  HTTP Middleware - Cookie Authentication                    │
│  OnRedirectToLogin checks if path is "/", "/welcome", etc.  │
│  ✅ Allows public paths without redirect                    │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│  Routes.razor - AuthorizeRouteView                          │
│  Checks for [AllowAnonymous] attribute                      │
│  ✅ Renders page with DynamicComponent if allowed           │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│  Landing.razor - OnInitializedAsync                         │
│  Checks if user is authenticated                            │
│  ✅ If authenticated → Navigate to /home                    │
│  ❌ If not → Show landing page with Sign In button          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓ (User clicks Sign In)
┌─────────────────────────────────────────────────────────────┐
│  Navigate to /authentication/login?returnUrl=/home          │
│  ⭐ returnUrl is /home, NOT /                               │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│  Keycloak Login                                             │
│  User authenticates                                         │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│  OnTokenValidated - Program.cs                              │
│  Create/retrieve user in database                           │
│  Set ActiveProfileId                                        │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│  Redirect to /home (from returnUrl)                         │
│  ✅ User sees authenticated home page                       │
│  ✅ NO redirect loop (not sent to /)                        │
└─────────────────────────────────────────────────────────────┘
```

---

### Keycloak Authentication Setup

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

### Debugging Authentication Issues

**Enable detailed logging:**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.AspNetCore.Authentication": "Debug",
        "Microsoft.AspNetCore.Authorization": "Debug"
      }
    }
  }
}
```

**Key log patterns:**

```
[OnRedirectToLogin] Path=/, IsAuthenticated=False
[OnRedirectToLogin] Allowing public path without redirect - Path=/

[Landing.OnInitializedAsync] User is authenticated, redirecting to /home
[Home.OnInitializedAsync] Loading feed for authenticated user
```

**Clear browser cookies:**

When testing authentication changes, ALWAYS clear cookies:
- Chrome: DevTools → Application → Cookies → Delete all
- Edge: DevTools → Application → Cookies → Delete all
- Or use Incognito/Private mode

---

### Key Lessons

1. **Three-Layer Authorization**
   - HTTP middleware (cookie authentication)
   - Component layer (AuthorizeRouteView)
   - Page level ([Authorize] / [AllowAnonymous])

2. **Root Route Must Be Public**
   - `/` should be landing page with `[AllowAnonymous]`
   - Authenticated home at `/home` or similar
   - Never put `[Authorize]` page at root

3. **Prevent Redirect Loops**
   - Landing page detects authenticated users
   - Redirects them immediately to `/home`
   - returnUrl never points to `/`

4. **Cookie Middleware Path Exclusions**
   - Public paths must be excluded from redirect
   - Check path in `OnRedirectToLogin` event
   - Return `Task.CompletedTask` for public paths

5. **Routes.razor Must Check Attributes**
   - `AuthorizeRouteView` doesn't automatically respect `[AllowAnonymous]`
   - Must use reflection to check attribute
   - Render with `DynamicComponent` if allowed

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

## Database Script System

### ⭐ PATTERN: SQL Script Management via Database

For database features that EF Core cannot handle (pgvector types, TimescaleDB hypertables, custom extensions), we use a **Database Script System** that stores and executes SQL scripts via the application.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Application Startup                      │
│                    (Updater.cs in XAF)                       │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                 SeedSqlScripts() Method                      │
│          Loads SQL files from disk, creates                  │
│          SqlScript entities in database                      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│             ExecuteSqlScriptBatch() Method                   │
│          Queries SqlScript entities, executes                │
│          SQL via ExecuteSqlRawAsync()                        │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                  PostgreSQL Database                         │
│          Scripts execute: Extensions, pgvector,              │
│          TimescaleDB, custom database features               │
└─────────────────────────────────────────────────────────────┘
```

### Why This Pattern?

**Problem:** EF Core 9.0 cannot handle certain database features:
- ❌ pgvector `Vector` type (incompatible with EF Core 9.0)
- ❌ TimescaleDB hypertables (database-level partitioning)
- ❌ PostgreSQL extensions (require raw SQL `CREATE EXTENSION`)
- ❌ Custom indexes (HNSW, GIN, etc.)

**Solution:** Store SQL scripts as entities, execute via raw SQL:
- ✅ Bypasses EF Core's type system completely
- ✅ Version-controlled (scripts tracked in Git)
- ✅ Idempotent (safe to run multiple times)
- ✅ Ordered execution (ExecutionOrder property)
- ✅ RunOnce support (prevents re-execution)
- ✅ Error tracking (LastExecutionError)
- ✅ Audit trail (ExecutionCount, LastExecutedAt)

### Existing SQL Scripts

The project currently has **5 SQL scripts** for PostgreSQL optimization:

#### Phase 5: pgvector Semantic Search

**1. ConvertContentEmbeddingToVector.sql** (ExecutionOrder: 1.0)
- **Purpose:** Convert ContentEmbedding column from TEXT to vector(384)
- **Why:** EF Core 9.0 cannot handle pgvector types
- **Features:**
  - Creates pgvector extension if not exists
  - Converts existing TEXT column to vector(384)
  - Creates HNSW index for cosine similarity search
  - Handles NULL and empty values
- **Location:** `Sivar.Os.Data/Scripts/ConvertContentEmbeddingToVector.sql`
- **Seeded by:** `Updater.SeedConvertContentEmbeddingToVectorScript()`

#### Phase 6: TimescaleDB Hypertables

**2. EnableTimescaleDB.sql** (ExecutionOrder: 2.0)
- **Purpose:** Enable TimescaleDB extension for time-series optimization
- **Features:** Creates TimescaleDB extension, verifies installation
- **Location:** `Sivar.Os.Data/Scripts/EnableTimescaleDB.sql`
- **Seeded by:** `Updater.SeedTimescaleDBEnableScript()`

**3. ConvertToHypertables.sql** (ExecutionOrder: 3.0)
- **Purpose:** Convert time-series tables to hypertables
- **Tables:** Activities (7d), Posts (30d), ChatMessages (7d), Notifications (7d)
- **Features:** Automatic partitioning, chunk exclusion, preserves indexes
- **Location:** `Sivar.Os.Data/Scripts/ConvertToHypertables.sql`
- **Seeded by:** `Updater.SeedConvertToHypertablesScript()`

**4. AddRetentionPolicies.sql** (ExecutionOrder: 4.0)
- **Purpose:** Automatic data cleanup for old chunks
- **Retention:** Activities (2yr), Posts (5yr), ChatMessages (1yr), Notifications (6mo)
- **Features:** Automatic background jobs, permanent deletion when exceeded
- **Location:** `Sivar.Os.Data/Scripts/AddRetentionPolicies.sql`
- **Seeded by:** `Updater.SeedRetentionPoliciesScript()`

**5. AddCompressionPolicies.sql** (ExecutionOrder: 5.0)
- **Purpose:** Automatic compression for storage savings (60-90% reduction)
- **Compression:** Activities/ChatMessages/Notifications (30d), Posts (90d)
- **Features:** Segment by user/author/chat, order by CreatedAt DESC
- **Location:** `Sivar.Os.Data/Scripts/AddCompressionPolicies.sql`
- **Seeded by:** `Updater.SeedCompressionPoliciesScript()`

#### Phase 7: TimescaleDB Continuous Aggregates

**6. AddContinuousAggregates.sql** (ExecutionOrder: 6.0)
- **Purpose:** Create real-time analytics materialized views for dashboards
- **Aggregates:**
  - `post_metrics_daily` - Daily post statistics by author and type
  - `activity_metrics_hourly` - Hourly activity stream statistics
  - `user_engagement_daily` - Daily user engagement metrics
  - `post_engagement_daily` - Daily post engagement with reactions/comments
- **Features:**
  - Automatic refresh policies (hourly/daily)
  - Pre-computed aggregations (1000x faster than on-demand queries)
  - HNSW indexes for fast lookups
  - Materialized view optimization
- **Location:** `Sivar.Os.Data/Scripts/AddContinuousAggregates.sql`
- **Seeded by:** `Updater.SeedContinuousAggregatesScript()`
- **Repository:** `AnalyticsRepository.cs` (13 query methods)
- **API:** `AnalyticsController.cs` (9 REST endpoints)

### How to Add More Continuous Aggregates

TimescaleDB continuous aggregates are **materialized views** that automatically refresh and provide real-time analytics. They're perfect for dashboard metrics, reports, and time-series data.

#### When to Add a Continuous Aggregate

✅ **DO use continuous aggregates for:**
- Dashboard metrics (user counts, post stats, engagement metrics)
- Time-series reports (daily/hourly/monthly aggregations)
- Frequently queried analytics (same query running multiple times)
- Complex JOINs that are expensive to compute on-demand
- Data that updates less frequently than it's queried

❌ **DON'T use continuous aggregates for:**
- Real-time data that must be 100% up-to-date (use hypertables directly)
- Simple queries that are already fast
- One-time reports or rarely used analytics
- Data with complex WHERE clauses (aggregates are pre-filtered)

#### Step-by-Step Guide: Adding New Continuous Aggregates

**Step 1: Design Your Aggregate Query**

First, write a standard SQL query that returns the data you need:

```sql
-- Example: Hourly comment metrics
SELECT 
    time_bucket('1 hour', "CreatedAt") AS bucket,
    "PostId",
    COUNT(*) AS total_comments,
    COUNT(DISTINCT "UserId") AS unique_commenters,
    AVG(CHAR_LENGTH("Content")) AS avg_comment_length
FROM "Sivar_Comments"
WHERE "IsDeleted" = false
GROUP BY bucket, "PostId"
ORDER BY bucket DESC;
```

**Step 2: Convert to Continuous Aggregate**

Wrap your query in a `CREATE MATERIALIZED VIEW` statement:

```sql
-- =====================================================
-- Continuous Aggregate: comment_metrics_hourly
-- Purpose: Track hourly comment activity per post
-- Refresh Policy: Every 1 hour
-- =====================================================

CREATE MATERIALIZED VIEW IF NOT EXISTS comment_metrics_hourly
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 hour', "CreatedAt") AS bucket,
    "PostId",
    COUNT(*) AS total_comments,
    COUNT(DISTINCT "UserId") AS unique_commenters,
    AVG(CHAR_LENGTH("Content")) AS avg_comment_length
FROM "Sivar_Comments"
WHERE "IsDeleted" = false
GROUP BY bucket, "PostId";

-- Create index for fast lookups
CREATE INDEX IF NOT EXISTS idx_comment_metrics_hourly_bucket 
ON comment_metrics_hourly (bucket DESC);

CREATE INDEX IF NOT EXISTS idx_comment_metrics_hourly_post 
ON comment_metrics_hourly ("PostId");

-- Set automatic refresh policy
SELECT add_continuous_aggregate_policy(
    'comment_metrics_hourly',
    start_offset => INTERVAL '3 hours',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour'
);
```

**Step 3: Add to AddContinuousAggregates.sql**

Add your aggregate to the existing SQL script file:

**Location:** `Sivar.Os.Data/Scripts/AddContinuousAggregates.sql`

```sql
-- ... existing aggregates ...

-- =====================================================
-- Continuous Aggregate 5: comment_metrics_hourly
-- Purpose: Track hourly comment activity per post
-- Refresh Policy: Every 1 hour
-- Performance: ~1000x faster than querying Comments table
-- =====================================================

-- (Your SQL from Step 2)
```

**Step 4: Update Updater.cs (If Needed)**

If you're adding to the existing script, **no changes needed**. The script will be re-executed if not marked as `RunOnce`, or you can reset it.

If creating a **new separate script** for comments analytics:

```csharp
/// <summary>
/// Seeds the AddCommentAggregates SQL script if it doesn't exist
/// </summary>
private void SeedCommentAggregatesScript()
{
    const string scriptName = "AddCommentAggregates";
    
    var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
        .FirstOrDefault(s => s.Name == scriptName);
    
    if (existingScript != null)
    {
        System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' already exists. Skipping.");
        return;
    }
    
    System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Creating seed script: {scriptName}");
    
    var script = ObjectSpace.CreateObject<SqlScript>();
    script.Name = scriptName;
    script.Description = "Creates continuous aggregates for comment analytics";
    script.ExecutionOrder = 7.0m;  // After AddContinuousAggregates (6.0)
    script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
    script.IsActive = true;
    script.RunOnce = true;
    
    script.SqlText = LoadScriptFromFile("AddCommentAggregates.sql");
    
    System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' created successfully.");
}

// Call in SeedSqlScripts()
private void SeedSqlScripts()
{
    SeedConvertContentEmbeddingToVectorScript();  // Order 1.0
    SeedTimescaleDBEnableScript();                // Order 2.0
    SeedConvertToHypertablesScript();             // Order 3.0
    SeedRetentionPoliciesScript();                // Order 4.0
    SeedCompressionPoliciesScript();              // Order 5.0
    SeedContinuousAggregatesScript();             // Order 6.0
    SeedCommentAggregatesScript();                // Order 7.0 ⭐ NEW
}
```

**Step 5: Create Repository Methods**

Add query methods to `AnalyticsRepository.cs` (or create `CommentAnalyticsRepository.cs`):

```csharp
/// <summary>
/// Get hourly comment metrics for a specific post
/// </summary>
public async Task<List<CommentMetricsHourlyDto>> GetCommentMetricsByPostAsync(
    Guid postId,
    DateTime? startDate = null,
    DateTime? endDate = null)
{
    var requestId = Guid.NewGuid().ToString("N");
    _logger.LogInformation(
        "[GetCommentMetricsByPostAsync] START - RequestId={RequestId}, PostId={PostId}, StartDate={StartDate}, EndDate={EndDate}",
        requestId, postId, startDate, endDate);

    try
    {
        var query = @"
            SELECT 
                bucket,
                ""PostId"",
                total_comments,
                unique_commenters,
                avg_comment_length
            FROM comment_metrics_hourly
            WHERE ""PostId"" = {0}
            AND bucket >= {1}
            AND bucket <= {2}
            ORDER BY bucket DESC";

        var start = startDate ?? DateTime.UtcNow.AddDays(-7);
        var end = endDate ?? DateTime.UtcNow;

        var results = await _context.Database
            .SqlQueryRaw<CommentMetricsHourlyDto>(query, postId, start, end)
            .ToListAsync();

        _logger.LogInformation(
            "[GetCommentMetricsByPostAsync] SUCCESS - RequestId={RequestId}, Results={Count}",
            requestId, results.Count);

        return results;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "[GetCommentMetricsByPostAsync] FAILED - RequestId={RequestId}, PostId={PostId}",
            requestId, postId);
        throw;
    }
}
```

**Step 6: Create DTOs**

Add DTOs to `AnalyticsDTOs.cs`:

```csharp
/// <summary>
/// DTO for hourly comment metrics
/// Maps to comment_metrics_hourly continuous aggregate
/// </summary>
public class CommentMetricsHourlyDto
{
    public DateTime bucket { get; set; }
    public Guid PostId { get; set; }
    public int total_comments { get; set; }
    public int unique_commenters { get; set; }
    public double avg_comment_length { get; set; }
}
```

**Step 7: Add API Endpoints**

Add endpoints to `AnalyticsController.cs`:

```csharp
/// <summary>
/// Get hourly comment metrics for a specific post
/// </summary>
[HttpGet("comments/post/{postId}")]
public async Task<ActionResult<List<CommentMetricsHourlyDto>>> GetCommentMetricsByPost(
    Guid postId,
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null)
{
    try
    {
        var results = await _analyticsRepository.GetCommentMetricsByPostAsync(
            postId, startDate, endDate);
        return Ok(results);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get comment metrics for post {PostId}", postId);
        return StatusCode(500, "Failed to retrieve comment metrics");
    }
}
```

**Step 8: Test Your Aggregate**

**1. Verify aggregate was created:**
```sql
-- List all continuous aggregates
SELECT view_name, refresh_lag, refresh_interval
FROM timescaledb_information.continuous_aggregates;
```

**2. Manually refresh (for testing):**
```sql
CALL refresh_continuous_aggregate('comment_metrics_hourly', NULL, NULL);
```

**3. Query the aggregate:**
```sql
SELECT * FROM comment_metrics_hourly
ORDER BY bucket DESC
LIMIT 10;
```

**4. Test the API endpoint:**
```bash
curl https://localhost:7165/api/analytics/comments/post/{postId}
```

#### Continuous Aggregate Best Practices

✅ **DO:**
- Use `time_bucket()` for time-based grouping (required for continuous aggregates)
- Create indexes on `bucket` column (almost always needed)
- Set appropriate `start_offset` and `end_offset` (controls refresh window)
- Choose `schedule_interval` based on data freshness needs (hourly, daily, etc.)
- Use `IF NOT EXISTS` for idempotency
- Add comprehensive comments explaining the aggregate's purpose
- Log all repository methods (START, SUCCESS, FAILED)

❌ **DON'T:**
- Don't use window functions (LAG, LEAD, ROW_NUMBER) - not supported
- Don't use volatile functions (NOW(), RANDOM()) - causes refresh issues
- Don't make refresh interval too short (increases database load)
- Don't forget indexes (aggregates can still be slow without them)
- Don't use `SELECT *` in aggregate definition
- Don't mix aggregation levels (e.g., daily + hourly in same view)

#### Common Patterns

**1. Daily Rollups:**
```sql
time_bucket('1 day', "CreatedAt") AS bucket
-- Refresh daily
schedule_interval => INTERVAL '1 day'
```

**2. Hourly Metrics:**
```sql
time_bucket('1 hour', "CreatedAt") AS bucket
-- Refresh hourly
schedule_interval => INTERVAL '1 hour'
```

**3. Weekly Summaries:**
```sql
time_bucket('1 week', "CreatedAt") AS bucket
-- Refresh daily (weekly buckets updated daily)
schedule_interval => INTERVAL '1 day'
```

**4. Multi-Dimensional Aggregates:**
```sql
-- Group by time AND category
SELECT 
    time_bucket('1 day', "CreatedAt") AS bucket,
    "Category",
    "Status",
    COUNT(*) AS total,
    AVG("Price") AS avg_price
FROM "Sivar_Products"
GROUP BY bucket, "Category", "Status";
```

#### Performance Benchmarks (Expected)

| Query Type | Without Aggregate | With Aggregate | Speedup |
|------------|------------------|----------------|---------|
| Simple COUNT | ~100ms | ~1ms | 100x |
| Complex JOINs | ~5000ms | ~10ms | 500x |
| Multi-GROUP BY | ~2000ms | ~2ms | 1000x |
| 7-day rollup | ~1500ms | ~3ms | 500x |

#### Troubleshooting Continuous Aggregates

**Check refresh policies:**
```sql
SELECT * FROM timescaledb_information.job_stats
WHERE job_id IN (
    SELECT job_id FROM timescaledb_information.jobs
    WHERE proc_name = 'policy_refresh_continuous_aggregate'
);
```

**Check last refresh:**
```sql
SELECT view_name, 
       completed_threshold,
       invalidation_threshold
FROM timescaledb_information.continuous_aggregates;
```

**Force manual refresh:**
```sql
CALL refresh_continuous_aggregate('your_aggregate_name', NULL, NULL);
```

**Drop and recreate aggregate (if broken):**
```sql
DROP MATERIALIZED VIEW IF EXISTS your_aggregate_name CASCADE;
-- Then run your creation script again
```

### How to Add a New Database Script

Follow this pattern when you need to add database features that EF Core cannot handle:

#### Step 1: Create SQL Script File

**Location:** `Sivar.Os.Data/Scripts/{ScriptName}.sql`

**Template:**
```sql
-- =====================================================
-- Script: {ScriptName}
-- Purpose: {Brief description}
-- Date: {Current date}
-- =====================================================

-- Your SQL code here
-- Make it IDEMPOTENT (safe to run multiple times)

-- Verification queries (optional)
SELECT * FROM ...;

-- =====================================================
-- IMPORTANT NOTES:
-- - Document any prerequisites
-- - Document any breaking changes
-- - Document expected results
-- =====================================================
```

**Best Practices:**
- ✅ Make scripts **idempotent** (use `IF NOT EXISTS`, `CREATE ... IF NOT EXISTS`)
- ✅ Include verification queries at the end
- ✅ Add comprehensive comments
- ✅ Handle NULL and edge cases
- ✅ Document expected behavior
- ✅ Use proper error handling (PL/pgSQL `DO` blocks)

#### Step 2: Add Seed Method in Updater.cs

**Location:** `Xaf.Sivar.Os/Xaf.Sivar.Os.Module/DatabaseUpdate/Updater.cs`

**Template:**
```csharp
/// <summary>
/// Seeds the {ScriptName} SQL script if it doesn't exist
/// </summary>
private void Seed{ScriptName}Script()
{
    const string scriptName = "{ScriptName}";
    
    // Check if script already exists
    var existingScript = ObjectSpace.GetObjectsQuery<SqlScript>()
        .FirstOrDefault(s => s.Name == scriptName);
    
    if (existingScript != null)
    {
        System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' already exists. Skipping.");
        return;
    }
    
    System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Creating seed script: {scriptName}");
    
    var script = ObjectSpace.CreateObject<SqlScript>();
    script.Name = scriptName;
    script.Description = "{Brief description}";
    script.ExecutionOrder = {X.0}m;  // Choose appropriate order
    script.BatchName = SqlScriptBatches.AfterSchemaUpdate;
    script.IsActive = true;
    script.RunOnce = true;  // Or false if should run every time
    
    // Load SQL from file
    script.SqlText = LoadScriptFromFile("{ScriptName}.sql");
    
    System.Diagnostics.Debug.WriteLine($"[SQL Scripts] Seed script '{scriptName}' created successfully.");
}
```

#### Step 3: Call Seed Method in SeedSqlScripts()

**Location:** Same file, in `SeedSqlScripts()` method

```csharp
private void SeedSqlScripts()
{
    SeedConvertContentEmbeddingToVectorScript();  // Order 1.0
    SeedTimescaleDBEnableScript();                // Order 2.0
    SeedConvertToHypertablesScript();             // Order 3.0
    SeedRetentionPoliciesScript();                // Order 4.0
    SeedCompressionPoliciesScript();              // Order 5.0
    Seed{YourNewScript}Script();                  // Order 6.0 ⭐ ADD HERE
}
```

#### Step 4: Test the Script

**Manual Testing (Recommended First):**
```bash
psql -h localhost -U postgres -d SivarOsDb
\i Sivar.Os.Data/Scripts/{ScriptName}.sql
```

**Automatic Testing:**
```bash
dotnet build
dotnet run --project Sivar.Os.Blazor.Server

# Watch Debug Output for:
# [SQL Scripts] Creating seed script: {ScriptName}
# [SQL Scripts] Executing: {ScriptName} (Order: X.0)
# [SQL Scripts] Successfully executed: {ScriptName}
```

**Verify Execution:**
```sql
SELECT "Name", "ExecutionOrder", "ExecutionCount", "LastExecutedAt", "LastExecutionError"
FROM "Sivar_SqlScripts"
ORDER BY "ExecutionOrder";
```

### Script Execution Order Reference

Current execution order:

1. **Order 1.0** - ConvertContentEmbeddingToVector (Phase 5: pgvector)
2. **Order 2.0** - EnableTimescaleDB (Phase 6: TimescaleDB)
3. **Order 3.0** - ConvertToHypertables (Phase 6: TimescaleDB)
4. **Order 4.0** - AddRetentionPolicies (Phase 6: TimescaleDB)
5. **Order 5.0** - AddCompressionPolicies (Phase 6: TimescaleDB)
6. **Order 6.0** - AddContinuousAggregates (Phase 7: TimescaleDB Continuous Aggregates)
7. **Order 7.0** - ⭐ **Available for next script**

**Ordering Rules:**
- Use decimal increments (1.0, 2.0, 3.0)
- Reserve space for future scripts (not 0.1 increments)
- Group related scripts together
- Can insert between scripts if needed (e.g., 1.5)

**Phase Dependencies:**
- Phase 5 (pgvector) has no dependencies
- Phase 6 (Hypertables) requires TimescaleDB extension (Order 2.0)
- Phase 7 (Continuous Aggregates) requires hypertables to exist (Order 3.0+)
- Future aggregates should use Order 7.0+ (after Phase 7)

### When to Use Database Script System

✅ **DO use for:**
- PostgreSQL extensions (TimescaleDB, pgvector, PostGIS)
- Custom indexes EF Core can't create (HNSW, GIN, GIST)
- Database-level features (partitioning, triggers, functions)
- Complex migrations EF Core can't handle
- Type conversions EF Core doesn't support

❌ **DON'T use for:**
- Regular schema changes (use EF Core migrations)
- Simple CRUD operations (use repositories)
- Business logic (belongs in services)
- Data seeding (use Updater seed methods directly)

### Troubleshooting

**Check if script was seeded:**
```sql
SELECT "Name", "ExecutionOrder", "IsActive", "RunOnce"
FROM "Sivar_SqlScripts"
ORDER BY "ExecutionOrder";
```

**Check execution history:**
```sql
SELECT "Name", "ExecutionCount", "LastExecutedAt", 
       SUBSTRING("LastExecutionError", 1, 100) as "Error"
FROM "Sivar_SqlScripts"
WHERE "ExecutionCount" > 0
ORDER BY "LastExecutedAt" DESC;
```

**Re-enable script execution:**
```sql
-- Reset execution count (for RunOnce scripts)
UPDATE "Sivar_SqlScripts"
SET "ExecutionCount" = 0, "LastExecutedAt" = NULL, "LastExecutionError" = NULL
WHERE "Name" = '{ScriptName}';
```

### Related Documentation

- `PHASE_5_COMPLETE_STATUS.md` - pgvector implementation details
- `PHASE_6_IMPLEMENTATION_COMPLETE.md` - TimescaleDB implementation details
- `PHASE_7_CONTINUOUS_AGGREGATES_COMPLETE.md` - Continuous aggregates implementation
- `posimp.md` - PostgreSQL optimization roadmap (88% complete, 7/8 phases)
- `Sivar.Os.Data/Scripts/` - All SQL script files
- `AnalyticsRepository.cs` - Repository with 13 analytics query methods
- `AnalyticsController.cs` - REST API with 9 analytics endpoints

### Quick Reference: Adding Analytics Features

**For new aggregate metrics:**
1. Design query using existing hypertables (Activities, Posts, ChatMessages, Notifications)
2. Add to `AddContinuousAggregates.sql` or create new script file
3. Create DTOs in `AnalyticsDTOs.cs`
4. Add repository methods in `AnalyticsRepository.cs`
5. Add API endpoints in `AnalyticsController.cs`
6. Test with `CALL refresh_continuous_aggregate()` and API calls

**For new time-series tables:**
1. Create table via EF Core migration
2. Convert to hypertable in `ConvertToHypertables.sql` (or new script)
3. Add retention policy in `AddRetentionPolicies.sql`
4. Add compression policy in `AddCompressionPolicies.sql`
5. Create aggregates in `AddContinuousAggregates.sql`

**Key Performance Targets:**
- Continuous aggregate queries: **< 100ms** (vs 5-10 seconds raw queries)
- Dashboard summary endpoint: **< 200ms** (9 aggregates combined)
- Refresh policies: Hourly for real-time, daily for historical
- Compression savings: **60-90%** storage reduction after 30-90 days

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
