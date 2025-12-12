using Microsoft.Extensions.Caching.Memory;
using Sivar.Os.Shared;
using Sivar.Os.Shared.Clients;
using Sivar.Os.Shared.DTOs;
using System.Globalization;

namespace Sivar.Os.Client.Services;

/// <summary>
/// Client-side service for loading and caching chat bot settings.
/// Phase 0.5: Configurable welcome messages and chat settings.
/// </summary>
public class ChatSettingsService
{
    private readonly ISivarClient _sivarClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ChatSettingsService> _logger;
    
    private const string CacheKey = "ChatBotSettings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    
    private ChatBotSettingsDto? _currentSettings;

    public ChatSettingsService(
        ISivarClient sivarClient,
        IMemoryCache cache,
        ILogger<ChatSettingsService> logger)
    {
        _sivarClient = sivarClient ?? throw new ArgumentNullException(nameof(sivarClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current chat bot settings
    /// </summary>
    public ChatBotSettingsDto CurrentSettings => _currentSettings ?? GetDefaultSettings();

    /// <summary>
    /// Whether settings have been loaded
    /// </summary>
    public bool IsLoaded => _currentSettings != null;

    /// <summary>
    /// Loads chat bot settings from the API
    /// </summary>
    /// <param name="culture">Culture code (e.g., "es", "en"). If null, uses current culture</param>
    /// <param name="forceReload">Force reload even if cached</param>
    /// <returns>Loaded settings</returns>
    public async Task<ChatBotSettingsDto> LoadSettingsAsync(string? culture = null, bool forceReload = false)
    {
        try
        {
            culture ??= CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var cacheKey = $"{CacheKey}_{culture}";

            if (!forceReload && _cache.TryGetValue(cacheKey, out ChatBotSettingsDto? cachedSettings) && cachedSettings != null)
            {
                _currentSettings = cachedSettings;
                _logger.LogDebug("[ChatSettingsService] Returning cached settings for {Culture}", culture);
                return cachedSettings;
            }

            _logger.LogInformation("[ChatSettingsService] Loading settings for culture: {Culture}", culture);
            
            var settings = await _sivarClient.Chat.GetSettingsAsync(culture, null, forceReload);
            
            if (settings != null)
            {
                _currentSettings = settings;
                _cache.Set(cacheKey, settings, CacheDuration);
                
                // Detailed logging for debugging quick actions
                _logger.LogInformation("[ChatSettingsService] ✅ Loaded settings '{Key}' for {Culture} with {Count} QuickActionItems", 
                    settings.Key, culture, settings.QuickActionItems?.Count ?? 0);
                
                if (settings.QuickActionItems != null && settings.QuickActionItems.Any())
                {
                    foreach (var qa in settings.QuickActionItems)
                    {
                        _logger.LogInformation("[ChatSettingsService] 📋 QuickAction: Label='{Label}', IsActive={IsActive}, DefaultQuery='{DefaultQuery}', CapabilityKey='{CapabilityKey}'",
                            qa.Label, qa.IsActive, qa.DefaultQuery, qa.CapabilityKey);
                    }
                }
                else
                {
                    _logger.LogWarning("[ChatSettingsService] ⚠️ No QuickActionItems loaded! Check database QuickActions table.");
                }
                
                return settings;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatSettingsService] Error loading settings, using defaults");
        }

        // Fall back to defaults
        _currentSettings = GetDefaultSettings();
        return _currentSettings;
    }

    /// <summary>
    /// Gets the welcome message
    /// </summary>
    public string GetWelcomeMessage()
    {
        return CurrentSettings.WelcomeMessage;
    }

    /// <summary>
    /// Gets quick action items with full metadata
    /// </summary>
    public List<QuickActionDto> GetQuickActionItems()
    {
        return CurrentSettings.QuickActionItems;
    }

    /// <summary>
    /// Gets the bot name
    /// </summary>
    public string GetBotName()
    {
        return CurrentSettings.BotName;
    }

    /// <summary>
    /// Gets the header tagline
    /// </summary>
    public string GetHeaderTagline()
    {
        return CurrentSettings.HeaderTagline ?? "Always here to help you explore";
    }

    /// <summary>
    /// Gets the system prompt for AI
    /// </summary>
    public string? GetSystemPrompt()
    {
        return CurrentSettings.SystemPrompt;
    }

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string GetErrorMessage()
    {
        return CurrentSettings.ErrorMessage ?? "Lo siento, ocurrió un error. Por favor intenta de nuevo.";
    }

    /// <summary>
    /// Gets the thinking/loading message
    /// </summary>
    public string GetThinkingMessage()
    {
        return CurrentSettings.ThinkingMessage ?? "Pensando...";
    }

    /// <summary>
    /// Clears cached settings
    /// </summary>
    public void ClearCache()
    {
        _currentSettings = null;
        // Note: IMemoryCache doesn't support clearing by key pattern
        // Cache will expire naturally
    }

    private static ChatBotSettingsDto GetDefaultSettings()
    {
        return new ChatBotSettingsDto
        {
            Id = Guid.Empty,
            Key = "default",
            Culture = "es",
            WelcomeMessage = "¡Hola! Soy tu asistente Sivar AI. Puedo ayudarte a:\n\n🔍 Encontrar negocios y servicios\n📝 Buscar lugares y eventos\n🏪 Descubrir lo mejor de El Salvador\n📋 Guiarte en trámites y papeleos\n\n¡Pregúntame algo como \"pizzerías cerca\" o \"cómo sacar pasaporte\"!",
            HeaderTagline = "Siempre aquí para ayudarte",
            BotName = "Sivar AI Assistant",
            QuickActionItems = new List<QuickActionDto>
            {
                new() { Id = Guid.NewGuid(), Label = "🍕 Buscar comida", Icon = "🍕", Color = "#FF5722", DefaultQuery = "Buscar restaurantes cerca", SortOrder = 1, RequiresLocation = true },
                new() { Id = Guid.NewGuid(), Label = "🏛️ Trámites", Icon = "🏛️", Color = "#3F51B5", DefaultQuery = "¿Qué trámites puedo hacer?", SortOrder = 2, RequiresLocation = false },
                new() { Id = Guid.NewGuid(), Label = "📍 Cerca de mí", Icon = "📍", Color = "#4CAF50", DefaultQuery = "¿Qué hay cerca de mí?", SortOrder = 3, RequiresLocation = true },
                new() { Id = Guid.NewGuid(), Label = "🎉 Eventos", Icon = "🎉", Color = "#9C27B0", DefaultQuery = "¿Qué eventos hay?", SortOrder = 4, RequiresLocation = false }
            },
            ErrorMessage = "Lo siento, ocurrió un error. Por favor intenta de nuevo.",
            ThinkingMessage = "Pensando..."
        };
    }
}
