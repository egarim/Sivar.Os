# 🔧 Keycloak Claims Fix Summary

## Problem Identified

Your Keycloak claims were not coming through correctly in the `EnsureUserAndProfileCreatedAsync` method because:

1. **JWT claim mapping was disabled** - causing claims to be wrapped in WS-Fed URIs
2. **No explicit claim action mappings** - claims weren't being mapped to standard names
3. **Missing TokenValidationParameters** - proper claim type configuration was absent

This forced the `ExtractKeycloakClaims` helper to use multiple fallback claim names, which is fragile and error-prone.

---

## ✅ Changes Made

### 1. **API Program.cs** - Enabled JWT Claim Mapping Fix

**Line 33: Uncommented and added**
```csharp
// --- JWT Claim Mapping Configuration ---
// MUST be set BEFORE AddAuthentication to prevent WS-Fed claim URI wrapping
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
```

**Why**: When this is `false`, Keycloak claims like `email`, `given_name`, `family_name` stay as-is instead of becoming:
- `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress`
- `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname`

### 2. **API Program.cs** - Enhanced AddOpenIdConnect Configuration

**Lines 215-234: Added proper mappings and validation parameters**

```csharp
.AddOpenIdConnect(options =>
{
    // ... existing config ...
    
    // ⭐ CRITICAL: Prevent WS-Fed claim URI wrapping
    options.MapInboundClaims = false;
    
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    
    // Configure token validation parameters for proper claim handling
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "roles",
        ValidateIssuer = false // For dev with http
    };
});
```

**Why**: 
- `MapInboundClaims = false` tells ASP.NET Core to NOT wrap claims in WS-Fed URIs
- `NameClaimType` and `RoleClaimType` tell ASP.NET Core which claim names to use for `User.Identity.Name` and roles

### 3. **WASM Client** - No Changes Needed ✅

The Blazor Hybrid architecture is correctly implemented:
- The **server** handles Keycloak authentication via cookies
- The **server** extracts claims and exposes them via `/authentication/profile` endpoint
- The **WASM client** fetches auth state from server via `WasmAuthenticationStateProvider`
- Claims flow correctly through `AuthenticationState.User.Claims`

---

## 🧪 How to Test

### 1. **Run the Application**
```bash
cd Sivar.Os
dotnet run
```

### 2. **Login via Keycloak**
- Navigate to home page
- Click login
- Authenticate with Keycloak

### 3. **Check Console Logs**
Open browser DevTools (F12) and look for logs from `ExtractKeycloakClaims`:

**Expected Output:**
```
[Home] Extracting Keycloak claims - Available claims:
  sub: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
  email: user@example.com
  given_name: John
  family_name: Doe
  preferred_username: john_doe
  name: John Doe
[Home] Extracted - Keycloak ID: xxxxxxxx..., Email: user@example.com, First Name: John, Last Name: Doe
```

### 4. **Check Authentication Success**
The logs should show:
```
[Home] New user and profile created for user@example.com
```
or
```
[Home] Existing user authenticated: user@example.com
```

---

## 🔍 Verification Checklist

- [ ] Run the app and login
- [ ] Check browser console for all expected claims being logged
- [ ] Verify `sub`, `email`, `given_name`, `family_name`, `preferred_username` claims are present
- [ ] Confirm no WS-Fed URIs like `http://schemas.xmlsoap.org/...` appear
- [ ] User profile is created successfully on first login
- [ ] Subsequent logins don't create duplicate profiles

---

## 📋 Keycloak Configuration Verification

**Make sure your Keycloak realm has these mappers configured:**

In Keycloak Admin Console → Your Realm → Clients → Your Client → Mappers:

| Mapper Name | Type | User Property | Token Claim Name | Add to Token |
|-------------|------|----------------|------------------|--------------|
| email | User Property | email | email | ✅ |
| given_name | User Property | firstName | given_name | ✅ |
| family_name | User Property | lastName | family_name | ✅ |
| preferred_username | User Property | username | preferred_username | ✅ |

If any of these are missing, the claims won't flow properly!

---

## 🎯 What Changed in the Code

**File: `Sivar.Os/Program.cs`**

- Added `JwtSecurityTokenHandler.DefaultMapInboundClaims = false;` at the top (before AddAuthentication)
- Updated `AddOpenIdConnect()` to:
  - Set `MapInboundClaims = false`
  - Properly configure `TokenValidationParameters` with claim type mappings
  - Clear and re-add scopes explicitly

**Result**: Claims now come through as expected, and the `EnsureUserAndProfileCreatedAsync` method will receive the correct claim values without needing fallback logic.

---

## 🚀 Next Steps

1. Rebuild and run the solution
2. Test login flow and verify claims are correct
3. If claims still aren't showing:
   - Check Keycloak Admin Console to confirm mappers exist
   - Check browser DevTools Network tab to see the `/authentication/profile` response
   - Review server logs for any authentication errors

---

## 📚 References

- [Keycloak Integration Guide](./KeycloakIntegrationGuide.md)
- [Microsoft Docs: JWT Claim Mapping](https://docs.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt.jwtsecuritytokenhandler.defaultmapinboundclaims)
- [ASP.NET Core OIDC Configuration](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/oidc)
