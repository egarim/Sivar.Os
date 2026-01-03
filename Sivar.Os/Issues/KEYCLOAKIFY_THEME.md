# 🎨 Keycloakify Theme Customization

**Issue:** Login/Signup pages should match Sivar.Os application branding  
**Priority:** Medium  
**Status:** Planning  
**Created:** 2026-01-03

---

## 📋 Overview

Keycloak's default login/registration pages have a generic look that doesn't match our Sivar.Os application branding. We'll use **Keycloakify** to create a custom React-based theme that mirrors our app's visual identity.

### Current State
- Keycloak realm: `sivar-os`
- Auth URL: `https://auth.sivar.sv/realms/sivar-os`
- Current theme: `keycloak` (default)
- Server access: SSH root access available

### Goal
Create a branded Keycloak theme with:
- Sivar.Os color scheme (primary: `#1da1f2`)
- Consistent typography and spacing
- Custom logo and branding
- Spanish/English localization support
- Mobile-responsive design

---

## 🎨 Design System Reference

### Color Variables (from wireframe-theme.css)
```css
--wire-bg: #f5f7fa;
--wire-surface: #ffffff;
--wire-border: #e1e8ed;
--wire-text-primary: #14171a;
--wire-text-secondary: #657786;
--wire-primary: #1da1f2;
--wire-hover: #f0f4f8;
```

### Component Styles
- Border radius: `12px` (cards), `8px` (inputs), `20px` (buttons)
- Font weights: 600-700 for headings, 400 for body
- Button style: Rounded pill buttons with primary color

---

## 📦 What is Keycloakify?

Keycloakify is a tool that allows you to create Keycloak themes using React/TypeScript. Benefits:

1. **Modern Development**: Use React, TypeScript, and modern tooling
2. **Component Reuse**: Share components with your main app
3. **Hot Reload**: Development server for rapid iteration
4. **Type Safety**: Full TypeScript support for Keycloak variables
5. **Easy Deployment**: Builds to a standard Keycloak theme JAR

---

## 🚀 Implementation Phases

### Phase 1: Project Setup
- [ ] Create Keycloakify project in repository
- [ ] Configure build tooling (Vite + React)
- [ ] Set up TypeScript configuration
- [ ] Install dependencies

### Phase 2: Theme Development
- [ ] Create login page component
- [ ] Create registration page component
- [ ] Create forgot password page component
- [ ] Apply Sivar.Os color scheme and typography
- [ ] Add logo and branding assets
- [ ] Implement responsive design

### Phase 3: Localization
- [ ] Add English translations
- [ ] Add Spanish translations
- [ ] Test language switching

### Phase 4: Build & Deploy
- [ ] Build theme JAR file
- [ ] SSH to Keycloak server
- [ ] Deploy theme to `/opt/keycloak/themes/`
- [ ] Configure realm to use new theme
- [ ] Test all authentication flows

### Phase 5: Testing & Polish
- [ ] Test login flow
- [ ] Test registration flow
- [ ] Test password reset flow
- [ ] Test on mobile devices
- [ ] Fix any visual issues

---

## 🛠️ Technical Implementation

### Step 1: Create Keycloakify Project

```bash
# In the Sivar.Os repository root, create the KeycloakifyTheme folder
cd /Users/joche/Documents/GitHub/Sivar.Os
mkdir KeycloakifyTheme
cd KeycloakifyTheme

# Initialize with Keycloakify starter
npx degit keycloakify/keycloakify-starter .
npm install
```

### Step 2: Project Structure

```
Sivar.Os/                        # Repository root
├── KeycloakifyTheme/            # Keycloak theme (isolated from app)
│   ├── src/
│   │   ├── login/
│   │   │   ├── KcPage.tsx           # Main login page router
│   │   │   ├── pages/
│   │   │   │   ├── Login.tsx        # Login form
│   │   │   │   ├── Register.tsx     # Registration form
│   │   │   │   └── LoginResetPassword.tsx
│   │   │   └── Theme.tsx            # Theme wrapper
│   │   ├── assets/
│   │   │   └── sivar-logo.svg       # App logo
│   │   └── styles/
│   │       └── sivar-theme.css      # Custom styles
│   ├── package.json
│   ├── vite.config.ts
│   ├── keycloak-theme.json
│   └── README.md                    # Theme-specific docs
├── Sivar.Os/                    # Main Blazor Server app
├── Sivar.Os.Client/             # Blazor WebAssembly client
├── Sivar.Os.Shared/             # Shared library
└── ...
```

### Step 3: Configure keycloak-theme.json

```json
{
  "keycloakVersionTargets": {
    "22-and-above": true
  },
  "themeName": "sivar-os",
  "themeType": ["login"],
  "environmentVariables": {
    "SIVAR_PRIMARY_COLOR": "#1da1f2",
    "SIVAR_APP_NAME": "Sivar.Os"
  }
}
```

---

### Step 4: Custom Styles (sivar-theme.css)

```css
/* Sivar.Os Keycloak Theme */
:root {
  --sivar-bg: #f5f7fa;
  --sivar-surface: #ffffff;
  --sivar-border: #e1e8ed;
  --sivar-text-primary: #14171a;
  --sivar-text-secondary: #657786;
  --sivar-primary: #1da1f2;
  --sivar-primary-hover: #1a8cd8;
  --sivar-hover: #f0f4f8;
}

body {
  background: var(--sivar-bg);
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

.login-pf-page {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
}

/* Card Container */
.card-pf {
  background: var(--sivar-surface);
  border: 1px solid var(--sivar-border);
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  max-width: 400px;
  width: 100%;
  padding: 32px;
}

/* Logo */
.kc-logo-text span {
  color: var(--sivar-primary);
  font-size: 24px;
  font-weight: 700;
}

/* Form Inputs */
.pf-c-form-control {
  border: 1px solid var(--sivar-border);
  border-radius: 8px;
  padding: 12px 16px;
  font-size: 14px;
  transition: border-color 0.2s;
}

.pf-c-form-control:focus {
  border-color: var(--sivar-primary);
  outline: none;
  box-shadow: 0 0 0 3px rgba(29, 161, 242, 0.2);
}

/* Primary Button */
.pf-c-button.pf-m-primary {
  background: var(--sivar-primary);
  border: none;
  border-radius: 20px;
  padding: 12px 24px;
  font-weight: 600;
  font-size: 14px;
  cursor: pointer;
  transition: background 0.2s;
}

.pf-c-button.pf-m-primary:hover {
  background: var(--sivar-primary-hover);
}

/* Links */
a {
  color: var(--sivar-primary);
  text-decoration: none;
}

a:hover {
  text-decoration: underline;
}

/* Responsive */
@media (max-width: 480px) {
  .card-pf {
    margin: 16px;
    padding: 24px;
  }
}
```

---

## 🚢 Deployment Instructions

### Build the Theme

```bash
cd /Users/joche/Documents/GitHub/Sivar.Os/KeycloakifyTheme
npm run build-keycloak-theme
```

This creates a JAR file in `dist_keycloak/`.

### Deploy to Keycloak Server

```bash
# From the KeycloakifyTheme folder, copy the JAR to the server
cd /Users/joche/Documents/GitHub/Sivar.Os/KeycloakifyTheme
scp dist_keycloak/keycloak-theme-sivar-os.jar root@auth.sivar.sv:/opt/keycloak/providers/

# SSH into the Keycloak server and restart
ssh root@auth.sivar.sv
systemctl restart keycloak
# OR if using Docker:
docker restart keycloak
```

### Configure Realm to Use Theme

> **Note:** Themes are deployed server-wide but configured **per realm**. Each realm can use a different theme. This theme will only apply to the `sivar-os` realm after configuration.

1. Log into Keycloak Admin Console (`https://auth.sivar.sv/admin`)
2. Select the **sivar-os** realm from the dropdown
3. Navigate to **Realm Settings** → **Themes**
4. Set **Login theme** to `sivar-os`
5. Optionally set **Account theme** to `sivar-os` (for user account pages)
6. Click **Save**

Other realms on the same server will remain unaffected.

### Alternative: Direct Theme Folder Deployment

```bash
# Extract JAR to themes folder
cd /opt/keycloak/themes
mkdir sivar-os
cd sivar-os
jar -xf /opt/keycloak/providers/keycloak-theme-sivar-os.jar
```

---

## 📍 Server Information

```
Keycloak Server: auth.sivar.sv
SSH Access: root@auth.sivar.sv
Keycloak Version: 22+ (verify with: /opt/keycloak/bin/kc.sh --version)
Theme Location: /opt/keycloak/themes/ OR /opt/keycloak/providers/
```

---

## 🧪 Testing Checklist

### Login Page
- [ ] Logo displays correctly
- [ ] Input fields match Sivar.Os styling
- [ ] Primary button uses correct color
- [ ] "Forgot password" link works
- [ ] "Register" link works
- [ ] Error messages display properly
- [ ] Remember me checkbox works

### Registration Page
- [ ] All form fields styled correctly
- [ ] Password requirements displayed
- [ ] Submit button works
- [ ] "Back to login" link works
- [ ] Validation errors display properly

### Password Reset
- [ ] Email input styled correctly
- [ ] Submit button works
- [ ] Success/error messages display

### General
- [ ] Mobile responsive on iPhone/Android
- [ ] Spanish locale works
- [ ] English locale works
- [ ] Dark mode consideration (future)

---

## 📚 Resources

- [Keycloakify Documentation](https://keycloakify.dev/)
- [Keycloakify GitHub](https://github.com/keycloakify/keycloakify)
- [Keycloakify Starter Template](https://github.com/keycloakify/keycloakify-starter)
- [Keycloak Theming Guide](https://www.keycloak.org/docs/latest/server_development/#_themes)

---

## 📝 Notes

- The theme lives in `/KeycloakifyTheme/` folder at repository root (isolated from .NET projects)
- Run `npm install` inside `KeycloakifyTheme/` to install dependencies
- The folder has its own `package.json`, `node_modules`, and build system
- Add `KeycloakifyTheme/node_modules/` to `.gitignore` if not already ignored
- Consider using a CI/CD pipeline to auto-deploy theme on changes
- Test with both Blazor Server and WebAssembly authentication flows
- Ensure the theme works with social login buttons if added in future
