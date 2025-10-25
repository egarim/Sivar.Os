# Data Flow Reference Guide

## Where User Data Comes From

```
┌─────────────────────────────────────────────────────────────────┐
│                     USER LOGS IN (Keycloak)                    │
│                                                                 │
│  JWT Token Issued With:                                        │
│  ├─ sub (Keycloak ID)                                          │
│  ├─ email                                                       │
│  ├─ given_name (FirstName)                                     │
│  ├─ family_name (LastName)                                     │
│  └─ other claims                                               │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│            UserAuthenticationService.AuthenticateUserAsync()    │
│                                                                 │
│  Executed During:                                              │
│  - Home.razor OnInitializedAsync()                             │
│  - EnsureUserAndProfileCreatedAsync()                          │
│                                                                 │
│  What it does:                                                 │
│  1. Extract Keycloak ID from JWT claims                        │
│  2. Call SivarClient.Auth.AuthenticateUserAsync()              │
│  3. Creates user in database if new                            │
│  4. Updates last login if existing                             │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                     DATABASE STORAGE                           │
│                                                                 │
│  User Table:                                                   │
│  ├─ Id (GUID)                                                  │
│  ├─ KeycloakId (sub claim value)                               │
│  ├─ FirstName (given_name claim)                               │
│  ├─ LastName (family_name claim)                               │
│  ├─ Email (email claim)                                        │
│  ├─ IsActive                                                   │
│  ├─ LastLoginAt                                                │
│  └─ ...other fields                                            │
│                                                                 │
│  Profile Table:                                                │
│  ├─ Id (GUID)                                                  │
│  ├─ UserId (FK to User)                                        │
│  ├─ DisplayName (user-created)                                 │
│  ├─ Avatar (user-created)                                      │
│  ├─ IsActive (current profile)                                 │
│  ├─ Bio                                                        │
│  ├─ LocationDisplay                                            │
│  └─ ...other fields                                            │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│         Home.razor LoadCurrentUserAsync() is called            │
│                                                                 │
│  HTTP Call from CLIENT:                                        │
│  GET /api/users/me                                             │
│  Header: Authorization: Bearer <JWT>                           │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│         UsersController.GetCurrentUser() processes it          │
│                                                                 │
│  Flow:                                                         │
│  1. Extract Keycloak ID from JWT 'sub' claim                   │
│  2. Call UserService.GetCurrentUserAsync(keycloakId)           │
│  3. UserRepository.GetByKeycloakIdAsync(keycloakId)            │
│     → Queries: SELECT * FROM "Users" WHERE "KeycloakId" = ?   │
│  4. Find User record with all fields (FirstName, LastName)     │
│  5. Map to UserDto and return                                  │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│         UserDto returned to Home.razor Component               │
│                                                                 │
│  Data in UserDto:                                              │
│  {                                                             │
│    "id": "9bd777a4-...",                                       │
│    "firstName": "Joche",        ✅ From Database!             │
│    "lastName": "User",         ✅ From Database!              │
│    "email": "joche@joche.com",  ✅ From Database!             │
│    ...                                                         │
│  }                                                             │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│      Home.razor Loads Active Profile (SECOND call)            │
│                                                                 │
│  HTTP Call from CLIENT:                                        │
│  GET /api/profiles/my/active                                   │
│  Header: Authorization: Bearer <JWT>                           │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│      ProfilesController.GetMyActiveProfile() processes it      │
│                                                                 │
│  Flow:                                                         │
│  1. Extract Keycloak ID from JWT 'sub' claim                   │
│  2. Call ProfileService.GetMyActiveProfileAsync(keycloakId)    │
│  3. ProfileRepository.GetActiveProfileByKeycloakIdAsync()      │
│     → Queries: SELECT * FROM "Profiles"                        │
│       WHERE "UserId" = (SELECT "Id" FROM "Users"              │
│       WHERE "KeycloakId" = ?) AND "IsActive" = true           │
│  4. Find Profile with DisplayName, Avatar, LocationDisplay     │
│  5. Map to ProfileDto and return                               │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│      ActiveProfileDto returned to Home.razor Component         │
│                                                                 │
│  Data in ActiveProfileDto:                                     │
│  {                                                             │
│    "id": "0c12973e-...",                                       │
│    "displayName": "Joche's Profile",  ✅ From Database!       │
│    "avatar": "https://...",           ✅ From Database!       │
│    "locationDisplay": "New York, NY",  ✅ From Database!      │
│    ...                                                         │
│  }                                                             │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│           Home.razor Updates Component State                   │
│                                                                 │
│  _userName = userDto.FirstName + " " + userDto.LastName        │
│           = "Joche User"                                       │
│           (or override with activeProfile.DisplayName)         │
│                                                                 │
│  _userEmail = userDto.Email                                    │
│            = "joche@joche.com"                                 │
│                                                                 │
│  StateHasChanged();  // Trigger re-render                      │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│              Header Component Re-renders                       │
│                                                                 │
│  Via two-way binding: @bind-UserName and @bind-UserEmail      │
│                                                                 │
│  Displays:                                                     │
│  ┌─────────────────────────────────────┐                       │
│  │  Joche User        ✅ (or Profile Name if set)              │
│  │  joche@joche.com   ✅                                       │
│  │  [JU Avatar]                                                │
│  └─────────────────────────────────────┘                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## Key Extraction Points

### 1. JWT Token Claims (Source of Truth)
```
JWT Token Header: Authorization: Bearer eyJ...

Payload (decoded):
{
  "sub": "12345-67890-abcdef",        ← Keycloak ID
  "email": "joche@joche.com",         ← Email
  "given_name": "Joche",              ← FirstName
  "family_name": "User",              ← LastName
  "preferred_username": "joche",      ← Username
  "iat": 1698284163,
  "exp": 1698370563,
  ...
}
```

### 2. Controller Extraction (UsersController.GetCurrentUser)
```csharp
var keycloakId = User.FindFirst("sub")?.Value;  // "12345-67890-abcdef"

// This keycloakId is used to look up user:
var user = await _userService.GetCurrentUserAsync(keycloakId);

// Service queries database:
var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);
// SQL: SELECT * FROM "Users" WHERE "KeycloakId" = '12345-67890-abcdef'
```

### 3. Repository Query
```sql
-- UserRepository.GetByKeycloakIdAsync()
SELECT 
    "Id",
    "KeycloakId",
    "Email",
    "FirstName",       ← Retrieved from DB
    "LastName",        ← Retrieved from DB
    "Role",
    "PreferredLanguage",
    "TimeZone",
    "IsActive",
    "LastLoginAt",
    "CreatedAt"
FROM "Users"
WHERE "KeycloakId" = @keycloakId
LIMIT 1;
```

### 4. DTO Mapping
```csharp
// UserService.MapToUserDto()
public UserDto MapToUserDto(User user)
{
    return new UserDto
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,      // ← From database
        LastName = user.LastName,        // ← From database
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };
}
```

---

## Why It Was Broken Before

### The Problem Chain

1. **Controller returned mock Keycloak ID**
   ```csharp
   private string GetKeycloakIdFromRequest()
   {
       return "mock-keycloak-user-id";  // ❌ ALWAYS THIS VALUE
   }
   ```

2. **Service looked up wrong user**
   ```csharp
   // Looking for user with KeycloakId = "mock-keycloak-user-id"
   // But actual user has KeycloakId = "12345-67890-abcdef"
   // Result: No match → returns null or empty user
   ```

3. **DTO returned with empty fields**
   ```csharp
   {
       "id": "...",
       "email": "user@example.com",  // ← Generic fallback
       "firstName": "",              // ← Empty! ❌
       "lastName": "",               // ← Empty! ❌
   }
   ```

4. **Header showed placeholders**
   ```
   Display: "Jordan Doe" (default)  ❌
   Display: "jordan.doe@example.com" (default)  ❌
   ```

---

## Why It's Fixed Now

### The Solution Chain

1. **Controller extracts REAL Keycloak ID from JWT**
   ```csharp
   private string GetKeycloakIdFromRequest()
   {
       if (User?.Identity?.IsAuthenticated == true)
       {
           var keycloakId = User.FindFirst("sub")?.Value;  // ✅ From JWT
           if (!string.IsNullOrEmpty(keycloakId))
           {
               return keycloakId;  // "12345-67890-abcdef"
           }
       }
       return null!;
   }
   ```

2. **Service looks up CORRECT user**
   ```csharp
   // Looking for user with KeycloakId = "12345-67890-abcdef"
   // Found! Returns user from database
   ```

3. **DTO returned with REAL DATA from database**
   ```csharp
   {
       "id": "9bd777a4-...",
       "email": "joche@joche.com",     // ✅ From database
       "firstName": "Joche",           // ✅ From database
       "lastName": "User",             // ✅ From database
   }
   ```

4. **Header shows REAL user information**
   ```
   Display: "Joche User" ✅
   Display: "joche@joche.com" ✅
   ```

---

## Implementation Checklist

- [x] Fixed `UsersController.GetKeycloakIdFromRequest()` to extract from JWT
- [x] Fixed `UsersController.CreateUserDtoFromKeycloakClaims()` to extract claims
- [x] Updated `Home.razor` to use two-way binding for user info
- [x] Added profile loading to `LoadCurrentUserAsync()`
- [x] Header component receives updated UserName and UserEmail

---

## Testing the Fix

### What to look for in browser console:

1. **After login**
   ```
   [Home] Existing user authenticated: joche@joche.com
   ```

2. **During data load**
   ```
   [UsersController] Extracted Keycloak ID from JWT: 12345-67890-abcdef
   [UsersController] Creating user from Keycloak claims: FirstName=Joche, LastName=User
   [HOME-CLIENT] User DTO Received:
     - FirstName: Joche
     - LastName: User
     - Email: joche@joche.com
   ```

3. **Header should display**
   ```
   Joche User
   joche@joche.com
   ```

---

## Hybrid Architecture Recap

### Server-Side Path (if component runs on server)
```
Home.razor (Server Blazor)
  ↓
SivarClient (Server-side: Services.Clients)
  ↓
UserService + UserRepository
  ↓
Database
```
No HTTP call needed - direct database access.

### Client-Side Path (current - WebAssembly)
```
Home.razor (WebAssembly)
  ↓
SivarClient (Client-side: Client.Clients)
  ↓ HTTP GET /api/users/me
UsersController
  ↓
UserService + UserRepository
  ↓
Database
```
HTTP call needed - RESTful communication.

Both use the **same ISivarClient interface** but different implementations!
