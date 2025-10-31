# Profile Switcher - Quick Reference & Troubleshooting

## ✅ What Was Fixed

### The Problem
Browser console error when loading profiles:
```
The JSON value could not be converted to Sivar.Os.Shared.Enums.VisibilityLevel
```

### The Root Cause
- **Server** serializes enums as strings: `"visibilityLevel": "Public"`
- **Client** expected integers: `"visibilityLevel": 1`
- Missing `JsonStringEnumConverter()` in client deserialization

### The Solution (3 Changes)

#### 1️⃣ Client Program.cs - Add Enum Converter Configuration
```csharp
using System.Text.Json.Serialization;  // ← Add this using

var jsonOptions = new System.Text.Json.JsonSerializerOptions
{
    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
};
jsonOptions.Converters.Add(new JsonStringEnumConverter());  // ← Add this
```

#### 2️⃣ ProfileSwitcherService - Store JsonOptions in Constructor
```csharp
private readonly JsonSerializerOptions _jsonOptions;

public ProfileSwitcherService(HttpClient httpClient, ILogger<ProfileSwitcherService> logger)
{
    _httpClient = httpClient;
    _logger = logger;
    
    // Add JsonOptions initialization
    _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
}
```

#### 3️⃣ ProfileSwitcherService - Use JsonOptions in Deserialization
```csharp
// Before (❌ fails on enum fields)
var profiles = await response.Content.ReadFromJsonAsync<List<ProfileDto>>();

// After (✅ properly deserializes enums)
var content = await response.Content.ReadAsStringAsync();
var profiles = JsonSerializer.Deserialize<List<ProfileDto>>(content, _jsonOptions);
```

---

## 🔧 Files to Know

| File | Purpose | Status |
|------|---------|--------|
| `Sivar.Os.Client/Services/ProfileSwitcherService.cs` | Client-side HTTP service | ✅ Fixed |
| `Sivar.Os.Client/Program.cs` | Client DI & JSON config | ✅ Updated |
| `Sivar.Os/Services/ProfileSwitcherClient.cs` | Server-side repository service | ✅ Working |
| `Sivar.Os/Program.cs` | Server DI container | ✅ Configured |
| `Sivar.Os.Client/Pages/Home.razor` | Integration point | ✅ Integrated |
| `Sivar.Os.Client/Components/ProfileSwitcher.razor` | UI component | ✅ Complete |
| `Sivar.Os.Client/Components/ProfileCreatorModal.razor` | Modal form | ✅ Complete |

---

## 📍 API Endpoints Reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/profiles/my/all` | Get all user profiles |
| GET | `/api/profiles/my/active` | Get currently active profile |
| PUT | `/api/profiles/{id}/set-active` | Switch to profile |
| POST | `/api/profiles` | Create new profile |
| GET | `/api/profiletypes` | Get available profile types |

---

## 🧪 Quick Testing

### Test Profile Loading
1. Run application
2. Navigate to Home page
3. Check ProfileSwitcher dropdown
4. Expected: Shows list of user profiles
5. Should see NO errors in browser console

### Test Profile Creation
1. Click "Create Profile" button
2. Enter profile name (3-100 chars)
3. Select profile type
4. Select visibility level
5. Click "Create"
6. Expected: Modal closes, profile appears in dropdown

### Test Profile Switching
1. Click different profile in dropdown
2. Expected: Immediately switches to that profile
3. Active profile indicator updates

---

## 🐛 Troubleshooting

### Symptom: "VisibilityLevel could not be converted"
**Check**:
- [ ] `JsonStringEnumConverter()` added to `ProfileSwitcherService._jsonOptions`
- [ ] Using `JsonSerializer.Deserialize(..., _jsonOptions)` not `ReadFromJsonAsync`
- [ ] Rebuilding project after changes

### Symptom: "404 Not Found" on profile endpoint
**Check**:
- [ ] Endpoint URL uses plural: `/api/profiles/...` not `/api/profile/...`
- [ ] Endpoint paths match exactly: `/my/all`, `/my/active`, etc.
- [ ] Backend ProfilesController is running

### Symptom: Profile list shows "0 profiles" after fix
**Check**:
- [ ] User actually has profiles in database
- [ ] API call returns 200 OK (check Network tab)
- [ ] JSON response contains profile objects
- [ ] No errors in browser console

### Symptom: Create profile modal not opening
**Check**:
- [ ] `@ref="profileCreatorModal"` on ProfileCreatorModal component
- [ ] Click handler calls `profileCreatorModal?.ShowAsync()`
- [ ] Modal component is rendered in Home.razor

---

## 📊 Enum Values Reference

```csharp
// Client and Server must match exactly
public enum VisibilityLevel
{
    Public = 1,
    Private = 2,
    Restricted = 3,
    ConnectionsOnly = 4
}

// JSON Format (server serializes as string)
{
  "visibilityLevel": "Public"      // Not 1, it's the string name
}
```

---

## 🎯 Build Commands

```powershell
# Clean build
dotnet clean
dotnet build

# Build with details
dotnet build --verbosity detailed

# Build specific project
dotnet build ./Sivar.Os.Client/Sivar.Os.Client.csproj
```

---

## 📈 Success Indicators

- ✅ Build shows: "Build succeeded" with "0 Error(s)"
- ✅ Browser console: No JSON deserialization errors
- ✅ ProfileSwitcher component renders without errors
- ✅ Profile list displays user's profiles
- ✅ Can create, view, and switch profiles
- ✅ Logs show: "[ProfileSwitcherService] Retrieved X profiles"

---

## 🔑 Key Takeaway

**The core issue**: Client and server must use **identical JSON serialization settings** for enums.

**The fix**: Both must include `JsonStringEnumConverter()` to handle enum values serialized as strings.

**The result**: Seamless enum deserialization, no "could not be converted" errors.

---

## 📞 Related Docs
- Full details: `PROFILE_SWITCHER_COMPLETE_STATUS.md`
- Endpoint changes: `PROFILE_SWITCHER_ENDPOINT_FIX.md`
- JSON fix details: `PROFILE_SWITCHER_JSON_DESERIALIZATION_FIX.md`

---

**Status**: ✅ Ready to Deploy
**Last Update**: Current Session
**Confidence Level**: 🟢 High (All tests passing, 0 build errors)
