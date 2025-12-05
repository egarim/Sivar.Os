# Keycloak Setup for Blazor Interactive Auto (Server + WASM)

This guide configures Keycloak at **http://localhost:8080/** with a realm named **blazor-interactive** for a Blazor Web App using **RenderMode=Auto**.

## 1) Create Realm
- Name: **blazor-interactive**

## 2) Create Clients
You'll create **two** clients:

### A) Public client for WebAssembly
- **Client ID:** `myhybridapp-client`
- **Client type:** Public
- **Standard flow:** Enabled (Authorization Code with PKCE)
- **Implicit flow:** Disabled
- **Valid Redirect URIs:**
  - `https://localhost:5001/authentication/login-callback`
  - `http://localhost:5000/authentication/login-callback`
- **Web origins:**
  - `https://localhost:5001`
  - `http://localhost:5000`
- **Logout redirect URIs:**
  - `https://localhost:5001/authentication/logout-callback`
  - `http://localhost:5000/authentication/logout-callback`

### B) Confidential client for Server (OIDC)
- **Client ID:** `myhybridapp-server`
- **Client type:** Confidential
- **Standard flow:** Enabled (Authorization Code)
- **Valid Redirect URIs:**
  - `https://localhost:5001/signin-oidc`
  - `http://localhost:5000/signin-oidc`
- **Credentials:** Generate a **Client Secret** and copy it.

> 🔐 Development: For HTTP (non-HTTPS) on localhost, allow insecure transport in Keycloak (Realm settings → Login → allow to use HTTP) or proxy behind HTTPS in Kestrel/Reverse proxy. In production, always use HTTPS.

## 3) Add User(s)
- Create a test user and set a password (or enable passwordless if you want).

## 4) Configure Scopes & Claims (optional)
- Default scopes: `openid`, `profile`, `email`.
- If you protect APIs with roles, add realm roles and map them into the Access Token:
  - Realm Roles → `roles` claim (token mapper → "User Realm Role")
- For APIs expecting an **audience**, add a client scope with audience mapper.

## 5) Wire Blazor Projects

### Server (`appsettings.json`)
```
"Keycloak": {
  "Authority": "http://localhost:8080/realms/blazor-interactive",
  "MetadataAddress": "http://localhost:8080/realms/blazor-interactive/.well-known/openid-configuration",
  "ClientIdServer": "myhybridapp-server",
  "ClientSecret": "<YOUR_SECRET>"
}
```

### Server `Program.cs` (already set)
- Cookie + OIDC using above settings.
- `AddAuthenticationStateSerialization()` so prerender auth state is passed to the client during hydration.
- `UseAuthentication()` and `UseAuthorization()`.

### Client `Program.cs`
- `AddOidcAuthentication` pointing to the realm:
  - Authority: `http://localhost:8080/realms/blazor-interactive`
  - ClientId: `myhybridapp-client`
  - ResponseType: `code`
  - DefaultScopes: `openid`, `profile`, `email`
- `AddAuthenticationStateDeserialization()` so the client picks up the server auth state in Auto mode.

## 6) Test Flow
1. Run the **Server** project.
2. Navigate to `/weather`.
3. You should be redirected to Keycloak for login.
4. After login, you return to the app and the protected page displays data.
5. Open browser dev tools to confirm tokens/cookies are present.
6. Refresh to see that hydration preserves the authenticated state.

## 7) Common Pitfalls
- **Missing HTTPS**: OIDC prefer HTTPS. For localhost, add both HTTP and HTTPS redirects in Keycloak.
- **Wrong Redirect URIs**: Ensure they match exactly (including trailing slashes).
- **CORS**: Add correct Web Origins for the WASM client.
- **Token audience**: If calling separate APIs, set audience/mapper so API validates token.
- **Losing auth after hydration**: Ensure `AddAuthenticationStateSerialization/Deserialization` is registered as in this project.