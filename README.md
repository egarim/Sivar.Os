# Sivar.Os - Blazor Interactive Application

A .NET 9 Blazor application with support for multiple render modes (Server, WebAssembly, and Auto).

## Setting the Application-Level Render Mode

This application uses **application-level render mode** configuration, meaning all pages share the same render mode set globally in the `App.razor` component.

### Current Configuration

The render mode is configured in **`Components/App.razor`**:

```razor
<HeadOutlet @rendermode="InteractiveAuto" />

<body>
    <CascadingAuthenticationState>
        <Routes @rendermode="InteractiveAuto" />
    </CascadingAuthenticationState>
</body>
```

### Available Render Modes

You can change the render mode by modifying the `@rendermode` attribute in `App.razor`. Here are your options:

#### 1. **InteractiveAuto** (Current - Recommended) ?
```razor
@rendermode="InteractiveAuto"
```
- **Best for**: Most applications
- **Behavior**: 
  - Initially renders on the server
  - Downloads WebAssembly in the background
  - Subsequent navigations run in WebAssembly (faster, offline-capable)
  - Automatically transitions from Server ? WASM
- **Pros**: Best of both worlds - fast initial load + rich client experience
- **Cons**: Requires both Server and WASM projects

#### 2. **InteractiveServer**
```razor
@rendermode="InteractiveServer"
```
- **Best for**: Low-bandwidth scenarios, real-time apps, internal tools
- **Behavior**: 
  - All interactivity runs on the server via SignalR
  - UI updates sent over WebSocket connection
- **Pros**: 
  - Smaller download size
  - Full access to server resources
  - Better for low-powered devices
- **Cons**: 
  - Requires persistent connection
  - Higher server load
  - Not offline-capable

#### 3. **InteractiveWebAssembly**
```razor
@rendermode="InteractiveWebAssembly"
```
- **Best for**: Offline-capable apps, client-heavy workloads
- **Behavior**: 
  - Entire app runs in the browser via WebAssembly
  - No server connection needed after initial load
- **Pros**: 
  - Works offline
  - Lower server load
  - Faster after initial load
- **Cons**: 
  - Larger initial download (~2-5 MB)
  - Longer initial load time

#### 4. **Static** (No Interactivity)
```razor
@rendermode="@null"
```
or remove the `@rendermode` attribute entirely.

- **Best for**: Static content pages, documentation
- **Behavior**: 
  - Server-side rendering only
  - No interactive components
- **Pros**: 
  - Fastest initial load
  - Best for SEO
  - Minimal resources
- **Cons**: 
  - No interactivity (buttons, forms won't work)

### How to Change the Render Mode

**Step 1:** Open `Sivar.Os/Components/App.razor`

**Step 2:** Find these lines:
```razor
<HeadOutlet @rendermode="InteractiveAuto" />
```
and
```razor
<Routes @rendermode="InteractiveAuto" />
```

**Step 3:** Replace `InteractiveAuto` with your desired mode:

**Example - Switch to Server-only:**
```razor
<HeadOutlet @rendermode="InteractiveServer" />
<Routes @rendermode="InteractiveServer" />
```

**Example - Switch to WebAssembly-only:**
```razor
<HeadOutlet @rendermode="InteractiveWebAssembly" />
<Routes @rendermode="InteractiveWebAssembly" />
```

**Step 4:** Rebuild and restart the application

### Required Services for Each Render Mode

Make sure your `Program.cs` has the appropriate services registered:

**For InteractiveServer or InteractiveAuto:**
```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

**For InteractiveWebAssembly or InteractiveAuto:**
```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
```

**Current configuration** (supports all modes):
```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
```

And in the app builder:
```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Sivar.Os.Client._Imports).Assembly);
```

## Visual Render Mode Indicator

The application includes a **render mode indicator** in the top-right corner of the app bar that shows:
- ?? **WASM** - When running in WebAssembly mode
- ??? **Server** - When running in Server mode

This indicator automatically detects the current runtime environment and updates accordingly.

## Architecture Notes

### Project Structure
- **Sivar.Os** - Server project (hosts the app)
- **Sivar.Os.Client** - WebAssembly project (runs in browser)
- **Sivar.Os.Shared** - Shared code between server and client

### Authentication
This application uses **Keycloak OIDC** for authentication. The authentication flow is handled server-side regardless of render mode, with the client fetching the auth state via API when running in WebAssembly.

### Services
Services are registered adaptively:
- **Server mode**: Uses `ServerAuthenticationService` and `ServerWeatherService`
- **WebAssembly mode**: Uses `ClientAuthenticationService` and `ClientWeatherService`
- The `IHttpContextAccessor` is used to detect the current execution context

## Development

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022 or VS Code
- Keycloak instance (or update `appsettings.json` with your identity provider)

### Running the Application

```bash
cd Sivar.Os
dotnet run
```

Or use Visual Studio to run the `Sivar.Os` project.

### Configuration

Update `appsettings.json` with your settings:

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/your-realm",
    "ClientIdServer": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

## Troubleshooting

### Issue: Components not interactive
**Solution**: Verify `@rendermode` is set in `App.razor` and not `@null`

### Issue: WebAssembly not loading
**Solution**: 
1. Check that `.AddInteractiveWebAssemblyComponents()` is registered in `Program.cs`
2. Ensure `Sivar.Os.Client` project is referenced and builds successfully

### Issue: Authentication not working in WASM
**Solution**: This is expected for the first load. The app fetches auth state from the server via the `/api/authentication/profile` endpoint.

## Learn More

- [Blazor Render Modes (.NET 9)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes)
- [Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly)
- [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-server)