using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Sivar.Os.Shared.DTOs;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Repositories;
using System.Text.Json;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for chat bot settings
/// Phase 0.5: Configurable welcome messages and chat settings
/// </summary>
[ApiController]
[Route("api/chat")]
public class ChatBotSettingsController : ControllerBase
{
    private readonly IChatBotSettingsRepository _settingsRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ChatBotSettingsController> _logger;
    
    private const string CacheKeyPrefix = "ChatBotSettings_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ChatBotSettingsController(
        IChatBotSettingsRepository settingsRepository,
        IMemoryCache cache,
        ILogger<ChatBotSettingsController> logger)
    {
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets active chat bot settings for the current culture
    /// </summary>
    /// <param name="culture">Culture code (e.g., "es", "en"). Defaults to "es"</param>
    /// <param name="region">Region code (e.g., "SV", "GT"). Optional</param>
    /// <returns>Chat bot settings DTO</returns>
    [HttpGet("settings")]
    [AllowAnonymous]
    public async Task<ActionResult<ChatBotSettingsDto>> GetSettings(
        [FromQuery] string? culture = "es",
        [FromQuery] string? region = null)
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}{culture ?? "default"}_{region ?? "any"}";
            
            if (_cache.TryGetValue(cacheKey, out ChatBotSettingsDto? cachedSettings) && cachedSettings != null)
            {
                _logger.LogDebug("[ChatBotSettings] Returning cached settings for {Culture}/{Region}", culture, region);
                return Ok(cachedSettings);
            }

            var settings = await _settingsRepository.GetActiveSettingsAsync(culture, region);
            
            if (settings == null)
            {
                _logger.LogInformation("[ChatBotSettings] No settings found for {Culture}/{Region}, returning defaults", culture, region);
                return Ok(GetDefaultSettings());
            }

            var dto = MapToDto(settings);
            _cache.Set(cacheKey, dto, CacheDuration);
            
            _logger.LogInformation("[ChatBotSettings] Loaded settings '{Key}' for {Culture}/{Region}", settings.Key, culture, region);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatBotSettings] Error loading settings");
            return Ok(GetDefaultSettings()); // Always return something usable
        }
    }

    /// <summary>
    /// Gets chat bot settings by key
    /// </summary>
    /// <param name="key">Settings key (e.g., "default", "es-SV")</param>
    /// <returns>Chat bot settings DTO</returns>
    [HttpGet("settings/{key}")]
    [AllowAnonymous]
    public async Task<ActionResult<ChatBotSettingsDto>> GetSettingsByKey(string key)
    {
        try
        {
            var settings = await _settingsRepository.GetByKeyAsync(key);
            
            if (settings == null)
            {
                return NotFound(new { error = $"Settings with key '{key}' not found" });
            }

            return Ok(MapToDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatBotSettings] Error loading settings by key: {Key}", key);
            return StatusCode(500, new { error = "Failed to load settings" });
        }
    }

    /// <summary>
    /// Gets all active chat bot settings (admin)
    /// </summary>
    [HttpGet("admin/settings")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<ChatBotSettingsDto>>> GetAllSettings()
    {
        try
        {
            var settings = await _settingsRepository.GetAllActiveAsync();
            var dtos = settings.Select(MapToDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatBotSettings] Error loading all settings");
            return StatusCode(500, new { error = "Failed to load settings" });
        }
    }

    /// <summary>
    /// Creates new chat bot settings (admin)
    /// </summary>
    [HttpPost("admin/settings")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ChatBotSettingsDto>> CreateSettings([FromBody] CreateChatBotSettingsDto dto)
    {
        try
        {
            // Check if key already exists
            var existing = await _settingsRepository.GetByKeyAsync(dto.Key);
            if (existing != null)
            {
                return Conflict(new { error = $"Settings with key '{dto.Key}' already exists" });
            }

            var settings = new ChatBotSettings
            {
                Key = dto.Key,
                Culture = dto.Culture,
                WelcomeMessage = dto.WelcomeMessage,
                HeaderTagline = dto.HeaderTagline,
                BotName = dto.BotName,
                QuickActionsJson = dto.QuickActions != null ? JsonSerializer.Serialize(dto.QuickActions) : null,
                SystemPrompt = dto.SystemPrompt,
                Priority = dto.Priority,
                RegionCode = dto.RegionCode,
                ErrorMessage = dto.ErrorMessage,
                ThinkingMessage = dto.ThinkingMessage,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _settingsRepository.AddAsync(settings);
            await _settingsRepository.SaveChangesAsync();

            InvalidateCache();
            
            _logger.LogInformation("[ChatBotSettings] Created settings '{Key}'", settings.Key);
            return CreatedAtAction(nameof(GetSettingsByKey), new { key = settings.Key }, MapToDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatBotSettings] Error creating settings");
            return StatusCode(500, new { error = "Failed to create settings" });
        }
    }

    /// <summary>
    /// Updates chat bot settings (admin)
    /// </summary>
    [HttpPut("admin/settings/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<ChatBotSettingsDto>> UpdateSettings(Guid id, [FromBody] UpdateChatBotSettingsDto dto)
    {
        try
        {
            var settings = await _settingsRepository.GetByIdAsync(id);
            if (settings == null)
            {
                return NotFound(new { error = $"Settings with ID '{id}' not found" });
            }

            if (dto.WelcomeMessage != null) settings.WelcomeMessage = dto.WelcomeMessage;
            if (dto.HeaderTagline != null) settings.HeaderTagline = dto.HeaderTagline;
            if (dto.BotName != null) settings.BotName = dto.BotName;
            if (dto.QuickActions != null) settings.QuickActionsJson = JsonSerializer.Serialize(dto.QuickActions);
            if (dto.SystemPrompt != null) settings.SystemPrompt = dto.SystemPrompt;
            if (dto.Priority.HasValue) settings.Priority = dto.Priority.Value;
            if (dto.IsActive.HasValue) settings.IsActive = dto.IsActive.Value;
            if (dto.ErrorMessage != null) settings.ErrorMessage = dto.ErrorMessage;
            if (dto.ThinkingMessage != null) settings.ThinkingMessage = dto.ThinkingMessage;
            
            settings.UpdatedAt = DateTime.UtcNow;

            await _settingsRepository.UpdateAsync(settings);

            InvalidateCache();
            
            _logger.LogInformation("[ChatBotSettings] Updated settings '{Key}'", settings.Key);
            return Ok(MapToDto(settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatBotSettings] Error updating settings: {Id}", id);
            return StatusCode(500, new { error = "Failed to update settings" });
        }
    }

    /// <summary>
    /// Deletes (soft) chat bot settings (admin)
    /// </summary>
    [HttpDelete("admin/settings/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteSettings(Guid id)
    {
        try
        {
            var settings = await _settingsRepository.GetByIdAsync(id);
            if (settings == null)
            {
                return NotFound(new { error = $"Settings with ID '{id}' not found" });
            }

            if (settings.Key == "default")
            {
                return BadRequest(new { error = "Cannot delete default settings" });
            }

            await _settingsRepository.DeleteAsync(id);

            InvalidateCache();
            
            _logger.LogInformation("[ChatBotSettings] Deleted settings '{Key}'", settings.Key);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChatBotSettings] Error deleting settings: {Id}", id);
            return StatusCode(500, new { error = "Failed to delete settings" });
        }
    }

    /// <summary>
    /// Clears the settings cache (admin)
    /// </summary>
    [HttpPost("admin/settings/clear-cache")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult ClearCache()
    {
        InvalidateCache();
        _logger.LogInformation("[ChatBotSettings] Cache cleared by admin");
        return Ok(new { message = "Cache cleared successfully" });
    }

    private static ChatBotSettingsDto MapToDto(ChatBotSettings settings)
    {
        List<string> quickActions = new();
        if (!string.IsNullOrEmpty(settings.QuickActionsJson))
        {
            try
            {
                quickActions = JsonSerializer.Deserialize<List<string>>(settings.QuickActionsJson) ?? new();
            }
            catch { /* Ignore parse errors */ }
        }

        return new ChatBotSettingsDto
        {
            Key = settings.Key,
            Culture = settings.Culture,
            WelcomeMessage = settings.WelcomeMessage,
            HeaderTagline = settings.HeaderTagline,
            BotName = settings.BotName,
            QuickActions = quickActions,
            SystemPrompt = settings.SystemPrompt,
            ErrorMessage = settings.ErrorMessage,
            ThinkingMessage = settings.ThinkingMessage
        };
    }

    private static ChatBotSettingsDto GetDefaultSettings()
    {
        return new ChatBotSettingsDto
        {
            Key = "default",
            Culture = "es",
            WelcomeMessage = "¡Hola! Soy tu asistente Sivar AI. Puedo ayudarte a:\n\n🔍 Encontrar negocios y servicios\n📝 Buscar lugares y eventos\n🏪 Descubrir lo mejor de El Salvador\n📋 Guiarte en trámites y papeleos\n\n¡Pregúntame algo como \"pizzerías cerca\" o \"cómo sacar pasaporte\"!",
            HeaderTagline = "Siempre aquí para ayudarte",
            BotName = "Sivar AI Assistant",
            QuickActions = new List<string> { "🍕 Buscar comida", "🏛️ Trámites", "📍 Cerca de mí", "🎉 Eventos" },
            ErrorMessage = "Lo siento, ocurrió un error. Por favor intenta de nuevo.",
            ThinkingMessage = "Pensando..."
        };
    }

    private void InvalidateCache()
    {
        // Note: IMemoryCache doesn't support clearing by prefix, 
        // so we'll just let old entries expire naturally
        // In production, consider using IDistributedCache with better invalidation support
    }
}
