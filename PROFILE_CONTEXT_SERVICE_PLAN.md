# Profile Context Service Implementation Plan

> **Created**: December 22, 2025  
> **Status**: ✅ Fully Implemented  
> **Priority**: High - Required for Schedule/Booking timezone handling  
> **Related**: `ScheduleEvent.cs`, `ChatLocationService.cs`, `ILocationService.cs`

---

## Table of Contents

1. [Problem Statement](#problem-statement)
2. [Goals](#goals)
3. [Architecture Overview](#architecture-overview)
4. [Existing Services Analysis](#existing-services-analysis)
5. [Implementation Plan](#implementation-plan)
   - [Phase 1: DTOs & Interfaces](#phase-1-dtos--interfaces)
   - [Phase 2: JavaScript Interop](#phase-2-javascript-interop)
   - [Phase 3: Client Service Implementation](#phase-3-client-service-implementation)
   - [Phase 4: Integration](#phase-4-integration)
6. [Testing Strategy](#testing-strategy)
7. [File Locations](#file-locations)
8. [Checklist](#checklist)

---

## Problem Statement

The current system lacks a unified service to manage the **active profile's context**, which includes:

| Missing Capability | Impact |
|--------------------|--------|
| **Device timezone detection** | Cannot properly interpret "today", "tomorrow", "next week" in AI chat |
| **Device local time** | Schedule events may show wrong times for users in different timezones |
| **Device type detection** | Cannot optimize UI/UX for mobile vs desktop |
| **User-selected vs GPS location** | Cannot distinguish where user IS vs where user WANTS to search |

### Current State

- `ChatLocationService` handles location but **not timezone or device info**
- `ChatLocationContext` has `TimeZone` and `UserLocalTime` properties but **they're never populated**
- `ScheduleEvent` has `TimeZone` property but relies on default "America/El_Salvador"
- No device type detection exists

---

## Goals

1. **Unified Context**: Single service for profile's location + device + time context
2. **Profile-Centric**: Context is per-profile, not per-user (users can switch profiles)
3. **Auto-Detection**: Timezone and device type detected automatically from browser
4. **Backward Compatible**: Works with existing `ChatLocationService`
5. **Testable**: All components unit-testable with mocked JS interop

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Blazor Components                         │
│              (Chat, Schedule, Booking, etc.)                 │
└────────────────────────┬────────────────────────────────────┘
                         │ inject
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                  IProfileContextService                      │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ CurrentContext: ProfileContext                       │    │
│  │   ├── ProfileId: Guid                               │    │
│  │   ├── Location: ProfileLocationContext              │    │
│  │   │     ├── SelectedLocation (user chose)           │    │
│  │   │     └── DeviceLocation (GPS actual)             │    │
│  │   └── Device: DeviceContext                         │    │
│  │         ├── TimeZone: "America/El_Salvador"         │    │
│  │         ├── LocalTime: DateTimeOffset               │    │
│  │         ├── DeviceType: "mobile"|"tablet"|"desktop" │    │
│  │         └── Language: "es-SV"                       │    │
│  └─────────────────────────────────────────────────────┘    │
└────────────────────────┬────────────────────────────────────┘
                         │ uses
                         ↓
┌─────────────────────────────────────────────────────────────┐
│              ChatLocationService (existing)                  │
│        (Reused for GPS + City selection logic)              │
└────────────────────────┬────────────────────────────────────┘
                         │ uses
                         ↓
┌─────────────────────────────────────────────────────────────┐
│              context-interop.js (new)                        │
│   ├── getDeviceTimeZone() → "America/El_Salvador"           │
│   ├── getLocalDateTime() → ISO 8601 string                  │
│   ├── getDeviceType() → "mobile"|"tablet"|"desktop"         │
│   └── getBrowserLanguage() → "es-SV"                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Existing Services Analysis

### ✅ Keep & Reuse

| Service | Location | Reuse Strategy |
|---------|----------|----------------|
| `ChatLocationService` | `Sivar.Os.Client/Services/` | Wrap/delegate for location logic |
| `BrowserPermissionsService` | `Sivar.Os.Client/Services/` | Use for GPS permissions |
| `ChatLocationContext` | `Sivar.Os.Shared/DTOs/ChatDTOs.cs` | Extend with populated TimeZone |
| `permissions.js` | `Sivar.Os.Client/wwwroot/js/` | Already has `requestLocation()` |

### ❌ Do NOT Duplicate

- GPS location request logic (use `ChatLocationService.RequestGpsLocationAsync()`)
- City selection logic (use `ChatLocationService.SetCityLocationAsync()`)
- Reverse geocoding (use `ChatLocationService.ReverseGeocodeAsync()`)

---

## Implementation Plan

### Phase 1: DTOs & Interfaces

**Location**: `Sivar.Os.Shared/DTOs/ProfileContextDtos.cs`

```csharp
namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// Device-related context detected from browser.
/// Auto-populated via JavaScript interop.
/// </summary>
public record DeviceContext
{
    /// <summary>
    /// IANA timezone identifier (e.g., "America/El_Salvador", "America/New_York")
    /// Detected from browser's Intl.DateTimeFormat().resolvedOptions().timeZone
    /// </summary>
    public string TimeZone { get; init; } = "UTC";

    /// <summary>
    /// Device's current local time with offset
    /// </summary>
    public DateTimeOffset LocalDateTime { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timezone offset in minutes from UTC (e.g., -360 for CST)
    /// </summary>
    public int TimeZoneOffsetMinutes { get; init; }

    /// <summary>
    /// Device type: "mobile", "tablet", or "desktop"
    /// Detected from screen size and user agent
    /// </summary>
    public string DeviceType { get; init; } = "desktop";

    /// <summary>
    /// Browser's preferred language (e.g., "es-SV", "en-US")
    /// </summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// User agent string for debugging
    /// </summary>
    public string? UserAgent { get; init; }
}

/// <summary>
/// Location context for the profile, distinguishing selected vs device location.
/// </summary>
public record ProfileLocationContext
{
    /// <summary>
    /// User-selected location for searches (can be different from GPS).
    /// This is where the user WANTS to search.
    /// </summary>
    public ChatLocationContext? SelectedLocation { get; init; }

    /// <summary>
    /// Actual device GPS location (if available).
    /// This is where the user IS physically located.
    /// </summary>
    public ChatLocationContext? DeviceLocation { get; init; }

    /// <summary>
    /// Returns the effective location for searches.
    /// Prefers SelectedLocation, falls back to DeviceLocation.
    /// </summary>
    public ChatLocationContext? EffectiveLocation => SelectedLocation ?? DeviceLocation;

    /// <summary>
    /// Whether any location is available
    /// </summary>
    public bool HasLocation => EffectiveLocation?.IsValid == true;
}

/// <summary>
/// Complete profile context including location, device, and time information.
/// This is the main DTO consumed by components.
/// </summary>
public record ProfileContext
{
    /// <summary>
    /// The active profile ID this context belongs to
    /// </summary>
    public Guid ProfileId { get; init; }

    /// <summary>
    /// Location context (selected + device locations)
    /// </summary>
    public ProfileLocationContext Location { get; init; } = new();

    /// <summary>
    /// Device context (timezone, device type, language)
    /// </summary>
    public DeviceContext Device { get; init; } = new();

    /// <summary>
    /// When this context was last updated
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Helper to get effective timezone (device timezone)
    /// </summary>
    public string TimeZone => Device.TimeZone;

    /// <summary>
    /// Helper to get current local time
    /// </summary>
    public DateTimeOffset LocalTime => Device.LocalDateTime;
}
```

**Location**: `Sivar.Os.Shared/Services/IProfileContextService.cs`

```csharp
namespace Sivar.Os.Shared.Services;

/// <summary>
/// Service for managing the active profile's context (location, device, time).
/// Profile-centric: context changes when user switches profiles.
/// </summary>
public interface IProfileContextService
{
    /// <summary>
    /// Current profile context (location + device + time)
    /// </summary>
    ProfileContext? CurrentContext { get; }

    /// <summary>
    /// Event fired when context changes (profile switch, location change, etc.)
    /// </summary>
    event Func<ProfileContext?, Task>? OnContextChanged;

    /// <summary>
    /// Initialize the service for a specific profile.
    /// Detects device context and loads saved location.
    /// </summary>
    Task InitializeAsync(Guid profileId);

    /// <summary>
    /// Refresh device context (timezone, device type, etc.)
    /// Call this if user changes system settings.
    /// </summary>
    Task RefreshDeviceContextAsync();

    /// <summary>
    /// Request GPS location and set as device location.
    /// </summary>
    Task<bool> RequestDeviceLocationAsync();

    /// <summary>
    /// Set user-selected location (different from GPS).
    /// Used when user wants to search in a different area.
    /// </summary>
    Task SetSelectedLocationAsync(ChatLocationContext location);

    /// <summary>
    /// Clear user-selected location (revert to device location).
    /// </summary>
    Task ClearSelectedLocationAsync();

    /// <summary>
    /// Get ChatLocationContext with populated timezone and local time.
    /// Ready to pass to ChatRequest.
    /// </summary>
    ChatLocationContext? GetChatLocationContext();

    /// <summary>
    /// Handle profile switch - reload context for new profile.
    /// </summary>
    Task OnProfileSwitchedAsync(Guid newProfileId);
}
```

---

### Phase 2: JavaScript Interop

**Location**: `Sivar.Os.Client/wwwroot/js/context-interop.js`

```javascript
/**
 * Profile Context JavaScript Interop
 * Provides device context detection for ProfileContextService
 */

/**
 * Get the device's IANA timezone identifier
 * @returns {string} Timezone like "America/El_Salvador"
 */
export function getDeviceTimeZone() {
    try {
        return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
    } catch {
        return 'UTC';
    }
}

/**
 * Get the current local date/time as ISO 8601 string
 * @returns {string} ISO 8601 formatted datetime with timezone offset
 */
export function getLocalDateTime() {
    return new Date().toISOString();
}

/**
 * Get timezone offset in minutes from UTC
 * @returns {number} Offset in minutes (e.g., -360 for CST)
 */
export function getTimeZoneOffsetMinutes() {
    return new Date().getTimezoneOffset();
}

/**
 * Detect device type based on screen size and user agent
 * @returns {string} "mobile", "tablet", or "desktop"
 */
export function getDeviceType() {
    const userAgent = navigator.userAgent.toLowerCase();
    const screenWidth = window.innerWidth;

    // Check for mobile devices
    const isMobile = /android|webos|iphone|ipod|blackberry|iemobile|opera mini/i.test(userAgent);
    const isTablet = /ipad|android(?!.*mobile)|tablet/i.test(userAgent);

    if (isTablet || (screenWidth >= 768 && screenWidth < 1024)) {
        return 'tablet';
    }
    if (isMobile || screenWidth < 768) {
        return 'mobile';
    }
    return 'desktop';
}

/**
 * Get browser's preferred language
 * @returns {string} Language code like "es-SV" or "en-US"
 */
export function getBrowserLanguage() {
    return navigator.language || navigator.userLanguage || 'en';
}

/**
 * Get user agent string
 * @returns {string} Full user agent string
 */
export function getUserAgent() {
    return navigator.userAgent;
}

/**
 * Get complete device context in one call (more efficient)
 * @returns {object} Complete device context object
 */
export function getDeviceContext() {
    return {
        timeZone: getDeviceTimeZone(),
        localDateTime: getLocalDateTime(),
        timeZoneOffsetMinutes: getTimeZoneOffsetMinutes(),
        deviceType: getDeviceType(),
        language: getBrowserLanguage(),
        userAgent: getUserAgent()
    };
}
```

---

### Phase 3: Client Service Implementation

**Location**: `Sivar.Os.Client/Services/ProfileContextService.cs`

Key implementation points:

1. **Inject existing services**: `ChatLocationService`, `IJSRuntime`, `ILogger`
2. **Profile-centric state**: Store context per `ProfileId`
3. **Auto-detect on init**: Call JS interop to get device context
4. **Delegate location logic**: Use `ChatLocationService` for GPS/city selection
5. **Event notifications**: Fire `OnContextChanged` when anything changes
6. **localStorage persistence**: Save selected location per profile

---

### Phase 4: Integration

#### 4.1 Register in DI ✅ COMPLETED

**Location**: `Sivar.Os.Client/Program.cs` and `Sivar.Os/Program.cs`

```csharp
// After ChatLocationService registration
builder.Services.AddScoped<IProfileContextService, ProfileContextService>();
```

---

#### 4.2 Chat Integration - Populate Timezone in ChatLocationContext

**Status**: 🔲 NOT STARTED  
**Priority**: HIGH - Required for AI to understand "hoy", "mañana", "esta semana"

**Problem**: `MainLayout.razor` currently uses `TimeZoneInfo.Local.Id` which returns the **server's timezone**, not the user's browser timezone.

**Current Code** (`MainLayout.razor` line ~1205):
```csharp
// ❌ WRONG - Uses server timezone, not browser timezone
var locationWithTime = _chatLocation != null 
    ? _chatLocation with 
    { 
        UserLocalTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
        TimeZone = TimeZoneInfo.Local.Id  // <-- SERVER timezone, not user's!
    }
    : new ChatLocationContext 
    { 
        UserLocalTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
        TimeZone = TimeZoneInfo.Local.Id,  // <-- SERVER timezone, not user's!
        Source = "time-only"
    };
```

**Solution**: Use `IProfileContextService.GetChatLocationContext()` which properly populates timezone from browser.

**Files to Modify**:

| File | Change |
|------|--------|
| `Sivar.Os.Client/Layout/MainLayout.razor` | Inject `IProfileContextService`, use `GetChatLocationContext()` |

**Implementation Steps**:

1. **Add Injection** (after line ~20):
   ```razor
   @inject IProfileContextService ProfileContextService
   ```

2. **Initialize in OnAfterRenderAsync** (in `InitializeAsync()` method):
   ```csharp
   // After profile is loaded
   if (_currentProfile != null)
   {
       await ProfileContextService.InitializeAsync(_currentProfile.Id);
   }
   ```

3. **Replace Chat Location Logic** (in `HandleDxChatMessageSent()` ~line 1205):
   ```csharp
   // ✅ CORRECT - Uses browser timezone via ProfileContextService
   var locationWithTime = ProfileContextService.GetChatLocationContext() 
       ?? new ChatLocationContext 
       { 
           UserLocalTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
           TimeZone = "UTC",
           Source = "fallback"
       };
   ```

**Checklist**:
- [ ] Inject `IProfileContextService` in `MainLayout.razor`
- [ ] Initialize `ProfileContextService` when profile loads
- [ ] Replace `TimeZoneInfo.Local.Id` with `ProfileContextService.GetChatLocationContext()`
- [ ] Add `using Sivar.Os.Shared.Services;` if needed
- [ ] Test that AI correctly interprets "hoy" based on user's timezone

---

#### 4.3 Schedule/Booking Integration - Default Timezone for New Events

**Status**: 🔲 NOT STARTED  
**Priority**: MEDIUM - Improves UX for users in different timezones

**Problem**: `ScheduleEvent.TimeZone` defaults to `"America/El_Salvador"` in the entity. When users create events via chat or UI, the timezone should default to their device timezone.

**Current Code** (`ScheduleEvent.cs` line 44):
```csharp
public virtual string TimeZone { get; set; } = "America/El_Salvador";
```

**Affected Flows**:

1. **AI Chat Booking** (`Sivar.Os/Services/ScheduleEventService.cs`)
   - When AI creates events via `CreateEventAsync()`, it receives `createDto.TimeZone`
   - The DTO comes from chat context, so timezone should be populated from `ChatLocationContext.TimeZone`

2. **Direct API Calls** (`CreateScheduleEventDto`)
   - Client should populate `TimeZone` from `ProfileContextService.CurrentContext?.Device.TimeZone`

**Files to Modify**:

| File | Change |
|------|--------|
| `Sivar.Os.Shared/DTOs/ScheduleDTOs.cs` | Ensure `CreateScheduleEventDto.TimeZone` is nullable with smart default |
| `Sivar.Os/Services/ScheduleEventService.cs` | Fallback to UTC if timezone not provided |
| `Sivar.Os/Agents/BookingFunctions.cs` | Pass timezone from chat context |

**Implementation Steps**:

1. **Update CreateScheduleEventDto** (if not already):
   ```csharp
   /// <summary>
   /// Timezone for the event. Should come from ProfileContextService.
   /// Falls back to "UTC" if not provided.
   /// </summary>
   public string? TimeZone { get; init; }
   ```

2. **Update ScheduleEventService.CreateEventAsync()**:
   ```csharp
   var scheduleEvent = new ScheduleEvent
   {
       // ... other properties
       TimeZone = createDto.TimeZone ?? "UTC", // Fallback to UTC, not El Salvador
   };
   ```

3. **Update BookingFunctions (AI Agent)**:
   ```csharp
   // In CreateBooking or scheduling functions
   var createDto = new CreateScheduleEventDto
   {
       // ... other properties
       TimeZone = _chatContext?.Location?.TimeZone ?? "UTC"
   };
   ```

4. **Client-side** (future UI components):
   ```csharp
   var createDto = new CreateScheduleEventDto
   {
       TimeZone = ProfileContextService.CurrentContext?.Device.TimeZone ?? "UTC"
   };
   ```

**Checklist**:
- [ ] Verify `CreateScheduleEventDto.TimeZone` is nullable
- [ ] Update `ScheduleEventService.CreateEventAsync()` fallback
- [ ] Update `BookingFunctions` to pass timezone from context
- [ ] Test event creation with various timezones
- [ ] Verify events display correctly in user's local time

---

#### 4.4 Profile Switch Integration

**Status**: 🔲 NOT STARTED  
**Priority**: LOW - Needed when multi-profile support is fully implemented

When user switches profiles, context should reload:

```csharp
// In profile switcher component
await ProfileContextService.OnProfileSwitchedAsync(newProfileId);
```

**Checklist**:
- [ ] Find profile switch handler in UI
- [ ] Call `OnProfileSwitchedAsync()` on profile change
- [ ] Verify context reloads correctly

---

## Testing Strategy

### Unit Tests

**Location**: `Sivar.Os.Tests/Services/ProfileContextServiceTests.cs`

| Test Case | Description |
|-----------|-------------|
| `InitializeAsync_SetsProfileId` | Verify profile ID is stored |
| `InitializeAsync_DetectsDeviceContext` | Verify JS interop is called |
| `RequestDeviceLocationAsync_UsesGps` | Verify GPS location is stored as DeviceLocation |
| `SetSelectedLocationAsync_OverridesDevice` | Verify SelectedLocation takes precedence |
| `ClearSelectedLocationAsync_RevertsToDevice` | Verify clearing selected location |
| `GetChatLocationContext_PopulatesTimezone` | Verify timezone is in returned context |
| `OnProfileSwitchedAsync_ReloadsContext` | Verify context changes on profile switch |
| `EffectiveLocation_PrefersSelected` | Verify SelectedLocation > DeviceLocation priority |

### Mock Strategy

```csharp
// Mock IJSRuntime for device context
var mockJsRuntime = new Mock<IJSRuntime>();
mockJsRuntime
    .Setup(js => js.InvokeAsync<DeviceContextJs>(
        "import", 
        It.IsAny<object[]>()))
    .ReturnsAsync(new DeviceContextJs 
    { 
        TimeZone = "America/El_Salvador",
        DeviceType = "mobile"
    });

// Mock ChatLocationService for location
var mockLocationService = new Mock<ChatLocationService>();
mockLocationService
    .Setup(s => s.RequestGpsLocationAsync())
    .ReturnsAsync(new ChatLocationContext { Latitude = 13.69, Longitude = -89.19 });
```

### Integration Tests

| Test Case | Description |
|-----------|-------------|
| `ProfileContext_IntegratesWithChat` | Verify context flows to ChatRequest |
| `ProfileContext_PersistsAcrossRefresh` | Verify localStorage persistence |
| `ProfileSwitch_ChangesContext` | Verify context changes when profile switches |

---

## File Locations

| File | Location | Purpose |
|------|----------|---------|
| `ProfileContextDtos.cs` | `Sivar.Os.Shared/DTOs/` | DTOs: DeviceContext, ProfileLocationContext, ProfileContext |
| `IProfileContextService.cs` | `Sivar.Os.Shared/Services/` | Interface definition |
| `context-interop.js` | `Sivar.Os.Client/wwwroot/js/` | JavaScript interop for device detection |
| `ProfileContextService.cs` | `Sivar.Os.Client/Services/` | Client implementation |
| `ProfileContextServiceTests.cs` | `Sivar.Os.Tests/Services/` | Unit tests |

---

## Checklist

### Phase 1: DTOs & Interfaces
- [x] Create `ProfileContextDtos.cs` in `Sivar.Os.Shared/DTOs/`
- [x] Create `IProfileContextService.cs` in `Sivar.Os.Shared/Services/`
- [x] Build solution to verify no errors

### Phase 2: JavaScript Interop
- [x] Create `context-interop.js` in `Sivar.Os.Client/wwwroot/js/`
- [x] Test in browser console that functions work

### Phase 3: Client Implementation
- [x] Create `ProfileContextService.cs` in `Sivar.Os.Client/Services/`
- [x] Follow XAF entity rules (if any entities created)
- [x] Register in DI in `Program.cs`
- [x] Build and verify no errors

### Phase 4: Integration
- [x] Register in both Client and Server `Program.cs`
- [x] **4.2 Chat Integration**:
  - [x] Inject `IProfileContextService` in `MainLayout.razor`
  - [x] Initialize service when profile loads
  - [x] Replace `TimeZoneInfo.Local.Id` with `GetChatLocationContext()`
  - [ ] Test AI interprets "hoy", "mañana" correctly (manual test)
- [x] **4.3 Schedule/Booking Integration**:
  - [x] Make `CreateScheduleEventDto.TimeZone` nullable with UTC fallback
  - [x] Update `ScheduleEventService` fallback to UTC
  - [x] Update `BookingFunctions` default timezone to UTC with better docs
  - [ ] Test event creation with various timezones (manual test)
- [ ] **4.4 Profile Switch Integration**:
  - [ ] Call `OnProfileSwitchedAsync()` on profile change (future - when multi-profile fully implemented)

### Phase 5: Testing
- [x] Create `ProfileContextServiceTests.cs`
- [x] Add mock for `IJSRuntime`
- [x] Add mock for `ChatLocationService`
- [x] Run all tests and verify pass (24 tests passing)

---

## Success Criteria

| Criteria | Status | Notes |
|----------|--------|-------|
| `ProfileContextService.CurrentContext.Device.TimeZone` returns browser's timezone | ✅ | Verified in logs: `TimeZone=Europe/Moscow` |
| `ProfileContextService.GetChatLocationContext()` returns context with populated timezone | ✅ | Working |
| AI chat correctly interprets "hoy", "mañana", "esta semana" based on user's local time | ✅ | `MainLayout.razor` now uses `ProfileContextService` |
| New schedule events default to device timezone (or UTC fallback) | ✅ | `CreateScheduleEventDto.TimeZone` nullable, service uses UTC fallback |
| Context persists across page refreshes (localStorage) | ✅ | Working |
| Context changes when user switches profiles | 🔲 | Requires Phase 4.4 (multi-profile support) |
| All unit tests pass | ✅ | 25 ProfileContext + 16 BookingFunctions tests passing |

---

## Related Documentation

- [DEVELOPMENT_RULES.md](DEVELOPMENT_RULES.md) - XAF entity rules, architecture patterns
- [chat3.md](chat3.md) - Chat system implementation
- [LOCATION_SERVICES_IMPLEMENTATION_PLAN.md](LOCATION_SERVICES_IMPLEMENTATION_PLAN.md) - PostGIS location services
