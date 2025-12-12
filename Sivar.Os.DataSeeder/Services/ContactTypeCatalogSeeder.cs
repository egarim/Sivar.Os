using Microsoft.Extensions.Logging;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;

namespace Sivar.Os.DataSeeder.Services;

/// <summary>
/// Service for seeding the contact type catalog with default values
/// </summary>
public class ContactTypeCatalogSeeder
{
    private readonly IContactTypeRepository _contactTypeRepository;
    private readonly ILogger<ContactTypeCatalogSeeder> _logger;

    public ContactTypeCatalogSeeder(
        IContactTypeRepository contactTypeRepository,
        ILogger<ContactTypeCatalogSeeder> logger)
    {
        _contactTypeRepository = contactTypeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Seed default contact types
    /// </summary>
    public async Task SeedContactTypesAsync()
    {
        _logger.LogInformation("🌱 Starting contact type catalog seeding...");

        var contactTypes = GetDefaultContactTypes();
        var seededCount = 0;

        foreach (var contactType in contactTypes)
        {
            if (!await _contactTypeRepository.KeyExistsAsync(contactType.Key))
            {
                await _contactTypeRepository.AddAsync(contactType);
                seededCount++;
                _logger.LogDebug("  ✓ Added contact type: {Key}", contactType.Key);
            }
            else
            {
                _logger.LogDebug("  ○ Contact type already exists: {Key}", contactType.Key);
            }
        }

        _logger.LogInformation("✅ Contact type seeding completed. Added {Count} new types.", seededCount);
    }

    /// <summary>
    /// Get the default contact types for the catalog
    /// </summary>
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
            Color = "#4CAF50",
            UrlTemplate = "tel:+{country_code}{value}",
            Category = "phone",
            SortOrder = 1,
            RegionalPopularity = @"{""SV"": 100, ""US"": 100, ""MX"": 100, ""GT"": 100}",
            ValidationRegex = @"^\d{8}$"
        },
        new ContactType
        {
            Key = "sms",
            DisplayName = "SMS",
            Icon = "💬",
            MudBlazorIcon = "Icons.Material.Filled.Sms",
            Color = "#2196F3",
            UrlTemplate = "sms:+{country_code}{value}?body={message}",
            Category = "phone",
            SortOrder = 2,
            MobileOnly = true,
            RegionalPopularity = @"{""SV"": 60, ""US"": 80}"
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
            Color = "#25D366",
            UrlTemplate = "https://wa.me/{country_code}{value}?text={message}",
            Category = "messaging",
            SortOrder = 1,
            RegionalPopularity = @"{""SV"": 100, ""MX"": 95, ""ES"": 90, ""BR"": 95, ""GT"": 98, ""HN"": 95, ""US"": 40, ""RU"": 10}",
            ValidationRegex = @"^\d{8,15}$",
            Placeholder = "7XXX-XXXX"
        },
        new ContactType
        {
            Key = "telegram",
            DisplayName = "Telegram",
            Icon = "✈️",
            MudBlazorIcon = "Icons.Custom.Brands.Telegram",
            Color = "#0088CC",
            UrlTemplate = "https://t.me/{value}",
            Category = "messaging",
            SortOrder = 2,
            RegionalPopularity = @"{""RU"": 100, ""IR"": 90, ""UA"": 85, ""US"": 30, ""SV"": 15}",
            Placeholder = "@username or +phone"
        },
        new ContactType
        {
            Key = "messenger",
            DisplayName = "Messenger",
            Icon = "💬",
            MudBlazorIcon = "Icons.Custom.Brands.Facebook",
            Color = "#0084FF",
            UrlTemplate = "https://m.me/{value}",
            Category = "messaging",
            SortOrder = 3,
            RegionalPopularity = @"{""US"": 70, ""SV"": 50, ""MX"": 45}",
            Placeholder = "Facebook username or Page ID"
        },
        new ContactType
        {
            Key = "signal",
            DisplayName = "Signal",
            Icon = "🔒",
            Color = "#3A76F0",
            UrlTemplate = "https://signal.me/#p/+{country_code}{value}",
            Category = "messaging",
            SortOrder = 4,
            RegionalPopularity = @"{""US"": 25, ""DE"": 40, ""SV"": 5}"
        },
        new ContactType
        {
            Key = "imessage",
            DisplayName = "iMessage",
            Icon = "💬",
            Color = "#34C759",
            UrlTemplate = "imessage:+{country_code}{value}",
            Category = "messaging",
            SortOrder = 5,
            MobileOnly = true,
            RegionalPopularity = @"{""US"": 60, ""SV"": 20}",
            Metadata = @"{""platform"": ""ios""}"
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
            Color = "#EA4335",
            UrlTemplate = "mailto:{value}?subject={subject}&body={message}",
            Category = "email",
            SortOrder = 1,
            RegionalPopularity = @"{""SV"": 80, ""US"": 90, ""MX"": 75}",
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
            RegionalPopularity = @"{""SV"": 90, ""US"": 70, ""MX"": 85}",
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
            RegionalPopularity = @"{""SV"": 85, ""US"": 80, ""MX"": 80}",
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
            RegionalPopularity = @"{""SV"": 75, ""US"": 85, ""MX"": 80}",
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
            RegionalPopularity = @"{""US"": 60, ""SV"": 30, ""MX"": 45}",
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
            RegionalPopularity = @"{""US"": 70, ""SV"": 40, ""MX"": 50}",
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
            RegionalPopularity = @"{""SV"": 80, ""US"": 85, ""MX"": 85}",
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
            UrlTemplate = "{value}",
            Category = "web",
            SortOrder = 1,
            RegionalPopularity = @"{""SV"": 70, ""US"": 90, ""MX"": 75}",
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
        // LOCATION / NAVIGATION
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
            RegionalPopularity = @"{""SV"": 90, ""US"": 95, ""MX"": 90}",
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
            RegionalPopularity = @"{""SV"": 85, ""US"": 50, ""MX"": 75, ""IL"": 90}",
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
            RegionalPopularity = @"{""US"": 40, ""SV"": 15}",
            Metadata = @"{""platform"": ""ios""}"
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
            RegionalPopularity = @"{""SV"": 95, ""US"": 90, ""MX"": 90}",
            OpenInNewTab = true
        },

        // ============================================
        // DELIVERY SERVICES (Regional)
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
            RegionalPopularity = @"{""US"": 90, ""SV"": 75, ""MX"": 85}",
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
            RegionalPopularity = @"{""SV"": 95, ""GT"": 90, ""HN"": 85, ""NI"": 80}",
            OpenInNewTab = true
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
            RegionalPopularity = @"{""SV"": 85, ""AR"": 95, ""UY"": 95, ""BO"": 80}",
            OpenInNewTab = true
        },
        new ContactType
        {
            Key = "rappi",
            DisplayName = "Rappi",
            Icon = "🛒",
            Color = "#FF4B4B",
            UrlTemplate = "https://www.rappi.com.sv/restaurantes/{value}",
            Category = "delivery",
            SortOrder = 4,
            RegionalPopularity = @"{""MX"": 90, ""CO"": 95, ""BR"": 85, ""SV"": 50}",
            OpenInNewTab = true
        },
        new ContactType
        {
            Key = "doordash",
            DisplayName = "DoorDash",
            Icon = "🚪",
            Color = "#FF3008",
            UrlTemplate = "https://www.doordash.com/store/{value}",
            Category = "delivery",
            SortOrder = 5,
            RegionalPopularity = @"{""US"": 95, ""CA"": 85}",
            OpenInNewTab = true
        }
    };
}
