# Sivar.Os Chat System Improvement Plan

## 📋 Overview

A phased approach where each phase delivers a **complete, deployable improvement** to the chat assistant. Phases are ordered by impact and dependency.

---

## Phase 0: Location-Aware Chat (Foundation) ✅ COMPLETED
**Goal**: Make the chat location-aware through auto-detection or user input

### Implementation Summary (Completed)
- **ChatLocationContext DTO** - New record type with lat/lng, city, state, country, display name, source, accuracy
- **ChatLocationService** - Full location management service with:
  - GPS location via BrowserPermissionsService
  - 15 pre-defined Salvadoran cities with coordinates
  - Nominatim reverse geocoding for GPS → city name
  - localStorage persistence (sivar_chat_location key)
  - OnLocationChanged event for UI updates
- **LocationPrompt.razor** - MudDialog component with:
  - GPS "Usar mi ubicación" button with loading state
  - City grid for quick selection (9 cities)
  - Autocomplete for all 15 cities
  - Skip button (defaults to San Salvador)
- **MainLayout.razor integration**:
  - Location indicator button in chat header
  - Auto-initialization on first chat open
  - Location passed to all chat messages
- **ChatFunctionService.SearchPosts** - Enhanced to:
  - Calculate distance using Haversine formula
  - Sort results by proximity when location available
  - Include distanceKm and distanceText in results

### Problem Being Solved
Users searching for "pizzerias cerca" or "banco más cercano" need location context. Currently the system has location infrastructure but doesn't proactively establish user location at the start of conversations.

### Scope
- Auto-detect user location via browser Geolocation API (with permission)
- Prompt user for location if auto-detection fails or is denied
- Allow users to manually set/change search location
- Persist location preference per session
- Show current search location in chat UI
- Location context passed to all search functions

### Location Acquisition Flow
```
┌─────────────────────────────────────────────────────────────┐
│                    Chat Session Start                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
              ┌───────────────────────────────┐
              │  Has saved location preference? │
              └───────────────────────────────┘
                     │                  │
                    Yes                 No
                     │                  │
                     ▼                  ▼
              ┌─────────────┐   ┌─────────────────────┐
              │ Use saved   │   │ Request browser     │
              │ location    │   │ geolocation         │
              └─────────────┘   └─────────────────────┘
                                       │
                          ┌────────────┴────────────┐
                       Granted                   Denied
                          │                         │
                          ▼                         ▼
                   ┌─────────────┐         ┌─────────────────┐
                   │ Auto-detect │         │ Show prompt:    │
                   │ coordinates │         │ "¿Dónde estás?" │
                   └─────────────┘         └─────────────────┘
                          │                         │
                          ▼                         ▼
                   ┌─────────────────────────────────────┐
                   │      Location Context Established    │
                   │  • City/Department shown in UI       │
                   │  • Passed to all search functions    │
                   └─────────────────────────────────────┘
```

### User Location Input Options
1. **Auto-detect** - Browser GPS (most accurate)
2. **City selection** - Dropdown: San Salvador, Santa Ana, San Miguel, etc.
3. **Natural language** - "Estoy en Escalón" or "Buscar en Santa Tecla"
4. **Map pin** - Click on map to set location

### Files to Modify
| File | Changes |
|------|---------|
| `Sivar.Os.Client/Components/AIChat/AIChatPanel.razor` | Add location indicator and change button |
| `Sivar.Os.Client/Components/AIChat/ChatInput.razor` | Detect location keywords in input |
| New: `Sivar.Os.Client/Components/AIChat/LocationPrompt.razor` | Location selection modal/prompt |
| `Sivar.Os.Client/Services/LocationService.cs` | Browser geolocation wrapper (exists, enhance) |
| `Sivar.Os/Services/ChatService.cs` | Accept and propagate location context |
| `Sivar.Os/Services/ChatFunctionService.cs` | Use location in all search functions |

### UI Components

#### Location Indicator (in chat header)
```
┌─────────────────────────────────────────────────────────┐
│ 🤖 Sivar AI Assistant          📍 San Salvador [Cambiar]│
│ Always here to help you explore                         │
└─────────────────────────────────────────────────────────┘
```

#### Location Prompt (when not set)
```
┌─────────────────────────────────────────────────────────┐
│ 📍 ¿Dónde te encuentras?                                │
│                                                          │
│ Para darte mejores resultados, necesito saber tu        │
│ ubicación.                                               │
│                                                          │
│ [📍 Usar mi ubicación]  [🗺️ Elegir en mapa]            │
│                                                          │
│ O selecciona una ciudad:                                │
│ ┌─────────────────────────────────────────────────────┐│
│ │ San Salvador  │ Santa Ana    │ San Miguel          ││
│ │ Santa Tecla   │ Soyapango    │ Mejicanos           ││
│ │ Apopa         │ La Libertad  │ Otra...             ││
│ └─────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────┘
```

#### In-Chat Location Change
User can say:
- "Buscar en Santa Ana" → Changes search context
- "Estoy en el centro" → Updates location
- "Cambiar ubicación" → Shows location prompt

### Acceptance Criteria
- [ ] Chat requests browser location on first message (with permission prompt)
- [ ] If denied, shows friendly location selection prompt
- [ ] Location indicator visible in chat header showing current city
- [ ] "Cambiar" button opens location selection
- [ ] User can type "Buscar en [ciudad]" to change context
- [ ] Location persists for session (localStorage)
- [ ] All search results include distance from user location
- [ ] "Cerca de mí" queries use actual user location
- [ ] Works without location (defaults to San Salvador or asks)

### Deliverable
Chat is location-aware from the start, enabling accurate "near me" searches and distance-based results.

---

## Phase 0.5: Configurable Welcome Messages & Chat Settings ✅ COMPLETED
**Goal**: Make welcome messages and chat bot settings configurable via database instead of hardcoded

### Implementation Summary (Completed)
- **ChatBotSettings Entity** - New entity with Key, Culture, WelcomeMessage, HeaderTagline, BotName, QuickActionsJson, SystemPrompt, IsActive, Priority, RegionCode, ErrorMessage, ThinkingMessage
- **ChatBotSettingsDto** - DTOs for API (read, create, update)
- **IChatBotSettingsRepository / ChatBotSettingsRepository** - Repository pattern with culture/region matching logic
- **ChatBotSettingsConfiguration** - EF Core config: table "Sivar_ChatBotSettings", indexes, soft delete filter
- **ChatBotSettingsController** - REST API endpoints:
  - `GET /api/chat/settings` - Active settings with caching (5 min TTL)
  - `GET /api/chat/settings/{key}` - By key
  - `POST /api/admin/chat/settings` - Create (admin)
  - `PUT /api/admin/chat/settings/{id}` - Update (admin)
  - `DELETE /api/admin/chat/settings/{id}` - Soft delete (admin)
  - `POST /api/admin/chat/settings/clear-cache` - Clear cache (admin)
- **ChatSettingsService** - Client-side service for loading/caching settings
- **ISivarChatClient** - Extended with GetSettingsAsync method
- **MainLayout.razor / Home.razor** - Updated to use ChatSettingsService instead of hardcoded messages
- **Updater.cs** - Seeds default Spanish and English settings on database update

### Problem Being Solved
Currently the welcome message is **hardcoded in 3+ places**:
- `MainLayout.razor` (lines 762, 978)
- `Home.razor` (line 917)
- `wireframe-facebook-reskinned.html` (line 1965)

This makes it impossible to:
- Change messages without code deployment
- A/B test different welcome messages
- Support multiple languages
- Customize per tenant/region
- Configure quick action buttons dynamically

### Scope
- Create new `ChatBotSettings` entity in ORM
- Create `ChatBotSettingsRepository` for CRUD operations
- Add admin API endpoint for managing settings
- Load settings on chat initialization
- Cache settings for performance
- Support localized messages (es, en)

### New Entity: `ChatBotSettings`
```csharp
public class ChatBotSettings : BaseEntity
{
    /// <summary>
    /// Unique key for this setting (e.g., "default", "es-SV", "en-US")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Key { get; set; } = "default";

    /// <summary>
    /// Language/culture code (e.g., "es", "en")
    /// </summary>
    [StringLength(10)]
    public string? Culture { get; set; }

    /// <summary>
    /// The welcome message shown when chat opens
    /// Supports markdown formatting
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string WelcomeMessage { get; set; } = string.Empty;

    /// <summary>
    /// Short tagline shown in chat header
    /// </summary>
    [StringLength(100)]
    public string? HeaderTagline { get; set; }

    /// <summary>
    /// Bot name displayed in header
    /// </summary>
    [StringLength(50)]
    public string BotName { get; set; } = "Sivar AI Assistant";

    /// <summary>
    /// Quick action buttons as JSON array
    /// e.g., ["🍕 Find pizza", "💼 Tech companies", "🎉 Events"]
    /// </summary>
    public string? QuickActionsJson { get; set; }

    /// <summary>
    /// System prompt for the AI agent
    /// </summary>
    [StringLength(5000)]
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Whether this setting is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority for selection (higher = preferred)
    /// </summary>
    public int Priority { get; set; } = 0;
}
```

### Files to Create
| File | Purpose |
|------|---------|
| `Sivar.Os.Shared/Entities/ChatBotSettings.cs` | New entity |
| `Sivar.Os.Shared/Repositories/IChatBotSettingsRepository.cs` | Repository interface |
| `Sivar.Os.Data/Repositories/ChatBotSettingsRepository.cs` | Repository implementation |
| `Sivar.Os.Data/Configurations/ChatBotSettingsConfiguration.cs` | EF Core configuration |
| `Sivar.Os.Shared/DTOs/ChatBotSettingsDto.cs` | DTOs for API |
| `Sivar.Os/Controllers/ChatBotSettingsController.cs` | Admin API endpoints |

### Files to Modify
| File | Changes |
|------|---------|
| `Sivar.Os.Data/Context/SivarDbContext.cs` | Add `DbSet<ChatBotSettings>` |
| `Sivar.Os.Client/Layout/MainLayout.razor` | Load welcome message from service |
| `Sivar.Os.Client/Pages/Home.razor` | Load welcome message from service |
| `Sivar.Os/Services/ChatService.cs` | Load system prompt from settings |
| New: `Sivar.Os.Client/Services/ChatSettingsService.cs` | Client-side settings loader |

### Database Migration
```sql
CREATE TABLE "Sivar_ChatBotSettings" (
    "Id" uuid PRIMARY KEY,
    "Key" varchar(50) NOT NULL UNIQUE,
    "Culture" varchar(10),
    "WelcomeMessage" varchar(2000) NOT NULL,
    "HeaderTagline" varchar(100),
    "BotName" varchar(50) DEFAULT 'Sivar AI Assistant',
    "QuickActionsJson" text,
    "SystemPrompt" varchar(5000),
    "IsActive" boolean DEFAULT true,
    "Priority" integer DEFAULT 0,
    "CreatedAt" timestamptz DEFAULT NOW(),
    "UpdatedAt" timestamptz,
    "IsDeleted" boolean DEFAULT false
);

-- Seed default settings
INSERT INTO "Sivar_ChatBotSettings" ("Id", "Key", "Culture", "WelcomeMessage", "HeaderTagline", "BotName", "QuickActionsJson")
VALUES (
    'a0000001-0001-0001-0001-000000000001',
    'default',
    'es',
    E'¡Hola! Soy tu asistente Sivar AI. Puedo ayudarte a:\n\n🔍 Encontrar negocios y servicios\n📝 Buscar lugares y eventos\n🏪 Descubrir lo mejor de El Salvador\n📋 Guiarte en trámites y papeleos\n\n¡Pregúntame algo como "pizzerías cerca" o "cómo sacar pasaporte"!',
    'Siempre aquí para ayudarte',
    'Sivar AI',
    '["🍕 Buscar comida", "🏛️ Trámites", "📍 Cerca de mí", "🎉 Eventos"]'
);
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/chat/settings` | Get active settings for current culture |
| GET | `/api/chat/settings/{key}` | Get specific settings by key |
| POST | `/api/admin/chat/settings` | Create new settings (admin) |
| PUT | `/api/admin/chat/settings/{id}` | Update settings (admin) |
| DELETE | `/api/admin/chat/settings/{id}` | Soft delete settings (admin) |

### Caching Strategy
```csharp
// Cache settings for 5 minutes
services.AddMemoryCache();

public class ChatBotSettingsService : IChatBotSettingsService
{
    private readonly IMemoryCache _cache;
    private const string CacheKey = "ChatBotSettings_{0}"; // {culture}
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<ChatBotSettingsDto> GetSettingsAsync(string? culture = null)
    {
        var key = string.Format(CacheKey, culture ?? "default");
        
        if (!_cache.TryGetValue(key, out ChatBotSettingsDto? settings))
        {
            settings = await LoadFromDatabaseAsync(culture);
            _cache.Set(key, settings, CacheDuration);
        }
        
        return settings!;
    }
}
```

### Acceptance Criteria
- [ ] `ChatBotSettings` entity exists with all fields
- [ ] Default seed data created on migration
- [ ] Welcome message loads from database, not hardcoded
- [ ] Quick actions configurable via database
- [ ] System prompt configurable via database
- [ ] Settings cached for performance
- [ ] Admin can update settings via API
- [ ] Changes reflect without app restart (cache expiry)
- [ ] Fallback to hardcoded defaults if DB unavailable

### Deliverable
Welcome messages and chat configuration are fully database-driven and can be changed without code deployment.

---

## Phase 1: Enhanced Contact Actions ✅ COMPLETED
**Goal**: Make it easier for users to contact businesses directly from chat results using a flexible, extensible contact type system.

**Status**: ✅ Implemented on 2024-12-12

### Scope
- Create a **Contact Type Catalog** with URL format templates
- Support regional preferences (WhatsApp in LATAM, Telegram in Russia, etc.)
- Add icons and display configuration per contact type
- URL schemes trigger browser/OS native actions (call, message, email, etc.)
- Extensible system for adding new contact types without code changes

### Contact Type Catalog Entity

```csharp
/// <summary>
/// Catalog of contact types with URL templates and display configuration.
/// Stored in database for easy extension without code changes.
/// </summary>
public class ContactType : BaseEntity
{
    /// <summary>
    /// Unique key for this contact type (e.g., "phone", "whatsapp", "telegram")
    /// </summary>
    [Required, StringLength(50)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI (localized)
    /// </summary>
    [Required, StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Icon identifier (emoji, icon name, or icon URL)
    /// </summary>
    [StringLength(50)]
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// MudBlazor icon constant (e.g., "Icons.Material.Filled.Phone")
    /// </summary>
    [StringLength(100)]
    public string? MudBlazorIcon { get; set; }

    /// <summary>
    /// CSS color for the icon/button
    /// </summary>
    [StringLength(20)]
    public string? Color { get; set; }

    /// <summary>
    /// URL template with placeholders: {value}, {country_code}, {message}
    /// Examples:
    /// - Phone: "tel:{country_code}{value}"
    /// - WhatsApp: "https://wa.me/{country_code}{value}?text={message}"
    /// - Telegram: "https://t.me/{value}"
    /// - Email: "mailto:{value}?subject={subject}&body={message}"
    /// </summary>
    [Required, StringLength(500)]
    public string UrlTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Category: "messaging", "social", "phone", "email", "web", "location"
    /// </summary>
    [StringLength(30)]
    public string Category { get; set; } = "other";

    /// <summary>
    /// Sort order within category (lower = first)
    /// </summary>
    public int SortOrder { get; set; } = 100;

    /// <summary>
    /// Is this contact type active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Regional popularity scores (JSON: {"SV": 100, "US": 30, "RU": 5})
    /// Higher = more popular in that region
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? RegionalPopularity { get; set; }

    /// <summary>
    /// Value validation regex (e.g., for phone numbers)
    /// </summary>
    [StringLength(200)]
    public string? ValidationRegex { get; set; }

    /// <summary>
    /// Placeholder text for input (e.g., "+503 7XXX-XXXX")
    /// </summary>
    [StringLength(100)]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Should this open in a new tab/window?
    /// </summary>
    public bool OpenInNewTab { get; set; } = true;

    /// <summary>
    /// Requires mobile device? (e.g., WhatsApp, iMessage)
    /// </summary>
    public bool MobileOnly { get; set; } = false;

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? Metadata { get; set; }
}
```

### Seed Data: Contact Type Catalog

```csharp
public static class ContactTypeCatalog
{
    public static List<ContactType> GetDefaultContactTypes() => new()
    {
        // ============================================
        // PHONE / CALLING
        // ============================================
        new ContactType
        {
            Key = "phone",
            DisplayName = "Llamar",
            Icon = "📞",
            MudBlazorIcon = "Icons.Material.Filled.Phone",
            Color = "#4CAF50",  // Green
            UrlTemplate = "tel:{country_code}{value}",
            Category = "phone",
            SortOrder = 1,
            RegionalPopularity = JsonDocument.Parse(@"{""SV"": 100, ""US"": 100, ""MX"": 100}"),
            ValidationRegex = @"^\+?[\d\s\-\(\)]+$"
        },
        new ContactType
        {
            Key = "sms",
            DisplayName = "SMS",
            Icon = "💬",
            MudBlazorIcon = "Icons.Material.Filled.Sms",
            Color = "#2196F3",  // Blue
            UrlTemplate = "sms:{country_code}{value}?body={message}",
            Category = "phone",
            SortOrder = 2,
            MobileOnly = true
        },

        // ============================================
        // MESSAGING APPS
        // ============================================
        new ContactType
        {
            Key = "whatsapp",
            DisplayName = "WhatsApp",
            Icon = "💬",
            MudBlazorIcon = "Icons.Custom.Brands.WhatsApp",
            Color = "#25D366",  // WhatsApp green
            UrlTemplate = "https://wa.me/{country_code}{value}?text={message}",
            Category = "messaging",
            SortOrder = 1,
            RegionalPopularity = JsonDocument.Parse(@"{""SV"": 100, ""MX"": 95, ""ES"": 90, ""BR"": 95, ""US"": 40, ""RU"": 10}"),
            ValidationRegex = @"^\+?[\d]+$",
            Placeholder = "+503 7XXX-XXXX"
        },
        new ContactType
        {
            Key = "telegram",
            DisplayName = "Telegram",
            Icon = "✈️",
            MudBlazorIcon = "Icons.Custom.Brands.Telegram",
            Color = "#0088CC",  // Telegram blue
            UrlTemplate = "https://t.me/{value}",
            Category = "messaging",
            SortOrder = 2,
            RegionalPopularity = JsonDocument.Parse(@"{""RU"": 100, ""IR"": 90, ""UA"": 85, ""US"": 30, ""SV"": 15}"),
            Placeholder = "@username or phone"
        },
        new ContactType
        {
            Key = "messenger",
            DisplayName = "Messenger",
            Icon = "💬",
            MudBlazorIcon = "Icons.Custom.Brands.Facebook",
            Color = "#0084FF",  // Messenger blue
            UrlTemplate = "https://m.me/{value}",
            Category = "messaging",
            SortOrder = 3,
            RegionalPopularity = JsonDocument.Parse(@"{""US"": 70, ""SV"": 50, ""MX"": 45}"),
            Placeholder = "Facebook Page ID or username"
        },
        new ContactType
        {
            Key = "signal",
            DisplayName = "Signal",
            Icon = "🔒",
            Color = "#3A76F0",  // Signal blue
            UrlTemplate = "https://signal.me/#p/{country_code}{value}",
            Category = "messaging",
            SortOrder = 4,
            RegionalPopularity = JsonDocument.Parse(@"{""US"": 25, ""DE"": 40, ""SV"": 5}")
        },
        new ContactType
        {
            Key = "imessage",
            DisplayName = "iMessage",
            Icon = "💬",
            Color = "#34C759",  // Apple green
            UrlTemplate = "imessage:{value}",
            Category = "messaging",
            SortOrder = 5,
            MobileOnly = true,
            RegionalPopularity = JsonDocument.Parse(@"{""US"": 60, ""SV"": 20}"),
            Metadata = JsonDocument.Parse(@"{""platform"": ""ios""}")
        },

        // ============================================
        // EMAIL
        // ============================================
        new ContactType
        {
            Key = "email",
            DisplayName = "Email",
            Icon = "📧",
            MudBlazorIcon = "Icons.Material.Filled.Email",
            Color = "#EA4335",  // Gmail red
            UrlTemplate = "mailto:{value}?subject={subject}&body={message}",
            Category = "email",
            SortOrder = 1,
            ValidationRegex = @"^[\w\.-]+@[\w\.-]+\.\w+$"
        },

        // ============================================
        // SOCIAL MEDIA
        // ============================================
        new ContactType
        {
            Key = "facebook",
            DisplayName = "Facebook",
            Icon = "📘",
            MudBlazorIcon = "Icons.Custom.Brands.Facebook",
            Color = "#1877F2",
            UrlTemplate = "https://facebook.com/{value}",
            Category = "social",
            SortOrder = 1,
            OpenInNewTab = true
        },
        new ContactType
        {
            Key = "instagram",
            DisplayName = "Instagram",
            Icon = "📷",
            MudBlazorIcon = "Icons.Custom.Brands.Instagram",
            Color = "#E4405F",
            UrlTemplate = "https://instagram.com/{value}",
            Category = "social",
            SortOrder = 2,
            OpenInNewTab = true,
            Placeholder = "@username"
        },
        new ContactType
        {
            Key = "tiktok",
            DisplayName = "TikTok",
            Icon = "🎵",
            MudBlazorIcon = "Icons.Custom.Brands.TikTok",
            Color = "#000000",
            UrlTemplate = "https://tiktok.com/@{value}",
            Category = "social",
            SortOrder = 3,
            OpenInNewTab = true
        },
        new ContactType
        {
            Key = "twitter",
            DisplayName = "X (Twitter)",
            Icon = "🐦",
            MudBlazorIcon = "Icons.Custom.Brands.Twitter",
            Color = "#1DA1F2",
            UrlTemplate = "https://x.com/{value}",
            Category = "social",
            SortOrder = 4,
            OpenInNewTab = true,
            Placeholder = "@username"
        },
        new ContactType
        {
            Key = "linkedin",
            DisplayName = "LinkedIn",
            Icon = "💼",
            MudBlazorIcon = "Icons.Custom.Brands.LinkedIn",
            Color = "#0A66C2",
            UrlTemplate = "https://linkedin.com/in/{value}",
            Category = "social",
            SortOrder = 5,
            OpenInNewTab = true
        },
        new ContactType
        {
            Key = "youtube",
            DisplayName = "YouTube",
            Icon = "▶️",
            MudBlazorIcon = "Icons.Custom.Brands.YouTube",
            Color = "#FF0000",
            UrlTemplate = "https://youtube.com/{value}",
            Category = "social",
            SortOrder = 6,
            OpenInNewTab = true,
            Placeholder = "@channel or /c/channel"
        },

        // ============================================
        // WEB
        // ============================================
        new ContactType
        {
            Key = "website",
            DisplayName = "Sitio Web",
            Icon = "🌐",
            MudBlazorIcon = "Icons.Material.Filled.Language",
            Color = "#607D8B",
            UrlTemplate = "{value}",  // Value is the full URL
            Category = "web",
            SortOrder = 1,
            OpenInNewTab = true,
            ValidationRegex = @"^https?://.*"
        },
        new ContactType
        {
            Key = "contact_form",
            DisplayName = "Formulario",
            Icon = "📝",
            MudBlazorIcon = "Icons.Material.Filled.ContactPage",
            Color = "#9C27B0",
            UrlTemplate = "{value}",
            Category = "web",
            SortOrder = 2,
            OpenInNewTab = true
        },
        new ContactType
        {
            Key = "booking",
            DisplayName = "Reservar",
            Icon = "📅",
            MudBlazorIcon = "Icons.Material.Filled.EventAvailable",
            Color = "#FF9800",
            UrlTemplate = "{value}",
            Category = "web",
            SortOrder = 3,
            OpenInNewTab = true
        },
        new ContactType
        {
            Key = "menu",
            DisplayName = "Menú",
            Icon = "🍽️",
            MudBlazorIcon = "Icons.Material.Filled.MenuBook",
            Color = "#795548",
            UrlTemplate = "{value}",
            Category = "web",
            SortOrder = 4,
            OpenInNewTab = true
        },
        new ContactType
        {
            Key = "order_online",
            DisplayName = "Ordenar",
            Icon = "🛒",
            MudBlazorIcon = "Icons.Material.Filled.ShoppingCart",
            Color = "#4CAF50",
            UrlTemplate = "{value}",
            Category = "web",
            SortOrder = 5,
            OpenInNewTab = true
        },

        // ============================================
        // LOCATION
        // ============================================
        new ContactType
        {
            Key = "google_maps",
            DisplayName = "Google Maps",
            Icon = "📍",
            MudBlazorIcon = "Icons.Material.Filled.Map",
            Color = "#4285F4",
            UrlTemplate = "https://www.google.com/maps/search/?api=1&query={lat},{lng}",
            Category = "location",
            SortOrder = 1,
            OpenInNewTab = true
        },
        new ContactType
        {
            Key = "waze",
            DisplayName = "Waze",
            Icon = "🚗",
            Color = "#33CCFF",
            UrlTemplate = "https://waze.com/ul?ll={lat},{lng}&navigate=yes",
            Category = "location",
            SortOrder = 2,
            OpenInNewTab = true,
            MobileOnly = true
        },
        new ContactType
        {
            Key = "apple_maps",
            DisplayName = "Apple Maps",
            Icon = "🗺️",
            Color = "#000000",
            UrlTemplate = "https://maps.apple.com/?ll={lat},{lng}&q={name}",
            Category = "location",
            SortOrder = 3,
            Metadata = JsonDocument.Parse(@"{""platform"": ""ios""}")
        },
        new ContactType
        {
            Key = "directions",
            DisplayName = "Cómo llegar",
            Icon = "🧭",
            MudBlazorIcon = "Icons.Material.Filled.Directions",
            Color = "#4285F4",
            UrlTemplate = "https://www.google.com/maps/dir/?api=1&destination={lat},{lng}",
            Category = "location",
            SortOrder = 4,
            OpenInNewTab = true
        },

        // ============================================
        // SPECIALIZED / INDUSTRY
        // ============================================
        new ContactType
        {
            Key = "uber_eats",
            DisplayName = "Uber Eats",
            Icon = "🍔",
            Color = "#06C167",
            UrlTemplate = "https://www.ubereats.com/store/{value}",
            Category = "delivery",
            SortOrder = 1,
            OpenInNewTab = true
        },
        new ContactType
        {
            Key = "hugo",
            DisplayName = "Hugo",
            Icon = "🛵",
            Color = "#FF6B00",
            UrlTemplate = "https://hugo.com/sv/store/{value}",
            Category = "delivery",
            SortOrder = 2,
            OpenInNewTab = true,
            RegionalPopularity = JsonDocument.Parse(@"{""SV"": 90, ""GT"": 80, ""HN"": 70}")
        },
        new ContactType
        {
            Key = "pedidosya",
            DisplayName = "PedidosYa",
            Icon = "🍕",
            Color = "#FA0050",
            UrlTemplate = "https://www.pedidosya.com.sv/restaurantes/{value}",
            Category = "delivery",
            SortOrder = 3,
            OpenInNewTab = true,
            RegionalPopularity = JsonDocument.Parse(@"{""SV"": 85, ""AR"": 90, ""UY"": 95}")
        }
    };
}
```

### Business Contact Info Entity

```csharp
/// <summary>
/// Contact information for a business/profile.
/// Stored as a collection, allowing multiple contact methods.
/// </summary>
public class BusinessContactInfo : BaseEntity
{
    /// <summary>
    /// The profile/business this contact belongs to
    /// </summary>
    public Guid ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;

    /// <summary>
    /// Reference to contact type catalog
    /// </summary>
    public Guid ContactTypeId { get; set; }
    public ContactType ContactType { get; set; } = null!;

    /// <summary>
    /// The actual value (phone number, email, username, URL, etc.)
    /// </summary>
    [Required, StringLength(500)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Optional label (e.g., "Ventas", "Soporte", "Personal")
    /// </summary>
    [StringLength(100)]
    public string? Label { get; set; }

    /// <summary>
    /// Country code for phone numbers (e.g., "503" for El Salvador)
    /// </summary>
    [StringLength(10)]
    public string? CountryCode { get; set; }

    /// <summary>
    /// Display order (lower = first)
    /// </summary>
    public int SortOrder { get; set; } = 100;

    /// <summary>
    /// Is this the primary contact of this type?
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Is this contact method active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional: Hours when this contact is available (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? AvailableHours { get; set; }

    /// <summary>
    /// Additional notes (e.g., "Solo WhatsApp, no llamadas")
    /// </summary>
    [StringLength(200)]
    public string? Notes { get; set; }
}
```

### Contact URL Builder Service

```csharp
public interface IContactUrlBuilder
{
    /// <summary>
    /// Build the action URL for a contact
    /// </summary>
    string BuildUrl(ContactType contactType, BusinessContactInfo contact, 
        string? message = null, string? subject = null);
    
    /// <summary>
    /// Get contacts for a profile, sorted by regional popularity
    /// </summary>
    Task<List<ContactDisplayDto>> GetContactsForProfile(
        Guid profileId, string? userRegion = null);
}

public class ContactUrlBuilder : IContactUrlBuilder
{
    private readonly IContactTypeRepository _contactTypeRepo;
    private readonly IBusinessContactInfoRepository _contactInfoRepo;

    public string BuildUrl(ContactType type, BusinessContactInfo contact, 
        string? message = null, string? subject = null)
    {
        var url = type.UrlTemplate;

        // Replace placeholders
        url = url.Replace("{value}", Uri.EscapeDataString(contact.Value));
        url = url.Replace("{country_code}", contact.CountryCode ?? "");
        url = url.Replace("{message}", Uri.EscapeDataString(message ?? ""));
        url = url.Replace("{subject}", Uri.EscapeDataString(subject ?? ""));
        
        // For location-based contacts
        if (contact.Profile?.Latitude != null && contact.Profile?.Longitude != null)
        {
            url = url.Replace("{lat}", contact.Profile.Latitude.Value.ToString());
            url = url.Replace("{lng}", contact.Profile.Longitude.Value.ToString());
            url = url.Replace("{name}", Uri.EscapeDataString(contact.Profile.DisplayName ?? ""));
        }

        return url;
    }

    public async Task<List<ContactDisplayDto>> GetContactsForProfile(
        Guid profileId, string? userRegion = null)
    {
        var contacts = await _contactInfoRepo.GetByProfileAsync(profileId);
        var contactTypes = await _contactTypeRepo.GetAllActiveAsync();

        // Build display list with URLs
        var displayList = contacts
            .Where(c => c.IsActive)
            .Select(c => {
                var type = contactTypes.First(t => t.Id == c.ContactTypeId);
                var popularity = GetRegionalPopularity(type, userRegion ?? "SV");
                
                return new ContactDisplayDto
                {
                    ContactId = c.Id,
                    TypeKey = type.Key,
                    DisplayName = type.DisplayName,
                    Icon = type.Icon,
                    MudBlazorIcon = type.MudBlazorIcon,
                    Color = type.Color,
                    Category = type.Category,
                    Value = c.Value,
                    Label = c.Label,
                    Url = BuildUrl(type, c),
                    OpenInNewTab = type.OpenInNewTab,
                    MobileOnly = type.MobileOnly,
                    RegionalPopularity = popularity,
                    SortOrder = c.SortOrder
                };
            })
            // Sort by: category, then regional popularity, then sort order
            .OrderBy(c => GetCategorySortOrder(c.Category))
            .ThenByDescending(c => c.RegionalPopularity)
            .ThenBy(c => c.SortOrder)
            .ToList();

        return displayList;
    }

    private int GetRegionalPopularity(ContactType type, string region)
    {
        if (type.RegionalPopularity == null) return 50;
        
        try
        {
            var popularity = type.RegionalPopularity.RootElement;
            if (popularity.TryGetProperty(region, out var value))
                return value.GetInt32();
            return 50; // Default if region not found
        }
        catch
        {
            return 50;
        }
    }

    private int GetCategorySortOrder(string category) => category switch
    {
        "phone" => 1,
        "messaging" => 2,
        "email" => 3,
        "web" => 4,
        "social" => 5,
        "location" => 6,
        "delivery" => 7,
        _ => 99
    };
}
```

### Contact Display DTO

```csharp
public class ContactDisplayDto
{
    public Guid ContactId { get; set; }
    public string TypeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string? MudBlazorIcon { get; set; }
    public string? Color { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool OpenInNewTab { get; set; }
    public bool MobileOnly { get; set; }
    public int RegionalPopularity { get; set; }
    public int SortOrder { get; set; }
}
```

### UI Component: Contact Actions

```razor
@* ContactActions.razor - Reusable contact buttons component *@

@foreach (var contactGroup in Contacts.GroupBy(c => c.Category).OrderBy(g => GetCategoryOrder(g.Key)))
{
    <div class="contact-category mb-2">
        @foreach (var contact in contactGroup.Take(MaxPerCategory))
        {
            <MudTooltip Text="@GetTooltip(contact)">
                <MudButton Href="@contact.Url"
                           Target="@(contact.OpenInNewTab ? "_blank" : "_self")"
                           Variant="Variant.Outlined"
                           Size="Size.Small"
                           StartIcon="@GetIcon(contact)"
                           Style="@($"border-color: {contact.Color}; color: {contact.Color};")"
                           Class="mr-1 mb-1">
                    @if (!string.IsNullOrEmpty(contact.Label))
                    {
                        <span>@contact.Label</span>
                    }
                    else
                    {
                        <span>@contact.DisplayName</span>
                    }
                </MudButton>
            </MudTooltip>
        }
    </div>
}

@code {
    [Parameter] public List<ContactDisplayDto> Contacts { get; set; } = new();
    [Parameter] public int MaxPerCategory { get; set; } = 3;

    private string GetIcon(ContactDisplayDto contact)
    {
        // Prefer MudBlazor icon, fallback to emoji
        return contact.MudBlazorIcon ?? contact.Icon;
    }

    private string GetTooltip(ContactDisplayDto contact)
    {
        var tooltip = contact.DisplayName;
        if (!string.IsNullOrEmpty(contact.Value))
            tooltip += $": {contact.Value}";
        if (contact.MobileOnly)
            tooltip += " (móvil)";
        return tooltip;
    }
}
```

### Card with Contact Actions

```
┌─────────────────────────────────────────────────────────────────────┐
│  🍕 Pizza Hut Centro                            ⭐ 4.5 (234 reviews)│
│  📍 1.2 km • 🕐 Abierto ahora                                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Contactar:                                                          │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ [📞 Llamar] [💬 WhatsApp] [📧 Email]                          │ │
│  │ [📍 Cómo llegar] [🌐 Sitio Web] [📝 Menú]                     │ │
│  │ [📷 Instagram] [📘 Facebook]                                   │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  Delivery:                                                           │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ [🛵 Hugo] [🍕 PedidosYa] [🍔 Uber Eats]                       │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Files to Create
| File | Purpose |
|------|---------|
| `Sivar.Os.Shared/Entities/ContactType.cs` | Contact type catalog entity |
| `Sivar.Os.Shared/Entities/BusinessContactInfo.cs` | Business contact info entity |
| `Sivar.Os.Shared/Repositories/IContactTypeRepository.cs` | Repository interface |
| `Sivar.Os.Data/Repositories/ContactTypeRepository.cs` | Repository implementation |
| `Sivar.Os/Services/ContactUrlBuilder.cs` | URL building service |
| `Sivar.Os.Shared/DTOs/ContactDisplayDto.cs` | Contact display DTO |
| `Sivar.Os.Client/Components/ContactActions.razor` | Reusable contact buttons |
| `Sivar.Os.DataSeeder/ContactTypeCatalogSeeder.cs` | Seed default contact types |

### Files to Modify
| File | Changes |
|------|---------|
| `Sivar.Os.Data/Context/SivarDbContext.cs` | Add `DbSet<ContactType>`, `DbSet<BusinessContactInfo>` |
| `Sivar.Os.Shared/DTOs/SearchResultDtos.cs` | Add `Contacts: List<ContactDisplayDto>` to result DTOs |
| `Sivar.Os/Services/SearchResultService.cs` | Load contacts using `IContactUrlBuilder` |
| `Sivar.Os.Client/Components/AIChat/ChatMessage.razor` | Use `<ContactActions>` component |

### URL Scheme Examples

| Contact Type | URL Template | Example Output |
|--------------|--------------|----------------|
| Phone | `tel:{country_code}{value}` | `tel:+50378887777` |
| SMS | `sms:{country_code}{value}?body={message}` | `sms:+50378887777?body=Hola` |
| WhatsApp | `https://wa.me/{country_code}{value}?text={message}` | `https://wa.me/50378887777?text=Hola` |
| Telegram | `https://t.me/{value}` | `https://t.me/username` |
| Email | `mailto:{value}?subject={subject}&body={message}` | `mailto:info@pizza.com?subject=Consulta` |
| Google Maps | `https://www.google.com/maps/search/?api=1&query={lat},{lng}` | `https://www.google.com/maps/search/?api=1&query=13.69,-89.19` |
| Waze | `https://waze.com/ul?ll={lat},{lng}&navigate=yes` | `https://waze.com/ul?ll=13.69,-89.19&navigate=yes` |
| iMessage | `imessage:{value}` | `imessage:+50378887777` |

### Acceptance Criteria
- [x] ContactType catalog entity created with URL templates
- [x] BusinessContactInfo entity links profiles to contact methods
- [x] Default contact types seeded (phone, WhatsApp, Telegram, email, social, etc.)
- [x] Regional popularity affects sort order (WhatsApp first in SV, Telegram first in RU)
- [x] URL builder generates correct URLs for all contact types
- [x] ContactActions component renders grouped buttons
- [x] Clicking buttons triggers browser/OS native actions
- [x] New contact types can be added via database (no code changes)
- [x] Mobile-only contacts indicated in UI
- [x] Icons and colors display correctly

### Deliverable
Extensible contact system with URL scheme support for triggering native browser/OS actions, adaptable to regional preferences.

### Implementation Summary (2024-12-12)
**Files Created:**
- `Sivar.Os.Shared/Entities/ContactType.cs` - Contact type catalog with URL templates, icons, regional popularity
- `Sivar.Os.Shared/Entities/BusinessContactInfo.cs` - Links profiles to contact methods
- `Sivar.Os.Shared/DTOs/ContactDisplayDto.cs` - DTOs for UI rendering
- `Sivar.Os.Shared/Repositories/IContactTypeRepository.cs` - Repository interface
- `Sivar.Os.Shared/Repositories/IBusinessContactInfoRepository.cs` - Repository interface
- `Sivar.Os.Shared/Services/IContactUrlBuilder.cs` - Service interface
- `Sivar.Os.Data/Repositories/ContactTypeRepository.cs` - Repository implementation
- `Sivar.Os.Data/Repositories/BusinessContactInfoRepository.cs` - Repository implementation
- `Sivar.Os.Data/Configurations/ContactTypeConfiguration.cs` - EF Core configuration
- `Sivar.Os.Data/Configurations/BusinessContactInfoConfiguration.cs` - EF Core configuration
- `Sivar.Os/Services/ContactUrlBuilder.cs` - URL builder service
- `Sivar.Os.Client/Components/Shared/ContactActions.razor` - Blazor component for contact buttons
- `Sivar.Os.DataSeeder/Services/ContactTypeCatalogSeeder.cs` - Seeder with 30+ contact types

**Files Modified:**
- `Sivar.Os.Data/Context/SivarDbContext.cs` - Added DbSets and configurations
- `Sivar.Os.Shared/DTOs/SearchResultDtos.cs` - Added Contacts property to DTOs
- `Sivar.Os.Client/Components/AIChat/ChatMessage.razor` - Integrated ContactActions component
- `Sivar.Os/Program.cs` - Registered repositories and services

---

## Phase 2: Unified Structured Search Pipeline ✅ COMPLETE
**Goal**: Ensure ALL search queries return rich card results, not just text

### Problem Being Solved
Currently two paths:
1. `AIAgent` → `ChatFunctionService` → **text response** ❌
2. `IsSearchQuery()` → `SearchResultService` → **structured cards** ✅

### Solution Implemented
Instead of modifying function return types (which would break the AI Agent function calling pattern), we:
1. Added `LastSearchResults` property to `ChatFunctionService` to capture structured DTOs
2. Each search function now populates `LastSearchResults` with `SearchResultsCollectionDto`
3. `ChatService` clears results before AI call, then retrieves them after
4. Primary: Use function call results, Fallback: Use `IsSearchQuery()` with hybrid search

### Files Modified
| File | Changes |
|------|---------|
| `Sivar.Os/Services/ChatFunctionService.cs` | Added `LastSearchResults`, `ClearLastSearchResults()`, mapping helpers for Profile/Post entities and DTOs |
| `Sivar.Os/Services/ChatService.cs` | Clear results before AI call, retrieve after, use as primary source |

### Acceptance Criteria
- [x] `FindBusinesses()` populates `LastSearchResults` with `SearchResultsCollectionDto`
- [x] `SearchPosts()` populates structured results
- [x] `SearchProfiles()` populates structured results  
- [x] `SearchNearbyProfiles()` populates structured results
- [x] `SearchNearbyPosts()` populates structured results
- [x] Function call results appear in `ChatResponseDto.SearchResults`
- [x] `IsSearchQuery()` fallback maintained for non-function-call searches

### Deliverable
Consistent card-based UI for ALL search queries regardless of how they're processed.

---

## Phase 3: Interactive Procedure Cards 🟡
**Goal**: Make government procedure/paperwork guidance actionable and trackable

### Scope
- Expandable procedure cards with full details
- Interactive requirements checklist
- Step-by-step process display
- Direct links to online services
- Office hours and location prominent

### Files to Modify
| File | Changes |
|------|---------|
| `Sivar.Os.Shared/DTOs/SearchResultDtos.cs` | Add `Steps[]`, `Documents[]` to `ProcedureSearchResultDto` |
| `Sivar.Os.Client/Components/AIChat/ChatMessage.razor` | New expandable procedure card template |
| `Sivar.Os.Client/Components/AIChat/ChatMessage.razor.css` | Styles for procedure cards |

### New UI Component
```
┌─────────────────────────────────────────────┐
│ 📋 Cómo sacar pasaporte                     │
│ ⏱️ 2-3 semanas  │  💰 $25 USD              │
├─────────────────────────────────────────────┤
│ 📍 Centro de Gobierno, San Salvador         │
│ 🕐 Lun-Vie 8:00am - 4:00pm                 │
├─────────────────────────────────────────────┤
│ ▼ Ver requisitos (5 documentos)            │
│ ┌─────────────────────────────────────────┐│
│ │ ☐ DUI vigente                          ││
│ │ ☐ Partida de nacimiento reciente       ││
│ │ ☐ Constancia de residencia             ││
│ │ ☐ 2 fotos tamaño pasaporte             ││
│ │ ☐ Comprobante de pago                  ││
│ └─────────────────────────────────────────┘│
├─────────────────────────────────────────────┤
│ [🌐 Cita en línea] [📞 Llamar] [📍 Mapa]  │
└─────────────────────────────────────────────┘
```

### Acceptance Criteria
- [ ] Procedure cards show summary (time, cost) at a glance
- [ ] Requirements displayed as interactive checklist
- [ ] Users can check off requirements locally
- [ ] Office hours clearly visible
- [ ] "Iniciar trámite en línea" button when available
- [ ] Map button shows office location

### Deliverable
Users can prepare for government procedures with a checklist and direct access to online services.

---

## Phase 4: Smart Follow-up Suggestions ✅ COMPLETED
**Goal**: Guide users to refine their search or take next steps

### Implementation Summary (2024-12-14)
- **SuggestedActionDto** - DTO with Label, Query, Icon, Type (Refinement/Filter/Location/Alternative)
- **SearchResultsCollectionDto.SuggestedActions** - Property holds contextual suggestions
- **ChatFunctionService.GenerateSuggestions()** - Generates 2-4 suggestions based on result types:
  - Business results: "🗺️ Ver en mapa", "🕐 Solo abiertos ahora", "📍 Los más cercanos"
  - Procedure results: "📋 Ver todos los requisitos"
  - Event results: "📅 Esta semana"
  - No results: "🔄 Buscar en toda la ciudad", "💡 Mostrar sugerencias similares"
- **ChatMessage.razor** - Renders suggestion chips with color-coded styling
- **ChatMessage.razor.css** - CSS for `.suggestion-chips`, `.chip-refinement`, `.chip-filter`, `.chip-location`, `.chip-alternative`
- **ChatMessages.razor** - Passes `OnSuggestionClick` callback through
- **MainLayout.razor** - `HandleSuggestionClick()` sends pre-filled query

### Scope
- Generate contextual suggestions after results ✅
- Quick action chips below results ✅
- Pre-filled queries for common refinements ✅

### Files Modified
| File | Changes |
|------|---------|
| `Sivar.Os.Shared/DTOs/SearchResultDtos.cs` | `SuggestedActionDto`, `SuggestedActionType` enum, `SuggestedActions` property |
| `Sivar.Os/Services/ChatFunctionService.cs` | `GenerateSuggestions()` method, `CreateSearchResultsCollection()` includes suggestions |
| `Sivar.Os.Client/Components/AIChat/ChatMessage.razor` | Renders suggestion chips with `GetSuggestionChipClass()` |
| `Sivar.Os.Client/Components/AIChat/ChatMessage.razor.css` | Styled chip variants with gradients |
| `Sivar.Os.Client/Components/AIChat/ChatMessages.razor` | Passes `OnSuggestionClick` callback |
| `Sivar.Os.Client/Layout/MainLayout.razor` | `HandleSuggestionClick()` handler |

### Suggestion Types
| Context | Suggestions |
|---------|-------------|
| Business results | "🗺️ Ver en mapa", "🕐 Solo abiertos ahora", "📍 Los más cercanos" |
| Procedure results | "📋 Ver todos los requisitos" |
| Event results | "📅 Esta semana" |
| No results | "🔄 Buscar en toda la ciudad", "💡 Mostrar sugerencias similares" |

### Acceptance Criteria
- [x] 2-4 relevant suggestions appear after results
- [x] Clicking suggestion sends pre-filled query
- [x] Suggestions adapt to result type
- [x] "No results" has helpful alternatives

### Deliverable
Users can quickly refine searches without typing new queries. ✅

---

## Phase 5: Real-time Business Status ✅ COMPLETED
**Goal**: Show users if a business is open right now

### Implementation Summary (2024-12-14)
- **WorkingHoursHelper.cs** - Already existed with full timezone-aware logic (El Salvador UTC-6)
- **SearchResultDtos.cs** - Already had `IsOpenNow`, `ClosingTime`, `NextOpenTime`, `OpenStatusText` properties
- **SearchResultService.cs** - Already calculating open status for hybrid search results
- **ChatFunctionService.cs** - **UPDATED** to calculate open status for AI agent function call results:
  - `MapPostToBusinessResult()` - Now parses BusinessMetadata and calculates open status
  - `MapPostDtoToBusinessResult()` - Now parses BusinessMetadata and calculates open status
- **ChatMessage.razor** - Already had UI badge with 🟢/🔴 indicators
- **ChatMessage.razor.css** - Already had gradient styling for open/closed badges

### Scope
- Parse working hours into structured format ✅
- Calculate "Open Now" / "Closed" status ✅
- Show next opening time when closed ✅
- Visual indicators on cards ✅

### Files Modified
| File | Changes |
|------|---------|
| `Sivar.Os.Shared/DTOs/SearchResultDtos.cs` | Already had `IsOpenNow`, `NextOpenTime`, `OpenStatusText` |
| `Sivar.Os/Services/SearchResultService.cs` | Already calculated open status |
| `Sivar.Os/Services/ChatFunctionService.cs` | **Added** open status calculation to mapping functions |
| `Sivar.Os/Helpers/WorkingHoursHelper.cs` | Already existed with El Salvador timezone logic |
| `Sivar.Os.Client/Components/AIChat/ChatMessage.razor` | Already had 🟢/🔴 badge UI |

### Visual Design
```
┌─────────────────────────────────────────┐
│ 🍕 Pizza Hut                            │
│ 🟢 Abierto ahora · Cierra a las 10pm   │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ 🏦 Banco Agrícola                       │
│ 🔴 Cerrado · Abre mañana 8:00am        │
└─────────────────────────────────────────┘
```

### Acceptance Criteria
- [x] Cards show 🟢/🔴 open status badge
- [x] Closing time shown when open
- [x] Next opening time shown when closed
- [ ] Filter "Solo abiertos ahora" works (future enhancement)
- [x] Timezone handled correctly (El Salvador = UTC-6)

### Deliverable
Users immediately know if a business is available right now.

---

## Phase 6: Intent-Based Routing 🟠
**Goal**: Better understand what users want and route to the right handler

### Scope
- Classify user intent before processing
- Route to specialized handlers
- Improve accuracy for specific queries

### Intent Categories
| Intent | Example | Handler |
|--------|---------|---------|
| `BusinessSearch` | "pizzerías cerca" | `SearchResultService` |
| `ProcedureHelp` | "cómo sacar DUI" | Procedure cards + steps |
| `ContactLookup` | "teléfono del BAC" | Single result with contact focus |
| `DirectionsRequest` | "cómo llego a X" | Map integration |
| `HoursQuery` | "horario de Y" | Hours card |
| `GeneralQuestion` | "qué es un DUI" | LLM response |

### Files to Modify
| File | Changes |
|------|---------|
| New: `Sivar.Os/Services/IntentClassifier.cs` | Intent classification logic |
| `Sivar.Os/Services/ChatService.cs` | Route based on intent |
| `Sivar.Os/Services/ChatFunctionService.cs` | Intent-specific functions |

### Acceptance Criteria
- [ ] "Teléfono de X" returns single result with phone prominent
- [ ] "Horario de X" returns hours card
- [ ] "Cómo llego a X" shows map/directions
- [ ] General questions get LLM-generated answers
- [ ] Intent logged for analytics

### Deliverable
More accurate, targeted responses based on what users actually need.

---

## Phase 7: Map View Integration 🟠
**Goal**: Show all results on an interactive map

### Scope
- "Ver en mapa" button for result sets
- Leaflet.js map modal/panel
- Markers for each result
- Click marker to see card

### Files to Modify
| File | Changes |
|------|---------|
| New: `Sivar.Os.Client/Components/AIChat/ChatMapView.razor` | Map component |
| `Sivar.Os.Client/Components/AIChat/ChatMessage.razor` | "Ver en mapa" button |
| Reference: `LEAFLET_INTEGRATION_GUIDE.md` | Existing docs |

### Acceptance Criteria
- [ ] "Ver en mapa" opens map with all results
- [ ] Each result has a marker
- [ ] Clicking marker shows mini-card
- [ ] User location shown if available
- [ ] Distance lines from user to results

### Deliverable
Visual geographic view of all search results.

---

## Phase 8: Saved Results & Favorites 🟢
**Goal**: Let users save results for later

### Scope
- "Guardar" button on cards
- Saved results page/panel
- Organize saved items by category
- Share saved collections

### Files to Modify
| File | Changes |
|------|---------|
| `Sivar.Os.Client/Components/AIChat/ChatResultCard.razor` | "Save" button handler |
| `Sivar.Os/Controllers/ChatMessagesController.cs` | Save/unsave endpoints |
| New: `Sivar.Os.Client/Components/AIChat/SavedResultsPanel.razor` | View saved items |

### Acceptance Criteria
- [ ] "📌 Guardar" button on every result card
- [ ] Saved items persist to database
- [ ] "Mis guardados" accessible from chat
- [ ] Can unsave items
- [ ] Saved items show last updated date

### Deliverable
Users can bookmark businesses and procedures for future reference.

---

## Phase 9: Chat Analytics & Metrics Dashboard 🟠
**Goal**: Track chat session effectiveness, search quality, and user engagement

### Problem Being Solved
Currently no visibility into:
- How long users spend in chat sessions
- Which searches are successful vs. unsuccessful
- What users are searching for most
- Conversion rates (search → action like call/save)
- Session abandonment rates
- AI response quality metrics

### ⚠️ Architecture Decision: Separate Analytics Database

**Analytics data MUST be stored in a separate database** from the main application data.

#### Rationale
| Concern | Solution |
|---------|----------|
| **Data Isolation** | User PII stays in main DB, analytics has only anonymized/aggregated data |
| **Performance** | Heavy analytics queries don't impact main app performance |
| **Scaling** | Analytics DB can scale independently (TimescaleDB, ClickHouse) |
| **Retention** | Different retention policies (analytics: 2 years, user data: indefinite) |
| **Compliance** | Easier GDPR/privacy audits with clear separation |
| **Backup/Restore** | Can restore main DB without analytics and vice versa |

#### TimescaleDB Benefits
Since we already have TimescaleDB in our stack, we leverage its time-series superpowers:

| Feature | Benefit |
|---------|---------|
| **Hypertables** | Auto-partitions data by time (weekly chunks) - no manual table partitioning |
| **Continuous Aggregates** | Real-time materialized views for hourly/daily rollups - fast dashboards |
| **Compression** | 90%+ compression on old data - chunks older than 30 days auto-compress |
| **Chunk Management** | Drop old chunks instantly (vs slow DELETE) - `drop_chunks()` |
| **Parallel Queries** | Automatic parallel scans across chunks - fast aggregations |
| **Native PostgreSQL** | Full SQL support, works with EF Core, no special drivers needed |

#### Database Configuration
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=sivaros;...",
    "AnalyticsConnection": "Host=localhost;Database=sivaros_analytics;..."
  }
}
```

#### Separate DbContext
```csharp
// New: AnalyticsDbContext.cs
public class AnalyticsDbContext : DbContext
{
    public DbSet<ChatSessionMetrics> SessionMetrics { get; set; }
    public DbSet<ChatSearchMetrics> SearchMetrics { get; set; }
    public DbSet<ChatActionMetrics> ActionMetrics { get; set; }

    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) 
        : base(options) { }
}

// Program.cs registration
builder.Services.AddDbContext<AnalyticsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AnalyticsConnection")));
```

#### Data Flow
```
┌─────────────────────────────────────────────────────────────────┐
│                        Main Application                          │
│  ┌─────────────────┐    ┌─────────────────┐                     │
│  │  ChatService    │───►│ Analytics       │                     │
│  │  (User actions) │    │ EventPublisher  │                     │
│  └─────────────────┘    └────────┬────────┘                     │
└──────────────────────────────────┼──────────────────────────────┘
                                   │ (async, fire-and-forget)
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Analytics Service                             │
│  ┌─────────────────┐    ┌─────────────────┐                     │
│  │ Event Consumer  │───►│ AnalyticsDb     │                     │
│  │ (Background)    │    │ Context         │                     │
│  └─────────────────┘    └─────────────────┘                     │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│              sivaros_analytics Database                          │
│  • ChatSessionMetrics                                            │
│  • ChatSearchMetrics                                             │
│  • ChatActionMetrics                                             │
│  • No user PII - only ProfileId references                       │
└─────────────────────────────────────────────────────────────────┘
```

#### Privacy-First Design
- **No PII in analytics DB**: Only GUIDs reference users, no names/emails
- **Anonymized queries**: Store normalized queries, not raw user input
- **Aggregation focus**: Dashboard shows aggregates, not individual sessions

### Scope
- Track session-level metrics (duration, messages, actions)
- Track search-level metrics (query, results, clicks)
- Track action-level metrics (calls, saves, map views)
- Real-time analytics dashboard
- Export reports

### New Entity: `ChatSessionMetrics`
```csharp
/// <summary>
/// Tracks analytics for a single chat session
/// </summary>
public class ChatSessionMetrics : BaseEntity
{
    /// <summary>
    /// The conversation being tracked
    /// </summary>
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    /// <summary>
    /// Profile who owns the session
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// When the session started (first message or chat open)
    /// </summary>
    public DateTime SessionStart { get; set; }

    /// <summary>
    /// When the session ended (chat closed or timeout)
    /// </summary>
    public DateTime? SessionEnd { get; set; }

    /// <summary>
    /// Total session duration in seconds
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Total messages sent by user
    /// </summary>
    public int UserMessageCount { get; set; }

    /// <summary>
    /// Total messages from AI
    /// </summary>
    public int AssistantMessageCount { get; set; }

    /// <summary>
    /// Number of search queries performed
    /// </summary>
    public int SearchCount { get; set; }

    /// <summary>
    /// Number of searches that returned results
    /// </summary>
    public int SuccessfulSearchCount { get; set; }

    /// <summary>
    /// Number of result cards clicked/viewed
    /// </summary>
    public int ResultClickCount { get; set; }

    /// <summary>
    /// Number of actions taken (call, email, save, share)
    /// </summary>
    public int ActionsTaken { get; set; }

    /// <summary>
    /// User's location city (for geographic analytics)
    /// </summary>
    [StringLength(100)]
    public string? UserCity { get; set; }

    /// <summary>
    /// Device type: "desktop", "mobile", "tablet"
    /// </summary>
    [StringLength(20)]
    public string? DeviceType { get; set; }

    /// <summary>
    /// Whether user provided location
    /// </summary>
    public bool LocationProvided { get; set; }
}
```

### New Entity: `ChatSearchMetrics`
```csharp
/// <summary>
/// Tracks analytics for individual search queries
/// </summary>
public class ChatSearchMetrics : BaseEntity
{
    /// <summary>
    /// The session this search belongs to
    /// </summary>
    public Guid SessionMetricsId { get; set; }
    public ChatSessionMetrics SessionMetrics { get; set; } = null!;

    /// <summary>
    /// The original user query
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Normalized/cleaned query for aggregation
    /// </summary>
    [StringLength(200)]
    public string? NormalizedQuery { get; set; }

    /// <summary>
    /// Detected intent category
    /// </summary>
    [StringLength(50)]
    public string? IntentCategory { get; set; }

    /// <summary>
    /// Number of results returned
    /// </summary>
    public int ResultCount { get; set; }

    /// <summary>
    /// Search execution time in milliseconds
    /// </summary>
    public int ExecutionTimeMs { get; set; }

    /// <summary>
    /// Whether the search returned results
    /// </summary>
    public bool HasResults { get; set; }

    /// <summary>
    /// Number of results the user clicked
    /// </summary>
    public int ClickedResultCount { get; set; }

    /// <summary>
    /// Position of first clicked result (1-based)
    /// </summary>
    public int? FirstClickPosition { get; set; }

    /// <summary>
    /// User satisfaction signal: explicit feedback or inferred
    /// </summary>
    public SearchSatisfaction? Satisfaction { get; set; }

    /// <summary>
    /// Time until first click in milliseconds
    /// </summary>
    public int? TimeToFirstClickMs { get; set; }

    /// <summary>
    /// Was this a "near me" or location-based search?
    /// </summary>
    public bool IsLocationBased { get; set; }
}

public enum SearchSatisfaction
{
    Unknown = 0,
    ClickedResult = 1,      // User clicked a result
    TookAction = 2,         // User called/saved/shared
    Reformulated = 3,       // User searched again (may indicate dissatisfaction)
    Abandoned = 4,          // User left without interaction
    ExplicitPositive = 5,   // User gave thumbs up
    ExplicitNegative = 6    // User gave thumbs down
}
```

### New Entity: `ChatActionMetrics`
```csharp
/// <summary>
/// Tracks individual actions taken on search results
/// </summary>
public class ChatActionMetrics : BaseEntity
{
    /// <summary>
    /// The search that produced this result
    /// </summary>
    public Guid SearchMetricsId { get; set; }
    public ChatSearchMetrics SearchMetrics { get; set; } = null!;

    /// <summary>
    /// Type of action taken
    /// </summary>
    public ChatActionType ActionType { get; set; }

    /// <summary>
    /// The search result that was acted upon
    /// </summary>
    public Guid? SearchResultId { get; set; }

    /// <summary>
    /// Position of the result in the list (1-based)
    /// </summary>
    public int ResultPosition { get; set; }

    /// <summary>
    /// Time from search to action in milliseconds
    /// </summary>
    public int TimeToActionMs { get; set; }

    /// <summary>
    /// Additional context (phone number called, URL visited, etc.)
    /// </summary>
    [StringLength(500)]
    public string? ActionContext { get; set; }
}

public enum ChatActionType
{
    ViewProfile = 1,
    Call = 2,
    WhatsApp = 3,
    Email = 4,
    Website = 5,
    Map = 6,
    Save = 7,
    Share = 8,
    Follow = 9,
    Directions = 10
}
```

### Files to Create
| File | Purpose |
|------|---------|
| `Sivar.Os.Analytics/` | **New project** for analytics (separate from main app) |
| `Sivar.Os.Analytics/Context/AnalyticsDbContext.cs` | Separate DbContext for analytics DB |
| `Sivar.Os.Analytics/Entities/ChatSessionMetrics.cs` | Session entity |
| `Sivar.Os.Analytics/Entities/ChatSearchMetrics.cs` | Search entity |
| `Sivar.Os.Analytics/Entities/ChatActionMetrics.cs` | Action entity |
| `Sivar.Os.Analytics/Repositories/ChatAnalyticsRepository.cs` | Repository implementation |
| `Sivar.Os.Analytics/Services/AnalyticsEventConsumer.cs` | Background service to process events |
| `Sivar.Os.Shared/DTOs/ChatAnalyticsDtos.cs` | DTOs for dashboards |
| `Sivar.Os.Shared/Events/ChatAnalyticsEvents.cs` | Event classes for pub/sub |
| `Sivar.Os/Services/AnalyticsEventPublisher.cs` | Publishes events to analytics (async) |
| `Sivar.Os/Controllers/ChatAnalyticsController.cs` | Admin API |
| `Sivar.Os.Client/Pages/Admin/ChatAnalytics.razor` | Dashboard page |

### Files to Modify
| File | Changes |
|------|---------|
| `Sivar.Os.sln` | Add new `Sivar.Os.Analytics` project |
| `appsettings.json` | Add `AnalyticsConnection` connection string |
| `Program.cs` | Register `AnalyticsDbContext` with separate connection |
| `Sivar.Os/Services/ChatService.cs` | Publish analytics events (fire-and-forget) |
| `Sivar.Os.Client/Components/AIChat/ChatMessage.razor` | Track result clicks via event |
| `Sivar.Os.Client/Layout/MainLayout.razor` | Track session start/end via event |

### Database Setup (Separate Database)
```sql
-- Create separate analytics database
CREATE DATABASE sivaros_analytics;

-- Connect to analytics database
\c sivaros_analytics

-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- ============================================
-- TIMESCALEDB HYPERTABLES FOR TIME-SERIES DATA
-- ============================================

-- Session-level metrics (NO foreign keys to main DB)
CREATE TABLE "ChatSessionMetrics" (
    "Id" uuid NOT NULL,
    "ConversationId" uuid NOT NULL,  -- Reference only, no FK
    "ProfileId" uuid NOT NULL,        -- Reference only, no FK
    "SessionStart" timestamptz NOT NULL,
    "SessionEnd" timestamptz,
    "DurationSeconds" integer,
    "UserMessageCount" integer DEFAULT 0,
    "AssistantMessageCount" integer DEFAULT 0,
    "SearchCount" integer DEFAULT 0,
    "SuccessfulSearchCount" integer DEFAULT 0,
    "ResultClickCount" integer DEFAULT 0,
    "ActionsTaken" integer DEFAULT 0,
    "UserCity" varchar(100),
    "DeviceType" varchar(20),
    "LocationProvided" boolean DEFAULT false,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    PRIMARY KEY ("Id", "CreatedAt")  -- Composite key for hypertable
);

-- Convert to TimescaleDB hypertable (auto-partitioned by time)
SELECT create_hypertable('"ChatSessionMetrics"', 'CreatedAt', chunk_time_interval => INTERVAL '1 week');

-- Search-level metrics
CREATE TABLE "ChatSearchMetrics" (
    "Id" uuid NOT NULL,
    "SessionMetricsId" uuid NOT NULL,
    "Query" varchar(500) NOT NULL,
    "NormalizedQuery" varchar(200),
    "IntentCategory" varchar(50),
    "ResultCount" integer DEFAULT 0,
    "ExecutionTimeMs" integer,
    "HasResults" boolean DEFAULT false,
    "ClickedResultCount" integer DEFAULT 0,
    "FirstClickPosition" integer,
    "Satisfaction" integer,
    "TimeToFirstClickMs" integer,
    "IsLocationBased" boolean DEFAULT false,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    PRIMARY KEY ("Id", "CreatedAt")
);

-- Convert to hypertable
SELECT create_hypertable('"ChatSearchMetrics"', 'CreatedAt', chunk_time_interval => INTERVAL '1 week');

-- Action-level metrics
CREATE TABLE "ChatActionMetrics" (
    "Id" uuid NOT NULL,
    "SearchMetricsId" uuid NOT NULL,
    "ActionType" integer NOT NULL,
    "SearchResultId" uuid,
    "ResultPosition" integer,
    "TimeToActionMs" integer,
    "ActionContext" varchar(500),
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    PRIMARY KEY ("Id", "CreatedAt")
);

-- Convert to hypertable
SELECT create_hypertable('"ChatActionMetrics"', 'CreatedAt', chunk_time_interval => INTERVAL '1 week');

-- ============================================
-- INDEXES FOR ANALYTICS QUERIES
-- ============================================
CREATE INDEX idx_session_metrics_profile ON "ChatSessionMetrics"("ProfileId", "CreatedAt" DESC);
CREATE INDEX idx_session_metrics_city ON "ChatSessionMetrics"("UserCity", "CreatedAt" DESC);
CREATE INDEX idx_search_metrics_query ON "ChatSearchMetrics"("NormalizedQuery", "CreatedAt" DESC);
CREATE INDEX idx_search_metrics_intent ON "ChatSearchMetrics"("IntentCategory", "CreatedAt" DESC);
CREATE INDEX idx_search_metrics_noresults ON "ChatSearchMetrics"("CreatedAt" DESC) WHERE "HasResults" = false;
CREATE INDEX idx_action_metrics_type ON "ChatActionMetrics"("ActionType", "CreatedAt" DESC);

-- ============================================
-- TIMESCALEDB CONTINUOUS AGGREGATES
-- Real-time materialized views for dashboards
-- ============================================

-- Hourly session aggregates
CREATE MATERIALIZED VIEW chat_sessions_hourly
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 hour', "CreatedAt") AS bucket,
    COUNT(*) AS session_count,
    COUNT(DISTINCT "ProfileId") AS unique_users,
    AVG("DurationSeconds") AS avg_duration_seconds,
    SUM("UserMessageCount") AS total_user_messages,
    SUM("SearchCount") AS total_searches,
    SUM("SuccessfulSearchCount") AS successful_searches,
    SUM("ActionsTaken") AS total_actions,
    SUM(CASE WHEN "LocationProvided" THEN 1 ELSE 0 END) AS location_provided_count
FROM "ChatSessionMetrics"
GROUP BY bucket
WITH NO DATA;

-- Refresh policy: update every 10 minutes
SELECT add_continuous_aggregate_policy('chat_sessions_hourly',
    start_offset => INTERVAL '2 hours',
    end_offset => INTERVAL '10 minutes',
    schedule_interval => INTERVAL '10 minutes');

-- Hourly search aggregates
CREATE MATERIALIZED VIEW chat_searches_hourly
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 hour', "CreatedAt") AS bucket,
    "IntentCategory",
    COUNT(*) AS search_count,
    SUM(CASE WHEN "HasResults" THEN 1 ELSE 0 END) AS with_results,
    SUM(CASE WHEN NOT "HasResults" THEN 1 ELSE 0 END) AS zero_results,
    AVG("ResultCount") AS avg_results,
    AVG("ExecutionTimeMs") AS avg_execution_ms,
    SUM("ClickedResultCount") AS total_clicks,
    AVG("FirstClickPosition")::numeric(4,2) AS avg_first_click_position
FROM "ChatSearchMetrics"
GROUP BY bucket, "IntentCategory"
WITH NO DATA;

SELECT add_continuous_aggregate_policy('chat_searches_hourly',
    start_offset => INTERVAL '2 hours',
    end_offset => INTERVAL '10 minutes',
    schedule_interval => INTERVAL '10 minutes');

-- Daily top queries aggregate
CREATE MATERIALIZED VIEW chat_top_queries_daily
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 day', "CreatedAt") AS bucket,
    "NormalizedQuery",
    COUNT(*) AS query_count,
    SUM(CASE WHEN "HasResults" THEN 1 ELSE 0 END) AS success_count,
    AVG("ClickedResultCount")::numeric(4,2) AS avg_clicks
FROM "ChatSearchMetrics"
WHERE "NormalizedQuery" IS NOT NULL
GROUP BY bucket, "NormalizedQuery"
WITH NO DATA;

SELECT add_continuous_aggregate_policy('chat_top_queries_daily',
    start_offset => INTERVAL '2 days',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour');

-- Hourly action aggregates
CREATE MATERIALIZED VIEW chat_actions_hourly
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 hour', "CreatedAt") AS bucket,
    "ActionType",
    COUNT(*) AS action_count,
    AVG("TimeToActionMs") AS avg_time_to_action_ms,
    AVG("ResultPosition")::numeric(4,2) AS avg_result_position
FROM "ChatActionMetrics"
GROUP BY bucket, "ActionType"
WITH NO DATA;

SELECT add_continuous_aggregate_policy('chat_actions_hourly',
    start_offset => INTERVAL '2 hours',
    end_offset => INTERVAL '10 minutes',
    schedule_interval => INTERVAL '10 minutes');

-- ============================================
-- COMPRESSION POLICY (for old data)
-- ============================================
-- Compress chunks older than 30 days to save storage
ALTER TABLE "ChatSessionMetrics" SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = '"ProfileId"'
);
SELECT add_compression_policy('"ChatSessionMetrics"', INTERVAL '30 days');

ALTER TABLE "ChatSearchMetrics" SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = '"IntentCategory"'
);
SELECT add_compression_policy('"ChatSearchMetrics"', INTERVAL '30 days');

ALTER TABLE "ChatActionMetrics" SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = '"ActionType"'
);
SELECT add_compression_policy('"ChatActionMetrics"', INTERVAL '30 days');
```

### Analytics Dashboard Metrics

#### Overview KPIs
| Metric | Description |
|--------|-------------|
| **Daily Active Users** | Unique profiles using chat per day |
| **Avg Session Duration** | Mean time spent in chat |
| **Messages per Session** | Avg user messages per session |
| **Search Success Rate** | % of searches returning results |
| **Action Rate** | % of searches leading to actions |

#### Search Effectiveness
| Metric | Description |
|--------|-------------|
| **Zero-Result Rate** | % of searches with no results |
| **Click-Through Rate** | % of results clicked |
| **Position 1 CTR** | % of first-position results clicked |
| **Time to First Click** | Avg time from results to click |
| **Reformulation Rate** | % of users who search again |

#### Top Queries
| Report | Description |
|--------|-------------|
| **Most Popular Queries** | Top 50 searches by volume |
| **Failed Queries** | Top queries with zero results |
| **Intent Distribution** | Breakdown by intent category |
| **Location Queries** | Geographic search patterns |

#### User Engagement
| Metric | Description |
|--------|-------------|
| **Conversion Rate** | Search → Action (call/save/etc) |
| **Most Called Businesses** | Top businesses by call actions |
| **Saved Results** | Most frequently saved items |
| **Session Depth** | Avg searches per session |

### Dashboard Wireframe
```
┌─────────────────────────────────────────────────────────────────────┐
│  📊 Chat Analytics Dashboard                       [Last 7 days ▼] │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────┐│
│  │ 1,234        │  │ 4m 32s       │  │ 78%          │  │ 45%      ││
│  │ Sessions     │  │ Avg Duration │  │ Search Rate  │  │ Action   ││
│  │ ▲ +12%       │  │ ▲ +8%        │  │ ▼ -3%        │  │ Rate     ││
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────┘│
│                                                                      │
│  ┌─────────────────────────────────┐  ┌────────────────────────────┐│
│  │ 📈 Sessions Over Time          │  │ 🔍 Top Searches            ││
│  │ [Line chart: daily sessions]   │  │ 1. pizzerias cerca    (234)││
│  │                                 │  │ 2. cómo sacar DUI     (189)││
│  │                                 │  │ 3. banco agrícola     (156)││
│  │                                 │  │ 4. restaurantes       (145)││
│  │                                 │  │ 5. horario isss       (98) ││
│  └─────────────────────────────────┘  └────────────────────────────┘│
│                                                                      │
│  ┌─────────────────────────────────┐  ┌────────────────────────────┐│
│  │ ❌ Failed Searches (0 results) │  │ 📱 Actions Breakdown       ││
│  │ 1. "dentista 24 horas"     (45)│  │ 📞 Calls: 234 (35%)        ││
│  │ 2. "notario fin de semana" (32)│  │ 💬 WhatsApp: 189 (28%)     ││
│  │ 3. "hospital veterinario"  (28)│  │ 📌 Saves: 156 (23%)        ││
│  │                                 │  │ 🗺️ Maps: 89 (14%)          ││
│  └─────────────────────────────────┘  └────────────────────────────┘│
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/analytics/session/start` | Start session tracking |
| POST | `/api/analytics/session/end` | End session, calc duration |
| POST | `/api/analytics/search` | Log search query |
| POST | `/api/analytics/action` | Log user action |
| GET | `/api/admin/analytics/overview` | Dashboard KPIs |
| GET | `/api/admin/analytics/searches` | Search reports |
| GET | `/api/admin/analytics/sessions` | Session reports |
| GET | `/api/admin/analytics/export` | Export CSV/Excel |

### Acceptance Criteria
- [ ] Session start/end tracked automatically
- [ ] Session duration calculated accurately
- [ ] All searches logged with query and results
- [ ] Result clicks tracked with position
- [ ] Actions (call, save, etc.) tracked
- [ ] Dashboard shows real-time metrics
- [ ] Can filter by date range
- [ ] Can export reports
- [ ] Zero-result queries visible for content gap analysis
- [ ] Privacy: no PII in analytics, just aggregates

### Deliverable
Complete visibility into chat usage, search effectiveness, and user engagement for data-driven improvements.

---

## Phase 9.5: Session Tracing & Debugging 🔍

### Overview
While Phase 9 provides **aggregated analytics** (dashboards, KPIs), this phase adds **granular session traces** - a detailed timeline of everything that happened in a specific chat session. Think of it like browser DevTools Network tab, but for chat sessions.

### Use Cases
| Scenario | What You Need to See |
|----------|---------------------|
| **Debugging** | "Why did user X get no results?" - See exact query, search params, DB execution time |
| **Support** | "User complained about wrong results" - Replay their session step-by-step |
| **Quality** | "Are AI responses accurate?" - Review AI tool calls and responses |
| **Performance** | "Chat feels slow" - See timing breakdown: AI inference vs DB query vs network |
| **Training** | "Which searches need better embeddings?" - Find patterns in failed searches |

### Architecture: Structured Event Log

```
Session Timeline (like a stack trace)
─────────────────────────────────────
│ 14:32:01.234 │ SESSION_START │ user=abc, location=(13.69,-89.19), city=San Salvador
│ 14:32:02.456 │ USER_MESSAGE  │ "busco pizzerias cerca de mi"
│ 14:32:02.467 │ AI_THINKING   │ intent=search, confidence=0.94
│ 14:32:02.523 │ TOOL_CALL     │ func=SearchNearbyProfiles, params={category:restaurant, q:pizza, radius:5km}
│ 14:32:02.534 │ DB_QUERY      │ type=hybrid_search, execution_time=156ms, rows_scanned=2341
│ 14:32:02.690 │ SEARCH_RESULT │ count=5, top_score=0.89, avg_distance=1.2km
│ 14:32:02.712 │ AI_RESPONSE   │ "Encontré 5 pizzerías cerca...", tokens_used=234
│ 14:32:02.756 │ CARDS_SHOWN   │ ids=[uuid1,uuid2,uuid3,uuid4,uuid5], render_time=44ms
│ 14:32:15.123 │ USER_CLICK    │ card=uuid2, position=2, business="Pizza Hut Centro"
│ 14:32:18.456 │ USER_ACTION   │ type=CALL, phone="+503 2222-3333", time_to_action=3.3s
│ 14:32:45.789 │ SESSION_END   │ duration=44s, searches=1, actions=1, satisfaction=ACTION_TAKEN
```

### New Entity: `ChatTraceEvent`

```csharp
/// <summary>
/// Individual event in a session trace timeline.
/// Stored in TimescaleDB hypertable for efficient time-range queries.
/// </summary>
public class ChatTraceEvent : BaseEntity
{
    /// <summary>
    /// Session this event belongs to
    /// </summary>
    public Guid SessionId { get; set; }
    
    /// <summary>
    /// Precise timestamp for ordering
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Event type for filtering
    /// </summary>
    public TraceEventType EventType { get; set; }
    
    /// <summary>
    /// Sequence number within session (for ordering if timestamps collide)
    /// </summary>
    public int SequenceNumber { get; set; }
    
    /// <summary>
    /// Parent event ID for nesting (e.g., DB_QUERY under TOOL_CALL)
    /// </summary>
    public Guid? ParentEventId { get; set; }
    
    /// <summary>
    /// Duration in milliseconds (for timed events)
    /// </summary>
    public int? DurationMs { get; set; }
    
    /// <summary>
    /// Human-readable summary
    /// </summary>
    [StringLength(500)]
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// Full event details as JSON (structured data for debugging)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? Details { get; set; }
    
    /// <summary>
    /// Error message if this event represents a failure
    /// </summary>
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Stack trace for exceptions
    /// </summary>
    public string? StackTrace { get; set; }
    
    /// <summary>
    /// Severity level
    /// </summary>
    public TraceLevel Level { get; set; } = TraceLevel.Info;
}

public enum TraceEventType
{
    // Session lifecycle
    SessionStart = 1,
    SessionEnd = 2,
    LocationUpdated = 3,
    
    // User interactions
    UserMessage = 10,
    UserClick = 11,
    UserAction = 12,
    UserFeedback = 13,
    
    // AI processing
    AiThinking = 20,
    AiToolCall = 21,
    AiToolResult = 22,
    AiResponse = 23,
    AiError = 24,
    
    // Search operations
    SearchStarted = 30,
    SearchDbQuery = 31,
    SearchEmbedding = 32,
    SearchResults = 33,
    SearchNoResults = 34,
    
    // Rendering
    CardsShown = 40,
    CardExpanded = 41,
    MapLoaded = 42,
    
    // System
    Warning = 50,
    Error = 51,
    PerformanceAlert = 52
}

public enum TraceLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}
```

### Event Details JSON Schemas

Each event type has a specific JSON structure in the `Details` column:

```csharp
// SESSION_START details
{
    "profileId": "uuid",
    "userAgent": "Mozilla/5.0...",
    "deviceType": "mobile",
    "location": { "lat": 13.69, "lng": -89.19, "accuracy": 15.0 },
    "city": "San Salvador",
    "timezone": "America/El_Salvador"
}

// USER_MESSAGE details
{
    "content": "busco pizzerias cerca de mi",
    "wordCount": 5,
    "language": "es",
    "hasLocation": true
}

// TOOL_CALL details
{
    "functionName": "SearchNearbyProfiles",
    "parameters": {
        "category": "restaurant",
        "query": "pizza",
        "radiusKm": 5,
        "userLat": 13.69,
        "userLng": -89.19
    },
    "startTime": "2024-12-12T14:32:02.523Z"
}

// DB_QUERY details
{
    "queryType": "hybrid_search",
    "tables": ["Profile", "Post"],
    "executionTimeMs": 156,
    "rowsScanned": 2341,
    "rowsReturned": 5,
    "indexesUsed": ["idx_profile_embedding", "idx_profile_location"],
    "queryHash": "abc123",  // For identifying similar queries
    "explain": "..." // Optional: EXPLAIN ANALYZE output
}

// SEARCH_RESULTS details
{
    "resultCount": 5,
    "hasResults": true,
    "topScore": 0.89,
    "avgScore": 0.72,
    "avgDistanceKm": 1.2,
    "resultIds": ["uuid1", "uuid2", "uuid3", "uuid4", "uuid5"],
    "resultTypes": { "restaurant": 4, "food_truck": 1 },
    "searchWeights": { "semantic": 0.4, "fullText": 0.3, "geo": 0.3 }
}

// USER_ACTION details
{
    "actionType": "CALL",
    "targetId": "uuid2",
    "targetName": "Pizza Hut Centro",
    "position": 2,
    "phone": "+503 2222-3333",
    "timeToActionMs": 3300
}

// ERROR details
{
    "errorType": "TimeoutException",
    "message": "Database query timed out after 30s",
    "query": "...",
    "stackTrace": "...",
    "context": { "sessionId": "...", "lastAction": "..." }
}
```

### Database Schema (TimescaleDB)

```sql
-- Add to sivaros_analytics database

-- Session trace events (high-volume, append-only)
CREATE TABLE "ChatTraceEvents" (
    "Id" uuid NOT NULL,
    "SessionId" uuid NOT NULL,
    "Timestamp" timestamptz NOT NULL,
    "EventType" integer NOT NULL,
    "SequenceNumber" integer NOT NULL,
    "ParentEventId" uuid,
    "DurationMs" integer,
    "Summary" varchar(500) NOT NULL,
    "Details" jsonb,
    "ErrorMessage" varchar(2000),
    "StackTrace" text,
    "Level" integer NOT NULL DEFAULT 1,
    PRIMARY KEY ("Id", "Timestamp")
);

-- Convert to hypertable (partitioned by time)
SELECT create_hypertable('"ChatTraceEvents"', 'Timestamp', chunk_time_interval => INTERVAL '1 day');

-- Indexes for common access patterns
CREATE INDEX idx_trace_session ON "ChatTraceEvents"("SessionId", "Timestamp");
CREATE INDEX idx_trace_type ON "ChatTraceEvents"("EventType", "Timestamp" DESC);
CREATE INDEX idx_trace_errors ON "ChatTraceEvents"("Timestamp" DESC) WHERE "Level" >= 2;
CREATE INDEX idx_trace_slow ON "ChatTraceEvents"("Timestamp" DESC) WHERE "DurationMs" > 1000;

-- GIN index for JSON queries (e.g., find all queries to a specific function)
CREATE INDEX idx_trace_details ON "ChatTraceEvents" USING GIN ("Details");

-- Compression for old traces (compress after 7 days)
ALTER TABLE "ChatTraceEvents" SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = '"SessionId"',
    timescaledb.compress_orderby = '"Timestamp", "SequenceNumber"'
);
SELECT add_compression_policy('"ChatTraceEvents"', INTERVAL '7 days');

-- Retention policy: keep traces for 30 days, then drop
SELECT add_retention_policy('"ChatTraceEvents"', INTERVAL '30 days');

-- ============================================
-- CONTINUOUS AGGREGATES FOR TRACE DATA
-- ============================================

-- Hourly error/warning summary
CREATE MATERIALIZED VIEW trace_errors_hourly
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 hour', "Timestamp") AS bucket,
    "EventType",
    COUNT(*) AS event_count,
    COUNT(*) FILTER (WHERE "Level" = 2) AS warnings,
    COUNT(*) FILTER (WHERE "Level" >= 3) AS errors,
    AVG("DurationMs") FILTER (WHERE "DurationMs" IS NOT NULL) AS avg_duration_ms,
    MAX("DurationMs") FILTER (WHERE "DurationMs" IS NOT NULL) AS max_duration_ms
FROM "ChatTraceEvents"
GROUP BY bucket, "EventType"
WITH NO DATA;

SELECT add_continuous_aggregate_policy('trace_errors_hourly',
    start_offset => INTERVAL '2 hours',
    end_offset => INTERVAL '10 minutes',
    schedule_interval => INTERVAL '10 minutes');

-- Slow query tracking
CREATE MATERIALIZED VIEW trace_slow_queries_hourly
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 hour', "Timestamp") AS bucket,
    COUNT(*) AS slow_query_count,
    AVG("DurationMs") AS avg_slow_duration_ms,
    percentile_cont(0.95) WITHIN GROUP (ORDER BY "DurationMs") AS p95_duration_ms
FROM "ChatTraceEvents"
WHERE "EventType" = 31  -- SearchDbQuery
  AND "DurationMs" > 500  -- Slow = over 500ms
GROUP BY bucket
WITH NO DATA;

SELECT add_continuous_aggregate_policy('trace_slow_queries_hourly',
    start_offset => INTERVAL '2 hours',
    end_offset => INTERVAL '10 minutes',
    schedule_interval => INTERVAL '10 minutes');
```

### Tracing Service

```csharp
// Sivar.Os.Analytics/Services/SessionTracer.cs

public interface ISessionTracer
{
    /// <summary>
    /// Start tracing a new session
    /// </summary>
    Task<Guid> StartSession(Guid profileId, string? userAgent = null, 
        GeoLocation? location = null);
    
    /// <summary>
    /// End a session and finalize trace
    /// </summary>
    Task EndSession(Guid sessionId);
    
    /// <summary>
    /// Log an event to the session trace
    /// </summary>
    Task TraceEvent(Guid sessionId, TraceEventType type, string summary,
        object? details = null, int? durationMs = null, Guid? parentId = null,
        TraceLevel level = TraceLevel.Info);
    
    /// <summary>
    /// Log an error with stack trace
    /// </summary>
    Task TraceError(Guid sessionId, Exception ex, string? context = null);
    
    /// <summary>
    /// Create a timed scope that auto-logs duration
    /// </summary>
    ITracerScope BeginScope(Guid sessionId, TraceEventType type, string summary,
        object? details = null);
}

public interface ITracerScope : IAsyncDisposable
{
    Guid EventId { get; }
    void SetResult(object result);
    void SetError(Exception ex);
}

// Usage in ChatService:
public async Task<ChatResponse> ProcessMessage(ChatRequest request)
{
    await using var scope = _tracer.BeginScope(
        request.SessionId,
        TraceEventType.UserMessage,
        $"Processing: {request.Message.Truncate(50)}",
        new { content = request.Message, wordCount = request.Message.Split(' ').Length }
    );

    try
    {
        // AI processing
        await using var aiScope = _tracer.BeginScope(
            request.SessionId,
            TraceEventType.AiThinking,
            "AI analyzing intent",
            parentId: scope.EventId
        );
        
        var intent = await _aiAgent.AnalyzeIntent(request.Message);
        aiScope.SetResult(new { intent = intent.Category, confidence = intent.Score });
        
        // Search execution
        if (intent.RequiresSearch)
        {
            await using var searchScope = _tracer.BeginScope(
                request.SessionId,
                TraceEventType.SearchStarted,
                $"Searching: {intent.Query}",
                parentId: scope.EventId
            );
            
            var results = await ExecuteSearch(request.SessionId, intent, searchScope.EventId);
            searchScope.SetResult(new { count = results.Count });
        }
        
        // ... rest of processing
    }
    catch (Exception ex)
    {
        scope.SetError(ex);
        throw;
    }
}
```

### Session Viewer UI

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│  🔍 Session Trace Viewer                                    Session: abc-123   │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  📍 User: Anonymous | 📱 Mobile | 📍 San Salvador | Duration: 44s               │
│                                                                                  │
│  Filter: [All ▼] [Errors Only ☐] [Slow (>500ms) ☐]      🔍 Search in trace      │
│                                                                                  │
│  ─────────────────────────────────────────────────────────────────────────────  │
│                                                                                  │
│  14:32:01.234  ▶ SESSION_START                                         [Info]   │
│                  Location: (13.69, -89.19) • San Salvador                       │
│                                                                                  │
│  14:32:02.456  ▶ USER_MESSAGE                                         [Info]    │
│                  "busco pizzerias cerca de mi"                                   │
│                                                                                  │
│  14:32:02.467  │  └─ AI_THINKING                              [12ms]  [Info]    │
│                │     Intent: search (94% confidence)                             │
│                │                                                                 │
│  14:32:02.523  │  └─ TOOL_CALL                               [167ms]  [Info]    │
│                │     SearchNearbyProfiles(category=restaurant, q=pizza)          │
│                │     │                                                           │
│  14:32:02.534  │     └─ DB_QUERY                             [156ms]  [Slow]    │
│                │        hybrid_search • 2341 rows scanned • 5 returned           │
│                │        ⚠️ Consider adding index on category+location            │
│                │                                                                 │
│  14:32:02.690  │  └─ SEARCH_RESULTS                                   [Info]    │
│                │     5 results • Top score: 0.89 • Avg distance: 1.2km          │
│                │                                                                 │
│  14:32:02.712  ▶ AI_RESPONSE                                          [Info]    │
│                  "Encontré 5 pizzerías cerca..." (234 tokens)                    │
│                                                                                  │
│  14:32:02.756  ▶ CARDS_SHOWN                                  [44ms]  [Info]    │
│                  5 cards rendered                                                │
│                                                                                  │
│  14:32:15.123  ▶ USER_CLICK                                           [Info]    │
│                  Card #2: "Pizza Hut Centro"                                     │
│                                                                                  │
│  14:32:18.456  ▶ USER_ACTION                                          [Info]    │
│                  📞 Called +503 2222-3333 (3.3s after results)                   │
│                                                                                  │
│  14:32:45.789  ▶ SESSION_END                                          [Info]    │
│                  Duration: 44s • Searches: 1 • Actions: 1 • ✅ Success           │
│                                                                                  │
│  ─────────────────────────────────────────────────────────────────────────────  │
│                                                                                  │
│  [Export JSON] [Export CSV] [Copy Session ID] [Find Similar Sessions]           │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Files to Create
| File | Purpose |
|------|---------|
| `Sivar.Os.Analytics/Entities/ChatTraceEvent.cs` | Trace event entity |
| `Sivar.Os.Analytics/Services/SessionTracer.cs` | Tracing implementation |
| `Sivar.Os.Analytics/Services/TracerScope.cs` | Disposable timed scope |
| `Sivar.Os.Analytics/Repositories/TraceRepository.cs` | Trace storage |
| `Sivar.Os.Shared/DTOs/TraceEventDtos.cs` | DTOs for API |
| `Sivar.Os/Controllers/SessionTraceController.cs` | Admin API |
| `Sivar.Os.Client/Pages/Admin/SessionTraceViewer.razor` | Trace viewer UI |

### Files to Modify
| File | Changes |
|------|---------|
| `Sivar.Os.Analytics/Context/AnalyticsDbContext.cs` | Add `DbSet<ChatTraceEvent>` |
| `Sivar.Os/Services/ChatService.cs` | Inject `ISessionTracer`, add trace calls |
| `Sivar.Os/Services/ChatFunctionService.cs` | Trace tool calls and results |
| `Sivar.Os/Services/SearchResultService.cs` | Trace DB queries with timing |
| `Sivar.Os.Client/Components/AIChat/ChatMessage.razor` | Trace card renders/clicks |

### API Endpoints
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/admin/traces/sessions` | List recent sessions |
| GET | `/api/admin/traces/sessions/{id}` | Get full session trace |
| GET | `/api/admin/traces/sessions/{id}/events` | Get events with filtering |
| GET | `/api/admin/traces/errors` | Recent errors across sessions |
| GET | `/api/admin/traces/slow-queries` | Slow query analysis |
| GET | `/api/admin/traces/search` | Search traces by content |

### Acceptance Criteria
- [ ] Every chat session has a trace
- [ ] All AI tool calls logged with parameters and duration
- [ ] All DB queries logged with execution time
- [ ] Search results logged with scores and counts
- [ ] User actions (clicks, calls) logged with timing
- [ ] Errors captured with full stack trace
- [ ] Session viewer shows hierarchical timeline
- [ ] Can filter by error level, event type, duration
- [ ] Can search within traces
- [ ] Slow queries (>500ms) highlighted
- [ ] Traces auto-compress after 7 days
- [ ] Traces auto-delete after 30 days (retention policy)
- [ ] Export to JSON/CSV for debugging

### Performance Considerations
- **Async logging**: Fire-and-forget writes to avoid blocking chat
- **Batching**: Buffer events and flush every 100ms or 10 events
- **Sampling**: In production, can sample (e.g., trace 10% of sessions)
- **Compression**: TimescaleDB compresses after 7 days (~90% savings)
- **Retention**: Auto-delete after 30 days

### Deliverable
Complete session-level observability for debugging issues, understanding user journeys, and identifying performance bottlenecks.

---

## Phase 10: Multi-Agent Configuration & Management 🤖

### Overview
Currently, agent configuration (system prompts, tools, models) is hardcoded in `Program.cs`. This phase moves agent configuration to the database, allowing dynamic management of multiple specialized agents without code changes.

### Current State Analysis
```csharp
// Currently hardcoded in Program.cs
builder.Services.AddScoped<AIAgent>(sp =>
{
    return new ChatClientAgent(
        chatClient,
        instructions: @"You are Sivar, a helpful AI assistant...",  // HARDCODED
        name: "SivarAgent",
        tools: tools,  // HARDCODED list
        ...
    );
});
```

**Existing Agents:**
| Agent | Location | Purpose |
|-------|----------|---------|
| `SivarAgent` | `Program.cs` | Main chat assistant |
| `BusinessSearchAgent` | `Sivar.Os/Agents/` | Specialized business search |

**Problems:**
- System prompt changes require deployment
- Can't A/B test different prompts
- Can't add/remove tools dynamically
- No way to have specialized agents (government, tourism, etc.)
- No versioning of prompt changes

### Solution: Database-Driven Agent Configuration

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Agent Configuration Flow                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────┐     ┌─────────────────┐     ┌──────────────┐  │
│  │  Admin UI       │────►│  AgentConfig    │────►│  AgentFactory │  │
│  │  (edit prompts) │     │  Table          │     │  (builds agents)│ │
│  └─────────────────┘     └─────────────────┘     └──────────────┘  │
│                                                          │           │
│                                                          ▼           │
│  ┌─────────────────┐     ┌─────────────────┐     ┌──────────────┐  │
│  │  Intent Router  │◄────│  Agent Registry │◄────│  AIAgent      │  │
│  │  (picks agent)  │     │  (cached)       │     │  instances    │  │
│  └─────────────────┘     └─────────────────┘     └──────────────┘  │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### New Entity: `AgentConfiguration`

```csharp
/// <summary>
/// Stores AI agent configuration in database for dynamic management
/// </summary>
public class AgentConfiguration : BaseEntity
{
    /// <summary>
    /// Unique identifier for this agent (e.g., "sivar-main", "business-search", "government-help")
    /// </summary>
    [Required, StringLength(50)]
    public string AgentKey { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for admin UI
    /// </summary>
    [Required, StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of agent's purpose
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// System prompt/instructions for the agent
    /// </summary>
    [Required]
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Which AI provider to use: "ollama", "openai", "azure-openai"
    /// </summary>
    [StringLength(50)]
    public string Provider { get; set; } = "ollama";

    /// <summary>
    /// Model ID for the provider (e.g., "llama3.2:latest", "gpt-4o")
    /// </summary>
    [StringLength(100)]
    public string ModelId { get; set; } = "llama3.2:latest";

    /// <summary>
    /// Temperature for AI responses (0.0 - 2.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens for response
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// JSON array of enabled tool/function names
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? EnabledTools { get; set; }

    /// <summary>
    /// JSON object with additional provider-specific settings
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? ProviderSettings { get; set; }

    /// <summary>
    /// Intent patterns that route to this agent (regex patterns)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? IntentPatterns { get; set; }

    /// <summary>
    /// Priority when multiple agents match (higher = preferred)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Is this agent currently active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Version number for tracking prompt changes
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// When was this configuration last updated?
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Who updated this configuration?
    /// </summary>
    public Guid? UpdatedByProfileId { get; set; }

    /// <summary>
    /// Optional: AB test variant (A, B, C, etc.)
    /// </summary>
    [StringLength(10)]
    public string? AbTestVariant { get; set; }

    /// <summary>
    /// Percentage of traffic for this variant (0-100)
    /// </summary>
    public int AbTestWeight { get; set; } = 100;
}
```

### New Entity: `AgentTool`

```csharp
/// <summary>
/// Registry of available tools/functions that can be assigned to agents
/// </summary>
public class AgentTool : BaseEntity
{
    /// <summary>
    /// Unique function name (must match code)
    /// </summary>
    [Required, StringLength(100)]
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name
    /// </summary>
    [Required, StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description for AI and admin UI
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Category for grouping (Search, Profile, Post, Location, etc.)
    /// </summary>
    [StringLength(50)]
    public string Category { get; set; } = "General";

    /// <summary>
    /// JSON schema for parameters (for documentation/validation)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? ParameterSchema { get; set; }

    /// <summary>
    /// Is this tool available for assignment?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Requires specific permissions?
    /// </summary>
    [StringLength(100)]
    public string? RequiredPermission { get; set; }
}
```

### Specialized Agent Configurations

```json
// Example: Main Sivar Agent
{
  "agentKey": "sivar-main",
  "displayName": "Sivar AI Assistant",
  "systemPrompt": "You are Sivar, a helpful AI assistant for El Salvador...",
  "provider": "ollama",
  "modelId": "llama3.2:latest",
  "temperature": 0.7,
  "enabledTools": [
    "SearchProfiles",
    "SearchPosts", 
    "SearchBusinesses",
    "SearchNearbyProfiles",
    "GetPostDetails",
    "SetCurrentLocation"
  ],
  "intentPatterns": [".*"],  // Catch-all
  "priority": 0
}

// Example: Government Procedures Agent
{
  "agentKey": "government-procedures",
  "displayName": "Government Procedures Helper",
  "systemPrompt": "You are a specialized assistant for El Salvador government procedures...\n\nYou help users with:\n- DUI (identity document) procedures\n- Passport applications\n- License renewals\n- Tax filings\n- Municipal permits\n\nAlways provide step-by-step instructions, required documents, and office locations.",
  "provider": "openai",
  "modelId": "gpt-4o",
  "temperature": 0.3,  // Lower for factual accuracy
  "enabledTools": [
    "SearchGovernmentOffices",
    "SearchProcedures",
    "GetOfficeHours",
    "GetRequiredDocuments"
  ],
  "intentPatterns": [
    ".*DUI.*",
    ".*pasaporte.*",
    ".*licencia.*",
    ".*trámite.*",
    ".*gobierno.*",
    ".*municipalidad.*"
  ],
  "priority": 10
}

// Example: Tourism Agent  
{
  "agentKey": "tourism-guide",
  "displayName": "El Salvador Tourism Guide",
  "systemPrompt": "You are a friendly tourism guide for El Salvador...\n\nYou help visitors discover:\n- Beaches and surfing spots\n- Mayan ruins and historical sites\n- Restaurants and local cuisine\n- Hotels and accommodations\n- Events and festivals\n\nBe enthusiastic and provide local tips!",
  "provider": "ollama",
  "modelId": "llama3.2:latest",
  "temperature": 0.8,  // More creative
  "enabledTools": [
    "SearchTourism",
    "SearchRestaurants",
    "SearchEvents",
    "SearchHotels",
    "SearchNearbyAttractions"
  ],
  "intentPatterns": [
    ".*turismo.*",
    ".*playa.*",
    ".*surf.*",
    ".*hotel.*",
    ".*visitar.*",
    ".*tour.*"
  ],
  "priority": 10
}
```

### Agent Factory Service

```csharp
public interface IAgentFactory
{
    /// <summary>
    /// Get an agent by key, building from database config
    /// </summary>
    Task<AIAgent> GetAgentAsync(string agentKey);

    /// <summary>
    /// Get the best agent for a given user message (intent routing)
    /// </summary>
    Task<AIAgent> GetAgentForIntentAsync(string userMessage);

    /// <summary>
    /// Refresh cached agent configurations
    /// </summary>
    Task RefreshCacheAsync();
}

public class AgentFactory : IAgentFactory
{
    private readonly IAgentConfigurationRepository _configRepo;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AgentFactory> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<AIAgent> GetAgentAsync(string agentKey)
    {
        var cacheKey = $"agent:{agentKey}";
        
        if (_cache.TryGetValue(cacheKey, out AIAgent? cached))
            return cached!;

        var config = await _configRepo.GetByKeyAsync(agentKey);
        if (config == null)
            throw new InvalidOperationException($"Agent '{agentKey}' not found");

        var agent = BuildAgent(config);
        _cache.Set(cacheKey, agent, CacheDuration);
        
        return agent;
    }

    public async Task<AIAgent> GetAgentForIntentAsync(string userMessage)
    {
        var configs = await GetActiveConfigsAsync();
        
        // Find matching agent by intent pattern
        var matched = configs
            .Where(c => c.IntentPatterns != null)
            .Select(c => new {
                Config = c,
                Matches = MatchesIntent(userMessage, c.IntentPatterns)
            })
            .Where(x => x.Matches)
            .OrderByDescending(x => x.Config.Priority)
            .FirstOrDefault();

        var agentKey = matched?.Config.AgentKey ?? "sivar-main";
        return await GetAgentAsync(agentKey);
    }

    private AIAgent BuildAgent(AgentConfiguration config)
    {
        // Get the appropriate chat client for provider
        var chatClient = GetChatClientForProvider(config.Provider, config.ModelId);
        
        // Get enabled tools
        var tools = GetToolsForAgent(config.EnabledTools);
        
        // Build the agent
        return new ChatClientAgent(
            chatClient,
            instructions: config.SystemPrompt,
            name: config.AgentKey,
            description: config.Description ?? config.DisplayName,
            tools: tools,
            loggerFactory: _serviceProvider.GetRequiredService<ILoggerFactory>()
        );
    }
}
```

### Admin UI for Agent Management

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│  🤖 Agent Configuration                                    [+ New Agent]        │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────────┐│
│  │ Active Agents                                                                ││
│  ├──────────────────┬──────────────┬──────────┬─────────┬───────────┬─────────┤│
│  │ Agent            │ Provider     │ Model    │ Tools   │ Priority  │ Actions ││
│  ├──────────────────┼──────────────┼──────────┼─────────┼───────────┼─────────┤│
│  │ 🟢 sivar-main    │ Ollama       │ llama3.2 │ 13      │ 0         │ ✏️ 📊   ││
│  │ 🟢 business      │ Ollama       │ llama3.2 │ 6       │ 10        │ ✏️ 📊   ││
│  │ 🟢 government    │ OpenAI       │ gpt-4o   │ 4       │ 10        │ ✏️ 📊   ││
│  │ 🟡 tourism       │ Ollama       │ llama3.2 │ 5       │ 10        │ ✏️ 📊   ││
│  │ ⚪ test-agent    │ OpenAI       │ gpt-4o   │ 2       │ 0         │ ✏️ 🗑️   ││
│  └──────────────────┴──────────────┴──────────┴─────────┴───────────┴─────────┘│
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────────┐│
│  │ Edit: sivar-main (v3)                                        [Save] [Test]  ││
│  ├─────────────────────────────────────────────────────────────────────────────┤│
│  │                                                                              ││
│  │ Display Name: [Sivar AI Assistant                           ]               ││
│  │ Description:  [Main chat assistant for Sivar.Os platform    ]               ││
│  │                                                                              ││
│  │ Provider: [Ollama      ▼]  Model: [llama3.2:latest          ]               ││
│  │ Temperature: [0.7      ]   Max Tokens: [2000                ]               ││
│  │                                                                              ││
│  │ System Prompt:                                                               ││
│  │ ┌───────────────────────────────────────────────────────────────────────┐   ││
│  │ │ You are Sivar, a helpful AI assistant for El Salvador.               │   ││
│  │ │                                                                        │   ││
│  │ │ You help users:                                                        │   ││
│  │ │ - Find businesses, restaurants, and services                          │   ││
│  │ │ - Search for government procedures                                    │   ││
│  │ │ - Discover events and tourism attractions                             │   ││
│  │ │ - Connect with other users on the network                             │   ││
│  │ │                                                                        │   ││
│  │ │ Always be friendly, helpful, and use Spanish when appropriate.        │   ││
│  │ └───────────────────────────────────────────────────────────────────────┘   ││
│  │                                                                              ││
│  │ Enabled Tools:                                                               ││
│  │ ☑️ SearchProfiles    ☑️ SearchPosts       ☑️ SearchBusinesses              ││
│  │ ☑️ SearchNearby      ☑️ GetPostDetails    ☑️ SetCurrentLocation            ││
│  │ ☑️ FollowProfile     ☑️ GetMyProfile      ☐ SearchGovernment               ││
│  │ ☐ SearchTourism     ☐ CreatePost         ☐ DeletePost                     ││
│  │                                                                              ││
│  │ Intent Patterns (regex, one per line):                                       ││
│  │ ┌───────────────────────────────────────────────────────────────────────┐   ││
│  │ │ .*                                                                     │   ││
│  │ └───────────────────────────────────────────────────────────────────────┘   ││
│  │                                                                              ││
│  └─────────────────────────────────────────────────────────────────────────────┘│
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Files to Create
| File | Purpose |
|------|---------|
| `Sivar.Os.Shared/Entities/AgentConfiguration.cs` | Agent config entity |
| `Sivar.Os.Shared/Entities/AgentTool.cs` | Tool registry entity |
| `Sivar.Os.Shared/Repositories/IAgentConfigurationRepository.cs` | Repository interface |
| `Sivar.Os.Data/Repositories/AgentConfigurationRepository.cs` | Repository impl |
| `Sivar.Os/Services/AgentFactory.cs` | Builds agents from config |
| `Sivar.Os/Services/IntentRouter.cs` | Routes messages to agents |
| `Sivar.Os/Controllers/AgentConfigController.cs` | Admin API |
| `Sivar.Os.Client/Pages/Admin/AgentConfig.razor` | Admin UI |

### Files to Modify
| File | Changes |
|------|---------|
| `Sivar.Os/Program.cs` | Replace hardcoded agent with factory |
| `Sivar.Os/Services/ChatService.cs` | Use `IAgentFactory` to get agent |
| `Sivar.Os.Data/Context/SivarDbContext.cs` | Add `DbSet<AgentConfiguration>` |
| `appsettings.json` | Move default configs to seeding |

### Migration: Seed Default Agents
```csharp
// In data seeding, create default agent configs from current hardcoded values
public class AgentConfigurationSeeder
{
    public async Task SeedAsync(SivarDbContext context)
    {
        if (await context.AgentConfigurations.AnyAsync())
            return;

        var defaultAgent = new AgentConfiguration
        {
            AgentKey = "sivar-main",
            DisplayName = "Sivar AI Assistant",
            SystemPrompt = @"You are Sivar, a helpful AI assistant...",
            // ... current hardcoded values
        };

        context.AgentConfigurations.Add(defaultAgent);
        await context.SaveChangesAsync();
    }
}
```

### Acceptance Criteria
- [ ] Agent configurations stored in database
- [ ] System prompts editable via admin UI
- [ ] Tools can be enabled/disabled per agent
- [ ] Intent patterns route to specialized agents
- [ ] Configuration changes apply without restart (cache refresh)
- [ ] Version history tracked for prompts
- [ ] A/B testing support for prompt variations
- [ ] Default agents seeded on first run
- [ ] Existing hardcoded agent migrated to database

### Deliverable
Database-driven agent configuration allowing dynamic management of multiple specialized AI agents without code changes.

---

## Phase 11: Results Ranking & Personalization 📊

> **📌 Related Document**: See `content_ranking.md` for the complete Elo-based content ranking system.
> This phase integrates the content ranking scores into search results with personalization.

### Overview
Currently, search results are ranked by a weighted combination of semantic similarity, full-text match, and geographic proximity. This phase adds:
1. **Content Ranking Integration** - Use Elo-based `CompositeScore` from `content_ranking.md`
2. **Visual ranking indicators** - Show users why results are ranked
3. **User feedback signals** - Learn from clicks, saves, and actions
4. **Personalization** - Boost results based on user history
5. **Business signals** - Ratings, reviews, verification status

### Relationship to content_ranking.md

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Ranking Architecture                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  content_ranking.md                    chat3.md Phase 11             │
│  ┌─────────────────────────┐          ┌─────────────────────────┐   │
│  │ CONTENT RANKING         │          │ PERSONALIZATION         │   │
│  │ (Elo-based, per-content)│          │ (Per-user preferences)  │   │
│  │                         │          │                         │   │
│  │ • LifetimeRating        │          │ • UserAffinityScore     │   │
│  │ • WeeklyRating          │          │ • CategoryPreference    │   │
│  │ • MonthlyRating         │──────────│ • RecentInteractions    │   │
│  │ • CompositeScore        │          │ • SearchHistory         │   │
│  │ • FreshnessDecay        │          │                         │   │
│  │ • VelocityBonus         │          │                         │   │
│  └─────────────────────────┘          └─────────────────────────┘   │
│           │                                    │                     │
│           └────────────────┬───────────────────┘                     │
│                            ▼                                         │
│                 ┌─────────────────────────┐                         │
│                 │     FINAL SCORE          │                         │
│                 │                          │                         │
│                 │ relevance × 0.45         │                         │
│                 │ + content_rank × 0.35    │ ◄── From content_ranking│
│                 │ + personalization × 0.20 │ ◄── From this phase     │
│                 └─────────────────────────┘                         │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Current Ranking System

```sql
-- Current: Weighted combination in PostRepository.HybridSearchAsync()
final_score = 
    (semantic_similarity * semantic_weight) +
    (fulltext_rank * fulltext_weight) +
    (geo_score * geo_weight)
```

**Current Weights (configurable via HybridSearchRequestDto):**
| Signal | Default Weight | Description |
|--------|---------------|-------------|
| Semantic | 0.4 | Vector similarity to query |
| Full-text | 0.3 | PostgreSQL ts_rank |
| Geographic | 0.3 | Distance decay (closer = higher) |

**What's Missing:**
- User doesn't see WHY something ranked higher
- No learning from user behavior
- No business quality signals
- No personalization

### Enhanced Ranking Formula

```
final_score = 
    // Content Relevance (existing)
    (semantic_score * w_semantic) +
    (fulltext_score * w_fulltext) +
    (geo_score * w_geo) +
    
    // Content Ranking (from content_ranking.md)
    (composite_score * w_content_rank) +
    
    // Quality Signals (new)
    (rating_score * w_rating) +
    (review_count_score * w_reviews) +
    (verification_boost * w_verified) +
    (recency_score * w_recency) +
    
    // Personalization (new)
    (user_affinity_score * w_personalization) +
    (category_preference * w_category) +
    
    // Behavioral Signals (new)
    (click_popularity * w_clicks) +
    (action_rate * w_actions)
```

### Visual Ranking Indicators

Show users why each result ranked where it did:

```
┌─────────────────────────────────────────────────────────────────────┐
│  🍕 Pizza Hut Centro                            ⭐ 4.5 (234 reviews) │
│  📍 1.2 km • 🕐 Abierto ahora                                       │
├─────────────────────────────────────────────────────────────────────┤
│  Why this result:                                                    │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ 📍 Nearby (1.2 km)              ████████████████░░░░ 85%       │ │
│  │ 🔤 Matches "pizza"              ████████████████████ 95%       │ │
│  │ ⭐ Highly rated                  ████████████████░░░░ 80%       │ │
│  │ 👥 Popular with users           ████████░░░░░░░░░░░░ 45%       │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  📞 Call   💬 WhatsApp   📍 Directions   💾 Save                    │
└─────────────────────────────────────────────────────────────────────┘
```

### New Entity: `SearchRankingFactors`

```csharp
/// <summary>
/// Stores computed ranking factors for analytics and display
/// </summary>
public class SearchRankingFactors
{
    // Content relevance
    public double SemanticScore { get; set; }
    public double FullTextScore { get; set; }
    public double GeoScore { get; set; }
    
    // Quality signals
    public double RatingScore { get; set; }
    public double ReviewCountScore { get; set; }
    public double VerificationBoost { get; set; }
    public double RecencyScore { get; set; }
    
    // Personalization
    public double UserAffinityScore { get; set; }
    public double CategoryPreference { get; set; }
    
    // Behavioral signals
    public double ClickPopularity { get; set; }
    public double ActionRate { get; set; }
    
    // Final
    public double FinalScore { get; set; }
    
    // For display
    public List<RankingReason> TopReasons { get; set; } = new();
}

public record RankingReason(
    string Icon,
    string Label,
    double Score,
    string Description
);
```

### New Entity: `UserSearchBehavior`

```csharp
/// <summary>
/// Tracks user search behavior for personalization
/// </summary>
public class UserSearchBehavior : BaseEntity
{
    public Guid ProfileId { get; set; }
    
    /// <summary>
    /// Category affinity scores (learned from interactions)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? CategoryAffinities { get; set; }
    // Example: {"restaurant": 0.8, "tourism": 0.3, "government": 0.5}
    
    /// <summary>
    /// Recently interacted business IDs (for boosting)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? RecentInteractions { get; set; }
    
    /// <summary>
    /// Frequently searched terms
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? FrequentQueries { get; set; }
    
    /// <summary>
    /// Preferred result types
    /// </summary>
    [Column(TypeName = "jsonb")]
    public JsonDocument? ResultTypePreferences { get; set; }
    
    /// <summary>
    /// Average search radius (learned from behavior)
    /// </summary>
    public double? PreferredRadiusKm { get; set; }
    
    public DateTimeOffset UpdatedAt { get; set; }
}
```

### New Entity: `ResultClickStats`

```csharp
/// <summary>
/// Aggregated click/action statistics per result (for popularity ranking)
/// Stored in analytics database
/// </summary>
public class ResultClickStats
{
    public Guid PostId { get; set; }
    
    /// <summary>
    /// Times shown in search results (impressions)
    /// </summary>
    public long ImpressionCount { get; set; }
    
    /// <summary>
    /// Times clicked/expanded
    /// </summary>
    public long ClickCount { get; set; }
    
    /// <summary>
    /// Actions taken (call, WhatsApp, save, etc.)
    /// </summary>
    public long ActionCount { get; set; }
    
    /// <summary>
    /// Click-through rate = clicks / impressions
    /// </summary>
    public double ClickThroughRate => ImpressionCount > 0 
        ? (double)ClickCount / ImpressionCount 
        : 0;
    
    /// <summary>
    /// Action rate = actions / clicks
    /// </summary>
    public double ActionRate => ClickCount > 0 
        ? (double)ActionCount / ClickCount 
        : 0;
    
    /// <summary>
    /// When was this last updated?
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
```

### Ranking Configuration

```csharp
/// <summary>
/// Database-stored ranking weights (per category or global)
/// </summary>
public class RankingConfiguration : BaseEntity
{
    [StringLength(50)]
    public string? Category { get; set; }  // null = global default
    
    // Content weights
    public double SemanticWeight { get; set; } = 0.30;
    public double FullTextWeight { get; set; } = 0.20;
    public double GeoWeight { get; set; } = 0.20;
    
    // Quality weights
    public double RatingWeight { get; set; } = 0.10;
    public double ReviewCountWeight { get; set; } = 0.05;
    public double VerifiedWeight { get; set; } = 0.05;
    public double RecencyWeight { get; set; } = 0.02;
    
    // Personalization weights
    public double PersonalizationWeight { get; set; } = 0.05;
    public double CategoryPreferenceWeight { get; set; } = 0.03;
    
    // Behavioral weights
    public double ClickPopularityWeight { get; set; } = 0.00;  // Start at 0, increase as data grows
    public double ActionRateWeight { get; set; } = 0.00;
    
    public bool IsActive { get; set; } = true;
}
```

### Enhanced Search Result DTO

```csharp
public class SearchResultBaseDto
{
    // Existing fields...
    public double RelevanceScore { get; set; }
    public double? DistanceKm { get; set; }
    
    // NEW: Ranking transparency
    public SearchRankingFactors? RankingFactors { get; set; }
    
    // NEW: Quality indicators (for card display)
    public double? AverageRating { get; set; }
    public int? ReviewCount { get; set; }
    public bool IsVerified { get; set; }
    public bool IsOpen { get; set; }
    
    // NEW: Popularity indicators
    public int? RecentViews { get; set; }
    public bool IsPopular { get; set; }  // true if in top 10% for category
}
```

### Ranking Service

```csharp
public interface IRankingService
{
    /// <summary>
    /// Apply full ranking with all signals
    /// </summary>
    Task<List<RankedResult>> RankResultsAsync(
        List<HybridSearchResult> searchResults,
        Guid? profileId,
        string query);

    /// <summary>
    /// Get ranking explanation for display
    /// </summary>
    List<RankingReason> ExplainRanking(SearchRankingFactors factors);

    /// <summary>
    /// Record user interaction for future personalization
    /// </summary>
    Task RecordInteractionAsync(Guid profileId, Guid postId, InteractionType type);
}

public class RankingService : IRankingService
{
    public async Task<List<RankedResult>> RankResultsAsync(
        List<HybridSearchResult> searchResults,
        Guid? profileId,
        string query)
    {
        var config = await GetRankingConfigAsync();
        var userBehavior = profileId.HasValue 
            ? await GetUserBehaviorAsync(profileId.Value) 
            : null;
        var clickStats = await GetClickStatsAsync(searchResults.Select(r => r.Post.Id));

        return searchResults.Select(result =>
        {
            var factors = ComputeRankingFactors(result, userBehavior, clickStats, config);
            return new RankedResult
            {
                Result = result,
                Factors = factors,
                FinalScore = factors.FinalScore,
                TopReasons = GetTopReasons(factors)
            };
        })
        .OrderByDescending(r => r.FinalScore)
        .ToList();
    }

    private SearchRankingFactors ComputeRankingFactors(
        HybridSearchResult result,
        UserSearchBehavior? userBehavior,
        Dictionary<Guid, ResultClickStats> clickStats,
        RankingConfiguration config)
    {
        var factors = new SearchRankingFactors
        {
            // From hybrid search
            SemanticScore = result.SemanticSimilarity ?? 0,
            FullTextScore = result.FullTextRank ?? 0,
            GeoScore = ComputeGeoScore(result.DistanceKm),
            
            // Quality signals
            RatingScore = ComputeRatingScore(result.Post.AverageRating),
            ReviewCountScore = ComputeReviewCountScore(result.Post.ReviewCount),
            VerificationBoost = result.Post.Profile?.IsVerified == true ? 1.0 : 0.0,
            RecencyScore = ComputeRecencyScore(result.Post.CreatedAt),
            
            // Personalization
            UserAffinityScore = ComputeAffinityScore(result.Post, userBehavior),
            CategoryPreference = GetCategoryPreference(result.Post.Category, userBehavior),
            
            // Behavioral
            ClickPopularity = clickStats.TryGetValue(result.Post.Id, out var stats) 
                ? stats.ClickThroughRate 
                : 0,
            ActionRate = stats?.ActionRate ?? 0
        };

        factors.FinalScore = 
            factors.SemanticScore * config.SemanticWeight +
            factors.FullTextScore * config.FullTextWeight +
            factors.GeoScore * config.GeoWeight +
            factors.RatingScore * config.RatingWeight +
            factors.ReviewCountScore * config.ReviewCountWeight +
            factors.VerificationBoost * config.VerifiedWeight +
            factors.RecencyScore * config.RecencyWeight +
            factors.UserAffinityScore * config.PersonalizationWeight +
            factors.CategoryPreference * config.CategoryPreferenceWeight +
            factors.ClickPopularity * config.ClickPopularityWeight +
            factors.ActionRate * config.ActionRateWeight;

        return factors;
    }

    public List<RankingReason> GetTopReasons(SearchRankingFactors factors)
    {
        var reasons = new List<RankingReason>
        {
            new("📍", "Nearby", factors.GeoScore, $"{factors.GeoScore:P0} - Location proximity"),
            new("🔤", "Matches query", factors.SemanticScore, $"{factors.SemanticScore:P0} - Text relevance"),
            new("⭐", "Highly rated", factors.RatingScore, $"{factors.RatingScore:P0} - User ratings"),
            new("👥", "Popular", factors.ClickPopularity, $"{factors.ClickPopularity:P0} - User popularity"),
            new("✅", "Verified", factors.VerificationBoost, "Verified business"),
            new("🕐", "Recent", factors.RecencyScore, "Recently updated")
        };

        return reasons
            .Where(r => r.Score > 0.1)  // Only show meaningful factors
            .OrderByDescending(r => r.Score)
            .Take(4)  // Top 4 reasons
            .ToList();
    }
}
```

### UI Component: Ranking Explanation

```razor
@* Add to ChatMessage.razor card template *@

@if (ShowRankingExplanation && Result.RankingFactors != null)
{
    <MudExpansionPanel Text="Why this result?" Dense="true" Class="mt-2">
        @foreach (var reason in Result.RankingFactors.TopReasons)
        {
            <div class="d-flex align-center mb-1">
                <span class="me-2">@reason.Icon</span>
                <span class="flex-grow-1">@reason.Label</span>
                <MudProgressLinear Value="@(reason.Score * 100)" 
                                   Color="Color.Primary" 
                                   Style="width: 100px; height: 8px;" />
                <span class="ms-2">@reason.Score.ToString("P0")</span>
            </div>
        }
    </MudExpansionPanel>
}
```

### Files to Create
| File | Purpose |
|------|---------|
| `Sivar.Os.Shared/Entities/UserSearchBehavior.cs` | User behavior tracking |
| `Sivar.Os.Shared/Entities/RankingConfiguration.cs` | Ranking weights |
| `Sivar.Os.Analytics/Entities/ResultClickStats.cs` | Click aggregates |
| `Sivar.Os.Shared/DTOs/SearchRankingFactors.cs` | Ranking factors DTO |
| `Sivar.Os/Services/RankingService.cs` | Ranking logic |
| `Sivar.Os/Controllers/RankingConfigController.cs` | Admin API |
| `Sivar.Os.Client/Components/RankingExplanation.razor` | UI component |

### Files to Modify
| File | Changes |
|------|---------|
| `Sivar.Os/Services/SearchResultService.cs` | Integrate `IRankingService` |
| `Sivar.Os.Shared/DTOs/SearchResultDtos.cs` | Add ranking factors to DTOs |
| `Sivar.Os.Client/Components/AIChat/ChatMessage.razor` | Display ranking reasons |
| `Sivar.Os.Analytics/Services/AnalyticsEventConsumer.cs` | Update click stats |

### Acceptance Criteria
- [ ] Results ranked by multi-signal formula
- [ ] Ranking weights configurable via database
- [ ] User sees "Why this result" explanation
- [ ] Click/action stats tracked for popularity
- [ ] User behavior tracked for personalization
- [ ] Verified businesses get ranking boost
- [ ] Ratings/reviews affect ranking
- [ ] A/B testable ranking configurations
- [ ] Admin can adjust weights without code changes
- [ ] Rankings improve over time with behavioral data

### Deliverable
Intelligent, transparent results ranking with personalization and quality signals.

---

## 📅 Implementation Timeline

| Phase | Name | Duration | Dependencies |
|-------|------|----------|--------------|
| **0** | Location-Aware Chat | 3-4 days | None (Foundation) |
| **0.5** | Configurable Welcome Messages | 2-3 days | None (Foundation) |
| **1** | Enhanced Contact Actions | 2-3 days | None |
| **2** | Unified Search Pipeline | 3-4 days | Phases 0, 0.5 |
| **3** | Interactive Procedure Cards | 3-4 days | Phase 2 |
| **4** | Smart Follow-up Suggestions | 2-3 days | Phase 2 |
| **5** | Real-time Business Status | 2-3 days | Phase 1 |
| **6** | Intent-Based Routing | 4-5 days | Phase 2 |
| **7** | Map View Integration | 4-5 days | Phases 0, 2, 5 |
| **8** | Saved Results & Favorites | 3-4 days | Phase 2 |
| **9** | Chat Analytics & Metrics | 4-5 days | Phase 2 |
| **9.5** | Session Tracing & Debugging | 3-4 days | Phase 9 |
| **10** | Multi-Agent Configuration | 4-5 days | Phase 6 |
| **11** | Results Ranking & Personalization | 5-6 days | Phases 2, 9 |

### Dependency Graph
```
Phase 0 (Location) ────┬──► Phase 2 (Unified Search) ──┬──► Phase 3 (Procedures)
                       │                               ├──► Phase 4 (Suggestions)
Phase 0.5 (Settings) ──┘                               ├──► Phase 6 (Intent) ──► Phase 10 (Multi-Agent)
                                                       ├──► Phase 8 (Saved Results)
                                                       ├──► Phase 9 (Analytics) ──► Phase 9.5 (Tracing)
                                                       │                               │
                                                       └───────────────────────────────┼──► Phase 11 (Ranking)
                                                                                       │
Phase 0 (Location) ────────► Phase 7 (Map View)                                        │
                                    ▲                                                  │
Phase 1 (Contact) ──► Phase 5 (Open Status) ──┘                                        │
                                                                                       │
                     Phase 9 (Analytics) ─────────────────────────────────────────────┘
                           └── Provides click/action data for ranking personalization
```

### Phase Categories

| Category | Phases | Focus |
|----------|--------|-------|
| **Foundation** | 0, 0.5 | Location, Settings |
| **User Experience** | 1, 3, 4, 7 | Contact, Procedures, Suggestions, Map |
| **Search Quality** | 2, 6, 11 | Pipeline, Intent Routing, Ranking |
| **Data & Analytics** | 8, 9, 9.5 | Saved Results, Analytics, Tracing |
| **Platform** | 5, 10 | Business Status, Agent Config |

---

## 🎯 Quick Wins (Can Start Immediately)

These can be done in parallel with any phase:

1. **Add WhatsApp button** - 1 hour
2. **Add Email button** - 1 hour  
3. **Show office hours on government cards** - 2 hours
4. **Add "Copiar teléfono" action** - 30 min
5. **Improve card loading animation** - 1 hour
6. **Add location indicator placeholder** - 1 hour

---

## 📊 Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Searches returning cards | ~60% | 95% |
| Contact actions per session | N/A | Track |
| Procedure completion rate | N/A | Track |
| Results saved per user | 0 | >2 |
| Map views per session | 0 | >1 |
| Location-aware searches | ~30% | 90% |
| Avg distance shown in results | N/A | Always |
| Avg session duration | N/A | Track (target >3 min) |
| Search success rate | N/A | >85% |
| Click-through rate | N/A | >40% |
| Action conversion rate | N/A | >25% |

---

## 🔧 Technical Notes

### Current Architecture
- **Frontend**: Blazor WebAssembly with MudBlazor
- **AI**: Microsoft Agent Framework (`AIAgent`) with tool calling
- **Search**: PostgreSQL + pgvector (semantic) + PostGIS (geo) + tsvector (full-text)
- **Data**: Hybrid search with configurable weights
- **Content Ranking**: Elo-inspired system (see `content_ranking.md`)

### Location Infrastructure (Already Exists)
- `ILocationService` - Abstraction for location operations
- `ChatFunctionService.SetCurrentLocation()` - Sets user coordinates
- `SearchNearbyProfiles()` / `SearchNearbyPosts()` - PostGIS spatial queries
- `HybridSearchRequestDto` - Has `UserLatitude`, `UserLongitude`, `MaxDistanceKm`
- Browser Geolocation docs: `BROWSER_GPS_IMPLEMENTATION_COMPLETE.md`

### Related Plan Documents
| Document | Purpose |
|----------|---------|
| `chat3.md` (this file) | Chat system improvement phases |
| `content_ranking.md` | Elo-based content ranking system |
