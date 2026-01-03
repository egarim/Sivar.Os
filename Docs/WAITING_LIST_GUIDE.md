# Waiting List System - User & Admin Guide

> **Feature:** Gradual user onboarding with phone verification  
> **Status:** Implemented  
> **Last Updated:** January 2026

---

## 📋 Overview

The Waiting List system controls access to Sivar.Os through a gradual onboarding process:

1. **New users register** via Keycloak (email + password)
2. **Verify phone number** via SMS or WhatsApp (country-dependent)
3. **Join the waiting queue** with optional referral code
4. **Admin approves users** through XAF dashboard
5. **Approved users** get full app access

This ensures user authenticity and allows controlled platform growth.

---

## 🔐 How It Works

### User Flow

```
┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│  1. Register     │────▶│  2. Verify Phone │────▶│  3. Wait in Queue│
│  (Keycloak)      │     │  (SMS/WhatsApp)  │     │  (Position #X)   │
└──────────────────┘     └──────────────────┘     └──────────────────┘
                                                           │
┌──────────────────┐     ┌──────────────────┐              │
│  5. Full Access  │◀────│  4. Admin Approves│◀────────────┘
│  (App works!)    │     │  (XAF Dashboard) │
└──────────────────┘     └──────────────────┘
```

### Verification Channels

The system automatically selects the best verification channel (SMS or WhatsApp) based on the user's selected country. This is determined by the **ISO country code**, not the phone dial code.

#### WhatsApp Countries (Latin America)

WhatsApp is preferred in Latin America due to higher adoption rates and lower costs.

| Country | ISO Code | Dial Code | Channel |
|---------|----------|-----------|---------|
| El Salvador | SV | +503 | WhatsApp |
| Guatemala | GT | +502 | WhatsApp |
| Honduras | HN | +504 | WhatsApp |
| Nicaragua | NI | +505 | WhatsApp |
| Costa Rica | CR | +506 | WhatsApp |
| Panama | PA | +507 | WhatsApp |
| Belize | BZ | +501 | WhatsApp |
| Mexico | MX | +52 | WhatsApp |
| Colombia | CO | +57 | WhatsApp |
| Peru | PE | +51 | WhatsApp |
| Argentina | AR | +54 | WhatsApp |
| Chile | CL | +56 | WhatsApp |
| Ecuador | EC | +593 | WhatsApp |
| Venezuela | VE | +58 | WhatsApp |
| Bolivia | BO | +591 | WhatsApp |
| Paraguay | PY | +595 | WhatsApp |
| Uruguay | UY | +598 | WhatsApp |
| Brazil | BR | +55 | WhatsApp |

#### SMS Countries (North America & Europe)

SMS is used for countries with reliable SMS delivery infrastructure.

| Country | ISO Code | Dial Code | Channel |
|---------|----------|-----------|---------|
| United States | US | +1 | SMS |
| Canada | CA | +1 | SMS |
| Spain | ES | +34 | SMS |
| United Kingdom | UK/GB | +44 | SMS |
| Germany | DE | +49 | SMS |
| France | FR | +33 | SMS |
| Italy | IT | +39 | SMS |

> 💡 **Note:** The channel is determined by the country selected in the dropdown, not by parsing the phone number. Users must select their correct country.

#### How Channel Selection Works

```csharp
// In TwilioOptions.cs
public string[] WhatsAppCountries { get; set; } = new[]
{
    // Central America
    "SV", "GT", "HN", "NI", "CR", "PA", "BZ",
    // Mexico
    "MX",
    // South America
    "CO", "PE", "AR", "CL", "EC", "VE", "BO", "PY", "UY", "BR"
};

public bool ShouldUseWhatsApp(string countryCode)
{
    return WhatsAppCountries.Contains(countryCode.ToUpperInvariant());
}
```

#### Adding New Countries

To add support for a new country:

1. **Update `appsettings.json`** - Add the ISO code to `WhatsAppCountries` array (if WhatsApp preferred)
2. **Update UI component** - Add country to `PhoneVerificationForm.razor` Countries list:
   ```csharp
   new("XX", "Country Name", "+YYY"),
   ```
3. **Test delivery** - Ensure Twilio can deliver to the new country

---

## 👤 User Guide

### Step 1: Register
1. Go to the Sivar.Os registration page
2. Enter your email and create a password
3. Complete Keycloak registration

### Step 2: Verify Your Phone
After registration, you'll be redirected to phone verification:

1. **Select your country** from the dropdown
2. **Enter your phone number** (without country code)
3. **Enter referral code** (optional) - moves you up in the queue!
4. Click **"Send Verification Code"**

You'll receive a 6-digit code via:
- **WhatsApp** (for Latin American countries)
- **SMS** (for US, Canada, Europe)

5. Enter the code and click **"Verify"**

### Step 3: Wait for Approval
Once verified, you'll see:
- Your **queue position** (e.g., #42 of 150)
- Your **referral code** to share with friends
- How many friends you've referred

> 💡 **Tip:** Share your referral code! Each friend who uses it moves you up in the queue.

### Step 4: Get Approved
When an admin approves you:
1. Your status changes to "Approved"
2. You get full access to the app
3. You can start using all features

---

## 👨‍💼 Admin Guide (XAF Dashboard)

### Accessing the Waiting List

1. Log in to the XAF Admin Dashboard (https://localhost:5001)
2. Navigate to **"Waiting List"** in the left menu
3. You'll see two entities:
   - **Waiting List Entries** - User signups and queue positions
   - **Phone Verifications** - OTP verification attempts

### Managing Users

#### View Queue Statistics
Click the **"Show Stats"** button to see:
- Total signups
- Pending verification count
- Waiting for approval count
- Approved (today/this week/total)
- Rejected count
- Signups by country

#### Approve Users
1. **Individual approval:** Select user(s) → Click **"Approve"**
2. **Batch approval:** Click **"Approve Next..."** → Enter count → Click "Approve"

#### Reject Users
1. Select user(s) in the list
2. Click **"Reject"**
3. Users are removed from the queue

### Filtering & Sorting

Use the grid filters to find users:
- **Status:** PendingVerification, Waiting, Approved, Rejected
- **Country Code:** SV, US, GT, etc.
- **Position:** Sort by queue position
- **JoinedAt:** Filter by signup date

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "Twilio": {
    "AccountSid": "your-account-sid",
    "AuthToken": "your-auth-token",
    "VerifyServiceSid": "your-verify-service-sid",
    "Enabled": true,
    "WhatsAppCountries": ["SV", "GT", "HN", "NI", "CR", "PA", "CO", "MX", "AR", "PE", "CL"]
  },
  "KeycloakAdmin": {
    "BaseUrl": "https://auth.sivar.sv",
    "Realm": "sivar-os",
    "ClientId": "sivaros-server",
    "ClientSecret": "your-client-secret",
    "Enabled": true
  }
}
```

---

## 📱 Twilio Verify Setup (Step-by-Step)

### Step 1: Create Twilio Account

1. Go to [https://www.twilio.com/try-twilio](https://www.twilio.com/try-twilio)
2. Sign up with email and verify your phone
3. Complete the onboarding wizard

### Step 2: Get Account Credentials

1. Go to **Console Dashboard** (https://console.twilio.com/)
2. Find your credentials in the "Account Info" panel:
   - **Account SID**: `ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`
   - **Auth Token**: Click "Show" to reveal

### Step 3: Create Verify Service

1. In the Twilio Console, go to **Explore Products** → **Verify**
2. Click **"Create a Verify Service"**
3. Configure the service:
   - **Friendly Name**: `Sivar.Os Verification`
   - **Code Length**: `6` (default)
   - **Code TTL**: `300` seconds (5 minutes)
4. Click **Create**
5. Copy the **Service SID**: `VAxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

### Step 4: Enable WhatsApp Channel

1. In your Verify Service, go to **Channels** tab
2. Click **WhatsApp**
3. Follow the WhatsApp Business setup:
   - Connect your WhatsApp Business Account
   - Or use Twilio's pre-approved WhatsApp Sender for testing
4. Enable the channel

> ⚠️ **Note**: WhatsApp requires Meta Business verification for production. For development, Twilio provides a sandbox.

### Step 5: Configure SMS Channel

1. SMS is enabled by default
2. For production, you may need to:
   - Register an A2P 10DLC campaign (US)
   - Get a Toll-Free number verified
   - Or use a Short Code

### Step 6: Update appsettings.json

```json
{
  "Twilio": {
    "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "AuthToken": "your-auth-token-here",
    "VerifyServiceSid": "VAxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "Enabled": true,
    "WhatsAppCountries": [
      "SV", "GT", "HN", "NI", "CR", "PA", "CO", "MX", "AR", "PE", "CL"
    ]
  }
}
```

### Step 7: Test in Sandbox Mode

For development without real SMS/WhatsApp:
```json
{
  "Twilio": {
    "Enabled": false
  }
}
```

This enables mock mode where:
- Any OTP request succeeds immediately
- Any 6-digit code is accepted
- No actual messages are sent

### Twilio Pricing

| Channel | Cost (approximate) |
|---------|-------------------|
| SMS (US) | ~$0.0079/message |
| SMS (El Salvador) | ~$0.0725/message |
| WhatsApp | ~$0.005/message + Meta fees |

> 💡 **Tip**: WhatsApp is often cheaper for Latin America and has better delivery rates.

---

## 🔑 Keycloak Admin API Setup (Step-by-Step)

### Step 1: Configure Service Account

1. Log in to Keycloak Admin Console
2. Go to **Clients** → Select `sivaros-server` (or create it)
3. In **Settings** tab:
   - **Client Authentication**: `ON`
   - **Service Account Roles**: `ON`
   - **Authorization**: `OFF`
4. Click **Save**

### Step 2: Get Client Secret

1. Go to **Credentials** tab
2. Copy the **Client Secret**

### Step 3: Assign Permissions

1. Go to **Service Account Roles** tab
2. Click **Assign Role**
3. Filter by clients: `realm-management`
4. Select these roles:
   - `manage-users` - Required to update user attributes
   - `view-users` - Required to query users
5. Click **Assign**

### Step 4: Add User Attributes to Realm

1. Go to **Realm Settings** → **User Profile**
2. Click **Create Attribute** for each:

| Attribute Name | Display Name | Required |
|----------------|--------------|----------|
| `phone_number` | Phone Number | No |
| `phone_verified` | Phone Verified | No |
| `phone_country_code` | Phone Country | No |
| `waiting_list_status` | Waiting List Status | No |

3. Save each attribute

### Step 5: Update appsettings.json

```json
{
  "KeycloakAdmin": {
    "BaseUrl": "https://auth.sivar.sv",
    "Realm": "sivar-os",
    "ClientId": "sivaros-server",
    "ClientSecret": "your-client-secret-here",
    "Enabled": true
  }
}
```

### Step 6: Test Connection

The service will automatically:
1. Obtain an access token using client credentials
2. Cache the token until expiration
3. Use the token to call Keycloak Admin API

Check logs for:
```
[KeycloakAdmin] Successfully obtained admin token
```

---

## 🌐 Environment Variables (Production)

For production, use environment variables instead of appsettings.json:

```bash
# Twilio
export Twilio__AccountSid="ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
export Twilio__AuthToken="your-auth-token"
export Twilio__VerifyServiceSid="VAxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
export Twilio__Enabled="true"

# Keycloak Admin
export KeycloakAdmin__BaseUrl="https://auth.sivar.sv"
export KeycloakAdmin__Realm="sivar-os"
export KeycloakAdmin__ClientId="sivaros-server"
export KeycloakAdmin__ClientSecret="your-client-secret"
export KeycloakAdmin__Enabled="true"
```

Or use Azure Key Vault / AWS Secrets Manager for secure secret storage.

---

## 🔒 Middleware (Access Control)

The `WaitingListAccessMiddleware` enforces access control:

| User Status | Behavior |
|-------------|----------|
| **Approved** | Full access to app |
| **Waiting** | Redirected to `/app/waiting` |
| **PendingVerification** | Redirected to `/app/verify-phone` |
| **Rejected** | Redirected to `/app/access-denied` |

### Enabling the Middleware

In `Program.cs`, uncomment:

```csharp
app.UseAuthentication();
app.UseWaitingListAccess(); // ← Uncomment this line
app.UseAuthorization();
```

### Bypass Paths

These paths skip the waiting list check:
- `/api/waitinglist` - Verification endpoints
- `/authentication` - Login/logout
- `/app/waiting` - Waiting status page
- `/app/verify-phone` - Phone verification page
- `/_blazor`, `/_framework` - Blazor internals
- `/css`, `/js`, `/images` - Static assets

---

## 📡 API Endpoints

### Public Endpoints (Authenticated Users)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/waitinglist/verify/request` | Request phone verification OTP |
| POST | `/api/waitinglist/verify/confirm` | Verify OTP and join queue |
| GET | `/api/waitinglist/status` | Get user's waiting list status |
| GET | `/api/waitinglist/referral/validate/{code}` | Check if referral code is valid |

### Admin Endpoints (Admin Role Required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/waitinglist/admin/stats` | Get queue statistics |
| POST | `/api/waitinglist/admin/approve/{userId}` | Approve single user |
| POST | `/api/waitinglist/admin/approve-next/{count}` | Approve next N users |
| POST | `/api/waitinglist/admin/reject/{userId}` | Reject user |

### Example: Request Verification

```bash
curl -X POST https://localhost:5001/api/waitinglist/verify/request \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber": "+50378901234", "countryCode": "SV"}'
```

Response:
```json
{
  "success": true,
  "channel": "WhatsApp",
  "error": null
}
```

---

## 🎨 UI Components

### PhoneVerificationForm.razor
Located in: `Sivar.Os.Client/Components/WaitingList/`

Features:
- Country selector with dial codes
- Phone number input
- Optional referral code field
- 6-digit OTP input
- Success screen with queue position and referral code

### WaitingListStatus.razor
Located in: `Sivar.Os.Client/Components/WaitingList/`

Features:
- Queue position display with progress bar
- Status indicator (Waiting, Approved, Rejected)
- Referral code sharing
- Referral count tracking

### Localization
Both components support English and Spanish:
- `Resources/Components/WaitingList/*.en.json`
- `Resources/Components/WaitingList/*.es.json`

---

## 🔄 Referral System

### How Referrals Work

1. Each user gets a unique **6-character referral code** (e.g., `ABC123`)
2. When a new user enters this code during verification:
   - They get **queue priority** (moved up in the queue)
   - The referrer's **referral count** increases
3. Users can share their code to move up faster

### Priority Calculation

When a referred user joins:
- Default position: `maxPosition + 1` (end of queue)
- With referral: `maxPosition / 2` (middle of queue)

This creates an incentive for users to invite friends.

---

## 📊 Database Entities

### WaitingListEntry
```
- UserId (FK to User)
- Email, PhoneNumber, CountryCode
- Status: PendingVerification | Waiting | Approved | Rejected | Expired
- Position (queue position)
- ReferralCode (user's code to share)
- UsedReferralCode (code they used to sign up)
- ReferredByUserId
- ReferralCount
- JoinedAt, ApprovedAt, ApprovedBy
- AdminNotes
```

### PhoneVerification
```
- UserId (FK to User)
- PhoneNumber, CountryCode
- Channel: SMS | WhatsApp
- Status: Pending | Verified | Expired | Failed
- TwilioVerificationSid
- RequestedAt, ExpiresAt, VerifiedAt
- AttemptCount (max 5)
- IpAddress, UserAgent
```

---

## 🛡️ Security Features

1. **Rate Limiting:** Max 3 OTP requests per hour per user
2. **Attempt Limiting:** Max 5 code entry attempts per OTP
3. **OTP Expiration:** Codes expire after 5 minutes
4. **IP Tracking:** All verification requests logged
5. **Phone Uniqueness:** Each phone can only be used once

---

## 🧪 Testing Without Twilio

For development, you can disable Twilio:

```json
{
  "Twilio": {
    "Enabled": false
  }
}
```

In mock mode:
- OTP requests always succeed
- Any 6-digit code is accepted
- `TwilioVerificationSid` will be "mock-{guid}"

---

## 📝 Troubleshooting

### "Too many verification attempts"
- User exceeded 3 requests per hour
- Wait 1 hour or have admin reset

### "Maximum attempts exceeded"
- User entered wrong code 5 times
- Request a new OTP

### "Phone number is already registered"
- Another user verified with this phone
- Contact support if this is your number

### User stuck in "Waiting" status
- Admin hasn't approved yet
- Check XAF dashboard → Waiting List

### Middleware not blocking users
- Ensure `app.UseWaitingListAccess()` is uncommented in Program.cs
- Must be placed after `UseAuthentication()` and before `UseAuthorization()`
