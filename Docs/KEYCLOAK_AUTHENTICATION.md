# Keycloak Authentication in Sivar.Os

## Overview

This document provides a comprehensive guide to how Keycloak authentication is implemented in the Sivar.Os application, which uses Blazor's **Interactive Auto Render Mode** (hybrid Server + WebAssembly).

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Keycloak Configuration](#keycloak-configuration)
- [Server-Side Authentication Setup](#server-side-authentication-setup)
- [Client-Side Authentication Setup](#client-side-authentication-setup)
- [Adaptive Authentication Services](#adaptive-authentication-services)
- [Authentication Flow](#authentication-flow)
- [Logout Implementation](#logout-implementation)
- [Troubleshooting](#troubleshooting)

---

## Architecture Overview

The application uses a **hybrid authentication approach** to support Blazor's Auto render mode:

### Render Modes

- **Server-Side Rendering (SSR)**: Initial page load and server-prerendered components
- **Interactive Server**: Real-time server components with SignalR
- **Interactive WebAssembly**: Client-side components running in the browser

### Authentication Strategy

```
┌─────────────────────────────────────────────────────────────────┐
│                         Keycloak                                 │
│                   (Identity Provider)                            │
└───────────────────┬─────────────────────────────────────────────┘
                    │ OpenID Connect (OIDC)
                    │
    ┌───────────────▼───────────────┐
    │   Server (Sivar.Os)           │
    │   - Cookie Authentication     │
    │   - OpenID Connect            │
    │   - AuthenticationController  │
    └───────────────┬───────────────┘
                    │
        ┌───────────┴───────────┐
        │                       │
┌───────▼────────┐    ┌────────▼────────────┐
│ Server Mode    │    │ WebAssembly Mode    │
│ Components     │    │ Components          │
│ - HttpContext  │    │ - API Calls         │
│ - Direct Auth  │    │ - Cookie-based      │
└────────────────┘    └─────────────────────┘
```

---

## Keycloak Configuration

### Server Configuration (appsettings.json)

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/blazor-interactive",
    "MetadataAddress": "http://localhost:8080/realms/blazor-interactive/.well-known/openid-configuration",
    "ClientIdServer": "myhybridapp-server",
    "ClientSecret": "yXXRrqKnQ3xFuQfEkeONJJtRzmQ2Pq1g"
  }
}
```

### Required Keycloak Client Settings

In your Keycloak admin console (`http://localhost:8080`), configure the client:

#### Basic Settings
- **Client ID**: `myhybridapp-server`
- **Client Protocol**: `openid-connect`
- **Access Type**: `confidential` (requires client secret)

#### Valid Redirect URIs
Add all possible redirect URIs where Keycloak can redirect after login:
```
https://localhost:5001/*
http://localhost:5001/*
https://localhost:5001/signin-oidc
http://localhost:5001/signin-oidc
```

#### Valid Post Logout Redirect URIs
Add URIs where users should be redirected after logout:
```
https://localhost:5001/*
http://localhost:5001/*
https://localhost:5001/
http://localhost:5001/
```

#### Web Origins
For CORS support:
```
https://localhost:5001
http://localhost:5001
```

#### Scopes
Ensure these scopes are enabled:
- `openid` ✓
- `profile` ✓
- `email` ✓

---

## Server-Side Authentication Setup

### Program.cs - Authentication Configuration

Located in: `Sivar.Os/Program.cs`

#### 1. OpenID Connect & Cookie Authentication

```csharp
var authority = builder.Configuration["Keycloak:Authority"] 
    ?? "http://localhost:8080/realms/blazor-interactive";
var metadata = builder.Configuration["Keycloak:MetadataAddress"] 
    ?? $"{authority}/.well-known/openid-configuration";
var clientId = builder.Configuration["Keycloak:ClientIdServer"] 
    ?? "myhybridapp-server";
var clientSecret = builder.Configuration["Keycloak:ClientSecret"] 
    ?? "CHANGE_ME";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            // Handle different request types
            var req = context.Request;
            var isApiRequest = req.Path.StartsWithSegments("/api")
                || req.Headers.TryGetValue("X-Requested-With", out StringValues header) 
                    && header == "XMLHttpRequest"
                || req.Headers.TryGetValue("Accept", out StringValues accept) 
                    && accept.ToString().Contains("application/json");

            if (isApiRequest)
            {
                // API requests get 401, not redirects
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            // Browser requests redirect to login or welcome page
            if (context.Request.Path == "/" || context.Request.Path == "")
            {
                context.Response.Redirect("/welcome");
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);
            }
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            // Similar handling for 403 Forbidden
            var req = context.Request;
            var isApiRequest = req.Path.StartsWithSegments("/api")
                || req.Headers.TryGetValue("X-Requested-With", out StringValues header) 
                    && header == "XMLHttpRequest"
                || req.Headers.TryGetValue("Accept", out StringValues accept) 
                    && accept.ToString().Contains("application/json");

            if (isApiRequest)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }
    };
})
.AddOpenIdConnect(options =>
{
    // Development setting - allow HTTP (NOT for production!)
    options.RequireHttpsMetadata = false;
    
    // Keycloak endpoints
    options.Authority = authority;
    options.MetadataAddress = metadata;
    
    // Client credentials
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    
    // OIDC settings
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    
    // Scopes
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    
    // Development setting - skip issuer validation for HTTP
    options.TokenValidationParameters.ValidateIssuer = false;
    
    // Event handlers
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            // Handle registration requests
            if (context.Properties.Items.TryGetValue("prompt", out var prompt) 
                && prompt == "create")
            {
                // Keycloak-specific parameter to show registration page
                context.ProtocolMessage.SetParameter("kc_action", "REGISTER");
            }
            
            // Handle logout redirect
            if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
            {
                // Override post_logout_redirect_uri to use root path
                // This ensures the URI is already registered in Keycloak
                var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
                context.ProtocolMessage.PostLogoutRedirectUri = baseUrl + "/";
            }
            
            return Task.CompletedTask;
        },
        OnSignedOutCallbackRedirect = context =>
        {
            // After successful logout, redirect to welcome page
            context.Response.Redirect("/welcome");
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
```

#### 2. Cascading Authentication State

```csharp
// Enable authentication state to cascade to all components
builder.Services.AddCascadingAuthenticationState();
```

#### 3. Post-Logout Middleware

```csharp
// After UseAuthentication() and UseAuthorization()
app.Use(async (context, next) =>
{
    // Redirect unauthenticated root access to welcome page
    if (context.Request.Path == "/" && 
        context.Request.Query.ContainsKey("logout") == false &&
        !context.User.Identity?.IsAuthenticated == true)
    {
        var referer = context.Request.Headers.Referer.ToString();
        
        // Check if this is a post-logout redirect from Keycloak
        if (string.IsNullOrEmpty(referer) || 
            referer.Contains("keycloak") || 
            referer.Contains("localhost:8080"))
        {
            context.Response.Redirect("/welcome");
            return;
        }
    }
    await next();
});
```

---

## Client-Side Authentication Setup

### Program.cs (WebAssembly)

Located in: `Sivar.Os.Client/Program.cs`

```csharp
// Custom authentication state provider for WASM
builder.Services.AddScoped<AuthenticationStateProvider, WasmAuthenticationStateProvider>();

// Client-side authentication service
builder.Services.AddScoped<IAuthenticationService, ClientAuthenticationService>();

// Authorization services required by AuthorizeView
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();
```

### WasmAuthenticationStateProvider

Located in: `Sivar.Os.Client/Auth/WasmAuthenticationStateProvider.cs`

This provider fetches authentication state from the server:

```csharp
public override async Task<AuthenticationState> GetAuthenticationStateAsync()
{
    try
    {
        // Use JS fetch with credentials to include cookies
        var jsonText = await _jsRuntime.InvokeAsync<string>(
            "fetchWithCredentials", 
            "authentication/profile"
        );
        
        // Parse profile response
        var json = System.Text.Json.JsonSerializer
            .Deserialize<Dictionary<string, object>>(jsonText);
        
        if (json != null && json.TryGetValue("isAuthenticated", out var isAuthObj))
        {
            bool isAuth = /* parse boolean */;
            
            if (isAuth)
            {
                // Parse claims and create authenticated principal
                var claims = ParseClaimsFromDictionary(json);
                var identity = new ClaimsIdentity(claims, "Server");
                var user = new ClaimsPrincipal(identity);
                return new AuthenticationState(user);
            }
        }
        
        // Return unauthenticated state
        return new AuthenticationState(new ClaimsPrincipal());
    }
    catch
    {
        return new AuthenticationState(new ClaimsPrincipal());
    }
}
```

### JavaScript Interop (wwwroot/index.html)

```javascript
window.fetchWithCredentials = async function (url) {
    const response = await fetch(url, {
        credentials: 'include'  // Include cookies in request
    });
    return await response.text();
};
```

---

## Adaptive Authentication Services

The application uses **adaptive services** that work differently based on the render mode.

### IAuthenticationService Interface

Located in: `Sivar.Os.Shared/Services/IAuthenticationService.cs`

```csharp
public interface IAuthenticationService
{
    Task<AuthenticationState> GetAuthenticationStateAsync();
}

public class AuthenticationState
{
    public bool IsAuthenticated { get; set; }
    public string? Name { get; set; }
    public ClaimsPrincipal? Principal { get; set; }
}
```

### Server Implementation

Located in: `Sivar.Os/Services/ServerAuthenticationService.cs`

```csharp
public class ServerAuthenticationService : IAuthenticationService
{
    private readonly IHttpContextAccessor _contextAccessor;

    public Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _contextAccessor.HttpContext;
        
        // Direct access to HttpContext on server
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var user = httpContext.User;
            var name = user.FindFirst(c => c.Type == ClaimTypes.Name)?.Value
                    ?? user.FindFirst(c => c.Type == "name")?.Value
                    ?? user.FindFirst(c => c.Type == "preferred_username")?.Value
                    ?? "User";

            return Task.FromResult(new AuthenticationState
            {
                IsAuthenticated = true,
                Name = name,
                Principal = user
            });
        }

        return Task.FromResult(new AuthenticationState
        {
            IsAuthenticated = false
        });
    }
}
```

### Client Implementation

Located in: `Sivar.Os.Client/Services/ClientAuthenticationService.cs`

```csharp
public class ClientAuthenticationService : IAuthenticationService
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Get auth state from the custom provider
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var isAuthenticated = user.Identity?.IsAuthenticated ?? false;

            return new AuthenticationState
            {
                IsAuthenticated = isAuthenticated,
                Name = user.Identity?.Name,
                Principal = user
            };
        }
        catch
        {
            return new AuthenticationState
            {
                IsAuthenticated = false
            };
        }
    }
}
```

### Registration Strategy

In **Server** (`Sivar.Os/Program.cs`):
```csharp
builder.Services.AddScoped<IAuthenticationService>(sp =>
{
    var contextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    return new ServerAuthenticationService(contextAccessor);
});
```

In **Client** (`Sivar.Os.Client/Program.cs`):
```csharp
builder.Services.AddScoped<IAuthenticationService, ClientAuthenticationService>();
```

---

## Authentication Flow

### Login Flow

```
1. User clicks "Sign In" → Navigates to /authentication/login
                          ↓
2. AuthenticationController.Login() → Challenge(OpenIdConnect)
                          ↓
3. Redirect to Keycloak → User enters credentials
                          ↓
4. Keycloak validates → Redirects to /signin-oidc with auth code
                          ↓
5. OIDC Middleware → Exchanges code for tokens
                          ↓
6. Creates Cookie → Saves claims in authentication cookie
                          ↓
7. Redirect to returnUrl → User sees authenticated page
```

### Registration Flow

```
1. User clicks "Sign Up" → Navigates to /authentication/register
                          ↓
2. AuthenticationController.Register() 
   → Challenge(OpenIdConnect) with prompt="create"
                          ↓
3. OnRedirectToIdentityProvider event
   → Adds kc_action=REGISTER parameter
                          ↓
4. Redirect to Keycloak → Keycloak shows registration form
                          ↓
5. User registers → Keycloak creates account
                          ↓
6. Auto-login → Same as login flow steps 4-7
```

### Component Authentication Check (Auto Mode)

#### Server-Side Components
```
Component renders on server
         ↓
Injects IAuthenticationService
         ↓
Calls GetAuthenticationStateAsync()
         ↓
ServerAuthenticationService
         ↓
Reads HttpContext.User directly
         ↓
Returns authentication state immediately
```

#### WebAssembly Components
```
Component renders in browser
         ↓
Injects IAuthenticationService
         ↓
Calls GetAuthenticationStateAsync()
         ↓
ClientAuthenticationService
         ↓
Calls WasmAuthenticationStateProvider
         ↓
Fetches /authentication/profile via JS interop
         ↓
Server returns profile with claims
         ↓
Parses claims and creates ClaimsPrincipal
         ↓
Returns authentication state
```

---

## Logout Implementation

### The Challenge

When logging out with Keycloak, the standard OIDC flow redirects to:
```
http://localhost:8080/realms/blazor-interactive/protocol/openid-connect/logout
  ?post_logout_redirect_uri=https://localhost:5001/signout-callback-oidc
  &id_token_hint=<token>
```

**Problem**: If `/signout-callback-oidc` is not registered in Keycloak's valid redirect URIs, Keycloak shows an error: "Invalid redirect uri"

### The Solution

#### Step 1: Use Root URL for Post-Logout Redirect

Instead of using the default `/signout-callback-oidc`, override it to use `/` which is already registered:

```csharp
// In OpenIdConnectEvents
OnRedirectToIdentityProvider = context =>
{
    if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
    {
        // Override to use root path
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        context.ProtocolMessage.PostLogoutRedirectUri = baseUrl + "/";
    }
    return Task.CompletedTask;
}
```

#### Step 2: Catch Post-Logout Redirect

Add middleware to intercept unauthenticated root access after logout:

```csharp
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && 
        !context.User.Identity?.IsAuthenticated == true)
    {
        var referer = context.Request.Headers.Referer.ToString();
        
        // If coming from Keycloak, redirect to welcome
        if (string.IsNullOrEmpty(referer) || 
            referer.Contains("keycloak") || 
            referer.Contains("localhost:8080"))
        {
            context.Response.Redirect("/welcome");
            return;
        }
    }
    await next();
});
```

#### Step 3: Handle OIDC Callback Event

```csharp
OnSignedOutCallbackRedirect = context =>
{
    context.Response.Redirect("/welcome");
    context.HandleResponse();
    return Task.CompletedTask;
}
```

### Complete Logout Flow

```
1. User clicks "Logout" → /authentication/logout
                        ↓
2. AuthenticationController.Logout()
   → SignOut("Cookies", "OpenIdConnect")
   → RedirectUri = "/"
                        ↓
3. OIDC Middleware → Constructs logout URL
   → OnRedirectToIdentityProvider event
   → Overrides post_logout_redirect_uri to "/"
                        ↓
4. Redirect to Keycloak logout endpoint
                        ↓
5. Keycloak logs out → Redirects to "https://localhost:5001/"
                        ↓
6. Custom Middleware → Detects unauthenticated root access
   → Checks referer contains "keycloak"
   → Redirects to "/welcome"
                        ↓
7. User sees landing page ✓
```

### AuthenticationController Logout Endpoints

Located in: `Sivar.Os/Controllers/AuthenticationController.cs`

```csharp
[HttpGet("logout")]
public IActionResult Logout()
{
    var authenticationProperties = new AuthenticationProperties
    {
        RedirectUri = "/"  // Will be overridden to root in OIDC event
    };

    return SignOut(authenticationProperties, 
        "Cookies",
        OpenIdConnectDefaults.AuthenticationScheme);
}

[HttpPost("logout")]
public async Task<IActionResult> LogoutPost()
{
    var authenticationProperties = new AuthenticationProperties
    {
        RedirectUri = "/"
    };

    return SignOut(authenticationProperties, 
        "Cookies",
        OpenIdConnectDefaults.AuthenticationScheme);
}
```

---

## Troubleshooting

### Common Issues

#### 1. "Invalid redirect uri" on Logout

**Symptom**: After clicking logout, Keycloak shows an error page

**Solution**: 
- Add valid post-logout redirect URIs in Keycloak client settings
- Or use the root URL override approach (already implemented)

#### 2. Authentication Not Working in WebAssembly

**Symptom**: Components show as unauthenticated when running in WASM mode

**Causes**:
- Cookies not being sent with requests
- CORS issues
- JavaScript interop not configured

**Solutions**:
```javascript
// Ensure this is in wwwroot/index.html or app.js
window.fetchWithCredentials = async function (url) {
    const response = await fetch(url, {
        credentials: 'include'  // Critical!
    });
    return await response.text();
};
```

#### 3. Claims Not Available in Components

**Symptom**: `@context.User.Claims` is empty even when authenticated

**Cause**: Using wrong authentication service or claims not being propagated

**Solution**:
```csharp
// Ensure you're injecting the right service
@inject IAuthenticationService AuthService

// Or use AuthorizeView
<AuthorizeView>
    <Authorized>
        <p>Hello, @context.User.Identity.Name!</p>
    </Authorized>
</AuthorizeView>
```

#### 4. 401 Unauthorized Loops

**Symptom**: Infinite redirect loops when trying to access protected pages

**Cause**: Cookie authentication events not properly configured

**Solution**: Check `OnRedirectToLogin` event in `Program.cs`:
```csharp
OnRedirectToLogin = context =>
{
    // For API requests, return 401, don't redirect
    var isApiRequest = context.Request.Path.StartsWithSegments("/api");
    if (isApiRequest)
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    }
    
    context.Response.Redirect(context.RedirectUri);
    return Task.CompletedTask;
}
```

#### 5. HTTPS/HTTP Issues

**Symptom**: Mixed content warnings or authentication failures

**Development Settings** (NOT for production):
```csharp
options.RequireHttpsMetadata = false;
options.TokenValidationParameters.ValidateIssuer = false;
```

**Production Settings**:
```csharp
options.RequireHttpsMetadata = true;
options.TokenValidationParameters.ValidateIssuer = true;
```

### Debugging Tips

#### Enable Detailed Logging

In `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore.Authentication": "Debug",
      "Microsoft.AspNetCore.Authorization": "Debug"
    }
  }
}
```

#### Check Authentication State

Add this to any component:
```razor
@inject IAuthenticationService AuthService

<button @onclick="CheckAuth">Check Auth State</button>

@code {
    private async Task CheckAuth()
    {
        var state = await AuthService.GetAuthenticationStateAsync();
        Console.WriteLine($"Authenticated: {state.IsAuthenticated}");
        Console.WriteLine($"Name: {state.Name}");
        
        if (state.Principal != null)
        {
            foreach (var claim in state.Principal.Claims)
            {
                Console.WriteLine($"{claim.Type}: {claim.Value}");
            }
        }
    }
}
```

#### Browser Console (WASM)

Check browser console for authentication provider logs:
```
[WasmAuthStateProvider] GetAuthenticationStateAsync called
[WasmAuthStateProvider] Parsed claims: ...
```

---

## Security Considerations

### Production Checklist

- [ ] Use HTTPS everywhere (`RequireHttpsMetadata = true`)
- [ ] Validate issuer (`ValidateIssuer = true`)
- [ ] Use secure client secret (not hardcoded)
- [ ] Configure proper CORS policies
- [ ] Set up appropriate token lifetimes in Keycloak
- [ ] Implement refresh token rotation
- [ ] Use secure cookie settings:
  ```csharp
  options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
  options.Cookie.SameSite = SameSiteMode.Strict;
  options.Cookie.HttpOnly = true;
  ```
- [ ] Validate redirect URIs strictly in Keycloak
- [ ] Enable Keycloak security features (brute force detection, etc.)
- [ ] Implement proper logout across all user sessions
- [ ] Set up monitoring and logging for authentication events

### Sensitive Data

Never commit to source control:
- Client secrets
- Connection strings
- Private keys

Use:
- Environment variables
- Azure Key Vault
- User secrets (development)

---

## Additional Resources

### Keycloak Documentation
- [Keycloak Server Administration](https://www.keycloak.org/docs/latest/server_admin/)
- [OpenID Connect Flow](https://www.keycloak.org/docs/latest/securing_apps/#_oidc)

### Microsoft Documentation
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [OpenID Connect Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/oidc)
- [Blazor Security](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/)

### This Application
- Main server configuration: `Sivar.Os/Program.cs`
- Client configuration: `Sivar.Os.Client/Program.cs`
- Authentication controller: `Sivar.Os/Controllers/AuthenticationController.cs`
- Server auth service: `Sivar.Os/Services/ServerAuthenticationService.cs`
- Client auth service: `Sivar.Os.Client/Services/ClientAuthenticationService.cs`
- WASM provider: `Sivar.Os.Client/Auth/WasmAuthenticationStateProvider.cs`

---

## Summary

This application implements a sophisticated authentication system that:

1. **Uses Keycloak** as the identity provider via OpenID Connect
2. **Supports hybrid rendering** with both server and WebAssembly modes
3. **Adapts authentication** based on render mode (HttpContext vs API calls)
4. **Handles logout gracefully** by overriding redirect URIs to avoid Keycloak errors
5. **Cascades authentication state** to all components automatically
6. **Provides consistent interface** (`IAuthenticationService`) across render modes

The key innovation is the **adaptive service pattern** that automatically uses the right implementation based on whether the component is rendering on the server or in WebAssembly, providing a seamless authentication experience regardless of render mode.
