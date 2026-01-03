# 📋 Waiting List with SMS/WhatsApp Verification

**Issue:** Implement gradual user onboarding with phone verification  
**Priority:** High  
**Status:** Planning  
**Created:** 2026-01-03

### ✅ Decisions Made
- **OTP Provider:** Twilio Verify API
- **Keycloak Integration:** External verification (no plugins needed)
- **Approval Mode:** Manual admin approval via XAF dashboard
- **Referral System:** Yes, with queue priority

---

## 📋 Overview

Implement a waiting list system to control user access to Sivar.Os, with phone verification via **WhatsApp** or **SMS** based on the user's country. This allows gradual onboarding while ensuring user authenticity.

### Goals
1. Control app access through invitation-based waiting list
2. Verify users via WhatsApp (El Salvador, Latin America) or SMS (US, Europe)
3. Integrate with Keycloak using external verification (no Java plugins)
4. Track waiting list position and notify users when approved
5. XAF admin dashboard for managing signups and approvals

---

## 🏗️ Architecture Overview (External Verification Approach)

**No Keycloak modification needed!** All verification happens in our .NET backend.

```
┌─────────────────────────────────────────────────────────────────┐
│  1. User registers in Keycloak (email + password only)          │
│     - Standard Keycloak registration                            │
│     - User gets "pending" attribute                             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  2. Redirect to /app/verify-phone (our Blazor page)             │
│     - Country detection → WhatsApp or SMS                       │
│     - Twilio Verify API sends OTP                               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  3. User verifies OTP → Added to waiting list                   │
│     - Position calculated (with referral priority)              │
│     - Keycloak attributes updated via Admin API                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  4. Admin approves via XAF Dashboard                            │
│     - View all pending users with stats                         │
│     - Batch approve / reject                                    │
│     - Send approval notification (WhatsApp/SMS)                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  5. User logs in → Full app access                              │
│     - Middleware checks waiting_list_status = "approved"        │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔐 Keycloak Configuration (No Code Required)

### 1. Add Custom User Attributes
In Keycloak Admin → Realm Settings → User Profile, add:

| Attribute | Type | Description |
|-----------|------|-------------|
| `phone_number` | String | User's verified phone |
| `phone_verified` | Boolean | Phone verification status |
| `waiting_list_status` | String | pending_verification, waiting, approved, rejected |
| `country_code` | String | ISO 3166-1 alpha-2 (SV, US, etc.) |

### 2. Post-Registration Redirect
Configure in app to redirect new users to `/app/verify-phone` if not verified.

### 3. Keycloak Admin API Access
Service account for updating user attributes after verification:
```json
{
  "Keycloak": {
    "AdminApiUrl": "https://auth.sivar.sv/admin/realms/sivar-os",
    "ServiceAccountClientId": "sivaros-server",
    "ServiceAccountSecret": "your-secret"
  }
}
```

---

## 🚀 Implementation Phases

### Phase 1: Database & Entities
- [ ] Create `WaitingListEntry` entity in Sivar.Os.Shared
- [ ] Create `PhoneVerification` entity in Sivar.Os.Shared
- [ ] Add `WaitingListStatus` enum
- [ ] Add `VerificationChannel` enum
- [ ] Database migrations
- [ ] Register entities in XAF Module

### Phase 2: Twilio Verify Service
- [ ] Add Twilio NuGet package
- [ ] Create `TwilioOptions` configuration class
- [ ] Create `ITwilioVerifyService` interface
- [ ] Implement `TwilioVerifyService`
- [ ] Country-based channel selection (WhatsApp vs SMS)
- [ ] Unit tests

### Phase 3: Keycloak Admin API Integration
- [ ] Create `IKeycloakAdminService` interface
- [ ] Implement user attribute updates
- [ ] Get user by Keycloak ID
- [ ] Update waiting list status in Keycloak

### Phase 4: Waiting List Service
- [ ] Create `IWaitingListService` interface
- [ ] Implement `WaitingListService`
- [ ] Queue position calculation
- [ ] Referral code generation
- [ ] Referral priority logic

### Phase 5: API Endpoints
- [ ] `POST /api/waitinglist/request-otp` - Send OTP
- [ ] `POST /api/waitinglist/verify-otp` - Verify phone
- [ ] `GET /api/waitinglist/status` - Check position
- [ ] `POST /api/waitinglist/approve` - Admin approve (batch)
- [ ] `POST /api/waitinglist/reject` - Admin reject

### Phase 6: Blazor UI Components
- [ ] Phone verification page (`/app/verify-phone`)
- [ ] Waiting list status page (`/app/waiting`)
- [ ] Referral share component

### Phase 7: XAF Admin Dashboard
- [ ] Register `WaitingListEntry` in OsModule
- [ ] Register `PhoneVerification` in OsModule  
- [ ] Create "Waiting List" navigation group
- [ ] Waiting list ListView with filters
- [ ] Batch approve action
- [ ] Batch reject action
- [ ] Dashboard with signup stats
- [ ] Verification analytics view

### Phase 8: Notifications
- [ ] Approval notification via WhatsApp/SMS
- [ ] Email notification backup
- [ ] Notification templates (Spanish/English)

### Phase 9: Middleware & Security
- [ ] Create `WaitingListMiddleware`
- [ ] Block unapproved users from protected routes
- [ ] Rate limiting for OTP requests

---

## 📊 Entity Designs

### WaitingListEntry
```csharp
public class WaitingListEntry : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string CountryCode { get; set; }  // ISO 3166-1 alpha-2
    
    public WaitingListStatus Status { get; set; }
    public int Position { get; set; }
    
    public DateTime JoinedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }  // Admin who approved
    
    public string ReferralCode { get; set; }  // This user's code to share
    public string? UsedReferralCode { get; set; }  // Code they used to sign up
    public Guid? ReferredByUserId { get; set; }
    public int ReferralCount { get; set; }  // How many people they referred
}
```

### PhoneVerification
```csharp
public class PhoneVerification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public string PhoneNumber { get; set; }
    public string CountryCode { get; set; }
    
    public VerificationChannel Channel { get; set; }  // SMS, WhatsApp
    public VerificationStatus Status { get; set; }  // Pending, Verified, Expired, Failed
    
    public string? TwilioVerificationSid { get; set; }  // Twilio's verification ID
    
    public DateTime RequestedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
```

### Enums
```csharp
public enum WaitingListStatus
{
    PendingVerification,  // Phone not verified yet
    Waiting,              // Verified, waiting for approval
    Approved,             // Can access the app
    Rejected,             // Denied access
    Expired               // Never completed verification
}

public enum VerificationChannel
{
    SMS,
    WhatsApp
}

public enum VerificationStatus
{
    Pending,
    Verified,
    Expired,
    Failed
}
```

---

## 🌍 Country-Based Channel Selection

| Region | Countries | Channel | Reason |
|--------|-----------|---------|--------|
| Central America | SV, GT, HN, NI, CR, PA | WhatsApp | Higher adoption, cheaper |
| South America | MX, CO, PE, AR, CL, etc. | WhatsApp | Higher adoption |
| North America | US, CA | SMS | Standard preference |
| Europe | ES, DE, FR, UK, etc. | SMS | GDPR compliance easier |
| Default | Others | SMS | Fallback |

### Country Detection
```csharp
public VerificationChannel GetChannelForCountry(string countryCode)
{
    var whatsAppCountries = new[] { 
        "SV", "GT", "HN", "NI", "CR", "PA",  // Central America
        "MX", "CO", "PE", "AR", "CL", "EC", "VE", "BO", "PY", "UY"  // South America
    };
    
    return whatsAppCountries.Contains(countryCode.ToUpper()) 
        ? VerificationChannel.WhatsApp 
        : VerificationChannel.SMS;
}
```

---

## 🔧 Twilio Verify Configuration

### appsettings.json
```json
{
  "Twilio": {
    "AccountSid": "ACxxxxxxxxxx",
    "AuthToken": "your-auth-token",
    "VerifyServiceSid": "VAxxxxxxxxxx"
  }
}
```

### TwilioOptions.cs
```csharp
public class TwilioOptions
{
    public const string SectionName = "Twilio";
    
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string VerifyServiceSid { get; set; } = string.Empty;
}
```

### ITwilioVerifyService.cs
```csharp
public interface ITwilioVerifyService
{
    Task<VerificationResult> SendVerificationAsync(string phoneNumber, string countryCode);
    Task<VerificationCheckResult> CheckVerificationAsync(string phoneNumber, string code);
    VerificationChannel GetChannelForCountry(string countryCode);
}
```

### Twilio Verify API Usage
```csharp
// Send verification (Twilio handles OTP generation)
var verification = await VerificationResource.CreateAsync(
    to: phoneNumber,
    channel: channel == VerificationChannel.WhatsApp ? "whatsapp" : "sms",
    pathServiceSid: _options.VerifyServiceSid
);

// Check verification
var verificationCheck = await VerificationCheckResource.CreateAsync(
    to: phoneNumber,
    code: userEnteredCode,
    pathServiceSid: _options.VerifyServiceSid
);
// verificationCheck.Status == "approved" means success
```

---

## � User Flow (Step by Step)

### 1. Registration
```
User clicks "Sign Up" → Keycloak registration page
       ↓
Fills: email, password, name → Keycloak creates user
       ↓
Redirect to: /app/verify-phone
```

### 2. Phone Verification
```
User enters phone number: +503 7890-1234
       ↓
System detects country (SV) → WhatsApp channel
       ↓
Twilio Verify API sends OTP via WhatsApp
       ↓
User receives: "Tu código Sivar.Os: 123456"
       ↓
User enters OTP → Twilio validates → Verified ✅
```

### 3. Join Waiting List
```
Phone verified → User added to waiting list
       ↓
Position calculated:
  - Regular signup → position = last + 1 (e.g., #100)
  - Has referral code → position = middle (e.g., #50) ⭐
       ↓
Keycloak Admin API updates user attributes:
  - phone_verified = true
  - phone_number = +50378901234
  - waiting_list_status = waiting
       ↓
User sees: "You're #42 in line! Share to move up!"
```

### 4. Admin Approval (XAF Dashboard)
```
Admin opens XAF → Waiting List navigation group
       ↓
Views WaitingListEntry list with filters:
  - Status: Waiting, Pending, Approved, Rejected
  - Country, Date joined, Referral count
       ↓
Selects users → Clicks "Approve Selected" action
       ↓
System:
  1. Updates WaitingListEntry.Status = Approved
  2. Calls Keycloak Admin API → waiting_list_status = approved
  3. Sends WhatsApp/SMS: "🎉 You're in! Click to access"
```

### 5. User Access
```
User clicks link or logs in
       ↓
Middleware checks: waiting_list_status == "approved"?
       ↓
YES → Full app access
NO  → Redirect to /app/waiting (shows position)
```

---

## 📋 API Endpoints

### Request OTP
```http
POST /api/waitinglist/request-otp
Authorization: Bearer {token}
Content-Type: application/json

{
  "phoneNumber": "+50378901234",
  "countryCode": "SV"
}

Response:
{
  "success": true,
  "channel": "WhatsApp",
  "expiresInSeconds": 300
}
```

### Verify OTP
```http
POST /api/waitinglist/verify-otp
Authorization: Bearer {token}
Content-Type: application/json

{
  "phoneNumber": "+50378901234",
  "code": "123456",
  "referralCode": "ABC123"  // optional
}

Response:
{
  "success": true,
  "position": 42,
  "referralCode": "XYZ789"  // their code to share
}
```

### Check Status
```http
GET /api/waitinglist/status
Authorization: Bearer {token}

Response:
{
  "status": "Waiting",
  "position": 42,
  "totalWaiting": 150,
  "referralCode": "XYZ789",
  "referralCount": 3,
  "joinedAt": "2026-01-03T10:00:00Z"
}
```

### Admin: Approve Users (Batch)
```http
POST /api/waitinglist/approve
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "userIds": ["guid1", "guid2", "guid3"]
}
// OR
{
  "approveNextCount": 100  // Approve next 100 in queue
}
```

---

## 🖥️ XAF Admin Dashboard

### Navigation Group: "Waiting List"
Add to `OsModule.cs`:
```csharp
// === Waiting List ===
AdditionalExportedTypes.Add(typeof(WaitingListEntry));
AdditionalExportedTypes.Add(typeof(PhoneVerification));
```

### Navigation Items

| Item | View | Description |
|------|------|-------------|
| 📋 All Signups | WaitingListEntry ListView | All entries with status filter |
| ⏳ Pending Verification | Filtered ListView | Status = PendingVerification |
| 🕐 Waiting for Approval | Filtered ListView | Status = Waiting |
| ✅ Approved Users | Filtered ListView | Status = Approved |
| ❌ Rejected | Filtered ListView | Status = Rejected |
| 📊 Signup Stats | Dashboard | Charts and metrics |
| 📱 Verifications | PhoneVerification ListView | All verification attempts |

### WaitingListEntry ListView Columns

| Column | Type | Notes |
|--------|------|-------|
| Position | int | Queue position |
| Email | string | From User |
| Phone Number | string | Verified phone |
| Country | string | Flag emoji + code |
| Status | enum | Color-coded badge |
| Joined At | DateTime | Registration date |
| Referral Code | string | Their shareable code |
| Referral Count | int | How many they referred |
| Referred By | string | Who referred them |
| Approved At | DateTime? | When approved |
| Approved By | string | Admin who approved |

### XAF Actions

#### Approve Selected (Batch)
```csharp
[Action("Approve Selected", TargetObjectsCriteria = "Status = 'Waiting'")]
public void ApproveSelected(IEnumerable<WaitingListEntry> entries)
{
    foreach (var entry in entries)
    {
        entry.Status = WaitingListStatus.Approved;
        entry.ApprovedAt = DateTime.UtcNow;
        entry.ApprovedBy = SecuritySystem.CurrentUserName;
        
        // Update Keycloak via service
        // Send notification via Twilio
    }
}
```

#### Reject Selected
```csharp
[Action("Reject Selected", TargetObjectsCriteria = "Status = 'Waiting'")]
public void RejectSelected(IEnumerable<WaitingListEntry> entries)
{
    foreach (var entry in entries)
    {
        entry.Status = WaitingListStatus.Rejected;
    }
}
```

#### Approve Next N
```csharp
[Action("Approve Next...")]
public void ApproveNextN(int count)
{
    var nextEntries = ObjectSpace.GetObjects<WaitingListEntry>()
        .Where(e => e.Status == WaitingListStatus.Waiting)
        .OrderBy(e => e.Position)
        .Take(count);
    
    ApproveSelected(nextEntries);
}
```

### Dashboard Stats View
- Total signups (all time)
- Signups today / this week / this month
- Pending verifications count
- Waiting for approval count
- Approved this week
- Top referrers leaderboard
- Signups by country (pie chart)
- Signups over time (line chart)

---

## ✅ Acceptance Criteria

### Phase 1 Complete When:
- [ ] `WaitingListEntry` entity created in Sivar.Os.Shared
- [ ] `PhoneVerification` entity created in Sivar.Os.Shared
- [ ] Enums created (WaitingListStatus, VerificationChannel, VerificationStatus)
- [ ] EF Core configurations added
- [ ] Database migration applied
- [ ] Entities registered in XAF OsModule

### Phase 2 Complete When:
- [ ] Twilio NuGet package installed
- [ ] `TwilioOptions` configuration class created
- [ ] `TwilioVerifyService` implemented
- [ ] Can send OTP via SMS to US number
- [ ] Can send OTP via WhatsApp to SV number
- [ ] Country-based channel selection works

### Phase 3 Complete When:
- [ ] `KeycloakAdminService` implemented
- [ ] Can update user attributes via Admin API
- [ ] Can read user by Keycloak ID

### Phase 4 Complete When:
- [ ] `WaitingListService` implemented
- [ ] Queue position calculation works
- [ ] Referral code generation works
- [ ] Referral priority (queue jump) works

### Phase 5 Complete When:
- [ ] All API endpoints functional and tested
- [ ] Proper authorization on admin endpoints

### Phase 6 Complete When:
- [ ] `/app/verify-phone` page works
- [ ] `/app/waiting` status page shows position and referral link
- [ ] Unapproved users redirected to waiting page

### Phase 7 Complete When:
- [ ] XAF shows "Waiting List" navigation group
- [ ] WaitingListEntry ListView displays correctly
- [ ] PhoneVerification ListView displays correctly
- [ ] "Approve Selected" action works
- [ ] "Reject Selected" action works
- [ ] "Approve Next N" action works
- [ ] Dashboard shows signup stats

### Phase 8 Complete When:
- [ ] Approved users receive WhatsApp/SMS notification
- [ ] Email backup notification sent
- [ ] Notifications in Spanish and English

### Phase 9 Complete When:
- [ ] `WaitingListMiddleware` blocks unapproved users
- [ ] Rate limiting prevents OTP abuse (max 3/hour)
- [ ] Protected routes require approved status

---

## 🔗 Related Issues
- [KEYCLOAKIFY_THEME.md](KEYCLOAKIFY_THEME.md) - Custom login theme
- [KEYCLOAK_DEMO_USERS.md](KEYCLOAK_DEMO_USERS.md) - Test users

---

## 📝 Notes

- Rate limiting: Max 3 OTP requests per phone per hour
- OTP expiry: 5 minutes (handled by Twilio Verify)
- Max verification attempts: 5 per OTP
- Consider adding CAPTCHA before phone verification
- Referral priority: referred users get 50% queue jump
- Referrer bonus: Each referral moves referrer up 10 positions

---

## 📁 Files to Create

### Sivar.Os.Shared/Entities
- `WaitingListEntry.cs`
- `PhoneVerification.cs`

### Sivar.Os.Shared/Enums
- `WaitingListStatus.cs`
- `VerificationChannel.cs`
- `VerificationStatus.cs`

### Sivar.Os/Configuration
- `TwilioOptions.cs`

### Sivar.Os/Services
- `ITwilioVerifyService.cs`
- `TwilioVerifyService.cs`
- `IKeycloakAdminService.cs`
- `KeycloakAdminService.cs`
- `IWaitingListService.cs`
- `WaitingListService.cs`

### Sivar.Os/Controllers
- `WaitingListController.cs`

### Sivar.Os/Middleware
- `WaitingListMiddleware.cs`

### Sivar.Os.Client/Pages
- `VerifyPhone.razor`
- `WaitingStatus.razor`

### Xaf.Sivar.Os.Module
- Update `Module.cs` with entity registrations
- `Controllers/WaitingListViewController.cs` (for actions)
