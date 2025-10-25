# 🛡️ Keycloak Integration Guide
**For Blazor Server, Blazor WebAssembly, and .NET API**

---

## 📘 Overview

This guide explains how to configure Keycloak as your **OpenID Connect (OIDC)** identity provider across three layers:

1. **API (Backend)** — Validates JWT access tokens.
2. **Blazor Server (Frontend + Host)** — Handles login, logout, and stores tokens.
3. **Blazor WebAssembly (Client)** — Uses OIDC to log in and attach tokens to API calls.

All of them must use the same **Keycloak Realm**, **Client IDs**, and **Authority URL**.

---

## ⚙️ 1. Keycloak Setup

### ✅ Realm
- Create or use an existing **Realm** (e.g., `sivar`).

### ✅ Clients

| Client | Type | Access Type | Redirect URIs | Scopes |
|---------|------|--------------|----------------|---------|
| `blazor-server` | Web | **Confidential** | `https://localhost:5001/signin-oidc` | `openid`, `profile`, `email` |
| `blazor-wasm` | Public (SPA) | **Public** | `https://localhost:5002/authentication/login-callback` | `openid`, `profile`, `email` |
| `api` | Service | **Confidential** | *n/a* | `openid`, `profile`, `email` |

### ✅ Mappers
In each client, go to **Mappers → Create** and ensure you have these mappings:

| Mapper Type | User Property | Token Claim Name | Add to Token |
|--------------|----------------|------------------|---------------|
| User Property | `firstName` | `given_name` | ✔️ |
| User Property | `lastName` | `family_name` | ✔️ |
| User Property | `email` | `email` | ✔️ |
| User Property | `username` | `preferred_username` | ✔️ |

Optional:  
- **Realm Role Mapper** → Claim name: `roles` (to use `[Authorize(Roles="admin")]` in C#)

---

## 🧱 2. API Configuration (JWT Authentication)

Add the following to your **API** project (`Program.cs`):

```csharp
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://auth.sivar.sv/realms/sivar";
        options.Audience = "api"; // match Keycloak client ID
        options.RequireHttpsMetadata = true;

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "preferred_username",
            RoleClaimType = "roles",
            ValidateIssuer = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

### Example controller:

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(claims);
    }
}
```

---

## 🖥️ 3. Blazor Server Configuration

### Add authentication in `Program.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority = "https://auth.sivar.sv/realms/sivar";
        options.ClientId = "blazor-server";
        options.ClientSecret = "YOUR_CLIENT_SECRET";
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.MapInboundClaims = false;
        options.ClaimActions.Clear();
        options.ClaimActions.MapUniqueJsonKey("sub", "sub");
        options.ClaimActions.MapUniqueJsonKey("email", "email");
        options.ClaimActions.MapUniqueJsonKey("given_name", "given_name");
        options.ClaimActions.MapUniqueJsonKey("family_name", "family_name");
        options.ClaimActions.MapUniqueJsonKey("preferred_username", "preferred_username");
        options.ClaimActions.MapUniqueJsonKey("name", "name");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };
    });

builder.Services.AddAuthorization();
```

### Protect routes in `App.razor`

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(Program).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        </Found>
        <NotFound>
            <LayoutView Layout="@typeof(MainLayout)">
                <p>Sorry, there’s nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

### Add secure pages

```razor
@page "/profile"
@attribute [Authorize]

<h3>Profile</h3>
<pre>@System.Text.Json.JsonSerializer.Serialize(context.User.Claims.Select(c => new { c.Type, c.Value }), new JsonSerializerOptions { WriteIndented = true })</pre>
```

---

## 🌐 4. Blazor WebAssembly (WASM)

Add OIDC authentication in `Program.cs` (client project):

```csharp
builder.Services.AddOidcAuthentication(options =>
{
    var provider = options.ProviderOptions;
    provider.Authority = "https://auth.sivar.sv/realms/sivar";
    provider.ClientId = "blazor-wasm";
    provider.ResponseType = "code";

    provider.DefaultScopes.Clear();
    provider.DefaultScopes.Add("openid");
    provider.DefaultScopes.Add("profile");
    provider.DefaultScopes.Add("email");

    options.UserOptions.NameClaim = "preferred_username";
    options.UserOptions.RoleClaim = "roles";
});
```

### Attach token to HTTP requests

```csharp
builder.Services.AddHttpClient("API", client =>
    client.BaseAddress = new Uri("https://localhost:5003/"))
    .AddHttpMessageHandler(sp =>
        sp.GetRequiredService<AuthorizationMessageHandler>()
          .ConfigureHandler(
              authorizedUrls: new[] { "https://localhost:5003" },
              scopes: new[] { "openid", "profile", "email" })
          );

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));
```

---

## 🧩 5. Shared Token Flow

| Action | Blazor Server | Blazor WASM | API |
|--------|----------------|-------------|------|
| Login | Redirects to Keycloak | Redirects to Keycloak | n/a |
| Token Storage | Cookie + saved tokens | Browser session | n/a |
| Token Validation | Middleware | Access token in API calls | JWT middleware |

---

## 🔒 6. Common Gotchas

| Problem | Cause | Fix |
|----------|--------|-----|
| Claims show WS-Fed URIs | Default .NET claim mapping | Set `JwtSecurityTokenHandler.DefaultMapInboundClaims = false` |
| Missing `email` / `given_name` | Keycloak client missing mappers | Add user property mappers |
| Roles not working | Role claim missing | Add “Realm Role” mapper and set `RoleClaimType = "roles"` |
| Old claim data persists | Cookies cache claims | Log out or clear browser cookies |
| 401 calling API from WASM | Missing bearer token | Configure `AuthorizationMessageHandler` |

---

## 🧪 7. Test Steps

1. Run Keycloak (`https://auth.sivar.sv` or local port).
2. Run API → visit `/api/profile/me` (requires token).
3. Run Blazor Server → sign in → view claims in `/profile`.
4. Run Blazor WASM → sign in → call API → success!

---

## 🧠 Optional Enhancements

- Enable **PKCE** for WASM.
- Add **refresh token** logic.
- Use `[Authorize(Roles="admin")]`.
- Centralize configs in `appsettings.json`.

---

## ✅ Summary

| Layer | Key Configuration |
|--------|--------------------|
| API | `AddJwtBearer` with `Authority`, `Audience`, and `MapInboundClaims=false` |
| Blazor Server | `AddOpenIdConnect` + cookie + explicit claim mapping |
| Blazor WASM | `AddOidcAuthentication` + `AuthorizationMessageHandler` |
| Keycloak | Proper mappers for `email`, `given_name`, `family_name`, and `roles` |
