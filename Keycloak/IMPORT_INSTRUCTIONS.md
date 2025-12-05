# Keycloak Production Setup for Sivar.Os

## Server Details
- **Keycloak URL**: https://auth.sivar.sv/
- **Realm Name**: sivar-os
- **Application URL**: http://os.sivar.lat/

---

## Step 1: Import the Realm

### Option A: Via Admin Console (Recommended)

1. **Login to Keycloak Admin Console**
   - Go to: https://auth.sivar.sv/admin/
   - Login with your admin credentials

2. **Create New Realm**
   - Click the dropdown next to "master" realm (top-left)
   - Click "Create realm"
   - Click "Browse..." and select the file: `realm-export.json`
   - Click "Create"

### Option B: Via Keycloak CLI (kcadm)

```bash
# Login to Keycloak
./kcadm.sh config credentials --server https://auth.sivar.sv/ --realm master --user admin

# Import realm
./kcadm.sh create realms -f realm-export.json
```

---

## Step 2: Generate New Client Secret (CRITICAL!)

After importing, you **MUST** generate a new secret for the server client:

1. Go to: **Clients** → **sivaros-server**
2. Click the **Credentials** tab
3. Click **Regenerate** next to "Client secret"
4. **Copy the new secret** - you'll need it for `appsettings.Production.json`

---

## Step 3: Update Application Configuration

### Server: `appsettings.Production.json`

```json
{
  "Keycloak": {
    "Authority": "https://auth.sivar.sv/realms/sivar-os",
    "MetadataAddress": "https://auth.sivar.sv/realms/sivar-os/.well-known/openid-configuration",
    "ClientIdServer": "sivaros-server",
    "ClientSecret": "YOUR_NEW_CLIENT_SECRET_FROM_STEP_2"
  }
}
```

### Client (WASM): `wwwroot/appsettings.Production.json`

```json
{
  "Keycloak": {
    "Authority": "https://auth.sivar.sv/realms/sivar-os",
    "ClientId": "sivaros-client"
  }
}
```

---

## Step 4: Create Admin User

1. Go to: **Users** → **Add user**
2. Fill in:
   - Username: `admin` (or your preferred admin username)
   - Email: `admin@sivar.sv`
   - First Name: Your name
   - Email Verified: ON
3. Click **Create**
4. Go to **Credentials** tab
5. Click **Set password**
6. Enter password and set "Temporary" to OFF
7. Go to **Role mapping** tab
8. Click **Assign role**
9. Select **admin** role

---

## Step 5: Verify Redirect URIs

The realm includes these redirect URIs. Update if your domain is different:

### sivaros-client (Public - WebAssembly)
| Setting | Values |
|---------|--------|
| Valid Redirect URIs | `http://os.sivar.lat/authentication/login-callback` |
| Web Origins | `http://os.sivar.lat` |

### sivaros-server (Confidential - Server)
| Setting | Values |
|---------|--------|
| Valid Redirect URIs | `http://os.sivar.lat/signin-oidc` |
| Web Origins | `http://os.sivar.lat` |

---

## Step 6: Configure SMTP (Optional but Recommended)

For password reset and email verification:

1. Go to: **Realm settings** → **Email**
2. Configure your SMTP server:
   ```
   Host: smtp.your-provider.com
   Port: 587
   From: noreply@sivar.sv
   Enable SSL: ON
   Enable StartTLS: ON
   Authentication: ON
   Username: your-smtp-username
   Password: your-smtp-password
   ```

---

## Realm Features Included

| Feature | Status | Description |
|---------|--------|-------------|
| ✅ User Registration | Enabled | Users can self-register |
| ✅ Email Login | Enabled | Login with email instead of username |
| ✅ Password Reset | Enabled | "Forgot password" functionality |
| ✅ Remember Me | Enabled | "Keep me logged in" option |
| ✅ Brute Force Protection | Enabled | 5 failed attempts = lockout |
| ✅ PKCE | Required | For WebAssembly client security |
| ✅ SSL | Required | External connections require HTTPS |
| ✅ Localization | Enabled | English and Spanish |

---

## Roles Included

| Role | Description |
|------|-------------|
| `user` | Default role for all users |
| `admin` | Full administrative access |
| `moderator` | Content moderation access |

---

## Security Settings

| Setting | Value |
|---------|-------|
| Password Policy | Min 8 chars, 1 uppercase, 1 lowercase, 1 digit |
| Access Token Lifespan | 5 minutes |
| SSO Session Idle | 30 minutes |
| SSO Session Max | 10 hours |
| Brute Force Lockout | 5 failures |
| Lockout Duration | 15 minutes |

---

## Troubleshooting

### "Invalid redirect_uri" Error
- Verify the redirect URI in Keycloak matches EXACTLY what your app sends
- Check for trailing slashes
- Ensure HTTPS is used

### "CORS" Errors
- Add your domain to Web Origins in both clients
- Include the protocol: `https://your-domain.com`

### Token Not Working
- Verify the client secret matches in `appsettings.Production.json`
- Check the Authority URL is correct
- Ensure the realm name matches: `sivar-os`

---

## Quick Test URLs

After setup, verify these URLs work:

1. **OpenID Configuration**:
   ```
   https://auth.sivar.sv/realms/sivar-os/.well-known/openid-configuration
   ```

2. **Keycloak Account Page**:
   ```
   https://auth.sivar.sv/realms/sivar-os/account/
   ```

3. **Authorization Endpoint**:
   ```
   https://auth.sivar.sv/realms/sivar-os/protocol/openid-connect/auth
   ```
